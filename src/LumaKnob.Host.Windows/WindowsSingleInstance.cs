using System.Runtime.Versioning;
using LumaKnob.App;

namespace LumaKnob.Host.Windows;

[SupportedOSPlatform("windows")]
internal sealed class WindowsSingleInstance : IDisposable
{
    private const string MutexName = @"Local\LumaKnob.Application";
    private const string ActivationEventName = @"Local\LumaKnob.Activate";

    private readonly EventWaitHandle _activationEvent;
    private readonly EventWaitHandle _stopEvent;
    private readonly Mutex _mutex;
    private readonly Thread? _listenerThread;
    private bool _disposed;

    internal WindowsSingleInstance()
    {
        _activationEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            ActivationEventName);
        _stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        _mutex = new Mutex(true, MutexName, out var createdNew);
        IsPrimary = createdNew;

        if (!IsPrimary)
        {
            _activationEvent.Set();
            return;
        }

        _listenerThread = new Thread(ListenForActivation)
        {
            IsBackground = true,
            Name = "LumaKnob activation listener",
        };
        _listenerThread.Start();
    }

    internal bool IsPrimary { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (IsPrimary)
        {
            _stopEvent.Set();
            _ = _listenerThread?.Join(TimeSpan.FromSeconds(2));
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
        _stopEvent.Dispose();
        _activationEvent.Dispose();
    }

    private void ListenForActivation()
    {
        WaitHandle[] signals = [_activationEvent, _stopEvent];
        while (WaitHandle.WaitAny(signals) == 0)
        {
            DesktopBootstrap.RequestActivation();
        }
    }
}
