namespace WeChatBot.Console.Commands;

public static partial class Command
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="commandText"></param>
    /// <returns></returns>
    public static async Task<string?> HandleWeatherCommand(string commandText)
    {
        return "雨下整夜~我的爱溢出就像雨水~";
    }
}
