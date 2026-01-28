# SimpleMcpClient 개요

이 프로젝트는 **Semantic Kernel의 MCP 확장 기능을 사용하지 않고**, 직접 구현한 로직으로 MCP 서버와 통신하는 **독립적인 MCP 클라이언트** 예제입니다.
`mcp-config.json`을 읽어 로컬 프로세스 또는 HTTP/SSE 기반의 MCP 서버에 연결하고, 도구(Tools), 리소스(Resources), 프롬프트(Prompts) 목록을 조회하여 Semantic Kernel에 등록합니다.

## 실행 방법

1. **사전 준비 사항**
   - **Ollama**: `llama3.2` 모델이 필요합니다. (`http://localhost:11434`)
   - **대상 MCP 서버**:
     - `simple-mcp-command`: `SimpleMcpCommand` 프로젝트를 빌드해야 합니다.
     - `simple-mcp-server`: `SimpleMcpServer` 프로젝트를 실행해 두어야 합니다 (`http://localhost:8282`).

2. **실행**
   ```bash
   dotnet run
   ```

## 구성 포인트

- **mcp-config.json**
  - 연결할 MCP 서버 목록을 정의합니다.
  - `command`: 실행 파일 경로 지정 (Stdio 통신).
  - `url`: 서버 URL 지정 (HTTP/SSE 통신).
- **Program.cs**
  - `LoadMcpConfig()`: 설정 파일 로드.
  - `CreateClientTransport()`: 설정에 따라 `StdioClientTransport` 또는 `HttpClientTransport`를 생성합니다.

## 확장 포인트

- **프로토콜 지원 확장**
  - 현재 HTTP/SSE와 Stdio를 지원하며, WebSocket 등 다른 전송 방식을 추가할 수 있습니다.
- **기능 통합**
  - 현재는 도구(Tools)만 Semantic Kernel에 등록(`kernel.Plugins.AddFromFunctions`)하고 있습니다. 리소스(Resources)와 프롬프트(Prompts)도 활용하도록 코드를 확장할 수 있습니다.