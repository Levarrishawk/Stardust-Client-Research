using System;
using System.Threading;
using Godot;

public sealed record TerrainOfflineRestartDiagnostic(int Request,bool CallbackInvoked,bool CallbackReturned,
    bool? ProposedRestart,bool StateCommitted,string? FailureType);

/// <summary>Owns persistent outcome/restart displays; borrows only caller owner and restart callback.</summary>
public sealed class TerrainOfflineRestartHud : IDisposable
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private Node? owner,parent;
    private Viewport? viewport;
    private Func<bool>? restart;
    private CancellationToken lifetime;
    private TerrainOfflineOutcomeHud? outcome;
    private TerrainOfflineRestartHudLayer? layer;
    private Button? button;
    private bool configured,attempted,busy,reentered,retired,connected,restartDisabled;
    private int requests,accepted,retirements;
    private string caption="";
    private TerrainOfflineRestartDiagnostic? diagnostic;
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Restart HUD owner/Godot main thread required.");}
    private void Enter(){Owner();if(busy){reentered=true;throw new InvalidOperationException("Restart HUD reentry.");}busy=true;reentered=false;}
    private static bool Live(Node? node)=>GodotObject.IsInstanceValid(node)&&node!.IsInsideTree()&&!node.IsQueuedForDeletion();
    private void Guard()
    {
        lifetime.ThrowIfCancellationRequested();
        if(reentered||retired||!Live(owner)||!Live(parent)||!Live(viewport)||owner!.GetParent()!=parent||owner.GetViewport()!=viewport||parent!.GetViewport()!=viewport||
            !Live(layer)||layer!.GetParent()!=owner||layer.GetViewport()!=viewport||!Live(button)||button!.GetParent()!=layer||button.GetViewport()!=viewport||button.MouseFilter!=Control.MouseFilterEnum.Stop||button.FocusMode!=Control.FocusModeEnum.None||button.Position!=new Vector2(16,484)||button.Size!=new Vector2(240,40)||button.Text!="Restart offline preview"||outcome is null||outcome.IsRetired)
            throw new InvalidOperationException("Exact live restart owner/parent/viewport/display required.");
        if(!outcome!.Refresh())throw new InvalidOperationException("Live passive outcome subtree required.");
    }
    public string Caption {get{Owner();return caption;}}
    public int RequestCount {get{Owner();return requests;}}
    public int AcceptedRestartCount {get{Owner();return accepted;}}
    public int RetirementCount {get{Owner();return retirements;}}
    public bool IsRetired {get{Owner();return retired;}}
    public bool IsRestartEnabled {get{Owner();return configured&&!retired&&accepted==0&&!restartDisabled;}}
    public TerrainOfflineRestartDiagnostic? LastDiagnostic {get{Owner();return diagnostic;}}
    public void Configure(Node owner,TerrainOfflineOutcomeHudState outcome,Func<bool> restart,CancellationToken token=default)
    {
        Enter();try
        {
            if(attempted||retired)throw new InvalidOperationException("Restart HUD configuration accepted once.");attempted=true;
            ArgumentNullException.ThrowIfNull(owner);ArgumentNullException.ThrowIfNull(outcome);ArgumentNullException.ThrowIfNull(restart);token.ThrowIfCancellationRequested();
            this.owner=owner;parent=owner.GetParent();viewport=owner.GetViewport();this.restart=restart;lifetime=token;
            this.outcome=new TerrainOfflineOutcomeHud();this.outcome.Configure(owner,()=>outcome,token);caption=this.outcome.Caption;
            layer=new TerrainOfflineRestartHudLayer{Name="OfflineRestartHud",Layer=23};layer.Bind(OnNativeRetirement);
            button=new Button{Name="OfflineRestartButton",Text="Restart offline preview",Position=new(16,484),Size=new(240,40),FocusMode=Control.FocusModeEnum.None,MouseFilter=Control.MouseFilterEnum.Stop};
            button.AddThemeFontSizeOverride("font_size",16);button.Pressed+=OnPressed;connected=true;layer.AddChild(button);owner.AddChild(layer);Guard();configured=true;
        }catch{Retire(false);if(!configured)FreeOwned();throw;}finally{busy=false;}
    }
    public bool RequestRestart()
    {
        Enter();try
        {
            if(retired||accepted>0||restartDisabled)return false;
            if(!configured)throw new InvalidOperationException("Configured restart HUD required.");
            bool invoked=false,returned=false;bool? proposed=null;
            try
            {
                Guard();requests=checked(requests+1);invoked=true;proposed=restart!();returned=true;Guard();
                if(proposed.Value){button!.Disabled=true;Guard();accepted=checked(accepted+1);}
                diagnostic=new(requests,invoked,returned,proposed,true,null);return proposed.Value;
            }
            catch(Exception error){diagnostic=new(requests,invoked,returned,proposed,false,error.GetType().FullName);Retire(false);throw;}
        }finally{busy=false;}
    }
    /// <summary>One-way availability latch; preserves outcome, callback history and request counters.</summary>
    public void DisableRestart()
    {
        Enter();try
        {
            if(retired||accepted>0||restartDisabled)return;
            if(!configured)throw new InvalidOperationException("Configured restart HUD required.");
            try{Guard();button!.Disabled=true;restartDisabled=true;Guard();}
            catch{Retire(false);throw;}
        }finally{busy=false;}
    }
    private void OnPressed(){try{RequestRestart();}catch(Exception){/* Engine signal boundary: diagnostic and retired display retained. */}}
    private void Retire(bool nativeExit)
    {
        if(retired)return;retired=true;retirements++;
        if(GodotObject.IsInstanceValid(button))
        {
            if(connected)button!.Pressed-=OnPressed;
            if(!nativeExit)button!.Disabled=true;
        }
        connected=false;layer?.ReleaseCallback();button=null;restart=null;owner=parent=null;viewport=null;lifetime=default;
    }
    private void OnNativeRetirement(){Owner();if(busy)reentered=true;Retire(true);}
    private void FreeOwned()
    {
        var owned=layer;layer=null;if(GodotObject.IsInstanceValid(owned)){owned!.ReleaseCallback();owned.Free();}
        var panel=outcome;outcome=null;panel?.Dispose();
    }
    public void Dispose(){Enter();try{Retire(false);FreeOwned();}finally{busy=false;}}
}
public partial class TerrainOfflineRestartHudLayer : CanvasLayer
{
    private Action? retired;
    public void Bind(Action callback)=>retired=callback;
    public void ReleaseCallback()=>retired=null;
    private void Notify(){var callback=retired;retired=null;callback?.Invoke();}
    public override void _ExitTree()=>Notify();
    protected override void Dispose(bool disposing){if(disposing)Notify();else retired=null;base.Dispose(disposing);}
}
