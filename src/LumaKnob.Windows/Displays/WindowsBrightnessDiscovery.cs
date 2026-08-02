using System.Runtime.Versioning;
using LumaKnob.Engine.Displays;
using LumaKnob.Windows.DdcCi;
using LumaKnob.Windows.InternalPanel;
using LumaKnob.Windows.Shade;

namespace LumaKnob.Windows.Displays;

public static class WindowsBrightnessDiscovery
{
    [SupportedOSPlatform("windows")]
    public static Task<DisplayDiscoveryReport> DiscoverAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => Discover(cancellationToken), cancellationToken);

    [SupportedOSPlatform("windows")]
    private static DisplayDiscoveryReport Discover(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new List<string>();
        var targets = new List<IBrightnessTarget>();
        WindowsSystemInventory.AddTo(diagnostics);
        var logicalMonitors = LogicalMonitorEnumerator.Enumerate();
        diagnostics.Add($"Windows reported {logicalMonitors.Count} active logical display(s).");
        var displayIdentities = DisplayConfigNameResolver.ResolveActiveDisplays(diagnostics);

        var primary = logicalMonitors.FirstOrDefault(monitor => monitor.IsPrimary);
        if (primary is null && logicalMonitors.Count > 0)
        {
            primary = logicalMonitors[0];
        }
        DisplayConfigIdentity? primaryIdentity = null;
        if (primary is not null)
        {
            _ = displayIdentities.TryGetValue(primary.DeviceName, out primaryIdentity);
        }

        var primaryFriendlyName = primaryIdentity?.FriendlyName;
        var wmiTargets = primary is null
            ? []
            : WmiDiscovery.Discover(
                primary.Bounds,
                primary.DeviceName,
                primaryFriendlyName,
                primaryIdentity?.AdvancedColorEnabled,
                diagnostics);
        targets.AddRange(wmiTargets);

        foreach (var logicalMonitor in logicalMonitors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<IBrightnessTarget> ddcTargets;
            displayIdentities.TryGetValue(logicalMonitor.DeviceName, out var identity);
            try
            {
                ddcTargets = DdcCiDiscovery.Discover(
                    logicalMonitor,
                    identity?.FriendlyName,
                    identity?.AdvancedColorEnabled,
                    diagnostics);
            }
            catch (Exception exception)
            {
                diagnostics.Add($"{logicalMonitor.DeviceName}: DDC/CI discovery failed safely ({exception.Message}).");
                ddcTargets = [];
            }

            targets.AddRange(ddcTargets);

            var representedByWmi = logicalMonitor.IsPrimary && wmiTargets.Count > 0;
            if (ddcTargets.Count == 0 && !representedByWmi)
            {
                targets.Add(new ShadeOnlyBrightnessTarget(
                    new BrightnessTargetInfo(
                        $"shade:{logicalMonitor.DeviceName}",
                        identity?.FriendlyName ?? $"{logicalMonitor.DeviceName} software shade",
                        "SHADE ONLY",
                        null,
                        logicalMonitor.Bounds,
                        null,
                        "No working hardware-brightness API was detected for this display.",
                        PlatformDisplayName: logicalMonitor.DeviceName,
                        AdvancedColorEnabled: identity?.AdvancedColorEnabled)));
            }
        }

        diagnostics.Add($"LumaKnob created {targets.Count} controllable target(s).");
        return new DisplayDiscoveryReport(targets, diagnostics);
    }
}
