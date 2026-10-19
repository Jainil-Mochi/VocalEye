using Microsoft.Extensions.AI;

namespace VocalEye.Application.Abstractions.Llm;

/// <summary>
/// Returns a ready-to-use chat client (telemetry and error handling applied) for any catalog model.
/// The only place application code gets an LLM from; it is what makes models switchable at runtime.
/// </summary>
public interface IChatClientFactory
{
    /// <exception cref="ArgumentException">The selection is not in the catalog.</exception>
    IChatClient Create(ModelSelection selection);
}
