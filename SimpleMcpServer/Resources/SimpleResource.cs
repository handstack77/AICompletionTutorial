using System.ComponentModel;

using ModelContextProtocol.Server;

namespace SimpleMcpServer.Resources
{
    [McpServerResourceType]
    public class SimpleResource
    {
        [McpServerResource, Description("직접 텍스트 리소스")]
        public string DirectTextResource()
        {
            return """
            {
                "appName": "McpServer",
                "environment": "Production",
                "maxConnections": 100
            }
            """;
        }
    }
}