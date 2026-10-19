namespace VocalEye.Application.Abstractions.Rag;

public interface IRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, int topK, CancellationToken cancellationToken);
}
