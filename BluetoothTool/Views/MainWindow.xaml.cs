using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using BluetoothTool.ViewModels;

namespace BluetoothTool.Views;

/// <summary>
/// 主窗口代码后置
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 非客户区命中测试消息
    /// </summary>
    private const int WM_NCHITTEST = 0x0084;

    // 命中测试返回值：各方向调整大小
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    /// <summary>
    /// 窗口边缘判定为缩放区域的宽度（像素）
    /// </summary>
    private const int ResizeBorderSize = 8;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // 订阅窗口状态变化，用于切换最大化/还原图标及调整边框
        StateChanged += OnWindowStateChanged;
    }

    /// <summary>
    /// 窗口句柄创建完成后挂接消息处理，实现无边框窗口的自定义缩放
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // 获取窗口句柄并挂接消息钩子
        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
    }

    /// <summary>
    /// 处理窗口消息：命中测试时返回对应缩放方向
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // 仅处理非客户区命中测试，且最大化时无需缩放
        if (msg != WM_NCHITTEST || WindowState == WindowState.Maximized)
        {
            return IntPtr.Zero;
        }

        // 解析鼠标屏幕坐标（低 16 位为 X，高 16 位为 Y）
        var x = (short)(lParam.ToInt64() & 0xFFFF);
        var y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        var pos = PointFromScreen(new Point(x, y));

        // 计算窗口四边位置
        var left = Left;
        var top = Top;
        var right = Left + ActualWidth;
        var bottom = Top + ActualHeight;

        // 判断鼠标是否落在各方向边缘
        var onLeft = pos.X >= left && pos.X < left + ResizeBorderSize;
        var onRight = pos.X > right - ResizeBorderSize && pos.X <= right;
        var onTop = pos.Y >= top && pos.Y < top + ResizeBorderSize;
        var onBottom = pos.Y > bottom - ResizeBorderSize && pos.Y <= bottom;

        // 优先返回角落方向，再返回四边方向
        if (onTop && onLeft)
        {
            handled = true;
            return new IntPtr(HTTOPLEFT);
        }

        if (onTop && onRight)
        {
            handled = true;
            return new IntPtr(HTTOPRIGHT);
        }

        if (onBottom && onLeft)
        {
            handled = true;
            return new IntPtr(HTBOTTOMLEFT);
        }

        if (onBottom && onRight)
        {
            handled = true;
            return new IntPtr(HTBOTTOMRIGHT);
        }

        if (onLeft)
        {
            handled = true;
            return new IntPtr(HTLEFT);
        }

        if (onRight)
        {
            handled = true;
            return new IntPtr(HTRIGHT);
        }

        if (onTop)
        {
            handled = true;
            return new IntPtr(HTTOP);
        }

        if (onBottom)
        {
            handled = true;
            return new IntPtr(HTBOTTOM);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 标题栏鼠标左键按下：支持拖动窗口与双击最大化
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 仅响应左键
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        // 双击切换最大化/还原
        if (e.ClickCount == 2)
        {
            MaximizeButton_Click(sender, e);
            return;
        }

        // 最大化状态下不响应拖动
        if (WindowState == WindowState.Maximized)
        {
            return;
        }

        // 拖动窗口
        DragMove();
    }

    /// <summary>
    /// 最小化窗口
    /// </summary>
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// 最大化或还原窗口
    /// </summary>
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 窗口状态变化时切换最大化/还原图标，并调整边框、圆角与阴影
    /// </summary>
    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        var isMaximized = WindowState == WindowState.Maximized;
        if (MaximizeIcon != null)
        {
            MaximizeIcon.Visibility = isMaximized ? Visibility.Collapsed : Visibility.Visible;
        }

        if (RestoreIcon != null)
        {
            RestoreIcon.Visibility = isMaximized ? Visibility.Visible : Visibility.Collapsed;
        }

        // 最大化时去除留白、圆角与阴影，使内容铺满屏幕
        if (WindowRootBorder != null && WindowShadow != null)
        {
            if (isMaximized)
            {
                WindowRootBorder.Margin = new Thickness(0);
                WindowRootBorder.CornerRadius = new CornerRadius(0);
                WindowShadow.Opacity = 0;
            }
            else
            {
                WindowRootBorder.Margin = new Thickness(16);
                WindowRootBorder.CornerRadius = new CornerRadius(8);
                WindowShadow.Opacity = 0.4;
            }
        }
    }

    /// <summary>
    /// 窗口关闭时释放视图模型资源
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        // 释放蓝牙连接与音频服务
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Dispose();
        }

        base.OnClosing(e);
    }
}
