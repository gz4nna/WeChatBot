using System.Drawing;
using System.Runtime.InteropServices;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using WeChatBot.Models;
using WeChatBot.Models.Settings;

using Application = FlaUI.Core.Application;

namespace WeChatBot.Console;


public partial class Program
{
    /// <summary>
    /// 配置对象
    /// </summary>
    private static IConfiguration? _configuration;
    /// <summary>
    /// 服务提供者
    /// </summary>
    private static IServiceProvider? _serviceProvider;
    /// <summary>
    /// 计时器，用于防抖处理
    /// </summary>
    private static Timer? _debounceTimer;


    // 存储UI元素的引用，供命令处理方法使用
    private static TextBox? _inputEdit;
    private static ConditionFactory? _conditionFactory;
    private static AutomationElement? _contentAreaPane;

    // 默认使用的微信客户端版本
    private static WeChatClientVersion? _wechatClientVersion = null;
    // 定义GetDpiForWindow函数
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    static async Task Main(string[] _)
    {
        // 配置依赖注入
        ConfigureServices();

        System.Console.WriteLine("正在查找微信进程...");

        System.Diagnostics.Process[]? processes = null;
        // 定义微信进程名数组，包含3.x和4.x版本的进程名
        string[] WeChatProcessName = ["WeChat", "weixin"];
        uint dpi = 96;

        // 遍历微信版本，尝试找到对应的进程, 现在只支持3和4)
        for (int v = 3; v <= 4; v++)
        {
            // 按照进程名获取微信进程
            processes = System.Diagnostics.Process.GetProcessesByName(WeChatProcessName[v - 3]);
            // 如果找到了对应版本的微信进程，则设置版本号
            if (processes.Length != 0)
            {
                _wechatClientVersion = (v == 3) ? WeChatClientVersion.WeChat3_x_x : WeChatClientVersion.WeChat4_x_x;
            }
        }

        // 如果没有找到任何微信进程，提示用户启动微信
        if (_wechatClientVersion is null)
        {
            System.Console.WriteLine("未找到微信进程，请确保微信已启动。");
            return;
        }

        System.Console.WriteLine($"找到 {processes.Length} 个微信进程;当前微信大版本编号为 {_wechatClientVersion}");

        Application? app = null;
        Window? mainWindow = null;

        using var automation = new UIA3Automation();

        // 遍历进程，找到拥有正确主窗口的那个
        // 这段不管是哪个版本的微信都是一样的
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
                    // 立刻获取窗口句柄
                    IntPtr wechatHwnd = window.Properties.NativeWindowHandle.Value;
                    // 获取该窗口所在的显示器的DPI
                    dpi = GetDpiForWindow(wechatHwnd);
                    break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"附加到进程 PID: {process.Id} 失败: {ex.Message}");
            }
        }

        // 4.0版本正在测试
        if (_wechatClientVersion == WeChatClientVersion.WeChat4_x_x)
        {
            // 获取主窗口的边界矩形
            var bounds = mainWindow?.BoundingRectangle;
            // 计算缩放倍率,原始dpi是96
            float scaleFactor = dpi / 96.0f;
            // 根据缩放倍率调整边界矩形
            bounds = new Rectangle(
                (int)(bounds.Value.Left * scaleFactor),
                (int)(bounds.Value.Top * scaleFactor),
                (int)(bounds.Value.Width * scaleFactor),
                (int)(bounds.Value.Height * scaleFactor)
            );

            // 创建一个bitmap对象，大小与窗口相同
            Bitmap screenshot = new Bitmap((int)bounds?.Width, (int)bounds?.Height);
            // 使用Graphics从屏幕捕获图像
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(
                    new Point(bounds.Value.Left, bounds.Value.Top),
                    Point.Empty,
                    bounds.Value.Size
                );
            }
            // 保存到桌面
            screenshot.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WeChatMainWindow.png"));
            // 加载"./TemplateImages/sendEmojiButtonImage.png"这张模板图片,这张图片是发送表情按钮的截图
            Bitmap templateImage = (Bitmap)Image.FromFile("./TemplateImages/sendEmojiButtonImage.png");
            // 进行模板匹配
            double threshold = 0.85; // 相似度阈值
            Rectangle? matchRect = FindTemplateMatch(screenshot, templateImage, threshold);

            if (matchRect != null)
            {
                // 计算目标控件的物理像素中心点
                Point targetPhysicalPoint = new Point(
                    matchRect.Value.X + matchRect.Value.Width / 2,
                    matchRect.Value.Y + matchRect.Value.Height / 2
                );

                // 将物理像素点转换为屏幕绝对坐标
                // 直接相加使用的是屏幕空间的坐标,但是模拟需要的是原始坐标
                Point screenPoint = new Point(
                    (int)((bounds.Value.X + targetPhysicalPoint.X) / scaleFactor),
                    (int)((bounds.Value.Y + targetPhysicalPoint.Y) / scaleFactor)
                );

                System.Console.WriteLine($"成功找到按钮，屏幕坐标为: ({screenPoint.X}, {screenPoint.Y})");

                // 6. 模拟鼠标点击
                System.Windows.Forms.Cursor.Position = screenPoint; // 移动鼠标

                // 接下来可以调用模拟点击的方法，比如 SendInput
                System.Threading.Thread.Sleep(50);
                MouseSimulator.DoLeftClick();
                System.Threading.Thread.Sleep(50);
            }
            else
            {
                System.Console.WriteLine("未找到目标按钮，请检查模板图片或调整阈值。");
            }
            // 4.0版本还在测试,先不继续执行了
            return;
        }

        // 3.0版本使用uia执行,直接走下去就好了

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

    /// <summary>
    /// 配置服务
    /// </summary>
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

    /// <summary>
    /// 在大图中查找模板图片的位置
    /// </summary>
    /// <param name="sourceImage">微信主窗口的截图</param>
    /// <param name="templateImage">要查找的模板图片</param>
    /// <param name="threshold">匹配相似度阈值，0到1之间，例如0.85</param>
    /// <returns>如果找到，返回匹配的矩形区域；否则返回null</returns>
    public static Rectangle? FindTemplateMatch(Bitmap sourceImage, Bitmap templateImage, double threshold)
    {
        // 确保图片不为空
        if (sourceImage == null || templateImage == null)
        {
            System.Console.WriteLine("源图片或模板图片为空。");
            return null;
        }

        // 将Bitmap对象转换为Emgu CV的Image对象
        // 使用using块确保资源被正确释放
        using var source = sourceImage.ToImage<Bgr, byte>();
        using var template = templateImage.ToImage<Bgr, byte>();

        // 创建一个结果图像，用于存储匹配结果
        using var result = new Image<Gray, float>(source.Width - template.Width + 1, source.Height - template.Height + 1);

        // 执行模板匹配
        // TemplateMatchingType.CcorrNormed 是最常用的方法，结果值在0到1之间，1表示完全匹配
        CvInvoke.MatchTemplate(source, template, result, TemplateMatchingType.CcorrNormed);

        // 寻找匹配度最高的点
        double minVal = 0, maxVal = 0;
        Point minLoc = Point.Empty, maxLoc = Point.Empty;
        CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

        // 如果最大匹配值超过设定的阈值，则认为找到匹配
        if (maxVal >= threshold)
        {
            // 匹配点是模板左上角的坐标，所以需要构建一个矩形
            Rectangle matchRect = new Rectangle(maxLoc, template.Size);
            return matchRect;
        }
        else
        {
            System.Console.WriteLine($"未找到匹配，最高相似度为：{maxVal}");
            return null;
        }
    }
}
