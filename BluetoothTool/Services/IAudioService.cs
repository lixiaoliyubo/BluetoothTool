namespace BluetoothTool.Services;

/// <summary>
/// 音频播放服务接口
/// </summary>
public interface IAudioService : IDisposable
{
    /// <summary>
    /// 是否正在播放
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// 打开音频文件
    /// </summary>
    void Open(string filePath);

    /// <summary>
    /// 播放
    /// </summary>
    void Play();

    /// <summary>
    /// 暂停
    /// </summary>
    void Pause();

    /// <summary>
    /// 停止
    /// </summary>
    void Stop();
}
