using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace VocalEye.UnitTests.Fakes;

/// <summary>A fake LLM: the test script decides what each model call returns (1-based call number).</summary>
internal sealed class ScriptedChatClient : IChatClient
{
    private readonly Func<int, IReadOnlyList<ChatMessage>, ChatResponse> _script;

    public ScriptedChatClient(Func<int, IReadOnlyList<ChatMessage>, ChatResponse> script)
    {
        _script = script;
    }

    public int Calls { get; private set; }

    public static ChatResponse Text(string text)
    {
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
    }

    public static ChatResponse CallTool(string callId, string toolName)
    {
        FunctionCallContent call = new FunctionCallContent(callId, toolName, new Dictionary<string, object?>());

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, new List<AIContent> { call }));
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Calls++;

        return Task.FromResult(_script(Calls, messages.ToList()));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Calls++;
        ChatResponse response = _script(Calls, messages.ToList());

        foreach (ChatResponseUpdate update in response.ToChatResponseUpdates())
        {
            await Task.Yield();
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public void Dispose()
    {
    }
}
