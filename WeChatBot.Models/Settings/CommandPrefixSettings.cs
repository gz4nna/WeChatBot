namespace WeChatBot.Models.Settings;

/// <summary>
/// 命令前缀设置
/// </summary>
public class CommandPrefixSettings
{
    /// <summary>
    /// help 命令的前缀
    /// </summary>
    public string Help { get; set; } = "\\help";

    /// <summary>
    /// chat 命令的前缀
    /// </summary>
    public string Chat { get; set; } = "\\chat";

    /// <summary>
    /// weather 命令的前缀
    /// </summary>
    public string Weather { get; set; } = "\\weather";

    /// <summary>
    /// picture 命令的前缀
    /// </summary>
    public string Picture { get; set; } = "\\picture";

    /// <summary>
    /// info 命令的前缀
    /// </summary>
    public string Info { get; set; } = "\\info";
}
