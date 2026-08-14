using System;
using System.Windows;
using BluetoothTool.Models;

namespace BluetoothTool.Services;

/// <summary>
/// 主题服务：通过替换应用级资源字典实现多主题切换
/// </summary>
public class ThemeService : IThemeService
{
    private ResourceDictionary? _baseTheme;
    private ResourceDictionary? _accentTheme;

    /// <inheritdoc />
    public void ApplyTheme(AppTheme theme, AccentColor accent)
    {
        var merged = Application.Current.Resources.MergedDictionaries;

        // 移除已加载的主题字典
        if (_baseTheme != null)
        {
            merged.Remove(_baseTheme);
        }

        if (_accentTheme != null)
        {
            merged.Remove(_accentTheme);
        }

        // 加载亮度主题字典
        var baseUri = theme == AppTheme.Light
            ? new Uri("Themes/LightTheme.xaml", UriKind.Relative)
            : new Uri("Themes/DarkTheme.xaml", UriKind.Relative);

        _baseTheme = new ResourceDictionary { Source = baseUri };
        merged.Add(_baseTheme);

        // 加载强调色字典（蓝色为默认，无需额外字典）
        var accentUri = GetAccentUri(accent);
        _accentTheme = null;
        if (accentUri != null)
        {
            _accentTheme = new ResourceDictionary { Source = accentUri };
            merged.Add(_accentTheme);
        }
    }

    /// <summary>
    /// 根据强调色枚举返回对应的资源字典地址
    /// </summary>
    private static Uri? GetAccentUri(AccentColor accent)
    {
        return accent switch
        {
            AccentColor.Green => new Uri("Themes/AccentGreen.xaml", UriKind.Relative),
            AccentColor.Purple => new Uri("Themes/AccentPurple.xaml", UriKind.Relative),
            AccentColor.Orange => new Uri("Themes/AccentOrange.xaml", UriKind.Relative),
            _ => null
        };
    }
}
