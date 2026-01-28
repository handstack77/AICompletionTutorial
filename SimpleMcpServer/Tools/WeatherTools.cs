using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SimpleMcpServer.Tools
{
    [McpServerToolType]
    public class WeatherTools
    {
        private readonly IHttpClientFactory httpClientFactory;

        public WeatherTools(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        [McpServerTool, Description("한국 기상 특보를 확인합니다.")]
        [McpMeta("category", "weather")]
        [McpMeta("dataSource", "weather.gov")]
        public async Task<string> GetAlerts([Description("기상 정보를 받을 한국의 지역을 입력하세요. 지역의 약어 2자리를 사용하세요 (예: 서울,경기,제주).")] string state)
        {
            var client = httpClientFactory.CreateClient("WeatherApi");
            using var responseStream = await client.GetStreamAsync($"/alerts/active/area/{state}");
            using var jsonDocument = await JsonDocument.ParseAsync(responseStream)
                ?? throw new McpException("알림(alerts) 엔드포인트에서 JSON이 반환되지 않았습니다.");

            var alerts = jsonDocument.RootElement.GetProperty("features").EnumerateArray();

            if (!alerts.Any())
            {
                return "이 지역에 대한 활성 알림이 없습니다.";
            }

            return string.Join("\n--\n", alerts.Select(alert =>
            {
                JsonElement properties = alert.GetProperty("properties");
                return $"""
                    Event: {properties.GetProperty("event").GetString()}
                    Area: {properties.GetProperty("areaDesc").GetString()}
                    Severity: {properties.GetProperty("severity").GetString()}
                    Description: {properties.GetProperty("description").GetString()}
                    Instruction: {properties.GetProperty("instruction").GetString()}
                    """;
            }));
        }

        [McpServerTool, Description("위치에 대한 일기 예보를 가져옵니다.")]
        [McpMeta("category", "weather")]
        [McpMeta("recommendedModel", "gpt-4")]
        public async Task<string> GetForecast([Description("위치의 위도입니다.")] double latitude, [Description("위치의 경도입니다.")] double longitude)
        {
            var client = httpClientFactory.CreateClient("WeatherApi");
            var pointUrl = string.Create(CultureInfo.InvariantCulture, $"/points/{latitude},{longitude}");

            using var locationResponseStream = await client.GetStreamAsync(pointUrl);
            using var locationDocument = await JsonDocument.ParseAsync(locationResponseStream);
            var forecastUrl = locationDocument?.RootElement.GetProperty("properties").GetProperty("forecast").GetString()
                ?? throw new McpException($"다음 주소에서 예보(forecast) URL이 제공되지 않았습니다: {client.BaseAddress}points/{latitude},{longitude}");

            using var forecastResponseStream = await client.GetStreamAsync(forecastUrl);
            using var forecastDocument = await JsonDocument.ParseAsync(forecastResponseStream);
            var periods = forecastDocument?.RootElement.GetProperty("properties").GetProperty("periods").EnumerateArray()
                ?? throw new McpException("예보(forecast) 엔드포인트에서 JSON이 반환되지 않았습니다.");

            return string.Join("\n---\n", periods.Select(period => $"""
                {period.GetProperty("name").GetString()}
                온도: {period.GetProperty("temperature").GetInt32()}°F
                바람: {period.GetProperty("windSpeed").GetString()} {period.GetProperty("windDirection").GetString()}
                예보: {period.GetProperty("detailedForecast").GetString()}
                """));
        }
    }
}