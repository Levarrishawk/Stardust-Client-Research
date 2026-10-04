using Godot;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Session;
using Stardust.Core.SmokeTests;
public partial class CurrentHostProof : Node
{
    private int checks;
    private readonly List<object> rows=new();
    private byte[] baseline=Array.Empty<byte>();
    private SharedCreatureAuthoredMovementInputs p=null!;
    private void Check(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
    private void Reject(Action action){bool rejected=false;try{action();}catch(Exception e)when(e is InvalidOperationException or ArgumentException or ObjectDisposedException){rejected=true;}Check(rejected,"expected rejection");}
    private static uint Bits(float f)=>BitConverter.SingleToUInt32Bits(f);
    private async Task KeyEvent(Key code,bool pressed){using var e=new InputEventKey{PhysicalKeycode=code,Keycode=code,Pressed=pressed};Input.ParseInputEvent(e);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private void Remove(SceneWorldSourcedInputHost host){host.Close();if(GodotObject.IsInstanceValid(host)){if(host.GetParent()==this)RemoveChild(host);host.Free();}host.Dispose();}
    public override async void _Ready()
    {
        try
        {
            string[] args=OS.GetCmdlineUserArgs();string Arg(string n){int i=Array.IndexOf(args,n);if(i<0)throw new ArgumentException(n);return args[i+1];}
            using var fixture=JsonDocument.Parse(File.ReadAllText(Arg("--fixture"),System.Text.Encoding.UTF8));p=SceneWorldExplicitStopScenarios.Profile(fixture.RootElement.GetProperty("Profiles")[0]);baseline=Convert.FromHexString(fixture.RootElement.GetProperty("Baseline").GetString()!);
            foreach(var source in new[]{SceneWorldMovementSpeedSource.CurrentField7,SceneWorldMovementSpeedSource.CurrentField11}){await Sequence(source);await Failures(source);}
            File.WriteAllText(Arg("--output"),JsonSerializer.Serialize(new{Cases=rows,Checks=checks}),new System.Text.UTF8Encoding(false));GD.Print($"R50_GODOT_PASS cases={rows.Count} checks={checks}");GetTree().Quit();
        }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
    private async Task Sequence(SceneWorldMovementSpeedSource source)
    {
        using var w=SceneWorldExplicitStopScenarios.World(p);long g=SceneWorldExplicitStopScenarios.Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=SceneWorldCurrentMovementAdmissionScenarios.Reader(w,p);var a=SceneWorldCurrentMovementAdmissionScenarios.Admission(w,motions,p,source,reader);long now=0;var host=new SceneWorldSourcedInputHost(()=>now);AddChild(host);host.Configure(a);
        try
        {
            float authored=source==SceneWorldMovementSpeedSource.CurrentField7?2:6,received=source==SceneWorldMovementSpeedSource.CurrentField7?3:8,restored=source==SceneWorldMovementSpeedSource.CurrentField7?4:9;
            await KeyEvent(Key.W,true);var bootstrap=host.LastAdmission!;Check(bootstrap.CurrentSource!.Get(source==SceneWorldMovementSpeedSource.CurrentField7?(ushort)7:(ushort)11).Origin==SceneCreatureMovementCurrentOrigin.ConstructorAuthoredRaw,"unchanged Node current choice bootstrap provenance");now=10000;var first=host.Advance(now);Check(Math.Abs(first.Transform.Translation.Z-authored*.01f)<1e-6,"actual bootstrap tick");
            SceneWorldExplicitStopScenarios.Received(w,g,Bits(3),Bits(8));Check(!a.IsSourceCurrent(bootstrap),"late first publication stales previous absence receipt");host.Advance(now);Check(host.LastAdmission!.Speed==received&&host.LastAdmission.CurrentSource!.Received is not null,"Node automatically fresh current receipt after first received source");now=20000;var late=host.Advance(now);
            SceneWorldExplicitStopScenarios.Received(w,g,0x7FC01234,0x7F800000);host.Advance(now);Check(host.SourceUnavailable&&host.LastAdmission is null&&host.LastStopAccepted,"present nonfinite cannot fall back to constructor value");now=30000;var missing=host.Advance(now);Check(missing.Transform==late.Transform&&!missing.Active,"unavailable current speed no stale catch-up");
            SceneWorldExplicitStopScenarios.Received(w,g,Bits(4),Bits(9));var resume=host.Advance(now);Check(resume.Transform==late.Transform&&resume.StepsThisUpdate==0,"restoration explicit resume without replay");now=40000;var fresh=host.Advance(now);Check(host.LastAdmission!.Speed==restored&&host.LastAdmission.CurrentSource!.Get(source==SceneWorldMovementSpeedSource.CurrentField7?(ushort)7:(ushort)11).Origin==SceneCreatureMovementCurrentOrigin.Received,"restored raw source captured");
            SceneWorldExplicitStopScenarios.Received(w,g,0,0x80000000);host.Advance(now);Check(host.LastAdmission!.RawSpeedBits==(source==SceneWorldMovementSpeedSource.CurrentField7?0U:0x80000000U)&&!host.SourceUnavailable,"present zero signed zero remain received, not fallback");now=50000;var zero=host.Advance(now);Check(zero.Speed==0&&zero.Transform==fresh.Transform,"actual received zero tick");await KeyEvent(Key.W,false);Check(host.LastAdmission is null&&host.LastStopAccepted,"release distinct zero stop");
            host.Notification((int)NotificationWMWindowFocusOut);now=100000;host.Notification((int)NotificationWMWindowFocusIn);now=110000;var focused=host.Advance(now);Check(focused.Transform==zero.Transform&&host.HeldKeyCount==0,"focus no catch-up/resurrection");
            host.ProcessMode=ProcessModeEnum.Pausable;ProcessMode=ProcessModeEnum.Always;GetTree().Paused=true;await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);now=150000;GetTree().Paused=false;await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);host.Advance(160000);Check(host.HeldKeyCount==0,"real tree pause clears current-source keys");now=160000;
            rows.Add(new{Kind="Current",Source=(int)source,Bootstrap=first.Transform.Translation.Z,Late=late.Transform.Translation.Z,Unavailable=missing.Transform.Translation.Z,Resume=resume.Transform.Translation.Z,Restored=fresh.Transform.Translation.Z,Zero=zero.Transform.Translation.Z,RawZero=host.SpeedSource==SceneWorldMovementSpeedSource.CurrentField7?0U:0x80000000U,Stopped=host.LastStopAccepted});
            host.Close();Check(reader.Read(g)is not null&&host.IsClosed,"close borrows current reader");Reject(()=>a.SubmitStop(now));
        }finally{GetTree().Paused=false;Remove(host);}
    }
    private async Task Failures(SceneWorldMovementSpeedSource source)
    {
        foreach(var kind in new[]{"reader-close","world-close","generation","reentry"})
        {
            using var w=SceneWorldExplicitStopScenarios.World(p);long g=SceneWorldExplicitStopScenarios.Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=SceneWorldCurrentMovementAdmissionScenarios.Reader(w,p);var a=SceneWorldCurrentMovementAdmissionScenarios.Admission(w,motions,p,source,reader);Action? callback=null;SceneWorldSourcedInputHost? host=null;host=new(()=>{callback?.Invoke();return 0;});AddChild(host);host.Configure(a);
            try
            {
                Task.Run(()=>Reject(()=>host.SetPaused(true))).GetAwaiter().GetResult();
                if(kind=="reader-close")reader.Dispose();else if(kind=="world-close")w.Close();else if(kind=="generation")SceneWorldExplicitStopScenarios.Stage(w,baseline);else callback=()=>host.Close();
                using var key=new InputEventKey{PhysicalKeycode=Key.W,Pressed=true};Reject(()=>host.HandleInput(key));Check(host.IsClosed&&host.Failure is not null&&host.LastAdmission is null&&host.HeldKeyCount==0,"new current mode failure closes without source prefix "+kind);rows.Add(new{Kind="Failure",Source=(int)source,Name=kind,Closed=host.IsClosed});
            }finally{callback=null;Remove(host);}
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        }
    }
}
