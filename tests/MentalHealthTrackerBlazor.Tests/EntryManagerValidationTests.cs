using System;
using Xunit;

namespace MentalHealthTrackerBlazor.Tests;

/// <summary>
/// Checks the input gates that run before EntryManager touches the database.
/// No connection is opened on these paths, so they run without SQL Server.
/// </summary>
public class EntryManagerValidationTests
{
    // Deliberately unreachable: any test that accidentally opens a connection
    // fails fast instead of hanging.
    private const string Unreachable =
        "Server=127.0.0.1,1;Database=does_not_exist;User Id=sa;Password=none;" +
        "Connect Timeout=1;TrustServerCertificate=True;";

    private static EntryManager CreateManager() => new(Unreachable);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterUser_RejectsMissingUsername(string? username)
        => Assert.False(CreateManager().RegisterUser(username!, "password"));

    [Fact]
    public void RegisterUser_RejectsUsernameLongerThanTheColumn()
    {
        // Users.Username is nvarchar(50); 51 characters must not reach the database.
        Assert.False(CreateManager().RegisterUser(new string('u', 51), "password"));
        Assert.False(CreateManager().RegisterUser(new string('u', 500), "password"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RegisterUser_RejectsMissingPassword(string? password)
        => Assert.False(CreateManager().RegisterUser("someone", password!));

    [Fact]
    public void RegisterUser_RejectsOverlongPassword()
        => Assert.False(CreateManager().RegisterUser("someone", new string('p', 257)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUser_RejectsMissingUsername(string? username)
        => Assert.Equal(0, CreateManager().ValidateUser(username!, "password"));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateUser_RejectsMissingPassword(string? password)
        => Assert.Equal(0, CreateManager().ValidateUser("someone", password!));

    [Fact]
    public void ValidateUser_ReturnsZeroForMissingCredentials()
        => Assert.Equal(0, CreateManager().ValidateUser(null!, null!));
}
