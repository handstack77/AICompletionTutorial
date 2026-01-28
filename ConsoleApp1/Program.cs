using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOllamaChatCompletion("gpt-oss:20b", new Uri("http://localhost:11434"));
            // builder.AddGoogleAIGeminiChatCompletion("gemini-3-flash-preview", (Environment.GetEnvironmentVariable("GOOGLE_GEMINI_API_KEY") ?? "AIzaSyBWr36KqyBkqXWl574g3f4IDW-4Bqe7jHo"));

            builder.Services.AddLogging();

            Kernel kernel = builder.Build();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            kernel.Plugins.AddFromType<LightsPlugin>("Lights");

            PromptExecutionSettings executionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var history = new ChatHistory("당신은 집안의 전등을 제어하는 유능한 AI 비서입니다.");

            string[] prompts = {
                "거실 전등 좀 켜줄래?",
                "지금 불이 어디 어디 켜져 있어?"
            };

            foreach (var userPrompt in prompts)
            {
                Console.WriteLine($"\n[User]: {userPrompt}");

                await SendMessageWithHistoryAsync(
                    kernel,
                    chatService,
                    history,
                    userPrompt,
                    executionSettings,
                    (role, content) => history.AddMessage(role, content)
                );

                var lastResponse = history.Last();
                Console.WriteLine($"[Assistant]: {lastResponse.Content}");
            }

            Console.WriteLine("\n작업이 완료되었습니다. 엔터를 누르면 종료합니다.");
            Console.ReadLine();
        }

        static async Task SendMessageWithHistoryAsync(
            Kernel kernel,
            IChatCompletionService chat,
            ChatHistory chatHistory,
            string userInput,
            PromptExecutionSettings settings,
            Action<AuthorRole, string> addMessageCallback)
        {
            addMessageCallback(AuthorRole.User, userInput);

            var response = await chat.GetChatMessageContentAsync(chatHistory, settings, kernel);

            addMessageCallback(response.Role, response.Content ?? string.Empty);
        }
    }

    public class LightsPlugin
    {
        [KernelFunction, Description("전등의 전원을 변경합니다.")]
        public string SetLightState(string room, bool isOn)
        {
            Console.WriteLine($"\n>>> [시스템 호출] {room} 전등을 {(isOn ? "켬" : "끔")}으로 설정 중...");
            return $"{room} 전등 상태가 성공적으로 변경되었습니다.";
        }

        [KernelFunction, Description("모든 전등의 현재 상태를 가져옵니다.")]
        public string GetStatus()
        {
            return "현재 거실 전등은 꺼져 있고, 주방 전등은 켜져 있습니다.";
        }
    }
}