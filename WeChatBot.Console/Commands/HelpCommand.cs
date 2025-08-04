using System.Text;

namespace WeChatBot.Console.Commands;

public static partial class Command
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="commandText"></param>
    /// <returns></returns>
    public static Task<string?> HandleHelpCommand(string commandText)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("[自动回复]:");
        stringBuilder.AppendLine("欢迎使用 WeChatBot！以下是可用的命令列表：");
        stringBuilder.AppendLine("1. \\bot <内容> - 发送内容到大语言模型并获取回复。");
        //stringBuilder.AppendLine("2. \\weather <城市> - 获取当地天气信息。(未实装)");
        stringBuilder.AppendLine("2. \\picture - 发送图库中随机图片到聊天。");

        return Task.FromResult<string?>(stringBuilder.ToString());
    }
}
