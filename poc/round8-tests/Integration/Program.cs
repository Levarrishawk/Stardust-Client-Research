using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

try
{
    if (args.Length != 3) throw new ArgumentException("baseline.bin delta.bin export.json (independently authored offline fixtures)");
    var limits = new CreatureView3BaselineLimits(262144, 65536, 64, 64, 262144);
    var baseline = CreatureView3BaselineCodec.Decode(File.ReadAllBytes(args[0]), limits);
    var plan = CreatureView3ScalarDeltaCodec.Decode(File.ReadAllBytes(args[1]), 65536, 64);
    var registry = new SceneObjectRegistry(2, 1);
    using var store = new SceneCreatureScalarStore(registry, 2);
    var create = new SceneCreateObjectByCrcMessage(baseline.ObjectId, 0, 0, 0, 1, 0, 0, 0, 1, 0);
    registry.Create(1, create);
    registry.EndBaselines(1, new(baseline.ObjectId));
    var handle = registry.Find(baseline.ObjectId)!.Handle;
    var seeded = store.Seed(handle, baseline.Scalars)!;
    var applied = store.Apply(handle, plan)!;
    Check(!store.IsCurrent(seeded) && store.IsCurrent(applied), "New state publication supersedes seed");
    Check(Equal(seeded.State, baseline.Scalars), "Baseline seed retains exact bits");

    // A valid ordered prefix followed by an unsupported field cannot produce any partial mutation.
    var envelope = ReplicationEnvelopeCodec.DecodeDelta(File.ReadAllBytes(args[1]), 65536);
    byte[] badFields = envelope.CopyOpaqueFields().Concat(new byte[] { 255, 255 }).ToArray();
    byte[] malformed = ReplicationEnvelopeCodec.EncodeDelta(new(ReplicationEnvelopeKind.Delta, envelope.ObjectId,
        envelope.RawTag, envelope.View, envelope.DeclaredCount, badFields, 65536), 65536);
    bool rejected = false;
    try { store.Apply(handle, CreatureView3ScalarDeltaCodec.Decode(malformed, 65536, 64)); }
    catch (InvalidDataException) { rejected = true; }
    Check(rejected && ReferenceEquals(store.Find(handle), applied), "Malformed trailing update is atomic");

    registry.Destroy(1, new(baseline.ObjectId, 255));
    Check(store.Find(handle) is null && !store.IsCurrent(applied), "Destroy retires state");
    registry.Create(1, create); registry.EndBaselines(1, new(baseline.ObjectId));
    var fresh = registry.Find(baseline.ObjectId)!.Handle;
    Check(fresh != handle && store.Apply(handle, plan) is null && store.Find(fresh) is null,
        "Old handle cannot update a recreated ID or invent a seed");
    var reseeded = store.Seed(fresh, baseline.Scalars)!;
    Check(Equal(reseeded.State, baseline.Scalars) && store.IsCurrent(reseeded), "Explicit baseline reseed restores state");
    File.WriteAllText(args[2], JsonSerializer.Serialize(new { baseline.ObjectId, baseline.DeclaredCount,
        Initial = Export(seeded.State), Applied = Export(applied.State), Reseeded = Export(reseeded.State),
        AtomicMalformedRejected = rejected, OldHandleRetired = true, ExplicitReseedRequired = true }));
    store.Close(); registry.Close();
    Check(store.Snapshots().Count == 0, "Closed store releases snapshots");
    Console.WriteLine("PASS: full CREO3 baseline -> explicit scalar seed -> ordered delta; malformed rollback, destroy/recreate, stale handle and explicit reseed");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }

static void Check(bool value, string detail) { if (!value) throw new InvalidDataException(detail); }
static bool Equal(CreatureView3ScalarState a, CreatureView3ScalarState b)
    => a == b && BitConverter.SingleToUInt32Bits(a.Height) == BitConverter.SingleToUInt32Bits(b.Height);
static object Export(CreatureView3ScalarState value) => new { value.Posture, value.Rank, value.LinkId,
    HeightBits = BitConverter.SingleToUInt32Bits(value.Height), value.Shock, value.States };
