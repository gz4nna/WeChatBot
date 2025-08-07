using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using WeChatBot.Models.Settings;

namespace WeChatBot.Console;

public partial class Program
{
    private static IConfiguration? _configuration;
    private static IServiceProvider? _serviceProvider;
    /// <summary>
    /// 计时器，用于防抖处理
    /// </summary>
    private static Timer? _debounceTimer;


    // 存储UI元素的引用，供命令处理方法使用
    private static TextBox? _inputEdit;
    private static ConditionFactory? _conditionFactory;
    private static AutomationElement? _contentAreaPane;


    static async Task Main(string[] _)
    {
        // 配置依赖注入
        ConfigureServices();

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

            // 初始化UI元素
            if (!InitializeUIElements(mainWindow))
            {
                System.Console.WriteLine("初始化UI元素失败，请检查微信界面。");
                return;
            }

            System.Console.WriteLine("成功找到聊天内容窗格，正在注册新消息事件...");

            // 注册结构变化事件，当 ContentAreaPane 的子元素发生变化时触发
            _contentAreaPane?.RegisterStructureChangedEvent(FlaUI.Core.Definitions.TreeScope.Subtree, (element, eventId, treeChangeType) =>
            {
                // 因为去监控了 Subtree,所以每次一条消息会导致多次触发,需要防抖处理
                // 销毁并重置上一个计时器
                _debounceTimer?.Dispose();

                // 创建一个新计时器，在300毫秒后执行一次处理逻辑
                _debounceTimer = new Timer(async _ => await ProcessNewMessageAsync(), null, 300, Timeout.Infinite);
            });

            // 初始化命令处理器
            InitializeCommandHandlers();

            // 所有初始化完成后，输出提示信息
            await SendResponseToWeChatAsync("WeChatBot已启动,现在可以进行对话,输入 \\help 获取使用帮助~");

            System.Console.WriteLine("事件监听已启动。程序将保持运行以接收新消息。按 Enter 键退出。");

            await Task.Run(() => System.Console.ReadLine());

            // 退出前也输出提示信息
            await SendResponseToWeChatAsync("WeChatBot已关闭,希望帮助到了你~");

            _debounceTimer?.Dispose();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"发生错误: {ex.Message}");
            System.Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 初始化UI元素
    /// </summary>
    private static bool InitializeUIElements(Window mainWindow)
    {
        _conditionFactory = new ConditionFactory(new UIA3PropertyLibrary());

        // 有时候会在主窗口前面加一些别的,比如提供阴影的之类的,我们取最后一个就是平时用的那个窗口
        var WeChatMainWndForPCDefaultPane = mainWindow
            .FindChildAt(mainWindow.FindAllChildren().Length - 1)
            ?.FindFirstChild();

        if (WeChatMainWndForPCDefaultPane == null) return false;

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
        _contentAreaPane = ContentAreaAndInputAreaPane
            ?.FindFirstChild()
            ?.FindFirstChild()
            ?.FindFirstChild();

        // 包含输入框和发送按钮,0是表情等按钮
        var InputAreaPane = ContentAreaAndInputAreaPane
            ?.FindChildAt(1)
            ?.FindChildAt(1)
            ?.FindFirstChild();

        // 输入框是个Edit控件,为了方便输入给处理成 TextBox
        _inputEdit = InputAreaPane
            ?.FindChildAt(1)
            ?.FindFirstChild()
            .AsTextBox();

        return _contentAreaPane != null && _inputEdit != null;
    }

    private static void ConfigureServices()
    {
        // 如果不存在 appsettings.json,创建一个并将默认配置写入
        if (!File.Exists("appsettings.json"))
        {
            var defaultConfig = new BotSettings();

            // 创建序列化选项，设置为保留中文字符而不是转义
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                // 以下设置确保中文字符不会被转义为 Unicode
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
            };

            // 要添加根节点 BotSettings否则识别不出来的话直接按照默认值进去了
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(
                new { BotSettings = new BotSettings() },
                options
            );

            File.WriteAllText("appsettings.json", jsonContent, System.Text.Encoding.UTF8);

            System.Console.WriteLine("已创建默认配置文件 appsettings.json");
        }

        // 创建配置
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // 创建服务集合
        var services = new ServiceCollection();

        // 注册配置
        services.AddSingleton(_configuration);

        // 前面的 BotSettings
        var botSettings = _configuration.GetSection("BotSettings").Get<BotSettings>();
        if (botSettings == null)
        {
            botSettings = new BotSettings();
            System.Console.WriteLine("BotSettings 配置未找到，使用默认配置。");
        }

        // 集合类型默认采用添加（Add）而不是替换（Replace）的策略
        // 所以先让 CommandsList里面是空的,然后再添加默认值
        if (botSettings != null &&
            botSettings.CommandParams?.HelpParams?.CommandsList != null &&
            botSettings.CommandParams.HelpParams.CommandsList.Count == 0)
        {
            botSettings.CommandParams.HelpParams.ResetToDefaults();
        }

        services.AddSingleton(botSettings);

        // 创建服务提供者
        _serviceProvider = services.BuildServiceProvider();

        // 先拿来自己用
        _settings = _serviceProvider.GetRequiredService<BotSettings>();
    }

    // 获取服务的辅助方法
    public static T GetService<T>() where T : class
    {
        return _serviceProvider!.GetRequiredService<T>();
    }
}
