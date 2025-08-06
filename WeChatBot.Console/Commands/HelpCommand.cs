using System.Text;

using WeChatBot.Models.Settings;

namespace WeChatBot.Console.Commands;

public static partial class Command
{

    static readonly BotSettings _settings = Program.GetService<BotSettings>();

    /// <summary>
    /// HttpClient设置超时时间为 10 分钟,因为我本地推理速度有点慢
    /// </summary>
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commandText"></param>
    /// <returns></returns>
    public static Task<string?> HandleHelpCommand(string _)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("欢迎使用 WeChatBot！以下是可用的命令列表：");
        _settings.CommandParams.HelpParams.CommandsList.ForEach(line => stringBuilder.AppendLine(line));

        return Task.FromResult<string?>(stringBuilder.ToString());
    }
}
