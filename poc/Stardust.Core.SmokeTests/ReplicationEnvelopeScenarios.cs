using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class ReplicationEnvelopeScenarios
{
    public const string BaselineHex = "05000C5FA76888776655443322F1443322F1FF06000000FFFFA1B200FF";
    public const string DeltaHex = "05005321861208070605040302FE4F455243030300000000007F";
    public static void Run()
    {
        byte[] baseline = Convert.FromHexString(BaselineHex), delta = Convert.FromHexString(DeltaHex);
        var b = ReplicationEnvelopeCodec.DecodeBaseline(baseline, 6); var d = ReplicationEnvelopeCodec.DecodeDelta(delta, 3);
        Check(b.ObjectId == 0xF122334455667788 && b.RawTag == 0xF1223344 && b.View == 255 && b.DeclaredCount == 65535 && b.PayloadSize == 6 && b.CopyOpaqueFields().SequenceEqual(new byte[] { 0xA1,0xB2,0,255 }));
        Check(d.ObjectId == 0xFE02030405060708 && d.RawTag == 0x4352454F && d.View == 3 && d.DeclaredCount == 0 && d.PayloadSize == 3 && d.CopyOpaqueFields().Single() == 127);
        Check(ReplicationEnvelopeCodec.EncodeBaseline(b, 6).SequenceEqual(baseline)); Check(ReplicationEnvelopeCodec.EncodeDelta(d, 3).SequenceEqual(delta));
        foreach (bool isDelta in new[] { false, true })
        {
            byte[] vector = isDelta ? delta : baseline;
            Action<byte[]> decode = bytes => { if (isDelta) ReplicationEnvelopeCodec.DecodeDelta(bytes, 100); else ReplicationEnvelopeCodec.DecodeBaseline(bytes, 100); };
            for (int size = 0; size < vector.Length; size++) Reject(() => decode(vector[..size]));
            Reject(() => decode([..vector, 0]));
            for (int offset = 0; offset < 6; offset++) { byte[] bad = (byte[])vector.Clone(); bad[offset] ^= 1; Reject(() => decode(bad)); }
            foreach (uint size in new[] { 0u,1u,2u,100u,uint.MaxValue })
            { byte[] bad=(byte[])vector.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(19),size); Reject(() => decode(bad)); }
            for (int view = 0; view <= 255; view++)
            {
                var envelope = new ReplicationEnvelope(isDelta ? ReplicationEnvelopeKind.Delta : ReplicationEnvelopeKind.Baseline,0,uint.MaxValue,(byte)view,ushort.MaxValue,[],2);
                byte[] encoded = isDelta ? ReplicationEnvelopeCodec.EncodeDelta(envelope,2) : ReplicationEnvelopeCodec.EncodeBaseline(envelope,2);
                var decoded = isDelta ? ReplicationEnvelopeCodec.DecodeDelta(encoded,2) : ReplicationEnvelopeCodec.DecodeBaseline(encoded,2);
                Check(encoded.Length == 25 && decoded.OpaqueFieldLength == 0 && decoded.View == view && decoded.RawTag == uint.MaxValue && decoded.DeclaredCount == ushort.MaxValue);
            }
        }
        Reject(() => ReplicationEnvelopeCodec.DecodeDelta(baseline,100)); Reject(() => ReplicationEnvelopeCodec.DecodeBaseline(delta,100));
        Reject(() => ReplicationEnvelopeCodec.EncodeDelta(b,100)); Reject(() => ReplicationEnvelopeCodec.EncodeBaseline(d,100));
        Reject(() => ReplicationEnvelopeCodec.DecodeBaseline(baseline,5)); Reject(() => ReplicationEnvelopeCodec.DecodeDelta(delta,2));
        Reject(() => ReplicationEnvelopeCodec.EncodeBaseline(b,5)); Reject(() => ReplicationEnvelopeCodec.EncodeDelta(d,2));
        Reject(() => new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline,1,2,3,4,new byte[5],6));
        foreach (int budget in new[] { -1,0,1,int.MaxValue })
        { Throws<ArgumentOutOfRangeException>(() => ReplicationEnvelopeCodec.DecodeBaseline(baseline,budget)); Throws<ArgumentOutOfRangeException>(() => ReplicationEnvelopeCodec.EncodeDelta(d,budget)); Throws<ArgumentOutOfRangeException>(() => new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,0,0,0,0,[],budget)); }
        Throws<ArgumentOutOfRangeException>(() => new ReplicationEnvelope((ReplicationEnvelopeKind)9,0,0,0,0,[],2));
        Throws<ArgumentNullException>(() => ReplicationEnvelopeCodec.EncodeBaseline(null!,2));
        byte[] borrowed = [1,2]; var owned = new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline,1,2,3,65535,borrowed,4); Array.Fill(borrowed,(byte)9);
        Check(owned.CopyOpaqueFields().SequenceEqual(new byte[] { 1,2 })); var copy=owned.CopyOpaqueFields(); copy[0]=9; Check(owned.CopyOpaqueFields()[0]==1);
        Array.Fill(baseline,(byte)0); Check(b.RawTag==0xF1223344 && b.CopyOpaqueFields()[0]==0xA1);
        byte[] output=ReplicationEnvelopeCodec.EncodeBaseline(b,6); Array.Fill(output,(byte)0); Check(ReplicationEnvelopeCodec.EncodeBaseline(b,6).SequenceEqual(Convert.FromHexString(BaselineHex)));
        Console.WriteLine("R6-NET-01 exact application envelope, raw counts/tag/views, sizes/budgets and owned opaque-byte scenarios passed.");
    }
    private static void Check(bool value) { if (!value) throw new Exception("Replication envelope assertion failed."); }
    private static void Reject(Action action) => Throws<InvalidDataException>(action);
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
}
