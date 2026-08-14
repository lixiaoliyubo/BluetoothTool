namespace BluetoothTool.Models;

/// <summary>
/// 蓝牙设备实体
/// </summary>
public class BluetoothDeviceInfo
{
    /// <summary>
    /// 设备唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 设备 MAC 地址
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 是否已配对
    /// </summary>
    public bool IsPaired { get; set; }
}
