using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using MentalHealthTrackerBlazor.Data;
using Xunit;

namespace MentalHealthTrackerBlazor.Tests;

/// <summary>
/// Exercises the claims the app makes about isolation and password storage against a real
/// SQL Server. Skipped unless <c>MHT_TEST_CONNECTION</c> is set — see the README.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EntryManagerDatabaseTests : IDisposable
{
    private readonly string _connectionString;
    private readonly EntryManager _sut;
    private readonly List<string> _usernames = new();
    private readonly List<int> _userIds = new();

    public EntryManagerDatabaseTests()
    {
        _connectionString = Environment.GetEnvironmentVariable(RequiresDatabaseFactAttribute.VariableName)!;
        _sut = new EntryManager(_connectionString);
        EnsureSchema();
    }

    // ---- account handling -------------------------------------------------

    [RequiresDatabaseFact]
    public void RegisteredPassword_LogsIn()
    {
        string username = NewUsername("login");

        Assert.True(_sut.RegisterUser(username, "CorrectHorse1!"));

        int userId = _sut.ValidateUser(username, "CorrectHorse1!");
        Assert.True(userId > 0);
        _userIds.Add(userId);
    }

    [RequiresDatabaseFact]
    public void WrongPassword_IsRejected()
    {
        string username = NewUsername("wrongpw");
        Assert.True(_sut.RegisterUser(username, "CorrectHorse1!"));
        _userIds.Add(_sut.ValidateUser(username, "CorrectHorse1!"));

        Assert.Equal(0, _sut.ValidateUser(username, "CorrectHorse1"));
    }

    [RequiresDatabaseFact]
    public void UnknownUser_IsRejected()
        => Assert.Equal(0, _sut.ValidateUser(NewUsername("ghost"), "anything"));

    [RequiresDatabaseFact]
    public void DuplicateRegistration_IsRejected()
    {
        string username = NewUsername("dup");

        Assert.True(_sut.RegisterUser(username, "CorrectHorse1!"));
        _userIds.Add(_sut.ValidateUser(username, "CorrectHorse1!"));

        Assert.False(_sut.RegisterUser(username, "AnotherPassword2!"));
    }

    [RequiresDatabaseFact]
    public void PasswordIsStoredHashed_NotInPlaintext()
    {
        string username = NewUsername("hash");
        Assert.True(_sut.RegisterUser(username, "CorrectHorse1!"));
        _userIds.Add(_sut.ValidateUser(username, "CorrectHorse1!"));

        string stored = ReadPasswordHash(username);

        Assert.NotEqual("CorrectHorse1!", stored);
        Assert.StartsWith("AQAAAA", stored); // ASP.NET Core Identity PBKDF2 format marker
    }

    [RequiresDatabaseFact]
    public void LegacyPlaintextRow_IsUpgradedOnSuccessfulLogin()
    {
        string username = NewUsername("legacy");
        InsertLegacyPlaintextUser(username, "not-hashed-yet");

        int userId = _sut.ValidateUser(username, "not-hashed-yet");

        Assert.True(userId > 0);
        _userIds.Add(userId);
        Assert.StartsWith("AQAAAA", ReadPasswordHash(username));
        // ...and the upgraded row still authenticates.
        Assert.Equal(userId, _sut.ValidateUser(username, "not-hashed-yet"));
    }

    // ---- per-user isolation ----------------------------------------------

    [RequiresDatabaseFact]
    public void Entries_AreOnlyVisibleToTheirOwner()
    {
        int alice = NewUser("alice");
        int bob = NewUser("bob");

        _sut.AddMentalHealthEntry(NewEntry("alice-only-note"), alice);

        Assert.Contains(_sut.GetEntries(alice), e => e.Notes == "alice-only-note");
        Assert.DoesNotContain(_sut.GetEntries(bob), e => e.Notes == "alice-only-note");
    }

    [RequiresDatabaseFact]
    public void AnotherUser_CannotUpdateYourEntry()
    {
        int alice = NewUser("alice2");
        int bob = NewUser("bob2");

        _sut.AddMentalHealthEntry(NewEntry("original"), alice);
        MentalHealthEntry stored = _sut.GetEntries(alice).Single(e => e.Notes == "original");

        // Bob replays Alice's entry id but claims it as his own row.
        _sut.UpdateMentalHealthEntry(new MentalHealthEntry
        {
            EntryId = stored.EntryId,
            UserId = bob,
            Date = stored.Date,
            Mood = 1,
            Notes = "tampered",
            Triggers = "tampered",
        });

        MentalHealthEntry after = _sut.GetEntries(alice).Single(e => e.EntryId == stored.EntryId);
        Assert.Equal("original", after.Notes);
        Assert.Equal(stored.Mood, after.Mood);
    }

    [RequiresDatabaseFact]
    public void AnotherUser_CannotDeleteYourEntry()
    {
        int alice = NewUser("alice3");
        int bob = NewUser("bob3");

        _sut.AddMentalHealthEntry(NewEntry("do-not-delete"), alice);
        MentalHealthEntry stored = _sut.GetEntries(alice).Single(e => e.Notes == "do-not-delete");

        _sut.DeleteMentalHealthEntry(stored.EntryId, bob);

        Assert.Contains(_sut.GetEntries(alice), e => e.EntryId == stored.EntryId);
    }

    [RequiresDatabaseFact]
    public void Owner_CanUpdateAndDeleteTheirOwnEntry()
    {
        int alice = NewUser("alice4");
        _sut.AddMentalHealthEntry(NewEntry("mine"), alice);
        MentalHealthEntry stored = _sut.GetEntries(alice).Single(e => e.Notes == "mine");

        stored.Notes = "edited";
        stored.Mood = 5;
        _sut.UpdateMentalHealthEntry(stored);
        Assert.Equal("edited", _sut.GetEntries(alice).Single(e => e.EntryId == stored.EntryId).Notes);

        _sut.DeleteMentalHealthEntry(stored.EntryId, alice);
        Assert.DoesNotContain(_sut.GetEntries(alice), e => e.EntryId == stored.EntryId);
    }

    // ---- helpers ---------------------------------------------------------

    private static MentalHealthEntry NewEntry(string notes) => new()
    {
        Date = DateTime.Today,
        Mood = 3,
        Notes = notes,
        Triggers = "tests",
    };

    private string NewUsername(string prefix)
    {
        // Unique per run so repeated runs never collide, and <= 50 chars for nvarchar(50).
        string username = $"{prefix}_{Guid.NewGuid():N}"[..Math.Min(40, prefix.Length + 33)];
        _usernames.Add(username);
        return username;
    }

    private int NewUser(string prefix)
    {
        string username = NewUsername(prefix);
        Assert.True(_sut.RegisterUser(username, "CorrectHorse1!"));
        int id = _sut.ValidateUser(username, "CorrectHorse1!");
        Assert.True(id > 0);
        _userIds.Add(id);
        return id;
    }

    private void EnsureSchema()
    {
        const string sql = @"
IF OBJECT_ID('MentalHealthEntries') IS NULL
CREATE TABLE MentalHealthEntries
(
    EntryID INT IDENTITY(1,1) PRIMARY KEY,
    userID INT,
    Date datetime,
    Mood INT,
    Notes VARCHAR(MAX),
    Triggers VARCHAR(MAX)
);

IF OBJECT_ID('Users') IS NULL
CREATE TABLE Users
(
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username nvarchar(50) not null,
    PasswordHash nvarchar(max) not null
);";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        connection.Open();
        command.ExecuteNonQuery();
    }

    private string ReadPasswordHash(string username)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("SELECT PasswordHash FROM Users WHERE Username = @Username", connection);
        command.Parameters.AddWithValue("@Username", username);
        connection.Open();
        return (string)command.ExecuteScalar()!;
    }

    private void InsertLegacyPlaintextUser(string username, string plaintext)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash)", connection);
        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@PasswordHash", plaintext);
        connection.Open();
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            foreach (int userId in _userIds)
            {
                using var deleteEntries = new SqlCommand(
                    "DELETE FROM MentalHealthEntries WHERE userID = @UserId", connection);
                deleteEntries.Parameters.AddWithValue("@UserId", userId);
                deleteEntries.ExecuteNonQuery();
            }

            foreach (string username in _usernames)
            {
                using var deleteUser = new SqlCommand(
                    "DELETE FROM Users WHERE Username = @Username", connection);
                deleteUser.Parameters.AddWithValue("@Username", username);
                deleteUser.ExecuteNonQuery();
            }
        }
        catch
        {
            // Cleanup is best effort; a failure here must not mask the test result.
        }
    }
}
