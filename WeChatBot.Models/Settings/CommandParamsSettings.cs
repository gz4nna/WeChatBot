using WeChatBot.Models.Settings.ParamsSettings;

namespace WeChatBot.Models.Settings;

/// <summary>
/// 命令参数设置
/// </summary>
public class CommandParamsSettings
{
    public HelpParamsSettings HelpParams { get; set; } = new();

    public ChatParamsSettings ChatParams { get; set; } = new();

    public PictureParamsSettings PictureParams { get; set; } = new();

    public InfoParamsSettings InfoParams { get; set; } = new();
}
