namespace VocalEye.Api.Middleware;

/// <summary>
/// The single catch-all for the API (coding standards section 7). Domain and application code lets faults bubble up;
/// this turns them into RFC 9457 problem responses and logs them once.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Path} was cancelled by the client.", context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "Unhandled exception after the response started for {Path}.", context.Request.Path);
                throw;
            }

            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        (int status, string title) = Classify(exception);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Request {Method} {Path} failed with {Status}.", context.Request.Method, context.Request.Path, status);
        }
        else
        {
            _logger.LogWarning("Request {Method} {Path} rejected with {Status}: {Message}", context.Request.Method, context.Request.Path, status, exception.Message);
        }

        // Internal faults never leak details outside Development. Client errors and upstream model errors are safe and useful.
        bool exposeDetail = status < StatusCodes.Status500InternalServerError
            || status == StatusCodes.Status502BadGateway
            || _environment.IsDevelopment();

        IResult problem = Results.Problem(
            title: title,
            detail: exposeDetail ? exception.Message : "An unexpected error occurred.",
            statusCode: status);

        await problem.ExecuteAsync(context);
    }

    private static (int Status, string Title) Classify(Exception exception)
    {
        return exception switch
        {
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Invalid request"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            HttpRequestException => (StatusCodes.Status502BadGateway, "Model call failed"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error"),
        };
    }
}
