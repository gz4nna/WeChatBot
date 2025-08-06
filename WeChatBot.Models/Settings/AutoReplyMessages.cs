namespace WeChatBot.Models.Settings;

/// <summary>
/// 自动回复文本
/// </summary>
public class AutoReplyMessages
{
    public string PatMessage { get; set; } = "绝对不许{0}";
    public string RecallMessage { get; set; } = "大胆!撤回了什么,让我看看";
}
