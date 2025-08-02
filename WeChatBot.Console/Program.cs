using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace WeChatBot.Console;

public class Program
{
    /// <summary>
    /// 计时器，用于防抖处理
    /// </summary>
    private static Timer? _debounceTimer;
    /// <summary>
    /// HttpClient设置超时时间为 10 分钟,因为我本地推理速度有点慢
    /// </summary>
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    static async Task Main(string[] _)
    {
        System.Console.WriteLine("正在查找微信进程...");

        // 按照进程名获取微信进程
        var processes = System.Diagnostics.Process.GetProcessesByName("WeChat");

        if (processes.Length == 0)
        {
            System.Console.WriteLine("未找到微信进程，请确保微信已启动。");
            return;
        }
        System.Console.WriteLine($"找到 {processes.Length} 个微信进程。");

        Application? app = null;
        Window? mainWindow = null;

        using var automation = new UIA3Automation();

        // 遍历进程，找到拥有正确主窗口的那个
        foreach (var process in processes)
        {
            if (process.MainWindowHandle == IntPtr.Zero) continue;

            System.Console.WriteLine($"找到微信主窗口，进程 ID: {process.Id}");

            try
            {
                // 尝试附加到微信应用程序
                app = Application.Attach(process);

                // 获取主窗口
                var window = app.GetMainWindow(automation);

                if (window != null && window.Name == "微信")
                {
                    mainWindow = window;
                    System.Console.WriteLine("成功获取微信主窗口。");
                    break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"附加到进程 PID: {process.Id} 失败: {ex.Message}");
            }
        }

        if (mainWindow == null)
        {
            System.Console.WriteLine("错误: 遍历了所有微信进程，但未能找到符合条件的主窗口。");
            return;
        }

        try
        {
            System.Console.WriteLine("----------- 开始自动化操作 -----------");

            var cf = new ConditionFactory(new UIA3PropertyLibrary());

            // 有时候会在主窗口前面加一些别的,比如提供阴影的之类的,我们取最后一个就是平时用的那个窗口
            // 有些只存在一个子元素的就直接跳着取了,后面同理
            var WeChatMainWndForPCDefaultPane = mainWindow
                .FindChildAt(mainWindow.FindAllChildren().Length - 1)
                ?.FindFirstChild();

            if (WeChatMainWndForPCDefaultPane == null)
            {
                System.Console.WriteLine("未找到主要窗口，请检查微信界面。");
                return;
            }
            System.Console.WriteLine("找到主要窗口，正在寻找群组列表...");

            // 从左到右分三部分,0侧边导航,1群聊列表,2聊天内容
            var WeChatMainWndForPCDefaultRightPane = WeChatMainWndForPCDefaultPane.FindChildAt(2);
            // 这里直接全跳了,都是只有一个子元素的
            var GroupPane = WeChatMainWndForPCDefaultRightPane
                ?.FindFirstChild()
                ?.FindFirstChild()
                ?.FindFirstChild()
                ?.FindFirstChild();

            // 第一个是顶上的群名字这些内容,第二个是聊天内容和输入框这些
            var ContentAreaAndInputAreaPane = GroupPane?.FindChildAt(1);

            // 所有消息的列表
            var ContentAreaPane = ContentAreaAndInputAreaPane
                ?.FindFirstChild()
                ?.FindFirstChild()
                ?.FindFirstChild();

            // 包含输入框和发送按钮,0是表情等按钮
            var InputAreaPane = ContentAreaAndInputAreaPane
                ?.FindChildAt(1)
                ?.FindChildAt(1)
                ?.FindFirstChild();

            // 输入框是个Edit控件,为了方便输入给处理成 TextBox
            var inputEdit = InputAreaPane
                ?.FindChildAt(1)
                ?.FindFirstChild()
                .AsTextBox();
            // 直接使用按钮容易被下线,后面这个就不用了(可能需要模拟地好一点?)
            var sendButton = InputAreaPane
                ?.FindChildAt(2)
                ?.FindChildAt(2)
                ?.FindFirstChild()
                .AsButton();

            //System.Console.WriteLine("----------- UI 树打印开始 -----------");

            //// 使用辅助函数打印 InputAreaPane 的 UI 树
            //PrintElementTree(InputAreaPane, 0);

            //System.Console.WriteLine("----------- UI 树打印完毕 -----------");

            System.Console.WriteLine("成功找到聊天内容窗格，正在注册新消息事件...");

            // 注册结构变化事件，当 ContentAreaPane 的子元素发生变化时触发
            // 一般来说新消息到来时 treeChangeType 第一个元素是 42,但是我还不确定是否是通用的( *︾▽︾)
            ContentAreaPane?.RegisterStructureChangedEvent(FlaUI.Core.Definitions.TreeScope.Subtree, (element, eventId, treeChangeType) =>
            {
                // 因为去监控了 Subtree,所以每次一条消息会导致多次触发,需要防抖处理
                // 我在自己的客户端上看是大约 100到200毫秒之间会触发一次,一共 3次,所以这里设置了 300 毫秒的防抖时间,这个要根据自己情况调整

                // 销毁并重置上一个计时器
                _debounceTimer?.Dispose();

                // 创建一个新计时器，在300毫秒后执行一次处理逻辑
                _debounceTimer = new Timer(async _ =>
                {
                    // 有新消息添加
                    System.Console.WriteLine("\n----------- 检测到新消息！-----------");
                    // 事件触发后，重新获取所有消息并处理最新的一个
                    var currentMessages = ContentAreaPane.FindAllChildren(cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).ToList();
                    if (currentMessages.Count != 0)
                    {
                        // 新来的消息太密集会导致这里处理可能丢掉几个,让群友闭嘴一会就好~(￣▽￣)~*
                        var lastMessage = currentMessages.Last();

                        System.Console.WriteLine("--- 最新消息的结构 ---");
                        PrintElementTree(lastMessage, 0);
                        System.Console.WriteLine("---------------------------\n");

                        // 消息主体内容直接就放在 name里面
                        var messageContent = lastMessage.Name;
                        // 我用开头携带关键词来判断是否需要处理,要换判断规则的改这边
                        if (messageContent.StartsWith("\\bot"))
                        {
                            // 提取 "\bot" 后面的内容
                            var text = messageContent[4..].Trim();
                            System.Console.WriteLine($"需要处理，提取的文本内容: \"{text}\"");

                            // 发送到大模型并获取回复
                            var response = await GetModelResponseAsync(text);
                            System.Console.WriteLine($"模型回复: \"{response}\"");

                            // 模拟发送回复到微信
                            if (!string.IsNullOrEmpty(response))
                            {
                                // 使用 Enter 方法模拟键盘输入,需要提前换成英文输入法,不然碰到洋文就会出现古神低语
                                // 小尾巴啥的在这里塞进去(或者去调教ai)
                                response = "[自动回复]" + response;
                                inputEdit?.Enter(response);

                                // 增加一个短暂的延迟，以模仿人类操作习惯
                                // 操作太快会被强制下线
                                await Task.Delay(1000);

                                // 我一开始用点击按钮,可能是鼠标有瞬移也被下线了几次
                                // 如果你的模型返回消息中有回车,那么需要和我一样用 Ctrl + Enter 来发送消息
                                inputEdit?.Focus();
                                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);
                                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
                                FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.RETURN);
                                FlaUI.Core.Input.Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL);

                                System.Console.WriteLine("已将模型回复发送到微信。");
                            }
                        }
                        else
                        {
                            System.Console.WriteLine("不需要处理\n");
                        }

                    }
                }, null, 300, Timeout.Infinite);
            });
            System.Console.WriteLine("事件监听已启动。程序将保持运行以接收新消息。按 Enter 键退出。");

            await Task.Run(() => System.Console.ReadLine());

            _debounceTimer?.Dispose();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"发生错误: {ex.Message}");
            System.Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 发送消息到大语言模型并获取回复 (使用 chat/completions 端点)
    /// </summary>
    private static async Task<string> GetModelResponseAsync(string prompt)
    {
        // 总之调用的地址在这里改
        var apiUrl = "http://localhost:5000/v1/chat/completions";

        var requestData = new Dictionary<string, object>
        {
            { "messages", new[] { new { role = "user", content = prompt } } },
            { "mode", "instruct" },
            // 模板名放这里
            //{ "instruction_template", "" }, 
            // 角色放这里
            //{ "character", "" }, 
            //{ "temperature", 0.7 },
            //{ "top_p", 0.8 },
            //{ "top_k", 20 }
        };

        try
        {
            var response = await HttpClient.PostAsJsonAsync(apiUrl, requestData);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            // 解析 JSON 响应
            using var jsonDoc = JsonDocument.Parse(responseBody);
            var choices = jsonDoc.RootElement.GetProperty("choices");
            var firstChoice = choices[0];
            var message = firstChoice.GetProperty("message");
            var generatedText = message.GetProperty("content").GetString();

            // 如果响应不为空，则尝试移除 <think>...</think> 标签,我们只需要输出内容就好了
            if (!string.IsNullOrEmpty(generatedText))
            {
                const string thinkTagEnd = "</think>";
                var thinkTagIndex = generatedText.LastIndexOf(thinkTagEnd, StringComparison.OrdinalIgnoreCase);
                if (thinkTagIndex != -1)
                {
                    // 提取 </think> 标签之后的内容并移除前后的空白字符
                    return generatedText[(thinkTagIndex + thinkTagEnd.Length)..].Trim();
                }
            }

            return generatedText ?? "未能从模型响应中提取文本。";
        }
        catch (HttpRequestException e)
        {
            return $"请求错误: {e.Message}";
        }
        catch (Exception e)
        {
            return $"处理模型响应时发生错误: {e.Message}";
        }
    }


    /// <summary>
    /// 递归打印UI元素的树状结构
    /// </summary>
    /// <param name="element">要开始打印的根元素</param>
    /// <param name="indent">缩进级别</param>
    public static void PrintElementTree(AutomationElement element, int indent)
    {
        var indentStr = new string(' ', indent * 4);
        var sb = new StringBuilder();
        sb.Append(indentStr);

        // 安全地获取每个属性
        sb.Append($"Name: '{GetSafeString(() => element.Name)}', ");
        sb.Append($"ClassName: '{GetSafeString(() => element.ClassName)}', ");
        sb.Append($"AutomationId: '{GetSafeString(() => element.AutomationId)}', ");
        sb.Append($"ControlType: '{GetSafeString(() => element.ControlType)}'");

        System.Console.WriteLine(sb.ToString());

        try
        {
            // 查找所有直接子元素并递归打印
            var children = element.FindAllChildren();
            foreach (var child in children)
            {
                PrintElementTree(child, indent + 1);
            }
        }
        catch (Exception ex)
        {
            // 在某些情况下，访问子元素可能会失败，尤其是在UI快速变化时
            System.Console.WriteLine($"{indentStr}    [无法获取子元素: {ex.Message}]");
        }
    }

    /// <summary>
    /// 安全地获取一个属性的字符串表示形式，如果不支持则返回提示信息。
    /// </summary>
    private static string GetSafeString<T>(Func<T> getter)
    {
        try
        {
            return getter()?.ToString() ?? "null";
        }
        catch (FlaUI.Core.Exceptions.PropertyNotSupportedException)
        {
            return "[Not Supported]";
        }
    }
}
