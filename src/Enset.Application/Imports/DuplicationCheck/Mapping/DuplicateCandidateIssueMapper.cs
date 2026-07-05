using Enset.Application.Imports.DuplicationCheck.Models;
using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.DuplicationCheck.Mapping;

public static class DuplicateCandidateIssueMapper
{
    public static ImportIssue ToImportIssue<T>(
        DuplicateCandidate<T> candidate,
        ImportIssueType issueType,
        string firstValue,
        string secondValue,
        string? fieldName = null)
    {
        return new ImportIssue
        {
            Type = issueType,
            Severity = ImportIssueSeverity.Warning,
            Message = candidate.Reason,
            SimilarityScore = candidate.SimilarityScore,
            RequiresUserDecision = candidate.RequiresUserDecision,
            FieldName = fieldName,
            FirstValue = firstValue,
            SecondValue = secondValue,
            ResolutionAction = ImportResolutionAction.None,
            CustomResolvedValue = null,
            IsResolved = false
        };
    }
}