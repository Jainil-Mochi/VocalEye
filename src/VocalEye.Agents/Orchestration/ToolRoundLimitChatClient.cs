using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace VocalEye.Agents.Orchestration;

/// <summary>
/// Sits under the agent's tool loop and counts the model calls of ONE turn (create a new instance per turn). Once the
/// limit is passed it stops offering tools and asks the model to answer with what it has, so a model that keeps calling
/// tools ends with a normal reply instead of an error.
/// </summary>
public sealed class ToolRoundLimitChatClient : DelegatingChatClient
{
    public const string NoticeText =
        "Notice: the tool limit for this request was reached. Do not call any tool. " +
        "Answer the user now with what you already have, and say clearly what could not be finished.";

    private readonly int _maxRounds;
    private readonly ILogger _logger;
    private int _calls;

    public ToolRoundLimitChatClient(IChatClient innerClient, int maxRounds, ILogger logger)
        : base(innerClient)
    {
        _maxRounds = maxRounds;
        _logger = logger;
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        (IEnumerable<ChatMessage> allowedMessages, ChatOptions? allowedOptions) = Apply(messages, options);

        return base.GetResponseAsync(allowedMessages, allowedOptions, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        (IEnumerable<ChatMessage> allowedMessages, ChatOptions? allowedOptions) = Apply(messages, options);

        return base.GetStreamingResponseAsync(allowedMessages, allowedOptions, cancellationToken);
    }

    private (IEnumerable<ChatMessage> Messages, ChatOptions? Options) Apply(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        int call = Interlocked.Increment(ref _calls);
        if (call <= _maxRounds)
        {
            return (messages, options);
        }

        if (call == _maxRounds + 1)
        {
            _logger.LogWarning("The model used all {Max} tool rounds in one turn; asking it to answer without tools.", _maxRounds);
        }

        ChatOptions withoutTools = options?.Clone() ?? new ChatOptions();
        withoutTools.Tools = null;
        withoutTools.ToolMode = null;

        // A user message is accepted by every provider after tool results; a late system message is not.
        List<ChatMessage> withNotice = new List<ChatMessage>(messages) { new ChatMessage(ChatRole.User, NoticeText) };

        return (withNotice, withoutTools);
    }
}
