using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using System.ComponentModel;
using System.Threading.Tasks;

namespace SimpleMcpCommand
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddMcpServer()
                .WithStdioServerTransport()
                .WithTools<EchoTool>();

            builder.Logging.AddConsole();

            using IHost host = builder.Build();
            await host.RunAsync();
        }
    }

    [McpServerToolType]
    internal class EchoTool
    {
        [McpServerTool(Name = "echo"), Description("클라이언트에게 메시지를 그대로 다시 보냅니다")]
        public static string Echo([Description("다시 보낼 메시지입니다.")] string message)
        {
            return $"Echo: {message}";
        }
    }
}