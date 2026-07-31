using Enset.Application.Authorization;
using Enset.Application.Quality;
using Enset.Domain.Buildings;
using Enset.Domain.Energy;
using Enset.Domain.Quality;
using Enset.Domain.Users;
using Enset.Infrastructure.Persistence;
using Enset.Infrastructure.Quality;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enset.Import.Tests;

public sealed class QualityDisplayNameResolutionTests
{
    private static async Task<(EnsetDbContext Db, CurrentUserContext User, Guid IssueId)> Fixture(
        DbContextOptions<EnsetDbContext> options, Guid employeeId)
    {
        var user = new CurrentUserContext();
        user.Initialize(employeeId, true, [GlobalUserRole.EnsetEmployee.ToString()]);
        var db = new EnsetDbContext(options, user);
        db.ApplicationUsers.Add(new ApplicationUser
        {
            Id = employeeId, ExternalIdentity = "ext", DisplayName = "Maria Muster", Email = "maria@example.com"
        });
        var building = new Building { BuildingNumber = $"DN-{Guid.NewGuid():N}", Name = "Test" };
        var meter = new Meter
        {
            MeterNumber = $"M-{Guid.NewGuid():N}", Name = "Meter", Building = building,
            Medium = MeterMedium.Electricity, Quantity = MeterQuantity.Energy,
            Unit = MeterUnit.KWh, Direction = MeterDirection.Consumption
        };
        db.AddRange(building, meter);
        await db.SaveChangesAsync();
        var analysis = new MeterProfileAnalysis
        {
            MeterId = meter.Id, AnalysisVersion = "1.0", PeriodFromUtc = DateTime.UtcNow.AddDays(-1),
            PeriodToUtc = DateTime.UtcNow, AnalysisStatus = ProfileAnalysisStatus.Completed,
            ExecutedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
        };
        db.Add(analysis);
        await db.SaveChangesAsync();
        var issue = new MeterProfileIssue
        {
            MeterProfileAnalysisId = analysis.Id, MeterId = meter.Id, Code = "OUTLIER",
            Category = ProfileIssueCategory.Outlier, Severity = ProfileIssueSeverity.Warning,
            Message = "Ausreißer", ResolutionStatus = ProfileIssueResolutionStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Add(issue);
        await db.SaveChangesAsync();
        return (db, user, issue.Id);
    }

    [Fact]
    public async Task Decision_resolves_display_name_from_user_directory_when_not_provided()
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var employeeId = Guid.NewGuid();
        var (db, user, issueId) = await Fixture(options, employeeId);
        await using var _ = db;

        var service = new EfQualityPersistenceService(db, user);
        var decision = await service.Decide(issueId, new(
            ProfileDecisionType.ConfirmAsCorrect, null, null, null, null,
            "Fachlich geprüft", null, true, null, null), default);

        Assert.Equal("Maria Muster", decision.DecidedByDisplayNameSnapshot);
    }

    [Fact]
    public async Task Explicit_display_name_takes_precedence_over_directory_lookup()
    {
        var options = new DbContextOptionsBuilder<EnsetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var employeeId = Guid.NewGuid();
        var (db, user, issueId) = await Fixture(options, employeeId);
        await using var _ = db;

        var service = new EfQualityPersistenceService(db, user);
        var decision = await service.Decide(issueId, new(
            ProfileDecisionType.ConfirmAsCorrect, null, null, null, null,
            "Fachlich geprüft", null, true, null, "Explizit übergeben"), default);

        Assert.Equal("Explizit übergeben", decision.DecidedByDisplayNameSnapshot);
    }
}
