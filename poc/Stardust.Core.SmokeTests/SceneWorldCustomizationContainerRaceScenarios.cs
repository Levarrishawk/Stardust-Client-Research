using System.Text.Json;
using Stardust.Core.Protocol;

namespace Stardust.Core.SmokeTests;

/// <summary>Bounded synthetic compound fixtures; rendering and lifecycle verified by own host.</summary>
public static class SceneWorldCustomizationContainerRaceScenarios
{
    public static void Run(string peerPath)
    {
        var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(peerPath))!;int assertions=0;
        void Check(bool value,string message){if(!value)throw new Exception(message);assertions++;}
        void Reject(Action action){try{action();}catch(InvalidDataException){assertions++;return;}throw new Exception("Expected malformed compound rejection");}
        var limits=new CreatureView3CompositeDeltaLimits(16384,32,16,32,128,128,32,128,128);
        var baseline=CreatureView3BaselineCodec.Decode(Convert.FromHexString(peer["human_baseline"]),new(1024,1024,1024,128,16384));
        var original=CreatureView3CompositeState.FromBaseline(baseline,128,128,128);var state=original;
        Check(original.Components.Counter==7&&original.Wounds.Counter==9,$"fixture baseline counters {original.Components.Counter}/{original.Wounds.Counter}");
        foreach(string key in new[]{"race_containers_first","race_containers_latest"})
        {
            byte[] bytes=Convert.FromHexString(peer[key]);var plan=CreatureView3CompositeDeltaCodec.Decode(bytes,limits,CreatureWoundsApplicationPolicy.StrictInterval);
            Check(plan.ObjectId==0xF122334455667788&&plan.Updates.Count==8&&plan.DeclaredCount==8,"ordered compound packet identity");
            Check(CreatureView3CompositeDeltaCodec.Encode(plan,limits).AsSpan().SequenceEqual(bytes),"exact compound encoding");
            state=plan.Apply(state);
            for(int n=1;n<bytes.Length;n++){int length=n;Reject(()=>CreatureView3CompositeDeltaCodec.Decode(bytes.AsSpan(0,length),limits,CreatureWoundsApplicationPolicy.StrictInterval));}
            Check(original.Components.Keys.Count==0&&original.Wounds.Values.Count==0&&original.Scalars.Height==1,"immutable baseline retained");
            if(key=="race_containers_first")Check(state.Components.Counter==9&&state.Components.Keys.SequenceEqual(new[]{-2,7})&&state.Wounds.Counter==10&&state.Wounds.Values.SequenceEqual(new[]{10,-20})&&state.Scalars.Height==1.25f,"first state");
        }
        var latest=CreatureView3CompositeDeltaCodec.Decode(Convert.FromHexString(peer["race_containers_latest"]),limits,CreatureWoundsApplicationPolicy.StrictInterval);
        var replay=latest.Apply(state);
        Check(state.Components.Counter==12&&state.Components.Keys.SequenceEqual(new[]{-7,99})&&state.Wounds.Counter==12&&state.Wounds.Values.SequenceEqual(new[]{5,-6,5}),"latest clear/reassign state");
        Check(replay.Components.Counter==12&&replay.Components.Keys.SequenceEqual(state.Components.Keys)&&replay.Wounds.Counter==12&&replay.Wounds.Values.SequenceEqual(state.Wounds.Values)&&replay.Scalars==state.Scalars,"legal counter replay");
        Check(state.Scalars==new CreatureView3ScalarState(7,42,0xF102030405060799,1.6f,0xDEADBEEF,0xFEDCBA9876543211),"six latest scalars");
        Reject(()=>latest.Apply(original)); // strict wounds interval rejects a gap, leaving original intact.
        byte[] bad=Convert.FromHexString(peer["race_containers_latest"]);bad[35]=255;
        Reject(()=>CreatureView3CompositeDeltaCodec.Decode(bad,limits,CreatureWoundsApplicationPolicy.StrictInterval));
        Check(original.Components.Counter==7&&original.Wounds.Counter==9,"rejection retains original counters");
        Console.WriteLine($"PASS: container race compound fixtures assertions={assertions} strict_wounds=true legal_replay=true");
    }
}

