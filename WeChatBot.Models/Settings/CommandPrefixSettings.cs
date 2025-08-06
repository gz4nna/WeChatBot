namespace WeChatBot.Models.Settings;

/// <summary>
/// 命令前缀设置
/// </summary>
public class CommandPrefixSettings
{
    public string Help { get; set; } = "\\help";
    public string Chat { get; set; } = "\\chat";
    public string Weather { get; set; } = "\\weather";
    public string Picture { get; set; } = "\\picture";
    public string Info { get; set; } = "\\info";
}
