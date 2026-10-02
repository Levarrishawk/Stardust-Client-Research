using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView3MixedDeltaScenarios
{
    public const ulong Id=0xF122334455667788;
    public static CreatureView3MixedDeltaLimits Limits=>new(4096,32,24,64,64,16);
    public static CreatureView3MixedState Seed()=>new(Id,new(1,2,3,4.5f,5,6),new(Id,0,new int[]{9},16));
    public static byte[] Scalar(ushort index,params byte[] value)
    { byte[] b=new byte[2+value.Length];BinaryPrimitives.WriteUInt16LittleEndian(b,index);value.CopyTo(b,2);return b; }
    public static byte[] GoldenFields(CreatureWoundsApplicationPolicy policy)
    {
        var w=CreatureView3WoundsDeltaScenarios.Field(policy==CreatureWoundsApplicationPolicy.LegacyBounded ? 5U : 2U,CreatureView3WoundsDeltaScenarios.Command(3,0,-1,0,7));
        if(policy==CreatureWoundsApplicationPolicy.LegacyBounded)BinaryPrimitives.WriteUInt32LittleEndian(w.AsSpan(2),4);
        return [..Scalar(11,255),..CreatureView3WoundsDeltaScenarios.Field(1,CreatureView3WoundsDeltaScenarios.Command(2,0,int.MinValue)),
            ..Scalar(14,0,0,0,128),..w,..Scalar(16,135,150,165,180,195,210,225,240),..Scalar(11,7),
            ..CreatureView3WoundsDeltaScenarios.Field(policy==CreatureWoundsApplicationPolicy.LegacyBounded ? 6U : 3U,CreatureView3WoundsDeltaScenarios.Command(0,1)),
            ..Scalar(12,254),..Scalar(13,8,7,6,5,4,3,2,254),..Scalar(15,68,51,34,241)];
    }
    public static byte[] Golden(CreatureWoundsApplicationPolicy policy,ushort count=0)=>CreatureView3WoundsDeltaScenarios.Envelope(GoldenFields(policy),count);
    private static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    private static void Reject(Action a){try{a();}catch(InvalidDataException){return;}throw new Exception("Expected mixed rejection.");}
    public static void Run()
    {
        var initial=Seed();
        foreach(var policy in new[]{CreatureWoundsApplicationPolicy.StrictInterval,CreatureWoundsApplicationPolicy.LegacyBounded})
        {
            byte[] wire=Golden(policy);var plan=CreatureView3MixedDeltaCodec.Decode(wire,Limits,policy);var final=plan.Apply(initial);
            Check(plan.Policy==policy && plan.Updates.Select(u=>u.Index).SequenceEqual(new ushort[]{11,17,14,17,16,11,17,12,13,15}),"Received order.");
            Check(final.Scalars.Posture==7 && final.Scalars.Rank==254 && final.Scalars.LinkId==0xFE02030405060708 && final.Scalars.Shock==0xF1223344 && final.Scalars.States==0xF0E1D2C3B4A59687,"Scalar bits.");
            Check(BitConverter.SingleToInt32Bits(final.Scalars.Height)==int.MinValue,"Signed zero.");
            Check(final.Wounds.Values.SequenceEqual(new[]{-1,7}) && final.Wounds.Counter==(policy==CreatureWoundsApplicationPolicy.LegacyBounded ? 6U : 3U),"Mixed wounds.");
            Check(CreatureView3MixedDeltaCodec.Encode(plan,Limits).SequenceEqual(wire),"Exact mixed encode.");
            for(int n=0;n<wire.Length;n++)Reject(()=>CreatureView3MixedDeltaCodec.Decode(wire[..n],Limits,policy));
            foreach(var cap in new[]{Limits with{MaximumPayloadBytes=wire.Length-24},Limits with{MaximumFields=9},Limits with{MaximumScalarUpdates=6},Limits with{MaximumWoundsCommands=policy==CreatureWoundsApplicationPolicy.LegacyBounded ? 5 : 2},Limits with{MaximumWoundsValues=3}})
            {Reject(()=>CreatureView3MixedDeltaCodec.Decode(wire,cap,policy));Reject(()=>CreatureView3MixedDeltaCodec.Encode(plan,cap));}
            Reject(()=>CreatureView3MixedDeltaCodec.Decode(wire,Limits with{MaximumWoundsElements=2},policy).Apply(initial));
            foreach(ushort count in new ushort[]{0,1,65535})Check(CreatureView3MixedDeltaCodec.Encode(CreatureView3MixedDeltaCodec.Decode(Golden(policy,count),Limits,policy),Limits).SequenceEqual(Golden(policy,count)),"Count tolerance.");
            byte[] borrowed=(byte[])wire.Clone();var owned=CreatureView3MixedDeltaCodec.Decode(borrowed,Limits,policy);Array.Clear(borrowed);Check(CreatureView3MixedDeltaCodec.Encode(owned,Limits).SequenceEqual(wire),"Owned plan.");
            try{((IList<CreatureView3MixedUpdate>)plan.Updates).Clear();throw new Exception("Mutable mixed updates.");}catch(NotSupportedException){}
            try{((IList<int>)plan.Updates[3].Wounds!.Commands[0].Values)[0]=5;throw new Exception("Mutable mixed values.");}catch(NotSupportedException){}
            Check(initial.Scalars.Height==4.5f && initial.Wounds.Counter==0 && initial.Wounds.Values[0]==9,"Initial state retained.");
        }
        Reject(()=>CreatureView3MixedDeltaCodec.Decode(Golden(CreatureWoundsApplicationPolicy.LegacyBounded),Limits));
        var wrong=CreatureView3MixedDeltaCodec.Decode(Golden(CreatureWoundsApplicationPolicy.StrictInterval),Limits,CreatureWoundsApplicationPolicy.LegacyBounded).Apply(initial);
        Check(wrong.Wounds.Counter==5 && wrong.Wounds.Values.SequenceEqual(new[]{-1,0,7}),"Wrong policy discriminator.");
        for(ushort index=0;index<=10;index++)Reject(()=>CreatureView3MixedDeltaCodec.Decode(CreatureView3WoundsDeltaScenarios.Envelope([..Scalar(11,8),..Scalar(index)]),Limits));
        Reject(()=>CreatureView3MixedDeltaCodec.Decode(CreatureView3WoundsDeltaScenarios.Envelope([..Scalar(11,8),0]),Limits));
        Reject(()=>CreatureView3MixedDeltaCodec.Decode(CreatureView3WoundsDeltaScenarios.Envelope([..Scalar(11,8),..Scalar(14,0,0,128,127)]),Limits));
        var lateCounter=CreatureView3MixedDeltaCodec.Decode(CreatureView3WoundsDeltaScenarios.Envelope([..Scalar(11,8),..CreatureView3WoundsDeltaScenarios.Field(8,CreatureView3WoundsDeltaScenarios.Command(2,0,7))]),Limits);Reject(()=>lateCounter.Apply(initial));
        var lateGrowth=CreatureView3MixedDeltaCodec.Decode(CreatureView3WoundsDeltaScenarios.Envelope([..Scalar(11,8),..CreatureView3WoundsDeltaScenarios.Field(1,CreatureView3WoundsDeltaScenarios.Command(1,65535,7))]),Limits,CreatureWoundsApplicationPolicy.LegacyBounded);Reject(()=>lateGrowth.Apply(initial));
        Check(initial.Scalars.Posture==1 && initial.Wounds.Values[0]==9,"Atomic late failures.");
        Reject(()=>lateCounter.Apply(new(0,initial.Scalars,new(0,0,new int[]{9},16))));
        foreach(int offset in new[]{0,2,14,18,19}){byte[] wire=Golden(CreatureWoundsApplicationPolicy.StrictInterval);wire[offset]^=1;Reject(()=>CreatureView3MixedDeltaCodec.Decode(wire,Limits));}
        var empty=CreatureView3MixedDeltaCodec.Decode(CreatureView3WoundsDeltaScenarios.Envelope(Array.Empty<byte>(),65535),Limits with{MaximumFields=0,MaximumScalarUpdates=0,MaximumWoundsCommands=0,MaximumWoundsValues=0});Check(empty.Apply(initial).Wounds.Values[0]==9,"Empty mixed plan.");
        foreach(var policy in new[]{CreatureWoundsApplicationPolicy.StrictInterval,CreatureWoundsApplicationPolicy.LegacyBounded})
        {
            byte[] all=CreatureView3WoundsDeltaScenarios.GoldenFields();
            if(policy==CreatureWoundsApplicationPolicy.LegacyBounded){BinaryPrimitives.WriteUInt32LittleEndian(all.AsSpan(2),10);BinaryPrimitives.WriteUInt32LittleEndian(all.AsSpan(6),7);}
            var woundSeed=CreatureView3WoundsDeltaScenarios.Initial();
            var composed=CreatureView3MixedDeltaCodec.Decode(CreatureView3WoundsDeltaScenarios.Envelope([..Scalar(11,8),..all,..Scalar(12,7)]),Limits,policy).Apply(new(Id,initial.Scalars,woundSeed));
            var accepted=CreatureView3WoundsDeltaCodec.Decode(CreatureView3WoundsDeltaScenarios.Envelope(all),new(4096,32,64,64,16),policy).Apply(woundSeed);
            Check(composed.Wounds.Counter==accepted.Counter && composed.Wounds.Values.SequenceEqual(accepted.Values) && composed.Scalars.Posture==8 && composed.Scalars.Rank==7,"All five commands compose accepted kernel.");
        }
        var exact=Limits with{MaximumPayloadBytes=Golden(CreatureWoundsApplicationPolicy.StrictInterval).Length-23,MaximumFields=10,MaximumScalarUpdates=7,MaximumWoundsCommands=3,MaximumWoundsValues=4,MaximumWoundsElements=3};
        Check(CreatureView3MixedDeltaCodec.Decode(Golden(CreatureWoundsApplicationPolicy.StrictInterval),exact).Apply(initial).Wounds.Values.Count==2,"Exact mixed limits.");
        Reject(()=>CreatureView3MixedDeltaCodec.Decode(CreatureView3WoundsDeltaScenarios.Envelope(Array.Empty<byte>()),Limits with{MaximumWoundsElements=0}).Apply(initial));
        var envelope=ReplicationEnvelopeCodec.DecodeDelta(Golden(CreatureWoundsApplicationPolicy.StrictInterval),4096);
        Check(CreatureView3MixedDeltaCodec.Decode(envelope,Limits).Policy==CreatureWoundsApplicationPolicy.StrictInterval,"Envelope overload default.");
        Reject(()=>CreatureView3MixedDeltaCodec.Decode(envelope,Limits with{MaximumPayloadBytes=2}));
        Reject(()=>CreatureView3MixedDeltaCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline,Id,0x4352454F,3,0,Array.Empty<byte>(),4096),Limits));
        Console.WriteLine("Mixed ordering, policy grammar, atomicity, bits, ownership and budgets passed.");
    }
}
