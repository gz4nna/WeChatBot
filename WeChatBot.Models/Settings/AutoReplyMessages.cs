namespace WeChatBot.Models.Settings;

/// <summary>
/// 自动回复文本
/// </summary>
public class AutoReplyMessages
{
    public string PatMessage { get; set; } = "";
    public string RecallMessage { get; set; } = "";
}
