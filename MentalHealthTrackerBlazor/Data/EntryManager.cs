// EntryManager.cs

using MentalHealthTrackerBlazor.Data;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

public class EntryManager
{
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
                VALUES (@Username, @Password);";

            using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@Username", username);
                insertCmd.Parameters.AddWithValue("@Password", password); // Use HASHED password in a real app

                connection.Open();
                insertCmd.ExecuteNonQuery();
                return true;
            }
        }
    }
    // --- Validate User Login ---
    public int ValidateUser(string username, string password)
    {
        string sqlQuery = "SELECT UserId FROM Users WHERE Username = @Username AND PasswordHash = @Password";

        using (SqlConnection connection = new SqlConnection(_connectionString))
        using (SqlCommand command = new SqlCommand(sqlQuery, connection))
        {
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", password); // Use HASHED password in a real app

            connection.Open();
            object result = command.ExecuteScalar(); // Gets the first column of the first row

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result); // Returns the UserId if found
            }
        }
        return 0; // Returns 0 if login fails
    }

}

