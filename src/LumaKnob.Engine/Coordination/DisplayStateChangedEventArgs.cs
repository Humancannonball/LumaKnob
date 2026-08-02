namespace LumaKnob.Engine.Coordination;

public sealed class DisplayStateChangedEventArgs(string targetId) : EventArgs
{
    public string TargetId { get; } = targetId;
}
