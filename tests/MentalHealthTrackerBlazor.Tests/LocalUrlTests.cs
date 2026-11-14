using MentalHealthTrackerBlazor.Security;
using Xunit;

namespace MentalHealthTrackerBlazor.Tests;

/// <summary>
/// LocalUrl is the only thing standing between a crafted <c>?returnUrl=</c> and an
/// open redirect straight off the site after a successful login.
/// </summary>
public class LocalUrlTests
{
    [Theory]
    [InlineData("/", true)]
    [InlineData("/mht", true)]
    [InlineData("/mht?", true)]
    [InlineData("/mht?date=2026-01-01", true)]
    [InlineData("/logout", true)]
    public void IsLocal_AcceptsAbsolutePathsOnThisHost(string url, bool expected)
        => Assert.Equal(expected, LocalUrl.IsLocal(url));

    [Theory]
    [InlineData("//evil.example")]
    [InlineData("//evil.example/mht")]
    [InlineData("///evil.example")]
    public void IsLocal_RejectsProtocolRelativeUrls(string url)
        => Assert.False(LocalUrl.IsLocal(url));

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("http://evil.example/mht")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    public void IsLocal_RejectsAbsoluteAndScriptUrls(string url)
        => Assert.False(LocalUrl.IsLocal(url));

    [Theory]
    [InlineData("mht")]
    [InlineData("?returnUrl=/mht")]
    [InlineData("#fragment")]
    public void IsLocal_RejectsUrlsWithoutALeadingSlash(string url)
        => Assert.False(LocalUrl.IsLocal(url));

    [Fact]
    public void IsLocal_RejectsBackslashTricks()
    {
        // Browsers normalise "\" to "/", so "/\evil.example" would become "//evil.example".
        Assert.False(LocalUrl.IsLocal("/\\evil.example"));
        Assert.False(LocalUrl.IsLocal("/\\\\evil.example"));
        Assert.False(LocalUrl.IsLocal("/\t/evil.example"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsLocal_RejectsMissingValues(string? url)
        => Assert.False(LocalUrl.IsLocal(url));

    [Theory]
    [InlineData("\t/mht")]
    [InlineData("/mht\r\nSet-Cookie: a=b")]
    [InlineData("/mht\u0000")]
    public void IsLocal_RejectsControlCharacters(string url)
        => Assert.False(LocalUrl.IsLocal(url));

    [Fact]
    public void IsLocal_RejectsOverlongInput()
    {
        string justUnder = "/" + new string('a', 2047);
        string justOver = "/" + new string('a', 2048);

        Assert.Equal(2048, justUnder.Length);
        Assert.True(LocalUrl.IsLocal(justUnder));

        Assert.Equal(2049, justOver.Length);
        Assert.False(LocalUrl.IsLocal(justOver));
    }

    [Theory]
    [InlineData(null, "/mht")]
    [InlineData("", "/mht")]
    [InlineData("https://evil.example", "/mht")]
    [InlineData("//evil.example", "/mht")]
    [InlineData("/mht", "/mht")]
    [InlineData("/mht?date=2026-01-01", "/mht?date=2026-01-01")]
    public void Sanitize_FallsBackForAnythingNotLocal(string? url, string expected)
        => Assert.Equal(expected, LocalUrl.Sanitize(url));

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData(null)]
    public void Sanitize_HonoursACustomFallback(string? url)
        => Assert.Equal("/somewhere-else", LocalUrl.Sanitize(url, "/somewhere-else"));

    [Fact]
    public void Sanitize_KeepsAValidUrlUnchanged()
        => Assert.Equal("/mht?date=2026-01-01", LocalUrl.Sanitize("/mht?date=2026-01-01"));
}
