using System.Globalization;
using System.Management;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace LumaKnob.Windows.Displays;

[SupportedOSPlatform("windows")]
internal static class WindowsSystemInventory
{
    public static void AddTo(ICollection<string> diagnostics)
    {
        AddSingle(
            "SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem",
            item =>
                $"OS: {Value(item, "Caption")} {Value(item, "Version")} " +
                $"(build {Value(item, "BuildNumber")}).",
            diagnostics);
        AddSingle(
            "SELECT Manufacturer, Model FROM Win32_ComputerSystem",
            item => $"Computer: {Value(item, "Manufacturer")} {Value(item, "Model")}.",
            diagnostics);
        AddMany(
            "SELECT Name, DriverVersion FROM Win32_VideoController",
            (item, index) =>
                $"GPU {index + 1}: {Value(item, "Name")} (driver {Value(item, "DriverVersion")}).",
            diagnostics);
    }

    private static void AddSingle(
        string query,
        Func<ManagementBaseObject, string> format,
        ICollection<string> diagnostics) =>
        AddMany(query, (item, _) => format(item), diagnostics, takeFirst: true);

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Optional diagnostic inventory must never block display discovery or brightness recovery.")]
    private static void AddMany(
        string query,
        Func<ManagementBaseObject, int, string> format,
        ICollection<string> diagnostics,
        bool takeFirst = false)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();
            var index = 0;
            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    diagnostics.Add(format(item, index));
                }

                index++;
                if (takeFirst)
                {
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            diagnostics.Add($"System inventory was unavailable ({exception.Message}).");
        }
    }

    private static string Value(ManagementBaseObject item, string propertyName) =>
        Convert.ToString(item[propertyName], CultureInfo.InvariantCulture)?.Trim() is { Length: > 0 } value
            ? value
            : "unknown";
}
