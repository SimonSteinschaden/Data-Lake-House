public class DuplicateCandidate<T>
{
    public T First { get; set; } = default!;
    public T Second { get; set; } = default!;

    public double SimilarityScore { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool RequiresUserDecision { get; set; }

    public string? ProposedResolution { get; set; }
}