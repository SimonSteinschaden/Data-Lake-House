namespace Enset.Application.Imports.Issues;

public enum ImportResolutionAction
{
    None = 0,
    KeepFirst = 1,
    KeepSecond = 2,
    UseCustomValue = 3,
    KeepSeparate = 4,
    Merge = 5,
    SkipRow = 6,
    EnterValue = 7,
    IgnoreMissingValue = 8,
    SetZero = 9,
    ParseDeAt = 10,
    ParseInvariant = 11,
    IgnoreInvalidValue = 12,
    ParseKnownDateFormat = 13,
    MapReference = 14,
    CreateNew = 15,
    ConfirmGeneratedHeader = 16,
    RenameColumn = 17,
    MapField = 18,
    IgnoreColumn = 19,
    SelectTimestampColumn = 20,
    SelectValueColumn = 21,
    SelectQualityColumn = 22,
    GenerateTimestamps = 23,
    AssignMeter = 24,
    CreateMeter = 25
}
