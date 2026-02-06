namespace Uqs.Weather.Tests.Unit;
using Microsoft.Extensions.Logging.Abstractions;
using Uqs.Weather.Controllers;
using Uqs.Weather.Wrappers;
using AdamTibi.OpenWeather;
using Xunit;

/*
SUT: WeatherForecastController
Behavior under test: ConvertCToF temperature conversion.
*/
public class WeatherForecastControllerTests
{
    [Fact]
    public void ConvertCToF_ZeroC_Returns32F()
    {
        // Arrange
        // Using NullLogger so the test does not write actual log output.
        var logger = NullLogger<WeatherForecastController>.Instance;
        var client = new ClientStub();
        var nowWrapper = new NowWrapper();
        var randomWrapper = new RandomWrapper();

        var controller = new WeatherForecastController(
            logger, client, nowWrapper, randomWrapper);

        // Act
        double result = controller.ConvertCToF(0);

        // Assert
        Assert.Equal(32.0, result, 5);
    }

    [Fact]
    public void ConvertCToF_NegativeOneC_Returns30Point2F()
    {
        // Arrange
        // Using NullLogger so the test does not write actual log output.
        var logger = NullLogger<WeatherForecastController>.Instance;
        var client = new ClientStub();
        var nowWrapper = new NowWrapper();
        var randomWrapper = new RandomWrapper();

        var controller = new WeatherForecastController(
            logger, client, nowWrapper, randomWrapper);

        // Act
        double result = controller.ConvertCToF(-1);

        // Assert
        Assert.Equal(30.2, result, 5);
    }

    [Theory]
    // InlineData: Celsius input, expected Fahrenheit output
    [InlineData(100, 212)]   // boiling point
    [InlineData(37, 98.6)]   // body temperature
    [InlineData(20, 68)]     // room temperature
    [InlineData(10, 50)]     // cool day
    [InlineData(-40, -40)]   // crossover point
    public void ConvertCToF_VariousInputs_ReturnsExpectedF(
        double celsius, double expectedFahrenheit)
    {
        // Arrange
        // Using NullLogger so the test does not write actual log output.
        var logger = NullLogger<WeatherForecastController>.Instance;
        var client = new ClientStub();
        var nowWrapper = new NowWrapper();
        var randomWrapper = new RandomWrapper();

        var controller = new WeatherForecastController(
            logger, client, nowWrapper, randomWrapper);

        // Act
        double result = controller.ConvertCToF(celsius);

        // Assert
        Assert.Equal(expectedFahrenheit, result, 5);
    }
}
