using System.ComponentModel;
using System.Windows;
using BluetoothTool.ViewModels;

namespace BluetoothTool.Views;

/// <summary>
/// 主窗口代码后置
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // 订阅窗口状态变化，用于切换最大化/还原图标
        StateChanged += OnWindowStateChanged;
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
    /// 窗口状态变化时切换最大化/还原图标
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
