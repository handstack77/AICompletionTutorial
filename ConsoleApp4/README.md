# ConsoleApp4 개요

이 프로젝트는 **Semantic Kernel**과 **MCP (Model Context Protocol)**, 그리고 **OpenAPI**를 결합한 예제입니다.
AI 비서가 로컬 파일 시스템(Local MCP), 원격 도구(Remote MCP), 그리고 웹 API(OpenAPI)를 통합적으로 사용하여 사용자의 요청을 처리하는 구조를 보여줍니다.

## 실행 방법

1. **사전 준비 사항**
   - **Google Gemini API Key**: `GOOGLE_GEMINI_API_KEY` 환경 변수가 필요합니다.
   - **Node.js & npx**: 로컬 MCP 서버(파일시스템) 실행을 위해 필요합니다.
   - **MCP 서버**:
     - 로컬: `npx -y @modelcontextprotocol/server-filesystem /` (코드 내에서 자동 실행 시도)
     - 원격: `http://localhost:3000/sse` (별도 실행 필요, 없으면 연결 실패 로그 후 무시됨)
   - **OpenAPI 서버**:
     - `http://localhost:5000/swagger/v1/swagger.json` (별도 실행 필요, 없으면 무시됨)

2. **실행**
   ```bash
   dotnet run
   ```

## 구성 포인트

- **Program.cs**
  - `AddLocalMcpPluginAsync`: 로컬 프로세스로 실행되는 MCP 서버를 연결합니다.
  - `AddRemoteMcpPluginAsync`: SSE(Server-Sent Events)를 통해 원격 MCP 서버를 연결합니다.
  - `AddOpenApiPluginAsync`: Swagger/OpenAPI 명세를 통해 REST API를 플러그인으로 가져옵니다.

## 확장 포인트

- **새로운 MCP 서버 연결**
  - `AddLocalMcpPluginAsync`를 호출하여 SQLite, PostgreSQL 등 다른 MCP 서버를 추가할 수 있습니다.
- **OpenAPI 서비스 추가**
  - 다른 공개 API의 Swagger URL을 `AddOpenApiPluginAsync`에 등록하여 기능을 확장할 수 있습니다.
- **McpPluginAdapter**
  - 현재는 기본적인 텍스트 결과만 처리하지만, 이미지나 이진 데이터 등 복잡한 MCP 응답을 처리하도록 `RegisterMcpToolsAsPlugin`을 개선할 수 있습니다.