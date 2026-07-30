using Enset.Domain.Buildings;
using Enset.Domain.Common;
using Enset.Domain.Documents;
using Enset.Domain.Energy;
using Enset.Domain.Projects;

namespace Enset.Domain.Associations;

public enum BuildingMeterRole { MainMeter, SubMeter, GenerationMeter, SumMeter, ControlMeter }
public enum BuildingDocumentRole { EnergyCertificate, Plan, Invoice, InspectionReport, Contract, Photo, Other }
public enum CustomerProjectRole { Client, Partner, Operator, Owner }

public sealed class BuildingMeterAssignment : BaseEntity
{
    public Guid BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public Guid MeterId { get; set; }
    public Meter Meter { get; set; } = null!;
    public BuildingMeterRole Role { get; set; }
    public bool IsPrimary { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public sealed class BuildingDocumentAssignment : BaseEntity
{
    public Guid BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public BuildingDocumentRole Role { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public sealed class CustomerProjectAssignment : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Enset.Domain.Customers.Customer Customer { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public CustomerProjectRole Role { get; set; }
    public bool IsPrimary { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public sealed class AssociationAuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperationId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string AssociationType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public Guid TargetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Before { get; set; }
    public string? After { get; set; }
    public string? Reason { get; set; }
}
