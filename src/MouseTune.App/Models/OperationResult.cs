namespace MouseTune.Models;

public sealed class OperationResult
{
    public bool Succeeded { get; init; }
    public bool RequiresElevation { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }

    public static OperationResult Success(string message) => new() { Succeeded = true, Message = message };

    public static OperationResult Failure(string message, string? errorCode = null, bool requiresElevation = false) =>
        new() { Succeeded = false, Message = message, ErrorCode = errorCode, RequiresElevation = requiresElevation };
}
