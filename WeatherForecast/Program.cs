using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace WeatherForecast;

public class Program
{
  public static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables("WF_");
    
    // Add services to the container.
    builder.Services.AddAuthorization();
    var luckyNumberConfig = builder.Configuration.GetSection("LuckyNumbers");
    // builder.Services.AddHttpClient();
    var httpBuilder =  builder.Services.AddHttpClient("lucky", client => {
        client.BaseAddress = new Uri(luckyNumberConfig["Host"]!);
      });
    if (bool.TryParse(luckyNumberConfig["BypassCertificateVerification"], out var bypass) && bypass)
    {
      httpBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler() {
        ServerCertificateCustomValidationCallback = (message, certificate, chain, errors) => true  
      });
    }

    builder.Services.AddScoped<SqlConnection>(sp => {
      var config = sp.GetRequiredService<IConfiguration>();

      return new SqlConnection(config.GetConnectionString("Main")!);
    });

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

    var summaries = new[] {
      "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/weatherforecast",  async (
        HttpContext httpContext, 
        [FromServices] SqlConnection connection,
        [FromServices] IHttpClientFactory factory)=> {
        
        var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
          })
          .ToArray();
        
        var client = factory.CreateClient("lucky");
        var numberResponse = await client.GetAsync("luckynumber");
        var number = 0;
        int.TryParse(await numberResponse.Content.ReadAsStringAsync(), out number);

        var sqlInfo = (await connection.ExecuteScalarAsync<string>("Select @@version"))
          .Replace("\t", string.Empty)
          .Split("\n");
        
        return new { forecast, number, sqlInfo };
      })
      .WithName("GetWeatherForecast")
      .WithOpenApi();

    app.Run();
  }
}