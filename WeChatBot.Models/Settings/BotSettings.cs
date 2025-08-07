namespace WeChatBot.Models.Settings;

/// <summary>
/// 机器人默认配置
/// </summary>
public class BotSettings
{
    /// <summary>
    /// 命令前缀
    /// </summary>
    public CommandPrefixSettings CommandPrefixes { get; set; } = new();
    /// <summary>
    /// 命令参数
    /// </summary>
    public CommandParamsSettings CommandParams { get; set; } = new();
    /// <summary>
    /// 自动回复的相关设置
    /// </summary>
    public AutoReplyMessages AutoReplyMessages { get; set; } = new();
    /// <summary>
    /// 发送微信消息的相关设置
    /// </summary>
    public MessageSettings MessageSettings { get; set; } = new();
}




