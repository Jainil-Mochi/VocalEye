using VocalEye.Agents;
using VocalEye.Api.Configuration;
using VocalEye.Api.Endpoints;
using VocalEye.Api.Middleware;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Infrastructure;
using VocalEye.Infrastructure.Persistence;
using VocalEye.Tools.CodeBuilder;
using VocalEye.Tools.DesktopCommand;
using VocalEye.Tools.Email;
using VocalEye.Tools.FileGeneration;
using VocalEye.Tools.Registry;
using VocalEye.Tools.Utility;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi(); // built-in .NET OpenAPI document generator (/openapi/v1.json)
builder.Services.AddLlm(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration); // Postgres by default; Persistence:Provider=InMemory for no database

// Tools. Move into a Tools-module AddTools() extension when the tools are ported.
builder.Services.AddSingleton<IAgentTool, FileGenerationTool>();
builder.Services.AddSingleton<IAgentTool, SendEmailTool>();
builder.Services.AddSingleton<IAgentTool, DesktopCommandTool>();
builder.Services.AddSingleton<IAgentTool, CodeBuilderTool>();
builder.Services.AddSingleton<IAgentTool, GetCurrentTimeTool>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();

// Which tool risk levels pause for user approval (config: Tools:RequireApprovalFor).
IReadOnlyList<ToolRiskLevel> approvalRisks = ToolRiskParser.Parse(
    builder.Configuration.GetSection("Tools:RequireApprovalFor").Get<string[]>());
builder.Services.AddAgents(approvalRisks);

WebApplication app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

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
app.MapModelEndpoints();
app.MapChatEndpoints();
app.MapConversationEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}

if (app.Environment.IsDevelopment())
{
    // Creates the tables on first run (no migrations yet, see ADR-0009).
    await app.Services.InitializePersistenceAsync();
}

app.Run();

public partial class Program; // exposed for WebApplicationFactory in integration tests
