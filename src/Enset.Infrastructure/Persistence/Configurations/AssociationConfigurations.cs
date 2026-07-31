using Enset.Domain.Associations;
using Enset.Domain.Energy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public sealed class BuildingMeterAssignmentConfiguration : IEntityTypeConfiguration<BuildingMeterAssignment>
{
    public void Configure(EntityTypeBuilder<BuildingMeterAssignment> b)
    {
        b.ToTable("BuildingMeterAssignments"); b.HasKey(x => x.Id);
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
        b.HasOne(x => x.Building).WithMany(x => x.MeterAssignments).HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Meter).WithMany(x => x.BuildingAssignments).HasForeignKey(x => x.MeterId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BuildingId, x.MeterId, x.ValidFrom }).IsUnique();
        b.HasIndex(x => new { x.MeterId, x.IsPrimary });
        b.HasIndex(x => new { x.ValidFrom, x.ValidTo });
    }
}
public sealed class BuildingDocumentAssignmentConfiguration : IEntityTypeConfiguration<BuildingDocumentAssignment>
{
    public void Configure(EntityTypeBuilder<BuildingDocumentAssignment> b)
    {
        b.ToTable("BuildingDocumentAssignments"); b.HasKey(x => x.Id);
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
        b.HasOne(x => x.Building).WithMany(x => x.DocumentAssignments).HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Document).WithMany(x => x.BuildingAssignments).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BuildingId, x.DocumentId, x.ValidFrom }).IsUnique();
    }
}
public sealed class CustomerProjectAssignmentConfiguration : IEntityTypeConfiguration<CustomerProjectAssignment>
{
    public void Configure(EntityTypeBuilder<CustomerProjectAssignment> b)
    {
        b.ToTable("CustomerProjectAssignments"); b.HasKey(x => x.Id);
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
        b.HasOne(x => x.Customer).WithMany(x => x.ProjectAssignments).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Project).WithMany(x => x.CustomerAssignments).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CustomerId, x.ProjectId, x.ValidFrom }).IsUnique();
        b.HasIndex(x => new { x.ProjectId, x.IsPrimary });
    }
}
public sealed class AssociationAuditEntryConfiguration : IEntityTypeConfiguration<AssociationAuditEntry>
{
    public void Configure(EntityTypeBuilder<AssociationAuditEntry> b)
    {
        b.ToTable("AssociationAuditHistory"); b.HasKey(x => x.Id);
        b.Property(x => x.AssociationType).HasMaxLength(64);
        b.Property(x => x.Action).HasMaxLength(32);
        b.Property(x => x.Before).HasMaxLength(2000); b.Property(x => x.After).HasMaxLength(2000);
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.HasIndex(x => x.OperationId);
        b.HasIndex(x => new { x.AssociationType, x.SourceId, x.TargetId, x.ChangedAtUtc });
    }
}
public sealed class EnergySystemBuildingAssignmentConfiguration : IEntityTypeConfiguration<EnergySystemBuildingAssignment>
{
    public void Configure(EntityTypeBuilder<EnergySystemBuildingAssignment> b)
    {
        b.ToTable("EnergySystemBuildingAssignments"); b.HasKey(x => x.Id);
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(40);
        b.HasOne(x => x.Building).WithMany(x => x.EnergySystemAssignments).HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EnergySystem).WithMany(x => x.BuildingAssignments).HasForeignKey(x => x.EnergySystemId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BuildingId, x.EnergySystemId, x.ValidFrom }).IsUnique();
        b.HasIndex(x => new { x.EnergySystemId, x.IsPrimary });
    }
}
