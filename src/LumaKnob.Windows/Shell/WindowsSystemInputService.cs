using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LumaKnob.Engine.Platform;

namespace LumaKnob.Windows.Shell;

[SupportedOSPlatform("windows")]
public sealed class WindowsSystemInputService : ISystemInputService
{
    private const int EmergencyRestoreHotkeyId = 0x4C4B;
    private const int ErrorTimeout = 1460;
    private const uint ModifierAlt = 0x0001;
    private const uint ModifierControl = 0x0002;
    private const uint ModifierNoRepeat = 0x4000;
    private const uint VirtualKeyHome = 0x24;

    private const uint MessageDestroy = 0x0002;
    private const uint MessageClose = 0x0010;
    private const uint MessageQuit = 0x0012;
    private const uint MessageInputDeviceChange = 0x00FE;
    private const uint MessageInput = 0x00FF;
    private const uint MessageHotkey = 0x0312;
    private const nuint InputDeviceRemoval = 2;

    private const uint RawInputTypeHid = 2;
    private const uint RawInputData = 0x10000003;
    private const uint RawInputDeviceInfoPreparsedData = 0x20000005;
    private const uint RawInputRemove = 0x00000001;
    private const uint RawInputInputSink = 0x00000100;
    private const uint RawInputDeviceNotify = 0x00002000;

    private const ushort ConsumerUsagePage = 0x000C;
    private const ushort ConsumerControlUsage = 0x0001;
    private const int HidInputReport = 0;
    private const int HidStatusSuccess = 0x00110000;

    private const int WindowProcedureIndex = -4;
    private const uint WindowStylePopup = 0x80000000;
    private static readonly nint MessageOnlyWindowParent = new(-3);
    private static readonly TimeSpan RepeatDelay = TimeSpan.FromMilliseconds(420);
    private static readonly TimeSpan RepeatPeriod = TimeSpan.FromMilliseconds(90);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(5);

    private readonly ManualResetEventSlim _ready = new(false);
    private readonly Dictionary<nint, nint> _preparsedDataByDevice = [];
    private readonly Dictionary<nint, int> _directionByDevice = [];
    private Thread? _thread;
    private Timer? _repeatTimer;
    private WindowProcedure? _windowProcedure;
    private nint _previousWindowProcedure;
    private nint _windowHandle;
    private uint _threadId;
    private int _activeDirection;
    private SystemInputRegistration _registration;
    private bool _disposed;

    public event EventHandler? EmergencyRestorePressed;

    public event EventHandler? BrightnessIncreasePressed;

    public event EventHandler? BrightnessDecreasePressed;

