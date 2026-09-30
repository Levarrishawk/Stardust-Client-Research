using System.Text.Json;

namespace Stardust.Core.Configuration;

/// <summary>Bounded, strict UTF-8 JSON settings. Credential fields are never accepted.</summary>
public static class LoginClientConfiguration
{
    public const int MaximumConfigurationBytes = 8_192;

    public static LoginClientOptions Load(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        // Read one byte beyond the limit without trusting seekability or Length.
        byte[] buffer = new byte[MaximumConfigurationBytes + 1];
        int count = 0;
        while (count < buffer.Length)
        {
            int read = source.Read(buffer, count, buffer.Length - count);
            if (read == 0) break;
            count += read;
        }
        if (count > MaximumConfigurationBytes)
            throw new InvalidDataException("Login configuration exceeds the local size limit.");
        return Parse(buffer.AsMemory(0, count));
    }

    public static LoginClientOptions Parse(ReadOnlyMemory<byte> utf8)
    {
        if (utf8.Length > MaximumConfigurationBytes)
            throw new InvalidDataException("Login configuration exceeds the local size limit.");
        try
        {
            using JsonDocument document = JsonDocument.Parse(utf8, new JsonDocumentOptions { MaxDepth = 8 });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Invalid();
            HashSet<string> keys = new(StringComparer.Ordinal);
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!keys.Add(property.Name) || property.Name is not (
                    "host" or "port" or "clientVersion" or "maximumDatagramSize" or
                    "sessionRetryMilliseconds" or "sessionTimeoutMilliseconds" or
                    "reliableRetransmitMilliseconds" or "loginResponseTimeoutMilliseconds"))
                    throw Invalid();
            }
            return new LoginClientOptions(
                String(root, "host"), Integer(root, "port"), String(root, "clientVersion"),
                checked((uint)OptionalInteger(root, "maximumDatagramSize", 496)),
                TimeSpan.FromMilliseconds(OptionalInteger(root, "sessionRetryMilliseconds", 1000)),
                TimeSpan.FromMilliseconds(OptionalInteger(root, "sessionTimeoutMilliseconds", 15000)),
                TimeSpan.FromMilliseconds(OptionalInteger(root, "reliableRetransmitMilliseconds", 2000)),
                TimeSpan.FromMilliseconds(OptionalInteger(root, "loginResponseTimeoutMilliseconds", 30000)));
        }
        catch (Exception error) when (error is JsonException or ArgumentException or OverflowException or InvalidOperationException)
        {
            // Parser and option messages can contain source text; do not carry them outward.
            throw Invalid();
        }
    }

    private static string String(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
            throw Invalid();
        return value.GetString()!;
    }

    private static int Integer(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out JsonElement value) || !value.TryGetInt32(out int number))
            throw Invalid();
        return number;
    }

    private static int OptionalInteger(JsonElement root, string name, int fallback)
        => root.TryGetProperty(name, out _) ? Integer(root, name) : fallback;

    private static InvalidDataException Invalid() => new("Invalid login configuration schema or values.");
}
