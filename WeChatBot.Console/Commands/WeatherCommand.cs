namespace WeChatBot.Console.Commands;

public static partial class Command
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="commandText"></param>
    /// <returns></returns>
    public static Task<string?> HandleWeatherCommand(string commandText)
    {
        return Task.FromResult<string?>("雨下整夜~我的爱溢出就像雨水~");
    }
}
