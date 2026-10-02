using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView3ComponentsDeltaScenarios
{
    public const ulong Id=0xF122334455667788;
    public static CreatureView3ComponentsDeltaLimits Limits=>new(4096,4,32,32,8);
    public static byte[] Command(byte kind,int key=0){using MemoryStream s=new();using BinaryWriter w=new(s);w.Write(kind);if(kind!=2)w.Write(key);return s.ToArray();}
    public static byte[] Field(uint counter,params byte[][] commands)
    {using MemoryStream s=new();using BinaryWriter w=new(s);w.Write((ushort)5);w.Write((uint)commands.Length);w.Write(counter);foreach(var c in commands)w.Write(c);return s.ToArray();}
    public static byte[] Message(byte[] fields,ushort declared=0)
    {using MemoryStream s=new();using BinaryWriter w=new(s);w.Write((ushort)5);w.Write(0x12862153U);w.Write(Id);w.Write(0x4352454FU);w.Write((byte)3);w.Write((uint)(fields.Length+2));w.Write(declared);w.Write(fields);return s.ToArray();}
    public static byte[] Snapshot(uint counter,params int[] keys)
    {using MemoryStream s=new();using BinaryWriter w=new(s);w.Write((uint)keys.Length);w.Write(counter);foreach(int key in keys)w.Write(key);return s.ToArray();}
    public static byte[] Golden()=>Message([..Field(5,Command(1,17),Command(1,17),Command(0,123),Command(0,-1),Command(2)),..Field(7,Command(1,int.MinValue),Command(1,int.MaxValue))]);
    private static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    private static void Reject(Action a){try{a();}catch(InvalidDataException){return;}throw new Exception("Expected component rejection.");}
    public static void Run()
    {
        byte[] raw=Snapshot(0,-1,int.MinValue,-1);var snap=CreatureView3ComponentsDeltaCodec.DecodeSnapshot(Id,raw,3);var initial=snap.ToState(2);
        Check(snap.RawCount==3 && snap.RawKeys.SequenceEqual(new[]{-1,int.MinValue,-1}) && initial.Keys.SequenceEqual(new[]{int.MinValue,-1}),"Raw versus normalized signed set.");
        Check(initial.Contains(-1) && !initial.Contains(17),"Signed membership.");
        Check(CreatureView3ComponentsDeltaCodec.EncodeSnapshot(snap,3).SequenceEqual(raw),"Raw snapshot exact encode.");
        Reject(()=>snap.ToState(1));Reject(()=>CreatureView3ComponentsDeltaCodec.EncodeSnapshot(snap,2));Reject(()=>CreatureView3ComponentsDeltaCodec.DecodeSnapshot(Id,raw,2));
        for(int n=0;n<raw.Length;n++)Reject(()=>CreatureView3ComponentsDeltaCodec.DecodeSnapshot(Id,raw[..n],3));Reject(()=>CreatureView3ComponentsDeltaCodec.DecodeSnapshot(Id,[..raw,0],3));
        byte[] huge=(byte[])raw.Clone();BinaryPrimitives.WriteUInt32LittleEndian(huge,uint.MaxValue);Reject(()=>CreatureView3ComponentsDeltaCodec.DecodeSnapshot(Id,huge,int.MaxValue));
        Array.Clear(raw);Check(snap.RawKeys[0]==-1,"Snapshot ownership.");
        var plan=CreatureView3ComponentsDeltaCodec.Decode(Golden(),Limits);var final=plan.Apply(initial);
        Check(final.Counter==7 && final.Keys.SequenceEqual(new[]{int.MinValue,int.MaxValue}),"Commands, duplicate no-op, repeated fields.");
        Check(CreatureView3ComponentsDeltaCodec.Encode(plan,Limits).SequenceEqual(Golden()),"Delta exact encode.");
        for(int n=0;n<Golden().Length;n++)Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(Golden()[..n],Limits));
        foreach(var limit in new[]{Limits with{MaximumPayloadBytes=Golden().Length-24},Limits with{MaximumFields=1},Limits with{MaximumCommands=6},Limits with{MaximumKeys=5}})
        {Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(Golden(),limit));Reject(()=>CreatureView3ComponentsDeltaCodec.Encode(plan,limit));}
        var exact=Limits with{MaximumPayloadBytes=Golden().Length-23,MaximumFields=2,MaximumCommands=7,MaximumKeys=6,MaximumElements=3};Check(CreatureView3ComponentsDeltaCodec.Decode(Golden(),exact).Apply(initial).Counter==7,"Exact budgets.");
        Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(Golden(),Limits with{MaximumElements=2}).Apply(initial));Check(initial.Counter==0 && initial.Keys.SequenceEqual(new[]{int.MinValue,-1}),"Atomic growth failure.");
        Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(Message([..Field(1,Command(1,17)),17,0]),Limits));
        Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(Message([..Field(1,Command(1,17)),0]),Limits));
        Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(Message(Field(1,new byte[]{3})),Limits));
        byte[] badCount=Message(Field(1));BinaryPrimitives.WriteUInt32LittleEndian(badCount.AsSpan(27),uint.MaxValue);Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(badCount,Limits));
        foreach(int offset in new[]{0,2,14,18,19}){byte[] bad=Golden();bad[offset]^=1;Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(bad,Limits));}
        var future=CreatureView3ComponentsDeltaCodec.Decode(Message(Field(7,Command(1,17))),Limits).Apply(initial);Check(future.Counter==7 && future.Contains(17),"Set future gap gate.");
        var zero=CreatureView3ComponentsDeltaCodec.Decode(Message(Field(5)),Limits).Apply(initial);Check(zero.Counter==5 && zero.Keys.SequenceEqual(initial.Keys),"Zero count raises counter.");
        foreach(byte kind in new byte[]{0,1,2})
        {
            int key=kind==1 ? -1 : 123;
            var noop=CreatureView3ComponentsDeltaCodec.Decode(Message(Field(1,Command(kind,key))),Limits).Apply(new(Id,uint.MaxValue,kind==2 ? Array.Empty<int>() : new int[]{-1},8));
            Check(noop.Counter==uint.MaxValue,"No-op counter under wrapped gate.");
        }
        var wrap=CreatureView3ComponentsDeltaCodec.Decode(Message(Field(1,Command(1,17))),Limits).Apply(new(Id,uint.MaxValue,new int[]{-1},8));Check(wrap.Counter==1 && wrap.Contains(17),"Mutation wrap then unsigned max.");
        var stale=CreatureView3ComponentsDeltaCodec.Decode(Message(Field(1,Command(1,17))),Limits).Apply(new(Id,9,new int[]{-1},8));Check(stale.Counter==9 && !stale.Contains(17),"Wholly stale prefix.");
        var overlap=CreatureView3ComponentsDeltaCodec.Decode(Message(Field(2,Command(1,17),Command(1,23))),Limits).Apply(new(Id,1,new int[]{17},8));Check(overlap.Counter==2 && overlap.Keys.SequenceEqual(new[]{17,23}),"Overlapping suffix.");
        foreach(ushort count in new ushort[]{0,1,65535}){byte[] wire=Message(Golden()[25..],count);Check(CreatureView3ComponentsDeltaCodec.Encode(CreatureView3ComponentsDeltaCodec.Decode(wire,Limits),Limits).SequenceEqual(wire),"Raw declared count.");}
        byte[] borrowed=Golden();var owned=CreatureView3ComponentsDeltaCodec.Decode(borrowed,Limits);Array.Clear(borrowed);Check(CreatureView3ComponentsDeltaCodec.Encode(owned,Limits).SequenceEqual(Golden()),"Plan ownership.");
        try{((IList<int>)snap.RawKeys)[0]=0;throw new Exception("Mutable raw snapshot.");}catch(NotSupportedException){}
        try{((IList<int>)final.Keys)[0]=0;throw new Exception("Mutable set.");}catch(NotSupportedException){}
        try{((IList<CreatureComponentsCommand>)plan.Updates[0].Commands)[0]=new(CreatureComponentsCommandKind.Clear,0);throw new Exception("Mutable commands.");}catch(NotSupportedException){}
        Reject(()=>plan.Apply(new(0,0,Array.Empty<int>(),8)));
        var clear=CreatureView3ComponentsDeltaCodec.Decode(Message(Field(1,Command(2))),Limits with{MaximumKeys=0,MaximumElements=0}).Apply(new(Id,0,Array.Empty<int>(),0));Check(clear.Counter==1 && clear.Keys.Count==0,"Empty clear/key-zero budget.");
        var emptySnapshot=CreatureView3ComponentsDeltaCodec.DecodeSnapshot(Id,Snapshot(uint.MaxValue),0);
        Check(emptySnapshot.ToState(0).Counter==uint.MaxValue && CreatureView3ComponentsDeltaCodec.EncodeSnapshot(emptySnapshot,0).SequenceEqual(Snapshot(uint.MaxValue)),"Empty raw snapshot/high counter.");
        var emptyPlan=CreatureView3ComponentsDeltaCodec.Decode(Message(Array.Empty<byte>(),65535),Limits with{MaximumFields=0,MaximumCommands=0,MaximumKeys=0});
        Check(emptyPlan.Apply(initial).Counter==0 && emptyPlan.Apply(initial).Keys.SequenceEqual(initial.Keys),"Empty envelope.");
        var normalized=new CreatureView3ComponentsState(Id,0,new int[]{int.MaxValue,-1,int.MinValue,-1,0},4);
        Check(normalized.Keys.SequenceEqual(new[]{int.MinValue,-1,0,int.MaxValue}),"Signed ordering includes high bits.");
        var late=CreatureView3ComponentsDeltaCodec.Decode(Message([..Field(1,Command(1,17)),..Field(2,Command(1,23))]),Limits with{MaximumElements=3});
        Reject(()=>late.Apply(initial));Check(!initial.Contains(17) && initial.Counter==0,"Late repeated-field add failure atomic.");
        Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(Message(Field(1,Command(0,-1))),Limits with{MaximumKeys=0}));
        var envelope=ReplicationEnvelopeCodec.DecodeDelta(Golden(),4096);Check(CreatureView3ComponentsDeltaCodec.Decode(envelope,Limits).Updates.Count==2,"Envelope overload.");
        Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(envelope,Limits with{MaximumPayloadBytes=2}));
        Reject(()=>CreatureView3ComponentsDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline,Id,0x4352454F,3,0,Array.Empty<byte>(),4096),Limits));
        Console.WriteLine("Components snapshot normalization, set kernel, bounds, atomicity and ownership passed.");
    }
}
