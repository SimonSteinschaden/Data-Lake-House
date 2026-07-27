namespace Enset.Domain.GoldProfiles;
public enum GoldProfileReleaseStatus { Draft, Released, Superseded, Revoked }
public sealed class GoldProfileVersion {
 public Guid Id{get;set;}=Guid.NewGuid();public string EntityType{get;set;}="";public Guid EntityId{get;set;}public int VersionNumber{get;set;}public string ProfileType{get;set;}="";
 public DateTime ValidFromUtc{get;set;}public DateTime? ValidToUtc{get;set;}public DateTime CreatedAtUtc{get;set;}public Guid CreatedByUserId{get;set;}public long SourceCurationRevision{get;set;}
 public string SnapshotHash{get;set;}="";public string SnapshotJson{get;set;}="";public bool IsCurrent{get;set;}public GoldProfileReleaseStatus ReleaseStatus{get;set;}public string? ReleaseReason{get;set;}public uint RowVersion{get;set;}
 public ICollection<GoldProfileEvent> Events{get;set;}=new List<GoldProfileEvent>();
}
public sealed class GoldProfileEvent {
 public Guid Id{get;set;}=Guid.NewGuid();public Guid GoldProfileVersionId{get;set;}public GoldProfileVersion Version{get;set;}=null!;public DateTime OccurredAtUtc{get;set;}public Guid UserId{get;set;}
 public string EventType{get;set;}="";public GoldProfileReleaseStatus? PreviousStatus{get;set;}public GoldProfileReleaseStatus NewStatus{get;set;}public string? Reason{get;set;}public string SnapshotHash{get;set;}="";
}
