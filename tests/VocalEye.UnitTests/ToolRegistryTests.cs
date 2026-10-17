using VocalEye.Tools.CodeBuilder;
using VocalEye.Tools.Email;
using VocalEye.Tools.Registry;
using Xunit;

namespace VocalEye.UnitTests;

public class ToolRegistryTests
{
    [Fact]
    public void Find_is_case_insensitive()
    {
        var registry = new ToolRegistry([new SendEmailTool(), new CodeBuilderTool()]);
        Assert.NotNull(registry.Find("SEND_EMAIL"));
        Assert.Null(registry.Find("nope"));
    }
}
