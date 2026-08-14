using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using BluetoothTool.Models;

namespace BluetoothTool.Services;

/// <summary>
/// 基于 Windows RFCOMM 的经典蓝牙串口服务实现
/// </summary>
public class BluetoothService : IBluetoothService
{
    private StreamSocket? _socket;
    private DataWriter? _writer;
    private DataReader? _reader;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;

    /// <inheritdoc />
    public event EventHandler<string>? MessageReceived;

    /// <inheritdoc />
    public event EventHandler<bool>? ConnectionStateChanged;

    /// <inheritdoc />
    public bool IsConnected => _socket != null;

    /// <inheritdoc />
    public async Task<IEnumerable<BluetoothDeviceInfo>> GetDevicesAsync()
    {
        var devices = new Dictionary<string, BluetoothDeviceInfo>();

        // 经典蓝牙设备选择器，可发现已配对及范围内可发现的设备
        var selector = BluetoothDevice.GetDeviceSelector();
        var properties = new[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsPresent" };

        // 使用观察者进行扫描，比 FindAllAsync 更能发现未配对设备
        var watcher = DeviceInformation.CreateWatcher(selector, properties, DeviceInformationKind.AssociationEndpoint);

        watcher.Added += (_, info) => AddOrUpdate(devices, info);
        watcher.Removed += (_, update) =>
        {
            lock (devices)
            {
                devices.Remove(update.Id);
            }
        };

        watcher.Start();

        // 扫描一段时间以收集附近设备
        await Task.Delay(TimeSpan.FromSeconds(6));
        watcher.Stop();

        return devices.Values.ToList();
    }

    /// <inheritdoc />
    public async Task ConnectAsync(BluetoothDeviceInfo device, string pin)
    {
        // 连接前先断开已有连接，避免资源占用
        await DisconnectAsync();

        // 未配对设备先进行配对，否则无法枚举 RFCOMM 服务
        await EnsurePairedAsync(device.Id, pin);

        // 通过设备标识获取蓝牙设备
        var bluetoothDevice = await BluetoothDevice.FromIdAsync(device.Id)
            ?? throw new InvalidOperationException("无法获取该蓝牙设备");

        // 枚举设备提供的 RFCOMM 服务，优先选择串口（SPP）服务
        // 使用未缓存模式重新查询 SDP，避免配对后缓存过期导致连接失败
        var servicesResult = await bluetoothDevice.GetRfcommServicesAsync(BluetoothCacheMode.Uncached);
        var service = servicesResult.Services
            .FirstOrDefault(s => s.ServiceId.Uuid == RfcommServiceId.SerialPort.Uuid)
            ?? servicesResult.Services.FirstOrDefault()
            ?? throw new InvalidOperationException("该设备未提供可用的串口（RFCOMM）服务");

        // 创建套接字并建立 RFCOMM 连接
        if (service.ConnectionHostName == null || string.IsNullOrWhiteSpace(service.ConnectionServiceName))
        {
            throw new InvalidOperationException("无法获取该服务的连接地址，请确认设备已配对后重新扫描再试");
        }

        var socket = new StreamSocket();
        await socket.ConnectAsync(
            service.ConnectionHostName,
            service.ConnectionServiceName,
            SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

        _socket = socket;
        _writer = new DataWriter(socket.OutputStream);
        _reader = new DataReader(socket.InputStream)
        {
            InputStreamOptions = InputStreamOptions.Partial
        };

        // 启动后台接收循环
        _receiveCts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));

        ConnectionStateChanged?.Invoke(this, true);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        // 通知接收循环停止（取消令牌会取消阻塞中的读取操作）
        _receiveCts?.Cancel();

        // 等待接收循环退出，避免与后台读取操作并发
        var receiveTask = _receiveTask;
        if (receiveTask != null)
        {
            try
            {
                await Task.WhenAny(receiveTask, Task.Delay(3000));
            }
            catch
            {
                // 忽略等待过程中可能抛出的异常
            }
        }

        // 释放读写器等资源
        CleanupResources();

        ConnectionStateChanged?.Invoke(this, false);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SendAsync(string message)
    {
        if (_writer == null)
        {
            throw new InvalidOperationException("蓝牙尚未连接，无法发送消息");
        }

        // 将字符串按 UTF-8 编码为字节后写入输出流
        var bytes = Encoding.UTF8.GetBytes(message);
        _writer.WriteBytes(bytes);
        await _writer.StoreAsync();
    }

    /// <summary>
    /// 后台循环接收数据
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _reader != null)
        {
            try
            {
                // 使用可取消读取，断开时通过取消令牌结束而非关闭流，避免抛出 COM 异常
                var loaded = await _reader.LoadAsync(1024).AsTask(token);
                if (loaded == 0)
                {
                    // 对端关闭连接
                    break;
                }

                var buffer = new byte[loaded];
                _reader.ReadBytes(buffer);

                // 按 UTF-8 解码并通知上层
                var message = Encoding.UTF8.GetString(buffer);
                MessageReceived?.Invoke(this, message);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // 读取异常说明连接已中断
                break;
            }
        }

