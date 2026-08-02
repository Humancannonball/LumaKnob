using Avalonia.Media;
using LumaKnob.App.Appearance;

namespace LumaKnob.App.Tests;

public sealed class AppearancePresetLoaderTests
{
    [Fact]
    public void SchemaTwoValidatesColorsMotionAndDepthIndependently()
    {
        const string json = """
            {
              "schemaVersion": 2,
              "name": "  Test Console  ",
              "colors": {
                "BackplaneSignalBrush": "#AA12EF34",
                "UnknownBrush": "#FFFFFF",
                "PanelBaseBrush": "not-a-color"
              },
              "visuals": {
                "motionEnabled": false,
                "motionSpeed": 1.4,
                "motionIntensity": 0.75,
                "framesPerSecond": 24,
                "knobDepth": 1.8,
                "panelDepth": 2,
                "cornerRadius": 6
              }
            }
            """;

        var preset = Assert.IsType<ValidatedAppearancePreset>(AppearancePresetLoader.Parse(json));

        Assert.Equal("Test Console", preset.Name);
        Assert.Equal(Color.Parse("#AA12EF34"), Assert.Single(preset.Colors).Value);
        Assert.False(preset.MotionEnabled);
        Assert.Equal(1.4, preset.MotionSpeed);
        Assert.Equal(0.75, preset.MotionIntensity);
        Assert.Equal(24, preset.FramesPerSecond);
        Assert.Equal(1.8, preset.KnobDepth);
        Assert.Equal(2, preset.PanelDepth);
        Assert.Equal(6, preset.CornerRadius);
    }

    [Fact]
    public void UnsafeVisualRangesFallBackWithoutDiscardingThePreset()
    {
        const string json = """
            {
              "schemaVersion": 2,
              "name": "Extreme",
              "visuals": {
                "motionSpeed": 99,
                "motionIntensity": -1,
                "framesPerSecond": 900,
                "knobDepth": 0,
                "panelDepth": 20,
                "cornerRadius": 50
              }
            }
            """;

        var preset = Assert.IsType<ValidatedAppearancePreset>(AppearancePresetLoader.Parse(json));

        Assert.Null(preset.MotionSpeed);
        Assert.Null(preset.MotionIntensity);
        Assert.Null(preset.FramesPerSecond);
        Assert.Null(preset.KnobDepth);
        Assert.Null(preset.PanelDepth);
        Assert.Null(preset.CornerRadius);
    }

    [Fact]
    public void LegacyColorPresetStillLoadsWithoutVisualExtensions()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "name": "Legacy",
              "colors": { "GreenLedBrush": "#A7ED79" },
              "visuals": { "motionEnabled": false }
            }
            """;

        var preset = Assert.IsType<ValidatedAppearancePreset>(AppearancePresetLoader.Parse(json));

        Assert.Single(preset.Colors);
        Assert.Null(preset.MotionEnabled);
    }

    [Fact]
    public void FutureSchemaIsRejected()
    {
        Assert.Null(AppearancePresetLoader.Parse("""{ "schemaVersion": 9000 }"""));
    }
}
