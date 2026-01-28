using System;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using SimpleMcpServer.Prompts;
using SimpleMcpServer.Resources;
using SimpleMcpServer.Services;
using SimpleMcpServer.Tools;

namespace SimpleMcpServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureServices(builder);

            var app = builder.Build();

            app.MapMcp();

            app.Run();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<AuthService>();

            builder.Services.AddMcpServer()
              .WithHttpTransport()
              .WithPrompts<CodeReviewPrompt>()
              .WithTools<EchoTool>()
              .WithTools<SampleLlmTool>()
              .WithTools<WeatherTools>()
              .WithResources<SimpleResource>();

            builder.Services.AddHttpClient("WeatherApi", client =>
            {
                client.BaseAddress = new Uri("https://api.weather.gov");
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
            });
        }
    }
}
