using FluentAssertions;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.CodeBuilder;
using VocalEye.Tools.Email;
using VocalEye.Tools.Registry;
using Xunit;

namespace VocalEye.UnitTests;

public class ToolRegistryTests
{
    [Fact]
    public void Find_IsCaseInsensitive()
    {
        ToolRegistry registry = new ToolRegistry(new IAgentTool[] { new SendEmailTool(), new CodeBuilderTool() });

        registry.Find("SEND_EMAIL").Should().NotBeNull();
        registry.Find("nope").Should().BeNull();
    }
}
