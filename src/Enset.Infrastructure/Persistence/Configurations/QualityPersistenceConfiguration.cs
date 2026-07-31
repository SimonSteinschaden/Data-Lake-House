using Enset.Domain.Buildings;
using Enset.Domain.Energy;
using Enset.Domain.Quality;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public sealed class BuildingInventoryDeclarationConfiguration : IEntityTypeConfiguration<BuildingInventoryDeclaration>
{
    public void Configure(EntityTypeBuilder<BuildingInventoryDeclaration> b)
    {
        b.ToTable("BuildingInventoryDeclarations", t => t.HasCheckConstraint("CK_BuildingInventoryDeclarations_VersionNumber", "\"VersionNumber\" > 0"));
        b.HasKey(x => x.Id); b.Property(x => x.RowVersion).IsRowVersion().HasColumnName("xmin");
        b.Property(x => x.ConfirmedByDisplayNameSnapshot).HasMaxLength(256); b.Property(x => x.Comment).HasMaxLength(2000);
        b.Property(x => x.InvalidationReason).HasMaxLength(2000);
        b.HasOne<Building>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.BuildingId); b.HasIndex(x => new { x.BuildingId, x.VersionNumber }).IsUnique();
        b.HasIndex(x => x.BuildingId).IsUnique().HasFilter("\"IsCurrent\" = TRUE").HasDatabaseName("UX_BuildingInventoryDeclarations_Current");
    }
}

public sealed class MeterProfileAnalysisConfiguration : IEntityTypeConfiguration<MeterProfileAnalysis>
{
    public void Configure(EntityTypeBuilder<MeterProfileAnalysis> b)
    {
        b.ToTable("MeterProfileAnalyses", t => { t.HasCheckConstraint("CK_MeterProfileAnalyses_VersionNumber", "\"VersionNumber\" > 0"); t.HasCheckConstraint("CK_MeterProfileAnalyses_Completeness", "\"CompletenessPercentage\" >= 0 AND \"CompletenessPercentage\" <= 100"); t.HasCheckConstraint("CK_MeterProfileAnalyses_Period", "\"PeriodFromUtc\" < \"PeriodToUtc\""); });
        b.HasKey(x => x.Id); b.Property(x => x.AnalysisStatus).HasConversion<string>().HasMaxLength(32); b.Property(x => x.ExecutedByActorType).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.AnalysisVersion).HasMaxLength(64).IsRequired(); b.Property(x => x.DetectedUnit).HasMaxLength(64); b.Property(x => x.ExecutedByDisplayNameSnapshot).HasMaxLength(256); b.Property(x => x.Summary).HasMaxLength(4000); b.Property(x => x.RowVersion).IsRowVersion().HasColumnName("xmin");
        b.HasOne<Meter>().WithMany().HasForeignKey(x => x.MeterId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.MeterId); b.HasIndex(x => new { x.MeterId, x.VersionNumber }).IsUnique(); b.HasIndex(x => new { x.PeriodFromUtc, x.PeriodToUtc }); b.HasIndex(x => x.AnalysisStatus);
        b.HasIndex(x => x.MeterId).IsUnique().HasFilter("\"IsCurrent\" = TRUE").HasDatabaseName("UX_MeterProfileAnalyses_Current");
    }
}

public sealed class MeterProfileIssueConfiguration : IEntityTypeConfiguration<MeterProfileIssue>
{
    public void Configure(EntityTypeBuilder<MeterProfileIssue> b)
    {
        b.ToTable("MeterProfileIssues"); b.HasKey(x => x.Id); b.Property(x => x.Code).HasMaxLength(128).IsRequired(); b.Property(x => x.Category).HasConversion<string>().HasMaxLength(32); b.Property(x => x.Severity).HasConversion<string>().HasMaxLength(16); b.Property(x => x.ResolutionStatus).HasConversion<string>().HasMaxLength(32); b.Property(x => x.OriginalValue).HasMaxLength(512); b.Property(x => x.ExpectedValue).HasMaxLength(512); b.Property(x => x.Message).HasMaxLength(2000).IsRequired(); b.Property(x => x.TechnicalDetails).HasMaxLength(8000); b.Property(x => x.RowVersion).IsRowVersion().HasColumnName("xmin");
        b.HasOne<MeterProfileAnalysis>().WithMany().HasForeignKey(x => x.MeterProfileAnalysisId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Meter>().WithMany().HasForeignKey(x => x.MeterId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.MeterProfileAnalysisId); b.HasIndex(x => x.MeterId); b.HasIndex(x => x.ResolutionStatus); b.HasIndex(x => x.Severity); b.HasIndex(x => x.IsBlocking); b.HasIndex(x => new { x.MeterProfileAnalysisId, x.ResolutionStatus });
    }
}

public sealed class MeterProfileCurationDecisionConfiguration : IEntityTypeConfiguration<MeterProfileCurationDecision>
{
    public void Configure(EntityTypeBuilder<MeterProfileCurationDecision> b)
    {
        b.ToTable("MeterProfileCurationDecisions", t => t.HasCheckConstraint("CK_MeterProfileCurationDecisions_Confidence", "\"ConfidencePercent\" IS NULL OR (\"ConfidencePercent\" >= 0 AND \"ConfidencePercent\" <= 100)"));
        b.HasKey(x => x.Id); b.Property(x => x.DecisionType).HasConversion<string>().HasMaxLength(32); b.Property(x => x.PreviousValue).HasMaxLength(512); b.Property(x => x.NewValue).HasMaxLength(512); b.Property(x => x.GeneratedValueMethod).HasMaxLength(128); b.Property(x => x.Reason).HasMaxLength(2000).IsRequired(); b.Property(x => x.Comment).HasMaxLength(2000); b.Property(x => x.DecidedByDisplayNameSnapshot).HasMaxLength(256); b.Property(x => x.RowVersion).IsRowVersion().HasColumnName("xmin");
        b.HasOne<MeterProfileIssue>().WithMany().HasForeignKey(x => x.MeterProfileIssueId).OnDelete(DeleteBehavior.Restrict); b.HasOne<MeterProfileAnalysis>().WithMany().HasForeignKey(x => x.MeterProfileAnalysisId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Meter>().WithMany().HasForeignKey(x => x.MeterId).OnDelete(DeleteBehavior.Restrict); b.HasOne<MeterProfileCurationDecision>().WithMany().HasForeignKey(x => x.SupersedesDecisionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.MeterProfileIssueId); b.HasIndex(x => x.MeterProfileAnalysisId); b.HasIndex(x => x.MeterId); b.HasIndex(x => x.DecidedAtUtc); b.HasIndex(x => x.DecidedByUserId); b.HasIndex(x => x.SupersedesDecisionId);
    }
}
