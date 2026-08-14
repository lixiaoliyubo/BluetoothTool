namespace BluetoothTool.Models;

/// <summary>
/// 主题亮度模式
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// 浅色主题
    /// </summary>
    Light,

    /// <summary>
    /// 深色主题
    /// </summary>
    Dark
}

/// <summary>
/// 主题强调色
/// </summary>
public enum AccentColor
{
    /// <summary>
    /// 蓝色
    /// </summary>
    Blue,

    /// <summary>
    /// 绿色
    /// </summary>
    Green,

    /// <summary>
    /// 紫色
    /// </summary>
    Purple,

    /// <summary>
    /// 橙色
    /// </summary>
    Orange
}

/// <summary>
/// 主题下拉选项
/// </summary>
public class ThemeOption
{
    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 主题枚举值
    /// </summary>
    public AppTheme Value { get; init; }
}

/// <summary>
/// 强调色下拉选项
/// </summary>
public class AccentOption
{
    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 强调色枚举值
    /// </summary>
    public AccentColor Value { get; init; }
}
