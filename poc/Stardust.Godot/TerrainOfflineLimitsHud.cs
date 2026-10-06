using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Godot;

public sealed record TerrainOfflineLimitsHudState(int MaximumUpdates,int MaximumClickAttempts,
    int RemainingClickAttempts,bool GameplayEnabled,bool Paused);

/// <summary>Owned noninteractive limits display over caller-associated immutable state.</summary>
public sealed class TerrainOfflineLimitsHud : IDisposable
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private Node? owner,parent;
    private Viewport? viewport;
    private Func<TerrainOfflineLimitsHudState>? source;
    private TerrainOfflineLimitsHudLayer? layer;
    private Label? label;
    private bool attempted,configured,busy,reentered,retired;
    private string caption="";
    private int refreshes,retirements;
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Limits HUD owner/Godot main thread required.");}
    private void Enter(){Owner();if(busy){reentered=true;throw new InvalidOperationException("Limits HUD reentry.");}busy=true;reentered=false;}
    private static bool Live(Node? node)=>GodotObject.IsInstanceValid(node)&&node!.IsInsideTree()&&!node.IsQueuedForDeletion();
    private void Guard(CancellationToken token,bool display=true)
    {
        token.ThrowIfCancellationRequested();
        if(reentered||retired||!Live(owner)||!Live(parent)||!Live(viewport)||owner!.GetParent()!=parent||owner.GetViewport()!=viewport||parent!.GetViewport()!=viewport||
            display&&(!Live(layer)||layer!.GetParent()!=owner||layer.GetViewport()!=viewport||!Live(label)||label!.GetParent()!=layer||label.GetViewport()!=viewport||label.MouseFilter!=Control.MouseFilterEnum.Ignore||label.FocusMode!=Control.FocusModeEnum.None))
            throw new InvalidOperationException("Exact live limits HUD owner/parent/viewport/display required.");
    }
    public string Caption {get{Owner();return caption;}}
    public int RefreshCount {get{Owner();return refreshes;}}
    public int RetirementCount {get{Owner();return retirements;}}
    public bool IsConfigured {get{Owner();return configured;}}
    public bool IsRetired {get{Owner();return retired;}}
    private static string Text(TerrainOfflineLimitsHudState state)
    {
        if(state is null||state.MaximumUpdates is <1 or >6000||state.MaximumClickAttempts is <1 or >128||state.RemainingClickAttempts<0||state.RemainingClickAttempts>state.MaximumClickAttempts)
            throw new InvalidDataException("Bounded limits HUD state required.");
        return (state.MaximumUpdates*.01).ToString("0.0",CultureInfo.InvariantCulture)+" simulation seconds | Terrain clicks remaining: "+state.RemainingClickAttempts.ToString(CultureInfo.InvariantCulture)+" | Limit keeps movement active";
    }
    public void Configure(Node owner,Func<TerrainOfflineLimitsHudState> source,CancellationToken token=default)
    {
        Enter();try
        {
            if(attempted||retired)throw new InvalidOperationException("Limits HUD configuration accepted once.");attempted=true;
            ArgumentNullException.ThrowIfNull(owner);ArgumentNullException.ThrowIfNull(source);
            this.owner=owner;parent=owner.GetParent();viewport=owner.GetViewport();this.source=source;Guard(token,false);
            layer=new TerrainOfflineLimitsHudLayer{Name="OfflineLimitsHud",Layer=21};layer.Bind(OnNativeRetirement);
            label=new Label{Name="OfflineLimitsLabel",Position=new(28,286),MouseFilter=Control.MouseFilterEnum.Ignore,FocusMode=Control.FocusModeEnum.None};label.AddThemeFontSizeOverride("font_size",16);layer.AddChild(label);owner.AddChild(layer);
            Guard(token);RefreshCore(token);configured=true;
        }catch{Retire(true);throw;}finally{busy=false;}
    }
    private void RefreshCore(CancellationToken token)
    {
        Guard(token);var state=source!();Guard(token);string text=Text(state);int next=checked(refreshes+1);
        label!.Text=text;Guard(token);caption=text;refreshes=next;
    }
    public bool Refresh(CancellationToken token=default)
    {
        Enter();try{if(retired)return false;if(!configured)throw new InvalidOperationException("Configured limits HUD required.");RefreshCore(token);return true;}
        catch{Retire(true);throw;}finally{busy=false;}
    }
    private void FreeOwned()
    {
        var owned=layer;layer=null;
        if(GodotObject.IsInstanceValid(owned)){owned!.ReleaseCallback();owned.Free();}
    }
    private void Retire(bool free)
    {
        if(!retired){retired=true;retirements++;layer?.ReleaseCallback();label=null;source=null;owner=parent=null;viewport=null;}
        if(free)FreeOwned();
    }
    private void OnNativeRetirement(){Owner();if(busy)reentered=true;Retire(false);}
    public void Dispose(){Enter();try{Retire(true);}finally{busy=false;}}
}

// Native lifecycle only retires managed ownership; it never recursively frees a node.
public partial class TerrainOfflineLimitsHudLayer : CanvasLayer
{
    private Action? retired;
    public void Bind(Action callback)=>retired=callback;
    public void ReleaseCallback()=>retired=null;
    private void Notify(){var callback=retired;retired=null;callback?.Invoke();}
    public override void _ExitTree()=>Notify();
    protected override void Dispose(bool disposing){if(disposing)Notify();else retired=null;base.Dispose(disposing);}
}
