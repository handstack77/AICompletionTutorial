# ConsoleApp2 개요

이 프로젝트는 **Semantic Kernel**의 **임베딩(Embedding)** 기능을 보여주는 예제입니다. 
텍스트를 벡터로 변환(임베딩)하고, 코사인 유사도(Cosine Similarity)를 계산하여 문장 간의 의미적 유사성을 비교하거나 사용자의 쿼리와 가장 관련된 문장을 찾는 방법을 시연합니다.

## 실행 방법

1. **사전 준비 사항**
   - **Google Gemini API Key**: 환경 변수 `GOOGLE_GEMINI_API_KEY`가 설정되어 있어야 합니다. (기본 설정: `gemini-embedding-001` 모델 사용)
   - **또는 Ollama**: 로컬 Ollama 사용 시 `builder.AddOllamaEmbeddingGenerator` 주석을 해제하고 설정해야 합니다.

2. **실행**
   ```bash
   dotnet run
   ```

## 구성 포인트

- **Program.cs**
  - **임베딩 생성기 설정**: `builder.AddGoogleAIEmbeddingGenerator`를 통해 임베딩 모델을 설정합니다.
  - **테스트 문장**: `sentences` 배열에 임베딩을 생성할 문장들을 정의합니다.

## 확장 포인트

- **데이터셋 확장**
  - `sentences` 배열에 더 많은 문장을 추가하여 임베딩 성능을 테스트하거나 검색 범위를 넓힐 수 있습니다.
- **검색 쿼리 변경**
  - `query` 변수의 값을 변경하여("불을 켜줘" 등) 다른 질문에 대해 어떤 문장이 가장 유사하게 검색되는지 확인할 수 있습니다.
- **유사도 알고리즘**
  - `CosineSimilarity` 메서드 외에 유클리드 거리 등 다른 유사도 측정 방식을 구현하여 비교해 볼 수 있습니다.