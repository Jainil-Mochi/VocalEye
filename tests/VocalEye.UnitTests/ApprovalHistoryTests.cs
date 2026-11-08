using FluentAssertions;
using Microsoft.Extensions.AI;
using VocalEye.Agents.Orchestration;
using VocalEye.Application.Abstractions.Agents;
using Xunit;

namespace VocalEye.UnitTests;

public class ApprovalHistoryTests
{
    private static ToolApprovalRequestContent Request(string callId)
    {
        return new ToolApprovalRequestContent(callId, new FunctionCallContent(callId, "file_generator", new Dictionary<string, object?>()));
    }

    private static List<ChatMessage> ResolvedThenNew(string resolvedId, string? liveId)
    {
        ToolApprovalRequestContent resolved = Request(resolvedId);
        List<ChatMessage> messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "make a file"),
            new ChatMessage(ChatRole.Assistant, new List<AIContent> { resolved }),
            new ChatMessage(ChatRole.User, new List<AIContent> { resolved.CreateResponse(true) }),
            new ChatMessage(ChatRole.Assistant, new List<AIContent> { new FunctionCallContent(resolvedId, "file_generator") }),
            new ChatMessage(ChatRole.Tool, new List<AIContent> { new FunctionResultContent(resolvedId, "done") }),
            new ChatMessage(ChatRole.Assistant, "created"),
            new ChatMessage(ChatRole.User, "another one"),
        };

        if (liveId is not null)
        {
            messages.Add(new ChatMessage(ChatRole.Assistant, new List<AIContent> { Request(liveId) }));
        }

        return messages;
    }

    [Fact]
    public void RemoveResolved_DropsAnsweredRequestAndResponse()
    {
        List<ChatMessage> cleaned = ApprovalHistory.RemoveResolved(ResolvedThenNew("fc_1", null));

        cleaned.SelectMany(message => message.Contents)
            .Should().NotContain(content => content is ToolApprovalRequestContent || content is ToolApprovalResponseContent);
        cleaned.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Should().HaveCount(1);
    }

    [Fact]
    public void RemoveResolved_KeepsTheRequestStillWaiting()
    {
        List<ChatMessage> cleaned = ApprovalHistory.RemoveResolved(ResolvedThenNew("fc_1", "fc_2"));

        cleaned.SelectMany(message => message.Contents).OfType<ToolApprovalRequestContent>().Should().ContainSingle();
    }

    [Fact]
    public void RemoveResolved_DoesNotChangeStoredMessages()
    {
        List<ChatMessage> stored = ResolvedThenNew("fc_1", null);
        int count = stored.Count;

        ApprovalHistory.RemoveResolved(stored);

        stored.Should().HaveCount(count);
        stored.SelectMany(message => message.Contents).OfType<ToolApprovalRequestContent>().Should().ContainSingle();
    }

    [Fact]
    public void BuildComplete_DeniesRequestsTheClientDidNotAnswer()
    {
        ChatMessage? message = ApprovalMessageBuilder.BuildComplete(ResolvedThenNew("fc_1", "fc_2"), null);

        message.Should().NotBeNull();
        message!.Contents.OfType<ToolApprovalResponseContent>().Should().ContainSingle().Which.Approved.Should().BeFalse();
    }

    [Fact]
    public void BuildComplete_ReturnsNullWhenNothingIsWaiting()
    {
        ApprovalMessageBuilder.BuildComplete(ResolvedThenNew("fc_1", null), null).Should().BeNull();
    }

    [Fact]
    public void BuildComplete_RejectsDecisionForAnAlreadyAnsweredRequest()
    {
        Action act = () => ApprovalMessageBuilder.BuildComplete(
            ResolvedThenNew("fc_1", "fc_2"),
            new List<ApprovalDecision> { new ApprovalDecision("fc_1", true) });

        act.Should().Throw<ArgumentException>();
    }
}
