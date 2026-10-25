using VocalEye.Tools.Documents;

namespace VocalEye.Tools.FileGeneration;

/// <summary>Writes parsed document blocks to one file format.</summary>
public interface IDocumentWriter
{
    /// <summary>File extension without the dot, for example "pdf".</summary>
    string Extension { get; }

    void Write(IReadOnlyList<MarkdownBlock> blocks, string path);
}
