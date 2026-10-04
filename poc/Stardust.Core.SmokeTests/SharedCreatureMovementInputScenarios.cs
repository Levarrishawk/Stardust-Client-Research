using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SharedCreatureMovementInputScenarios
{
    private sealed record Resource(string Path, string Kind, string? Hex);
    private sealed record Fixture(string Name, string RootPath, string RootSha256, Resource[] Resources,
        SharedCreatureMovementInputLimits? Limits, bool Reject);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    internal static object Export(SharedCreatureMovementInputProfile p) => new {
        p.RootPath, p.Operational, p.SourceBytes, p.DecodedBytes, p.Work, p.Sources,
        Visits = p.Visits.Select(v => new { v.SourcePath, v.Version, v.ParentPath,
            Parameters = v.Parameters.ToDictionary(k => k.Key, k => k.Value is null ? null : new {
                k.Value.Mode, k.Value.RelativeByte, Hex = Convert.ToHexString(k.Value.CopyEncoding()),
                k.Value.LiteralBits }) }),
        Inputs = p.Inputs.ToDictionary(k => k.Key, k => new { Kind = k.Value.Kind.ToString(),
            Bits = k.Value.Value is float f ? (uint?)BitConverter.SingleToUInt32Bits(f) : null,
            k.Value.Field, Provenance = k.Value.Provenance.Select(v => v.SourcePath) })
    };
    internal static object Run(string fixturePath)
    {
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(fixturePath))!;
        var records = new List<object>(); int rejected = 0, ownership = 0, lifecycle = 0, boundaries = 0;
        foreach (var fixture in fixtures)
        {
            var resources = fixture.Resources.ToDictionary(r => r.Path, r => new TreLookupResult(Enum.Parse<TreLookupKind>(r.Kind),
                r.Hex is null ? null : Convert.FromHexString(r.Hex), 77)); var calls = new List<string>();
            TreLookupResult Resolve(string path) { calls.Add(path); return resources.GetValueOrDefault(path, new(TreLookupKind.Missing)); }
            try
            {
                var profile = SharedCreatureMovementInputProfile.Load(Resolve, fixture.RootPath, fixture.RootSha256, fixture.Limits);
                Check(!fixture.Reject, "Expected fixture rejection: " + fixture.Name);
                records.Add(new { fixture.Name, Rejected = false, Calls = calls.ToArray(), Profile = Export(profile) });
                string before = JsonSerializer.Serialize(Export(profile));
                foreach (var visit in profile.Visits) foreach (var raw in visit.Parameters.Values) if (raw is not null) Array.Clear(raw.CopyEncoding());
                foreach (var source in resources.Values) if (source.Payload is not null) Array.Clear(source.Payload);
                Check(before == JsonSerializer.Serialize(Export(profile)), "Owned movement snapshot: " + fixture.Name); ownership++;
            }
            catch (Exception error) when (error is InvalidDataException or ArgumentException or AppearanceGraphBudgetException)
            { Check(fixture.Reject, "Unexpected fixture rejection: " + fixture.Name + ": " + error.Message); rejected++; records.Add(new { fixture.Name, Rejected = true, Calls = calls.ToArray() }); }
        }
        var positive = fixtures.First(f => !f.Reject && f.Name == "inherited");
        var originals = positive.Resources.ToDictionary(r => r.Path, r => new TreLookupResult(TreLookupKind.Present, Convert.FromHexString(r.Hex!), 77));
        TreLookupResult Source(string path) => originals.GetValueOrDefault(path, new(TreLookupKind.Missing));
        void Reject(Action operation, Type type)
        {
            try { operation(); } catch (Exception e) when (type.IsInstanceOfType(e)) { lifecycle++; return; }
            throw new Exception("Expected lifecycle/boundary rejection: " + type.Name);
        }
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel(); int invoked = 0;
            Reject(() => SharedCreatureMovementInputProfile.Load(p => { invoked++; return Source(p); }, positive.RootPath, positive.RootSha256,
                cancellationToken: cancelled.Token), typeof(OperationCanceledException)); Check(invoked == 0, "Precancel before resolver");
        }
        using (var cancelled = new CancellationTokenSource())
            Reject(() => SharedCreatureMovementInputProfile.Load(p => { cancelled.Cancel(); return Source(p); }, positive.RootPath, positive.RootSha256,
                cancellationToken: cancelled.Token), typeof(OperationCanceledException));
        Reject(() => SharedCreatureMovementInputProfile.Load(p => {
            try { _ = SharedCreatureMovementInputProfile.Load(Source, positive.RootPath, positive.RootSha256); }
            catch (InvalidOperationException) { }
            return Source(p);
        }, positive.RootPath, positive.RootSha256), typeof(InvalidOperationException));
        var stable = SharedCreatureMovementInputProfile.Load(Source, positive.RootPath, positive.RootSha256);
        Check(stable.Operational, "Recovery after cancellation and reentry"); lifecycle++;
        var limits = new SharedCreatureMovementInputLimits(MaximumDecodedBytes: stable.DecodedBytes, MaximumWork: stable.Work);
        Check(SharedCreatureMovementInputProfile.Load(Source, positive.RootPath, positive.RootSha256, limits).Operational, "Exact aggregate boundaries"); boundaries++;
        Reject(() => SharedCreatureMovementInputProfile.Load(Source, positive.RootPath, positive.RootSha256,
            limits with { MaximumDecodedBytes = stable.DecodedBytes - 1 }), typeof(AppearanceGraphBudgetException)); boundaries++;
        Reject(() => SharedCreatureMovementInputProfile.Load(Source, positive.RootPath, positive.RootSha256,
            limits with { MaximumWork = stable.Work - 1 }), typeof(AppearanceGraphBudgetException)); boundaries++;
        int reads = 0;
        Reject(() => SharedCreatureMovementInputProfile.Load(p => { reads++; return Source(p); }, positive.RootPath, positive.RootSha256,
            new(MaximumDecodedBytes: 1)), typeof(AppearanceGraphBudgetException)); Check(reads == 0, "Capacity preflight before any resolver");
        var normalized = SharedCreatureMovementInputProfile.Load(Source, positive.RootPath.ToUpperInvariant().Replace('/', '\\'), positive.RootSha256);
        Check(normalized.RootPath == positive.RootPath, "Existing logical normalization"); lifecycle++;
        return new { Records = records, Rejected = rejected, Ownership = ownership, Lifecycle = lifecycle, Boundaries = boundaries };
    }
}
