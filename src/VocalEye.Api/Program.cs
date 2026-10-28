using VocalEye.Agents;
using VocalEye.Api.Configuration;
using VocalEye.Api.Endpoints;
using VocalEye.Api.Middleware;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Infrastructure;
using VocalEye.Infrastructure.Persistence;
using VocalEye.Tools;
using VocalEye.Tools.Common;
using VocalEye.Tools.DesktopCommand;
using VocalEye.Tools.Email;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Optional shared secret for every /api call (the desktop app sets it for each launch). Empty means open.
ApiAccessOptions apiAccess = builder.Configuration.GetSection("Api").Get<ApiAccessOptions>() ?? new ApiAccessOptions();
apiAccess.Validate();
builder.Services.AddSingleton(apiAccess);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi(); // built-in .NET OpenAPI document generator (/openapi/v1.json)
builder.Services.AddLlm(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration); // Persistence:Provider = Postgres (default) | Sqlite (desktop) | InMemory

// Tools. Output folder for file-writing tools: config section "Tools:Output" (default Documents\VocalEye).
ToolOutputOptions toolOutput = builder.Configuration.GetSection("Tools:Output").Get<ToolOutputOptions>() ?? new ToolOutputOptions();
DesktopCommandOptions toolDesktop = builder.Configuration.GetSection("Tools:Desktop").Get<DesktopCommandOptions>() ?? new DesktopCommandOptions();
EmailOptions toolEmail = builder.Configuration.GetSection("Tools:Email").Get<EmailOptions>() ?? new EmailOptions();
builder.Services.AddTools(toolOutput, toolDesktop, toolEmail);

// Which tool risk levels pause for user approval (config: Tools:RequireApprovalFor).
IReadOnlyList<ToolRiskLevel> approvalRisks = ToolRiskParser.Parse(
    builder.Configuration.GetSection("Tools:RequireApprovalFor").Get<string[]>());
AgentOptions agentOptions = builder.Configuration.GetSection("Agents").Get<AgentOptions>() ?? new AgentOptions();
agentOptions.Validate();
builder.Services.AddAgents(approvalRisks, agentOptions);

WebApplication app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<AccessTokenMiddleware>();

if (!apiAccess.IsEnabled && !app.Environment.IsDevelopment())
{
    app.Logger.LogWarning("Api:AccessToken is not set: every program on this computer can call the API.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                   // GET /openapi/v1.json
    app.UseSwaggerUI(options =>                         // GET /swagger
    {
        options.SwaggerEndpoint("/openapi/v1.json", "VocalEye API v1");
        options.DocumentTitle = "VocalEye API";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapHealthChecks("/health");
app.MapGet("/api/tools", (IToolRegistry registry) =>
    registry.All.Select(tool => new { tool.Name, tool.Description, Risk = tool.Risk.ToString() }));
app.MapInfoEndpoints();
app.MapModelEndpoints();
app.MapChatEndpoints();
app.MapConversationEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}

if (app.Environment.IsDevelopment() || PersistenceProviders.IsSqlite(builder.Configuration))
{
    // Creates the tables on first run (no migrations yet, see ADR-0009). SQLite is the desktop database, so it is
    // created in every environment; Postgres only in Development.
    await app.Services.InitializePersistenceAsync();
}

app.Run();

public partial class Program; // exposed for WebApplicationFactory in integration tests
