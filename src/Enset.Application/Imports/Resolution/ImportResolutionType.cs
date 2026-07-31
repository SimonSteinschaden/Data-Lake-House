namespace Enset.Application.Imports.Resolution;

public enum ImportResolutionType
{
    ExistingAction = 0,
    ParseWithCulture = 1,
    SumMonthlyValues = 2,
    SkipRow = 3,
    ConfirmGeneratedHeader = 4,
    RenameColumn = 5,
    KeepAsAdditionalColumn = 6
}
