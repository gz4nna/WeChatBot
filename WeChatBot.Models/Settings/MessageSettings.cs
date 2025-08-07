namespace WeChatBot.Models.Settings;

/// <summary>
/// 发送消息的设置
/// </summary>
public class MessageSettings
{
    /// <summary>
    /// 单条消息的最大长度
    /// </summary>
    public int MaxMessageLength { get; set; } = 1000;

    /// <summary>
    /// 消息输入结束到发送的延迟时间（毫秒）
    /// </summary>
    public int MessageDelay { get; set; } = 1000;
}
