namespace VocalEye.Application.Abstractions.Llm;

/// <summary>The list of models the user may pick from (built from configuration; later from a database per user/tenant).</summary>
public interface IModelCatalog
{
    ModelSelection Default { get; }
    IReadOnlyList<ModelInfo> List();
    ModelInfo? Find(ModelSelection selection);
}
