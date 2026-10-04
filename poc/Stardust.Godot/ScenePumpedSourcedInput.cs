using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

// Explicit modern movement policy using the original resource publication; offline host proof.
public partial class ScenePumpedWorldPreview
{
    private bool sourcedRequested, sourcedStarted, sourcedComplete;
    private SceneWorldMovementSpeedSource sourcedSelection;
    private SceneWorldSourcedInputHost? sourcedHost;
    private SceneWorldExplicitMovementAdmission? sourcedAdmission;
    private SceneWorldReceivedCreatureMovementInputs? sourcedReader;
    private SceneWorldCreatureMovementCurrentInputs? sourcedCurrentReader;
    private System.Numerics.Vector3 sourcedStart, sourcedStopped;
    private uint sourcedSpeedBits;
    private string? sourcedVisualFingerprint;
    private int sourcedTicks;
    private bool sourcedPresentationPlaying;
    private void LoadSourcedInputArguments(string[] args)
    {
        int index=Array.IndexOf(args,"--sourced-input");
        if(index<0)return;
        Check(args.Count(a=>a=="--sourced-input")==1 && index+1<args.Length,"One explicit source argument");
        sourcedSelection=args[index+1] switch {
            "AuthoredSpeed0"=>SceneWorldMovementSpeedSource.AuthoredSpeed0,
            "AuthoredSpeed1"=>SceneWorldMovementSpeedSource.AuthoredSpeed1,
            "ReceivedField7"=>SceneWorldMovementSpeedSource.ReceivedField7,
            "ReceivedField11"=>SceneWorldMovementSpeedSource.ReceivedField11,
            "CurrentField7"=>SceneWorldMovementSpeedSource.CurrentField7,
            "CurrentField11"=>SceneWorldMovementSpeedSource.CurrentField11,
            _=>throw new ArgumentException("Explicit known movement source required")};
        Check(args.Contains("--local-input") && args.Contains("--creature4-partial-peer") && !args.Contains("--terrain-peer") && !args.Contains("--input-loss") && !args.Contains("--pump-cancel"),"Sourced proof requires the complete planar partial-view host");
        sourcedRequested=true;
    }
    private void BeginSourcedInput()
    {
        Check(sourcedRequested && !sourcedStarted && phase==47 && calls==3 && closed==3,"Sourced host starts after existing offline gates");
        inputHost!.Close();inputHost=null; // Release the prior local writer before constructing the new one.
        var inputs=originalHuman!.Resources.Creature!.Creature.AuthoredMovement!.Inputs;
        bool useCurrent=sourcedSelection is SceneWorldMovementSpeedSource.CurrentField7 or SceneWorldMovementSpeedSource.CurrentField11;
        var current=new SceneWorldCreatureMovementCurrentInputs(world!,humanId,inputs,
            "E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243",
            SceneCreatureMovementConstructorScope.CallerVerifiedConcreteClientCreatureRawInputs,true);
        if(useCurrent)sourcedCurrentReader=current;
        try {
            var view=current.Read(generation)!;
            Check(current.IsCurrent(view) && view.IsOperationalContext,"Current original-resource bound movement view");
            GD.Print("sourced_current_inputs="+JsonSerializer.Serialize(new{Current=true,view.OutputBytes,Scope=view.ConstructorScope.ToString(),view.RootSha256,Fields=view.Fields.Select(f=>new{f.Index,f.RawBits,Origin=f.Origin.ToString(),Eligibility=f.Eligibility.ToString(),AuthoredBits=f.AuthoredInput?.Bits,f.ConstructorLiteralBits,f.ConstructorLiteralStoreVA}).ToArray(),RawReceivedStoreSeeded=false,MovementApplied=false}));
        } finally { if(!useCurrent)current.Dispose(); }
        bool received=sourcedSelection is SceneWorldMovementSpeedSource.ReceivedField7 or SceneWorldMovementSpeedSource.ReceivedField11;
        if(received)sourcedReader=new(world!);
        var admission=new SceneWorldExplicitMovementAdmission(world!,motions!,humanId,sourcedSelection,inputs,
            "E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243",sourcedReader,localClock,10000,8,currentInputs:sourcedCurrentReader);
        sourcedAdmission=admission;
        try {
            sourcedHost=new(()=>localClock);AddChild(sourcedHost);
            sourcedHost.Configure(admission,generation,sourcedReader);
        } catch { admission.Dispose();throw; }
        sourcedStart=sourcedHost.LastState!.Transform.Translation;
        sourcedVisualFingerprint=visuals[humanId].CreatureVisual!.Fingerprint();
        SourcedKey(true);
        Check(sourcedHost.LastAdmission is not null && !sourcedHost.SourceUnavailable,"Selected original/received speed is eligible without fallback");
        sourcedSpeedBits=sourcedHost.LastAdmission!.RawSpeedBits;sourcedStarted=true;phase=60;
        locomotion!.SetPlaying(true);sourcedPresentationPlaying=true;
    }
    private void SourcedKey(bool pressed)
    {
        using var input=new InputEventKey{PhysicalKeycode=Key.W,Pressed=pressed};
        Check(sourcedHost!.HandleInput(input),"Accepted sourced physical key transition");
    }
    private void AdvanceSourcedInput()
    {
        if(!sourcedStarted || sourcedComplete || finishing)return;
        localClock+=10000;var state=sourcedHost!.Advance(localClock);
        Check(state.FaultType is null && sourcedHost.Failure is null,"Healthy sourced host");
        bool playing=state.Active && !sourcedHost.SourceUnavailable;
        if(playing!=sourcedPresentationPlaying){locomotion!.SetPlaying(playing);sourcedPresentationPlaying=playing;}
    }
    private void SourcedState(string name,System.Numerics.Vector3 expected)
    {
        var state=sourcedHost!.LastState!;var position=state.Transform.Translation;
        var inputs=originalHuman!.Resources.Creature!.Creature.AuthoredMovement!.Inputs;
        var visual=visuals[humanId].CreatureVisual!;var origin=visual.Root.Position;
        Check(System.Numerics.Vector3.Distance(position,expected)<2e-5f,"Sourced native expected displacement");
        Check(origin.DistanceTo(new Vector3(expected.X,expected.Y,-expected.Z))<2e-5f,"Sourced native/engine reflection");
        Check(ReferenceEquals(visuals[humanId].Publication,originalHuman) && visual.Fingerprint()==sourcedVisualFingerprint && calls==3 && closed==3,"Sourced motion retains original resource publication without I/O");
        var sourceReceipt=sourcedHost.LastAdmission;
        ushort currentIndex=sourcedSelection==SceneWorldMovementSpeedSource.CurrentField7?(ushort)7:(ushort)11;
        GD.Print("sourced_input_state="+JsonSerializer.Serialize(new{Name=name,Source=sourcedSelection.ToString(),CurrentSourceCurrent=sourceReceipt?.CurrentSource is not null && !sourcedHost.IsClosed && sourcedAdmission!.IsSourceCurrent(sourceReceipt),CurrentOrigin=sourceReceipt?.CurrentSource?.Get(currentIndex).Origin.ToString(),RawSpeedBits=sourcedSpeedBits,Native=new[]{position.X,position.Y,position.Z},Engine=new[]{origin.X,origin.Y,origin.Z},Start=new[]{sourcedStart.X,sourcedStart.Y,sourcedStart.Z},state.Speed,state.TimestampMicroseconds,state.StepsThisUpdate,state.Active,Stop=sourcedHost.LastStopAccepted,SourceUnavailable=sourcedHost.SourceUnavailable,Keys=sourcedHost.HeldKeyCount,Ticks=sourcedTicks,RootPath=inputs.RootPath,SourceCount=inputs.Sources.Count,PublicationRetained=true,calls,closed,OriginalEffectiveMovementParity=false}));
    }
    private void FrameSourcedInput()
    {
        float speed=BitConverter.UInt32BitsToSingle(sourcedSpeedBits);
        if(phase==60) {
            sourcedTicks++;var expected=sourcedStart+new System.Numerics.Vector3(0,0,speed*.01f*sourcedTicks);
            SourcedState("moving"+sourcedTicks,expected);
            if(sourcedTicks==3){sourcedStopped=sourcedHost!.LastState!.Transform.Translation;SourcedKey(false);phase=61;}
        } else if(phase==61) {
            SourcedState("released",sourcedStopped);Check(sourcedHost!.LastStopAccepted && sourcedHost.LastState!.Speed==0,"Explicit release stop");
            sourcedHost.SetFocused(false);phase=62;
        } else if(phase==62) {
            SourcedState("focus_suspended",sourcedStopped);Check(!sourcedHost!.LastState!.Active && sourcedHost.HeldKeyCount==0,"Focus suspension clears keys");
            sourcedHost.SetFocused(true);phase=63;
        } else if(phase==63) {
            SourcedState("focus_resumed_idle",sourcedStopped);Check(sourcedHost!.LastState!.Speed==0 && sourcedHost.HeldKeyCount==0,"No catch-up or key resurrection");
            CloseSourcedInput();sourcedComplete=true;phase=64;ScheduleFinish();
        } else throw new InvalidOperationException("Unexpected sourced phase");
    }
    private void CloseSourcedInput()
    {
        sourcedHost?.Close();sourcedReader?.Dispose();sourcedReader=null;
        sourcedCurrentReader?.Dispose();sourcedCurrentReader=null;
        if(sourcedStarted)Check(sourcedHost!.IsClosed,"Sourced input owner closes before world");
        sourcedAdmission=null;
    }
}
