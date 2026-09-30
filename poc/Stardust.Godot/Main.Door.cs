using System;
using System.IO;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

public partial class Main
{
    private SingleLeafDoorRuntime? doorRuntime;
    private async System.Threading.Tasks.Task BuildDoorPreview(string[] args)
    {
        string Read(string key)
        {
            int i=Array.IndexOf(args,key);
            if(i<0 || i+1>=args.Length || args.Count(v=>v==key)!=1)throw new ArgumentException("Missing/duplicate door option.");
            return args[i+1];
        }
        SingleLeafDoorAsset asset;
        using(StreamReader config=File.OpenText(Read("--asset-config")))
        using(ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(Read("--asset-root"),config))
            asset=SingleLeafDoorAssetReader.Load(overlay.Lookup,Read("--door-style"),Read("--door-table-sha256"),Read("--asset-sha256"));
        if(asset.Selection.Style.PrimaryAppearance!=Read("--asset-member"))throw new InvalidDataException("Selected leaf differs from pinned member.");
        geometryPreview=new(asset.Geometry);AddChild(geometryPreview.Root);
        doorRuntime=new(asset.Selection.Style,asset.Extent,AppearanceTransform.Identity,0);
        var camera=new Camera3D{Position=new Vector3(1,3,8),Current=true};AddChild(camera);camera.LookAt(new Vector3(-1,1,0));
        cameraLodUpdate=()=>geometryPreview.SelectFromCamera(camera.GlobalPosition);
        var environment=new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color(.08f,.10f,.14f)}};AddChild(environment);
        var layer=new CanvasLayer();AddChild(layer);
        var panel=new VBoxContainer{Position=new Vector2(18,18)};layer.AddChild(panel);
        panel.AddChild(new Label{Text=$"Original style: {asset.Selection.Style.Name} — single normal leaf"});
        panel.AddChild(new Label{Text="Neutral authored mesh; clearance uses its conservative appearance box."});
        var contact=new CheckBox{Text="Actor retained in trigger",ButtonPressed=true};panel.AddChild(contact);
        var permission=new CheckBox{Text="Entry allowed",ButtonPressed=true};panel.AddChild(permission);
        var step=new Button{Text="Advance 0.1 seconds"};panel.AddChild(step);
        var reset=new Button{Text="Reset closed"};panel.AddChild(reset);
        var status=new Label();panel.AddChild(status);long frame=0;
        void Refresh()
        {
            geometryPreview.Root.Transform=GodotAppearanceTransform.ToGodot(doorRuntime.State.LeafPose);
            cameraLodUpdate();
            var hit=doorRuntime.SweepLeaf(new(0,1,-2),new(0,1,2),new(.25f,.9f,.25f));
            status.Text=$"Opening fraction: {doorRuntime.State.Controller.Fraction:F3}\nLeaf displacement: {doorRuntime.State.DisplacementFraction:F3}\nActor crossing: {hit.Status}\nEffect event: {doorRuntime.State.Effect}";
        }
        step.Pressed+=()=>
        {
            if(contact.ButtonPressed && permission.ButtonPressed)doorRuntime.NotifyContact(1,0,1,new(0,1,0),true,frame);
            doorRuntime.Advance(frame++,.1f,_=>contact.ButtonPressed?System.Numerics.Vector3.Zero:null,permission.ButtonPressed);Refresh();
        };
        reset.Pressed+=()=>{doorRuntime.Reset();frame=0;Refresh();};Refresh();
        if(args.Contains("--asset-self-test") || args.Contains("--capture"))
        {
            int openingSteps=(int)Math.Min(1000,Math.Ceiling(asset.Selection.Style.OpenSeconds/.1)+2),closingSteps=(int)Math.Min(1000,Math.Ceiling(asset.Selection.Style.CloseSeconds/.1)+4);
            for(int i=0;i<openingSteps;i++)step.EmitSignal(Button.SignalName.Pressed);
            if(doorRuntime.State.Controller.Fraction!=1 || doorRuntime.SweepLeaf(new(0,1,-2),new(0,1,2),new(.25f,.9f,.25f)).Status!=PortalBarrierContactStatus.Clear)throw new InvalidDataException("Door open controls failed.");
            permission.ButtonPressed=false;
            for(int i=0;i<closingSteps;i++)step.EmitSignal(Button.SignalName.Pressed);
            if(doorRuntime.State.Controller.Fraction!=0)throw new InvalidDataException("Door permission controls failed.");
            reset.EmitSignal(Button.SignalName.Pressed);permission.ButtonPressed=true;
            for(int i=0;i<openingSteps;i++)step.EmitSignal(Button.SignalName.Pressed);
            if(geometryPreview.Root.Transform!=GodotAppearanceTransform.ToGodot(doorRuntime.State.LeafPose))throw new InvalidDataException("Door adapter pose failed.");
            geometryPreview.VerifyCameraSwitches();Refresh();
            GD.Print($"PASS: door inspector controls, leaf pose and clearance; style={asset.Selection.Style.Name} meshes={geometryPreview.MeshInstances}");
        }
        if(DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        if(args.Contains("--capture") || args.Contains("--asset-self-test"))
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using Image image=GetViewport().GetTexture().GetImage();
            if(args.Contains("--capture") && image.SavePng(Read("--capture"))!=Error.Ok)throw new IOException("Door capture failed.");
            GD.Print("PASS: door inspector rendered through GPU host.");GetTree().Quit();
        }
    }
}
