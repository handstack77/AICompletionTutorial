using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Kernel.CreateBuilder();

            // builder.AddOllamaEmbeddingGenerator(modelId: "bge-m3:567m", endpoint: new Uri("http://localhost:11434"));
            builder.AddGoogleAIEmbeddingGenerator("gemini-embedding-001", (Environment.GetEnvironmentVariable("GOOGLE_GEMINI_API_KEY") ?? "AIzaSyBWr36KqyBkqXWl574g3f4IDW-4Bqe7jHo"));

            builder.Services.AddLogging();

            Kernel kernel = builder.Build();

            var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            Console.WriteLine("=== 임베딩 테스트 ===\n");

            string[] sentences = {
                "안녕하세요, 반갑습니다.",
                "오늘 날씨가 참 좋네요.",
                "인공지능 기술이 발전하고 있습니다.",
                "거실 전등을 켜주세요.",
                "조명을 켜줄래요?"
            };

            Console.WriteLine("[ 1. 단일 문장 임베딩 생성 ]");
            var singleEmbedding = await embeddingGenerator.GenerateAsync(sentences[0]);
            Console.WriteLine($"문장: {sentences[0]}");
            Console.WriteLine($"임베딩 차원: {singleEmbedding.Vector.Length}");
            Console.WriteLine($"임베딩 벡터 (처음 10개): [{string.Join(", ", singleEmbedding.Vector.ToArray().Take(10).Select(v => $"{v:F4}"))}...]\n");

            Console.WriteLine("[ 2. 배치 임베딩 생성 ]");
            var embeddingResults = await embeddingGenerator.GenerateAsync(sentences);
            var embeddings = embeddingResults.ToList();

            for (int i = 0; i < sentences.Length; i++)
            {
                Console.WriteLine($"\n문장 {i + 1}: {sentences[i]}");
                Console.WriteLine($"임베딩 차원: {embeddings[i].Vector.Length}");
                Console.WriteLine($"임베딩 벡터 (처음 5개): [{string.Join(", ", embeddings[i].Vector.ToArray().Take(5).Select(v => $"{v:F4}"))}...]");
            }

            Console.WriteLine("\n\n[ 3. 문장 간 코사인 유사도 ]");
            for (int i = 0; i < sentences.Length; i++)
            {
                for (int j = i + 1; j < sentences.Length; j++)
                {
                    double similarity = CosineSimilarity(
                        embeddings[i].Vector.ToArray(),
                        embeddings[j].Vector.ToArray()
                    );
                    Console.WriteLine($"\n'{sentences[i]}' <-> '{sentences[j]}'");
                    Console.WriteLine($"유사도: {similarity:F4} ({similarity * 100:F2}%)");
                }
            }

            Console.WriteLine("\n\n[ 4. 쿼리와 가장 유사한 문장 찾기 ]");
            string query = "불을 켜줘";
            var queryEmbedding = await embeddingGenerator.GenerateAsync(query);

            Console.WriteLine($"쿼리: '{query}'\n");

            var similarities = sentences.Select((sentence, index) => new
            {
                Sentence = sentence,
                Index = index,
                Similarity = CosineSimilarity(queryEmbedding.Vector.ToArray(), embeddings[index].Vector.ToArray())
            })
            .OrderByDescending(x => x.Similarity)
            .ToList();

            Console.WriteLine("유사도 순위:");
            for (int i = 0; i < similarities.Count; i++)
            {
                Console.WriteLine($"{i + 1}. '{similarities[i].Sentence}' - 유사도: {similarities[i].Similarity:F4} ({similarities[i].Similarity * 100:F2}%)");
            }

            Console.WriteLine("\n\n작업이 완료되었습니다. 엔터를 누르면 종료합니다.");
            Console.ReadLine();
        }

        static double CosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
                throw new ArgumentException("벡터의 길이가 같아야 합니다.");

            double dotProduct = 0.0;
            double magnitude1 = 0.0;
            double magnitude2 = 0.0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0.0 || magnitude2 == 0.0)
                return 0.0;

            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}