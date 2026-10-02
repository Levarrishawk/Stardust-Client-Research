using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;

public static class SceneObjectLifecycleScenarios
{
    // Hand-authored synthetic bytes; quaternion deliberately not unit length.
    public const string CreateHex = "0500EADD89FE88776655443322F10000803E000000BF0000403F0000A03F000020C10000A8410000F4C1443322F1A5";
    public const string DestroyHex = "030004D5454D88776655443322F1FF";
    public const string ContainmentHex = "04009EDECB5688776655443322F108070605040302FEFEFFFFFF";
    public const string EndHex = "02003760432C88776655443322F1";
    public static SceneCreateObjectByCrcMessage CreateValue() => new(0xF122334455667788, .25f, -.5f, .75f, 1.25f, -10f, 21f, -30.5f, 0xF1223344, 0xA5);
    public static void Run()
    {
        byte[] create = Convert.FromHexString(CreateHex), destroy = Convert.FromHexString(DestroyHex), containment = Convert.FromHexString(ContainmentHex), end = Convert.FromHexString(EndHex);
        SceneCreateObjectByCrcMessage c = CreateValue();
        SceneDestroyObjectMessage d = new(c.ObjectId, 255);
        UpdateContainmentMessage u = new(c.ObjectId, 0xFE02030405060708, 0xFFFFFFFE);
        SceneEndBaselinesMessage e = new(c.ObjectId);
        Check(SceneObjectLifecycleCodec.EncodeCreate(c).SequenceEqual(create));
        Check(SceneObjectLifecycleCodec.EncodeDestroy(d).SequenceEqual(destroy));
        Check(SceneObjectLifecycleCodec.EncodeContainment(u).SequenceEqual(containment));
        Check(SceneObjectLifecycleCodec.EncodeEndBaselines(e).SequenceEqual(end));
        Check(SceneObjectLifecycleCodec.DecodeCreate(create) == c);
        Check(SceneObjectLifecycleCodec.DecodeDestroy(destroy) == d);
        Check(SceneObjectLifecycleCodec.DecodeContainment(containment) == u);
        Check(SceneObjectLifecycleCodec.DecodeEndBaselines(end) == e);
        Action<byte[]>[] decoders = [b => SceneObjectLifecycleCodec.DecodeCreate(b), b => SceneObjectLifecycleCodec.DecodeDestroy(b), b => SceneObjectLifecycleCodec.DecodeContainment(b), b => SceneObjectLifecycleCodec.DecodeEndBaselines(b)];
        byte[][] vectors = [create, destroy, containment, end];
        for (int index = 0; index < vectors.Length; index++)
        {
            byte[] vector = vectors[index]; Action<byte[]> decode = decoders[index];
            for (int length = 0; length < vector.Length; length++) Reject(() => decode(vector[..length]));
            Reject(() => decode([..vector, 0]));
            for (int offset = 0; offset < 6; offset++)
            { byte[] bad = (byte[])vector.Clone(); bad[offset] ^= 1; Reject(() => decode(bad)); }
            for (int other = 0; other < vectors.Length; other++) if (other != index) Reject(() => decode(vectors[other]));
        }
        for (int offset = 14; offset < 42; offset += 4)
            foreach (uint bits in new[] { 0x7F800000u, 0xFF800000u, 0x7FC00000u })
            { byte[] bad = (byte[])create.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset), bits); Reject(() => SceneObjectLifecycleCodec.DecodeCreate(bad)); }
        Func<float, SceneCreateObjectByCrcMessage>[] fields = [v => c with { QuaternionX = v }, v => c with { QuaternionY = v }, v => c with { QuaternionZ = v }, v => c with { QuaternionW = v }, v => c with { PositionX = v }, v => c with { PositionZ = v }, v => c with { PositionY = v }];
        foreach (var field in fields) foreach (float v in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Reject(() => SceneObjectLifecycleCodec.EncodeCreate(field(v)));
        for (int flag = 0; flag <= 255; flag++)
        {
            Check(SceneObjectLifecycleCodec.DecodeCreate(SceneObjectLifecycleCodec.EncodeCreate(c with { RawHyperspaceFlag = (byte)flag })).RawHyperspaceFlag == flag);
            Check(SceneObjectLifecycleCodec.DecodeDestroy(SceneObjectLifecycleCodec.EncodeDestroy(d with { RawHyperspaceFlag = (byte)flag })).RawHyperspaceFlag == flag);
        }
        // Finite bit patterns and zero/non-unit quaternion remain unaltered.
        SceneCreateObjectByCrcMessage zeros = c with { QuaternionX = -0.0f, QuaternionY = 0, QuaternionZ = 0, QuaternionW = 0, PositionX = float.Epsilon, PositionZ = float.MaxValue, PositionY = float.MinValue };
        byte[] unusual = SceneObjectLifecycleCodec.EncodeCreate(zeros);
        Check(SceneObjectLifecycleCodec.EncodeCreate(SceneObjectLifecycleCodec.DecodeCreate(unusual)).SequenceEqual(unusual));
        SceneCreateObjectByCrcMessage owned = SceneObjectLifecycleCodec.DecodeCreate(create);
        Array.Fill(create, (byte)0); Check(owned == c);
        byte[] first = SceneObjectLifecycleCodec.EncodeCreate(c); Array.Fill(first, (byte)0);
        Check(SceneObjectLifecycleCodec.EncodeCreate(c).SequenceEqual(Convert.FromHexString(CreateHex)));
        Check(SceneObjectLifecycleCodec.DecodeContainment(SceneObjectLifecycleCodec.EncodeContainment(new(0, 0, uint.MaxValue))).ArrangementBits == uint.MaxValue);
        Null(() => SceneObjectLifecycleCodec.EncodeCreate(null!)); Null(() => SceneObjectLifecycleCodec.EncodeDestroy(null!)); Null(() => SceneObjectLifecycleCodec.EncodeContainment(null!)); Null(() => SceneObjectLifecycleCodec.EncodeEndBaselines(null!));
        Console.WriteLine("R4-NET-01 full lifecycle byte, malformed, finite, raw-flag and ownership scenarios passed.");
    }
    private static void Check(bool value) { if (!value) throw new Exception("Lifecycle assertion failed."); }
    private static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Malformed lifecycle accepted."); }
    private static void Null(Action action) { try { action(); } catch (ArgumentNullException) { return; } throw new Exception("Null DTO accepted."); }
}
