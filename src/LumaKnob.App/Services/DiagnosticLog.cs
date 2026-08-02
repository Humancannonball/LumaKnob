using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace LumaKnob.App.Services;

internal static class DiagnosticLog
{
    private static readonly object Gate = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LumaKnob",
        "logs");

    public static string PathName { get; } = Path.Combine(LogDirectory, "session.log");

    public static string PreviousPathName { get; } = Path.Combine(LogDirectory, "session.previous.log");

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Diagnostics must never prevent the application from starting.")]
    public static void BeginSession()
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(LogDirectory);
                if (File.Exists(PathName))
                {
                    File.Move(PathName, PreviousPathName, overwrite: true);
                }
            }
        }
        catch (Exception)
        {
        }

        Write("SESSION BEGIN");
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A failed diagnostic read is reported in-band and must not affect brightness control.")]
    public static string ReadSession()
    {
        try
        {
            lock (Gate)
            {
                return File.Exists(PathName)
                    ? File.ReadAllText(PathName, Encoding.UTF8)
                    : $"LumaKnob diagnostic log has not been created yet.{Environment.NewLine}";
            }
        }
        catch (Exception exception)
        {
            return $"Could not read the diagnostic log: {exception.Message}{Environment.NewLine}";
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Diagnostics must never break display recovery or brightness control.")]
    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(LogDirectory);
                RotateIfNeeded();
                var line = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
                File.AppendAllText(PathName, line, Encoding.UTF8);
            }
        }
        catch (Exception)
        {
        }
    }

    private static void RotateIfNeeded()
    {
        var file = new FileInfo(PathName);
        if (!file.Exists || file.Length < 1_000_000)
        {
            return;
        }

        File.Move(PathName, PreviousPathName, overwrite: true);
    }
}
