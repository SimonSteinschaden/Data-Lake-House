using System.Text.Json;
using System.Data;
using Enset.Application.Authorization;
using Enset.Application.Quality;
using Enset.Application.ReadModel;
using Enset.Domain.Common;
using Enset.Domain.Quality;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Quality;

public sealed class EfQualityPersistenceService(
    EnsetDbContext db, ICurrentUserContext user) : IQualityPersistenceService
{
    public Task<BuildingInventoryDeclaration?> GetCurrentDeclaration(Guid id, CancellationToken ct) =>
        db.BuildingInventoryDeclarations.AsNoTracking().SingleOrDefaultAsync(x => x.BuildingId == id && x.IsCurrent, ct);
    public Task<PagedResult<BuildingInventoryDeclaration>> GetDeclarationHistory(Guid id, int page, int pageSize, CancellationToken ct) =>
        Page(db.BuildingInventoryDeclarations.AsNoTracking().Where(x => x.BuildingId == id)
            .OrderByDescending(x => x.VersionNumber), page, pageSize, ct);

    public async Task<BuildingInventoryDeclaration> DeclareInventory(Guid id, InventoryDeclarationRequest request, CancellationToken ct)
    {
        EnsureEmployee(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var current = await db.BuildingInventoryDeclarations.SingleOrDefaultAsync(x => x.BuildingId == id && x.IsCurrent, ct);
        var version = await db.BuildingInventoryDeclarations.Where(x => x.BuildingId == id).MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0;
        if (current is not null) { current.IsCurrent = false; current.InvalidatedAtUtc = DateTime.UtcNow; current.InvalidatedByUserId = user.UserId; current.InvalidationReason = "Durch eine neue Inventarerklärung ersetzt."; }
        var displayName = await ResolveDisplayName(request.DisplayName, ct);
        var value = new BuildingInventoryDeclaration { BuildingId=id, VersionNumber=version+1, IsCurrent=true, MeterInventoryComplete=request.MeterInventoryComplete, EnergySystemInventoryComplete=request.EnergySystemInventoryComplete, NoRelevantEnergySystemsConfirmed=request.NoRelevantEnergySystemsConfirmed, ConfirmedByUserId=user.UserId!.Value, ConfirmedByDisplayNameSnapshot=displayName, ConfirmedAtUtc=DateTime.UtcNow, Comment=request.Comment, CreatedAtUtc=DateTime.UtcNow };
        db.BuildingInventoryDeclarations.Add(value); Audit("BuildingInventoryDeclaration", value.Id, "InventoryConfirmed", null, value, request.Comment);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return value;
    }
    public async Task InvalidateInventory(Guid id, string reason, CancellationToken ct)
    {
        EnsureEmployee(); if(string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Eine Begründung ist erforderlich.");
        var value=await db.BuildingInventoryDeclarations.SingleOrDefaultAsync(x=>x.BuildingId==id&&x.IsCurrent,ct); if(value is null)return;
        value.IsCurrent=false; value.InvalidatedAtUtc=DateTime.UtcNow; value.InvalidatedByUserId=user.UserId; value.InvalidationReason=reason; Audit("BuildingInventoryDeclaration",value.Id,"InventoryInvalidated",null,value,reason); await db.SaveChangesAsync(ct);
    }
    public async Task<MeterProfileAnalysis> StartAnalysis(Guid meterId, DateTime from, DateTime to, string analysisVersion, CancellationToken ct)
    {
        EnsureEmployee(); if(from>=to)throw new ArgumentException("Der Analysezeitraum ist ungültig."); await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var version=await db.MeterProfileAnalyses.Where(x=>x.MeterId==meterId).MaxAsync(x=>(int?)x.VersionNumber,ct)??0;
        var displayName=await ResolveDisplayName(null,ct);
        var value=new MeterProfileAnalysis{MeterId=meterId,VersionNumber=version+1,AnalysisStatus=ProfileAnalysisStatus.Running,AnalysisVersion=analysisVersion,PeriodFromUtc=from.ToUniversalTime(),PeriodToUtc=to.ToUniversalTime(),ExecutedAtUtc=DateTime.UtcNow,ExecutedByActorType=QualityActorType.EnsetEmployee,ExecutedByUserId=user.UserId,ExecutedByDisplayNameSnapshot=displayName,CreatedAtUtc=DateTime.UtcNow};
        db.MeterProfileAnalyses.Add(value); Audit("MeterProfileAnalysis",value.Id,"AnalysisStarted",null,value,null); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return value;
    }
    public async Task<MeterProfileAnalysis> CompleteAnalysis(Guid id, AnalysisCompletion c, CancellationToken ct)
    {
        EnsureEmployee(); await using var tx=await db.Database.BeginTransactionAsync(ct); var value=await db.MeterProfileAnalyses.SingleAsync(x=>x.Id==id,ct);
        if(value.AnalysisStatus!=ProfileAnalysisStatus.Running)throw new InvalidOperationException("Nur eine laufende Analyse darf abgeschlossen werden.");if(c.Status is not(ProfileAnalysisStatus.Completed or ProfileAnalysisStatus.RequiresReview or ProfileAnalysisStatus.Curated or ProfileAnalysisStatus.Confirmed))throw new ArgumentException("Ungültiger Abschlussstatus.");
        var current=await db.MeterProfileAnalyses.SingleOrDefaultAsync(x=>x.MeterId==value.MeterId&&x.IsCurrent,ct); if(current is not null){current.IsCurrent=false;current.SupersededAtUtc=DateTime.UtcNow;}
        value.ExpectedReadingCount=c.ExpectedReadingCount;value.ActualReadingCount=c.ActualReadingCount;value.CompletenessPercentage=c.CompletenessPercentage;value.GapCount=c.GapCount;value.AnomalyCount=c.AnomalyCount;value.BlockingIssueCount=c.BlockingIssueCount;value.WarningCount=c.WarningCount;value.DetectedIntervalSeconds=c.DetectedIntervalSeconds;value.DetectedUnit=c.DetectedUnit;value.Summary=c.Summary;value.AnalysisStatus=c.Status;value.IsCurrent=true;
        Audit("MeterProfileAnalysis",value.Id,"AnalysisCompleted",null,value,c.Summary);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return value;
    }
    public async Task<MeterProfileAnalysis> FailAnalysis(Guid id,bool cancelled,string summary,CancellationToken ct){EnsureEmployee();var v=await db.MeterProfileAnalyses.SingleAsync(x=>x.Id==id,ct);if(v.AnalysisStatus!=ProfileAnalysisStatus.Running)throw new InvalidOperationException("Nur eine laufende Analyse darf beendet werden.");v.AnalysisStatus=cancelled?ProfileAnalysisStatus.Cancelled:ProfileAnalysisStatus.Failed;v.Summary=summary;v.IsCurrent=false;Audit("MeterProfileAnalysis",v.Id,cancelled?"AnalysisCancelled":"AnalysisFailed",null,v,summary);await db.SaveChangesAsync(ct);return v;}
    public Task<PagedResult<MeterProfileAnalysis>> GetAnalysisHistory(Guid id,int page,int pageSize,CancellationToken ct)=>
        Page(db.MeterProfileAnalyses.AsNoTracking().Where(x=>x.MeterId==id).OrderByDescending(x=>x.VersionNumber),page,pageSize,ct);
    public Task<MeterProfileAnalysis?> GetAnalysis(Guid meterId,Guid analysisId,CancellationToken ct)=>db.MeterProfileAnalyses.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==analysisId&&x.MeterId==meterId,ct);
    public async Task AddIssues(Guid id,IReadOnlyCollection<MeterProfileIssue> issues,CancellationToken ct){EnsureEmployee();var meterId=await db.MeterProfileAnalyses.Where(x=>x.Id==id).Select(x=>x.MeterId).SingleAsync(ct);foreach(var issue in issues){issue.MeterProfileAnalysisId=id;issue.MeterId=meterId;issue.CreatedAtUtc=DateTime.UtcNow;db.MeterProfileIssues.Add(issue);Audit("MeterProfileIssue",issue.Id,"IssueCreated",null,issue,issue.Message);}await db.SaveChangesAsync(ct);}
    public Task<PagedResult<MeterProfileIssue>> GetIssues(Guid id,int page,int pageSize,CancellationToken ct)=>
        Page(db.MeterProfileIssues.AsNoTracking().Where(x=>x.MeterProfileAnalysisId==id).OrderBy(x=>x.CreatedAtUtc),page,pageSize,ct);
    public async Task<MeterProfileCurationDecision> Decide(Guid issueId,CurationDecisionRequest r,CancellationToken ct)
    {
        EnsureEmployee();if(string.IsNullOrWhiteSpace(r.Reason))throw new ArgumentException("Eine Begründung ist erforderlich.");if(r.DecisionType==ProfileDecisionType.GenerateEstimatedValue&&(string.IsNullOrWhiteSpace(r.GeneratedValueMethod)||r.ConfidencePercent is null))throw new ArgumentException("Ersatzwerte benötigen Methode und Confidence.");
        var issue=await db.MeterProfileIssues.SingleAsync(x=>x.Id==issueId,ct);if(r.SupersedesDecisionId.HasValue&&!await db.MeterProfileCurationDecisions.AnyAsync(x=>x.Id==r.SupersedesDecisionId&&x.MeterProfileIssueId==issueId,ct))throw new ArgumentException("Die ersetzte Entscheidung gehört nicht zum Issue.");
        var displayName=await ResolveDisplayName(r.DisplayName,ct);
        var v=new MeterProfileCurationDecision{MeterProfileIssueId=issueId,MeterProfileAnalysisId=issue.MeterProfileAnalysisId,MeterId=issue.MeterId,DecisionType=r.DecisionType,PreviousValue=r.PreviousValue,NewValue=r.NewValue,GeneratedValueMethod=r.GeneratedValueMethod,ConfidencePercent=r.ConfidencePercent,Reason=r.Reason,Comment=r.Comment,DecidedByUserId=user.UserId!.Value,DecidedByDisplayNameSnapshot=displayName,DecidedAtUtc=DateTime.UtcNow,IsFinal=r.IsFinal,SupersedesDecisionId=r.SupersedesDecisionId,CreatedAtUtc=DateTime.UtcNow};db.MeterProfileCurationDecisions.Add(v);issue.ResolutionStatus=r.IsFinal?ProfileIssueResolutionStatus.Resolved:ProfileIssueResolutionStatus.UnderReview;issue.ResolvedAtUtc=r.IsFinal?DateTime.UtcNow:null;Audit("MeterProfileCurationDecision",v.Id,r.SupersedesDecisionId.HasValue?"DecisionSuperseded":"DecisionCreated",null,v,r.Reason);await db.SaveChangesAsync(ct);return v;
    }
    public Task<PagedResult<MeterProfileCurationDecision>> GetDecisionHistory(Guid issueId,int page,int pageSize,CancellationToken ct)=>
        Page(db.MeterProfileCurationDecisions.AsNoTracking().Where(x=>x.MeterProfileIssueId==issueId).OrderByDescending(x=>x.DecidedAtUtc),page,pageSize,ct);
    private static async Task<PagedResult<T>> Page<T>(IQueryable<T> query,int page,int pageSize,CancellationToken ct)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,200);
        var total=await query.CountAsync(ct);
        var items=await query.Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
        return new(items,page,pageSize,total);
    }
    private void EnsureEmployee(){if(!user.IsAuthenticated||!user.IsEnsetEmployee||!user.UserId.HasValue)throw new UnauthorizedAccessException("Nur ENSET-Mitarbeitende dürfen diese Qualitätsentscheidung durchführen.");}
    private async Task<string?> ResolveDisplayName(string? provided,CancellationToken ct){if(!string.IsNullOrWhiteSpace(provided))return provided;if(!user.UserId.HasValue)return null;return await db.ApplicationUsers.AsNoTracking().Where(x=>x.Id==user.UserId).Select(x=>x.DisplayName).SingleOrDefaultAsync(ct);}
    private void Audit(string type,Guid id,string action,object? oldValue,object newValue,string? reason)=>db.EntityAuditEntries.Add(new EntityAuditEntry{OperationId=Guid.NewGuid(),EntityType=type,EntityId=id,ChangedAtUtc=DateTime.UtcNow,ChangedByUserId=user.UserId??Guid.Empty,ActorType="EnsetEmployee",Action=action,ChangeType=EntityChangeType.Updated,FieldName=action,OldValue=oldValue is null?null:JsonSerializer.Serialize(oldValue),NewValue=JsonSerializer.Serialize(newValue),Source=LastModifiedSource.User,Reason=reason,RelatedAnalysisId=newValue is MeterProfileAnalysis a?a.Id:newValue is MeterProfileIssue i?i.MeterProfileAnalysisId:null,RelatedIssueId=newValue is MeterProfileIssue issue?issue.Id:newValue is MeterProfileCurationDecision d?d.MeterProfileIssueId:null});
}
