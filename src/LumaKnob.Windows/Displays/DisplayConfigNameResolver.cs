using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;

namespace LumaKnob.Windows.Displays;

/// <summary>
/// Resolves the GDI names returned by GetMonitorInfo (for example,
/// \\.\DISPLAY1) to the EDID-backed friendly names used by Windows Settings.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class DisplayConfigNameResolver
{
    private const int MaximumTopologyRetries = 3;

    public static IReadOnlyDictionary<string, DisplayConfigIdentity> ResolveActiveDisplays(
        ICollection<string> diagnostics)
    {
        var identities = new Dictionary<string, DisplayConfigIdentity>(StringComparer.OrdinalIgnoreCase);
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            diagnostics.Add("Display identity lookup requires Windows 7 or newer; generic names were retained.");
            return identities;
        }

        var flags = QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS;

        for (var attempt = 1; attempt <= MaximumTopologyRetries; attempt++)
        {
            var sizeResult = PInvoke.GetDisplayConfigBufferSizes(flags, out var pathCount, out var modeCount);
            if (sizeResult != WIN32_ERROR.ERROR_SUCCESS)
            {
                diagnostics.Add($"Display identity lookup was unavailable ({(uint)sizeResult}).");
                return identities;
            }

            var paths = new DISPLAYCONFIG_PATH_INFO[checked((int)pathCount)];
            var modes = new DISPLAYCONFIG_MODE_INFO[checked((int)modeCount)];
            var queryResult = PInvoke.QueryDisplayConfig(flags, ref pathCount, paths, ref modeCount, modes);

            if (queryResult == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER && attempt < MaximumTopologyRetries)
            {
                continue;
            }

            if (queryResult != WIN32_ERROR.ERROR_SUCCESS)
            {
                diagnostics.Add($"Display identity lookup was unavailable ({(uint)queryResult}).");
                return identities;
            }

            foreach (var path in paths.AsSpan(0, checked((int)pathCount)))
            {
                if (!TryGetSourceName(path, out var sourceName) ||
                    !TryGetTargetIdentity(path, out var identity))
                {
                    continue;
                }

                // A cloned source can have several active targets. The current
                // UI has one selector entry per HMONITOR, so preserve the first
                // unambiguous match and record the clone for diagnostics.
                if (identities.TryAdd(sourceName, identity))
                {
                    var advancedColor = identity.AdvancedColorEnabled switch
                    {
                        true => "advanced color/HDR enabled",
                        false when identity.AdvancedColorSupported == true => "advanced color/HDR supported but disabled",
                        false => "advanced color/HDR not supported",
                        null => "advanced color/HDR state unavailable",
                    };
                    diagnostics.Add(
                        $"{sourceName}: Windows identified \"{identity.FriendlyName}\" " +
                        $"via {identity.OutputTechnology}; {advancedColor}; " +
                        $"EDID manufacturer 0x{identity.EdidManufacturerId:X4}, " +
                        $"product 0x{identity.EdidProductCode:X4}.");
                }
                else
                {
                    diagnostics.Add(
                        $"{sourceName}: an additional cloned target named \"{identity.FriendlyName}\" was present.");
                }
            }

            return identities;
        }

        diagnostics.Add("Display topology changed repeatedly during identity lookup; generic names were retained.");
        return identities;
    }

    [SupportedOSPlatform("windows6.1")]
    private static unsafe bool TryGetSourceName(
        DISPLAYCONFIG_PATH_INFO path,
        out string sourceName)
    {
        var source = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            header =
            {
                type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME),
                adapterId = path.sourceInfo.adapterId,
                id = path.sourceInfo.id,
            },
        };

        var result = PInvoke.DisplayConfigGetDeviceInfo(
            (DISPLAYCONFIG_DEVICE_INFO_HEADER*)&source);
        sourceName = result == 0 ? source.viewGdiDeviceName.ToString().Trim() : string.Empty;
        return sourceName.Length > 0;
    }

    [SupportedOSPlatform("windows6.1")]
    private static unsafe bool TryGetTargetIdentity(
        DISPLAYCONFIG_PATH_INFO path,
        out DisplayConfigIdentity identity)
    {
        var target = new DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            header =
            {
                type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                size = (uint)sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME),
                adapterId = path.targetInfo.adapterId,
                id = path.targetInfo.id,
            },
        };

        var result = PInvoke.DisplayConfigGetDeviceInfo(
            (DISPLAYCONFIG_DEVICE_INFO_HEADER*)&target);
        if (result != 0)
        {
            identity = default!;
            return false;
        }

        var friendlyName = target.monitorFriendlyDeviceName.ToString().Trim();
        if (friendlyName.Length == 0)
        {
            identity = default!;
            return false;
        }

        var advancedColor = TryGetAdvancedColor(path, out var colorInfo);
        identity = new DisplayConfigIdentity(
            friendlyName,
            FriendlyOutputTechnology(target.outputTechnology.ToString()),
            target.edidManufactureId,
            target.edidProductCodeId,
            advancedColor ? colorInfo.advancedColorSupported : null,
            advancedColor ? colorInfo.advancedColorEnabled : null,
            advancedColor ? colorInfo.bitsPerColorChannel : null);
        return true;
    }

    [SupportedOSPlatform("windows6.1")]
    private static unsafe bool TryGetAdvancedColor(
        DISPLAYCONFIG_PATH_INFO path,
        out DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO colorInfo)
    {
        var info = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
        {
            header =
            {
                type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO,
                size = (uint)sizeof(DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO),
                adapterId = path.targetInfo.adapterId,
                id = path.targetInfo.id,
            },
        };

        var succeeded = PInvoke.DisplayConfigGetDeviceInfo(
            (DISPLAYCONFIG_DEVICE_INFO_HEADER*)&info) == 0;
        colorInfo = info;
        return succeeded;
    }

    private static string FriendlyOutputTechnology(string value) =>
        value.Replace("DISPLAYCONFIG_OUTPUT_TECHNOLOGY_", string.Empty, StringComparison.Ordinal)
            .Replace('_', ' ')
            .ToLowerInvariant();
}

internal sealed record DisplayConfigIdentity(
    string FriendlyName,
    string OutputTechnology,
    ushort EdidManufacturerId,
    ushort EdidProductCode,
    bool? AdvancedColorSupported,
    bool? AdvancedColorEnabled,
    uint? BitsPerColorChannel);
