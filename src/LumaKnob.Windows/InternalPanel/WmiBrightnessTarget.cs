using System.Globalization;
using System.Management;
using System.Runtime.Versioning;
using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Displays;

namespace LumaKnob.Windows.InternalPanel;

[SupportedOSPlatform("windows")]
internal sealed class WmiBrightnessTarget : IBrightnessTarget
{
    private const string ScopePath = @"\\.\root\wmi";
    private readonly string _instanceName;

    public WmiBrightnessTarget(string instanceName, BrightnessTargetInfo info)
    {
        _instanceName = instanceName;
        Info = info;
    }

    public BrightnessTargetInfo Info { get; }

    public ValueTask<BrightnessValue> ReadBrightnessAsync(CancellationToken cancellationToken) =>
        new(Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return WithMatchingObject(
                "SELECT * FROM WmiMonitorBrightness",
                "The integrated-panel WMI brightness instance disappeared.",
                monitor => BrightnessValue.FromPercent(
                    Convert.ToByte(monitor["CurrentBrightness"], CultureInfo.InvariantCulture)));
        }, cancellationToken));

    public ValueTask SetNativeBrightnessAsync(int nativeValue, CancellationToken cancellationToken) =>
        new(Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            _ = WithMatchingObject(
                "SELECT * FROM WmiMonitorBrightnessMethods",
                "The integrated-panel WMI brightness methods instance disappeared.",
                methods =>
                {
                    using var input = methods.GetMethodParameters("WmiSetBrightness");
                    input["Timeout"] = 0u;
                    input["Brightness"] = checked((byte)nativeValue);
                    using var output = methods.InvokeMethod("WmiSetBrightness", input, null);
                    if (output is null)
                    {
                        throw new InvalidOperationException("WMI returned no brightness result.");
                    }

                    var result = Convert.ToUInt32(output["ReturnValue"], CultureInfo.InvariantCulture);
                    if (result != 0)
                    {
                        throw new InvalidOperationException($"WMI brightness write failed with result 0x{result:X8}.");
                    }

                    return true;
                });
        }, cancellationToken));

    public void Dispose()
    {
    }

    private bool InstanceMatches(ManagementBaseObject monitor) =>
        string.Equals(
            Convert.ToString(monitor["InstanceName"], CultureInfo.InvariantCulture),
            _instanceName,
            StringComparison.OrdinalIgnoreCase);

    private T WithMatchingObject<T>(
        string query,
        string missingMessage,
        Func<ManagementObject, T> action)
    {
        using var searcher = new ManagementObjectSearcher(new ManagementScope(ScopePath), new ObjectQuery(query));
        using var results = searcher.Get();
        foreach (ManagementObject item in results)
        {
            using (item)
            {
                if (InstanceMatches(item))
                {
                    return action(item);
                }
            }
        }

        throw new InvalidOperationException(missingMessage);
    }
}
