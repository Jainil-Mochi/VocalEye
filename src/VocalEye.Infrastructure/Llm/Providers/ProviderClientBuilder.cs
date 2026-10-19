using System.ClientModel;
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
            "openai" => BuildOpenAi(provider.ApiKey!, endpoint: null, model),
            "openaicompatible" => BuildOpenAi(provider.ApiKey!, provider.Endpoint, model),
            "azureopenai" => new AzureOpenAIClient(new Uri(provider.Endpoint!), new ApiKeyCredential(provider.ApiKey!))
                .GetChatClient(model)
                .AsIChatClient(),
            "ollama" => new OllamaApiClient(new Uri(provider.Endpoint!), model),
            _ => throw new NotSupportedException($"Unknown provider type '{provider.Type}'."),
        };
    }

    private static IChatClient BuildOpenAi(string apiKey, string? endpoint, string model)
    {
        OpenAIClientOptions options = new OpenAIClientOptions();

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            options.Endpoint = new Uri(endpoint);
        }

        return new OpenAIClient(new ApiKeyCredential(apiKey), options)
            .GetChatClient(model)
            .AsIChatClient();
    }
}
