using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VocalEye.Infrastructure.Llm.Providers;

/// <summary>
/// Gemini 3 models attach an opaque "thought signature" to each function call and reject (HTTP 400, "Function call is
/// missing a thought_signature") a request that sends the call back without it. The OpenAI client we use for Gemini's
/// OpenAI-compatible endpoint drops that field, so every tool round would fail.
/// This handler adds Google's documented placeholder signature to the first tool call of every assistant message that has
/// none. A real signature is kept when one is present. The model then lacks its earlier reasoning for that step, which is
/// the documented trade-off of the placeholder.
/// </summary>
internal sealed class GeminiThoughtSignatureHandler : DelegatingHandler
{
    public const string DefaultFallback = "skip_thought_signature_validator";

    private readonly string _fallback;

    public GeminiThoughtSignatureHandler(string fallback, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);

        _fallback = fallback;
    }

    /// <summary>True for Gemini's OpenAI-compatible endpoint.</summary>
    public static bool AppliesTo(string? endpoint)
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)
            && uri.Host.Equals("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Adds the placeholder where a signature is missing.</summary>
    /// <returns>The changed request body, or null when nothing needed changing (or the body is not a chat request).</returns>
    internal static string? AddMissingSignatures(string json, string fallback)
    {
        JsonNode? root;

        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (root?["messages"] is not JsonArray messages)
        {
            return null;
        }

        bool changed = false;

        foreach (JsonNode? node in messages)
        {
            if (node is not JsonObject message)
            {
                continue;
            }

            string? role = message["role"]?.GetValue<string>();
            if (!string.Equals(role, "assistant", StringComparison.Ordinal))
            {
                continue;
            }

            if (message["tool_calls"] is JsonArray calls && calls.Count > 0 && calls[0] is JsonObject first && first["extra_content"] is null)
            {
                first["extra_content"] = new JsonObject
                {
                    ["google"] = new JsonObject
                    {
                        ["thought_signature"] = fallback,
                    },
                };
                changed = true;
            }
        }

        return changed ? root.ToJsonString() : null;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        bool isChatRequest = request.Method == HttpMethod.Post
            && request.Content is not null
            && request.RequestUri?.AbsolutePath.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) == true;

        if (isChatRequest)
        {
            string body = await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string? patched = AddMissingSignatures(body, _fallback);

            request.Content = new StringContent(patched ?? body, Encoding.UTF8, "application/json");
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