        // 非主动取消时，说明连接被对端断开，清理资源并通知上层
        if (!token.IsCancellationRequested)
        {
            CleanupResources();
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    /// <summary>
    /// 释放读写器、套接字与取消令牌等资源
    /// </summary>
    private void CleanupResources()
    {
        // 依次释放，并忽略释放过程中可能抛出的 WinRT/COM 异常
        try { _writer?.DetachStream(); } catch { }
        try { _writer?.Dispose(); } catch { }
        try { _reader?.DetachStream(); } catch { }
        try { _reader?.Dispose(); } catch { }
        try { _socket?.Dispose(); } catch { }
        try { _receiveCts?.Dispose(); } catch { }

        // 清空引用
        _writer = null;
        _reader = null;
        _socket = null;
        _receiveCts = null;
        _receiveTask = null;
    }

    /// <summary>
    /// 确保设备已配对，未配对时使用 PIN 发起配对
    /// </summary>
    private static async Task EnsurePairedAsync(string deviceId, string pin)
    {
        // 获取设备信息以检查配对状态
        var deviceInfo = await DeviceInformation.CreateFromIdAsync(deviceId);
        if (deviceInfo == null || deviceInfo.Pairing.IsPaired)
        {
            return;
        }

        // 处理配对请求，向系统提供 PIN 码
        TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> handler =
            (_, args) => args.Accept(pin);

        deviceInfo.Pairing.Custom.PairingRequested += handler;

        try
        {
            // 使用 PIN 方式发起配对
            var result = await deviceInfo.Pairing.Custom.PairAsync(
                DevicePairingKinds.ProvidePin,
                DevicePairingProtectionLevel.Default);

            // 判断配对结果
            if (result.Status != DevicePairingResultStatus.Paired &&
                result.Status != DevicePairingResultStatus.AlreadyPaired)
            {
                throw new InvalidOperationException($"设备配对失败：{result.Status}");
            }
        }
        finally
        {
            deviceInfo.Pairing.Custom.PairingRequested -= handler;
        }
    }

    /// <summary>
    /// 新增或更新设备到集合
    /// </summary>
    private static void AddOrUpdate(IDictionary<string, BluetoothDeviceInfo> devices, DeviceInformation info)
    {
        lock (devices)
        {
            devices[info.Id] = CreateDeviceInfo(info);
        }
    }

    /// <summary>
    /// 将设备信息转换为实体
    /// </summary>
    private static BluetoothDeviceInfo CreateDeviceInfo(DeviceInformation info)
    {
        return new BluetoothDeviceInfo
        {
            Id = info.Id,
            Name = string.IsNullOrWhiteSpace(info.Name) ? "未知设备" : info.Name,
            Address = ExtractAddress(info),
            IsPaired = info.Pairing?.IsPaired ?? false
        };
    }

    /// <summary>
    /// 从设备属性中提取 MAC 地址
    /// </summary>
    private static string ExtractAddress(DeviceInformation info)
    {
        // 优先读取系统提供的设备地址属性
        if (info.Properties.TryGetValue("System.Devices.Aep.DeviceAddress", out var value) &&
            value is string address &&
            !string.IsNullOrWhiteSpace(address))
        {
            return address;
        }

        return "未知地址";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }
}
