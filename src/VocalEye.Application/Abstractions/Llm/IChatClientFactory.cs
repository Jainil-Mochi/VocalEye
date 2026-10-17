using Microsoft.Extensions.AI;

namespace VocalEye.Application.Abstractions.Llm;

/// <summary>
/// Returns a ready-to-use chat client (with logging/telemetry/resilience already applied) for any catalog model.
/// This is the ONLY place app code gets an LLM from; it is what makes models switchable at runtime.
/// </summary>
public interface IChatClientFactory
{
    /// <exception cref="ArgumentException">The selection is not in the catalog.</exception>
    IChatClient Create(ModelSelection selection);
}
