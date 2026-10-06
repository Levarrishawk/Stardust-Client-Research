using System.Text;
using System.Text.Json;

namespace Stardust.Core.Session;

/// <summary>Immutable explicit modern offline movement policy; no world or driver effects.</summary>
public sealed class TerrainOfflinePlaygroundSettings
{
    public const int MaximumJsonBytes = 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private TerrainOfflinePlaygroundSettings(int version, int maximumUpdates, int maximumClickAttempts,
        long stepMicroseconds, float walkSpeed, float runSpeed, float arrivalRadius, bool holdMissingDestination)
    {
        Version = version; MaximumUpdates = maximumUpdates; MaximumClickAttempts = maximumClickAttempts;
        StepMicroseconds = stepMicroseconds; WalkSpeed = walkSpeed; RunSpeed = runSpeed;
        ArrivalRadius = arrivalRadius; HoldMissingDestination = holdMissingDestination;
    }
    public int Version { get; }
    public int MaximumUpdates { get; }
    public int MaximumClickAttempts { get; }
    public long StepMicroseconds { get; }
    public float WalkSpeed { get; }
    public float RunSpeed { get; }
    public float ArrivalRadius { get; }
    public bool HoldMissingDestination { get; }

    public static TerrainOfflinePlaygroundSettings Create(int version = 1, int maximumUpdates = 6000,
        int maximumClickAttempts = 128, long stepMicroseconds = 10000, float walkSpeed = 3,
        float runSpeed = 4, float arrivalRadius = .001f, bool holdMissingDestination = true)
    {
        if (version != 1) throw new ArgumentOutOfRangeException(nameof(version));
        if (maximumUpdates is < 1 or > 6000) throw new ArgumentOutOfRangeException(nameof(maximumUpdates));
        if (maximumClickAttempts is < 1 or > 128) throw new ArgumentOutOfRangeException(nameof(maximumClickAttempts));
        if (stepMicroseconds != 10000) throw new ArgumentOutOfRangeException(nameof(stepMicroseconds));
        if (!float.IsFinite(walkSpeed) || walkSpeed <= 0 || walkSpeed > 10) throw new ArgumentOutOfRangeException(nameof(walkSpeed));
        if (!float.IsFinite(runSpeed) || runSpeed < walkSpeed || runSpeed > 10) throw new ArgumentOutOfRangeException(nameof(runSpeed));
        if (!float.IsFinite(arrivalRadius) || arrivalRadius < 1e-4f || arrivalRadius > 1) throw new ArgumentOutOfRangeException(nameof(arrivalRadius));
        return new(version, maximumUpdates, maximumClickAttempts, stepMicroseconds, walkSpeed, runSpeed, arrivalRadius, holdMissingDestination);
    }

    public TerrainOfflineSessionBudget CreateBudget() => new(MaximumUpdates, MaximumClickAttempts);

    public static TerrainOfflinePlaygroundSettings Parse(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > MaximumJsonBytes) throw Invalid();
        try
        {
            // Validate all bytes, including property names, before JSON decoding can replace malformed UTF-8.
            _ = StrictUtf8.GetCharCount(utf8);
            using var document = JsonDocument.Parse(utf8.ToArray(), new JsonDocumentOptions
                { MaxDepth = 1, AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Invalid();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!names.Add(property.Name) || property.Name is not (
                    "Version" or "MaximumUpdates" or "MaximumClickAttempts" or "StepMicroseconds" or
                    "WalkSpeed" or "RunSpeed" or "ArrivalRadius" or "HoldMissingDestination")) throw Invalid();
            }
            if (names.Count != 8) throw Invalid();
            static int Integer(JsonElement root, string name)
            {
                var value = root.GetProperty(name);
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result)) throw Invalid();
                return result;
            }
            static float Single(JsonElement root, string name)
            {
                var value = root.GetProperty(name);
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetSingle(out var result)) throw Invalid();
                return result;
            }
            var hold = root.GetProperty("HoldMissingDestination");
            if (hold.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw Invalid();
            return Create(Integer(root, "Version"), Integer(root, "MaximumUpdates"), Integer(root, "MaximumClickAttempts"),
                Integer(root, "StepMicroseconds"), Single(root, "WalkSpeed"), Single(root, "RunSpeed"),
                Single(root, "ArrivalRadius"), hold.GetBoolean());
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or OverflowException)
        {
            throw Invalid();
        }
    }
    private static InvalidDataException Invalid() => new("Invalid offline playground settings schema or values.");
}
