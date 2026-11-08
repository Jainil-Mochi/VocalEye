using System.Text.Json.Nodes;
using FluentAssertions;
using VocalEye.Infrastructure.Llm.Providers;
using Xunit;

namespace VocalEye.UnitTests;

public class GeminiThoughtSignatureHandlerTests
{
    private const string Fallback = "skip_thought_signature_validator";

    private const string RequestWithToolCall = """
        {"model":"gemini-3-pro","messages":[
          {"role":"user","content":"make a file"},
          {"role":"assistant","content":null,"tool_calls":[
            {"id":"fc_1","type":"function","function":{"name":"file_generator","arguments":"{}"}},
            {"id":"fc_2","type":"function","function":{"name":"get_current_time","arguments":"{}"}}]},
          {"role":"tool","tool_call_id":"fc_1","content":"done"}]}
        """;

    [Fact]
    public void AddMissingSignatures_FirstToolCall_GetsThePlaceholder()
    {
        string? patched = GeminiThoughtSignatureHandler.AddMissingSignatures(RequestWithToolCall, Fallback);

        patched.Should().NotBeNull();
        JsonArray calls = JsonNode.Parse(patched!)!["messages"]![1]!["tool_calls"]!.AsArray();
        calls[0]!["extra_content"]!["google"]!["thought_signature"]!.GetValue<string>().Should().Be(Fallback);
    }

    [Fact]
    public void AddMissingSignatures_SecondParallelCall_IsLeftAlone()
    {
        string? patched = GeminiThoughtSignatureHandler.AddMissingSignatures(RequestWithToolCall, Fallback);

        JsonArray calls = JsonNode.Parse(patched!)!["messages"]![1]!["tool_calls"]!.AsArray();
        calls[1]!["extra_content"].Should().BeNull();
    }

    [Fact]
    public void AddMissingSignatures_ExistingSignature_IsKept()
    {
        string json = """
            {"messages":[{"role":"assistant","tool_calls":[
              {"id":"a","type":"function","function":{"name":"x","arguments":"{}"},
               "extra_content":{"google":{"thought_signature":"REAL"}}}]}]}
            """;

        GeminiThoughtSignatureHandler.AddMissingSignatures(json, Fallback).Should().BeNull();
    }

    [Theory]
    [InlineData("""{"messages":[{"role":"user","content":"hi"}]}""")]
    [InlineData("""{"messages":[{"role":"assistant","content":"hello"}]}""")]
    [InlineData("""{"input":"not a chat request"}""")]
    [InlineData("not json")]
    public void AddMissingSignatures_NothingToDo_ReturnsNull(string json)
    {
        GeminiThoughtSignatureHandler.AddMissingSignatures(json, Fallback).Should().BeNull();
    }

    [Fact]
    public async Task Handler_PatchesTheBodyThatIsSent()
    {
        CapturingHandler inner = new CapturingHandler();
        using HttpClient client = new HttpClient(new GeminiThoughtSignatureHandler(Fallback, inner));
        using StringContent content = new StringContent(RequestWithToolCall);

        await client.PostAsync("https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", content);

        inner.Body.Should().Contain("thought_signature").And.Contain(Fallback);
    }

    [Theory]
    [InlineData("https://generativelanguage.googleapis.com/v1beta/openai/", true)]
    [InlineData("https://api.groq.com/openai/v1", false)]
    [InlineData(null, false)]
    [InlineData("not a url", false)]
    public void AppliesTo_OnlyForGeminiEndpoint(string? endpoint, bool expected)
    {
        GeminiThoughtSignatureHandler.AppliesTo(endpoint).Should().Be(expected);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }
}
