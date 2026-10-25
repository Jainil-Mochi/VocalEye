using FluentAssertions;
using VocalEye.Api.Configuration;
using Xunit;

namespace VocalEye.IntegrationTests;

public class ApiAccessOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0123456789abcdef")]
    public void Validate_NoTokenOrLongEnoughToken_Passes(string? token)
    {
        ApiAccessOptions options = new ApiAccessOptions { AccessToken = token };

        Action act = options.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("   ")]
    public void Validate_TooShortOrBlankToken_ThrowsAtStartup(string token)
    {
        ApiAccessOptions options = new ApiAccessOptions { AccessToken = token };

        Action act = options.Validate;

        act.Should().Throw<InvalidOperationException>().WithMessage("*Api:AccessToken*16*");
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("0123456789abcdef", true)]
    public void IsEnabled_FollowsWhetherATokenIsSet(string? token, bool expected)
    {
        new ApiAccessOptions { AccessToken = token }.IsEnabled.Should().Be(expected);
    }
}
