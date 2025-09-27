using System.Runtime.InteropServices;

/// <summary>
/// 鼠标模拟器
/// </summary>
public static class MouseSimulator
{
    // 定义常量
    private const int INPUT_MOUSE = 0;
    private const int MOUSEEVENTF_LEFTDOWN = 0x0002; // 模拟鼠标左键按下
    private const int MOUSEEVENTF_LEFTUP = 0x0004;   // 模拟鼠标左键抬起

    // INPUT 包含鼠标、键盘、硬件等事件
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public InputUnion u;
    }

    // 包含各种输入类型
    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        // [FieldOffset(0)]
        // public KEYBDINPUT ki;
        // [FieldOffset(0)]
        // public HARDWAREINPUT hi;
    }

    // 定义 MOUSEINPUT 结构体
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // 引入 SendInput 函数
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// 模拟一次鼠标左键点击
    /// </summary>
    public static void DoLeftClick()
    {
        INPUT[] inputs = new INPUT[2];

        // 鼠标左键按下
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

        // 鼠标左键抬起
        inputs[1].type = INPUT_MOUSE;
        inputs[1].u.mi.dwFlags = MOUSEEVENTF_LEFTUP;

        // 发送输入事件
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }
}