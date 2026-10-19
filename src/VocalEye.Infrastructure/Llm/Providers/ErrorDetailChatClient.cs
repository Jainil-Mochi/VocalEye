using System.ClientModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace VocalEye.Infrastructure.Llm.Providers;

/// <summary>
/// Translates provider SDK errors into <see cref="HttpRequestException"/> that name the model and include the
/// response body. SDK messages often say only "Status: 400 (Bad Request)" and hide the real reason in the body.
/// This is a vendor-exception translation boundary (ADR-0008), not error swallowing.
/// </summary>
internal sealed class ErrorDetailChatClient : DelegatingChatClient
{
    private const int MaximumBodyCharacters = 1000;

    private readonly string _modelLabel;

    public ErrorDetailChatClient(IChatClient innerClient, string modelLabel)
        : base(innerClient)
    {
        _modelLabel = modelLabel;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        }
        catch (ClientResultException exception)
        {
            throw new HttpRequestException(Describe(exception), exception);
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using IAsyncEnumerator<ChatResponseUpdate> enumerator =
            base.GetStreamingResponseAsync(messages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            bool hasNext;

            try
            {
                hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
            }
            catch (ClientResultException exception)
            {
                throw new HttpRequestException(Describe(exception), exception);
            }

            if (!hasNext)
            {
                yield break;
            }

            yield return enumerator.Current;
        }
    }

    private string Describe(ClientResultException exception)
    {
        string body;

        try
        {
            body = exception.GetRawResponse()?.Content.ToString() ?? string.Empty;
        }
        catch (InvalidOperationException)
        {
            body = string.Empty; // response content was not buffered
        }

        body = body.Trim();
        if (body.Length > MaximumBodyCharacters)
        {
            body = string.Concat(body.AsSpan(0, MaximumBodyCharacters), "...");
        }

        return string.IsNullOrEmpty(body)
            ? $"Model '{_modelLabel}' failed: {exception.Message}"
            : $"Model '{_modelLabel}' failed: {exception.Message} Response body: {body}";
    }
}
