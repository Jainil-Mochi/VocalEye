namespace VocalEye.Application.Abstractions.Tools;

public interface IToolRegistry
{
    IReadOnlyCollection<IAgentTool> All { get; }
    IAgentTool? Find(string name);
}
