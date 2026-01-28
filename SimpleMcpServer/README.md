# SimpleMcpServer 개요

이 프로젝트는 **ASP.NET Core**를 기반으로 한 **HTTP/SSE 방식의 MCP (Model Context Protocol) 서버**입니다.
다양한 도구(Tools), 리소스(Resources), 프롬프트(Prompts)를 제공하며, 외부 날씨 API와 연동하는 기능도 포함하고 있습니다.

## 실행 방법

1. **실행**
   ```bash
   dotnet run
   ```
   - 서버는 기본적으로 `http://localhost:8282` (또는 설정된 포트)에서 수신 대기합니다.

2. **클라이언트 연결**
   - MCP 클라이언트에서 이 서버의 SSE 엔드포인트(기본적으로 `/sse` 경로가 사용될 수 있음, 또는 루트)로 연결합니다.

## 구성 포인트

- **Program.cs**
  - `app.MapMcp()`: MCP 엔드포인트를 매핑합니다.
  - `.WithHttpTransport()`: HTTP 기반 전송을 사용합니다.
  - `.WithPrompts<CodeReviewPrompt>()`: 프롬프트 등록.
  - `.WithResources<SimpleResource>()`: 리소스 등록.
  - `.WithTools<...>()`: Echo, SampleLlm, Weather 등 도구 등록.
- **appsettings.json**
  - `Urls`: 서버가 바인딩할 URL 및 포트를 설정합니다.

## 확장 포인트

- **Tools (도구)**
  - `Tools/` 폴더에 새로운 도구 클래스를 추가하고 `Program.cs`에 등록하여 기능을 추가할 수 있습니다. (예: DB 조회, 계산기 등)
- **Resources (리소스)**
  - `Resources/` 폴더에 정적 텍스트나 파일 내용을 제공하는 리소스를 추가할 수 있습니다.
- **Prompts (프롬프트)**
  - `Prompts/` 폴더에 재사용 가능한 프롬프트 템플릿을 정의할 수 있습니다.
- **Services**
  - `AuthService` 등을 통해 인증/인가 로직을 강화할 수 있습니다.