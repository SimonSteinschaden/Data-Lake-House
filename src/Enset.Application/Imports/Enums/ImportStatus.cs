namespace Enset.Application.Imports.Enums;

public enum ImportStatus
{
    Pending = 0,
    Analyzing = 1,
    AwaitingResolution = 2,
    ReadyToCommit = 3,
    Committing = 4,
    Committed = 5,
    Failed = 6
}
