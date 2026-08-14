using System.Windows.Media;

namespace BluetoothTool.Services;

/// <summary>
/// 基于 WPF MediaPlayer 的音频播放服务实现
/// </summary>
public class AudioService : IAudioService
{
    private readonly MediaPlayer _player = new();
    private bool _isPlaying;

    /// <inheritdoc />
    public bool IsPlaying => _isPlaying;

    /// <inheritdoc />
    public void Open(string filePath)
    {
        // 打开本地音频文件
        _player.Open(new Uri(filePath, UriKind.Absolute));
    }

    /// <inheritdoc />
    public void Play()
    {
        _player.Play();
        _isPlaying = true;
    }

    /// <inheritdoc />
    public void Pause()
    {
        _player.Pause();
        _isPlaying = false;
    }

    /// <inheritdoc />
    public void Stop()
    {
        _player.Stop();
        _isPlaying = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _player.Close();
    }
}
