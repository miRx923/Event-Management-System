using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public class HomeController : Controller
{
    private readonly HttpClient _httpClient;

    public HomeController()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5001") // Update with Flask API URL
        };
    }

    public async Task<IActionResult> Recommendations()
    {
        var user = new { name = "John", preferences = new[] { "music", "sports" } };
        var events = new[]
        {
            new { name = "Rock Concert", tags = new[] { "music", "live" } },
            new { name = "Football Match", tags = new[] { "sports", "outdoor" } },
            new { name = "Cooking Class", tags = new[] { "cooking", "indoor" } }
        };

        var inputData = new { user, events };
        var jsonInput = JsonConvert.SerializeObject(inputData);

        var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/recommend", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to fetch recommendations from Flask API");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var recommendations = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

        ViewBag.Recommendations = recommendations.recommendations;
        return View();
    }
}