using EventManagementSystem.Models;
using Microsoft.Data.SqlClient;

public class EventRepository
{
    private readonly string _connectionString = "Server=tcp:eventmanagementsystem.database.windows.net,1433;Initial Catalog=eventmanagementsystem;Persist Security Info=False;User ID=mk451gx;Password=eventManagementSystem1;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

    private static List<Event> _events = new List<Event>();

    public List<Event> GetAllEvents()
    {
        _events.Clear();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "SELECT Id, Name, Tags FROM Events";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _events.Add(new Event
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Tags = reader.GetString(2)
                        });
                    }
                }
            }
        }
        return _events;
    }

    public void CreateEvent(Event newEvent)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "INSERT INTO Events (Name, Tags) VALUES (@Name, @Tags)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Name", newEvent.Name);
                command.Parameters.AddWithValue("@Tags", newEvent.Tags);
                command.ExecuteNonQuery();
            }
        }
    }

    public void UpdateEvent(Event updatedEvent)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "UPDATE Events SET Name = @Name, Tags = @Tags WHERE Id = @Id";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", updatedEvent.Id);
                command.Parameters.AddWithValue("@Name", updatedEvent.Name);
                command.Parameters.AddWithValue("@Tags", updatedEvent.Tags);
                command.ExecuteNonQuery();
            }
        }
    }

    public void DeleteEvent(int eventId)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "DELETE FROM Events WHERE Id = @Id";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", eventId);
                command.ExecuteNonQuery();
            }
        }
    }

    public List<string> GetUserPreferences(int userId)
    {
        var preferences = new List<string>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "SELECT Preferences FROM Users WHERE Id = @UserId";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (!reader.IsDBNull(0)) // Check if the column is NULL
                        {
                            string prefString = reader.GetString(0); // Read preferences as a string
                            if (!string.IsNullOrEmpty(prefString))
                            {
                                preferences = prefString.Split(',').Select(p => p.Trim()).ToList();
                            }
                        }
                    }
                }
            }
        }
        return preferences;
    }


    // Add event to "My Events" list for a user
    public void AddToMyEvents(int userId, int eventId)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "INSERT INTO MyEvents (UserId, EventId) VALUES (@UserId, @EventId)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@EventId", eventId);
                command.ExecuteNonQuery();
            }
        }
    }


    // Get all events in "My Events" for a specific user
    public List<Event> GetMyEvents(int userId)
    {
        var myEvents = new List<Event>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = @"
                SELECT e.Id, e.Name, e.Tags
                FROM Events e
                INNER JOIN MyEvents me ON e.Id = me.EventId
                WHERE me.UserId = @UserId";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        myEvents.Add(new Event
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Tags = reader.GetString(2)
                        });
                    }
                }
            }
        }
        return myEvents;
    }

    // Remove event from "My Events"
    public void RemoveFromMyEvents(int userId, int eventId)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "DELETE FROM MyEvents WHERE UserId = @UserId AND EventId = @EventId";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@EventId", eventId);
                command.ExecuteNonQuery();
            }
        }
    }

    public Event GetEventById(int eventId)
    {
        return _events.FirstOrDefault(e => e.Id == eventId);
    }

    public void ClearCache()
    {
        _events.Clear();
    }
}