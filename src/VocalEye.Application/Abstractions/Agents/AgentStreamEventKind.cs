namespace VocalEye.Application.Abstractions.Agents;

public enum AgentStreamEventKind
{
    /// <summary>A piece of the assistant's answer, as the model produces it.</summary>
    TextDelta,

    /// <summary>The turn is finished. Carries the full reply, tool calls and any pending approvals.</summary>
    Completed
}
