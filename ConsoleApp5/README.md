# ConsoleApp5 개요

이 프로젝트는 **설정 파일(`mcp-config.json`)**을 기반으로 동작하는 **MCP(Model Context Protocol) 클라이언트** 구현체입니다.
Semantic Kernel과 통합되어, 설정 파일에 정의된 여러 MCP 서버(예: Playwright)를 동적으로 로드하고 AI가 해당 도구들을 활용할 수 있게 합니다.

## 실행 방법

1. **사전 준비 사항**
   - **Ollama**: `gpt-oss:20b` 모델이 필요합니다.
   - **Node.js**: MCP 서버(예: Playwright) 실행을 위해 필요합니다.
   - **Playwright 설치**: `npx playwright install` 명령어로 브라우저 바이너리를 설치해야 할 수 있습니다.

2. **실행**
   ```bash
   dotnet run
   ```

## 구성 포인트

- **mcp-config.json**
  - 사용할 MCP 서버들을 정의합니다. 각 서버의 실행 명령어(`command`), 인수(`args`), 설명(`description`)을 설정합니다.
  - 예: Playwright 서버 설정이 기본적으로 포함되어 있습니다.
- **Program.cs**
  - `RegisterMcpServersAsync`: 설정 파일을 읽어 프로세스 기반(Stdio) MCP 서버들을 실행하고 연결합니다.

## 확장 포인트

- **도구 추가 (mcp-config.json)**
  - 파일 시스템 서버, 데이터베이스 서버 등 다른 MCP 구현체를 JSON 설정에 추가하기만 하면 코드 수정 없이 기능을 확장할 수 있습니다.
- **도구 호출 로직 개선**
  - `RegisterMcpServersAsync` 내의 함수 호출 핸들러에서 JSON 객체 파싱 및 오류 처리 로직을 고도화할 수 있습니다.