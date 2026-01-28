using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Kernel.CreateBuilder();

            builder.Services.AddLogging();

            builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionInvocationLoggingFilter>();
            builder.Services.AddSingleton<IPromptRenderFilter, GuardrailPromptFilter>();
            builder.Services.AddSingleton<IAutoFunctionInvocationFilter, LightControlAutoFilter>();

            builder.AddOllamaChatCompletion("gpt-oss:20b", new Uri("http://localhost:11434"));
            // builder.AddGoogleAIGeminiChatCompletion("gemini-3-flash-preview", (Environment.GetEnvironmentVariable("GOOGLE_GEMINI_API_KEY") ?? "AIzaSyBWr36KqyBkqXWl574g3f4IDW-4Bqe7jHo"));

            Kernel kernel = builder.Build();

            kernel.Plugins.AddFromType<LightsPlugin>("Lights");

            var chatService = kernel.GetRequiredService<IChatCompletionService>();

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
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"[User]: {userPrompt}");
                Console.ResetColor();

                history.AddUserMessage(userPrompt);

                var result = await chatService.GetChatMessageContentAsync(history, executionSettings, kernel);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[Assistant]: {result.Content}");
                Console.ResetColor();

                history.AddMessage(result.Role, result.Content ?? string.Empty);
            }

            Console.WriteLine("\n작업이 완료되었습니다. 엔터를 누르면 종료합니다.");
            Console.ReadLine();
        }
    }

    public sealed class FunctionInvocationLoggingFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[필터:함수호출] '{context.Function.Name}' 함수를 실행하려 합니다.");

            if (context.Arguments.Any())
            {
                Console.WriteLine($"  - 인수: {string.Join(", ", context.Arguments.Select(a => $"{a.Key}={a.Value}"))}");
            }
            Console.ResetColor();

            await next(context);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[필터:함수호출] '{context.Function.Name}' 실행 완료. 결과: {context.Result}");
            Console.ResetColor();
        }
    }

    public class GuardrailPromptFilter : IPromptRenderFilter
    {
        public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
        {
             List<string> forbiddenWords = new() { "폭탄", "해킹", "마약", "비밀번호" };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[필터:프롬프트] 프롬프트 렌더링 중...");
            Console.ResetColor();

            var userMessage = context.Arguments.ToString();
            if (string.IsNullOrEmpty(userMessage) == false)
            {
                foreach (var word in forbiddenWords)
                {
                    if (userMessage.Contains(word))
                    {
                        throw new Exception($"부적절한 키워드('{word}')가 포함되어 있어 요청을 중단합니다.");
                    }
                }
            }

            await next(context);

            if (!string.IsNullOrEmpty(context.RenderedPrompt))
            {
                context.RenderedPrompt += "\n(System Note: 사용자가 무엇을 물어보든, 답변 끝에 '주인님'을 붙여서 아주 공손하게 대답하세요.)";

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[필터:프롬프트] 프롬프트 수정됨 (공손함 지시 추가).");
                Console.ResetColor();
            }
        }
    }

    public sealed class LightControlAutoFilter : IAutoFunctionInvocationFilter
    {
        public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
        {
            await next(context);

            var functionResult = context.Result.GetValue<string>();
            var functionName = context.Function.Name;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[필터:자동호출] 루프 감지 - 함수: {functionName}, 채팅 기록 수: {context.ChatHistory.Count}");

            if (functionName == "SetLightState")
            {
                Console.WriteLine($"[필터:자동호출] 전등 제어 함수가 실행되었습니다. (Terminate=true로 설정 시 여기서 즉시 종료 가능)");
                context.Terminate = true;
            }
            Console.ResetColor();
        }
    }

    public class LightsPlugin
    {
        [KernelFunction, Description("전등의 전원을 변경합니다.")]
        public string SetLightState(
            [Description("전등 위치 (예: 거실, 주방)")] string room,
            [Description("켜기(true) 또는 끄기(false)")] bool isOn)
        {
            return $"{room} 전등을 {(isOn ? "켜짐" : "꺼짐")} 상태로 변경했습니다.";
        }

        [KernelFunction, Description("모든 전등의 현재 상태를 가져옵니다.")]
        public string GetStatus()
        {
            return "현재 거실 전등은 꺼져 있고, 주방 전등은 켜져 있습니다.";
        }
    }
}