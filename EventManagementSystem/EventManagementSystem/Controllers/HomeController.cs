using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using EventManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Org.BouncyCastle.Crypto.Generators;
using Microsoft.AspNetCore.Identity;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.SqlServer.Server;


public class HomeController : Controller
{
    private readonly EventRepository _eventRepository = new EventRepository();
    private readonly HttpClient _httpClient;

    public HomeController()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5001") // Update with Flask API URL
        };
    }

    public IActionResult Index()
    {
        if (!HttpContext.Session.TryGetValue("UserId", out _))
        {
            return RedirectToAction("LoginPage"); // Redirect to login if not logged in
        }

        List<Event> events = _eventRepository.GetAllEvents();
        return View(events);
    }

    [HttpPost]
    public IActionResult Register(string username, string email, string password, string confirmPassword)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.ErrorMessage = "All fields are required.";
            ViewBag.Username = username;
            ViewBag.Email = email;
            return View("Registration");
        }

        // Validate password match
        if (password != confirmPassword)
        {
            ViewBag.ErrorMessage = "Passwords do not match.";
            ViewBag.Username = username;
            ViewBag.Email = email;
            return View("Registration");
        }

        // Check if username or email already exists
        var existingUserByUsername = _eventRepository.GetUserByUsername(username);
        var existingUserByEmail = _eventRepository.GetUserByEmail(email);

        if (existingUserByUsername != null)
        {
            ViewBag.ErrorMessage = "Username already exists.";
            ViewBag.Username = username;
            ViewBag.Email = email;
            return View("Registration");
        }

        if (existingUserByEmail != null)
        {
            ViewBag.ErrorMessage = "Email already exists.";
            ViewBag.Username = username;
            ViewBag.Email = email;
            return View("Registration");
        }

        // Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Username = username,
            Password = passwordHash,
            Email = email
        };

        try
        {
            _eventRepository.RegisterUser(user);

            // Fetch the user ID for session and redirect to Preferences
            var registeredUser = _eventRepository.GetUserByUsername(username);
            if (registeredUser != null)
            {
                HttpContext.Session.SetInt32("UserId", registeredUser.Id);
                HttpContext.Session.SetString("Username", registeredUser.Username);
            }

            return RedirectToAction("SelectPreferences"); // Redirect to preferences page
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = "Registration failed due to an unexpected error.";
            return View("Registration");
        }
    }


    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Fetch user by username
        var user = _eventRepository.GetUserByUsername(username);
        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            // If credentials match, store user information in session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);

            return RedirectToAction("Index"); // Redirect to the main page
        }

        // Show error message if login fails
        ViewBag.ErrorMessage = "Invalid username or password.";
        return View("LoginPage");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear(); // Clear session
        return RedirectToAction("LoginPage");
    }

    public IActionResult SelectPreferences()
    {
        var availableTags = _eventRepository.GetAllTags(); // Fetch all tags from the database
        return View(availableTags);
    }

    [HttpPost]
    public IActionResult SavePreferences(List<string> selectedTags)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("LoginPage");
        }

        _eventRepository.SaveUserPreferences(userId.Value, selectedTags);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult CreateEvent(Event newEvent)
    {
        _eventRepository.CreateEvent(newEvent);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult UpdateEvent(Event updatedEvent)
    {
        _eventRepository.UpdateEvent(updatedEvent);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteEvent(int id)
    {
        _eventRepository.DeleteEvent(id);
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Recommendations()
    {
        if (!HttpContext.Session.TryGetValue("UserId", out _))
        {
            ViewBag.ErrorMessage = "You need to log in to access recommendations.";
            return View(); // Render the view with a message
        }

        // Proceed with fetching recommendations as before
        var repository = new EventRepository();
        int userId = HttpContext.Session.GetInt32("UserId").Value;
        var userPreferences = repository.GetUserPreferences(userId);
        var allEvents = repository.GetAllEvents();
        var myEvents = repository.GetMyEvents(userId);

        var user = new
        {
            name = HttpContext.Session.GetString("Username"), // Get actual username
            preferences = userPreferences
        };

        ViewBag.Username = user.name;
        ViewBag.Tags = user.preferences;

        var inputData = new
        {
            user,
            events = allEvents.Select(e => new { name = e.Name, tags = e.Tags.Split(',') })
        };

        var jsonInput = JsonConvert.SerializeObject(inputData);

        var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/recommend", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to fetch recommendations from Flask API");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var recommendedEventNames = JsonConvert.DeserializeObject<dynamic>(jsonResponse).recommendations.ToObject<List<string>>();

        var recommendedEvents = allEvents
            .Where(e => recommendedEventNames.Contains(e.Name))
            .ToList();

        var myEventIds = myEvents.Select(e => e.Id).ToList();

        ViewBag.Recommendations = recommendedEvents;
        ViewBag.MyEventIds = myEventIds;

        return View();
    }

    [HttpPost]
    public JsonResult AddPreference([FromBody] dynamic data)
    {
        int userId = data.userId;
        string preferenceName = data.preferenceName;

        try
        {
            var repository = new EventRepository();
            var currentPreferences = repository.GetUserPreferences(userId);

            if (!currentPreferences.Contains(preferenceName))
            {
                currentPreferences.Add(preferenceName);
                repository.SaveUserPreferences(userId, currentPreferences);
            }

            return Json(new { success = true, preferences = currentPreferences });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    public IActionResult ProfilePage()
    {
        if (!HttpContext.Session.TryGetValue("UserId", out _))
        {
            ViewBag.ErrorMessage = "You need to log in to access recommendations.";
            return View(); // Render the view with a message
        }

        var repository = new EventRepository();
        int userId = HttpContext.Session.GetInt32("UserId").Value;
        var userPreferences = repository.GetUserPreferences(userId);
        var AllTags = repository.GetAllTags();

        var user = new
        {
            name = HttpContext.Session.GetString("Username"), // Get actual username
            preferences = userPreferences
        };

        if (!HttpContext.Session.TryGetValue("UserId", out _))
        {
            return RedirectToAction("LoginPage"); // Redirect to login if the user is not logged in
        }

        if (user == null)
        {
            ViewBag.ErrorMessage = "User not found.";
            return RedirectToAction("Index"); // Redirect to the home page if user not found
        }

        ViewBag.Preferences = user.preferences;
        ViewBag.AllPrefernces = AllTags;
        ViewBag.UserID = userId;
        return View();
    }

    public IActionResult MyEvents()
    {
        if (!HttpContext.Session.TryGetValue("UserId", out _))
        {
            ViewBag.ErrorMessage = "You need to log in to access your events.";
            return View(new List<Event>()); // Render empty list with message
        }

        int userId = HttpContext.Session.GetInt32("UserId").Value;
        var repository = new EventRepository();
        var myEvents = repository.GetMyEvents(userId);

        return View(myEvents);
    }


    [HttpPost]
    public IActionResult AddToMyEvents(int eventId)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            // Redirect to login if no user is logged in
            return RedirectToAction("LoginPage");
        }

        var repository = new EventRepository();
        try
        {
            repository.AddToMyEvents(userId.Value, eventId);
            return RedirectToAction("Recommendations");
        }
        catch (Exception ex)
        {
            // Log the error for debugging
            Console.WriteLine($"Error adding event to My Events: {ex.Message}");
            ViewBag.ErrorMessage = "An error occurred while adding the event. Please try again.";
            return RedirectToAction("Recommendations");
        }
    }


    [HttpPost]
    public IActionResult RemoveFromMyEvents(int eventId)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            // Redirect to login if no user is logged in
            return RedirectToAction("LoginPage");
        }

        try
        {
            var repository = new EventRepository();
            repository.RemoveFromMyEvents(userId.Value, eventId); // Call repository method to remove the event
            return RedirectToAction("MyEvents"); // Redirect back to My Events page
        }
        catch (Exception ex)
        {
            // Log the error for debugging
            Console.WriteLine($"Error removing event from My Events: {ex.Message}");
            ViewBag.ErrorMessage = "An error occurred while removing the event. Please try again.";
            return RedirectToAction("MyEvents");
        }
    }

    public IActionResult Loginpage()
    {
        return View();
    }

    public IActionResult Registration()
    {
        return View();
    }
}