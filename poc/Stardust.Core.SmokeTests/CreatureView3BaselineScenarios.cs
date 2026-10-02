using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView3BaselineScenarios
{
    public static void Run(byte[] wire)
    {
        var limits = new CreatureView3BaselineLimits(wire.Length - 23, 3, 3, 3, 119);
        var value = CreatureView3BaselineCodec.Decode(wire, limits);
        if (!CreatureView3BaselineCodec.Encode(value, limits).SequenceEqual(wire) || value.DecodedBytes != 119)
            throw new Exception("Baseline round trip.");
        void Reject(byte[] input, CreatureView3BaselineLimits? budget = null)
        {
            try { CreatureView3BaselineCodec.Decode(input, budget ?? limits); }
            catch (InvalidDataException) { return; }
            throw new Exception("Malformed baseline accepted.");
        }
        for (int n = 0; n < wire.Length; n++) Reject(wire[..n]);
        for (int n = 25; n < wire.Length; n++)
        {
            byte[] partial = wire[..n]; BinaryPrimitives.WriteUInt32LittleEndian(partial.AsSpan(19), (uint)(n - 23)); Reject(partial);
        }
        byte[] tail = [.. wire, 0]; BinaryPrimitives.WriteUInt32LittleEndian(tail.AsSpan(19), (uint)(tail.Length - 23));
        Reject(tail, limits with { MaximumPayloadBytes = tail.Length - 23 });
        foreach (int offset in new[] { 2, 14, 18 }) { var bad = (byte[])wire.Clone(); bad[offset] ^= 1; Reject(bad); }
        var nonfinite = (byte[])wire.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(nonfinite.AsSpan(25), 0x7FC00000); Reject(nonfinite);
        foreach (int offset in new[] { 31, 46, 62, 69, 132 })
        { var bad = (byte[])wire.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset), uint.MaxValue); Reject(bad); }
        var height = (byte[])wire.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(height.AsSpan(116), 0x7F800000); Reject(height);
        // Synthetic duplicate set entries are retained; original runtime normalization is unknown.
        var duplicate = (byte[])wire.Clone(); duplicate.AsSpan(77, 4).CopyTo(duplicate.AsSpan(81, 4));
        if (!CreatureView3BaselineCodec.Encode(CreatureView3BaselineCodec.Decode(duplicate, limits), limits).SequenceEqual(duplicate)) throw new Exception("Raw component entries.");
        Reject(wire, limits with { MaximumPayloadBytes = limits.MaximumPayloadBytes - 1 });
        Reject(wire, limits with { MaximumStringBytes = 2 });
        Reject(wire, limits with { MaximumUnicodeUnits = 2 });
        Reject(wire, limits with { MaximumContainerElements = 2 });
        Reject(wire, limits with { MaximumDecodedBytes = 118 });
        var owned = (byte[])wire.Clone(); var snapshot = CreatureView3BaselineCodec.Decode(owned, limits); Array.Clear(owned);
        var bytes = snapshot.NameFile.CopyBytes(); Array.Clear(bytes);
        if (!CreatureView3BaselineCodec.Encode(snapshot, limits).SequenceEqual(wire)) throw new Exception("Ownership.");
        try { ((IList<ushort>)snapshot.CustomNameUnits)[0] = 0; throw new Exception("Mutable units."); } catch (NotSupportedException) { }
        try { ((IList<int>)snapshot.Components.Values)[0] = 0; throw new Exception("Mutable components."); } catch (NotSupportedException) { }
        Console.WriteLine("CREO:3 round trip, truncation, tail, schema, budgets and ownership passed.");
    }
}
