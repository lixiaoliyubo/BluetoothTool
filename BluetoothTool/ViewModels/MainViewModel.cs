using System.Collections.ObjectModel;
using System.Windows;
using BluetoothTool.Models;
using BluetoothTool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BluetoothTool.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// 日志最大保留条数，避免长时间运行占用过多内存
    /// </summary>
    private const int MaxLogCount = 500;

    private readonly IBluetoothService _bluetoothService;
    private readonly IAudioService _audioService;
    private readonly IThemeService _themeService;

    /// <summary>
    /// 蓝牙设备列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<BluetoothDeviceInfo> devices = new();

    /// <summary>
    /// 当前选中的蓝牙设备
    /// </summary>
    [ObservableProperty]
    private BluetoothDeviceInfo? selectedDevice;

    /// <summary>
    /// 消息日志列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<BluetoothMessage> messages = new();

    /// <summary>
    /// 待发送消息输入框内容
    /// </summary>
    [ObservableProperty]
    private string inputMessage = string.Empty;

    /// <summary>
    /// 是否已连接
    /// </summary>
    [ObservableProperty]
    private bool isConnected;

    /// <summary>
    /// 是否正在扫描
    /// </summary>
    [ObservableProperty]
    private bool isScanning;

    /// <summary>
    /// 连接状态文本
    /// </summary>
    [ObservableProperty]
    private string statusText = "未连接";

    /// <summary>
    /// 当前音乐文件路径
    /// </summary>
    [ObservableProperty]
    private string musicFilePath = string.Empty;

    /// <summary>
    /// 配对 PIN 码
    /// </summary>
    [ObservableProperty]
    private string pin = "1234";

    /// <summary>
    /// 可选主题列表
    /// </summary>
    public IReadOnlyList<ThemeOption> Themes { get; } = new List<ThemeOption>
    {
        new() { Name = "浅色", Value = AppTheme.Light },
        new() { Name = "深色", Value = AppTheme.Dark }
    };

    /// <summary>
    /// 可选强调色列表
    /// </summary>
    public IReadOnlyList<AccentOption> Accents { get; } = new List<AccentOption>
    {
        new() { Name = "蓝色", Value = AccentColor.Blue },
        new() { Name = "绿色", Value = AccentColor.Green },
        new() { Name = "紫色", Value = AccentColor.Purple },
        new() { Name = "橙色", Value = AccentColor.Orange }
    };

    /// <summary>
    /// 当前选中的主题
    /// </summary>
    [ObservableProperty]
    private ThemeOption? selectedTheme;

    /// <summary>
    /// 当前选中的强调色
    /// </summary>
    [ObservableProperty]
    private AccentOption? selectedAccent;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MainViewModel(IBluetoothService bluetoothService, IAudioService audioService, IThemeService themeService)
    {
        _bluetoothService = bluetoothService;
        _audioService = audioService;
        _themeService = themeService;

        // 订阅蓝牙服务事件
        _bluetoothService.MessageReceived += OnMessageReceived;
        _bluetoothService.ConnectionStateChanged += OnConnectionStateChanged;

        // 初始化主题选择（与 App 启动时应用的主题保持一致）
        SelectedTheme = Themes[0];
        SelectedAccent = Accents[0];
    }

    /// <summary>
    /// 是否正在播放音乐
    /// </summary>
    public bool IsMusicPlaying => _audioService.IsPlaying;

    /// <summary>
    /// 扫描并刷新设备列表
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshDevicesAsync()
    {
        IsScanning = true;
        try
        {
            Devices.Clear();
            AddSystemMessage("正在扫描蓝牙设备...");

            // 获取附近经典蓝牙设备
            var devices = await _bluetoothService.GetDevicesAsync();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            AddSystemMessage($"扫描完成，共发现 {Devices.Count} 个设备");

            // 未发现设备时给出排查提示
            if (Devices.Count == 0)
            {
                AddSystemMessage("未发现设备，请确认：蓝牙已开启、设备已通电并处于可被发现/可配对状态，或先在 Windows 蓝牙设置中完成配对");
            }
        }
        catch (Exception ex)
        {
            AddSystemMessage($"扫描失败：{ex.Message}");
        }
        finally
        {
            IsScanning = false;
        }
    }

    /// <summary>
    /// 判断是否可以刷新设备列表
    /// </summary>
    private bool CanRefresh() => !IsScanning;

    /// <summary>
    /// 连接选中的蓝牙设备
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        if (SelectedDevice == null)
        {
            return;
        }

        try
        {
            AddSystemMessage($"正在连接 {SelectedDevice.Name} ...");
            await _bluetoothService.ConnectAsync(SelectedDevice, Pin);
            AddSystemMessage($"已连接 {SelectedDevice.Name}");
        }
        catch (Exception ex)
        {
            AddSystemMessage($"连接失败：{FormatError(ex)}");
        }
    }

    /// <summary>
    /// 将异常转换为可读的错误信息
    /// </summary>
    private static string FormatError(Exception ex)
    {
        // 有可读信息时直接返回
        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            return ex.Message;
        }

        // WinRT/COM 异常通常 Message 为空，补充 HRESULT 便于排查
        if (ex.HResult != 0)
        {
            return $"{ex.GetType().Name}(0x{ex.HResult:X8})：请确认设备已配对且 PIN 码正确";
        }

        return ex.GetType().Name;
    }

    /// <summary>
    /// 判断是否可以连接设备
    /// </summary>
    private bool CanConnect() => !IsConnected && SelectedDevice != null && !IsScanning;

    /// <summary>
    /// 断开当前蓝牙连接
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        try
        {
            await _bluetoothService.DisconnectAsync();
            AddSystemMessage("已断开连接");
        }
        catch (Exception ex)
        {
            AddSystemMessage($"断开失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 判断是否可以断开连接
    /// </summary>
    private bool CanDisconnect() => IsConnected;

    /// <summary>
    /// 发送消息
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
        {
            return;
        }

        try
        {
            // 发送并记录发送日志
            await _bluetoothService.SendAsync(InputMessage);
            AddMessage(new BluetoothMessage
            {
                Direction = MessageDirection.Sent,
                Content = InputMessage,
                Timestamp = DateTime.Now
            });

            InputMessage = string.Empty;
        }
        catch (Exception ex)
        {
            AddSystemMessage($"发送失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 判断是否可以发送消息
    /// </summary>
    private bool CanSend() => IsConnected && !string.IsNullOrWhiteSpace(InputMessage);

    /// <summary>
    /// 清空消息日志
    /// </summary>
    [RelayCommand]
    private void ClearLog()
    {
        Messages.Clear();
    }

    /// <summary>
    /// 打开音乐文件
    /// </summary>
    [RelayCommand]
    private void OpenMusic()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择音乐文件",
            Filter = "音频文件|*.mp3;*.wav;*.wma;*.m4a|所有文件|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            MusicFilePath = dialog.FileName;
            _audioService.Open(MusicFilePath);
            AddSystemMessage($"已加载音乐：{MusicFilePath}");
        }
    }

    /// <summary>
    /// 播放或暂停音乐
    /// </summary>
    [RelayCommand]
    private void PlayPauseMusic()
    {
        if (_audioService.IsPlaying)
        {
            _audioService.Pause();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(MusicFilePath))
            {
                AddSystemMessage("请先选择音乐文件");
                return;
            }

            _audioService.Play();
        }

        // 通知播放状态已变化
        OnPropertyChanged(nameof(IsMusicPlaying));
    }

    /// <summary>
    /// 停止播放音乐
    /// </summary>
    [RelayCommand]
    private void StopMusic()
    {
        _audioService.Stop();
        OnPropertyChanged(nameof(IsMusicPlaying));
    }

    /// <summary>
    /// 收到蓝牙消息时回调（后台线程）
    /// </summary>
    private void OnMessageReceived(object? sender, string message)
    {
        // 切换到 UI 线程更新集合
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddMessage(new BluetoothMessage
            {
                Direction = MessageDirection.Received,
                Content = message,
                Timestamp = DateTime.Now
            });
        });
    }

    /// <summary>
    /// 连接状态变化时回调（后台线程）
    /// </summary>
    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        // 切换到 UI 线程更新状态
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = connected;
            StatusText = connected ? "已连接" : "未连接";

            // 刷新命令可执行状态
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            SendMessageCommand.NotifyCanExecuteChanged();
        });
    }

    /// <summary>
    /// 添加一条消息到日志
    /// </summary>
    private void AddMessage(BluetoothMessage message)
    {
        Messages.Add(message);

        // 超出上限时移除最旧的消息，避免内存无限增长
        while (Messages.Count > MaxLogCount)
        {
            Messages.RemoveAt(0);
        }
    }

    /// <summary>
    /// 添加一条系统提示消息
    /// </summary>
    private void AddSystemMessage(string content)
    {
        AddMessage(new BluetoothMessage
        {
            Direction = MessageDirection.System,
            Content = content,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 选中设备变化时刷新连接命令状态
    /// </summary>
    partial void OnSelectedDeviceChanged(BluetoothDeviceInfo? value)
    {
        ConnectCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 输入内容变化时刷新发送命令状态
    /// </summary>
    partial void OnInputMessageChanged(string value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 扫描状态变化时刷新命令状态
    /// </summary>
    partial void OnIsScanningChanged(bool value)
    {
        RefreshDevicesCommand.NotifyCanExecuteChanged();
        ConnectCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 主题变化时应用新的亮度主题
    /// </summary>
    partial void OnSelectedThemeChanged(ThemeOption? value)
    {
        if (value != null)
        {
            _themeService.ApplyTheme(value.Value, SelectedAccent?.Value ?? AccentColor.Blue);
        }
    }

    /// <summary>
    /// 强调色变化时应用新的强调色
    /// </summary>
    partial void OnSelectedAccentChanged(AccentOption? value)
    {
        if (value != null)
        {
            _themeService.ApplyTheme(SelectedTheme?.Value ?? AppTheme.Light, value.Value);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 取消事件订阅并释放服务
        _bluetoothService.MessageReceived -= OnMessageReceived;
        _bluetoothService.ConnectionStateChanged -= OnConnectionStateChanged;
        _bluetoothService.Dispose();
        _audioService.Dispose();
    }
}
