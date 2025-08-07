using System.Net.Http.Json;
using System.Text.Json;

namespace WeChatBot.Console.Commands;

public static partial class Command
{

    /// <summary>
    /// 处理 \bot 命令
    /// </summary>
    /// <param name="commandText"></param>
    /// <returns></returns>
    public static async Task<string?> HandleChatCommand(string commandText)
    {
        try
        {
            System.Console.WriteLine($"处理Bot命令，提取的文本内容: \"{commandText}\"");

            // 发送到大模型并获取回复
            var response = await GetModelResponseAsync(commandText);
            System.Console.WriteLine($"模型回复: \"{response}\"");

            return response;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"处理Bot命令时出错: {ex.Message}");
            return "处理命令时出错，请稍后再试。";
        }
    }

    /// <summary>
    /// 发送消息到大语言模型并获取回复 (使用 chat/completions 端点)
    /// </summary>
    private static async Task<string> GetModelResponseAsync(string prompt)
    {
        var apiUrl = _settings.CommandParams.ChatParams.ApiEndpoint;

        var requestData = new Dictionary<string, object>
        {
            { "messages", new[] { new { role = "user", content = prompt } } },
            { "mode", "instruct" },
            // 模板名放这里
            //{ "instruction_template", "" }, 
            // 角色放这里
            //{ "character", "" }, 
            //{ "temperature", 0.7 },
            //{ "top_p", 0.8 },
            //{ "top_k", 20 }
        };

        try
        {
            var response = await HttpClient.PostAsJsonAsync(apiUrl, requestData);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            // 解析 JSON 响应
            using var jsonDoc = JsonDocument.Parse(responseBody);
            var choices = jsonDoc.RootElement.GetProperty("choices");
            var firstChoice = choices[0];
            var message = firstChoice.GetProperty("message");
            var generatedText = message.GetProperty("content").GetString();

            // 如果响应不为空，则尝试移除 <think>...</think> 标签,我们只需要输出内容就好了
            if (!string.IsNullOrEmpty(generatedText))
            {
                const string thinkTagEnd = "</think>";
                var thinkTagIndex = generatedText.LastIndexOf(thinkTagEnd, StringComparison.OrdinalIgnoreCase);
                if (thinkTagIndex != -1)
                {
                    // 提取 </think> 标签之后的内容并移除前后的空白字符
                    return generatedText[(thinkTagIndex + thinkTagEnd.Length)..].Trim();
                }
            }

            return generatedText ?? "未能从模型响应中提取文本。";
        }
        catch (HttpRequestException e)
        {
            return $"请求错误: {e.Message}";
        }
        catch (Exception e)
        {
            return $"处理模型响应时发生错误: {e.Message}";
        }
    }
}
