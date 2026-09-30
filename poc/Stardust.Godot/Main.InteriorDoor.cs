using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

public partial class Main
{
    private readonly List<Resource> interiorDoorResources=new();
    private void DisposeInteriorDoorResources(){foreach(Resource resource in interiorDoorResources.AsEnumerable().Reverse())resource.Dispose();interiorDoorResources.Clear();}
    private async System.Threading.Tasks.Task BuildInteriorDoorPreview(string[] args)
    {
        string Read(string key)
        {
            int i=Array.IndexOf(args,key);
            if(i<0 || i+1>=args.Length || args.Count(v=>v==key)!=1)throw new ArgumentException("Missing/duplicate interior door option.");
            return args[i+1];
        }
        PortalSingleLeafDoorBundle bundle;
        using(StreamReader config=File.OpenText(Read("--asset-config")))
        using(ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(Read("--asset-root"),config))
            bundle=PortalSingleLeafDoorBundleReader.Load(overlay.Lookup,Read("--asset-member"),Read("--asset-sha256"),int.Parse(Read("--door-cell")),int.Parse(Read("--door-portal")),Read("--door-table-sha256"),Read("--door-leaf-sha256"));
        if(bundle.Leaf.Selection.Style.Name!=Read("--door-style"))throw new InvalidDataException("Door reference style differs from inspector selection.");
        geometryPreview=new(bundle.Leaf.Geometry);AddChild(geometryPreview.Root);
        floorPreview=new(bundle.Floors);AddChild(floorPreview.Root);
        var floorFrame=new Node3D();AddChild(floorFrame);RemoveChild(floorPreview.Root);floorFrame.AddChild(floorPreview.Root);
        var actorMesh=new BoxMesh{Size=new(.5f,1.8f,.5f)};
        var actorMaterial=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new Color(.95f,.65f,.18f,.45f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha};actorMesh.Material=actorMaterial;
        interiorDoorResources.Add(actorMaterial);interiorDoorResources.Add(actorMesh);
        var actor=new MeshInstance3D{Mesh=actorMesh};AddChild(actor);
        var camera=new Camera3D{Current=true};AddChild(camera);
        AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color(.08f,.10f,.14f)}});
        var layer=new CanvasLayer();AddChild(layer);var panel=new VBoxContainer{Position=new Vector2(18,18)};layer.AddChild(panel);
        panel.AddChild(new Label{Text=$"Original doorway: {bundle.Layout.Cells[bundle.Cell].Name} → {bundle.Layout.Cells[bundle.TargetCell].Name} / {bundle.Leaf.Selection.Style.Name}"});
        panel.AddChild(new Label{Text="Actor .5 × 1.8 × .5; conservative leaf box; one portal transfer."});
        var contact=new CheckBox{Text="Actor retained near doorway",ButtonPressed=true};panel.AddChild(contact);
        var permission=new CheckBox{Text="Entry allowed",ButtonPressed=true};panel.AddChild(permission);
        var transformed=new CheckBox{Text="Rotate and translate building owner"};panel.AddChild(transformed);
        var step=new Button{Text="Advance door 0.125 seconds"};panel.AddChild(step);
        var cross=new Button{Text="Test crossing from source cell"};panel.AddChild(cross);
        var reset=new Button{Text="Reset closed"};panel.AddChild(reset);
        var status=new Label();panel.AddChild(status);
        var approach=bundle.InspectionApproach();AppearanceTransform owner=AppearanceTransform.Identity;long frame=0;
        PortalBarrierFloorWalker? walker=null;PortalBarrierFloorResult? result=null;
        System.Numerics.Vector3 WorldStart()=>owner.TransformPoint(approach.Start.LocalPosition);
        void Refresh()
        {
            geometryPreview.Root.Transform=GodotAppearanceTransform.ToGodot(doorRuntime!.State.LeafPose);
            var foot=result?.Movement.WorldPosition??WorldStart();actor.Position=new(foot.X,foot.Y+.9f,-foot.Z);
            status.Text=$"Fraction: {doorRuntime.State.Controller.Fraction:F3} / displacement: {doorRuntime.State.DisplacementFraction:F3}\nMovement: {result?.Movement.Status.ToString()??"not queried"}\nOwner: {result?.Movement.Locator.CellIndex??bundle.Cell}; transfers: {result?.Movement.Transfers??0}\nContact: {result?.ContactKind?.ToString()??"none"}";
        }
        void ConfigureOwner(bool changed)
        {
            doorRuntime?.Dispose();owner=changed?new(new(0,0,1),System.Numerics.Vector3.UnitY,new(-1,0,0),new(30,4,-11)):AppearanceTransform.Identity;
            doorRuntime=bundle.CreateRuntime(owner);frame=0;result=null;
            walker=new(bundle.Layout,bundle.Floors,bundle.Floors.Keys.ToDictionary(c=>c,_=>owner),new Dictionary<(int,int),SingleLeafDoorRuntime>{[(bundle.Cell,bundle.Portal)]=doorRuntime});
            floorFrame.Transform=GodotAppearanceTransform.ToGodot(owner);
            var p=owner.TransformPoint(bundle.DoorToCell.Translation);var n=owner.TransformDirection(bundle.DoorToCell.TransformDirection(System.Numerics.Vector3.UnitZ));
            var view=p+n*7+System.Numerics.Vector3.UnitY*4;camera.Position=new(view.X,view.Y,-view.Z);camera.LookAt(new Vector3(p.X,p.Y+1,-p.Z));Refresh();
        }
        step.Pressed+=()=>
        {
            if(contact.ButtonPressed && permission.ButtonPressed)doorRuntime!.NotifyContact(1,bundle.Cell,bundle.TargetCell,WorldStart()+System.Numerics.Vector3.UnitY*.9f,true,frame);
            doorRuntime!.Advance(frame++,.125f,_=>contact.ButtonPressed?WorldStart():null,permission.ButtonPressed);result=null;Refresh();
        };
        cross.Pressed+=()=>{result=walker!.Walk(approach.Start,owner.TransformPoint(approach.End),new(.25f,.9f,.25f),_=>new(permission.ButtonPressed,true));floorPreview.ShowActorWalk(WorldStart(),result);Refresh();};
        reset.Pressed+=()=>{doorRuntime!.Reset();frame=0;result=null;floorPreview.SelectCell(-1);Refresh();};
        transformed.Toggled+=ConfigureOwner;ConfigureOwner(false);
        if(args.Contains("--asset-self-test") || args.Contains("--capture"))
        {
            int probes=0;
            foreach(bool change in new[]{false,true})
            {
                ConfigureOwner(change);permission.ButtonPressed=true;contact.ButtonPressed=true;
                void Check(CellFloorWalkStatus expected, int expectedOwner, int count)
                {
                    cross.EmitSignal(Button.SignalName.Pressed);probes++;
                    if(result!.Movement.Status!=expected || result.Movement.Locator.CellIndex!=expectedOwner || result.Movement.Transfers!=count)throw new InvalidDataException("Interior door control/owner mismatch.");
                    var foot=result.Movement.WorldPosition;
                    if(actor.Position.DistanceTo(new Vector3(foot.X,foot.Y+.9f,-foot.Z))>1e-5f || geometryPreview.Root.Transform!=GodotAppearanceTransform.ToGodot(doorRuntime!.State.LeafPose) || floorFrame.Transform!=GodotAppearanceTransform.ToGodot(owner))throw new InvalidDataException("Interior door reflection mismatch.");
                }
                Check(CellFloorWalkStatus.PortalBlocked,bundle.Cell,0);
                step.EmitSignal(Button.SignalName.Pressed);Check(CellFloorWalkStatus.PortalBlocked,bundle.Cell,0);
                for(int i=0;i<3;i++)step.EmitSignal(Button.SignalName.Pressed);Check(CellFloorWalkStatus.Accepted,bundle.TargetCell,1);
                permission.ButtonPressed=false;Check(CellFloorWalkStatus.EntryDenied,bundle.Cell,0);
                for(int i=0;i<7;i++)step.EmitSignal(Button.SignalName.Pressed);
                if(doorRuntime!.State.Controller.Fraction!=0)throw new InvalidDataException("Revoked entry did not close integrated leaf.");
                permission.ButtonPressed=true;reset.EmitSignal(Button.SignalName.Pressed);Check(CellFloorWalkStatus.PortalBlocked,bundle.Cell,0);
                for(int i=0;i<4;i++)step.EmitSignal(Button.SignalName.Pressed);Check(CellFloorWalkStatus.Accepted,bundle.TargetCell,1);
            }
            transformed.ButtonPressed=true;
            // The checkbox applies a fresh owner configuration; restore an open crossing for the capture.
            for(int i=0;i<4;i++)step.EmitSignal(Button.SignalName.Pressed);cross.EmitSignal(Button.SignalName.Pressed);
            GD.Print($"PASS: original interior door controls and reflected leaf/floor/actor ownership; probes={probes} source={bundle.Cell} target={bundle.TargetCell}");
        }
        if(DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        if(args.Contains("--capture") || args.Contains("--asset-self-test"))
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using Image image=GetViewport().GetTexture().GetImage();if(args.Contains("--capture") && image.SavePng(Read("--capture"))!=Error.Ok)throw new IOException("Interior door capture failed.");
            GD.Print("PASS: original interior door GPU rendering.");GetTree().Quit();
        }
    }
}
