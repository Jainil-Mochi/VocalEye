using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Contracts.Chat;
using Xunit;

namespace VocalEye.IntegrationTests;

public class ConversationToolHistoryTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ConversationToolHistoryTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMessages_ToolMessage_ShowsNameArgumentsOutputAndFiles()
    {
        ConversationState state = new ConversationState { Id = Guid.NewGuid() };
        state.Messages.Add(new ChatMessage(ChatRole.User, "make a pdf"));
        state.Messages.Add(new ChatMessage(ChatRole.Assistant, new List<AIContent>
        {
            new FunctionCallContent("call_1", "file_generator", new Dictionary<string, object?> { ["filename"] = "ai-basics" }),
        }));
        state.Messages.Add(new ChatMessage(ChatRole.Tool, new List<AIContent>
        {
            new FunctionResultContent("call_1", "Saved the PDF file: C:\\docs\\ai-basics.pdf\n[artifacts] [{\"kind\":\"file\",\"value\":\"C:\\\\docs\\\\ai-basics.pdf\"}]"),
        }));
        await SaveAsync(state);

        List<ConversationMessageDto>? messages = await _factory.CreateClient()
            .GetFromJsonAsync<List<ConversationMessageDto>>($"/api/conversations/{state.Id}/messages");

        ToolCallDto result = messages!.Single(message => message.ToolResults.Count > 0).ToolResults.Single();
        result.Name.Should().Be("file_generator");
        result.ArgumentsJson.Should().Contain("ai-basics");
        result.Success.Should().BeTrue();
        result.Output.Should().Be("Saved the PDF file: C:\\docs\\ai-basics.pdf");
        result.Artifacts.Should().ContainSingle().Which.Value.Should().Be("C:\\docs\\ai-basics.pdf");
    }

    [Fact]
    public async Task GetPendingApprovals_ConversationWaitingForTheUser_ListsTheToolCall()
    {
        ConversationState state = new ConversationState { Id = Guid.NewGuid() };
        state.Messages.Add(new ChatMessage(ChatRole.User, "send an email"));
        FunctionCallContent call = new FunctionCallContent("call_9", "send_email", new Dictionary<string, object?> { ["to"] = "a@example.com" });
        state.Messages.Add(new ChatMessage(ChatRole.Assistant, new List<AIContent> { new ToolApprovalRequestContent("req-9", call) }));
        await SaveAsync(state);

        List<PendingApprovalDto>? pending = await _factory.CreateClient()
            .GetFromJsonAsync<List<PendingApprovalDto>>($"/api/conversations/{state.Id}/pending-approvals");

        PendingApprovalDto approval = pending.Should().ContainSingle().Subject;
        approval.ApprovalId.Should().Be("call_9");
        approval.ToolName.Should().Be("send_email");
        approval.ArgumentsJson.Should().Contain("a@example.com");
    }

    [Fact]
    public async Task GetPendingApprovals_UnknownConversation_Returns404()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync($"/api/conversations/{Guid.NewGuid()}/pending-approvals");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task SaveAsync(ConversationState state)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IConversationStore store = scope.ServiceProvider.GetRequiredService<IConversationStore>();
        await store.SaveAsync(state, CancellationToken.None);
    }
}
