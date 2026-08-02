using System.Management;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using LumaKnob.Core.Brightness;
using LumaKnob.Engine.Displays;

namespace LumaKnob.Windows.InternalPanel;

[SupportedOSPlatform("windows")]
internal static class WmiDiscovery
{
    public static IReadOnlyList<IBrightnessTarget> Discover(
        DisplayPixelBounds primaryBounds,
        string primaryDeviceName,
        string? primaryFriendlyName,
        bool? advancedColorEnabled,
        ICollection<string> diagnostics)
    {
        var targets = new List<IBrightnessTarget>();

        try
        {
            var scope = new ManagementScope(@"\\.\root\wmi");
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM WmiMonitorBrightness"));
            using var results = searcher.Get();

            foreach (ManagementObject monitor in results)
            {
                using (monitor)
                {
                    if (monitor["Active"] is bool active && !active)
                    {
                        continue;
                    }

                    var instanceName = Convert.ToString(monitor["InstanceName"], CultureInfo.InvariantCulture);
                    if (string.IsNullOrWhiteSpace(instanceName))
                    {
                        continue;
                    }

                    var levels = ((byte[]?)monitor["Level"] ?? [])
                        .Select(level => (int)level)
                        .Distinct()
                        .Order()
                        .ToArray();

                    if (levels.Length == 0)
                    {
                        diagnostics.Add("An active WMI panel exposed no brightness levels.");
                        continue;
                    }

                    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(instanceName)))[..12];
                    targets.Add(new WmiBrightnessTarget(
                        instanceName,
                        new BrightnessTargetInfo(
                            $"wmi:{hash}",
                            CreateDisplayName(primaryFriendlyName, targets.Count),
                            "WMI + SHADE",
                            "WMI",
                            primaryBounds,
                            new HardwareBrightnessCapability(0, 100, levels),
                            "Software shade is currently mapped to the Windows primary display.",
                            PlatformDisplayName: primaryDeviceName,
                            AdvancedColorEnabled: advancedColorEnabled)));
                }
            }
        }
        catch (ManagementException exception)
        {
            diagnostics.Add(
                $"Integrated-panel WMI brightness is unavailable on this system " +
                $"({exception.Message}); this is normal for many desktop PCs.");
        }
        catch (UnauthorizedAccessException exception)
        {
            diagnostics.Add($"Integrated-panel WMI brightness access was denied ({exception.Message}).");
        }

        if (targets.Count == 0 && !diagnostics.Any(message => message.Contains("WMI brightness", StringComparison.Ordinal)))
        {
            diagnostics.Add("No active integrated-panel WMI brightness target was exposed; external displays use DDC/CI.");
        }

        return targets;
    }

    private static string CreateDisplayName(string? primaryFriendlyName, int targetIndex)
    {
        var baseName = string.IsNullOrWhiteSpace(primaryFriendlyName)
            ? "Integrated display"
            : primaryFriendlyName.Trim();
        return targetIndex == 0 ? baseName : $"{baseName} {targetIndex + 1}";
    }
}
