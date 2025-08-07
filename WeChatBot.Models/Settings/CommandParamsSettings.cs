using WeChatBot.Models.Settings.ParamsSettings;

namespace WeChatBot.Models.Settings;

/// <summary>
/// 命令参数设置
/// </summary>
public class CommandParamsSettings
{
    /// <summary>
    /// help 命令参数设置
    /// </summary>
    public HelpParamsSettings HelpParams { get; set; } = new();

    /// <summary>
    /// chat 命令参数设置
    /// </summary>
    public ChatParamsSettings ChatParams { get; set; } = new();

    /// <summary>
    /// picture 命令参数设置
    /// </summary>
    public PictureParamsSettings PictureParams { get; set; } = new();

    /// <summary>
    /// info 命令参数设置
    /// </summary>
    public InfoParamsSettings InfoParams { get; set; } = new();
}
