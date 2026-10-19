namespace VocalEye.Application.Abstractions.Llm;

/// <summary>The models the user may pick from (built from configuration).</summary>
public interface IModelCatalog
{
    ModelSelection Default { get; }

    IReadOnlyList<ModelInfo> List();

    ModelInfo? Find(ModelSelection selection);
}
