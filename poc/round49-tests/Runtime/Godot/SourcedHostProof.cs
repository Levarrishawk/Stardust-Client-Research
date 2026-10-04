using Godot;
using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
using Stardust.Core.SmokeTests;
using V2=System.Numerics.Vector2;
using V3=System.Numerics.Vector3;

public partial class SourcedHostProof : Node
{
    private int checks;
    private byte[] baseline=Array.Empty<byte>();
    private SharedCreatureAuthoredMovementInputs profile=null!,missing=null!;
    private readonly List<object> rows=new();
    private void Check(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
    private void Reject(Action action){bool rejected=false;try{action();}catch(Exception e)when(e is InvalidOperationException or ArgumentException or ObjectDisposedException){rejected=true;}Check(rejected,"expected host rejection");}
    private async Task KeyEvent(Key physical,bool pressed,Key logical=Key.None,bool echo=false,bool control=false)
    {using var e=new InputEventKey{PhysicalKeycode=physical,Keycode=logical,Pressed=pressed,Echo=echo,CtrlPressed=control};Input.ParseInputEvent(e);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private static uint Bits(float f)=>BitConverter.SingleToUInt32Bits(f);
    private void Remove(SceneWorldSourcedInputHost host){host.Close();if(GodotObject.IsInstanceValid(host)){if(host.GetParent()==this)RemoveChild(host);host.Free();}host.Dispose();}
    public override async void _Ready()
    {
        try
        {
            string[] args=OS.GetCmdlineUserArgs();string Arg(string name){int i=Array.IndexOf(args,name);if(i<0||i+1>=args.Length)throw new ArgumentException(name);return args[i+1];}
            using var fixture=JsonDocument.Parse(File.ReadAllText(Arg("--fixture")));profile=SceneWorldExplicitStopScenarios.Profile(fixture.RootElement.GetProperty("Profile"));missing=SceneWorldExplicitStopScenarios.Profile(fixture.RootElement.GetProperty("Missing"));baseline=Convert.FromHexString(fixture.RootElement.GetProperty("Baseline").GetString()!);
            for(int source=1;source<=4;source++)await SourceProof((SceneWorldMovementSpeedSource)source);
            await UnavailableProof(SceneWorldMovementSpeedSource.ReceivedField7);await UnavailableProof(SceneWorldMovementSpeedSource.ReceivedField11);
            await MissingAuthoredProof();await Failures();
            File.WriteAllText(Arg("--output"),JsonSerializer.Serialize(new{Checks=checks,Cases=rows}));GD.Print($"R49_GODOT_PASS checks={checks} cases={rows.Count}");GetTree().Quit();
        }
        catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
    private async Task SourceProof(SceneWorldMovementSpeedSource source)
    {
        using var w=SceneWorldExplicitStopScenarios.World(profile);long g=SceneWorldExplicitStopScenarios.Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=new SceneWorldReceivedCreatureMovementInputs(w);SceneWorldExplicitStopScenarios.Received(w,g,Bits(3),Bits(8));var a=SceneWorldExplicitStopScenarios.Admission(w,motions,profile,source,reader);long now=0;
        var host=new SceneWorldSourcedInputHost(()=>now);AddChild(host);host.Configure(a,(int)source>=3?g:0,(int)source>=3?reader:null);
        try
        {
            float speed=source switch{SceneWorldMovementSpeedSource.AuthoredSpeed0=>2,SceneWorldMovementSpeedSource.AuthoredSpeed1=>6,SceneWorldMovementSpeedSource.ReceivedField7=>3,_=>8};
            Check(host.SpeedSource==source&&host.LastAdmission is null&&host.LastStopAccepted,"configure fixed source and explicit initial stop");
            await KeyEvent(Key.W,true,Key.S);Check(host.HeldKeyCount==1&&host.LastAdmission!.Speed==speed,"physical W wins logical S");var receipt=host.LastAdmission;
            await KeyEvent(Key.Shift,true);Check(ReferenceEquals(receipt,host.LastAdmission)&&host.SpeedSource==source,"shift does not switch source");
            await KeyEvent(Key.W,true,echo:true);Check(ReferenceEquals(receipt,host.LastAdmission),"echo ignored");
            await KeyEvent(Key.A,true,control:true);Check(host.HeldKeyCount==1,"modified shortcut press ignored");
            now=10000;var forward=host.Advance(now);Check(Math.Abs(forward.Transform.Translation.Z-speed*.01f)<1e-6,"actual sourced forward tick");
            await KeyEvent(Key.D,true);var diagonal=host.LastAdmission!;Check(Math.Abs(diagonal.NativeXZDirection.LengthSquared()-1)<1e-6&&diagonal.RawSpeedBits==Bits(speed),"diagonal supplied unit direction retains source raw bits");
            now=20000;var second=host.Advance(now);Check(second.Transform.Translation.X>0&&second.Transform.Translation.Z>forward.Transform.Translation.Z,"actual diagonal tick");
            await KeyEvent(Key.W,false,control:true);Check(host.HeldKeyCount==1&&host.LastAdmission!.NativeXZDirection==V2.UnitX,"modified release clears physical W");
            now=30000;host.Advance(now);await KeyEvent(Key.D,false);Check(host.LastAdmission is null&&host.LastStopAccepted&&host.HeldKeyCount==0,"release distinct explicit stop");
            now=40000;var stopped=host.Advance(now);Check(stopped.Speed==0,"actual zero stop tick");
            await KeyEvent(Key.W,true);await KeyEvent(Key.S,true);Check(host.LastAdmission is null&&host.LastStopAccepted&&host.HeldKeyCount==2,"opposing keys stop without selected speed");
            now=50000;var opposed=host.Advance(now);Check(opposed.Transform==stopped.Transform,"opposing keys do not move");
            host.Notification((int)NotificationWMWindowFocusOut);Check(host.HeldKeyCount==0,"native focus out releases keys");
            now=100000;host.Notification((int)NotificationWMWindowFocusIn);now=110000;var resumed=host.Advance(now);Check(resumed.Transform==opposed.Transform&&resumed.StepsThisUpdate==0,"focus resume no catch-up");
            await KeyEvent(Key.W,true);now=120000;var live=host.Advance(now);Check(live.Speed>0,"fresh key after focus resumes sourced motion");
            host.ProcessMode=ProcessModeEnum.Pausable;ProcessMode=ProcessModeEnum.Always;GetTree().Paused=true;await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Check(host.HeldKeyCount==0,"real tree pause releases keys");
            now=180000;GetTree().Paused=false;await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);now=190000;Check(host.Advance(now).Transform==live.Transform,"real unpause no catch-up");
            host.Reset(now);Check(host.HeldKeyCount==0&&host.LastState!.Speed==0&&host.LastAdmission is null,"reset clears held keys and admission evidence");
            host.SetOrientation(new ActorViewOrientation(MathF.PI/2,.9f));await KeyEvent(Key.None,true,Key.W);Check(host.LastAdmission!.NativeXZDirection.X>.9999&&Math.Abs(host.LastAdmission.NativeXZDirection.Y)<1e-6,"injected native heading; pitch excluded");
            rows.Add(new{Kind="Source",Source=(int)source,Speed=speed,Forward=new[]{forward.Transform.Translation.X,forward.Transform.Translation.Y,forward.Transform.Translation.Z},DiagonalDirection=new[]{diagonal.NativeXZDirection.X,diagonal.NativeXZDirection.Y},Stopped=stopped.Speed,FocusNoCatchup=resumed.Transform==opposed.Transform});
            now=200000;host.Close();host.Close();Check(host.IsClosed&&host.HeldKeyCount==0&&reader.Read(g,42)is not null&&w.State!=ZoneSceneTransitionState.Closed,"close owns admission only");Reject(()=>a.SubmitStop(now));
        }
        finally{GetTree().Paused=false;Remove(host);}
    }
    private async Task UnavailableProof(SceneWorldMovementSpeedSource source)
    {
        using var w=SceneWorldExplicitStopScenarios.World(profile);long g=SceneWorldExplicitStopScenarios.Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=new SceneWorldReceivedCreatureMovementInputs(w);SceneWorldExplicitStopScenarios.Received(w,g,Bits(3),Bits(8));var a=SceneWorldExplicitStopScenarios.Admission(w,motions,profile,source,reader);long now=0;var host=new SceneWorldSourcedInputHost(()=>now);AddChild(host);host.Configure(a,g,reader);
        try
        {
            await KeyEvent(Key.W,true);now=10000;var valid=host.Advance(now);
            SceneWorldExplicitStopScenarios.Received(w,g,0x7FC01234,0x7FC01234);now=20000;var unavailable=host.Advance(now);
            Check(host.SourceUnavailable&&host.LastAdmission is null&&host.LastStopAccepted&&unavailable.Transform==valid.Transform&&!unavailable.Active,"unavailable selected source stops and suspends; no stale catch-up");
            SceneWorldExplicitStopScenarios.Received(w,g,Bits(4),Bits(4));now=30000;var resume=host.Advance(now);Check(resume.Transform==valid.Transform&&resume.StepsThisUpdate==0,"restored source explicit resume boundary no replay");
            now=40000;var fresh=host.Advance(now);Check(Math.Abs(fresh.Transform.Translation.Z-valid.Transform.Translation.Z-.04f)<1e-6&&!host.SourceUnavailable&&host.LastAdmission!.Speed==4,"fresh receipt used after restoration");
            rows.Add(new{Kind="Unavailable",Source=(int)source,Before=valid.Transform.Translation.Z,Unavailable=unavailable.Transform.Translation.Z,Resume=resume.Transform.Translation.Z,After=fresh.Transform.Translation.Z,Stopped=host.LastStopAccepted});
        }
        finally{Remove(host);}
    }
    private async Task MissingAuthoredProof()
    {
        using var w=SceneWorldExplicitStopScenarios.World(missing);long g=SceneWorldExplicitStopScenarios.Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=new SceneWorldReceivedCreatureMovementInputs(w);var a=SceneWorldExplicitStopScenarios.Admission(w,motions,missing,SceneWorldMovementSpeedSource.AuthoredSpeed0,reader);long now=0;var host=new SceneWorldSourcedInputHost(()=>now);AddChild(host);host.Configure(a);
        try{await KeyEvent(Key.W,true);now=10000;var state=host.Advance(now);Check(host.SourceUnavailable&&host.LastAdmission is null&&state.Speed==0&&state.Transform.Translation==V3.Zero,"missing authored input has stop only, no seeded2/6 fallback");rows.Add(new{Kind="MissingAuthored",Speed=state.Speed});}finally{Remove(host);}
    }
    private async Task Failures()
    {
        foreach(string failure in new[]{"backward","reentry","state-mutation","parent-mutation","native-free","generation","recreate","reader-close","future-prefix"})
        {
            using var w=SceneWorldExplicitStopScenarios.World(profile);long g=SceneWorldExplicitStopScenarios.Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=new SceneWorldReceivedCreatureMovementInputs(w);SceneWorldExplicitStopScenarios.Received(w,g,Bits(3),Bits(8));var a=SceneWorldExplicitStopScenarios.Admission(w,motions,profile,SceneWorldMovementSpeedSource.ReceivedField7,reader);long now=0;Action? mutation=null;int calls=0;SceneWorldSourcedInputHost? host=null;host=new(()=>{calls++;mutation?.Invoke();return now;});AddChild(host);host.Configure(a,g,reader);
            try
            {
                Task.Run(()=>Reject(()=>host.SetPaused(true))).GetAwaiter().GetResult();Check(!host.IsClosed&&host.Failure is null,"foreign owner fails before mutation");
                if(failure=="backward"){now=10000;await KeyEvent(Key.W,true);now=9999;}
                else if(failure=="reentry")mutation=()=>host.Close();
                else if(failure=="state-mutation")mutation=()=>a.Advance(10000);
                else if(failure=="parent-mutation")mutation=()=>RemoveChild(host);
                else if(failure=="native-free")mutation=()=>host.Free();
                else if(failure=="generation")mutation=()=>SceneWorldExplicitStopScenarios.Stage(w,baseline);
                else if(failure=="recreate")mutation=()=>{w.Accept(g,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0)));w.Accept(g,SceneWorldExplicitStopScenarios.Create());w.Accept(g,baseline);w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42)));};
                else if(failure=="reader-close")mutation=()=>reader.Dispose();
                else mutation=()=>a.SubmitStop(60000000);
                int before=calls;using var key=new InputEventKey{PhysicalKeycode=Key.D,Pressed=true};Reject(()=>host.HandleInput(key));Check(host.IsClosed&&host.HeldKeyCount==0&&host.LastAdmission is null&&host.Failure is not null&&calls==before+1,"failure closes once with no recursive clock/source prefix "+failure);Reject(()=>a.SubmitStop(now));rows.Add(new{Kind="Failure",Name=failure,Closed=host.IsClosed,ClockCalls=calls-before});
            }
            finally{mutation=null;Remove(host);}
        }
        using(var w=SceneWorldExplicitStopScenarios.World(profile))
        {
            long g=SceneWorldExplicitStopScenarios.Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=new SceneWorldReceivedCreatureMovementInputs(w);var a=SceneWorldExplicitStopScenarios.Admission(w,motions,profile,SceneWorldMovementSpeedSource.AuthoredSpeed0,reader);var orphan=new SceneWorldSourcedInputHost(()=>0);Reject(()=>orphan.Configure(a));Check(a.SubmitStop(0),"failed parent validation leaves admission with caller");orphan.Close();orphan.Free();orphan.Dispose();a.Dispose();
            var owned=SceneWorldExplicitStopScenarios.Admission(w,motions,profile,SceneWorldMovementSpeedSource.AuthoredSpeed0,reader);var host=new SceneWorldSourcedInputHost(()=>0);AddChild(host);host.Configure(owned);var candidate=SceneWorldExplicitStopScenarios.Admission(w,motions,profile,SceneWorldMovementSpeedSource.AuthoredSpeed1,reader);Reject(()=>host.Configure(candidate));Check(candidate.SubmitStop(0),"second configure leaves candidate with caller");candidate.Dispose();Remove(host);
        }
    }
}
