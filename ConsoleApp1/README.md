# ConsoleApp1 개요

이 프로젝트는 **Semantic Kernel**을 사용하여 기본적인 AI 채팅 기능을 구현한 콘솔 애플리케이션입니다. 
**Ollama**(또는 Google Gemini)를 통해 AI 모델과 대화하며, 로컬 함수(플러그인)를 호출하여 전등 제어와 같은 작업을 수행하는 방법을 보여줍니다.

## 실행 방법

1. **사전 준비 사항**
   - **Ollama**: 로컬에서 Ollama가 실행 중이어야 하며, `gpt-oss:20b` 모델이 설치되어 있어야 합니다. (기본 설정)
     - 설치 명령: `ollama pull gpt-oss:20b`
   - **또는 Google Gemini API Key**: `GOOGLE_GEMINI_API_KEY` 환경 변수가 설정되어 있어야 합니다. (소스 코드에서 주석 해제 필요)

2. **실행**
   ```bash
   dotnet run
   ```

## 구성 포인트

- **Program.cs**
  - **AI 서비스 설정**: `builder.AddOllamaChatCompletion` 또는 `builder.AddGoogleAIGeminiChatCompletion`을 사용하여 사용할 AI 모델과 엔드포인트를 설정합니다.
  - **플러그인 등록**: `kernel.Plugins.AddFromType<LightsPlugin>("Lights")`를 통해 `LightsPlugin`을 AI가 사용할 수 있는 도구로 등록합니다.
- **appsettings.json**
  - 로깅 레벨을 설정합니다.

## 확장 포인트

- **LightsPlugin 클래스**
  - 새로운 메서드를 추가하고 `[KernelFunction]` 및 `[Description]` 특성을 붙여 AI가 사용할 수 있는 새로운 기능을 추가할 수 있습니다. (예: `SetThermostat`, `LockDoor` 등)
- **대화 시나리오**
  - `prompts` 배열에 새로운 사용자 질문을 추가하여 다양한 시나리오를 테스트할 수 있습니다.