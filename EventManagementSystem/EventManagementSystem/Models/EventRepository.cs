using EventManagementSystem.Models;
using Microsoft.Data.SqlClient;

public class EventRepository
{
    private readonly string _connectionString = "Server=tcp:eventmanagementsystem.database.windows.net,1433;Initial Catalog=eventmanagementsystem;Persist Security Info=False;User ID=mk451gx;Password=eventManagementSystem1;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

    private static List<Event> _events = new List<Event>();

    public void RegisterUser(User user)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "INSERT INTO Users (Username, Password, Email, Preferences, Role) VALUES (@Username, @Password, @Email, @Preferences, @Role)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@Password", user.Password);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@Preferences", (object)user.Preferences ?? DBNull.Value);
                command.Parameters.AddWithValue("@Role", user.Role ?? "User"); // Default role is "User"
                command.ExecuteNonQuery();
            }
        }
    }

    public User GetUserByUsername(string username)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "SELECT Id, Username, Password, Email, Preferences, Role FROM Users WHERE Username = @Username";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Password = reader.GetString(2),
                            Email = reader.GetString(3),
                            Preferences = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Role = reader.GetString(5) // Fetch the role
                        };
                    }
                }
            }
        }
        return null;
    }

    public User GetUserByEmail(string email)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "SELECT Id, Username, Password, Email, Preferences FROM Users WHERE Email = @Email";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Password = reader.GetString(2),
                            Email = reader.GetString(3),
                            Preferences = reader.IsDBNull(4) ? null : reader.GetString(4)
                        };
                    }
                }
            }
        }
        return null;
    }

    public void SaveUserPreferences(int userId, List<string> preferences)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string prefString = string.Join(",", preferences);

            string query = "UPDATE Users SET Preferences = @Preferences WHERE Id = @UserId";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Preferences", prefString);
                command.Parameters.AddWithValue("@UserId", userId);
                command.ExecuteNonQuery();
            }
        }
    }

    public List<User> GetAllUsers()
    {
        var users = new List<User>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "SELECT Id, Username, Email, Role FROM Users";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Email = reader.GetString(2),
                            Role = reader.GetString(3)
                        });
                    }
                }
            }
        }
        return users;
    }


    public List<string> GetAllTags()
    {
        var tags = new List<string>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string query = "SELECT DISTINCT Tags FROM Events";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var eventTags = reader.GetString(0).Split(',').Select(t => t.Trim());
                        tags.AddRange(eventTags);
                    }
                }
            }
        }
        return tags.Distinct().ToList();
    }

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

            // First, delete from MyEvents
            string deleteFromMyEventsQuery = "DELETE FROM MyEvents WHERE EventId = @EventId";
            using (SqlCommand myEventsCommand = new SqlCommand(deleteFromMyEventsQuery, connection))
            {
                myEventsCommand.Parameters.AddWithValue("@EventId", eventId);
                myEventsCommand.ExecuteNonQuery();
            }

            // Then, delete from Events
            string deleteFromEventsQuery = "DELETE FROM Events WHERE Id = @EventId";
            using (SqlCommand eventsCommand = new SqlCommand(deleteFromEventsQuery, connection))
            {
                eventsCommand.Parameters.AddWithValue("@EventId", eventId);
                eventsCommand.ExecuteNonQuery();
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

            // Check if UserId and EventId exist
            string userCheckQuery = "SELECT COUNT(*) FROM Users WHERE Id = @UserId";
            string eventCheckQuery = "SELECT COUNT(*) FROM Events WHERE Id = @EventId";

            using (SqlCommand userCommand = new SqlCommand(userCheckQuery, connection))
            using (SqlCommand eventCommand = new SqlCommand(eventCheckQuery, connection))
            {
                userCommand.Parameters.AddWithValue("@UserId", userId);
                eventCommand.Parameters.AddWithValue("@EventId", eventId);

                int userExists = (int)userCommand.ExecuteScalar();
                int eventExists = (int)eventCommand.ExecuteScalar();

                if (userExists == 0 || eventExists == 0)
                {
                    throw new Exception("Invalid UserId or EventId.");
                }
            }

            // Insert into MyEvents
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