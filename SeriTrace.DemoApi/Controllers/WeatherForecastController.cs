using Microsoft.AspNetCore.Mvc;

namespace SeriTrace.DemoApi.Controllers;

/// <summary>
/// Controller providing sample weather forecast data.
/// Demonstrates a simple API endpoint returning randomized weather data for demo purposes.
/// </summary>
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    // Predefined weather summary descriptions
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherForecastController"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and request logging.</param>
    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    // Change the return type from IActionResult<IEnumerable<WeatherForecast>> to ActionResult<IEnumerable<WeatherForecast>>
    [HttpGet(Name = "GetWeatherForecast")]
    public ActionResult<IEnumerable<WeatherForecast>> Get()
    {
        // Generate and return 5 random weather forecasts
        return Ok(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray());
    }
}
