using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Tools.Registry;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools;

    public ToolRegistry(IEnumerable<IAgentTool> tools)
    {
        _tools = tools.ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<IAgentTool> All => _tools.Values;

    public IAgentTool? Find(string name)
    {
        return _tools.GetValueOrDefault(name);
    }
}
