namespace LumaKnob.Engine.Displays;

public sealed record DisplayDiscoveryReport(
    IReadOnlyList<IBrightnessTarget> Targets,
    IReadOnlyList<string> Diagnostics);
