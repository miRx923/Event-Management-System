using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using EventManagementSystem.Models;
using Microsoft.AspNetCore.Http;

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
        List<Event> events = _eventRepository.GetAllEvents();
        return View(events);
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
        var repository = new EventRepository();
        int userId = 1; // Example user ID, replace with actual logged-in user ID
        var userPreferences = repository.GetUserPreferences(userId); // Fetch user preferences
        var allEvents = repository.GetAllEvents(); // Fetch all events
        var myEvents = repository.GetMyEvents(userId); // Fetch "My Events"

        var user = new
        {
            name = "John", // Replace with actual username
            preferences = userPreferences
        };

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
        Console.WriteLine($"Response from Flask API: {jsonResponse}");

        var recommendedEventNames = JsonConvert.DeserializeObject<dynamic>(jsonResponse).recommendations.ToObject<List<string>>();

        // Match the recommended names with the full event details
        var recommendedEvents = allEvents
            .Where(e => recommendedEventNames.Contains(e.Name))
            .ToList();

        // Pass the IDs of "My Events" to the view for comparison
        var myEventIds = myEvents.Select(e => e.Id).ToList();

        ViewBag.Recommendations = recommendedEvents; // Pass recommendations to the view
        ViewBag.MyEventIds = myEventIds; // Pass "My Events" IDs to the view

        return View();
    }

    public IActionResult Loginpage()
    {
        return View();
    }

    public IActionResult Registration()
    {
        return View();
    }


    [HttpPost]
    public IActionResult AddToMyEvents(int eventId)
    {
        int userId = 1; // Example user ID, replace with actual user context
        var repository = new EventRepository();
        repository.AddToMyEvents(userId, eventId);

        return RedirectToAction("Recommendations");
    }

    public IActionResult MyEvents()
    {
        int userId = 1; // Example user ID, replace with actual user context
        var repository = new EventRepository();
        var myEvents = repository.GetMyEvents(userId);

        return View(myEvents);
    }

    [HttpPost]
    public IActionResult RemoveFromMyEvents(int eventId)
    {
        int userId = 1; // Replace with the actual logged-in user ID
        var repository = new EventRepository();
        repository.RemoveFromMyEvents(userId, eventId); // Call the repository method to delete the event
        return RedirectToAction("MyEvents"); // Redirect back to "My Events"
    }
}