using VocalEye.Application.Abstractions.Llm;

namespace VocalEye.Application.Abstractions.Agents;

/// <summary>The chosen model could not be called (network, bad key, quota, provider error). Names the model.</summary>
public sealed class ModelCallFailedException(ModelSelection model, Exception inner)
    : Exception($"Model '{model.Provider}/{model.Model}' failed: {inner.Message}", inner)
{
    public ModelSelection Model { get; } = model;
}
