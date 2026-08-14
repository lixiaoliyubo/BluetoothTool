using System.Windows;
using BluetoothTool.Models;
using BluetoothTool.Services;
using BluetoothTool.ViewModels;
using BluetoothTool.Views;

namespace BluetoothTool;

/// <summary>
/// 应用程序入口
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 应用启动时初始化服务、视图模型与主窗口
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 创建服务实例
        IBluetoothService bluetoothService = new BluetoothService();
        IAudioService audioService = new AudioService();
        IThemeService themeService = new ThemeService();

        // 应用默认主题（浅色 + 蓝色强调色）
        themeService.ApplyTheme(AppTheme.Light, AccentColor.Blue);

        // 创建主视图模型并注入服务
        var mainViewModel = new MainViewModel(bluetoothService, audioService, themeService);

        // 创建主窗口并绑定视图模型
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show();
    }
}
