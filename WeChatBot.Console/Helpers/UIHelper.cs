using System.Text;

using FlaUI.Core.AutomationElements;

namespace WeChatBot.Console.Helpers;

public class UIHelper
{
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
