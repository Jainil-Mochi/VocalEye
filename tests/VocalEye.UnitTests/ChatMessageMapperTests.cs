using FluentAssertions;
using Microsoft.Extensions.AI;
using VocalEye.Application.Conversations;
using VocalEye.Domain.Conversations;
using Xunit;

namespace VocalEye.UnitTests;

public class ChatMessageMapperTests
{
    [Fact]
    public void ToJson_ThenFromJson_KeepsTextAndRole()
    {
        ChatMessage original = new ChatMessage(ChatRole.User, "hello there");

        ChatMessage restored = ChatMessageMapper.FromJson(ChatMessageMapper.ToJson(original));

        restored.Role.Should().Be(ChatRole.User);
        restored.Text.Should().Be("hello there");
    }

    [Fact]
    public void ToJson_ThenFromJson_KeepsToolCalls()
    {
        FunctionCallContent call = new FunctionCallContent("c1", "get_time", new Dictionary<string, object?> { ["timezone"] = "UTC" });
        ChatMessage original = new ChatMessage(ChatRole.Assistant, new List<AIContent> { call });

        ChatMessage restored = ChatMessageMapper.FromJson(ChatMessageMapper.ToJson(original));

        FunctionCallContent restoredCall = restored.Contents.OfType<FunctionCallContent>().Should().ContainSingle().Subject;
        restoredCall.CallId.Should().Be("c1");
        restoredCall.Name.Should().Be("get_time");
    }

    [Fact]
    public void FromJson_NotAMessage_ThrowsInvalidOperationException()
    {
        Action act = () => ChatMessageMapper.FromJson("null");

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("user", MessageRole.User)]
    [InlineData("assistant", MessageRole.Assistant)]
    [InlineData("tool", MessageRole.Tool)]
    [InlineData("system", MessageRole.System)]
    public void ToRole_KnownRoles_MapToDomainRoles(string role, MessageRole expected)
    {
        MessageRole result = ChatMessageMapper.ToRole(new ChatRole(role));

        result.Should().Be(expected);
    }

    [Fact]
    public void ToRole_UnknownRole_ThrowsInvalidOperationException()
    {
        Action act = () => ChatMessageMapper.ToRole(new ChatRole("narrator"));

        act.Should().Throw<InvalidOperationException>();
    }
}
