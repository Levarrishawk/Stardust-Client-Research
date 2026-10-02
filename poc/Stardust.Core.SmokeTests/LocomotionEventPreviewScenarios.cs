using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class LocomotionEventPreviewScenarios
{
    private static readonly PreviewLocomotionClip Idle=new(16,8,1,0), Walk=new(16,8,1,2), RunClip=new(16,4,1,8);
    internal static void Run()
    {
        var idle=Fixture(8,0,"idle",new ushort[]{0,3,8});
        byte[] raw=FixtureBytes(8,2,"step",new ushort[]{0,2,2,5,8},second:true);
        var walk=Decode(raw); var run=Fixture(4,8,"run_step",new ushort[]{0,1,3,4});
        LocomotionEventPreviewClock Create(float threshold=4,int cycleBudget=64,int eventBudget=4096)=>
            new(Idle,Walk,RunClip,threshold,idle,walk,run,cycleBudget,eventBudget);
        var clock=Create(); Array.Clear(raw); var legacy=new LocomotionPreviewClock(Idle,Walk,RunClip,4);
        LocomotionEventPreviewBatch Tick(double seconds,Vector3 delta,bool active=true)
        {bool changed=legacy.Advance(seconds,delta,active);var batch=clock.Tick(seconds,delta,active);Check(clock.State==legacy.State && batch.State==legacy.State && batch.Advanced==changed,"Exact legacy state");return batch;}
        var initial=Tick(.125,Vector3.Zero); Check(initial.Count==1 && initial[0].Frame==0,"Initial idle zero");
        var entered=Tick(.125,new(0,1,.125f));Check(entered.State.Mode==PreviewLocomotionMode.Walk && entered.State.Frame==3 && entered.Count==0 && !clock.IsCurrent(initial),"Skip events at/earlier than gait entry");
        var traversed=Tick(.125,new(0,0,.25f));Check(traversed.Count==1 && traversed[0].Frame==5 && clock.IsCurrent(entered),"Traverse current gait");
        var running=Tick(.125,Vector3.UnitX);Check(running.Count==3 && running[0].Frame==3 && running[1].Frame==4 && running[2].Frame==0 && running[2].Cycle==1,"Preserved phase plus terminal/zero");
        var loops=Tick(.125,new(8,0,0));Check(loops.Count==16 && clock.CompletedCycles==5,"Multiple accepted-distance loops");
        var before=Snapshot(clock);var held=Tick(.125,Vector3.UnitZ,active:false);Check(held.Count==0 && Snapshot(clock)==before,"Inactive distance ignored");
        var blocked=Tick(.125,Vector3.Zero);Check(blocked.State.Mode==PreviewLocomotionMode.Idle && blocked.All(e=>e.Name=="idle") && !clock.IsCurrent(running),"Blocked movement emits no movement events");
        before=Snapshot(clock);Check(Tick(0,Vector3.Zero).Count==0 && Snapshot(clock)==before,"Zero time");
        foreach(double bad in new[]{-1.0,double.NaN,double.PositiveInfinity,.251})RejectUnchanged(clock,()=>clock.Tick(bad,Vector3.Zero));
        foreach(Vector3 bad in new[]{new Vector3(float.NaN,0,0),new Vector3(0,float.PositiveInfinity,0),new Vector3(101,0,0),new Vector3(0,1e7f,0)})
            RejectUnchanged(clock,()=>clock.Tick(.125,bad));
        RejectUnchanged(clock,()=>clock.Tick(0,Vector3.UnitX));RejectUnchanged(clock,()=>clock.Reset(Vector3.UnitY));
        var foreign=Create();Check(foreign.Generation==initial.Generation && !foreign.IsCurrent(initial) && !foreign.IsCurrent(blocked),"Foreign owner despite matching generation");
        clock.Reset(-Vector3.UnitZ);legacy.Reset(-Vector3.UnitZ);Check(clock.State==legacy.State && clock.CompletedCycles==0 && clock.GenerationSeconds==0 && !clock.IsCurrent(blocked),"Reset epoch");
        Check(initial[0].Name=="idle" && running.Count==3 && entered.State.Frame==3,"Owned retained batches");
        var eventBudget=Create(eventBudget:1);RejectUnchanged(eventBudget,()=>eventBudget.Tick(.125,new(0,0,2)));
        var retry=eventBudget.Tick(.125,new(0,0,.5f));Check(retry.Count==1 && retry[0].Frame==1 && retry.Generation==2,"Atomic switch/event rejection retry");
        var cycleBudget=Create(cycleBudget:1);RejectUnchanged(cycleBudget,()=>cycleBudget.Tick(.125,new(0,0,8)));
        Check(cycleBudget.Tick(.125,new(0,0,2)).Count==4 && cycleBudget.CompletedCycles==1,"Cycle rejection recovery");
        var tinyClip=Walk with{AuthoredSpeed=float.Epsilon};var tinySource=Fixture(8,float.Epsilon,"tiny",new ushort[]{2});
        var phaseLimit=new LocomotionEventPreviewClock(Idle,tinyClip,RunClip,4,idle,tinySource,run);
        RejectUnchanged(phaseLimit,()=>phaseLimit.Tick(.125,new(0,0,.03125f)));
        var idleScaled=Idle with{IdleTimeScale=2};var scaled=new LocomotionEventPreviewClock(idleScaled,Walk,RunClip,4,idle,walk,run);
        var scaledLegacy=new LocomotionPreviewClock(idleScaled,Walk,RunClip,4);scaled.Tick(.125,Vector3.Zero);scaledLegacy.Advance(.125,Vector3.Zero);Check(scaled.State==scaledLegacy.State && scaled.State.Frame==4,"Idle proxy scale");
        Reject(()=>new LocomotionEventPreviewClock(Idle,Walk,RunClip,4,run,walk,run));
        Reject(()=>new LocomotionEventPreviewClock(Idle,Walk with{AuthoredSpeed=0},RunClip,4,idle,walk,run));
        Reject(()=>Create(cycleBudget:0));Reject(()=>Create(eventBudget:262145));
        int cases=0;
        foreach(bool isRun in new[]{false,true})
            for(int quarter=1;quarter<=256;quarter++)
            {
                double target=quarter*.25;var descriptor=isRun?RunClip:Walk;var source=isRun?run:walk;
                float distance=(float)(target*descriptor.AuthoredSpeed/descriptor.FrameRate);
                float threshold=isRun?.01f:1000f;var whole=Create(threshold);var split=Create(threshold);
                var batch=whole.Tick(.25,new(0,0,distance));var actual=new List<AnimationEventOccurrence>();
                for(int part=0;part<64;part++)actual.AddRange(split.Tick(.25/64,new(0,0,distance/64)));
                var expected=Oracle(source,0,target);
                Check(batch.SequenceEqual(expected) && actual.SequenceEqual(expected) && whole.State==split.State && whole.UnwrappedPhase==split.UnwrappedPhase,"Binary steady-gait oracle/partition");
                cases++;
            }
        Console.WriteLine($"PASS: accepted-distance events {cases} independent/partition cases; exact legacy state, entry skip/loop/blocked/gates/reset/ownership and transactional budgets");
    }

    internal static void Original(string root,string configuration)
    {
        byte[] cfg=File.ReadAllBytes(configuration);
        Check(Convert.ToHexString(SHA256.HashData(cfg))=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524","Config identity");
        using var reader=new StringReader(Encoding.UTF8.GetString(cfg));using var overlay=ConfiguredTreOverlay.Open(root,reader);
        using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");
        var selections=SkeletalAnimationBankProfile.Load(bankFile);
        var reads=new Dictionary<string,int>(StringComparer.Ordinal);
        TreLookupResult Resolve(string path){reads[path]=reads.GetValueOrDefault(path)+1;return overlay.Lookup(path);}
        var bundle=CreatureAppearanceDescriptorBundleReader.Load(Resolve,"object/creature/player/shared_human_male.iff",
            "E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243",
            "951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C",
            loadSkeletons:true,loadLogicalTables:true,
            animationSelection:new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),
            prepareSelectedAnimation:true,additionalAnimationSelections:selections);
        var preparedIdle=bundle.PreparedAnimation!;var preparedWalk=bundle.AdditionalAnimations!["walk"];var preparedRun=bundle.AdditionalAnimations["run"];
        var idle=(Clip:LocomotionPreviewClock.Describe(preparedIdle),Events:CompressedLocomotion.Create(CompressedKeyframeDecoder.Create(bundle.SelectedAnimation!.Clip)));
        var walk=(Clip:LocomotionPreviewClock.Describe(preparedWalk),Events:preparedWalk.Locomotion!);
        var run=(Clip:LocomotionPreviewClock.Describe(preparedRun),Events:preparedRun.Locomotion!);
        Check(preparedWalk.ClipSha256=="FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6" &&
            preparedRun.ClipSha256=="72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4" &&
            reads.Values.All(n=>n==1),"Prepared bank identities/read once");
        Console.WriteLine("PASS: original idle resolved through object/SAT/LAT logical entry: "+preparedIdle.ClipPath+"; prepared idle proxy scale="+preparedIdle.ProxyTimeScale);
        foreach(bool isRun in new[]{false,true})
        {
            var selected=isRun?run:walk;
            var clock=new LocomotionEventPreviewClock(idle.Clip,walk.Clip,run.Clip,3,idle.Events,walk.Events,run.Events);
            var legacy=new LocomotionPreviewClock(idle.Clip,walk.Clip,run.Clip,3);
            var actual=new List<AnimationEventOccurrence>();
            for(int tick=0;tick<selected.Clip.EndFrame*3+1;tick++)
            {
                Vector3 accepted=new(0,0,selected.Clip.AuthoredSpeed/selected.Clip.FrameRate);
                var batch=clock.Tick(1.0/selected.Clip.FrameRate,accepted);legacy.Advance(1.0/selected.Clip.FrameRate,accepted);
                Check(batch.State==legacy.State,"Original exact legacy state");actual.AddRange(batch);
            }
            var frames=isRun?new ushort[]{6,17}:new ushort[]{7,23};
            var expected=Enumerable.Range(0,3).SelectMany(c=>frames.Select((f,i)=>new AnimationEventOccurrence(c,0,i,"event_footstep",f))).ToArray();
            Check(actual.SequenceEqual(expected) && clock.CompletedCycles==3,"Original accepted-distance footsteps");
            var blocked=clock.Tick(.1,Vector3.Zero);Check(blocked.State.Mode==PreviewLocomotionMode.Idle && blocked.Count==0,"Original blocked no footstep");
            Console.WriteLine($"PASS: pinned {(isRun?"run":"walk")} accepted-distance ticks={selected.Clip.EndFrame*3+1}, three wraps/six footsteps, exact old gait/frame/heading and blocked idle");
        }
    }
    private static AnimationEventOccurrence[] Oracle(CompressedLocomotion source,double start,double target)
    {
        var events=new List<(double Time,AnimationEventOccurrence Event)>();
        for(long cycle=0;cycle<=target/source.EndFrame;cycle++)
            for(int group=0;group<source.Messages.Count;group++)
                for(int index=0;index<source.Messages[group].Frames.Count;index++)
                {
                    ushort frame=source.Messages[group].Frames[index];double at=cycle*source.EndFrame+frame;
                    if(at>start&&at<=target)events.Add((at,new(cycle,group,index,source.Messages[group].Name,frame)));
                }
        return events.OrderBy(e=>e.Time).ThenBy(e=>e.Event.Cycle).ThenBy(e=>e.Event.GroupIndex).ThenBy(e=>e.Event.OccurrenceIndex).Select(e=>e.Event).ToArray();
    }
    private static (LocomotionPreviewState,long,double,double,long) Snapshot(LocomotionEventPreviewClock clock)=> (clock.State,clock.Generation,clock.UnwrappedPhase,clock.GenerationSeconds,clock.CompletedCycles);
    private static void RejectUnchanged(LocomotionEventPreviewClock clock,Action action){var before=Snapshot(clock);Reject(action);Check(Snapshot(clock)==before,"Failure changed clock/cursor/epoch");}
    private static void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("Expected rejection");}
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static CompressedLocomotion Decode(byte[] bytes)=>CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(bytes));
    private static CompressedLocomotion Fixture(ushort end,float speed,string name,ushort[] frames)=>Decode(FixtureBytes(end,speed,name,frames));
    private static byte[] FixtureBytes(ushort end,float speed,string name,ushort[] frames,bool second=false)
    {
        byte[] Chunk(string tag,byte[] payload){byte[] length=new byte[4];BinaryPrimitives.WriteInt32BigEndian(length,payload.Length);return Encoding.ASCII.GetBytes(tag).Concat(length).Concat(payload).ToArray();}
        byte[] Form(string tag,params byte[][] children)=>Chunk("FORM",Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(c=>c)).ToArray());
        byte[] Group(ushort[] values)=>Chunk("MESG",BitConverter.GetBytes((ushort)values.Length).Concat(Encoding.ASCII.GetBytes(name+"\0")).Concat(values.SelectMany(BitConverter.GetBytes)).ToArray());
        var children=new List<byte[]>{Chunk("INFO",BitConverter.GetBytes((ushort)(second?2:1))),Group(frames)};if(second)children.Add(Group(new ushort[]{2}));
        byte[] info=BitConverter.GetBytes(16f).Concat(new short[]{(short)end,0,0,0,0,0}.SelectMany(BitConverter.GetBytes)).ToArray();
        using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.UTF8,true)){writer.Write(speed);writer.Write((short)2);writer.Write((ushort)0);writer.Write(0f);writer.Write(0f);writer.Write(0f);writer.Write(end);writer.Write(0f);writer.Write(0f);writer.Write(0f);}
        return Form("CKAT",Form("0001",Chunk("INFO",info),Form("XFRM"),Form("MSGS",children.ToArray()),Chunk("LOCT",stream.ToArray())));
    }
}
