using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Endpoints;

public static class ModelEndpoints
{
    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/models", (IModelCatalog catalog) =>
        {
            var def = catalog.Default;

            return catalog.List().Select(m => new ModelDto
            {
                Provider = m.Provider,
                Model = m.Model,
                DisplayName = m.DisplayName,
                SupportsTools = m.SupportsTools,
                SupportsStreaming = m.SupportsStreaming,
                SupportsVision = m.SupportsVision,
                IsDefault = m.Provider == def.Provider && string.Equals(m.Model, def.Model, StringComparison.OrdinalIgnoreCase)
            });
        }); // <-- Fixed: Added missing closing parenthesis and semicolon here

        return app;
    }
}
