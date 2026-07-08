using Enset.Infrastructure.Imports.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Imports.Persistence.Configurations;

public sealed class ImportIssueEntityConfiguration
    : IEntityTypeConfiguration<ImportIssueEntity>
{
    public void Configure(EntityTypeBuilder<ImportIssueEntity> builder)
    {
        builder.ToTable("ImportIssues");

        builder.HasKey(issue => issue.IssueId);

        builder.HasIndex(issue => issue.ImportId);

        builder.Property(issue => issue.Type)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(issue => issue.Severity)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(issue => issue.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(issue => issue.FieldName)
            .HasMaxLength(256);

        builder.Property(issue => issue.FirstValue)
            .HasMaxLength(4000);

        builder.Property(issue => issue.SecondValue)
            .HasMaxLength(4000);

        builder.Property(issue => issue.ResolutionAction)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(issue => issue.CustomResolvedValue)
            .HasMaxLength(4000);
    }
}
