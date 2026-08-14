using BluetoothTool.Models;

namespace BluetoothTool.Services;

/// <summary>
/// 蓝牙服务接口
/// </summary>
public interface IBluetoothService : IDisposable
{
    /// <summary>
    /// 收到消息时触发
    /// </summary>
    event EventHandler<string>? MessageReceived;

    /// <summary>
    /// 连接状态变化时触发
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 扫描附近的经典蓝牙设备
    /// </summary>
    Task<IEnumerable<BluetoothDeviceInfo>> GetDevicesAsync();

    /// <summary>
    /// 连接指定蓝牙设备（必要时使用 PIN 进行配对）
    /// </summary>
    Task ConnectAsync(BluetoothDeviceInfo device, string pin);

    /// <summary>
    /// 断开当前蓝牙连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 发送字符串消息
    /// </summary>
    Task SendAsync(string message);
}
