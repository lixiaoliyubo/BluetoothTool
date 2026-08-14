using BluetoothTool.Models;

namespace BluetoothTool.Services;

/// <summary>
/// 主题服务接口
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// 应用指定的亮度主题与强调色
    /// </summary>
    void ApplyTheme(AppTheme theme, AccentColor accent);
}
