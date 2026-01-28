# SimpleMcpCommand 개요

이 프로젝트는 표준 입출력(Stdio)을 통해 통신하는 **경량 MCP (Model Context Protocol) 서버**입니다.
콘솔 애플리케이션 형태로 실행되며, 클라이언트(MCP Client)의 요청을 받아 "Echo" 도구를 제공합니다.

## 실행 방법

1. **빌드**
   - 이 프로젝트는 독립적으로 실행하기보다는 MCP 클라이언트에 의해 실행되는 경우가 많습니다.
   ```bash
   dotnet build
   ```
   
2. **MCP 클라이언트에서 사용**
   - 클라이언트의 `mcp-config.json` 등에서 이 프로젝트의 빌드된 실행 파일(`SimpleMcpCommand.exe`) 경로를 `command`로 지정하여 사용합니다.

## 구성 포인트

- **Program.cs**
  - `builder.Services.AddMcpServer()`: MCP 서버를 초기화합니다.
  - `.WithStdioServerTransport()`: 표준 입출력을 전송 계층으로 사용하도록 설정합니다.
  - `.WithTools<EchoTool>()`: 사용할 도구를 등록합니다.

## 확장 포인트

- **새로운 도구 추가**
  - `[McpServerTool]` 속성을 가진 새로운 클래스나 메서드를 생성하고 `Program.cs`에서 `.WithTools<T>()`로 등록하여 기능을 확장할 수 있습니다.
- **전송 계층 변경**
  - `.WithStdioServerTransport()` 대신 `.WithHttpTransport()` 등을 사용하여 네트워크 기반 서버로 변경할 수 있습니다.