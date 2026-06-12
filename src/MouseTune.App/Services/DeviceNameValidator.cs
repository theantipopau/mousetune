using MouseTune.Models;

namespace MouseTune.Services;

public static class DeviceNameValidator
{
    public const int MaximumLength = 64;

    public static OperationResult Validate(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return OperationResult.Failure("Device name cannot be empty.", "EmptyName");
        }

        if (trimmed.Length > MaximumLength)
        {
            return OperationResult.Failure("Device name must be 64 characters or fewer.", "NameTooLong");
        }

        if (trimmed.Any(char.IsControl))
        {
            return OperationResult.Failure("Device name cannot contain control characters.", "ControlCharacter");
        }

        return OperationResult.Success(trimmed);
    }

    public static string Normalize(string? name) => (name ?? string.Empty).Trim();
}
