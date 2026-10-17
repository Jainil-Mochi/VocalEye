namespace VocalEye.Application.Abstractions.Rag;

public sealed record RetrievedChunk(
    string DocumentId,
    string Text,
    double Score,
    IReadOnlyDictionary<string, string> Metadata);

public interface IRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, int topK, CancellationToken cancellationToken);
}
