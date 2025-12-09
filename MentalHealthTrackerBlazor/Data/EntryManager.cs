using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using MentalHealthTrackerBlazor.Data;

namespace MentalHealthTrackerBlazor.Data
{

    public class EntryManager
    {
        private readonly string _connectionString;

        public EntryManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<MentalHealthEntry> GetEntries()
        {
            var entries = new List<MentalHealthEntry>();
            string query = "SELECT EntryId, Date, Mood, Notes, Triggers FROM MentalHealthEntries ORDER BY Date DESC"; //where userID = @UserId 

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(new MentalHealthEntry
                        {
                            //UserId = (int)reader["UserId"],
                            EntryId = (int)reader["EntryId"],
                            Date = (DateTime)reader["Date"],
                            Mood = (int)reader["Mood"],
                            Notes = reader["Notes"] as string,
                            Triggers = reader["Triggers"] as string
                        });
                    }
                }
            }
            return entries;
        }
        public void AddMentalHealthEntry(MentalHealthEntry entry)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "INSERT INTO MentalHealthEntries (UserId,Date,Mood,Notes,Triggers) VALUES (@UserId,@Date,@Mood,@Notes,@Triggers)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", entry.UserId);
                    command.Parameters.AddWithValue("@Date", entry.Date);
                    command.Parameters.AddWithValue("@Mood", entry.Mood);
                    command.Parameters.AddWithValue("@Notes", entry.Notes);
                    command.Parameters.AddWithValue("@Triggers", entry.Triggers);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
          

        // Add methods for UpdateEntry, DeleteEntry similarly...
    }
}

