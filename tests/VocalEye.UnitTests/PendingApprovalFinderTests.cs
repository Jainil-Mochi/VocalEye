using FluentAssertions;
using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Conversations;
using VocalEye.Domain.Conversations;
using Xunit;

namespace VocalEye.UnitTests;

public class PendingApprovalFinderTests
{
    [Fact]
    public void Find_LastMessageIsAnApprovalRequest_ReturnsIt()
    {
        List<StoredMessageDto> messages = new List<StoredMessageDto>
        {
            Stored(0, MessageRole.User, new ChatMessage(ChatRole.User, "make a pdf")),
            Stored(1, MessageRole.Assistant, RequestMessage("call_1", "file_generator")),
        };

        IReadOnlyList<PendingApproval> pending = PendingApprovalFinder.Find(messages);

        PendingApproval approval = pending.Should().ContainSingle().Subject;
        approval.ApprovalId.Should().Be("call_1");
        approval.ToolName.Should().Be("file_generator");
        approval.ArgumentsJson.Should().Contain("ai-basics");
    }

    [Fact]
    public void Find_UserAnsweredAndTheTurnContinued_ReturnsNothing()
    {
        List<StoredMessageDto> messages = new List<StoredMessageDto>
        {
            Stored(0, MessageRole.User, new ChatMessage(ChatRole.User, "make a pdf")),
            Stored(1, MessageRole.Assistant, RequestMessage("call_1", "file_generator")),
            Stored(2, MessageRole.User, new ChatMessage(ChatRole.User, "thanks")),
            Stored(3, MessageRole.Assistant, new ChatMessage(ChatRole.Assistant, "You are welcome.")),
        };

        PendingApprovalFinder.Find(messages).Should().BeEmpty();
    }

    [Fact]
    public void Find_PlainAssistantAnswerOrNoMessages_ReturnsNothing()
    {
        PendingApprovalFinder.Find(new List<StoredMessageDto>()).Should().BeEmpty();
        PendingApprovalFinder.Find(new List<StoredMessageDto>
        {
            Stored(0, MessageRole.Assistant, new ChatMessage(ChatRole.Assistant, "hello")),
        }).Should().BeEmpty();
    }

    private static ChatMessage RequestMessage(string callId, string toolName)
    {
        FunctionCallContent call = new FunctionCallContent(callId, toolName, new Dictionary<string, object?> { ["filename"] = "ai-basics" });

        return new ChatMessage(ChatRole.Assistant, new List<AIContent> { new ToolApprovalRequestContent("request-" + callId, call) });
    }

    private static StoredMessageDto Stored(int sequence, MessageRole role, ChatMessage message)
    {
        return new StoredMessageDto { Sequence = sequence, Role = role, ContentJson = ChatMessageMapper.ToJson(message) };
    }
}
