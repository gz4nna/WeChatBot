namespace WeChatBot.Models.Settings.ParamsSettings;

/// <summary>
/// \chat 命令参数
/// </summary>
public class ChatParamsSettings
{
    /// <summary>
    /// 大语言模型API端点
    /// </summary>
    public string ApiEndpoint { get; set; } = "";

    /// <summary>
    /// API超时时间（分钟）
    /// </summary>
    public int ApiTimeoutMinutes { get; set; } = 10;
}
