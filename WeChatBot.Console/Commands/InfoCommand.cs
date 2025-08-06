namespace WeChatBot.Console.Commands;

public static partial class Command
{
    /// <summary>
    /// 处理 \info 命令，介绍基本信息
    /// </summary>
    public static Task<string?> HandleInfoCommand(string commandText)
    {
        return Task.FromResult<string?>("我是WeChatBot");
    }
}
