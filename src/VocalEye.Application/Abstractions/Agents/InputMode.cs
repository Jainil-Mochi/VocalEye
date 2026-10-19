namespace VocalEye.Application.Abstractions.Agents;

/// <summary>How the text was produced. The agent treats both identically; this is metadata for logging, UX and evals.</summary>
public enum InputMode
{
    Text,
    Voice,
}
