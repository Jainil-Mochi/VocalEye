using VocalEye.Agents;
using VocalEye.Api.Endpoints;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Infrastructure;
using VocalEye.Tools.CodeBuilder;
using VocalEye.Tools.DesktopCommand;
using VocalEye.Tools.Email;
using VocalEye.Tools.FileGeneration;
using VocalEye.Tools.Registry;
using VocalEye.Tools.Utility;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi(); // built-in .NET OpenAPI document generator (/openapi/v1.json)
builder.Services.AddLlm(builder.Configuration);
builder.Services.AddInMemoryConversationStore();

// Tools. Move into a Tools-module AddTools() extension when the tools are ported.
builder.Services.AddSingleton<IAgentTool, FileGenerationTool>();
builder.Services.AddSingleton<IAgentTool, SendEmailTool>();
builder.Services.AddSingleton<IAgentTool, DesktopCommandTool>();
builder.Services.AddSingleton<IAgentTool, CodeBuilderTool>();
builder.Services.AddSingleton<IAgentTool, GetCurrentTimeTool>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();

// Which tool risk levels pause for user approval (config: Tools:RequireApprovalFor).
var approvalRisks = (builder.Configuration.GetSection("Tools:RequireApprovalFor").Get<string[]>() ?? [])
    .Select(r => Enum.Parse<ToolRiskLevel>(r, ignoreCase: true));
builder.Services.AddAgents(approvalRisks);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                   // GET /openapi/v1.json
    app.UseSwaggerUI(o =>                               // GET /swagger
    {
        o.SwaggerEndpoint("/openapi/v1.json", "VocalEye API v1");
        o.DocumentTitle = "VocalEye API";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapHealthChecks("/health");
app.MapGet("/api/tools", (IToolRegistry registry) =>
    registry.All.Select(t => new { t.Name, t.Description, Risk = t.Risk.ToString() }));
app.MapModelEndpoints();
app.MapChatEndpoints();
if (app.Environment.IsDevelopment())
    app.MapDevEndpoints();

app.Run();

public partial class Program; // exposed for WebApplicationFactory in integration tests
