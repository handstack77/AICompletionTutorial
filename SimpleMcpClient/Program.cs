using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using ModelContextProtocol.Client;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleMcpClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            McpConfig? mcpConfig = null;
            try
            {
                mcpConfig = LoadMcpConfig();
                if (mcpConfig == null || mcpConfig.McpServers == null || mcpConfig.McpServers.Count == 0)
                {
                    Console.WriteLine("[오류] mcp-config.json 파일을 찾을 수 없거나 서버가 정의되지 않았습니다.");
                    return;
                }

                var serverList = new List<string>(mcpConfig.McpServers.Keys);
                for (int i = 0; i < serverList.Count; i++)
                {
                    Console.WriteLine($"사용 가능한 MCP 서버: {i + 1}. {serverList[i]}");

                    var serverName = serverList[i];
                    var serverConfig = mcpConfig.McpServers[serverName];

                    Console.WriteLine($"\n[1] '{serverName}' 서버에 연결 중...\n");

                    using var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Warning);
                    });

                    var transport = CreateClientTransport(serverName, serverConfig, loggerFactory);
                    if (transport == null)
                    {
                        Console.WriteLine($"[오류] 서버 '{serverName}' 설정이 잘못되었습니다.");
                        continue;
                    }

                    var client = await McpClient.CreateAsync(transport);

                    Console.WriteLine("[2] 연결 성공! 서버에 연결 되었습니다.\n");

                    if (client.ServerInfo != null)
                    {
                        Console.WriteLine($"[서버 정보]");
                        Console.WriteLine($"    - 이름: {client.ServerInfo.Name}");
                        Console.WriteLine($"    - 버전: {client.ServerInfo.Version}");
                    }

                    // 도구 목록 조회
                    Console.WriteLine("\n[3] 도구 목록 요청 중 (tools/list)...");
                    try
                    {
                        var tools = await client.ListToolsAsync();
                        Console.WriteLine($"\n[도구 목록] ----------------------");
                        Console.WriteLine($"사용 가능한 도구 수: {tools.Count}\n");

                        foreach (var tool in tools)
                        {
                            Console.WriteLine($"    - 도구 이름: {tool.Name}");
                            Console.WriteLine($"    - 설명: {tool.Description}");
                            Console.WriteLine($"    - 입력 스키마: {tool.JsonSchema}");
                            Console.WriteLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  도구 목록을 가져올 수 없습니다: {ex.Message}");
                    }

                    Console.WriteLine("[4] 리소스 목록 요청 중 (resources/list)...");
                    try
                    {
                        var resources = await client.ListResourcesAsync();
                        Console.WriteLine($"사용 가능한 리소스 수: {resources.Count}\n");

                        foreach (var resource in resources)
                        {
                            Console.WriteLine($"    - {resource.Name}: {resource.Uri}");
                        }
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    리소스 목록을 가져올 수 없습니다: {ex.Message}\n");
                    }

                    Console.WriteLine("[5] 프롬프트 목록 요청 중 (prompts/list)...");
                    try
                    {
                        var prompts = await client.ListPromptsAsync();
                        Console.WriteLine($"사용 가능한 프롬프트 수: {prompts.Count}\n");

                        foreach (var prompt in prompts)
                        {
                            Console.WriteLine($"    - {prompt.Name}: {prompt.Description}");
                        }
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    프롬프트 목록을 가져올 수 없습니다: {ex.Message}\n");
                    }

                    Console.WriteLine("[완료] MCP 서버 검사가 완료되었습니다.");

                    await client.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[오류] 서버 연결 실패: {ex}");
            }

            var builder = Kernel.CreateBuilder();

            builder.AddOllamaChatCompletion(
                modelId: "llama3.2",
                endpoint: new Uri("http://localhost:11434")
            );

            var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
            Console.WriteLine($"프로젝트 루트: {projectRoot}\n");

            var kernel = builder.Build();
            var mcpClients = new List<McpClient>();

            foreach (var (serverName, serverConfig) in mcpConfig!.McpServers)
            {
                try
                {
                    Console.WriteLine($"MCP 서버 연결 중: {serverName}");

                    using var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Warning);
                    });

                    var transport = CreateClientTransport(serverName, serverConfig, loggerFactory);
                    if (transport == null)
                    {
                        Console.WriteLine($"{serverName} 서버 설정이 잘못되었습니다, 건너뜀");
                        continue;
                    }

                    var mcpClient = await McpClient.CreateAsync(transport);
                    mcpClients.Add(mcpClient);

                    var tools = await mcpClient.ListToolsAsync();
                    if (tools.Any())
                    {
                        kernel.Plugins.AddFromFunctions(
                            ReplaceInvalidCharsWithSpace(serverName),
                            tools.Select(tool => tool.AsKernelFunction())
                        );

                        Console.WriteLine($"{tools.Count()}개 도구 등록됨:");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{serverName} 연결 실패: {ex.Message}");
                }
            }
        }

        private static IClientTransport? CreateClientTransport(string serverName, McpServerConfig config, ILoggerFactory? loggerFactory)
        {
            IClientTransport? transport = null;
            if (config.Command != null && config.Command.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = config.Args?.FirstOrDefault();
                if (!string.IsNullOrEmpty(endpoint))
                {
                    var sharedHandler = new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
                    };
                    var httpClient = new HttpClient(sharedHandler);

                    transport = new HttpClientTransport(new HttpClientTransportOptions
                    {
                        Endpoint = new Uri(endpoint)
                    }, httpClient, loggerFactory);
                }
            }
            else if (config.Url != null && config.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var sharedHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
                };
                var httpClient = new HttpClient(sharedHandler);

                transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri(config.Url)
                }, httpClient, loggerFactory);
            }
            else if (!string.IsNullOrEmpty(config.Command))
            {
                transport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = serverName,
                    Command = config.Command,
                    Arguments = config.Args ?? Array.Empty<string>(),
                    WorkingDirectory = config.Env?.ContainsKey("WORKING_DIR") == true ? config.Env["WORKING_DIR"] : null
                }, loggerFactory);
            }

            return transport;
        }

        private static string ReplaceInvalidCharsWithSpace(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string pattern = @"[^a-zA-Z0-9_]";

            return Regex.Replace(input, pattern, "");
        }

        private static McpConfig? LoadMcpConfig()
        {
            try
            {
                var configPath = "mcp-config.json";

                if (!File.Exists(configPath))
                {
                    configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mcp-config.json");
                }

                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"[Warning] mcp-config.json을 찾을 수 없습니다: {configPath}");
                    return null;
                }

                var json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<McpConfig>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[오류] mcp-config.json 로드 실패: {ex.Message}");
                return null;
            }
        }
    }

    public class McpConfig
    {
        public Dictionary<string, McpServerConfig> McpServers { get; set; } = new();
    }

    public class McpServerConfig
    {
        public string? Command { get; set; }
        public string[]? Args { get; set; }
        public string? Url { get; set; }
        public Dictionary<string, string>? Env { get; set; }
    }
}