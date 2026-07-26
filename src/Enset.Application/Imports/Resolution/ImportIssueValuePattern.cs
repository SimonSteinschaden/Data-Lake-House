namespace Enset.Application.Imports.Resolution;

public enum ImportIssueValuePattern
{
    None = 0,
    ExactValue = 1,
    GermanDecimal = 2,
    MissingAnnualTotalWithMonthlyValues = 3,
    EmptyGeneratedHeader = 4,
    DuplicateExternalId = 5
}
