using FluentAssertions;
using Moq;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.Email;
using VocalEye.UnitTests.Fakes;
using Xunit;

namespace VocalEye.UnitTests;

public class SendEmailToolTests
{
    [Fact]
    public async Task ExecuteAsync_ValidEmail_SendsItOnce()
    {
        Mock<IEmailSender> sender = CreateSender(true);
        SendEmailTool tool = new SendEmailTool(sender.Object, new EmailOptions());

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { to = "alex@example.com", subject = "Hello", body = "Hi Alex," }),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        sender.Verify(item => item.SendAsync("alex@example.com", "Hello", "Hi Alex,", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AddressWithSpaces_IsCleanedBeforeSending()
    {
        Mock<IEmailSender> sender = CreateSender(true);
        SendEmailTool tool = new SendEmailTool(sender.Object, new EmailOptions());

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { to = "alex @ example.com", subject = "Hello", body = "Hi" }),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        sender.Verify(item => item.SendAsync("alex@example.com", "Hello", "Hi", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("alex@localhost")]
    [InlineData("@example.com")]
    public async Task ExecuteAsync_InvalidAddress_IsRefusedAndNothingIsSent(string to)
    {
        Mock<IEmailSender> sender = CreateSender(true);
        SendEmailTool tool = new SendEmailTool(sender.Object, new EmailOptions());

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { to, subject = "Hello", body = "Hi" }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        sender.Verify(item => item.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_SubjectWithLineBreak_IsRefused()
    {
        Mock<IEmailSender> sender = CreateSender(true);
        SendEmailTool tool = new SendEmailTool(sender.Object, new EmailOptions());

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { to = "alex@example.com", subject = "Hello\r\nBcc: evil@example.com", body = "Hi" }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        sender.Verify(item => item.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NotConfigured_ReturnsClearFailure()
    {
        Mock<IEmailSender> sender = CreateSender(false);
        SendEmailTool tool = new SendEmailTool(sender.Object, new EmailOptions());

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { to = "alex@example.com", subject = "Hello", body = "Hi" }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        tool.Description.Should().Contain("not configured");
        sender.Verify(item => item.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_DomainNotInAllowList_IsRefused()
    {
        Mock<IEmailSender> sender = CreateSender(true);
        EmailOptions options = new EmailOptions();
        options.AllowedRecipientDomains.Add("gmail.com");
        SendEmailTool tool = new SendEmailTool(sender.Object, options);

        ToolResult blocked = await tool.ExecuteAsync(
            JsonArguments.From(new { to = "alex@evil.example", subject = "Hello", body = "Hi" }),
            CancellationToken.None);
        ToolResult allowed = await tool.ExecuteAsync(
            JsonArguments.From(new { to = "alex@GMAIL.com", subject = "Hello", body = "Hi" }),
            CancellationToken.None);

        blocked.Success.Should().BeFalse();
        allowed.Success.Should().BeTrue();
        sender.Verify(item => item.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Risk_IsDestructive_SoItAlwaysNeedsApproval()
    {
        SendEmailTool tool = new SendEmailTool(CreateSender(true).Object, new EmailOptions());

        tool.Risk.Should().Be(ToolRiskLevel.Destructive);
    }

    private static Mock<IEmailSender> CreateSender(bool configured)
    {
        Mock<IEmailSender> sender = new Mock<IEmailSender>();
        sender.SetupGet(item => item.IsConfigured).Returns(configured);
        return sender;
    }
}
