using FluentAssertions;
using Microsoft.Extensions.Options;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Infrastructure.Llm;
using Xunit;

namespace VocalEye.UnitTests;

public class ModelCatalogTests
{
    [Fact]
    public void List_ProvidersWithoutCredentials_AreHidden()
    {
        ModelCatalog catalog = Create(Sample(geminiKey: "key", openAiKey: null));

        catalog.List().Select(model => model.Provider).Should().Equal("gemini", "ollama");
    }

    [Fact]
    public void List_OllamaWithoutEndpoint_IsHiddenAndCannotBecomeDefault()
    {
        LlmOptions options = Sample(geminiKey: null, openAiKey: null);
        options.Providers["ollama"].Endpoint = string.Empty;

        ModelCatalog catalog = Create(options);

        catalog.List().Should().BeEmpty();
    }

    [Fact]
    public void Default_ConfiguredModelAvailable_IsUsed()
    {
        ModelCatalog catalog = Create(Sample("key", "key"));

        catalog.Default.Should().Be(new ModelSelection("gemini", "g-1"));
    }

    [Fact]
    public void Default_ConfiguredModelHasNoKey_FallsBackToFirstAvailable()
    {
        ModelCatalog catalog = Create(Sample(geminiKey: null, openAiKey: "key"));

        catalog.Default.Provider.Should().Be("openai");
    }

    [Fact]
    public void Find_IsCaseInsensitiveAndReturnsNullForUnknown()
    {
        ModelCatalog catalog = Create(Sample("key", "key"));

        catalog.Find(new ModelSelection("OPENAI", "O-1")).Should().NotBeNull();
        catalog.Find(new ModelSelection("openai", "nope")).Should().BeNull();
    }

    private static ModelCatalog Create(LlmOptions options)
    {
        return new ModelCatalog(Options.Create(options));
    }

    private static LlmOptions Sample(string? geminiKey, string? openAiKey)
    {
        return new LlmOptions
        {
            Default = new DefaultModelOptions { Provider = "gemini", Model = "g-1" },
            Providers =
            {
                ["gemini"] = new ProviderOptions
                {
                    Type = "OpenAICompatible",
                    ApiKey = geminiKey,
                    Models = new List<ModelOptions> { new ModelOptions { Id = "g-1" } },
                },
                ["openai"] = new ProviderOptions
                {
                    Type = "OpenAI",
                    ApiKey = openAiKey,
                    Models = new List<ModelOptions> { new ModelOptions { Id = "o-1", DisplayName = "O One" } },
                },
                ["ollama"] = new ProviderOptions
                {
                    Type = "Ollama",
                    Endpoint = "http://localhost:11434",
                    Models = new List<ModelOptions> { new ModelOptions { Id = "l-1" } },
                },
            },
        };
    }
}
