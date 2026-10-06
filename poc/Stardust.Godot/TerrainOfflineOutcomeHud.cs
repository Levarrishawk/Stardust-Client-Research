using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Godot;

public sealed record TerrainOfflineOutcomeHudState(string Reason,int CompletedUpdates,int MaximumUpdates,
    ulong RootFrames,int QueueCount,int PendingMessages,int RetainedPackets);

/// <summary>Owned noninteractive outcome display over caller-associated immutable state.</summary>
public sealed class TerrainOfflineOutcomeHud : IDisposable
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private Node? owner,parent;
    private Viewport? viewport;
    private Func<TerrainOfflineOutcomeHudState>? source;
    private TerrainOfflineOutcomeHudLayer? layer;
    private Panel? panel;
    private Label? label;
    private bool attempted,configured,busy,reentered,retired;
    private string caption="";
    private int refreshes,retirements;
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Outcome HUD owner/Godot main thread required.");}
    private void Enter(){Owner();if(busy){reentered=true;throw new InvalidOperationException("Outcome HUD reentry.");}busy=true;reentered=false;}
    private static bool Live(Node? node)=>GodotObject.IsInstanceValid(node)&&node!.IsInsideTree()&&!node.IsQueuedForDeletion();
    private void Guard(CancellationToken token,bool display=true)
    {
        token.ThrowIfCancellationRequested();
        if(reentered||retired||!Live(owner)||!Live(parent)||!Live(viewport)||owner!.GetParent()!=parent||owner.GetViewport()!=viewport||parent!.GetViewport()!=viewport||
            display&&(!Live(layer)||layer!.GetParent()!=owner||layer.GetViewport()!=viewport||!Live(panel)||panel!.GetParent()!=layer||panel.GetViewport()!=viewport||panel.MouseFilter!=Control.MouseFilterEnum.Ignore||panel.FocusMode!=Control.FocusModeEnum.None||!Live(label)||label!.GetParent()!=panel||label.GetViewport()!=viewport||label.MouseFilter!=Control.MouseFilterEnum.Ignore||label.FocusMode!=Control.FocusModeEnum.None))
            throw new InvalidOperationException("Exact live outcome HUD owner/parent/viewport/display required.");
    }
    public string Caption {get{Owner();return caption;}}
    public int RefreshCount {get{Owner();return refreshes;}}
    public int RetirementCount {get{Owner();return retirements;}}
    public bool IsConfigured {get{Owner();return configured;}}
    public bool IsRetired {get{Owner();return retired;}}
    private static string Text(TerrainOfflineOutcomeHudState state)
    {
        if(state is null||state.MaximumUpdates is <1 or >6000||state.CompletedUpdates<0||state.CompletedUpdates>state.MaximumUpdates||state.RootFrames>6000||
            state.QueueCount is <0 or >8||state.PendingMessages is <0 or >8||state.RetainedPackets is <0 or >6000)
            throw new InvalidDataException("Bounded outcome HUD state required.");
        string reason=state.Reason switch
        {
            "BudgetCompleted"=>"Budget completed","FinishRequested"=>"Finished with Escape",
            "Faulted"=>"Stopped after an error","Incomplete"=>"Incomplete",
            _=>throw new InvalidDataException("Known primitive outcome reason required.")
        };
        bool empty=state.QueueCount==0&&state.PendingMessages==0&&state.RetainedPackets==0;
        return "Offline preview outcome\nReason: "+reason+"\nUpdates: "+state.CompletedUpdates.ToString(CultureInfo.InvariantCulture)+" / "+state.MaximumUpdates.ToString(CultureInfo.InvariantCulture)+
            "\nOffline root frames: "+state.RootFrames.ToString(CultureInfo.InvariantCulture)+"\nBuffers: "+(empty?"empty":"remaining");
    }
    public void Configure(Node owner,Func<TerrainOfflineOutcomeHudState> source,CancellationToken token=default)
    {
        Enter();try
        {
            if(attempted||retired)throw new InvalidOperationException("Outcome HUD configuration accepted once.");attempted=true;
            ArgumentNullException.ThrowIfNull(owner);ArgumentNullException.ThrowIfNull(source);
            this.owner=owner;parent=owner.GetParent();viewport=owner.GetViewport();this.source=source;Guard(token,false);
            layer=new TerrainOfflineOutcomeHudLayer{Name="OfflineOutcomeHud",Layer=22};layer.Bind(OnNativeRetirement);
            label=new Label{Name="OfflineOutcomeLabel",Position=new(12,8),MouseFilter=Control.MouseFilterEnum.Ignore,FocusMode=Control.FocusModeEnum.None};label.AddThemeFontSizeOverride("font_size",16);
            panel=new Panel{Name="OfflineOutcomePanel",Position=new(16,328),Size=new(608,148),MouseFilter=Control.MouseFilterEnum.Ignore,FocusMode=Control.FocusModeEnum.None};
            panel.AddChild(label);layer.AddChild(panel);owner.AddChild(layer);
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
        Enter();try{if(retired)return false;if(!configured)throw new InvalidOperationException("Configured outcome HUD required.");RefreshCore(token);return true;}
        catch{Retire(true);throw;}finally{busy=false;}
    }
    private void FreeOwned()
    {
        var owned=layer;layer=null;
        if(GodotObject.IsInstanceValid(owned)){owned!.ReleaseCallback();owned.Free();}
    }
    private void Retire(bool free)
    {
        if(!retired){retired=true;retirements++;layer?.ReleaseCallback();panel=null;label=null;source=null;owner=parent=null;viewport=null;}
        if(free)FreeOwned();
    }
    private void OnNativeRetirement(){Owner();if(busy)reentered=true;Retire(false);}
    public void Dispose(){Enter();try{Retire(true);}finally{busy=false;}}
}

// Native lifecycle only retires managed ownership; it never recursively frees a node.
public partial class TerrainOfflineOutcomeHudLayer : CanvasLayer
{
    private Action? retired;
    public void Bind(Action callback)=>retired=callback;
    public void ReleaseCallback()=>retired=null;
    private void Notify(){var callback=retired;retired=null;callback?.Invoke();}
    public override void _ExitTree()=>Notify();
    protected override void Dispose(bool disposing){if(disposing)Notify();else retired=null;base.Dispose(disposing);}
}
