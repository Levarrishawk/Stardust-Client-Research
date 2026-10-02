using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

/// <summary>Independent offline human/terminal peer against accepted codecs and registry only.</summary>
public static class MixedObjectLifecyclePeerScenarios
{
    private const ulong Human = 0xF122334455667788, Terminal = 0xFE02030405060708, Generation = 42;
    private const uint Seed = 0xA1B2C3D4;
    private const int PayloadLimit = 768;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static string Hex(ReadOnlySpan<byte> value) => Convert.ToHexString(value);
    private static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value);
    private static uint[] RawBits(SceneCreateObjectByCrcMessage v) => [Bits(v.QuaternionX), Bits(v.QuaternionY), Bits(v.QuaternionZ), Bits(v.QuaternionW), Bits(v.PositionX), Bits(v.PositionZ), Bits(v.PositionY)];
    private static object[] Snapshots(SceneObjectRegistry registry) => registry.Snapshots().Select(v => (object)new
    {
        ObjectId = v.Handle.ObjectId, v.Handle.Revision, v.Incarnation.CreationRevision,
        v.Handle.SceneGeneration, Phase = v.Phase.ToString(), v.ContainerId, v.ArrangementBits,
        CreateHex = Hex(SceneObjectLifecycleCodec.EncodeCreate(v.Create)), v.Create.TemplateCrc
    }).ToArray();

    private static (object Value, Dictionary<string, object> Message) Decode(string kind, byte[] wire)
    {
        object value; byte[] encoded; ulong id;
        var message = new Dictionary<string, object> { ["Kind"] = kind, ["WireHex"] = Hex(wire) };
        switch (kind)
        {
            case "create":
                var create = SceneObjectLifecycleCodec.DecodeCreate(wire); value = create; id = create.ObjectId;
                encoded = SceneObjectLifecycleCodec.EncodeCreate(create);
                message["Bits"] = RawBits(create); message["TemplateCrc"] = create.TemplateCrc; message["Flag"] = create.RawHyperspaceFlag;
                break;
            case "destroy":
                var destroy = SceneObjectLifecycleCodec.DecodeDestroy(wire); value = destroy; id = destroy.ObjectId;
                encoded = SceneObjectLifecycleCodec.EncodeDestroy(destroy); message["Flag"] = destroy.RawHyperspaceFlag;
                break;
            case "contain":
                var contain = SceneObjectLifecycleCodec.DecodeContainment(wire); value = contain; id = contain.ObjectId;
                encoded = SceneObjectLifecycleCodec.EncodeContainment(contain);
                message["ContainerId"] = contain.ContainerId; message["ArrangementBits"] = contain.ArrangementBits;
                break;
            case "end":
                var end = SceneObjectLifecycleCodec.DecodeEndBaselines(wire); value = end; id = end.ObjectId;
                encoded = SceneObjectLifecycleCodec.EncodeEndBaselines(end);
                break;
            case "baseline":
            case "delta":
                var envelope = kind == "baseline" ? ReplicationEnvelopeCodec.DecodeBaseline(wire, PayloadLimit) : ReplicationEnvelopeCodec.DecodeDelta(wire, PayloadLimit);
                value = envelope; id = envelope.ObjectId;
                encoded = kind == "baseline" ? ReplicationEnvelopeCodec.EncodeBaseline(envelope, PayloadLimit) : ReplicationEnvelopeCodec.EncodeDelta(envelope, PayloadLimit);
                message["RawTag"] = envelope.RawTag; message["View"] = envelope.View;
                message["DeclaredCount"] = envelope.DeclaredCount; message["OpaqueHex"] = Hex(envelope.CopyOpaqueFields());
                Check(envelope.RawTag == 0x54414E4F && envelope.View == 3, "Only synthetic opaque TANO3 fixture expected.");
                break;
            default: throw new ArgumentException("Unknown fixture kind.");
        }
        Check(encoded.AsSpan().SequenceEqual(wire), "Exact whole application re-encoding.");
        message["ObjectId"] = id;
        return (value, message);
    }

    private static bool Apply(SceneObjectRegistry registry, ObjectTemplateCatalog catalog, ulong generation, object value) => value switch
    {
        SceneCreateObjectByCrcMessage c => ApplyCreate(registry, catalog, generation, c),
        SceneDestroyObjectMessage d => registry.Destroy(generation, d),
        UpdateContainmentMessage c => registry.Contain(generation, c),
        SceneEndBaselinesMessage e => registry.EndBaselines(generation, e),
        ReplicationEnvelope e => registry.Find(e.ObjectId) is not null ? true : throw new InvalidDataException("Missing opaque recipient."),
        _ => throw new ArgumentException("Unknown DTO.")
    };
    private static bool ApplyCreate(SceneObjectRegistry registry, ObjectTemplateCatalog catalog, ulong generation, SceneCreateObjectByCrcMessage create)
    {
        if (generation == registry.SceneGeneration) _ = catalog.Require(create.TemplateCrc);
        return registry.Create(generation, create) is not null;
    }
    private static void Reject(Action action)
    {
        try { action(); } catch (InvalidDataException) { return; }
        throw new Exception("Malformed fixture accepted.");
    }
    private static SoeClientTransport NewTransport(byte[] response)
    {
        var transport = new SoeClientTransport(); transport.BeginSession(0x12345678, 496);
        transport.AcceptSessionResponse(response); return transport;
    }

    public static void Run(string peerPath, string exportPath)
    {
        byte[] peerBytes = File.ReadAllBytes(peerPath);
        Check(peerBytes.Length <= 1048576, "Bounded authored fixture bytes.");
        var peer = JsonSerializer.Deserialize<Peer>(peerBytes) ?? throw new InvalidDataException("Peer required.");
        Check(peer.Version == 1 && peer.Applications.Length == 12 && peer.Negatives.Length == 10 && peer.PacketsHex.Length == 13 && peer.TranscriptHex.Length == 16, "Bounded v1 peer shape.");
        byte[] catalogBytes = Convert.FromHexString(peer.CatalogHex);
        string catalogHash = Hex(SHA256.HashData(catalogBytes));
        Check(catalogHash.Equals(peer.CatalogSha256, StringComparison.OrdinalIgnoreCase), "Catalog identity.");
        var catalog = ObjectTemplateCatalogProfile.Decode(catalogBytes); Array.Clear(catalogBytes);
        Check(catalog.Entries.Count == 2 && catalog.Require(2937962913).Recipe == SceneObjectResourceRecipe.CreatureDetail0 && catalog.Require(2907007746).Recipe == SceneObjectResourceRecipe.StaticAppearance, "Two accepted catalog CRCs/recipes.");
        byte[] response = Convert.FromHexString(peer.SessionResponseHex);
        var transport = NewTransport(response); var registry = new SceneObjectRegistry(2, Generation);
        var traces = new List<object>(); var records = new List<object>(); var placements = new List<object>();
        var negatives = new List<object>(); var epochs = new List<object>();
        var packets = new List<string>(); var wires = new List<string>();
        int cursor = 0, malformedProbes = 0;
        SceneObjectSnapshot? firstHuman = null, beforeContainment = null, stableTerminal = null;
        bool containmentKeepsIncarnation = false, containmentStalesHandle = false, recreateStalesIncarnation = false, otherObjectPreserved = false;
        bool foreignRejected = false, staleRejected = false, resetStales = false;
        try
        {
            Check(peer.PacketsHex.Length == peer.WiresHex.Length, "Packet/wire pairs.");
            for (int i = 0; i < peer.PacketsHex.Length; i++)
            {
                byte[] packet = Convert.FromHexString(peer.PacketsHex[i]);
                byte[] wire = SoeDatagramCodec.EncodeUncompressed(packet, Seed, 2, 496);
                Check(Hex(wire) == peer.WiresHex[i], "Independent framed wire.");
                packets.Add(Hex(SoeDatagramCodec.Decode(wire, Seed, 2, 496))); wires.Add(Hex(wire));
            }
            foreach (string wireHex in peer.TranscriptHex)
            {
                var incoming = transport.AcceptDatagram(Convert.FromHexString(wireHex));
                traces.Add(new { Rejected = incoming.RejectedFraming, Delivered = incoming.ApplicationPayloads.Select(v => Hex(v)).ToArray(), Controls = incoming.ControlDatagrams.Select(v => Hex(v)).ToArray() });
                foreach (byte[] bytes in incoming.ApplicationPayloads)
                {
                    Check(cursor < peer.Applications.Length, "No extra transport delivery.");
                    var app = peer.Applications[cursor]; Check(Hex(bytes) == app.Hex, "Ordered whole application.");
                    var decoded = Decode(app.Kind, bytes);
                    Array.Clear(bytes); // Decoded DTO/envelope must own all state.
                    if (cursor == 6) { beforeContainment = registry.Find(Human)!; stableTerminal = registry.Find(Terminal)!; }
                    Check(Apply(registry, catalog, Generation, decoded.Value), "Accepted lifecycle application.");
                    if (cursor == 0) firstHuman = registry.Find(Human)!;
                    if (cursor == 6)
                    {
                        containmentKeepsIncarnation = registry.IsCurrent(beforeContainment!.Incarnation);
                        containmentStalesHandle = !registry.IsCurrent(beforeContainment.Handle);
                    }
                    if (cursor == 9)
                    {
                        recreateStalesIncarnation = !registry.IsCurrent(firstHuman!.Incarnation) && registry.Find(Human)!.Incarnation.CreationRevision > firstHuman.Incarnation.CreationRevision;
                        otherObjectPreserved = ReferenceEquals(registry.Find(Terminal), stableTerminal);
                    }
                    if (decoded.Value is SceneCreateObjectByCrcMessage create)
                    {
                        var placement = SceneCreatePlacementBuilder.Create(create, SceneCreateZeroQuaternionPolicy.Reject);
                        Check(Hex(SceneObjectLifecycleCodec.EncodeCreate(placement.RawCreate)) == app.Hex, "Placement retains exact raw create.");
                        placements.Add(new { create.ObjectId, RawBits = RawBits(placement.RawCreate), TranslationBits = new[] { Bits(placement.Transform.Translation.X), Bits(placement.Transform.Translation.Y), Bits(placement.Transform.Translation.Z) } });
                    }
                    records.Add(new { Message = decoded.Message, Snapshots = Snapshots(registry) }); cursor++;
                }
            }
            Check(cursor == peer.Applications.Length, "Complete transport delivery.");
            foreach (var app in peer.Applications)
            {
                byte[] source = Convert.FromHexString(app.Hex);
                for (int length = 0; length < source.Length; length++) { int n = length; Reject(() => Decode(app.Kind, source[..n])); malformedProbes++; }
                Reject(() => Decode(app.Kind, [..source, 0])); malformedProbes++;
                for (int offset = 0; offset < 6; offset++) { byte[] bad = (byte[])source.Clone(); bad[offset] ^= 1; Reject(() => Decode(app.Kind, bad)); malformedProbes++; }
            }
            byte[] baselineBytes = Convert.FromHexString(peer.Applications[2].Hex);
            var opaque = ReplicationEnvelopeCodec.DecodeBaseline(baselineBytes, baselineBytes.Length - 23);
            Reject(() => ReplicationEnvelopeCodec.DecodeBaseline(baselineBytes, baselineBytes.Length - 24));
            string originalOpaque = Hex(opaque.CopyOpaqueFields()); byte[] copied = opaque.CopyOpaqueFields(); Array.Clear(copied); Array.Clear(baselineBytes);
            Check(Hex(opaque.CopyOpaqueFields()) == originalOpaque, "Opaque owned fields.");
            var before = registry.Snapshots();
            var humanCreate = (SceneCreateObjectByCrcMessage)Decode("create", Convert.FromHexString(peer.Applications[0].Hex)).Value;
            var terminalCreate = (SceneCreateObjectByCrcMessage)Decode("create", Convert.FromHexString(peer.Applications[1].Hex)).Value;
            var foreign = new SceneObjectRegistry(2, Generation);
            try { foreignRejected = !registry.IsCurrent(foreign.Create(Generation, humanCreate)!.Incarnation); } finally { foreign.Close(); }
            staleRejected = registry.Create(Generation-1, humanCreate) is null && registry.Replace(Generation+1, humanCreate) is null &&
                !registry.Contain(Generation-1, new(Human, Terminal, 1)) && !registry.EndBaselines(Generation+1, new(Human)) &&
                !registry.Destroy(Generation-1, new(Human, 255)) && before.SequenceEqual(registry.Snapshots());
            registry.Reset(Generation+1); resetStales = before.All(v => !registry.IsCurrent(v.Incarnation));
            epochs.Add(new { Name = "reset", Snapshots = Snapshots(registry) });
            Check(registry.Create(Generation, humanCreate) is null && registry.Count == 0, "Replay previous generation after reset.");
            foreach (int index in new[] { 0, 1, 4, 5 })
            {
                var app = peer.Applications[index]; var value = Decode(app.Kind, Convert.FromHexString(app.Hex)).Value;
                Check(Apply(registry, catalog, Generation+1, value), "New epoch.");
                epochs.Add(new { Name = app.Kind, Snapshots = Snapshots(registry) });
            }
            foreach (var negative in peer.Negatives)
            {
                var failedRegistry = new SceneObjectRegistry(2, Generation); var failedTransport = NewTransport(response);
                bool rejected = false, unchanged = false, suffixApplied = false;
                try
                {
                    failedRegistry.Create(Generation, humanCreate); failedRegistry.Create(Generation, terminalCreate);
                    failedRegistry.Contain(Generation, new(Human, Terminal, 0xFFFFFFFE));
                    var prior = failedRegistry.Snapshots();
                    // Prime a future frame: fatal application failure must close and drain transport too.
                    failedTransport.AcceptDatagram(Convert.FromHexString(peer.WiresHex[1]));
                    try
                    {
                        var invalid = Decode(negative.Kind, Convert.FromHexString(negative.Hex));
                        Apply(failedRegistry, catalog, Generation, invalid.Value);
                        failedRegistry.EndBaselines(Generation, new(Human)); suffixApplied = true;
                    }
                    catch (InvalidDataException)
                    { rejected = true; unchanged = prior.SequenceEqual(failedRegistry.Snapshots()); }
                }
                finally { failedRegistry.Close(); failedTransport.Close(); }
                bool closedReplayRejected = false;
                try { failedTransport.AcceptDatagram(Convert.FromHexString(peer.WiresHex[0])); }
                catch (InvalidOperationException) { closedReplayRejected = true; }
                Check(rejected && unchanged && !suffixApplied && closedReplayRejected && failedTransport.State == SoeClientTransportState.Closed && failedTransport.PendingMessageCount == 0 && failedTransport.RetainedOutboundPackets == 0, "Late failure atomicity and closure.");
                negatives.Add(new { negative.Name, Rejected = rejected, UnchangedBeforeClose = unchanged, Closed = failedRegistry.IsClosed, Remaining = failedRegistry.Count, SuffixApplied = suffixApplied });
            }
        }
        finally { registry.Close(); transport.Close(); }
        epochs.Add(new { Name = "close", Snapshots = Snapshots(registry) });
        Check(containmentKeepsIncarnation && containmentStalesHandle && recreateStalesIncarnation && otherObjectPreserved && foreignRejected && staleRejected && resetStales && registry.IsClosed && transport.State == SoeClientTransportState.Closed, "Identity and closure checks.");
        var result = new
        {
            CatalogSha256 = catalogHash, Transport = traces, PacketsHex = packets, WiresHex = wires,
            Records = records, Placements = placements, MalformedProbes = malformedProbes, Negatives = negatives, Epochs = epochs,
            IdentityChecks = new { ContainmentKeepsIncarnation = containmentKeepsIncarnation, ContainmentStalesHandle = containmentStalesHandle,
                RecreateStalesIncarnation = recreateStalesIncarnation, OtherObjectPreserved = otherObjectPreserved,
                ForeignIncarnationRejected = foreignRejected, StaleGenerationRejected = staleRejected,
                ResetStalesIncarnations = resetStales, Closed = registry.IsClosed }
        };
        string full = Path.GetFullPath(exportPath); Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, JsonSerializer.SerializeToUtf8Bytes(result, Json));
        Console.WriteLine($"Mixed human/terminal lifecycle peer passed: {cursor} ordered records, {malformedProbes} malformed probes, {negatives.Count} late failures; closed.");
    }

    public sealed record Application(string Kind, string Hex);
    public sealed record Negative(string Name, string Kind, string Hex);
    public sealed record Peer(int Version, string Label, string CatalogHex, string CatalogSha256,
        string SessionResponseHex, Application[] Applications, string[] PacketsHex, string[] WiresHex,
        string[] TranscriptHex, Negative[] Negatives);
}
