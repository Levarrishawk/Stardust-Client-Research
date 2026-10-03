using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

/// <summary>Actual supplied projection invariants through injected pure factories; no Godot/TANO decoder dependency.</summary>
public static class SceneStaticPresentationScenarios
{
    public static void Run<T>(string peerPath,Func<SceneObjectIncarnation,ulong,CreatureView3NameState,byte,T> make,
        Action<T,SceneObjectIncarnation,T?> validate,Func<T,(SceneObjectIncarnation Incarnation,ulong Revision,CreatureView3NameState Names,byte Visibility)> read)where T:class
    {
        var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(peerPath))!;int assertions=0;
        void Check(bool value,string why){if(!value)throw new Exception(why);assertions++;}
        void Reject(Action action){try{action();}catch(InvalidDataException){assertions++;return;}throw new Exception("Expected rejected supplied projection");}
        CreatureView3NameState Names(string key)=>CreatureView3NameState.FromBaseline(CreatureView3BaselineCodec.Decode(Convert.FromHexString(peer[key]),new(1024,1024,1024,128,16384)));
        var first=Names("name_first_baseline");var latest=Names("name_latest_baseline");var incarnation=new SceneObjectIncarnation(Guid.NewGuid(),1,0xE122334455667799,1);
        foreach(byte raw in new byte[]{0,1,2,255})
        {
            var projection=make(incarnation,1,first,raw);validate(projection,incarnation,null);var fields=read(projection);
            Check(fields.Incarnation==incarnation&&fields.Revision==1&&fields.Visibility==raw&&ReferenceEquals(fields.Names,first),"actual record preserves supplied raw identity");
            validate(make(incarnation,1,first,raw),incarnation,projection);validate(make(incarnation,2,latest,raw),incarnation,projection);
            Reject(()=>validate(make(incarnation,1,latest,raw),incarnation,projection));
            Reject(()=>validate(make(incarnation,1,first,(byte)(raw==0?255:0)),incarnation,projection));
            Reject(()=>validate(make(incarnation,0,first,raw),incarnation,projection));
            Reject(()=>validate(make(incarnation with{ObjectId=999},2,latest,raw),incarnation,projection));
            Reject(()=>validate(make(incarnation,1,first,raw),incarnation,make(incarnation,2,latest,raw)));
            Check(read(projection).Names.NameKey.CopyBytes().SequenceEqual("alpha"u8.ToArray()),"initial projection remains immutable");
        }
        Check(latest.CustomNameUnits.SequenceEqual("LATEST CAPTION".Select(c=>(ushort)c))&&latest.NameSpacer==0xF1234568,"supplied raw custom and spacer");
        var duplicate=first.Apply(CreatureView3NameUpdate.FromCustomName(Array.Empty<ushort>()));
        Reject(()=>validate(make(incarnation,1,duplicate,255),incarnation,make(incarnation,1,first,255)));
        Reject(()=>validate(make(incarnation,1,null!,255),incarnation,null));
        Console.WriteLine($"PASS: actual supplied static projection assertions={assertions} raw255=true names_identity=true stale_reused_rejected=true no_typed_TANO=true");
    }
}
