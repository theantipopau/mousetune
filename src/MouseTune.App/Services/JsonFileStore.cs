using System.Text.Json;

namespace MouseTune.Services;

public sealed class JsonFileStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<T> ReadAsync<T>(string path, T fallback, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken).ConfigureAwait(false) ?? fallback;
        }
        catch (JsonException)
        {
            var recoveryPath = $"{path}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(path, recoveryPath, overwrite: true);
            return fallback;
        }
        catch (IOException)
        {
            return fallback;
        }
    }

    public async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, Options, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var validationStream = File.OpenRead(tempPath))
        {
            _ = await JsonSerializer.DeserializeAsync<T>(validationStream, Options, cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(path))
        {
            File.Copy(path, $"{path}.bak", overwrite: true);
        }

        File.Move(tempPath, path, overwrite: true);
    }
}
