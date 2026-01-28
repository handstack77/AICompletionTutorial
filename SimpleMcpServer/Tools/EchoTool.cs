using System.ComponentModel;

using ModelContextProtocol.Server;

namespace SimpleMcpServer.Tools
{
    [McpServerToolType]
    public sealed class EchoTool
    {
        [McpServerTool, Description("입력값을 클라이언트에게 그대로 다시 보냅니다")]
        public string Echo(string message)
        {
            return "Echo: " + message;
        }
    }
}