using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsoleApp4
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Kernel.CreateBuilder();

            builder.AddGoogleAIGeminiChatCompletion("gemini-3-flash-preview", (Environment.GetEnvironmentVariable("GOOGLE_GEMINI_API_KEY") ?? "AIzaSyBWr36KqyBkqXWl574g3f4IDW-4Bqe7jHo"));

            builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));

            Kernel kernel = builder.Build();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            kernel.Plugins.AddFromType<LightsPlugin>("Lights");

            await AddOpenApiPluginAsync(kernel);
            await AddLocalMcpPluginAsync(kernel, "LocalFileSystem", "npx", "-y @modelcontextprotocol/server-filesystem /");
            await AddRemoteMcpPluginAsync(kernel, "RemoteTools", new Uri("http://localhost:3000/sse"));

            PromptExecutionSettings executionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var history = new ChatHistory("당신은 다양한 도구(OpenAPI, MCP)를 사용할 수 있는 AI 비서입니다.");

            Console.WriteLine(">>> 대화를 시작합니다. (종료: exit)");

            while (true)
            {
                Console.Write("\n[User]: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input == "exit") break;

                history.AddUserMessage(input);

                try
                {
                    var response = await chatService.GetChatMessageContentAsync(history, executionSettings, kernel);
                    history.AddMessage(response.Role, response.Content ?? string.Empty);
                    Console.WriteLine($"[Assistant]: {response.Content}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error]: {ex.Message}");
                }
            }
        }

        static async Task AddOpenApiPluginAsync(Kernel kernel)
        {
            try
            {
                // OpenAPI 프로토콜을 호스팅하는 서버의 Swagger JSON 엔드포인트
                var uri = new Uri("http://localhost:5000/swagger/v1/swagger.json");
                await kernel.ImportPluginFromOpenApiAsync("WeatherApi", uri);
                Console.WriteLine("[System] OpenAPI 플러그인 등록 시도 완료.");
            }
            catch
            {
                Console.WriteLine("[System] OpenAPI 서버 연결 실패 (무시하고 진행)");
            }
        }

        static async Task AddLocalMcpPluginAsync(Kernel kernel, string pluginName, string command, string arguments)
        {
            try
            {
                var transport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Command = command,
                    Arguments = arguments.Split(' ').ToList()
                });

                var client = await McpClient.CreateAsync(transport);

                await McpPluginAdapter.RegisterMcpToolsAsPlugin(kernel, client, pluginName);

                Console.WriteLine($"[System] 로컬 MCP 플러그인 '{pluginName}' 등록 완료.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[System] 로컬 MCP 서버 '{pluginName}' 연결 실패: {ex.Message}");
            }
        }

        static async Task AddRemoteMcpPluginAsync(Kernel kernel, string pluginName, Uri endpoint)
        {
            try
            {
                var transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = endpoint
                });

                var client = await McpClient.CreateAsync(transport);

                await McpPluginAdapter.RegisterMcpToolsAsPlugin(kernel, client, pluginName);

                Console.WriteLine($"[System] 원격 MCP 플러그인 '{pluginName}' 등록 완료.");
            }
            catch
            {
                Console.WriteLine($"[System] 원격 MCP 서버 '{pluginName}' 연결 실패 (무시하고 진행)");
            }
        }
    }

    public static class McpPluginAdapter
    {
        public static async Task RegisterMcpToolsAsPlugin(Kernel kernel, McpClient mcpClient, string pluginName)
        {
            var toolsResult = await mcpClient.ListToolsAsync();
            var tools = toolsResult.ToList();

            var functions = new List<KernelFunction>();

            foreach (var tool in tools)
            {
                var function = KernelFunctionFactory.CreateFromMethod(
                    method: async (KernelArguments args) =>
                    {
                        var arguments = args
                            .Where(kv => kv.Value is JsonElement)
                            .ToDictionary(
                                kv => kv.Key,
                                kv => (JsonElement)kv.Value!
                            );

                        var result = await mcpClient.CallToolAsync(new CallToolRequestParams
                        {
                            Name = tool.Name,
                            Arguments = arguments
                        });

                        var textContent = result.Content.FirstOrDefault() as ContentBlock;
                        return textContent?.ToString() ?? "No text content returned.";
                    },
                    functionName: tool.Name,
                    description: tool.Description,
                    parameters: ConvertMcpParamsToSkParams(tool.JsonSchema)
                );

                functions.Add(function);
            }

            kernel.Plugins.Add(KernelPluginFactory.CreateFromFunctions(pluginName, functions));
        }

        // JSON Schema(MCP)를 SK Parameter로 변환하는 간단한 매퍼
        private static IEnumerable<KernelParameterMetadata> ConvertMcpParamsToSkParams(object? schemaObj)
        {
            return new List<KernelParameterMetadata>();
        }
    }

    public class LightsPlugin
    {
        [KernelFunction, Description("전등 제어")]
        public string SetLightState(string room, bool isOn) => $"{room} 불을 {(isOn ? "켰어" : "껐어")}.";

        [KernelFunction, Description("전등 상태 확인")]
        public string GetStatus() => "거실: 켜짐, 방: 꺼짐";
    }
}