using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView3ScalarDeltaScenarios
{
    public const string FieldsHex = "0B00FF0C00FE0D0008070605040302FE0E000000E0BF0F00443322F110008796A5B4C3D2E1F00B00070E0000000080";
    public const string GoldenHex = "05005321861288776655443322F14F45524303310000000000" + FieldsHex;
    public static CreatureView3ScalarState Initial() => new(1,2,3,4.5f,5,6);
    public static void Run()
    {
        byte[] golden = Convert.FromHexString(GoldenHex), fields = Convert.FromHexString(FieldsHex);
        var plan = CreatureView3ScalarDeltaCodec.Decode(golden,49,8);
        Check(plan.ObjectId == 0xF122334455667788 && plan.DeclaredCount == 0 && plan.Updates.Count == 8);
        Check(plan.Updates.Select(v => v.Index).SequenceEqual(new ushort[] {11,12,13,14,15,16,11,14}));
        var initial = Initial(); var applied = plan.Apply(initial);
        Check(applied.Posture == 7 && applied.Rank == 254 && applied.LinkId == 0xFE02030405060708 && applied.Shock == 0xF1223344 && applied.States == 0xF0E1D2C3B4A59687 && BitConverter.SingleToUInt32Bits(applied.Height) == 0x80000000);
        Check(initial == Initial()); Check(CreatureView3ScalarDeltaCodec.Encode(plan,49,8).SequenceEqual(golden));
        byte[] highCount = (byte[])golden.Clone(); highCount[23]=255; highCount[24]=255;
        var highPlan = CreatureView3ScalarDeltaCodec.Decode(highCount,49,8); Check(highPlan.DeclaredCount==65535 && highPlan.Apply(initial)==applied && CreatureView3ScalarDeltaCodec.Encode(highPlan,49,8).SequenceEqual(highCount));
        for (int length=0; length<golden.Length; length++) Reject(() => CreatureView3ScalarDeltaCodec.Decode(golden[..length],49,8));
        Reject(() => CreatureView3ScalarDeltaCodec.Decode([..golden,0],50,8));
        int[] boundaries=[0,3,6,16,22,28,38,41,47];
        for (int size=0; size<fields.Length; size++)
        {
            var envelope = new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,CreatureView3ScalarDeltaCodec.RawTag,3,65535,fields[..size],49);
            if (!boundaries.Contains(size)) Reject(() => CreatureView3ScalarDeltaCodec.Decode(envelope,8));
            else Check(CreatureView3ScalarDeltaCodec.Decode(envelope,8).Updates.Count == Array.IndexOf(boundaries,size));
        }
        foreach (ushort index in new ushort[] {0,10,17,65535})
        { byte[] bad = [11,0,255,0,0]; BinaryPrimitives.WriteUInt16LittleEndian(bad.AsSpan(3),index); Reject(() => CreatureView3ScalarDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,CreatureView3ScalarDeltaCodec.RawTag,3,1,bad,7),8)); Check(initial==Initial()); }
        foreach (int offset in new[] {18,43}) foreach (uint bits in new[] {0x7F800000u,0xFF800000u,0x7FC00000u})
        { byte[] bad=(byte[])fields.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset),bits); Reject(() => CreatureView3ScalarDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,CreatureView3ScalarDeltaCodec.RawTag,3,0,bad,49),8)); }
        Reject(() => CreatureView3ScalarDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline,1,CreatureView3ScalarDeltaCodec.RawTag,3,0,[],2),0));
        Reject(() => CreatureView3ScalarDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,0x4F455243,3,0,[],2),0));
        Reject(() => CreatureView3ScalarDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,CreatureView3ScalarDeltaCodec.RawTag,255,0,[],2),0));
        Reject(() => CreatureView3ScalarDeltaCodec.Decode(golden,48,8)); Reject(() => CreatureView3ScalarDeltaCodec.Decode(golden,49,7));
        Reject(() => CreatureView3ScalarDeltaCodec.Encode(plan,48,8)); Reject(() => CreatureView3ScalarDeltaCodec.Encode(plan,49,7));
        Throws<ArgumentOutOfRangeException>(() => CreatureView3ScalarDeltaCodec.Decode(golden,49,-1)); Throws<ArgumentOutOfRangeException>(() => CreatureView3ScalarDeltaCodec.Decode(golden,1,8));
        Throws<ArgumentOutOfRangeException>(() => CreatureView3ScalarDeltaCodec.Encode(plan,int.MaxValue,8));
        var empty = CreatureView3ScalarDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,0,CreatureView3ScalarDeltaCodec.RawTag,3,65535,[],2),0);
        Check(empty.Updates.Count==0 && ReferenceEquals(empty.Apply(initial),initial));
        var firstFour = CreatureView3ScalarDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,CreatureView3ScalarDeltaCodec.RawTag,3,0,fields[..22],24),4);
        Check(firstFour.Apply(initial).Height == -1.75f); // no clamp, normalization or sign repair
        Array.Fill(golden,(byte)0); Array.Fill(fields,(byte)0);
        Check(plan.Apply(initial)==applied && CreatureView3ScalarDeltaCodec.Encode(plan,49,8).SequenceEqual(Convert.FromHexString(GoldenHex)));
        Throws<NotSupportedException>(() => ((IList<CreatureView3ScalarUpdate>)plan.Updates)[0] = new CreaturePostureUpdate(0));
        byte[] exported=CreatureView3ScalarDeltaCodec.Encode(plan,49,8); exported[0]=0; Check(CreatureView3ScalarDeltaCodec.Encode(plan,49,8)[0]==5);
        Reject(() => plan.Apply(initial with { Height = float.NaN })); Throws<ArgumentNullException>(() => plan.Apply(null!));
        Console.WriteLine("R7-NET-01 typed ordered scalar plan, count tolerance, atomic rejection, bits/budgets and ownership scenarios passed.");
    }
    private static void Check(bool value) { if (!value) throw new Exception("Scalar delta assertion failed."); }
    private static void Reject(Action action) => Throws<InvalidDataException>(action);
    private static void Throws<T>(Action action) where T:Exception { try {action();} catch(T){return;} throw new Exception($"Expected {typeof(T).Name}"); }
}
