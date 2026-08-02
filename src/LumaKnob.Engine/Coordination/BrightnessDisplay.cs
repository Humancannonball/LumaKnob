using System.Diagnostics.CodeAnalysis;
using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Displays;

namespace LumaKnob.Engine.Coordination;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "BrightnessEngine owns display-session lifetime and cancels every pending apply before disposing targets.")]
public sealed class BrightnessDisplay
{
    private readonly object _gate = new();
    private CancellationTokenSource? _pendingApply;
    private long _requestVersion;

    internal BrightnessDisplay(IBrightnessTarget target) => Target = target;

    internal IBrightnessTarget Target { get; }

    public bool IsHardwareFaulted { get; internal set; }

    internal SemaphoreSlim HardwareWriteGate { get; } = new(1, 1);

    public BrightnessTargetInfo Info => Target.Info;

    public string Id => Info.Id;

    public string Name => Info.Name;

    public string BackendLabel => Info.BackendLabel;

    public string? HardwareBackendLabel => Info.HardwareBackendLabel;

    public HardwareBrightnessCapability? HardwareCapability => Info.HardwareCapability;

    public bool IsSimulator => Info.IsSimulator;

    internal (long Version, CancellationToken Token) ReplacePendingApply()
    {
        lock (_gate)
        {
            _pendingApply?.Cancel();
            _pendingApply?.Dispose();
            _pendingApply = new CancellationTokenSource();
            return (++_requestVersion, _pendingApply.Token);
        }
    }

    internal bool IsCurrent(long version)
    {
        lock (_gate)
        {
            return version == _requestVersion;
        }
    }

    internal void CancelPendingApply()
    {
        lock (_gate)
        {
            _requestVersion++;
            _pendingApply?.Cancel();
            _pendingApply?.Dispose();
            _pendingApply = null;
        }
    }
}
