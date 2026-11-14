// EntryManager.cs

using MentalHealthTrackerBlazor.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

public class EntryManager
{
    // PasswordHasher<TUser> is salted PBKDF2-HMAC-SHA512 (210,000 iterations by default) and needs
    // no user object, so a shared instance is enough.
    private static readonly IPasswordHasher<object> PasswordHasher = new PasswordHasher<object>();
    private static readonly object HasherUser = new();

    private readonly string _connectionString;

    public EntryManager(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("DefaultConnection not found in configuration.");
    }

    public EntryManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    // --- ADD Entry (INSERT) ---
    public void AddMentalHealthEntry(MentalHealthEntry newEntry, int userId)
    {
        newEntry.UserId = userId;

        string sqlQuery = @"
            INSERT INTO MentalHealthEntries (UserId, Date, Mood, Notes, Triggers)
            VALUES (@UserId, @Date, @Mood, @Notes, @Triggers);
        ";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sqlQuery, connection);
        command.Parameters.AddWithValue("@UserId", newEntry.UserId);
        command.Parameters.AddWithValue("@Date", newEntry.Date);
        command.Parameters.AddWithValue("@Mood", newEntry.Mood);
        command.Parameters.AddWithValue("@Notes", newEntry.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Triggers", newEntry.Triggers ?? (object)DBNull.Value);

        connection.Open();
        command.ExecuteNonQuery();
    }

    // --- GET Entries (SELECT filtered by UserId) ---
    public List<MentalHealthEntry> GetEntries(int userId)
    {
        List<MentalHealthEntry> entries = new List<MentalHealthEntry>();
        string sqlQuery = "SELECT EntryId, UserId, Date, Mood, Notes, Triggers FROM MentalHealthEntries WHERE UserId = @UserId ORDER BY Date DESC";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sqlQuery, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        connection.Open();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new MentalHealthEntry
            {
                EntryId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Date = reader.GetDateTime(2),
                Mood = reader.GetInt32(3),
                Notes = reader.GetString(4),
                Triggers = reader.GetString(5)
            });
        }
        return entries;
    }

    // --- UPDATE Entry (UPDATE filtered by EntryId AND UserId) ---
    public void UpdateMentalHealthEntry(MentalHealthEntry updatedEntry)
    {
        string sqlQuery = @"
            UPDATE MentalHealthEntries
            SET Date = @Date, Mood = @Mood, Notes = @Notes, Triggers = @Triggers
            WHERE EntryId = @EntryId AND UserId = @UserId;
        ";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sqlQuery, connection);
        command.Parameters.AddWithValue("@Date", updatedEntry.Date);
        command.Parameters.AddWithValue("@Mood", updatedEntry.Mood);
        command.Parameters.AddWithValue("@Notes", updatedEntry.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Triggers", updatedEntry.Triggers ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@EntryId", updatedEntry.EntryId);
        command.Parameters.AddWithValue("@UserId", updatedEntry.UserId);

        connection.Open();
        command.ExecuteNonQuery();
    }

    // --- DELETE Entry (DELETE filtered by EntryId AND UserId) ---
    public void DeleteMentalHealthEntry(int entryID, int userId)
    {
        string sqlQuery = "DELETE FROM MentalHealthEntries WHERE EntryId = @EntryID AND UserId = @UserId";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sqlQuery, connection);
        command.Parameters.AddWithValue("@EntryID", entryID);
        command.Parameters.AddWithValue("@UserId", userId);

        connection.Open();
        command.ExecuteNonQuery();
    }
    // --- Sign Up New User ---
    public bool RegisterUser(string username, string password)
    {
        username = username?.Trim() ?? string.Empty;

        // Username column is nvarchar(50); keep oversized input out of the database.
        if (username.Length == 0 || username.Length > 50 || string.IsNullOrEmpty(password) || password.Length > 256)
        {
            return false;
        }

        // The password is hashed before the database is touched, so the plaintext is never stored.
        string passwordHash = PasswordHasher.HashPassword(HasherUser, password);

        // First, check if the user already exists
        string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
        using (SqlConnection connection = new SqlConnection(_connectionString))
        using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
        {
            checkCmd.Parameters.AddWithValue("@Username", username);
            connection.Open();
            int userCount = (int)checkCmd.ExecuteScalar();

            if (userCount > 0)
            {
                return false; // User already exists
            }
            // Close the connection for the check
            connection.Close();

            // Insert the new user
            string insertQuery = @"
                INSERT INTO Users (Username, PasswordHash) 
                VALUES (@Username, @PasswordHash);";

            using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@Username", username);
                insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                connection.Open();
                insertCmd.ExecuteNonQuery();
                return true;
            }
        }
    }
    // --- Validate User Login ---
    public int ValidateUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return 0;
        }

        string sqlQuery = "SELECT TOP 1 UserId, PasswordHash FROM Users WHERE Username = @Username ORDER BY UserId";

        using (SqlConnection connection = new SqlConnection(_connectionString))
        using (SqlCommand command = new SqlCommand(sqlQuery, connection))
        {
            command.Parameters.AddWithValue("@Username", username);

            connection.Open();

            int userId;
            string storedValue;

            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return 0; // Returns 0 if login fails
                }

                userId = reader.GetInt32(0);
                storedValue = reader.GetString(1);
            }

            if (IsHashedPassword(storedValue))
            {
                var verification = PasswordHasher.VerifyHashedPassword(HasherUser, storedValue, password);

                if (verification == PasswordVerificationResult.Failed)
                {
                    return 0;
                }

                if (verification == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    UpdatePasswordHash(connection, userId, PasswordHasher.HashPassword(HasherUser, password));
                }

                return userId;
            }

            // Rows created before hashing was introduced still hold the plaintext password.
            // Compare in constant time, then upgrade the row so it is never read again.
            if (!FixedTimeEquals(storedValue, password))
            {
                return 0;
            }

            UpdatePasswordHash(connection, userId, PasswordHasher.HashPassword(HasherUser, password));
            return userId;
        }
    }

    // ASP.NET Core Identity hashes are Base64 blobs whose first byte (the format marker) is 0x01,
    // which always encodes to the prefix "AQAAAA".
    private static bool IsHashedPassword(string storedValue) =>
        storedValue.StartsWith("AQAAAA", StringComparison.Ordinal);

    private static void UpdatePasswordHash(SqlConnection connection, int userId, string passwordHash)
    {
        const string sqlQuery = "UPDATE Users SET PasswordHash = @PasswordHash WHERE UserId = @UserId";

        using var command = new SqlCommand(sqlQuery, connection);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        command.Parameters.AddWithValue("@UserId", userId);
        command.ExecuteNonQuery();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
               && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

}


