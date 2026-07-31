using Enset.Application.Authorization;
using Enset.Application.Quality;
using Enset.Domain.Buildings;
using Enset.Domain.Curation;
using Enset.Domain.Energy;
using Enset.Domain.Quality;
using Enset.Domain.Users;
using Enset.Infrastructure.Persistence;
using Enset.Infrastructure.Quality;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class QualityPersistencePostgresTests
{
    [Fact]
    public async Task Phase1_history_constraints_authorization_and_restart_are_persistent()
    {
        var connection = Environment.GetEnvironmentVariable("ENSET_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(connection)) return;
        var options = new DbContextOptionsBuilder<EnsetDbContext>().UseNpgsql(connection).Options;
        var user = new CurrentUserContext();
        user.Initialize(Guid.NewGuid(), true, [GlobalUserRole.EnsetEmployee.ToString()]);
        Guid buildingId, meterId, analysisId, issueId;
        await using (var db = new EnsetDbContext(options, user))
        {
            var building = new Building { BuildingNumber = $"Q-{Guid.NewGuid():N}", Name = "Quality test" };
            var meter = new Meter { MeterNumber = $"M-{Guid.NewGuid():N}", Name = "Meter", Building = building,
                Medium = MeterMedium.Electricity, Quantity = MeterQuantity.Energy,
                Unit = MeterUnit.KWh, Direction = MeterDirection.Consumption };
            db.AddRange(building, meter); await db.SaveChangesAsync();
            buildingId=building.Id; meterId=meter.Id;
            var service = new EfQualityPersistenceService(db, user);
            Assert.Equal(1, (await service.DeclareInventory(buildingId, new(true,true,true,null,"Test"), default)).VersionNumber);
            Assert.Equal(2, (await service.DeclareInventory(buildingId, new(true,true,true,null,"Test"), default)).VersionNumber);
            Assert.Single((await service.GetDeclarationHistory(buildingId,1,50,default)).Items, x=>x.IsCurrent);
            var analysis=await service.StartAnalysis(meterId,DateTime.UtcNow.AddDays(-1),DateTime.UtcNow,"1.0",default);
            analysis=await service.CompleteAnalysis(analysis.Id,new(96,96,100,0,1,1,0,900,"KilowattHour","review",ProfileAnalysisStatus.RequiresReview),default);
            analysisId=analysis.Id;
            var issue=new MeterProfileIssue{Code="OUTLIER",Category=ProfileIssueCategory.Outlier,Severity=ProfileIssueSeverity.Blocking,Message="Ausreißer",IsBlocking=true,ResolutionStatus=ProfileIssueResolutionStatus.Open};
            await service.AddIssues(analysis.Id,[issue],default);issueId=issue.Id;
            await service.Decide(issue.Id,new(ProfileDecisionType.ConfirmAsCorrect,"1","1",null,null,"fachlich geprüft",null,true,null,"Test"),default);
        }
        await using (var restarted = new EnsetDbContext(options))
        {
            Assert.Equal(2, await restarted.BuildingInventoryDeclarations.CountAsync(x=>x.BuildingId==buildingId));
            Assert.True(await restarted.MeterProfileAnalyses.AnyAsync(x=>x.Id==analysisId&&x.IsCurrent));
            Assert.True(await restarted.MeterProfileIssues.AnyAsync(x=>x.Id==issueId));
            Assert.True(await restarted.MeterProfileCurationDecisions.AnyAsync(x=>x.MeterProfileIssueId==issueId));
            Assert.True(await restarted.EntityAuditEntries.AnyAsync(x=>x.EntityType=="MeterProfileCurationDecision"));
            var assessment = await new EfHierarchicalQualityAssessmentService(restarted)
                .AssessMeters([meterId], default);
            Assert.Equal(ProfileAnalysisStatus.RequiresReview,
                assessment[meterId].ProfileAnalysisStatus);
            Assert.Equal(DataMaturityLevel.Silver,
                assessment[meterId].QualityLevel);
        }
        await using (var constraintCheck = new EnsetDbContext(options))
        {
            constraintCheck.BuildingInventoryDeclarations.Add(new()
            {
                BuildingId=buildingId,VersionNumber=3,IsCurrent=true,
                ConfirmedByUserId=Guid.NewGuid(),ConfirmedAtUtc=DateTime.UtcNow,
                CreatedAtUtc=DateTime.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(()=>constraintCheck.SaveChangesAsync());
        }
        await using (var checkConstraint = new EnsetDbContext(options))
        {
            checkConstraint.MeterProfileAnalyses.Add(new()
            {
                MeterId=meterId,VersionNumber=2,AnalysisVersion="invalid",
                PeriodFromUtc=DateTime.UtcNow.AddHours(-1),PeriodToUtc=DateTime.UtcNow,
                CompletenessPercentage=101,AnalysisStatus=ProfileAnalysisStatus.Completed,
                ExecutedAtUtc=DateTime.UtcNow,CreatedAtUtc=DateTime.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(()=>checkConstraint.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Customer_context_cannot_confirm_inventory()
    {
        var options=new DbContextOptionsBuilder<EnsetDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var customer=new CurrentUserContext(); customer.Initialize(Guid.NewGuid(),false,[UserCustomerRole.CustomerAdmin.ToString()]);
        await using var db=new EnsetDbContext(options,customer);
        var service=new EfQualityPersistenceService(db,customer);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.DeclareInventory(Guid.NewGuid(),new(true,true,true,null,null),default));
    }
}
