using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView3WoundsDeltaScenarios
{
    public const ulong ObjectId = 0xF122334455667788;
    public static CreatureView3WoundsDeltaLimits Limits => new(4096, 4, 16, 32, 8);
    public static byte[] Field(uint counter, params byte[][] commands)
    {
        using MemoryStream stream = new(); using BinaryWriter writer = new(stream);
        writer.Write((ushort)17); writer.Write((uint)commands.Length); writer.Write(counter);
        foreach (var command in commands) writer.Write(command); return stream.ToArray();
    }
    public static byte[] Command(byte kind, ushort index = 0, params int[] values)
    {
        using MemoryStream stream = new(); using BinaryWriter writer = new(stream);
        writer.Write(kind);
        if (kind != 4) writer.Write(kind == 3 ? checked((ushort)values.Length) : index);
        foreach (int value in values) writer.Write(value); return stream.ToArray();
    }
    public static byte[] Envelope(byte[] fields, ushort declared = 0)
    {
        using MemoryStream stream = new(); using BinaryWriter writer = new(stream);
        writer.Write((ushort)5); writer.Write(0x12862153U); writer.Write(ObjectId); writer.Write(0x4352454FU);
        writer.Write((byte)3); writer.Write((uint)(fields.Length + 2)); writer.Write(declared); writer.Write(fields); return stream.ToArray();
    }
    public static byte[] GoldenFields() => Field(4,
        Command(0,1), Command(1,1,int.MinValue), Command(2,0,17), Command(3,0,-1,0,42), Command(4), Command(1,0,-8), Command(1,1,int.MaxValue));
    public static byte[] Golden() => Envelope(GoldenFields());
    public static CreatureView3WoundsState Initial() => new(ObjectId, 0xFFFFFFFD, new int[] {-2,int.MaxValue,-2},8);
    private static void Assert(bool condition, string label) { if (!condition) throw new Exception(label); }
    private static void Reject(Action action)
    { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected strict rejection."); }
    public static void Run()
    {
        byte[] wire = Golden(); var plan = CreatureView3WoundsDeltaCodec.Decode(wire, Limits); var initial = Initial();
        var result = plan.Apply(initial); Assert(result.Counter == 4 && result.Values.SequenceEqual(new[] {-8,int.MaxValue}),"Ordered commands/wrap.");
        Assert(initial.Counter == 0xFFFFFFFD && initial.Values.SequenceEqual(new[] {-2,int.MaxValue,-2}),"Initial unchanged.");
        Assert(CreatureView3WoundsDeltaCodec.Encode(plan,Limits).SequenceEqual(wire),"Plan encode.");
        foreach (var state in new[] { new CreatureView3WoundsState(ObjectId,0,new int[] {17,int.MinValue,-2},8),
            new CreatureView3WoundsState(ObjectId,2,Array.Empty<int>(),8), new CreatureView3WoundsState(ObjectId,3,new int[] {-8},8), result })
            Assert(plan.Apply(state).Values.SequenceEqual(result.Values),"Prefix/duplicate reconciliation.");
        foreach (uint counter in new uint[] {0xFFFFFFFC,5,0x7FFFFFFF}) Reject(()=>plan.Apply(new(ObjectId,counter,new int[] {1},8)));
        Reject(()=>plan.Apply(new(0,0xFFFFFFFD,new int[] {1},8)));
        for (int n=0;n<wire.Length;n++) Reject(()=>CreatureView3WoundsDeltaCodec.Decode(wire[..n],Limits));
        for (int n=1;n<GoldenFields().Length;n++) Reject(()=>CreatureView3WoundsDeltaCodec.Decode(Envelope(GoldenFields()[..n]),Limits));
        foreach (int offset in new[] {0,2,14,18,19}) { byte[] bad=(byte[])wire.Clone(); bad[offset]^=1; Reject(()=>CreatureView3WoundsDeltaCodec.Decode(bad,Limits)); }
        Reject(()=>CreatureView3WoundsDeltaCodec.Decode(Envelope([..GoldenFields(),0]),Limits));
        Reject(()=>CreatureView3WoundsDeltaCodec.Decode(Envelope([..GoldenFields(),11,0,0]),Limits));
        Reject(()=>CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(1,new byte[]{5})),Limits));
        byte[] huge=(byte[])wire.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(huge.AsSpan(27),uint.MaxValue); Reject(()=>CreatureView3WoundsDeltaCodec.Decode(huge,Limits));
        byte[] assigned=Envelope(Field(1,Command(3,0,1))); BinaryPrimitives.WriteUInt16LittleEndian(assigned.AsSpan(36),ushort.MaxValue); Reject(()=>CreatureView3WoundsDeltaCodec.Decode(assigned,Limits));
        foreach (var budget in new[] {Limits with{MaximumPayloadBytes=wire.Length-24}, Limits with{MaximumFields=0}, Limits with{MaximumCommands=6}, Limits with{MaximumValues=6}})
        { Reject(()=>CreatureView3WoundsDeltaCodec.Decode(wire,budget)); Reject(()=>CreatureView3WoundsDeltaCodec.Encode(plan,budget)); }
        Reject(()=>CreatureView3WoundsDeltaCodec.Decode(wire,Limits with{MaximumElements=2}).Apply(initial));
        foreach (byte kind in new byte[]{0,1,2})
        { var invalid=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(0xFFFFFFFE,Command(kind,ushort.MaxValue,kind==0 ? Array.Empty<int>() : new int[]{7}))),Limits); Reject(()=>invalid.Apply(initial)); }
        var growth=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(1,Command(1,1,7))),Limits with{MaximumElements=1});
        Reject(()=>growth.Apply(new(ObjectId,0,new int[]{1},1)));
        var laterFailure=CreatureView3WoundsDeltaCodec.Decode(Envelope([..GoldenFields(),..Field(5,Command(0,7))]),Limits);
        Reject(()=>laterFailure.Apply(initial)); Assert(initial.Values.SequenceEqual(new[]{-2,int.MaxValue,-2}),"Atomic later-field rejection.");
        var two=CreatureView3WoundsDeltaCodec.Decode(Envelope([..GoldenFields(),..Field(6,Command(2,0,int.MinValue),Command(0,1))]),Limits).Apply(initial);
        Assert(two.Counter==6 && two.Values.SequenceEqual(new[]{int.MinValue}),"Repeated field order.");
        Reject(()=>CreatureView3WoundsDeltaCodec.Decode(Envelope([..GoldenFields(),..Field(4)]),Limits with{MaximumFields=1}));
        Reject(()=>CreatureView3WoundsDeltaCodec.Decode(Envelope([..GoldenFields(),..Field(5,Command(4))]),Limits with{MaximumCommands=7}));
        Reject(()=>CreatureView3WoundsDeltaCodec.Decode(Envelope([..GoldenFields(),..Field(5,Command(1,0,1))]),Limits with{MaximumValues=7}));
        var staleInvalid=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(1,Command(0,ushort.MaxValue))),Limits);
        Assert(staleInvalid.Apply(new(ObjectId,1,new int[]{1},8)).Values[0]==1,"Skipped command consumed without semantic application.");
        var zero=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(9)),Limits with{MaximumCommands=0,MaximumValues=0});
        Assert(zero.Apply(new(ObjectId,9,Array.Empty<int>(),0)).Counter==9,"Empty vector update."); Reject(()=>zero.Apply(new(ObjectId,8,Array.Empty<int>(),0)));
        var empty=CreatureView3WoundsDeltaCodec.Decode(Envelope(Array.Empty<byte>(),65535),Limits with{MaximumFields=0,MaximumCommands=0,MaximumValues=0});
        Assert(empty.Apply(initial).Values.SequenceEqual(initial.Values),"Empty envelope/count tolerance.");
        byte[] borrowed=Golden(); var owned=CreatureView3WoundsDeltaCodec.Decode(borrowed,Limits); Array.Clear(borrowed);
        Assert(CreatureView3WoundsDeltaCodec.Encode(owned,Limits).SequenceEqual(wire),"Plan ownership.");
        int[] seed={1}; var ownedState=new CreatureView3WoundsState(ObjectId,0,seed,8); seed[0]=2; Assert(ownedState.Values[0]==1,"State ownership.");
        try { ((IList<int>)result.Values)[0]=0; throw new Exception("Mutable state."); } catch(NotSupportedException) { }
        try { ((IList<int>)plan.Updates[0].Commands[3].Values)[0]=0; throw new Exception("Mutable plan."); } catch(NotSupportedException) { }
        foreach (ushort count in new ushort[]{0,1,65535}) Assert(CreatureView3WoundsDeltaCodec.Encode(CreatureView3WoundsDeltaCodec.Decode(Envelope(GoldenFields(),count),Limits),Limits).SequenceEqual(Envelope(GoldenFields(),count)),"Raw declared count.");
        var exact=Limits with{MaximumPayloadBytes=wire.Length-23,MaximumFields=1,MaximumCommands=7,MaximumValues=7,MaximumElements=3};
        Assert(CreatureView3WoundsDeltaCodec.Decode(wire,exact).Apply(initial).Values.SequenceEqual(result.Values),"Exact budgets.");
        int[] largest=Enumerable.Range(0,65535).ToArray();
        var wideLimits=new CreatureView3WoundsDeltaLimits(300000,1,1,65535,65536);
        var wideAssign=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(1,Command(3,0,largest))),wideLimits);
        var wideState=wideAssign.Apply(new(ObjectId,0,Array.Empty<int>(),65536));
        Assert(wideState.Values.Count==65535 && wideState.Values[65534]==65534,"U16 assignment count maximum.");
        var wideInsert=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(2,Command(1,65535,int.MinValue))),wideLimits).Apply(wideState);
        Assert(wideInsert.Values.Count==65536 && wideInsert.Values[65535]==int.MinValue,"U16 insert index maximum.");
        var wideReplace=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(3,Command(2,65535,int.MaxValue))),wideLimits).Apply(wideInsert);
        Assert(wideReplace.Values[65535]==int.MaxValue,"U16 replace index maximum.");
        var wideRemove=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(4,Command(0,65535))),wideLimits).Apply(wideReplace);
        Assert(wideRemove.Values.SequenceEqual(largest),"U16 remove index maximum.");
        Assert(CreatureView3WoundsDeltaCodec.Encode(wideAssign,wideLimits).SequenceEqual(Envelope(Field(1,Command(3,0,largest)))),"Large assignment encode.");
        var emptyAssign=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(1,Command(3))),Limits with{MaximumValues=0,MaximumElements=0});
        Assert(emptyAssign.Apply(new(ObjectId,0,Array.Empty<int>(),0)).Values.Count==0,"Empty assignment.");
        var sameSource=Initial(); var clearOnly=CreatureView3WoundsDeltaCodec.Decode(Envelope(Field(0xFFFFFFFE,Command(4))),Limits).Apply(sameSource);
        Assert(clearOnly.Values.Count==0 && sameSource.Values.Count==3,"Clear ownership.");
        try { ((IList<CreatureWoundsCommand>)plan.Updates[0].Commands).Clear(); throw new Exception("Mutable commands."); } catch(NotSupportedException) { }
        try { ((IList<CreatureWoundsUpdate>)plan.Updates).Clear(); throw new Exception("Mutable updates."); } catch(NotSupportedException) { }
        Console.WriteLine("Wounds delta grammar, atomicity, counters, bounds, ownership and budgets passed.");
    }
}
