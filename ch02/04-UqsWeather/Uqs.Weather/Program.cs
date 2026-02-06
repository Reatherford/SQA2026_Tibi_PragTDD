using AdamTibi.OpenWeather;
using Uqs.Weather;
using Uqs.Weather.Wrappers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Singleton: IClient is treated as a reusable service; in tests it can be replaced with a stub/fake to avoid real network calls.
builder.Services.AddSingleton<IClient>(_ => {
    bool isLoad = builder.Configuration.GetValue("LoadTest:IsActive", false);
    if (isLoad) return new ClientStub();
    else
    {
        string apiKey = builder.Configuration["OpenWeather:Key"]
            ?? throw new InvalidOperationException("Missing configuration value: OpenWeather:Key");
        HttpClient httpClient = new HttpClient();
        return new Client(apiKey, httpClient);
    }
});
builder.Services.AddSingleton<INowWrapper>(_ => new NowWrapper()); // Singleton: NowWrapper is stateless/thread-safe; wrapping time enables deterministic unit tests by swapping INowWrapper with a fake.
builder.Services.AddTransient<IRandomWrapper>(_ => new RandomWrapper()); // Transient: avoids shared mutable/random state across requests and keeps tests isolated by allowing per-test fake randomness.


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
