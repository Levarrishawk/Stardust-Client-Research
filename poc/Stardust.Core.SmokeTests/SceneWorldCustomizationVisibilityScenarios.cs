using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

/// <summary>Complete baseline reseed boundary and current common-state mapping, without asset loading.</summary>
public static class SceneWorldCustomizationVisibilityScenarios
{
    public static void Run(string peerPath,string catalogPath,Func<CreatureView3TangibleScalarState,bool> visible)
    {
        var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(peerPath))!;int assertions=0;
        void Check(bool value,string why){if(!value)throw new Exception(why);assertions++;}
        var limits=new CreatureView3BaselineLimits(1024,1024,1024,128,16384);
        var baseline=CreatureView3BaselineCodec.Decode(Convert.FromHexString(peer["human_baseline"]),limits);
        var initial=CreatureView3TangibleScalarState.FromBaseline(baseline);
        Check(initial.Visibility==1&&visible(initial),"baseline visible seed");
        foreach(byte raw in new byte[]{0,1,2,255})
        {
            var state=initial.Apply(CreatureView3TangibleScalarUpdate.FromVisibility(raw));
            Check(state.Visibility==raw&&visible(state)==(raw!=0),"actual engine helper maps current raw byte");
            Check(initial.Visibility==1&&baseline.Visibility==1,"overlay never edits baseline/old state");
            Check(state.Complexity==initial.Complexity&&state.Volume==initial.Volume&&state.Options==initial.Options&&state.UseCount==initial.UseCount&&state.ConditionDamage==initial.ConditionDamage&&state.MaximumCondition==initial.MaximumCondition,"unrelated common fields retained");
        }
        bool nullRejected=false;try{visible(null!);}catch(ArgumentNullException){nullRejected=true;}Check(nullRejected,"null state rejected");
        var recipe=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(catalogPath)).Require(2937962913);
        var idle=new LogicalAnimationClipSelection("appearance/lat/synthetic.lat","idle",new string('A',64));
        using var world=new SceneWorldCoordinator(new ObjectTemplateCatalog(new[]{recipe}),idle,new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}},
            (_,_)=>throw new Exception("Visibility model fixture must not load assets"),SceneCreateZeroQuaternionPolicy.Reject,
            replicationPolicy:new(SceneWorldReplicationMode.CreatureView3Scalars,limits,16,16384));
        long generation=world.BeginSelection(0xF122334455667788).Generation;
        void Accept(string key)=>Check(world.Accept(generation,Convert.FromHexString(peer[key])),"accepted baseline fixture "+key);
        foreach(string key in new[]{"start","human_create","human_baseline","human_end"})Accept(key);
        Check(world.CompleteMinimumSceneBarrier(generation,true)is not null,"minimum barrier");Accept("ready");
        foreach(var (key,raw) in new[]{("visibility_hidden_baseline",(byte)0),("visibility_shown_baseline",(byte)255),("human_baseline",(byte)1)})
        {
            var old=world.Snapshots().Single();Accept(key);var current=world.Snapshots().Single();
            Check(current.Object.Incarnation==old.Object.Incarnation&&current.Replication!.Revision==old.Replication!.Revision+1,"baseline reseed retains incarnation and advances state");
            Check(!world.IsCurrent(old.Replication!)&&world.IsCurrent(current.Replication!),"old/current freshness");
            Check(current.Replication!.TangibleScalars.Visibility==raw&&visible(current.Replication.TangibleScalars)==(raw!=0),"current common visibility forwarder");
            var bytes=Convert.FromHexString(peer[key]);Check(CreatureView3BaselineCodec.Encode(CreatureView3BaselineCodec.Decode(bytes,limits),limits).AsSpan().SequenceEqual(bytes),"exact raw baseline roundtrip");
        }
        world.Close();Check(world.DrainCompletion.IsCompleted,"no background work to drain");
        Console.WriteLine($"PASS: visibility current-state/helper/repeated-baseline assertions={assertions} raw255=true no_assets=true no_delta_claim=true");
    }
}
