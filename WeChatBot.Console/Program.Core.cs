using WeChatBot.Console.Commands;
using WeChatBot.Console.Helpers;

namespace WeChatBot.Console;

public partial class Program
{
    // 命令前缀和处理器映射字典
    private static readonly Dictionary<string, Func<string, Task<string?>>> CommandHandlers = new();

    /// <summary>
    /// 初始化命令处理器
    /// </summary>
    private static void InitializeCommandHandlers()
    {
        // 注册所有命令处理器
        CommandHandlers.Add("\\help", Command.HandleHelpCommand);
        CommandHandlers.Add("\\bot", Command.HandleBotCommand);
        CommandHandlers.Add("\\weather", Command.HandleWeatherCommand);
        CommandHandlers.Add("\\picture", Command.HandlePictureCommand);
    }

    /// <summary>
    /// 处理新消息的方法，由防抖计时器触发
    /// </summary>
    private static async Task ProcessNewMessageAsync()
    {
        if (_contentAreaPane == null || _conditionFactory == null) return;

        try
        {
            // 有新消息添加
            System.Console.WriteLine("\n----------- 检测到新消息！-----------");

            // 获取所有消息并处理最新的一个
            var currentMessages = _contentAreaPane.FindAllChildren(_conditionFactory.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).ToList();
            if (currentMessages.Count == 0) return;

            // 获取最新消息
            var lastMessage = currentMessages.Last();

            System.Console.WriteLine("--- 最新消息的结构 ---");
            UIHelper.PrintElementTree(lastMessage, 0);
            System.Console.WriteLine("---------------------------\n");

            // 消息主体内容直接就放在 name里面
            var messageContent = lastMessage.Name;

            // 查找匹配的命令处理器
            foreach (var commandPrefix in CommandHandlers.Keys)
            {
                if (messageContent.StartsWith(commandPrefix))
                {
                    // 提取命令后面的内容
                    var commandText = messageContent[commandPrefix.Length..].Trim();
                    System.Console.WriteLine($"检测到{commandPrefix}命令，参数: \"{commandText}\"");

                    // 调用对应的处理器
                    var response = await CommandHandlers[commandPrefix](commandText);

                    // 发送到微信
                    // 空消息微信是不会发出去的,这里可以不用判
                    await SendResponseToWeChatAsync(response);

                    return;
                }
            }

            System.Console.WriteLine("不需要处理\n");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"处理消息时发生错误: {ex.Message}");
            System.Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 发送响应消息到微信聊天框
    /// </summary>
    private static async Task SendResponseToWeChatAsync(string? response)
    {
        if (_inputEdit == null || string.IsNullOrEmpty(response)) return;

        try
        {
            // 特殊处理图片命令
            if (response == "##PICTURE##")
            {
                // 聚焦到输入框
                _inputEdit.Focus();

                // 等待一段时间确保焦点已设置
                await Task.Delay(1000);

                // 执行粘贴操作 (Ctrl+V)
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);
                FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);
                FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);

                // 等待粘贴操作完成
                await Task.Delay(1000);

                // 发送消息 (按回车键)
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
                FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
                FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);

                System.Console.WriteLine("已将图片发送到微信。");
                return;
            }

            // 使用 Enter 方法模拟键盘输入
            _inputEdit.Enter(response);

            // 增加延迟模仿人类操作
            await Task.Delay(1000);

            // 聚焦输入框并发送消息
            _inputEdit.Focus();
            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
            FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
            FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);

            System.Console.WriteLine("已将回复发送到微信。");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"发送消息时出错: {ex.Message}");
        }
    }
}
