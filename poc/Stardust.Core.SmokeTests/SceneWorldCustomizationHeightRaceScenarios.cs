using System.Text.Json;
using Stardust.Core.Protocol;

namespace Stardust.Core.SmokeTests;

/// <summary>Independent scalar packet identities for the controlled Godot race; no mixed field4 claim.</summary>
public static class SceneWorldCustomizationHeightRaceScenarios
{
    public static void Run(string peerPath)
    {
        var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(peerPath))!;int assertions=0;
        void Check(bool value,string message){if(!value)throw new Exception(message);assertions++;}
        void Reject(Action action){try{action();}catch(InvalidDataException){assertions++;return;}throw new Exception("Expected malformed scalar rejection");}
        var original=new CreatureView3ScalarState(0,0,0,1,0,0);var state=original;
        foreach(string key in new[]{"race_scalars_first","race_scalars_latest"})
        {
            byte[] bytes=Convert.FromHexString(peer[key]);var plan=CreatureView3ScalarDeltaCodec.Decode(bytes,1024,16);
            Check(plan.ObjectId==0xF122334455667788&&plan.DeclaredCount==6&&plan.Updates.Count==6,"exact independently authored scalar packet");
            Check(CreatureView3ScalarDeltaCodec.Encode(plan,1024,16).AsSpan().SequenceEqual(bytes),"scalar roundtrip identity");
            state=plan.Apply(state);Check(original.Height==1&&state!=original,"immutable body state");
            for(int length=1;length<bytes.Length;length++){int n=length;Reject(()=>CreatureView3ScalarDeltaCodec.Decode(bytes.AsSpan(0,n),1024,16));}
            Reject(()=>CreatureView3ScalarDeltaCodec.Decode(bytes,1024,5));
        }
        Check(state==new CreatureView3ScalarState(7,42,0xF102030405060799,1.6f,0xDEADBEEF,0xFEDCBA9876543211),"latest six scalar state");
        Check(BitConverter.SingleToUInt32Bits(state.Height)==0x3FCCCCCD,"latest Height exact bits");
        Console.WriteLine("height_race_scalars="+JsonSerializer.Serialize(new{assertions,state.Posture,state.Rank,state.LinkId,heightBits=BitConverter.SingleToUInt32Bits(state.Height),state.Shock,state.States,independent_scalar_packets=true}));
        Console.WriteLine("PASS: Height race scalar fixtures; GPU lifecycle proof is separate");
    }
}
