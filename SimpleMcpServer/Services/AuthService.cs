using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

using ModelContextProtocol.Protocol;

namespace SimpleMcpServer.Services
{
    /*
    // 클라이언트 코드 예시 (Basic Auth)
    var username = "admin";
    var password = "password123";

    using var client = new HttpClient();

    // 1. 아이디:비밀번호 조합 생성
    var authToken = Encoding.UTF8.GetBytes($"{username}:{password}");
    var base64Token = Convert.ToBase64String(authToken);

    // 2. 헤더 설정
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Token);
     */
    public class AuthService
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public AuthService(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void ValidateBearerToken()
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null)
            {
                throw new InvalidOperationException("HTTP Context를 사용할 수 없습니다.");
            }

            var authHeader = AuthenticationHeaderValue.Parse(context.Request.Headers["Authorization"]!);
            if (!"Basic".Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("누락되었거나 잘못된 Authorization 헤더입니다. Bearer 토큰이 필요합니다.");
            }

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? string.Empty);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            var username = credentials[0];
            var password = credentials[1];

            if (username == "admin" && password == "password123")
            {
                // 인증 성공
            }
            else
            {
                throw new UnauthorizedAccessException("Invalid token.");
            }
        }
    }
}