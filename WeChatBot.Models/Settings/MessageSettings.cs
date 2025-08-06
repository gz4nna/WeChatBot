namespace WeChatBot.Models.Settings;

public class MessageSettings
{
    public int MaxMessageLength { get; set; } = 1000;
    public int MessageDelay { get; set; } = 1000;
}
