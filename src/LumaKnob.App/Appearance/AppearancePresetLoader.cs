using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Media;
using LumaKnob.App.Services;

namespace LumaKnob.App.Appearance;

internal static partial class AppearancePresetLoader
{
    public const int CurrentSchemaVersion = 2;
    private const long MaximumPresetBytes = 64 * 1024;

    private static readonly HashSet<string> AllowedColorTokens =
    [
        "KnobOuterBrush",
        "KnobFaceBrush",
        "KnobHubBrush",
        "KnobRimBrush",
        "KnobShadowBrush",
        "KnobTickBrush",
        "KnobActiveTickBrush",
        "KnobMarkerBrush",
        "KnobFocusBrush",
        "KnobHighlightBrush",
        "PanelBaseBrush",
        "PanelRaisedBrush",
        "PanelRecessBrush",
        "PanelHighlightBrush",
        "PanelShadowBrush",
        "DisplayGlassBrush",
        "GreenLedBrush",
        "AmberLedBrush",
        "BlueLedBrush",
        "BackplaneBaseBrush",
        "BackplaneGridBrush",
        "BackplaneSignalBrush",
        "BackplaneGlowBrush",
        "BackplanePulseBrush",
        "BackplaneScanBrush",
        "ScrewFaceBrush",
        "ScrewSlotBrush",
    ];

    public static string PathName { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LumaKnob",
        "appearance.json");

    public static void TryApply(Application application, string? pathName = null)
    {
        var selectedPath = string.IsNullOrWhiteSpace(pathName) ? PathName : pathName;
        if (!File.Exists(selectedPath))
        {
            if (!string.IsNullOrWhiteSpace(pathName))
            {
                DiagnosticLog.Write(
                    $"APPEARANCE Requested preset was not found: {Path.GetFileName(selectedPath)}.");
            }

            return;
        }

        try
        {
            if (new FileInfo(selectedPath).Length > MaximumPresetBytes)
            {
                DiagnosticLog.Write("APPEARANCE Preset was ignored safely because it exceeds 64 KiB.");
                return;
            }

            var json = File.ReadAllText(selectedPath);
            var preset = Parse(json);
            if (preset is null)
            {
                return;
            }

            foreach (var (token, value) in preset.Colors)
            {
                application.Resources[token] = new SolidColorBrush(value);
            }

            ApplyOptionalResource(application, "BackplaneMotionEnabled", preset.MotionEnabled);
            ApplyOptionalResource(application, "BackplaneMotionSpeed", preset.MotionSpeed);
            ApplyOptionalResource(application, "BackplaneMotionIntensity", preset.MotionIntensity);
            ApplyOptionalResource(application, "BackplaneFramesPerSecond", preset.FramesPerSecond);
            ApplyOptionalResource(application, "KnobDepthScale", preset.KnobDepth);

            if (preset.PanelDepth is double panelDepth)
            {
                application.Resources["PanelBevelThickness"] = new Thickness(
                    panelDepth,
                    panelDepth,
                    panelDepth * 2,
                    panelDepth * 2);
            }

            if (preset.CornerRadius is double cornerRadius)
            {
                application.Resources["PanelCornerRadius"] = new CornerRadius(cornerRadius);
            }

            DiagnosticLog.Write(
                $"APPEARANCE Applied preset \"{preset.Name}\" with {preset.Colors.Count} color token(s), " +
                $"motion {DescribeOptional(preset.MotionEnabled)}, depth {DescribeOptional(preset.KnobDepth)}; " +
                $"source {Path.GetFileName(selectedPath)}.");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            DiagnosticLog.Write($"APPEARANCE Preset was ignored safely: {exception.Message}");
        }
    }

    internal static ValidatedAppearancePreset? Parse(string json)
    {
        var preset = JsonSerializer.Deserialize(json, AppearancePresetJsonContext.Default.AppearancePreset)
            ?? throw new JsonException("The appearance preset was empty.");
        if (preset.SchemaVersion is not (1 or CurrentSchemaVersion))
        {
            DiagnosticLog.Write(
                $"APPEARANCE Ignored preset schema {preset.SchemaVersion}; " +
                $"this build supports schemas 1 and {CurrentSchemaVersion}.");
            return null;
        }

        var colors = new Dictionary<string, Color>(StringComparer.Ordinal);
        foreach (var (token, value) in preset.Colors ?? EmptyColors)
        {
            if (!AllowedColorTokens.Contains(token) || value is null || !IsHexColor(value))
            {
                continue;
            }

            colors[token] = Color.Parse(value);
        }

        var visuals = preset.SchemaVersion == CurrentSchemaVersion ? preset.Visuals : null;
        return new ValidatedAppearancePreset(
            string.IsNullOrWhiteSpace(preset.Name) ? "Unnamed" : preset.Name.Trim(),
            colors,
            visuals?.MotionEnabled,
            InRange(visuals?.MotionSpeed, 0.1, 3),
            InRange(visuals?.MotionIntensity, 0, 1),
            InRange(visuals?.FramesPerSecond, 10, 60),
            InRange(visuals?.KnobDepth, 0.5, 2.5),
            InRange(visuals?.PanelDepth, 0.5, 3),
            InRange(visuals?.CornerRadius, 0, 12));
    }

    private static IReadOnlyDictionary<string, string?> EmptyColors { get; } =
        new Dictionary<string, string?>();

    private static void ApplyOptionalResource<T>(Application application, string key, T? value)
        where T : struct
    {
        if (value is T actual)
        {
            application.Resources[key] = actual;
        }
    }

    private static double? InRange(double? value, double minimum, double maximum) =>
        value is double actual && double.IsFinite(actual) && actual >= minimum && actual <= maximum
            ? actual
            : null;

    private static string DescribeOptional<T>(T? value) where T : struct =>
        value?.ToString() ?? "default";

    private static bool IsHexColor(string? value)
    {
        if (value is null || value.Length is not (7 or 9) || value[0] != '#')
        {
            return false;
        }

        return value.AsSpan(1).ToString().All(Uri.IsHexDigit);
    }

    private sealed record AppearancePreset(
        int SchemaVersion,
        string? Name,
        IReadOnlyDictionary<string, string?>? Colors,
        AppearanceVisuals? Visuals);

    private sealed record AppearanceVisuals(
        bool? MotionEnabled,
        double? MotionSpeed,
        double? MotionIntensity,
        double? FramesPerSecond,
        double? KnobDepth,
        double? PanelDepth,
        double? CornerRadius);

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(AppearancePreset))]
    private sealed partial class AppearancePresetJsonContext : JsonSerializerContext;
}

internal sealed record ValidatedAppearancePreset(
    string Name,
    IReadOnlyDictionary<string, Color> Colors,
    bool? MotionEnabled,
    double? MotionSpeed,
    double? MotionIntensity,
    double? FramesPerSecond,
    double? KnobDepth,
    double? PanelDepth,
    double? CornerRadius);
