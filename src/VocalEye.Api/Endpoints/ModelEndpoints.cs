using VocalEye.Application.Abstractions.Llm;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Endpoints;

public static class ModelEndpoints
{
    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/models", ListModels);

        return app;
    }

    private static List<ModelDto> ListModels(IModelCatalog catalog)
    {
        ModelSelection defaultModel = catalog.Default;

        return catalog.List()
            .Select(model => new ModelDto
            {
                Provider = model.Provider,
                Model = model.Model,
                DisplayName = model.DisplayName,
                SupportsTools = model.SupportsTools,
                SupportsStreaming = model.SupportsStreaming,
                SupportsVision = model.SupportsVision,
                IsDefault = model.Provider == defaultModel.Provider
                    && string.Equals(model.Model, defaultModel.Model, StringComparison.OrdinalIgnoreCase),
            })
            .ToList();
    }
}
