using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace VocalEye.Infrastructure.Llm.Providers;

/// <summary>
/// The only place provider SDKs are referenced. One arm per provider Type.
/// Gemini, Groq, OpenRouter, LM Studio, vLLM and Anthropic's OpenAI-compatible endpoint all use "OpenAICompatible".
/// </summary>
internal static class ProviderClientBuilder
{
    public static IChatClient Build(ProviderOptions provider, string model)
    {
        return provider.Type.ToLowerInvariant() switch
        {
            "openai" => BuildOpenAi(provider, endpoint: null, model),
            "openaicompatible" => BuildOpenAi(provider, provider.Endpoint, model),
            "azureopenai" => BuildAzureOpenAi(provider, model),
            "ollama" => BuildOllama(provider, model),
            _ => throw new NotSupportedException($"Unknown provider type '{provider.Type}'."),
        };
    }

    private static IChatClient BuildOpenAi(ProviderOptions provider, string? endpoint, string model)
    {
        OpenAIClientOptions options = new OpenAIClientOptions();
        ApplyLimits(options, provider);

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            options.Endpoint = new Uri(endpoint);
        }

        UseGeminiSignatureWorkaround(options, provider, endpoint);

        return new OpenAIClient(new ApiKeyCredential(provider.ApiKey!), options)
            .GetChatClient(model)
            .AsIChatClient();
    }

    /// <summary>Gemini 3 rejects tool calls sent back without their thought signature, which the OpenAI client drops.</summary>
    private static void UseGeminiSignatureWorkaround(OpenAIClientOptions options, ProviderOptions provider, string? endpoint)
    {
        string fallback = provider.ThoughtSignatureFallback ?? GeminiThoughtSignatureHandler.DefaultFallback;
        if (!GeminiThoughtSignatureHandler.AppliesTo(endpoint) || string.IsNullOrWhiteSpace(fallback))
        {
            return;
        }

        // The SDK pipeline already enforces the timeout and retries, so the HttpClient must not add its own 100 s limit.
        HttpClient httpClient = new HttpClient(new GeminiThoughtSignatureHandler(fallback, new HttpClientHandler()))
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

        options.Transport = new HttpClientPipelineTransport(httpClient);
    }

    private static IChatClient BuildAzureOpenAi(ProviderOptions provider, string model)
    {
        AzureOpenAIClientOptions options = new AzureOpenAIClientOptions();
        ApplyLimits(options, provider);

        return new AzureOpenAIClient(new Uri(provider.Endpoint!), new ApiKeyCredential(provider.ApiKey!), options)
            .GetChatClient(model)
            .AsIChatClient();
    }

    private static IChatClient BuildOllama(ProviderOptions provider, string model)
    {
        HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri(provider.Endpoint!),
            Timeout = TimeSpan.FromSeconds(Math.Max(1, provider.RequestTimeoutSeconds)),
        };

        return new OllamaApiClient(httpClient, model);
    }

    /// <summary>Without this the SDK waits 100 seconds per try and retries 3 times, so one stuck call can hold a request for ~7 minutes.</summary>
    private static void ApplyLimits(ClientPipelineOptions options, ProviderOptions provider)
    {
        options.NetworkTimeout = TimeSpan.FromSeconds(Math.Max(1, provider.RequestTimeoutSeconds));
        options.RetryPolicy = new ClientRetryPolicy(Math.Max(0, provider.MaxRetries));
    }
}
