using System.Drawing;
using System.Windows.Forms;

namespace WeChatBot.Console.Commands;

public static partial class Command
{
    /// <summary>
    /// 处理 \picture 命令，发送图片到聊天
    /// </summary>
    public static async Task<string?> HandlePictureCommand(string commandText)
    {
        try
        {
            System.Console.WriteLine($"处理Picture命令，参数: \"{commandText}\"");

            // 默认图片文件夹
            string imageFolder = "E:\\ARCHIVE\\Image\\Other\\ForBot";

            // 验证文件夹是否存在
            if (!Directory.Exists(imageFolder))
            {
                System.Console.WriteLine($"图片文件夹不存在: {imageFolder}");
                return "图片文件夹不存在，请检查配置。";
            }

            // 获取文件夹中随机一张图片
            var imageFiles = Directory.GetFiles(imageFolder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (imageFiles.Count == 0)
            {
                // 文件夹中没有符合条件的图片
                System.Console.WriteLine("图片文件夹中没有符合条件的图片。");
            }
            var random = new Random();
            var imagePath = imageFiles[random.Next(imageFiles.Count)];

            // 加载图片并放入剪贴板，使用 STA 线程
            using var bitmap = new Bitmap(imagePath);
            var clipboardSuccess = false;

            // 创建一个 STA 线程来处理剪贴板操作
            var staThread = new Thread(() =>
            {
                try
                {
                    Clipboard.SetImage(bitmap);
                    clipboardSuccess = true;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"在 STA 线程中设置剪贴板时出错: {ex.Message}");
                }
            });

            // 设置线程为 STA 模式
            staThread.SetApartmentState(ApartmentState.STA);

            // 启动并等待线程完成
            staThread.Start();
            staThread.Join();

            if (!clipboardSuccess)
            {
                return "复制图片到剪贴板失败。";
            }

            System.Console.WriteLine($"图片已复制到剪贴板: {imagePath}");

            // 返回特殊标记，表示需要执行粘贴操作
            return "##PICTURE##";
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"处理图片命令时出错: {ex.Message}");
            return "处理图片命令时出错，请稍后再试。";
        }
    }
}
