using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Godot;

public sealed record TerrainOfflinePlaygroundHudState(string Mode,double Speed,int Updates,int MaximumUpdates,
    int RemainingUpdates,bool TargetActive,bool Offline);

/// <summary>Owned noninteractive offline status display over a caller-associated immutable state source.</summary>
public sealed class TerrainOfflinePlaygroundHud : IDisposable
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private Node? owner,parent;
    private Viewport? viewport;
    private Func<TerrainOfflinePlaygroundHudState>? source;
    private CanvasLayer? layer;
    private Panel? panel;
    private Label? title,status,controls;
    private StyleBoxFlat? style;
    private bool configured,attempted,busy,reentered,retired;
    private string caption="";
    private int refreshes,retirements;
    private const string Title="Offline preview\nModern supplied movement";
    private const string Controls="WASD move  |  Shift run\nRight-drag orbit  |  Wheel zoom  |  Left-click target";
    private bool interactiveControls;
    private string TitleText=>interactiveControls?"Offline preview\nMovement controls":Title;
    private string ControlText=>Controls+(interactiveControls?"\nP pause  |  Escape finish":"");
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("HUD owner/Godot main thread required.");}
    private void Enter(){Owner();if(busy){reentered=true;throw new InvalidOperationException("HUD reentry.");}busy=true;reentered=false;}
    private static bool Live(Node? node)=>GodotObject.IsInstanceValid(node)&&node!.IsInsideTree()&&!node.IsQueuedForDeletion();
    private void Guard(CancellationToken cancellation,bool root=true)
    {
        cancellation.ThrowIfCancellationRequested();
        if(reentered||retired||!Live(owner)||!Live(parent)||owner!.GetParent()!=parent||
            !Live(viewport)||owner.GetViewport()!=viewport||parent!.GetViewport()!=viewport||
            root&&(!Live(layer)||layer!.GetParent()!=owner||layer.GetViewport()!=viewport||!Live(panel)||panel!.GetParent()!=layer||
                !Live(title)||title!.GetParent()!=panel||!Live(status)||status!.GetParent()!=panel||!Live(controls)||controls!.GetParent()!=panel))
            throw new InvalidOperationException("Exact live HUD owner/parent/viewport and owned display required.");
    }
    public string Caption {get{Owner();return caption;}}
    public int RefreshCount {get{Owner();return refreshes;}}
    public int RetirementCount {get{Owner();return retirements;}}
    public bool IsConfigured {get{Owner();return configured;}}
    public bool IsRetired {get{Owner();return retired;}}
    private static string Text(TerrainOfflinePlaygroundHudState state)
    {
        if(state is null||state.Mode is not ("Idle" or "Moving" or "Arrived" or "Paused" or "Manual")||
            !double.IsFinite(state.Speed)||state.Speed<0||state.Speed>10||state.MaximumUpdates is <1 or >6000||
            state.Updates<0||state.Updates>state.MaximumUpdates||state.RemainingUpdates!=state.MaximumUpdates-state.Updates||!state.Offline)
            throw new InvalidDataException("Bounded offline HUD state required.");
        return state.Mode+"  |  Speed "+state.Speed.ToString("0.00",CultureInfo.InvariantCulture)+"\nPreview time remaining: "+
            (state.RemainingUpdates*.01).ToString("0.0",CultureInfo.InvariantCulture)+"s\nTarget: "+(state.TargetActive?"active":"inactive");
    }
    private Label AddLabel(Panel panel,string name,string text,Vector2 position,int fontSize)
    {
        var label=new Label{Name=name,Text=text,Position=position,MouseFilter=Control.MouseFilterEnum.Ignore,FocusMode=Control.FocusModeEnum.None};
        label.AddThemeFontSizeOverride("font_size",fontSize);panel.AddChild(label);return label;
    }
    public void Configure(Node owner,Func<TerrainOfflinePlaygroundHudState> source,CancellationToken cancellation=default)
        => Configure(owner,source,cancellation,false);
    public void Configure(Node owner,Func<TerrainOfflinePlaygroundHudState> source,CancellationToken cancellation,bool interactiveControls)
    {
        Enter();try
        {
            if(attempted||retired)throw new InvalidOperationException("HUD configuration accepted once.");
            attempted=true;ArgumentNullException.ThrowIfNull(owner);ArgumentNullException.ThrowIfNull(source);
            this.interactiveControls=interactiveControls;
            this.owner=owner;parent=owner.GetParent();viewport=owner.GetViewport();this.source=source;Guard(cancellation,false);
            layer=new CanvasLayer{Name="OfflinePlaygroundHud",Layer=20};
            panel=new Panel{Name="OfflinePlaygroundPanel",Position=new(16,16),Size=new(608,interactiveControls?260:230),MouseFilter=Control.MouseFilterEnum.Ignore,FocusMode=Control.FocusModeEnum.None};
            style=new StyleBoxFlat{BgColor=new Color(.045f,.075f,.12f,.94f),BorderColor=new Color(.2f,.4f,.55f),BorderWidthLeft=1,BorderWidthTop=1,BorderWidthRight=1,BorderWidthBottom=1,CornerRadiusTopLeft=8,CornerRadiusTopRight=8,CornerRadiusBottomLeft=8,CornerRadiusBottomRight=8};
            panel.AddThemeStyleboxOverride("panel",style);layer.AddChild(panel);
            title=AddLabel(panel,"OfflinePlaygroundTitle",TitleText,new(12,8),20);
            status=AddLabel(panel,"OfflinePlaygroundStatus","",new(12,72),16);
            controls=AddLabel(panel,"OfflinePlaygroundControls",ControlText,new(12,160),16);
            owner.AddChild(layer);Guard(cancellation);RefreshCore(cancellation);configured=true;
        }catch{Retire();throw;}finally{busy=false;}
    }
    private void RefreshCore(CancellationToken cancellation)
    {
        Guard(cancellation);var state=source!();Guard(cancellation);string text=Text(state);
        int next=checked(refreshes+1);status!.Text=text;Guard(cancellation);
        caption=TitleText+"\n"+text+"\n"+ControlText;refreshes=next;
    }
    public bool Refresh(CancellationToken cancellation=default)
    {
        Enter();try{if(retired)return false;if(!configured)throw new InvalidOperationException("Configured HUD required.");RefreshCore(cancellation);return true;}
        catch{Retire();throw;}finally{busy=false;}
    }
    private void Retire()
    {
        if(retired)return;retired=true;retirements++;
        try{if(GodotObject.IsInstanceValid(layer))layer!.Free();}
        finally{style?.Dispose();style=null;layer=null;panel=null;title=null;status=null;controls=null;source=null;owner=null;parent=null;viewport=null;}
    }
    public void Dispose(){Enter();try{Retire();}finally{busy=false;}}
}
