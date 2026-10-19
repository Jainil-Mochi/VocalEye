using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Memory;

namespace VocalEye.Agents.Orchestration;

/// <summary>Everything prepared for one conversation turn, shared by the streaming and non-streaming paths.</summary>
internal sealed class TurnContext
{
    public TurnContext(
        ConversationState state,
        ModelSelection selection,
        AIAgent agent,
        List<ChatMessage> input,
        List<ChatMessage> history,
        AgentRequest request,
        int toolCount)
    {
        State = state;
        Selection = selection;
        Agent = agent;
        Input = input;
        History = history;
        Request = request;
        ToolCount = toolCount;
    }

    public ConversationState State { get; }

    public ModelSelection Selection { get; }

    public AIAgent Agent { get; }

    /// <summary>The new messages this turn adds (user text and/or approval answers).</summary>
    public List<ChatMessage> Input { get; }

    /// <summary>Stored history plus <see cref="Input"/>: what the agent receives.</summary>
    public List<ChatMessage> History { get; }

    public AgentRequest Request { get; }

    public int ToolCount { get; }
}
