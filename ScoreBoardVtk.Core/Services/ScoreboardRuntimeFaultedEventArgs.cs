namespace ScoreBoardVtk.Core.Services;

public sealed class ScoreboardRuntimeFaultedEventArgs(string operationName, Exception exception) : EventArgs
{
    public string OperationName { get; } = operationName;

    public Exception Exception { get; } = exception;
}
