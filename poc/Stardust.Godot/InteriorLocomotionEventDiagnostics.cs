using System;
using System.Linq;
using Godot;
using Stardust.Core.Assets;
using NVector = System.Numerics.Vector3;

/// <summary>Modern diagnostics fed only by the existing accepted scene ticks.</summary>
internal sealed class InteriorLocomotionEventDiagnostics : IDisposable
{
    private LocomotionEventPreviewClock? clock;
    private LocomotionEventDiagnosticLog? log;
    private Label? label;
    private CheckBox? enabled;
    private long walkEvents, runEvents;
    private bool verifyPinnedFrames, verifyTeardown;
    public LocomotionPreviewState State => Clock.State;
    public long DeliveredTotal => Log.DeliveredTotal;
    private LocomotionEventPreviewClock Clock => clock ?? throw new ObjectDisposedException(nameof(InteriorLocomotionEventDiagnostics));
    private LocomotionEventDiagnosticLog Log => log ?? throw new ObjectDisposedException(nameof(InteriorLocomotionEventDiagnostics));
    public static void ValidateArguments(string[] args)
    {
        if(!args.Contains("--scene-character-events"))return;
        string[] needed={"--asset-root","--asset-config","--asset-member","--asset-sha256","--door-table-sha256","--interior-scene-spec","--interior-scene-sha256","--scene-geometry","--scene-character","--scene-character-appearance-sha256","--actor-template","--actor-template-sha256","--scene-character-locomotion-profile","--scene-character-animation-table","--scene-character-animation-name","--scene-character-animation-sha256"};
        if(args.Count(a=>a=="--scene-character-events")!=1||needed.Any(a=>args.Count(v=>v==a)!=1)||new[]{"--cancel-scene-load","--asset-creature","--asset-detail","--asset-component","--asset-interior","--palette-inspector","--door-style","--animation-event-preview"}.Any(args.Contains))throw new ArgumentException("Scene events require one owned pinned interior idle/movement bank and exclude cancellation.");
        foreach(string option in needed.Where(a=>a!="--scene-geometry"&&a!="--scene-character"))
        {int i=Array.IndexOf(args,option);if(i+1>=args.Length||args[i+1].StartsWith("--",StringComparison.Ordinal))throw new ArgumentException("Scene event option value required.");}
    }
    public static void VerifyArgumentPolicy(string[] valid)
    {
        void Reject(string[] candidate){try{ValidateArguments(candidate);throw new AssetPreviewException("Invalid event options accepted.");}catch(ArgumentException){}}
        string[] required={"--asset-root","--asset-config","--asset-member","--asset-sha256","--door-table-sha256","--interior-scene-spec","--interior-scene-sha256","--scene-geometry","--scene-character","--scene-character-appearance-sha256","--actor-template","--actor-template-sha256","--scene-character-locomotion-profile","--scene-character-animation-table","--scene-character-animation-name","--scene-character-animation-sha256"};
        foreach(string option in required)
        {
            int i=Array.IndexOf(valid,option);int width=option=="--scene-geometry"||option=="--scene-character"?1:2;
            Reject(valid.Take(i).Concat(valid.Skip(i+width)).ToArray());Reject(valid.Append(option).ToArray());
        }
        foreach(string option in new[]{"--cancel-scene-load","--asset-creature","--asset-detail","--asset-component","--asset-interior","--palette-inspector","--door-style","--animation-event-preview","--scene-character-events"})Reject(valid.Append(option).ToArray());
        ValidateArguments(Array.Empty<string>());
        GD.Print("PASS: interior event raw option policy;41 missing/duplicate/conflicting cases reject before asset reads");
    }
    public InteriorLocomotionEventDiagnostics(CreatureAppearanceDescriptorBundle bundle,int panelCapacity=16)
    {
        var idle=bundle.PreparedAnimation??throw new AssetPreviewException("Prepared idle required.");
        var bank=bundle.AdditionalAnimations??throw new AssetPreviewException("Owned movement bank required.");
        clock=new(LocomotionPreviewClock.Describe(idle),LocomotionPreviewClock.Describe(bank["walk"]),LocomotionPreviewClock.Describe(bank["run"]),3,
            CompressedLocomotion.Create(CompressedKeyframeDecoder.Create(bundle.SelectedAnimation!.Clip)),bank["walk"].Locomotion!,bank["run"].Locomotion!);
        log=new(clock,panelCapacity);
    }
    public void Install(VBoxContainer panel)
    {
        enabled=new CheckBox{Text="Show accepted-movement events",ButtonPressed=true};panel.AddChild(enabled);
        var clear=new Button{Text="Clear event history"};panel.AddChild(clear);clear.Pressed+=()=>{if(log is null)return;log.ClearRecent();Refresh();};
        label=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart};panel.AddChild(label);Refresh();
    }
    public void Tick(double seconds,NVector accepted,bool active,LocomotionPreviewState authoritative)
    {
        var batch=Clock.Tick(seconds,accepted,active);
        if(batch.State!=authoritative)throw new AssetPreviewException("Event/authoritative locomotion state mismatch.");
        Log.SynchronizeGeneration();
        if(batch.Count>0&&(enabled?.ButtonPressed??true))
        {
            Log.Deliver(batch);
            foreach(var e in batch)
            {
                if(batch.State.Mode==PreviewLocomotionMode.Walk){if(verifyPinnedFrames&&e.Frame!=7&&e.Frame!=23)throw new AssetPreviewException("Unexpected pinned walk event frame.");walkEvents++;}
                if(batch.State.Mode==PreviewLocomotionMode.Run){if(verifyPinnedFrames&&e.Frame!=6&&e.Frame!=17)throw new AssetPreviewException("Unexpected pinned run event frame.");runEvents++;}
            }
        }
        Refresh();
    }
    public void Reset(NVector heading){Clock.Reset(heading);Log.SynchronizeGeneration();Refresh();}
    private void Refresh()
    {
        if(label is null)return;
        label.Text=$"Modern movement events: {State.Mode} frame {State.Frame:F2} epoch {Clock.Generation}\nCurrent: {Log.GenerationDeliveredTotal}; lifetime: {Log.DeliveredTotal}\n"+string.Join("\n",Log.Recent.Select(r=>$"{r.Occurrence.Name} @{r.Occurrence.Frame} cycle {r.Occurrence.Cycle}"));
    }
    public void RevealCapture(ScrollContainer scroll)
    {if(label is not null)scroll.EnsureControlVisible(label);}
    public void VerifyReset(long lifetime)
    {if(Log.Count!=0||Log.GenerationDeliveredTotal!=0||Log.DeliveredTotal!=lifetime)throw new AssetPreviewException("Event reset history/lifetime failed.");}
    public void VerifyOriginalRoute(CreatureAppearanceDescriptorBundle bundle,Action identity,Action rotated,Action<NVector,float> manual,NVector direction,SkeletalMeshPreview mesh)
    {
        verifyPinnedFrames=true;verifyTeardown=true;
        string topology=mesh.GeometryFingerprint(false);int textures=mesh.TextureCount;long bytes=mesh.TextureBytes;
        foreach(var configure in new[]{identity,rotated})
        {
            foreach(string key in new[]{"walk","run"})
            {
                configure();long before=key=="walk"?walkEvents:runEvents;
                var clip=bundle.AdditionalAnimations![key];float distance=clip.Locomotion!.AuthoredSpeed/clip.FrameRate;
                float seconds=distance/(key=="walk"?1.6f:5.2f);
                for(int i=0;i<clip.EndFrame*3+1;i++)manual(direction*(i%2==0?distance:-distance),seconds);
                if((key=="walk"?walkEvents:runEvents)-before!=6)throw new AssetPreviewException("Original accepted route did not emit six footsteps.");
                manual(NVector.Zero,.05f);if(State.Mode!=PreviewLocomotionMode.Idle||Log.Recent.Any(r=>r.Occurrence.Frame==7||r.Occurrence.Frame==23||r.Occurrence.Frame==6||r.Occurrence.Frame==17))throw new AssetPreviewException("Blocked tick retained moving history.");
            }
        }
        if(topology!=mesh.GeometryFingerprint(false)||textures!=mesh.TextureCount||bytes!=mesh.TextureBytes)throw new AssetPreviewException("Event diagnostics recreated retained resources.");
        long lifetime=DeliveredTotal;enabled!.ButtonPressed=false;manual(NVector.Zero,.1f);if(DeliveredTotal!=lifetime)throw new AssetPreviewException("Disabled diagnostics delivered.");enabled.ButtonPressed=true;
        Log.ClearRecent();if(Log.Count!=0||DeliveredTotal!=lifetime)throw new AssetPreviewException("Clear event history changed total.");
        verifyPinnedFrames=false;
        GD.Print($"PASS: original interior accepted events; owner_poses=2 walk/run_three_wraps=6_each state_equal_each_tick=true reset_clear_disable=true retained_resources=true lifetime={DeliveredTotal}");
    }
    public void Dispose()
    {
        log?.ClearRecent();log=null;clock=null;label=null;enabled=null;
        if(verifyTeardown)
        {
            try{_ = State;throw new AssetPreviewException("Disposed event owner remains accessible.");}catch(ObjectDisposedException){}
            GD.Print("PASS: interior event teardown; ring cleared clock/log/UI references released disposed owner rejects");verifyTeardown=false;
        }
    }
}
