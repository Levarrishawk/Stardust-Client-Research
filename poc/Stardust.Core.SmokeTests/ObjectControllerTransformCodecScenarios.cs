using System.Buffers.Binary;
using System.Text.Json;
using System.Numerics;
using Stardust.Core.Protocol;

namespace Stardust.Core.SmokeTests;

public static class ObjectControllerTransformCodecScenarios
{
    public sealed record Case(string Name, int Direction, string Hex, uint[] Bits);
    private static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static void Reject(Action action)
    { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Invalid frame accepted."); }
    public static void Run(string fixturePath, string outputPath)
    {
        var cases = JsonSerializer.Deserialize<Case[]>(File.ReadAllBytes(fixturePath)) ?? throw new Exception("Missing fixture.");
        Check(cases.Length == 32, "Fixture case count");
        int rejected = 0;
        var output = new List<object>();
        foreach (var item in cases)
        {
            byte[] bytes = Convert.FromHexString(item.Hex);
            var direction = (ObjectControllerTransformDirection)item.Direction;
            var value = ObjectControllerTransformCodec.Decode(bytes, direction);
            Check(Convert.ToHexString(ObjectControllerTransformCodec.Encode(value)) == item.Hex, "Exact roundtrip");
            uint[] bits = new[] { value.QuaternionX, value.QuaternionY, value.QuaternionZ, value.QuaternionW,
                value.PositionX, value.PositionZ, value.PositionY, value.Speed }.Select(BitConverter.SingleToUInt32Bits).ToArray();
            Check(bits.SequenceEqual(item.Bits), "Float bit order");
            Check(value.OuterWord == BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(22)), "Outer word");
            Check(value.Flags == BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(6)) && value.ObjectId == BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(14))
                && value.MovementCounter == BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(26)), "Integer fields");
            Check(value.ParentId == (bytes.Length == 70 ? BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(30)) : (ulong?)null), "Parent field");
            if (direction == ObjectControllerTransformDirection.ClientToServer)
            { Check(value.Timestamp == value.OuterWord, "Timestamp bits"); Check(value.WithTimestamp(uint.MaxValue).Timestamp == uint.MaxValue, "Typed timestamp update"); }
            else { Check(BitConverter.SingleToUInt32Bits(value.ServerScalar) == value.OuterWord, "Scalar bits"); Check(BitConverter.SingleToUInt32Bits(value.WithServerScalar(-0f).ServerScalar) == 0x80000000, "Typed scalar update"); }
            var q = new Vector4(value.QuaternionX, value.QuaternionY, value.QuaternionZ, value.QuaternionW);
            var position = new Vector3(value.PositionX, value.PositionY, value.PositionZ);
            var constructed = direction == ObjectControllerTransformDirection.ClientToServer
                ? ObjectControllerTransformMessage.ClientToServer(value.Kind, value.Flags, value.ObjectId, value.Timestamp, value.MovementCounter, value.ParentId, q, position, value.Speed)
                : ObjectControllerTransformMessage.ServerToClient(value.Kind, value.Flags, value.ObjectId, value.ServerScalar, value.MovementCounter, value.ParentId, q, position, value.Speed);
            Check(Convert.ToHexString(ObjectControllerTransformCodec.Encode(constructed)) == item.Hex, "Direction-specific factory");
            try { if (direction == ObjectControllerTransformDirection.ClientToServer) _ = value.ServerScalar; else _ = value.Timestamp;
                throw new Exception("Wrong-direction accessor admitted"); } catch (InvalidOperationException) { }
            for (int n = 0; n < bytes.Length; n++) { int length = n; Reject(() => ObjectControllerTransformCodec.Decode(bytes.AsSpan(0, length), direction)); rejected++; }
            Reject(() => ObjectControllerTransformCodec.Decode([..bytes, 0], direction)); rejected++;
            foreach (int offset in new[] { 0, 2, 10 })
            { byte[] bad = bytes.ToArray(); bad[offset] ^= 0x02; Reject(() => ObjectControllerTransformCodec.Decode(bad, direction)); rejected++; }
            int start = bytes.Length == 70 ? 38 : 30;
            foreach (uint badBits in new uint[] { 0x7FC00001, 0x7F800000, 0xFF800000 })
            {
                foreach (int offset in Enumerable.Range(0, 8).Select(i => start + i * 4).Concat(direction == ObjectControllerTransformDirection.ServerToClient ? new[] { 22 } : Array.Empty<int>()))
                { byte[] bad = bytes.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset), badBits); Reject(() => ObjectControllerTransformCodec.Decode(bad, direction)); rejected++; }
                float f = BitConverter.UInt32BitsToSingle(badBits);
                foreach (var bad in new[] { value with { QuaternionX = f }, value with { QuaternionY = f }, value with { QuaternionZ = f }, value with { QuaternionW = f },
                    value with { PositionX = f }, value with { PositionY = f }, value with { PositionZ = f }, value with { Speed = f } })
                { Reject(() => ObjectControllerTransformCodec.Encode(bad)); rejected++; }
                if (direction == ObjectControllerTransformDirection.ServerToClient) { Reject(() => ObjectControllerTransformCodec.Encode(value.WithServerScalar(f))); rejected++; }
                else Check(ObjectControllerTransformCodec.Decode(ObjectControllerTransformCodec.Encode(value.WithTimestamp(badBits)), direction).Timestamp == badBits, "All timestamp words admitted");
            }
            Reject(() => ObjectControllerTransformCodec.Decode(bytes, (ObjectControllerTransformDirection)99));
            Reject(() => ObjectControllerTransformCodec.Encode(value with { Direction = (ObjectControllerTransformDirection)99 }));
            Reject(() => ObjectControllerTransformCodec.Encode(value with { Kind = (ObjectControllerTransformKind)99 }));
            Reject(() => ObjectControllerTransformCodec.Encode(value with { ParentId = value.ParentId.HasValue ? null : 0UL })); rejected += 4;
            Array.Clear(bytes); Check(Convert.ToHexString(ObjectControllerTransformCodec.Encode(value)) == item.Hex, "Decoded ownership");
            byte[] encoded = ObjectControllerTransformCodec.Encode(value); Array.Clear(encoded);
            Check(Convert.ToHexString(ObjectControllerTransformCodec.Encode(value)) == item.Hex, "Encoded ownership");
            output.Add(new { item.Name, Hex = Convert.ToHexString(ObjectControllerTransformCodec.Encode(value)), Bits = bits });
        }
        File.WriteAllText(outputPath, JsonSerializer.Serialize(new { Cases = output, Rejected = rejected }));
        Console.WriteLine($"PASS {cases.Length} synthetic cases; {rejected} malformed checks");
    }
}