    public SystemInputRegistration Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_thread is not null)
        {
            return _registration;
        }

        _repeatTimer = new Timer(
            static state => ((WindowsSystemInputService)state!).RepeatBrightness(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
        _thread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "LumaKnob system input",
        };
        _thread.Start();
        if (!_ready.Wait(StartupTimeout))
        {
            _registration = new SystemInputRegistration(false, ErrorTimeout, false, ErrorTimeout);
        }

        return _registration;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Volatile.Write(ref _activeDirection, 0);
        _repeatTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        var window = _windowHandle;
        if (window != 0)
        {
            _ = PostMessage(window, MessageClose, 0, 0);
        }
        else if (_threadId != 0)
        {
            _ = PostThreadMessage(_threadId, MessageQuit, 0, 0);
        }

        var stopped = _thread is null || _thread.Join(TimeSpan.FromSeconds(2));
        if (stopped)
        {
            _repeatTimer?.Dispose();
            _ready.Dispose();
        }
    }

    private void MessageLoop()
    {
        var restoreRegistered = false;
        var brightnessKeysListening = false;

        try
        {
            _threadId = GetCurrentThreadId();
            _windowProcedure = HandleWindowMessage;

            var window = CreateWindowEx(
                0,
                "STATIC",
                "LumaKnob system input",
                WindowStylePopup,
                0,
                0,
                0,
                0,
                MessageOnlyWindowParent,
                0,
                0,
                0);
            if (window == 0)
            {
                var error = Marshal.GetLastWin32Error();
                _registration = new SystemInputRegistration(false, error, false, error);
                return;
            }

            _windowHandle = window;
            Marshal.SetLastPInvokeError(0);
            _previousWindowProcedure = SetWindowLongPtr(
                window,
                WindowProcedureIndex,
                Marshal.GetFunctionPointerForDelegate(_windowProcedure));
            var subclassError = Marshal.GetLastWin32Error();
            if (_previousWindowProcedure == 0 && subclassError != 0)
            {
                _registration = new SystemInputRegistration(false, subclassError, false, subclassError);
                return;
            }

            restoreRegistered = RegisterHotKey(
                window,
                EmergencyRestoreHotkeyId,
                ModifierAlt | ModifierControl | ModifierNoRepeat,
                VirtualKeyHome);
            var restoreError = restoreRegistered ? 0 : Marshal.GetLastWin32Error();

            var rawInputDevices = new[]
            {
                new RawInputDevice
                {
                    UsagePage = ConsumerUsagePage,
                    Usage = ConsumerControlUsage,
                    Flags = RawInputInputSink | RawInputDeviceNotify,
                    Target = window,
                },
            };
            brightnessKeysListening = RegisterRawInputDevices(
                rawInputDevices,
                (uint)rawInputDevices.Length,
                (uint)Marshal.SizeOf<RawInputDevice>());
            var brightnessError = brightnessKeysListening ? 0 : Marshal.GetLastWin32Error();

            _registration = new SystemInputRegistration(
                restoreRegistered,
                restoreError,
                brightnessKeysListening,
                brightnessError);
            _ready.Set();

            while (GetMessage(out var message, 0, 0, 0) > 0)
            {
                _ = TranslateMessage(in message);
                _ = DispatchMessage(in message);
            }
        }
        catch
        {
            _registration = new SystemInputRegistration(false, -1, false, -1);
        }
        finally
        {
            Volatile.Write(ref _activeDirection, 0);
            _repeatTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            if (restoreRegistered)
            {
                _ = UnregisterHotKey(_windowHandle, EmergencyRestoreHotkeyId);
            }

            if (brightnessKeysListening)
            {
                var removal = new[]
                {
                    new RawInputDevice
                    {
                        UsagePage = ConsumerUsagePage,
                        Usage = ConsumerControlUsage,
                        Flags = RawInputRemove,
                        Target = 0,
                    },
                };
                _ = RegisterRawInputDevices(
                    removal,
                    (uint)removal.Length,
                    (uint)Marshal.SizeOf<RawInputDevice>());
            }

            FreeAllPreparsedData();

            var window = _windowHandle;
            if (window != 0 && IsWindow(window))
            {
                _ = DestroyWindow(window);
            }

            _windowHandle = 0;
            _ready.Set();
        }
    }

    private nint HandleWindowMessage(nint window, uint message, nuint wParam, nint lParam)
    {
        switch (message)
        {
            case MessageHotkey when unchecked((int)wParam) == EmergencyRestoreHotkeyId:
                EmergencyRestorePressed?.Invoke(this, EventArgs.Empty);
                return 0;

            case MessageInput:
                ProcessRawInput(lParam);
                break;

            case MessageInputDeviceChange when wParam == InputDeviceRemoval:
                RemoveInputDevice(lParam);
                break;

            case MessageClose:
                _ = DestroyWindow(window);
                return 0;

            case MessageDestroy:
                PostQuitMessage(0);
                return 0;
        }

        return _previousWindowProcedure == 0
            ? DefWindowProc(window, message, wParam, lParam)
            : CallWindowProc(_previousWindowProcedure, window, message, wParam, lParam);
    }

    private void ProcessRawInput(nint rawInputHandle)
    {
        var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        uint dataSize = 0;
        var sizeResult = GetRawInputData(rawInputHandle, RawInputData, 0, ref dataSize, headerSize);
        if (sizeResult == uint.MaxValue || dataSize < headerSize + (2 * sizeof(uint)))
        {
            return;
        }

        var buffer = Marshal.AllocHGlobal(checked((int)dataSize));
        try
        {
            var bytesRead = GetRawInputData(rawInputHandle, RawInputData, buffer, ref dataSize, headerSize);
            if (bytesRead == uint.MaxValue || bytesRead < headerSize + (2 * sizeof(uint)))
            {
                return;
            }

            var header = Marshal.PtrToStructure<RawInputHeader>(buffer);
            if (header.Type != RawInputTypeHid)
            {
                return;
            }

            var reportSize = unchecked((uint)Marshal.ReadInt32(buffer, checked((int)headerSize)));
            var reportCount = unchecked((uint)Marshal.ReadInt32(buffer, checked((int)headerSize + sizeof(uint))));
            var reportDataOffset = headerSize + (2 * sizeof(uint));
            var requiredSize = (ulong)reportDataOffset + ((ulong)reportSize * reportCount);
            if (reportSize == 0 || reportCount == 0 || requiredSize > bytesRead)
            {
                return;
            }

            var preparsedData = GetPreparsedData(header.Device);
            if (preparsedData == 0)
            {
                return;
            }

            for (var index = 0u; index < reportCount; index++)
            {
                var reportOffset = checked((int)((ulong)reportDataOffset + ((ulong)index * reportSize)));
                ProcessHidReport(header.Device, preparsedData, buffer + reportOffset, reportSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void ProcessHidReport(nint device, nint preparsedData, nint report, uint reportLength)
    {
        var usages = new HidUsageAndPage[16];
        var usageCount = (uint)usages.Length;
        var status = HidPGetUsagesEx(
            HidInputReport,
            0,
            usages,
            ref usageCount,
            preparsedData,
            report,
            reportLength);
        if (status != HidStatusSuccess)
        {
            return;
        }

        UpdateDeviceDirection(
            device,
            BrightnessHidUsage.GetDirection(usages.AsSpan(0, checked((int)usageCount))));
    }

    private nint GetPreparsedData(nint device)
    {
        if (_preparsedDataByDevice.TryGetValue(device, out var cached))
        {
            return cached;
        }

        uint dataSize = 0;
        var sizeResult = GetRawInputDeviceInfo(device, RawInputDeviceInfoPreparsedData, 0, ref dataSize);
        if (sizeResult == uint.MaxValue || dataSize == 0)
        {
            return 0;
        }

        var data = Marshal.AllocHGlobal(checked((int)dataSize));
        var result = GetRawInputDeviceInfo(device, RawInputDeviceInfoPreparsedData, data, ref dataSize);
        if (result == uint.MaxValue)
        {
            Marshal.FreeHGlobal(data);
            return 0;
        }

        _preparsedDataByDevice[device] = data;
        return data;
    }

    private void RemoveInputDevice(nint device)
    {
        if (_preparsedDataByDevice.Remove(device, out var preparsedData))
        {
            Marshal.FreeHGlobal(preparsedData);
        }

        if (_directionByDevice.Remove(device))
        {
            SetActiveDirection(_directionByDevice.Values.FirstOrDefault());
        }
    }

    private void FreeAllPreparsedData()
    {
        foreach (var data in _preparsedDataByDevice.Values)
        {
            Marshal.FreeHGlobal(data);
        }

        _preparsedDataByDevice.Clear();
        _directionByDevice.Clear();
    }

    private void UpdateDeviceDirection(nint device, int direction)
    {
        if (direction == 0)
        {
            _directionByDevice.Remove(device);
        }
        else
        {
            _directionByDevice[device] = direction;
        }

        var activeDirection = direction != 0
            ? direction
            : _directionByDevice.Values.FirstOrDefault();
        SetActiveDirection(activeDirection);
    }

    private void SetActiveDirection(int direction)
    {
        if (direction == Volatile.Read(ref _activeDirection))
        {
            return;
        }

        Volatile.Write(ref _activeDirection, direction);
        _repeatTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        if (direction == 0)
        {
            return;
        }

        RaiseBrightness(direction);
        _repeatTimer?.Change(RepeatDelay, RepeatPeriod);
    }

    private void RepeatBrightness()
    {
        var direction = Volatile.Read(ref _activeDirection);
        if (direction != 0)
        {
            RaiseBrightness(direction);
        }
    }

    private void RaiseBrightness(int direction)
    {
        if (direction > 0)
        {
            BrightnessIncreasePressed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            BrightnessDecreasePressed?.Invoke(this, EventArgs.Empty);
        }
    }

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(
        uint extendedStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint window, int index, nint newValue);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static extern nint DefWindowProc(nint window, uint message, nuint wParam, nint lParam);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern nint CallWindowProc(
        nint previousWindowProcedure,
        nint window,
        uint message,
        nuint wParam,
        nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint window, int id);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices(
        [In] RawInputDevice[] rawInputDevices,
        uint numberOfDevices,
        uint size);

    [DllImport("user32.dll", EntryPoint = "GetRawInputData", SetLastError = true)]
    private static extern uint GetRawInputData(
        nint rawInput,
        uint command,
        nint data,
        ref uint size,
        uint headerSize);

    [DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", SetLastError = true)]
    private static extern uint GetRawInputDeviceInfo(
        nint device,
        uint command,
        nint data,
        ref uint size);

    [DllImport("hid.dll", EntryPoint = "HidP_GetUsagesEx")]
    private static extern int HidPGetUsagesEx(
        int reportType,
        ushort linkCollection,
        [Out] HidUsageAndPage[] buttonList,
        ref uint usageLength,
        nint preparsedData,
        nint report,
        uint reportLength);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage message, nint window, uint minimum, uint maximum);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(in NativeMessage message);

    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
    private static extern nint DispatchMessage(in NativeMessage message);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nuint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint threadId, uint message, nuint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam);
}

internal static class BrightnessHidUsage
{
    internal const ushort ConsumerUsagePage = 0x000C;
    internal const ushort BrightnessIncrement = 0x006F;
    internal const ushort BrightnessDecrement = 0x0070;

    internal static int GetDirection(ReadOnlySpan<HidUsageAndPage> usages)
    {
        foreach (var usage in usages)
        {
            if (usage.UsagePage != ConsumerUsagePage)
            {
                continue;
            }

            if (usage.Usage == BrightnessIncrement)
            {
                return 1;
            }

            if (usage.Usage == BrightnessDecrement)
            {
                return -1;
            }
        }

        return 0;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputDevice
{
    internal ushort UsagePage;
    internal ushort Usage;
    internal uint Flags;
    internal nint Target;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputHeader
{
    internal uint Type;
    internal uint Size;
    internal nint Device;
    internal nuint WParam;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HidUsageAndPage(ushort usage, ushort usagePage)
{
    internal readonly ushort Usage = usage;
    internal readonly ushort UsagePage = usagePage;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeMessage
{
    internal nint Window;
    internal uint Message;
    internal nuint WParam;
    internal nint LParam;
    internal uint Time;
    internal NativePoint Point;
    internal uint Private;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativePoint
{
    internal int X;
    internal int Y;
}
