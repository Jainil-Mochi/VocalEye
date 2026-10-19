using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Llm;

namespace VocalEye.Application.Abstractions.Memory;

/// <summary>
/// Provider-neutral conversation history. We own it (not the model provider, not the agent session),
/// which is what lets the user switch model between turns.
/// </summary>
public sealed class ConversationState
{
    public required Guid Id { get; init; }

    public List<ChatMessage> Messages { get; } = new List<ChatMessage>();

    /// <summary>Model used on the last turn. Used when a request does not pick one.</summary>
    public ModelSelection? Model { get; set; }
}
