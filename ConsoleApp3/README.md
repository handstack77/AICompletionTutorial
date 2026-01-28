# ConsoleApp3 개요

이 프로젝트는 **Semantic Kernel**의 고급 기능인 **필터(Filter)** 기능을 중점적으로 다룹니다.
함수 호출 전후 로깅, 프롬프트 렌더링 시 금칙어 검사 및 시스템 지시 사항 주입, 그리고 자동 함수 호출 시 루프 방지 등의 제어 로직을 구현하는 방법을 보여줍니다.

## 실행 방법

1. **사전 준비 사항**
   - **Ollama**: 로컬에서 Ollama가 실행 중이어야 하며, `gpt-oss:20b` 모델이 필요합니다.
     - `http://localhost:11434` 포트 사용.

2. **실행**
   ```bash
   dotnet run
   ```

## 구성 포인트

- **Program.cs**
  - **필터 등록**: `builder.Services.AddSingleton`을 통해 `IFunctionInvocationFilter`, `IPromptRenderFilter`, `IAutoFunctionInvocationFilter`를 구현한 필터들을 등록합니다.
  - **AI 서비스**: `builder.AddOllamaChatCompletion`을 사용합니다.

## 확장 포인트

- **GuardrailPromptFilter**
  - `forbiddenWords` 리스트에 금칙어를 추가하여 보안/안전성 검사를 강화할 수 있습니다.
  - `OnPromptRenderAsync`에서 프롬프트를 수정하는 로직(예: 공손함 지시 추가)을 변경하여 AI의 페르소나를 강제할 수 있습니다.
- **FunctionInvocationLoggingFilter**
  - 함수 호출 로그를 파일로 저장하거나 외부 모니터링 시스템으로 전송하도록 확장할 수 있습니다.
- **LightControlAutoFilter**
  - 특정 함수 호출 시 강제로 대화를 종료하거나(`context.Terminate = true`), 사용자에게 확인을 요청하는 로직을 추가할 수 있습니다.