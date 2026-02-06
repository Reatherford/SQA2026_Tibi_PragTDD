using Microsoft.AspNetCore.Mvc;
using AdamTibi.OpenWeather;
using Uqs.Weather.Wrappers;

namespace Uqs.Weather.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private const int FORECAST_DAYS = 5;
    // Injected OpenWeather client so unit tests can replace external API/network calls with a stub/fake.
    private readonly IClient _client;
    //Injected time wraper to avoid the DateTime.Now and enable deterministic, time-based unit tests
    private readonly INowWrapper _nowWrapper;
    //Injected random wrapper to avoid unpredictable values associated with Random.Shared and enable deterministic unit tests
    private readonly IRandomWrapper _randomWrapper;
    //Injected logger so unit tests can us NullLogger and avoid producing real log output.
    private readonly ILogger<WeatherForecastController> _logger;

    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public WeatherForecastController(ILogger<WeatherForecastController> logger,
        IClient client, INowWrapper nowWrapper, IRandomWrapper randomWrapper)
    {
        //Dependencies injected to improve testing and avoid hard-codeding 
        // 
        _logger = logger; //DI log
        _client = client;//DI API
        _nowWrapper = nowWrapper;//DI time
        _randomWrapper = randomWrapper; //DI randomness 
    }

    [HttpGet("ConvertCToF")]
    public double ConvertCToF(double c)
    {
        double f = c * (9d / 5d) + 32;
        _logger.LogInformation("conversion requested");
        return f;
    }

    [HttpGet("GetRealWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> GetReal()
    {
        const decimal GREENWICH_LAT = 51.4810m;
        const decimal GREENWICH_LON = 0.0052m;
        OneCallResponse res = await _client.OneCallAsync // External dependency abstracted behind IClient for testability.
            (GREENWICH_LAT, GREENWICH_LON, new[] {
                Excludes.Current, Excludes.Minutely,
                Excludes.Hourly, Excludes.Alerts }, Units.Metric);

        WeatherForecast[] wfs = new WeatherForecast[FORECAST_DAYS];
        for (int i = 0; i < wfs.Length; i++)
        {
            var wf = wfs[i] = new WeatherForecast();
            wf.Date = res.Daily[i + 1].Dt; 
            double forecastedTemp = res.Daily[i + 1].Temp.Day;
            wf.TemperatureC = (int)Math.Round(forecastedTemp);
            wf.Summary = MapFeelToTemp(wf.TemperatureC);
        }
        return wfs;
    }

    [HttpGet("GetRandomWeatherForecast")]
    public IEnumerable<WeatherForecast> GetRandom()
    {
        WeatherForecast[] wfs = new WeatherForecast[FORECAST_DAYS];
        for (int i = 0; i < wfs.Length; i++)
        {
            var wf = wfs[i] = new WeatherForecast();
            wf.Date = _nowWrapper.Now.AddDays(i + 1); //Wrapper enables deterministic time in unit tests by abstracting DateTime.Now behind INowWrapper
            wf.TemperatureC = _randomWrapper.Next(-20, 55); //Wrapper enables deterministic randomness by abstracting Random.Next behind IRandomWrapper
            wf.Summary = MapFeelToTemp(wf.TemperatureC);
        }
        return wfs;
    }

    private string MapFeelToTemp(int temperatureC)
    {
        if (temperatureC <= 0)
        {
            return Summaries.First();
        }
        int summariesIndex = (temperatureC / 5) + 1;
        if (summariesIndex >= Summaries.Length)
        {
            return Summaries.Last();
        }
        return Summaries[summariesIndex];
    }
}