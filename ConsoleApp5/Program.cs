using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    class Program
    {
        private static readonly List<McpClient> McpClients = new();

        static async Task Main(string[] args)
        {
            try
            {
                var builder = Kernel.CreateBuilder();
                builder.Services.AddLogging();

                builder.AddOllamaChatCompletion("gpt-oss:20b", new Uri("http://localhost:11434"));

                Kernel kernel = builder.Build();

                kernel.Plugins.AddFromType<LightsPlugin>("Lights");

                var mcpConfigPath = "mcp-config.json";
                if (File.Exists(mcpConfigPath))
                {
                    await RegisterMcpServersAsync(kernel, mcpConfigPath);
                }
                else
                {
                    Console.WriteLine("mcp-config.json 파일을 찾을 수 없습니다.");
                }

                var chatService = kernel.GetRequiredService<IChatCompletionService>();

                PromptExecutionSettings executionSettings = new()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                var history = new ChatHistory("당신은 집안의 전등을 제어하고 웹 검색도 가능한 유능한 AI 비서입니다.");

                string[] prompts = {
                    "거실 전등 좀 켜줄래?",
                    "google.com 에 접속해서 페이지 제목이 뭔지 알려줘."
                };

                foreach (var userPrompt in prompts)
                {
                    Console.WriteLine($"\n[User]: {userPrompt}");

                    await SendMessageWithHistoryAsync(
                        kernel,
                        chatService,
                        history,
                        userPrompt,
                        executionSettings
                    );

                    var lastResponse = history.Last();
                    Console.WriteLine($"[Assistant]: {lastResponse.Content}");
                }

                Console.WriteLine("\n작업이 완료되었습니다. 엔터를 누르면 종료합니다.");
                Console.ReadLine();
            }
            finally
            {
                foreach (var client in McpClients)
                {
                    await client.DisposeAsync();
                }
            }
        }

        static async Task RegisterMcpServersAsync(Kernel kernel, string configPath)
        {
            var jsonContent = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<McpConfig>(jsonContent);

            if (config?.McpServers == null) return;

            foreach (var server in config.McpServers)
            {
                try
                {
                    Console.WriteLine($"[System] MCP 서버 시작 중: {server.Key} ({server.Value.Description})");

                    var transport = new StdioClientTransport(new StdioClientTransportOptions
                    {
                        Command = server.Value.Command,
                        Arguments = server.Value.Args
                    });

                    var client = await McpClient.CreateAsync(transport);
                    McpClients.Add(client);

                    var toolsResult = await client.ListToolsAsync();
                    var pluginFunctions = new List<KernelFunction>();

                    foreach (var tool in toolsResult)
                    {
                        Console.WriteLine($"  - 도구 발견: {tool.Name}");

                        var toolName = tool.Name;
                        var toolDescription = tool.Description;
                        var toolSchema = tool.JsonSchema;
                        var serverKey = server.Key;
                        var mcpClient = client;

                        var function = KernelFunctionFactory.CreateFromMethod(
                            async (KernelArguments args, CancellationToken ct) =>
                            {
                                Console.WriteLine($"\n>>> [MCP 호출] {serverKey} -> {toolName}");

                                var arguments = new Dictionary<string, object?>();
                                foreach (var kv in args)
                                {
                                    if (kv.Value == null) continue;

                                    if (kv.Value is JsonElement jsonElement)
                                    {
                                        arguments[kv.Key] = jsonElement;
                                    }
                                    else
                                    {
                                        var json = JsonSerializer.Serialize(kv.Value);
                                        arguments[kv.Key] = JsonDocument.Parse(json).RootElement.Clone();
                                    }
                                }

                                try
                                {
                                    var result = await mcpClient.CallToolAsync(
                                        toolName,
                                        arguments,
                                        cancellationToken: ct
                                    );

                                    return string.Join("\n", result.Content.Select(c =>
                                        c.ToString() ?? string.Empty));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[Error] MCP ��구 호출 실패: {ex.Message}");
                                    return $"Error calling tool: {ex.Message}";
                                }
                            },
                            functionName: toolName,
                            description: toolDescription ?? "",
                            parameters: MapJsonSchemaToParameters(toolSchema)
                        );

                        pluginFunctions.Add(function);
                    }

                    if (pluginFunctions.Count > 0)
                    {
                        kernel.Plugins.Add(KernelPluginFactory.CreateFromFunctions(server.Key, pluginFunctions));
                        Console.WriteLine($"[System] {server.Key} 플러그인 등록 완료 ({pluginFunctions.Count}개 도구)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] MCP 서버 '{server.Key}' 시작 실패: {ex.Message}");
                }
            }
        }

        static IEnumerable<KernelParameterMetadata> MapJsonSchemaToParameters(JsonElement? inputSchemaObj)
        {
            var parameters = new List<KernelParameterMetadata>();

            if (inputSchemaObj == null) return parameters;

            try
            {
                var root = inputSchemaObj.Value;

                if (root.TryGetProperty("properties", out var props))
                {
                    var requiredParams = new HashSet<string>();
                    if (root.TryGetProperty("required", out var requiredArray))
                    {
                        foreach (var item in requiredArray.EnumerateArray())
                        {
                            if (item.GetString() is string reqName)
                            {
                                requiredParams.Add(reqName);
                            }
                        }
                    }

                    foreach (var prop in props.EnumerateObject())
                    {
                        var paramName = prop.Name;
                        var paramDesc = prop.Value.TryGetProperty("description", out var d)
                            ? d.GetString() ?? ""
                            : "";
                        var paramType = prop.Value.TryGetProperty("type", out var t)
                            ? t.GetString()
                            : "string";

                        parameters.Add(new KernelParameterMetadata(paramName)
                        {
                            Description = paramDesc,
                            IsRequired = requiredParams.Contains(paramName),
                            ParameterType = GetTypeFromJsonType(paramType)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] 스키마 파싱 오류: {ex.Message}");
            }

            return parameters;
        }

        static Type GetTypeFromJsonType(string? jsonType)
        {
            return jsonType switch
            {
                "string" => typeof(string),
                "integer" => typeof(int),
                "number" => typeof(double),
                "boolean" => typeof(bool),
                "array" => typeof(object[]),
                "object" => typeof(object),
                _ => typeof(string)
            };
        }

        static async Task SendMessageWithHistoryAsync(
            Kernel kernel,
            IChatCompletionService chat,
            ChatHistory chatHistory,
            string userInput,
            PromptExecutionSettings settings)
        {
            chatHistory.AddUserMessage(userInput);

            var response = await chat.GetChatMessageContentAsync(chatHistory, settings, kernel);

            if (response.Content != null)
            {
                chatHistory.AddAssistantMessage(response.Content);
            }
        }
    }

    public class McpConfig
    {
        [System.Text.Json.Serialization.JsonPropertyName("mcpServers")]
        public Dictionary<string, McpServerDef>? McpServers { get; set; }
    }

    public class McpServerDef
    {
        [System.Text.Json.Serialization.JsonPropertyName("command")]
        public string Command { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("args")]
        public string[]? Args { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }

    public class LightsPlugin
    {
        [KernelFunction, Description("전등의 전원을 변경합니다.")]
        public string SetLightState(
            [Description("방 이름 (예: 거실, 주방, 침실)")] string room,
            [Description("전등 상태 (true: 켜기, false: 끄기)")] bool isOn)
        {
            Console.WriteLine($"\n>>> [시스템 호출] {room} 전등을 {(isOn ? "켬" : "끔")}으로 설정 중...");
            return $"{room} 전등이 {(isOn ? "켜졌습니다" : "꺼졌습니다")}.";
        }

        [KernelFunction, Description("모든 전등의 현재 상태를 가져옵니다.")]
        public string GetStatus()
        {
            return "현재 거실 전등은 꺼져 있고, 주방 전등은 켜져 있습니다.";
        }
    }
}