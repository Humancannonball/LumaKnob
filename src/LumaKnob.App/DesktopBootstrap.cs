using Avalonia;
using LumaKnob.App.Services;
using LumaKnob.Engine.Platform;

namespace LumaKnob.App;

public static class DesktopBootstrap
{
    private static IDisplayPlatform? _platform;

    internal static event EventHandler? ActivationRequested;

    internal static string? AppearancePresetPath { get; private set; }

    internal static bool MotionDisabled { get; private set; }

    internal static IDisplayPlatform Platform =>
        _platform ?? throw new InvalidOperationException("The desktop platform adapter was not configured.");

    public static void Run(string[] args, IDisplayPlatform platform, AppBuilder appBuilder)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(appBuilder);
        if (_platform is not null)
        {
            throw new InvalidOperationException("The desktop host can only be started once per process.");
        }

        ReadPresentationArguments(args);
        _platform = platform;
        DiagnosticLog.BeginSession();
        appBuilder.StartWithClassicDesktopLifetime(args);
    }

    public static void RequestActivation() =>
        ActivationRequested?.Invoke(null, EventArgs.Empty);

    private static void ReadPresentationArguments(IReadOnlyList<string> args)
    {
        MotionDisabled = args.Any(argument =>
            string.Equals(argument, "--no-motion", StringComparison.OrdinalIgnoreCase));

        const string appearancePrefix = "--appearance=";
        var appearanceArgument = args.FirstOrDefault(argument =>
            argument.StartsWith(appearancePrefix, StringComparison.OrdinalIgnoreCase));
        if (appearanceArgument is not null)
        {
            var requestedPath = appearanceArgument[appearancePrefix.Length..].Trim('"');
            if (!string.IsNullOrWhiteSpace(requestedPath))
            {
                var workingDirectoryPath = Path.GetFullPath(requestedPath);
                var executableDirectoryPath = Path.GetFullPath(requestedPath, AppContext.BaseDirectory);
                AppearancePresetPath = File.Exists(workingDirectoryPath)
                    ? workingDirectoryPath
                    : executableDirectoryPath;
            }
        }
    }
}
