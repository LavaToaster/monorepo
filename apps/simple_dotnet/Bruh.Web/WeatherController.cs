using Bruh.Lib;
using Microsoft.AspNetCore.Mvc;

namespace Bruh.Web;

[ApiController]
public class WeatherController
{
    private readonly string[] _summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    
    [HttpGet("/weatherforecast")]
    public WeatherForecast[] WeatherForecast()
    {
        var forecast =  Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    _summaries[Random.Shared.Next(_summaries.Length)]
                ))
            .ToArray();
        return forecast;
    }
    
    [HttpGet("/message")]
    public string GetMessage()
    {
        return Class1.GetMessage();
    }
}