using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureWoundsLegacyPolicyScenarios
{
    private const ulong Id = 0xF122334455667788;
    public static CreatureView3WoundsDeltaLimits Limits => new(500000,8,100000,100000,70000);
    public static byte[] Packet(uint count,uint incoming,params byte[][] commands)
    {
        using MemoryStream s=new(); using BinaryWriter w=new(s);
        w.Write((ushort)5); w.Write(0x12862153U); w.Write(Id); w.Write(0x4352454FU); w.Write((byte)3);
        w.Write((uint)(12+commands.Sum(c=>c.Length))); w.Write((ushort)65535); w.Write((ushort)17); w.Write(count); w.Write(incoming);
        foreach(var c in commands) w.Write(c); return s.ToArray();
    }
    public static byte[] Op(byte kind,ushort index=0,params int[] values)
    {
        using MemoryStream s=new(); using BinaryWriter w=new(s); w.Write(kind);
        if(kind!=4) w.Write(kind==3 ? checked((ushort)values.Length) : index);
        foreach(int v in values) w.Write(v); return s.ToArray();
    }
    private static CreatureView3WoundsDeltaPlan Legacy(byte[] wire,CreatureView3WoundsDeltaLimits? limits=null)=>CreatureView3WoundsDeltaCodec.Decode(wire,limits??Limits,CreatureWoundsApplicationPolicy.LegacyBounded);
    private static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    private static void Reject(Action a){try{a();}catch(InvalidDataException){return;}throw new Exception("Expected rejection.");}
    public static void Run()
    {
        var seed=new CreatureView3WoundsState(Id,0,new int[]{9},70000);
        var equal=Legacy(Packet(1,1,Op(2,0,9))).Apply(seed); Check(equal.Counter==0,"Equal replace does not advance.");
        var strict=CreatureView3WoundsDeltaCodec.Decode(Packet(1,1,Op(2,0,9)),Limits).Apply(seed); Check(strict.Counter==1,"Strict default retained.");
        var badRemove=Legacy(Packet(1,1,Op(0,65535))).Apply(seed); Check(badRemove.Counter==0 && badRemove.Values[0]==9,"Invalid remove ignored.");
        Reject(()=>CreatureView3WoundsDeltaCodec.Decode(Packet(1,1,Op(0,65535)),Limits).Apply(seed));
        var gap=Legacy(Packet(1,7,Op(2,0,4))).Apply(seed); Check(gap.Counter==0 && gap.Values[0]==9,"Future gap clamp.");
        var older=Legacy(Packet(1,1,Op(2,0,4))).Apply(new(Id,9,new int[]{9},70000)); Check(older.Counter==9,"Old packet preserves local counter.");
        var grown=Legacy(Packet(1,1,Op(1,3,-8))).Apply(seed); Check(grown.Values.SequenceEqual(new[]{9,0,0,-8}) && grown.Counter==1,"Insert gap zero-fill.");
        var replaced=Legacy(Packet(1,1,Op(2,3,-8))).Apply(seed); Check(replaced.Values.SequenceEqual(grown.Values),"Replace gap zero-fill.");
        Reject(()=>Legacy(Packet(1,1,Op(1,65535,8)),Limits with{MaximumElements=8}).Apply(seed));
        Reject(()=>Legacy(Packet(1,1,Op(2,65535,8)),Limits with{MaximumElements=8}).Apply(seed));
        var wide=Legacy(Packet(1,1,Op(2,65535,-1))).Apply(seed); Check(wide.Values.Count==65536 && wide.Values[65534]==0 && wide.Values[65535]==-1,"U16 maximum bounded growth.");
        var assignWire=Packet(5,5,Op(3,0,1,2,3),Op(2,1,7)); var assign=Legacy(assignWire);
        Check(assign.Updates[0].CommandCount==5 && assign.Updates[0].Commands.Count==2,"Raw count versus actual commands.");
        Check(assign.Apply(seed).Counter==5 && assign.Apply(seed).Values.SequenceEqual(new[]{1,7,3}),"Assign plus following command.");
        Check(CreatureView3WoundsDeltaCodec.Encode(assign,Limits).SequenceEqual(assignWire),"Legacy count round trip.");
        Reject(()=>CreatureView3WoundsDeltaCodec.Decode(assignWire,Limits));
        var over=Legacy(Packet(1,1,Op(3,0,1,2,3))).Apply(seed); Check(over.Counter==4,"Assignment overshoots raw bound.");
        var skipAssign=Legacy(Packet(5,5,Op(3,0,1,2,3),Op(2,0,7))).Apply(new(Id,2,new int[]{9},70000));
        Check(skipAssign.Counter==3 && skipAssign.Values.SequenceEqual(new[]{7}),"Skipped assignment cursor crosses skip threshold.");
        var empty=new CreatureView3WoundsState(Id,0,Array.Empty<int>(),70000);
        Check(Legacy(Packet(1,1,Op(4))).Apply(empty).Counter==0,"Empty clear does not advance.");
        Check(Legacy(Packet(1,1,Op(3))).Apply(empty).Counter==1,"Empty assignment advances once.");
        Check(Legacy(Packet(0,77)).Apply(seed).Counter==0,"Zero count ignores incoming.");
        Check(Legacy(Packet(1,0,Op(2,0,7))).Apply(new(Id,uint.MaxValue,new int[]{9},70000)).Counter==0,"Mutation counter wraps.");
        var atomic=Legacy(Packet(2,2,Op(2,0,7),Op(1,65535,1)),Limits with{MaximumElements=8}); Reject(()=>atomic.Apply(seed)); Check(seed.Counter==0 && seed.Values[0]==9,"Late growth failure atomic.");
        byte[] wire=Packet(1,1,Op(2,0,7));
        for(int n=0;n<wire.Length;n++) Reject(()=>Legacy(wire[..n]));
        Reject(()=>Legacy(Packet(1,1,new byte[]{5})));
        Reject(()=>Legacy(Packet(1,1,Op(3,0,1)),Limits with{MaximumValues=0}));
        Reject(()=>Legacy(Packet(1,1,Op(3,0,1),Op(4)))); // Cursor overshoots bound; trailing command is not another valid field.
        var capped=Limits with{MaximumCommands=4}; Reject(()=>Legacy(assignWire,capped)); Reject(()=>CreatureView3WoundsDeltaCodec.Encode(assign,capped));
        byte[] owned=(byte[])assignWire.Clone(); var plan=Legacy(owned); Array.Clear(owned); Check(CreatureView3WoundsDeltaCodec.Encode(plan,Limits).SequenceEqual(assignWire),"Owned legacy plan.");
        Check(plan.Policy==CreatureWoundsApplicationPolicy.LegacyBounded,"Explicit policy retained.");
        byte[] first=Packet(1,1,Op(2,0,9)), second=Packet(1,2,Op(2,0,7));
        byte[] repeated=[..first,..second[25..]];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(repeated.AsSpan(19),(uint)(repeated.Length-23));
        var repeatedResult=Legacy(repeated).Apply(seed); Check(repeatedResult.Counter==0 && repeatedResult.Values[0]==9,"Repeated fields reconcile against actual mutation counter.");
        var overlap=Legacy(Packet(2,2,Op(2,0,2),Op(2,0,7))).Apply(new(Id,1,new int[]{2},70000));
        Check(overlap.Counter==2 && overlap.Values[0]==7,"Ordinary overlap suffix.");
        Check(Legacy(Packet(1,1,Op(2,0,7))).Apply(new(Id,1,new int[]{7},70000)).Counter==1,"Duplicate no mutation.");
        int[] maximumAssign=Enumerable.Repeat(-7,65535).ToArray();
        var maximumPlan=Legacy(Packet(65536,65536,Op(3,0,maximumAssign)));
        var maximumResult=maximumPlan.Apply(seed); Check(maximumResult.Counter==65536 && maximumResult.Values.Count==65535,"U16 assignment with weighted raw count.");
        try { ((IList<int>)maximumResult.Values)[0]=0; throw new Exception("Mutable legacy result."); } catch(NotSupportedException) { }
        try { ((IList<int>)maximumPlan.Updates[0].Commands[0].Values)[0]=0; throw new Exception("Mutable legacy values."); } catch(NotSupportedException) { }
        Console.WriteLine("Recovered legacy arithmetic, count grammar, mutation semantics, bounds and rollback passed.");
    }
}
