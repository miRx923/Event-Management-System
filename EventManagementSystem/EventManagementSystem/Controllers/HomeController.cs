using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Recommendations()
    {
        // Sample user and event data
        var user = new { name = "John", preferences = new[] { "music", "sports" } };
        var events = new[]
        {
            new { name = "Rock Concert", tags = new[] { "music", "live" } },
            new { name = "Football Match", tags = new[] { "sports", "outdoor" } },
            new { name = "Cooking Class", tags = new[] { "cooking", "indoor" } }
        };

        // Prepare JSON input for Python
        var inputData = new { user, events };
        var jsonInput = JsonConvert.SerializeObject(inputData);

        // Call the Python script
        string pythonExe = @"path\to\python.exe"; // Update this path
        string pythonScript = @"path\to\Python\algorithm.py"; // Update this path
        string jsonOutput;

        using (var process = new Process())
        {
            process.StartInfo.FileName = pythonExe;
            process.StartInfo.Arguments = pythonScript;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            using (var writer = process.StandardInput)
            {
                if (writer.BaseStream.CanWrite)
                {
                    writer.Write(jsonInput);
                }
            }

            jsonOutput = process.StandardOutput.ReadToEnd();
        }

        // Parse Python's JSON output
        var recommendations = JsonConvert.DeserializeObject<dynamic>(jsonOutput);

        // Pass recommendations to the view
        ViewBag.Recommendations = recommendations.recommendations;
        return View();
    }
}
