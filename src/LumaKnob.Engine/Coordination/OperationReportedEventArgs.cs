namespace LumaKnob.Engine.Coordination;

public sealed class OperationReportedEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}
