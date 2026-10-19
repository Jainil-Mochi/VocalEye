namespace VocalEye.Application.Abstractions.Llm;

/// <summary>Which model to use for a request. Chosen by the user at runtime; falls back to the configured default.</summary>
public sealed record ModelSelection(string Provider, string Model);
