namespace BluetoothTool.Models;

/// <summary>
/// 消息方向枚举
/// </summary>
public enum MessageDirection
{
    /// <summary>
    /// 已发送
    /// </summary>
    Sent,

    /// <summary>
    /// 已接收
    /// </summary>
    Received,

    /// <summary>
    /// 系统提示
    /// </summary>
    System
}

/// <summary>
/// 蓝牙消息实体
/// </summary>
public class BluetoothMessage
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消息方向
    /// </summary>
    public MessageDirection Direction { get; set; }

    /// <summary>
    /// 消息时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}
