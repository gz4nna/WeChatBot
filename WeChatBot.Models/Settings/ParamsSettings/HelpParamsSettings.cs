namespace WeChatBot.Models.Settings.ParamsSettings;

/// <summary>
/// \help 命令参数
/// </summary>
public class HelpParamsSettings
{
    // 命令说明列表
    public List<string>? CommandsList { get; set; } = [];

    public void ResetToDefaults()
    {
        CommandsList = [
            "1. \\help - 获取帮助。",
            "2. \\chat <内容> - 发送内容到大语言模型并获取回复。",
            "3. \\picture - 发送图库中随机图片到聊天。",
            "4. \\info - 显示机器人信息"
        ];
    }
}
