using System.Buffers.Binary;
using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SharedCreatureMovementInputLimits(int MaximumDepth = 16, int MaximumResources = 16,
    int MaximumTemplateBytes = 1048576, long MaximumSourceBytes = 1048576,
    int MaximumParameterBytes = 65536, int MaximumParameterNesting = 16, int MaximumArrayEntries = 64,
    long MaximumDecodedBytes = 8388608, long MaximumWork = 8388608);
public enum SharedCreatureMovementInputKind { Absent, Literal, Unsupported }

/// <summary>Owned serialized parameter; copied exports cannot mutate the retained input.</summary>
public sealed class SharedCreatureMovementParameter
{
    private readonly byte[] raw;
    internal SharedCreatureMovementParameter(byte mode, byte relative, byte[] raw, float? literal)
    { Mode = mode; RelativeByte = relative; this.raw = raw; Literal = literal; }
    public byte Mode { get; }
    public byte RelativeByte { get; }
    internal float? Literal { get; }
    public uint? LiteralBits => Literal is float value ? BitConverter.SingleToUInt32Bits(value) : null;
    public int Bytes => raw.Length;
    public byte[] CopyEncoding() => raw.ToArray();
}
public sealed record SharedCreatureMovementVisit(string SourcePath, string? Version, string? ParentPath,
    IReadOnlyDictionary<string, SharedCreatureMovementParameter?> Parameters);
public sealed record SharedCreatureMovementInput(SharedCreatureMovementInputKind Kind, float? Value,
    ResolvedTemplateField? Field, IReadOnlyList<SharedCreatureMovementVisit> Provenance);

