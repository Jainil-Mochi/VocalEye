namespace VocalEye.Application.Abstractions.Rag;

public interface IDocumentIngestor
{
    Task IngestAsync(string sourceId, Stream content, string contentType, CancellationToken cancellationToken);
}
