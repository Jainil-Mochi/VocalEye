using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using VocalEye.Agents;
using VocalEye.Agents.Orchestration;
using Xunit;

namespace VocalEye.UnitTests;

public class ToolRoundLimitChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_WithinTheLimit_PassesToolsAndMessagesUnchanged()
    {
        RecordingClient inner = new RecordingClient();
        ToolRoundLimitChatClient client = new ToolRoundLimitChatClient(inner, 2, NullLogger.Instance);
        ChatOptions options = new ChatOptions { Tools = new List<AITool> { AIFunctionFactory.Create(() => "x", "t") } };

        await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") }, options);
        await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") }, options);

        inner.Options.Should().OnlyContain(item => item != null && item.Tools != null && item.Tools.Count == 1);
        inner.Messages.Should().OnlyContain(list => list.Count == 1);
    }

    [Fact]
    public async Task GetResponseAsync_PastTheLimit_RemovesToolsAndAddsTheNotice()
    {
        RecordingClient inner = new RecordingClient();
        ToolRoundLimitChatClient client = new ToolRoundLimitChatClient(inner, 1, NullLogger.Instance);
        ChatOptions options = new ChatOptions { Tools = new List<AITool> { AIFunctionFactory.Create(() => "x", "t") } };
        List<ChatMessage> original = new List<ChatMessage> { new ChatMessage(ChatRole.User, "hi") };

        await client.GetResponseAsync(original, options);
        await client.GetResponseAsync(original, options);

        inner.Options[1]!.Tools.Should().BeNull();
        inner.Messages[1].Last().Text.Should().Be(ToolRoundLimitChatClient.NoticeText);
        original.Should().ContainSingle("the caller's own list must not be changed");
        options.Tools.Should().NotBeNull("the caller's own options must not be changed");
    }

    [Fact]
    public async Task GetStreamingResponseAsync_PastTheLimit_RemovesToolsToo()
    {
        RecordingClient inner = new RecordingClient();
        ToolRoundLimitChatClient client = new ToolRoundLimitChatClient(inner, 1, NullLogger.Instance);
        ChatOptions options = new ChatOptions { Tools = new List<AITool> { AIFunctionFactory.Create(() => "x", "t") } };

        await foreach (ChatResponseUpdate unused in client.GetStreamingResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") }, options))
        {
        }

        await foreach (ChatResponseUpdate unused in client.GetStreamingResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") }, options))
        {
        }

        inner.Options[0]!.Tools.Should().NotBeNull();
        inner.Options[1]!.Tools.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_OutsideTheRange_ThrowsAtStartup(int rounds)
    {
        AgentOptions options = new AgentOptions { MaxToolRounds = rounds };

        Action act = options.Validate;

        act.Should().Throw<InvalidOperationException>().WithMessage("*Agents:MaxToolRounds*");
    }

    [Fact]
    public void Validate_DefaultValue_IsAccepted()
    {
        Action act = new AgentOptions().Validate;

        act.Should().NotThrow();
    }

    private sealed class RecordingClient : IChatClient
    {
        public List<ChatOptions?> Options { get; } = new List<ChatOptions?>();

        public List<List<ChatMessage>> Messages { get; } = new List<List<ChatMessage>>();

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Record(messages, options);

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Record(messages, options);
            await Task.Yield();

            yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return null;
        }

        public void Dispose()
        {
        }

        private void Record(IEnumerable<ChatMessage> messages, ChatOptions? options)
        {
            Options.Add(options);
            Messages.Add(messages.ToList());
        }
    }
}