/// <summary>Authored input profile only; no angle conversion, RNG, simulation or replicated-state defaults.</summary>
public sealed class SharedCreatureMovementInputProfile
{
    private sealed class Frame { public bool Violated; }
    [ThreadStatic] private static Frame? active;
    private static readonly string[] slots = ["speed[0]", "speed[1]", "slopeModAngle", "slopeModPercent", "waterModPercent"];
    private SharedCreatureMovementInputProfile(string root, List<SharedCreatureMovementVisit> visits,
        Dictionary<string, SharedCreatureMovementInput> inputs, IReadOnlyDictionary<string, AssetSourceIdentity> sources,
        long decodedBytes, long work)
    {
        RootPath = root; Visits = visits.AsReadOnly(); Inputs = new ReadOnlyDictionary<string, SharedCreatureMovementInput>(inputs);
        Sources = new ReadOnlyDictionary<string, AssetSourceIdentity>(sources.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal));
        DecodedBytes = decodedBytes; Work = work;
    }
    public string RootPath { get; }
    public IReadOnlyList<SharedCreatureMovementVisit> Visits { get; }
    public IReadOnlyDictionary<string, SharedCreatureMovementInput> Inputs { get; }
    public IReadOnlyDictionary<string, AssetSourceIdentity> Sources { get; }
    public long SourceBytes => Sources.Values.Sum(s => (long)s.Bytes);
    public long DecodedBytes { get; }
    public long Work { get; }
    public bool Operational => Inputs.Values.All(p => p.Kind == SharedCreatureMovementInputKind.Literal);

    public static SharedCreatureMovementInputProfile Load(Func<string, TreLookupResult> resolve, string rootPath,
        string rootSha256, SharedCreatureMovementInputLimits? limits = null, CancellationToken cancellationToken = default)
    {
        if (active is not null) { active.Violated = true; throw new InvalidOperationException("Recursive movement input profile."); }
        ArgumentNullException.ThrowIfNull(resolve); ObjectTemplateCatalog.ValidatePin(rootSha256);
        limits ??= new(); Validate(limits); rootPath = ObjectPath(rootPath);
        var frame = new Frame(); active = frame;
        try
        {
            var budget = new Budget(limits, frame, cancellationToken); budget.Check();
            var cache = new OwnedAssetSourceCache(path => {
                budget.Check(); var source = resolve(path); budget.Check();
                Need(source is not null, "Null movement source.");
                if (source!.Kind == TreLookupKind.Present)
                {
                    Need(source.Payload is not null && source.Payload.Length <= limits.MaximumTemplateBytes, "Movement template byte limit.");
                    // Reserve before the cache owns a clone and before the accepted decoder copies its tree/parameters.
                    budget.Decoded(checked(source.Payload!.Length * 2L)); budget.WorkUnits(source.Payload.Length);
                }
                return source;
            }, new(MaximumResources: limits.MaximumResources, MaximumSourceBytes: limits.MaximumSourceBytes));
            budget.Decoded(limits.MaximumDepth * 8L);
            var visits = new List<SharedCreatureMovementVisit>(limits.MaximumDepth); var seen = new HashSet<string>(StringComparer.Ordinal);
            string? current = rootPath;
            while (current is not null)
            {
                budget.Check(); current = ObjectPath(current);
                Need(seen.Add(current), "Movement inheritance cycle.");
                Need(visits.Count < limits.MaximumDepth, "Movement inheritance depth limit.");
                byte[] source;
                if (visits.Count == 0) source = cache.ReadPinned(current, rootSha256);
                else { var found = cache.Read(current); Need(found.Kind == TreLookupKind.Present && found.Payload is not null, "Movement parent missing/masked."); source = found.Payload!; }
                budget.Check(); var decoded = SharedObjectTemplateDecoder.Decode(source); budget.Check();
                if (visits.Count == 0) Need(decoded.Layers[0].Family == "SCOT", "Movement profile requires SCOT root.");
                var layer = decoded.Layers.FirstOrDefault(l => l.Family == "SCOT");
                budget.Decoded(5 * 64); budget.WorkUnits(5);
                var values = slots.ToDictionary(s => s, _ => (SharedCreatureMovementParameter?)null, StringComparer.Ordinal);
                if (layer is not null)
                {
                    foreach (string name in new[] { "speed", "slopeModAngle", "slopeModPercent", "waterModPercent" })
                    {
                        if (!layer.Parameters.TryGetValue(name, out var memory)) continue;
                        var raw = memory.Span; Need(raw.Length <= limits.MaximumParameterBytes, "Movement parameter byte limit.");
                        int cursor = 0;
                        if (name == "speed")
                        {
                            int count = Int(raw, ref cursor, budget);
                            Need(count >= 0 && count <= limits.MaximumArrayEntries, "Movement speed count limit.");
                            for (int i = 0; i < count; i++)
                            {
                                var value = Parameter(raw, ref cursor, 0, budget, i < 2);
                                if (i < 2) values[slots[i]] = value;
                            }
                        }
                        else values[name] = Parameter(raw, ref cursor, 0, budget, true);
                        Need(cursor == raw.Length, "Trailing movement parameter bytes.");
                    }
                }
                visits.Add(new(current, layer?.Version, decoded.ParentPath,
                    new ReadOnlyDictionary<string, SharedCreatureMovementParameter?>(values)));
                current = decoded.ParentPath;
            }
            budget.Decoded(5 * 64); var inputs = new Dictionary<string, SharedCreatureMovementInput>(StringComparer.Ordinal);
            foreach (string slot in slots)
            {
                budget.Decoded(visits.Count * 8L);
                var provenance = new List<SharedCreatureMovementVisit>(visits.Count); SharedCreatureMovementInput? result = null;
                foreach (var visit in visits)
                {
                    budget.Check(); budget.WorkUnits(1); provenance.Add(visit);
                    var value = visit.Parameters[slot]; if (value is null || value.Mode == 0) continue;
                    bool supported = value.Mode == 1 && value.RelativeByte == 0x20;
                    result = new(supported ? SharedCreatureMovementInputKind.Literal : SharedCreatureMovementInputKind.Unsupported,
                        supported ? value.Literal : null, new(visit.SourcePath, "SCOT", visit.Version!), provenance.AsReadOnly()); break;
                }
                inputs.Add(slot, result ?? new(SharedCreatureMovementInputKind.Absent, null, null, provenance.AsReadOnly()));
            }
            budget.Decoded(cache.Sources.Count * 64L);
            budget.Check(); var profile = new SharedCreatureMovementInputProfile(rootPath, visits, inputs, cache.Sources, budget.DecodedTotal, budget.WorkTotal);
            budget.Check(); return profile;
        }
        finally { active = null; }
    }

    private static SharedCreatureMovementParameter? Parameter(ReadOnlySpan<byte> raw, ref int cursor, int depth, Budget budget, bool retain)
    {
        budget.Check(); Need(depth < budget.Limits.MaximumParameterNesting, "Movement parameter nesting limit.");
        int start = cursor; byte mode = Byte(raw, ref cursor, budget), relative = Byte(raw, ref cursor, budget); float? literal = null;
        switch (mode)
        {
            case 0: break;
            case 1:
                literal = BitConverter.Int32BitsToSingle(Int(raw, ref cursor, budget));
                Need(float.IsFinite(literal.Value), "Nonfinite movement literal outside operational slice."); break;
            case 2:
                int count = Int(raw, ref cursor, budget);
                Need(count >= 0 && count <= budget.Limits.MaximumArrayEntries, "Movement weighted count limit.");
                for (int i = 0; i < count; i++) { _ = Int(raw, ref cursor, budget); _ = Parameter(raw, ref cursor, depth + 1, budget, false); }
                break;
            case 3: _ = Int(raw, ref cursor, budget); _ = Int(raw, ref cursor, budget); break;
            default: throw new InvalidDataException("Unsupported serialized movement mode.");
        }
        if (!retain) return null;
        budget.Decoded(checked(cursor - start + 32L));
        return new(mode, relative, raw[start..cursor].ToArray(), literal);
    }
    private static byte Byte(ReadOnlySpan<byte> raw, ref int cursor, Budget budget)
    { Need(cursor < raw.Length, "Truncated movement header."); budget.WorkUnits(1); return raw[cursor++]; }
    private static int Int(ReadOnlySpan<byte> raw, ref int cursor, Budget budget)
    { Need(raw.Length - cursor >= 4, "Truncated movement scalar/count."); budget.WorkUnits(4); int result = BinaryPrimitives.ReadInt32LittleEndian(raw[cursor..]); cursor += 4; return result; }
    private static string ObjectPath(string path)
    {
        path = LegacyAssetPath.Normalize(path);
        Need(path.StartsWith("object/", StringComparison.Ordinal) && path.EndsWith(".iff", StringComparison.Ordinal)
            && !path.Contains(':') && !path.Split('/').Any(p => p is "." or ".."), "Invalid normalized movement object path."); return path;
    }
    private static void Validate(SharedCreatureMovementInputLimits p)
    {
        if (p.MaximumDepth is < 1 or > 16 || p.MaximumResources is < 1 or > 16 || p.MaximumTemplateBytes is < 1 or > 1048576 ||
            p.MaximumSourceBytes is < 1 or > 16777216 || p.MaximumParameterBytes is < 2 or > 65536 ||
            p.MaximumParameterNesting is < 1 or > 16 || p.MaximumArrayEntries is < 1 or > 64 ||
            p.MaximumDecodedBytes is < 1 or > 67108864 || p.MaximumWork is < 1 or > 67108864)
            throw new ArgumentOutOfRangeException(nameof(p));
    }
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    private sealed class Budget(SharedCreatureMovementInputLimits limits, Frame frame, CancellationToken token)
    {
        internal SharedCreatureMovementInputLimits Limits => limits;
        internal long DecodedTotal, WorkTotal;
        internal void Check() { if (frame.Violated) throw new InvalidOperationException("Movement callback reentry rejected."); token.ThrowIfCancellationRequested(); }
        internal void Decoded(long amount) { Check(); if (amount > limits.MaximumDecodedBytes - DecodedTotal) throw new AppearanceGraphBudgetException("Movement decoded-output aggregate limit."); DecodedTotal += amount; }
        internal void WorkUnits(long amount) { Check(); if (amount > limits.MaximumWork - WorkTotal) throw new AppearanceGraphBudgetException("Movement work aggregate limit."); WorkTotal += amount; }
    }
}
