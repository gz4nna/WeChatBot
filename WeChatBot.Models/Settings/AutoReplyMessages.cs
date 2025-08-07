namespace WeChatBot.Models.Settings;

/// <summary>
/// 自动回复文本
/// </summary>
public class AutoReplyMessages
{
    /// <summary>
    /// 拍一拍自动回复文本
    /// </summary>
    public string PatMessage { get; set; } = "";

    /// <summary>
    /// 撤回消息自动回复文本
    /// </summary>
    public string RecallMessage { get; set; } = "";
}
