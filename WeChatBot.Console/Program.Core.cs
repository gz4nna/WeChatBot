using WeChatBot.Console.Commands;
using WeChatBot.Console.Helpers;

namespace WeChatBot.Console;

public partial class Program
{
    // 命令前缀和处理器映射字典
    private static readonly Dictionary<string, Func<string, Task<string?>>> commandHandlers = new();
    private static readonly SemaphoreSlim _processingLock = new(1, 1);

    /// <summary>
    /// 初始化命令处理器
    /// </summary>
    private static void InitializeCommandHandlers()
    {
        // 注册所有命令处理器
        commandHandlers.Add("\\help", Command.HandleHelpCommand);
        commandHandlers.Add("\\bot", Command.HandleBotCommand);
        // commandHandlers.Add("\\weather", Command.HandleWeatherCommand);
        commandHandlers.Add("\\picture", Command.HandlePictureCommand);
    }

    /// <summary>
    /// 处理新消息的方法，由防抖计时器触发
    /// </summary>
    private static async Task ProcessNewMessageAsync()
    {
        // 尝试获取锁，如果已有处理过程在进行中则立即返回
        if (!await _processingLock.WaitAsync(0))
        {
            System.Console.WriteLine("已有消息处理过程在进行中，跳过此次处理。");
            return;
        }

        try
        {
            // 有新消息添加
            System.Console.WriteLine("\n----------- 检测到新消息！-----------");

            // 获取所有消息并处理最新的一个
            var currentMessages = _contentAreaPane?.FindAllChildren(condition: _conditionFactory.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).ToList();
            if (currentMessages?.Count == 0) return;

            // 获取最新消息
            var lastMessage = currentMessages?.Last();

            System.Console.WriteLine("--- 最新消息的结构 ---");
            UIHelper.PrintElementTree(lastMessage, 0);
            System.Console.WriteLine("---------------------------\n");

            try
            {
                // 消息主体内容直接就放在 name里面
                var messageContent = lastMessage?.Name;

                if (string.IsNullOrEmpty(messageContent)) throw new Exception("消息内容为空或未找到,按照特殊消息处理");

                // 查找匹配的命令处理器
                foreach (var commandPrefix in commandHandlers.Keys)
                {
                    if (messageContent.StartsWith(commandPrefix))
                    {
                        // 提取命令后面的内容
                        var commandText = messageContent[commandPrefix.Length..].Trim();
                        System.Console.WriteLine($"检测到{commandPrefix}命令，参数: \"{commandText}\"");

                        // 调用对应的处理器
                        var response = await commandHandlers[commandPrefix](commandText);

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
                // 无论什么消息都有一个子元素,三个孙元素
                var specialMessage = lastMessage?.FindFirstChild();

                // 第一个孙元素不知道干嘛
                var firstChild = specialMessage?.FindChildAt(0);
                // 第二个孙元素是消息主体
                var secondChild = specialMessage?.FindChildAt(1);
                // 第三个孙元素是发送人
                var thirdChild = specialMessage?.FindChildAt(2);

                // 如果子元素孙元素都是 null,应该是撤回的消息
                if (firstChild == null && secondChild == null && thirdChild == null)
                {
                    System.Console.WriteLine("检测到撤回消息");
                    await SendResponseToWeChatAsync("[自动回复]大胆!撤回了什么,让我看看");
                    return;
                }


                // "拍一拍"功能的处理
                // "拍一拍"的 secondChild是没有 name内容的,可以一直往下看
                var PatMessage = secondChild?.FindFirstChild()?.FindFirstChild();
                if (PatMessage != null && PatMessage.Name.Contains("拍了拍"))
                {
                    var patContent = string.Empty;
                    // 我被拍的情况
                    if (PatMessage.Name.Contains("拍了拍我"))
                    {
                        // 取 拍了拍我 后面的内容
                        patContent = PatMessage.Name[(PatMessage.Name.IndexOf("拍了拍我") + "拍了拍我".Length)..].Trim();
                    }
                    else if (PatMessage.Name.Contains("拍了拍自己"))
                    {
                        // 取 拍了拍自己 后面的内容
                        patContent = PatMessage.Name[(PatMessage.Name.IndexOf("拍了拍自己") + "拍了拍自己".Length)..].Trim();
                    }
                    // 其他人被拍,谁拍的不需要管
                    else
                    {
                        // 拍一拍有两种符号,一种是双引号,一种是中文引号
                        if (PatMessage.Name.Contains("拍了拍\""))
                        {
                            // 先把 拍了拍 和它前面的内容去掉
                            patContent = PatMessage.Name[(PatMessage.Name.IndexOf("拍了拍\"") + "拍了拍\"".Length)..].Trim();
                            // 这时候还有 名字+引号+内容
                            // 取引号后面的内容
                            patContent = patContent[(patContent.IndexOf('\"') + 1)..].Trim();
                        }
                        // 处理同理
                        else if (PatMessage.Name.Contains("拍了拍「"))
                        {
                            // 先把 拍了拍 和它前面的内容去掉
                            patContent = PatMessage.Name[(PatMessage.Name.IndexOf("拍了拍「") + "拍了拍「".Length)..].Trim();
                            // 这时候还有 名字+引号+内容
                            // 取引号后面的内容
                            patContent = patContent[(patContent.IndexOf('」') + 1)..].Trim();
                        }
                        else
                        {
                            patContent = "你这个消息我识别出错了,算了这次允许你拍";
                        }
                    }

                    System.Console.WriteLine($"检测到拍一拍消息，内容为: {PatMessage.Name}");

                    // 发送回复时使用更长的延迟
                    await Task.Delay(2000); // 增加到2秒
                    await SendResponseToWeChatAsync($"[自动回复]绝对不许{patContent}");

                    // 发送后额外等待以确保微信UI完成更新
                    await Task.Delay(3000);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"处理消息时发生错误: {ex.Message}");
            System.Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            // 总是释放锁
            _processingLock.Release();
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

            // response过长,需要按照1000字进行截断处理,分批发送,避免微信将多余内容舍弃
            const int maxLength = 1000;
            if (response.Length > maxLength)
            {
                var parts = new List<string>();
                for (int i = 0; i < response.Length; i += maxLength)
                {
                    parts.Add(response.Substring(i, Math.Min(maxLength, response.Length - i)));
                }
                foreach (var part in parts)
                {
                    await SendResponseToWeChatAsync(part);
                }
                return;
            }

            // 使用 Enter 方法模拟键盘输入
            _inputEdit.Enter($"WeChatBot如是说:\n{response}");

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
