using System.ComponentModel;

using ModelContextProtocol.Server;

using SimpleMcpServer.Services;

namespace SimpleMcpServer.Prompts
{
    [McpServerPromptType]
    public class CodeReviewPrompt
    {
        private readonly AuthService authService;

        public CodeReviewPrompt(AuthService authService)
        {
            this.authService = authService;
        }

        [McpServerPrompt(Name = "code_review")]
        [Description("코드 리뷰를 수행하기 위한 프롬프트입니다.")]
        public string ReviewCode([Description("리뷰할 코드")] string code, [Description("프로그래밍 언어")] string language = "csharp")
        {
            authService.ValidateBearerToken();

            return $"""
                다음 {language} 코드를 리뷰해주세요:
                
                ```{language}
                {code}
                ```
                
                다음 항목들을 검토해주세요:
                1. 코드 품질 및 가독성
                2. 잠재적인 버그나 오류
                3. 성능 개선 가능성
                4. 보안 취약점
                5. 모범 사례 준수 여부
                """;
        }
    }
}