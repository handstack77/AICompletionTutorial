using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.AI;

using ModelContextProtocol.Server;

namespace SimpleMcpServer.Tools
{
    [McpServerToolType]
    public sealed class SampleLlmTool
    {
        [McpServerTool(Name = "sampleLLM"), Description("MCP의 샘플링 기능을 사용하여 LLM으로부터 샘플을 추출합니다.")]
        public async Task<string> SampleLLM(McpServer thisServer, [Description("LLM에 전송할 프롬프트입니다.")] string prompt, [Description("생성할 최대 토큰 수입니다.")] int maxTokens, CancellationToken cancellationToken)
        {
            ChatOptions options = new()
            {
                Instructions = "당신은 도움이 되는 테스트 서버입니다.",
                MaxOutputTokens = maxTokens,
                Temperature = 0.7f,
            };

            var samplingResponse = await thisServer.AsSamplingChatClient().GetResponseAsync(prompt, options, cancellationToken);

            return $"LLM sampling result: {samplingResponse}";
        }
    }
}