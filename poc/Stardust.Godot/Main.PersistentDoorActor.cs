using System;
using System.IO;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

public partial class Main
{
    private PortalDoorActorSimulation? actorSimulation;
    private FixedStepPortalActorHost? actorHost;
    private PortalActorInputTimeline? actorTimeline;
    private Action<InputEventKey>? actorKeyInput;
    private Action<double>? actorUpdate;
    private Action<bool>? actorFocus;
    private async System.Threading.Tasks.Task BuildPersistentDoorActorPreview(PortalSingleLeafDoorBundle bundle,string[] args,Func<string,string> read)
    {
        bool preciseMovement=args.Contains("--precise-door-walk");
        bool stepMovement=args.Contains("--step-door-walk");
        bool localStepMovement=args.Contains("--local-step-door-walk");
        bool connectedApproach=args.Contains("--connected-door-approach");
        if(stepMovement&&!preciseMovement)throw new AssetPreviewException("Step movement requires precise movement.");
        if(localStepMovement&&!stepMovement)throw new AssetPreviewException("Local step coverage requires step movement.");
        if(connectedApproach&&!localStepMovement)throw new AssetPreviewException("Connected approach preview requires local step coverage.");
        geometryPreview=new(bundle.Leaf.Geometry);AddChild(geometryPreview.Root);
        floorPreview=new(bundle.Floors);var floorFrame=new Node3D();AddChild(floorFrame);floorFrame.AddChild(floorPreview.Root);
        var mesh=new BoxMesh{Size=new(.5f,1.8f,.5f)};
        var material=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new Color(.95f,.65f,.18f,.55f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha};mesh.Material=material;
        interiorDoorResources.Add(material);interiorDoorResources.Add(mesh);var actor=new MeshInstance3D{Mesh=mesh};AddChild(actor);
        var camera=new Camera3D{Current=true};AddChild(camera);
        cameraLodUpdate=()=>geometryPreview.SelectFromCamera(camera.GlobalPosition);
        AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color(.08f,.10f,.14f)}});
        var layer=new CanvasLayer();AddChild(layer);var panelFrame=new PanelContainer{Position=new Vector2(18,18)};layer.AddChild(panelFrame);
        var panelStyle=new StyleBoxFlat{BgColor=new Color(.02f,.03f,.06f,.94f),ContentMarginLeft=12,ContentMarginRight=12,ContentMarginTop=12,ContentMarginBottom=12};interiorDoorResources.Add(panelStyle);panelFrame.AddThemeStyleboxOverride("panel",panelStyle);
        var panel=new VBoxContainer();panelFrame.AddChild(panel);
        panel.AddChild(new Label{Text=$"Persistent actor: {bundle.Layout.Cells[bundle.Cell].Name} ↔ {bundle.Layout.Cells[bundle.TargetCell].Name}"});
        panel.AddChild(new Label{Text="Actor .5 × 1.8 × .5 • conservative leaf clearance"});
        panel.AddChild(new Label{Text=stepMovement?"Sloped floor movement with bounded steps":preciseMovement?"Sloped floor movement":"Level floor movement"});
        var permission=new CheckBox{Text="Entry allowed",ButtonPressed=true};panel.AddChild(permission);
        var transformed=new CheckBox{Text="Rotate and translate building owner"};panel.AddChild(transformed);
        bool fixture=args.Contains("--asset-self-test") || args.Contains("--capture");
        int manualStepEvents=0;
        var automatic=new CheckBox{Text="Continuous movement (W / A / S / D)",ButtonPressed=!fixture};panel.AddChild(automatic);
        panel.AddChild(new Label{Text=$"W: {bundle.Layout.Cells[bundle.TargetCell].Name} • S: {bundle.Layout.Cells[bundle.Cell].Name}"});
        panel.AddChild(new Label{Text="A / D: doorway left / right • normalized diagonals"});
        var paused=new CheckBox{Text="Pause simulation"};panel.AddChild(paused);
        var forward=new Button{Text="Step toward target 0.08m"};panel.AddChild(forward);
        var backward=new Button{Text="Step toward source 0.08m"};panel.AddChild(backward);
        var wait=new Button{Text="Wait 0.1 seconds"};panel.AddChild(wait);
        var reset=new Button{Text="Reset actor and closed door"};panel.AddChild(reset);
        var status=new Label();panel.AddChild(status);
        var approach=connectedApproach?bundle.ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true)):bundle.InspectionApproach();var direction=approach.End-approach.Start.LocalPosition;direction.Y=0;direction=System.Numerics.Vector3.Normalize(direction);
        float KeyAxis()=>(Input.IsPhysicalKeyPressed(Key.W)?1:0)-(Input.IsPhysicalKeyPressed(Key.S)?1:0);
        AppearanceTransform owner=AppearanceTransform.Identity;
        bool wHeld=false,sHeld=false,aHeld=false,dHeld=false;
        PortalActorInput CurrentInput()=>PlanarActorInputMapping.Create(owner.TransformDirection(direction),(wHeld?1:0)-(sHeld?1:0),(dHeld?1:0)-(aHeld?1:0),1.6f,permission.ButtonPressed);
        void SubmitInput(long now)=>actorTimeline!.Submit(new(now,CurrentInput()));
        void Refresh()
        {
            var state=actorSimulation!.State;geometryPreview.Root.Transform=GodotAppearanceTransform.ToGodot(state.Door.LeafPose);
            cameraLodUpdate();
            var p=state.WorldPosition;actor.Position=new(p.X,p.Y+.9f,-p.Z);
            status.Text=$"Owner: {state.Locator.CellIndex} / {bundle.Layout.Cells[state.Locator.CellIndex].Name}\nTransfers: {state.TotalTransfers}; ticks: {state.Frame}\nDoor fraction: {state.Door.Controller.Fraction:F3}; retained actors: {state.Door.RetainedActors}\nMovement: {state.Movement?.Movement.Status.ToString()??"not moved"}\nContact: {state.Movement?.ContactKind?.ToString()??"none"}\nCatch-up time dropped: {actorHost?.State.DroppedMicroseconds/1000.0:F1} ms\n{(actorHost?.State.FaultType is not null?"Simulation stopped; reset required":"")}";
        }
        void Configure(bool change)
        {
            actorSimulation?.Dispose();owner=change?new(new(0,0,1),System.Numerics.Vector3.UnitY,new(-1,0,0),new(30,4,-11)):AppearanceTransform.Identity;
            actorSimulation=new(bundle,approach.Start,owner,usePreciseMovement:preciseMovement,stepPolicy:stepMovement?new(UseLocalRayCoverage:localStepMovement):null);actorHost=new(actorSimulation);wHeld=sHeld=aHeld=dHeld=false;long now=checked((long)Time.GetTicksUsec());actorTimeline=new(actorHost,now,CurrentInput());actorTimeline.AdvanceTo(now,automatic.ButtonPressed&&!paused.ButtonPressed&&GetWindow().HasFocus());floorFrame.Transform=GodotAppearanceTransform.ToGodot(owner);floorPreview.SelectCell(-1);
            var p=owner.TransformPoint(bundle.DoorToCell.Translation);var n=owner.TransformDirection(bundle.DoorToCell.TransformDirection(System.Numerics.Vector3.UnitZ));var view=p+n*7+System.Numerics.Vector3.UnitY*4;
            camera.Position=new(view.X,view.Y,-view.Z);camera.LookAt(new Vector3(p.X,p.Y+1,-p.Z));Refresh();
        }
        void Tick(float seconds,float meters)
        {
            var from=actorSimulation!.State.WorldPosition;var state=actorSimulation.Tick(seconds,owner.TransformDirection(direction*meters),permission.ButtonPressed);
            if(fixture)manualStepEvents+=state.Movement?.Steps??0;
            floorPreview.ShowActorWalk(from,state.Movement!);Refresh();
        }
        forward.Pressed+=()=>Tick(.05f,.08f);backward.Pressed+=()=>Tick(.05f,-.08f);wait.Pressed+=()=>Tick(.1f,0);
        reset.Pressed+=()=>{wHeld=sHeld=aHeld=dHeld=false;long now=checked((long)Time.GetTicksUsec());actorTimeline!.Reset(now,CurrentInput());actorTimeline.AdvanceTo(now,automatic.ButtonPressed&&!paused.ButtonPressed&&GetWindow().HasFocus());floorPreview.SelectCell(-1);Refresh();};transformed.Toggled+=Configure;Configure(false);
        void ModeBoundary(bool focused)
        {
            bool active=automatic.ButtonPressed&&!paused.ButtonPressed&&focused;
            wHeld=active&&Input.IsPhysicalKeyPressed(Key.W);sHeld=active&&Input.IsPhysicalKeyPressed(Key.S);
            aHeld=active&&Input.IsPhysicalKeyPressed(Key.A);dHeld=active&&Input.IsPhysicalKeyPressed(Key.D);
            long now=checked((long)Time.GetTicksUsec());SubmitInput(now);actorTimeline!.AdvanceTo(now,active);
            forward.Disabled=backward.Disabled=wait.Disabled=automatic.ButtonPressed;Refresh();
        }
        automatic.Toggled+=_=>ModeBoundary(GetWindow().HasFocus());paused.Toggled+=_=>ModeBoundary(GetWindow().HasFocus());actorFocus=ModeBoundary;ModeBoundary(GetWindow().HasFocus());
        permission.Toggled+=_=>SubmitInput(checked((long)Time.GetTicksUsec()));
        actorKeyInput=e=>
        {
            if(e.Echo || e.PhysicalKeycode is not (Key.W or Key.S or Key.A or Key.D))return;
            switch(e.PhysicalKeycode){case Key.W:wHeld=e.Pressed;break;case Key.S:sHeld=e.Pressed;break;case Key.A:aHeld=e.Pressed;break;case Key.D:dHeld=e.Pressed;break;}
            SubmitInput(checked((long)Time.GetTicksUsec()));
        };
        actorUpdate=_=>
        {
            bool active=automatic.ButtonPressed&&!paused.ButtonPressed&&GetWindow().HasFocus();
            bool w=active&&Input.IsPhysicalKeyPressed(Key.W),s=active&&Input.IsPhysicalKeyPressed(Key.S);
            bool a=active&&Input.IsPhysicalKeyPressed(Key.A),d=active&&Input.IsPhysicalKeyPressed(Key.D);
            long now=checked((long)Time.GetTicksUsec());if(w!=wHeld || s!=sHeld || a!=aHeld || d!=dHeld){wHeld=w;sHeld=s;aHeld=a;dHeld=d;SubmitInput(now);}
            var from=actorSimulation!.State.WorldPosition;
            var clock=actorTimeline!.AdvanceTo(now,active);
            if(clock.StepsThisUpdate>0)floorPreview.ShowActorWalk(from,clock.Actor.Movement!);
            if(clock.FaultType is not null)automatic.ButtonPressed=false;Refresh();
        };
        if(args.Contains("--asset-self-test") || args.Contains("--capture"))
        {
            int checkedTransfers=0;
            foreach(bool change in new[]{false,true})
            {
                Configure(change);permission.ButtonPressed=true;
                for(int trip=0;trip<5;trip++)
                {
                    for(int i=0;i<20;i++)forward.EmitSignal(Button.SignalName.Pressed);
                    if(actorSimulation!.State.Locator.CellIndex!=bundle.TargetCell)throw new AssetPreviewException("Persistent target ownership failed.");
                    checkedTransfers++;
                    for(int i=0;i<20;i++)backward.EmitSignal(Button.SignalName.Pressed);
                    if(actorSimulation.State.Locator.CellIndex!=bundle.Cell)throw new AssetPreviewException("Persistent reverse ownership failed.");
                    checkedTransfers++;
                }
                if(actorSimulation!.State.TotalTransfers!=10 || actorSimulation.State.Door.RetainedActors!=1)throw new AssetPreviewException("Shared leaf transfer/contact accounting failed.");
                reset.EmitSignal(Button.SignalName.Pressed);permission.ButtonPressed=false;
                for(int i=0;i<12;i++)forward.EmitSignal(Button.SignalName.Pressed);
                var blocked=actorSimulation.State.WorldPosition;backward.EmitSignal(Button.SignalName.Pressed);
                if(actorSimulation.State.TotalTransfers!=0 || System.Numerics.Vector3.Distance(blocked,actorSimulation.State.WorldPosition)<.07f)throw new AssetPreviewException("Persistent denied retreat failed.");
                permission.ButtonPressed=true;reset.EmitSignal(Button.SignalName.Pressed);
                for(int i=0;i<20;i++)forward.EmitSignal(Button.SignalName.Pressed);
                var state=actorSimulation.State;var foot=state.WorldPosition;
                if(actor.Position.DistanceTo(new Vector3(foot.X,foot.Y+.9f,-foot.Z))>1e-5f || geometryPreview.Root.Transform!=GodotAppearanceTransform.ToGodot(state.Door.LeafPose) || floorFrame.Transform!=GodotAppearanceTransform.ToGodot(owner))throw new AssetPreviewException("Persistent reflected placement failed.");
            }
            transformed.ButtonPressed=true;
            for(int i=0;i<20;i++)forward.EmitSignal(Button.SignalName.Pressed);
            GD.Print($"PASS: persistent original actor controls; repeated_transfers={checkedTransfers} manual_step_events={manualStepEvents} owner_poses=2 retreat/reset/contact/reflection verified");
            actorHost!.Reset(0);var input=new PortalActorInput(owner.TransformDirection(direction*1.6f),true);
            for(long t=10_000;t<=1_200_000;t+=10_000)actorHost.AdvanceTo(t,input);
            if(actorSimulation!.State.Locator.CellIndex!=bundle.TargetCell || actorHost.State.Actor.Frame!=60 || actorHost.State.FaultType is not null)throw new AssetPreviewException("Continuous fixed-step adapter failed.");
            var held=actorSimulation.State;actorHost.AdvanceTo(1_210_000,input,false);actorHost.AdvanceTo(2_210_000,input,false);actorHost.AdvanceTo(2_230_000,input,true);
            if(actorSimulation.State!=held || actorHost.State.PendingMicroseconds!=0)throw new AssetPreviewException("Pause/focus adapter advanced suspended actor.");
            actorHost.AdvanceTo(2_250_000,input);if(actorSimulation.State.Frame!=61)throw new AssetPreviewException("Resume fresh-step adapter failed.");
            actorHost.AdvanceTo(4_250_007,new(System.Numerics.Vector3.Zero,true));
            if(actorHost.State.StepsThisUpdate!=8 || actorHost.State.DroppedMicroseconds!=1_840_000 || actorHost.State.PendingMicroseconds!=7)throw new AssetPreviewException("Stall adapter accounting failed.");
            Configure(false);automatic.ButtonPressed=true;paused.ButtonPressed=false;
            _Notification((int)NotificationWMWindowFocusOut);if(actorHost.State.Active || actorHost.State.PendingMicroseconds!=0)throw new AssetPreviewException("Focus-loss control failed.");
            _Notification((int)NotificationWMWindowFocusIn);if(!actorHost.State.Active)throw new AssetPreviewException("Focus-resume control failed.");
            paused.ButtonPressed=true;if(actorHost.State.Active || !forward.Disabled)throw new AssetPreviewException("Pause/control ownership failed.");
            automatic.ButtonPressed=false;paused.ButtonPressed=false;if(forward.Disabled || backward.Disabled || wait.Disabled)throw new AssetPreviewException("Manual control ownership failed.");
            void KeyEvent(Key key,bool pressed){using var e=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=pressed};Input.ParseInputEvent(e);Input.FlushBufferedEvents();}
            try
            {
                int queuedBefore=actorTimeline!.PendingTransitions;
                KeyEvent(Key.W,true);if(KeyAxis()!=1)throw new AssetPreviewException("Physical forward input failed.");
                if(!wHeld || actorTimeline.PendingTransitions!=queuedBefore+1)throw new AssetPreviewException("Timestamped forward event route failed.");
                KeyEvent(Key.S,true);if(KeyAxis()!=0)throw new AssetPreviewException("Opposing physical input cancellation failed.");
                KeyEvent(Key.W,false);if(KeyAxis()!=-1)throw new AssetPreviewException("Physical reverse input failed.");
                KeyEvent(Key.S,false);if(KeyAxis()!=0)throw new AssetPreviewException("Physical input release failed.");
                if(wHeld || sHeld || actorTimeline.PendingTransitions!=queuedBefore+4)throw new AssetPreviewException("Timestamped input release route failed.");
                KeyEvent(Key.D,true);KeyEvent(Key.W,true);
                if(!dHeld || !wHeld || Math.Abs(CurrentInput().WorldVelocity.Length()-1.6f)>1e-6)throw new AssetPreviewException("Planar diagonal keyboard mapping failed.");
                KeyEvent(Key.A,true);if(Math.Abs(System.Numerics.Vector3.Dot(CurrentInput().WorldVelocity,owner.TransformDirection(System.Numerics.Vector3.Cross(System.Numerics.Vector3.UnitY,direction))))>1e-6)throw new AssetPreviewException("Opposing lateral keys failed.");
                KeyEvent(Key.D,false);KeyEvent(Key.A,false);KeyEvent(Key.W,false);
                if(CurrentInput().WorldVelocity!=System.Numerics.Vector3.Zero || actorTimeline.PendingTransitions!=queuedBefore+10)throw new AssetPreviewException("Planar key release route failed.");
                actorTimeline.AdvanceTo(checked((long)Time.GetTicksUsec()),false);
                if(actorTimeline.CurrentInput.WorldVelocity!=System.Numerics.Vector3.Zero || actorTimeline.PendingTransitions!=0)throw new AssetPreviewException("Suspended input timeline consumption failed.");
            }
            finally{KeyEvent(Key.W,false);KeyEvent(Key.S,false);KeyEvent(Key.A,false);KeyEvent(Key.D,false);}
            Configure(false);actorTimeline!.Reset(0,new(System.Numerics.Vector3.Zero,true));
            actorTimeline.Submit(new(137_000,new(owner.TransformDirection(direction*1.6f),true)));
            for(long t=50_000;t<=1_200_000;t+=50_000)actorTimeline.AdvanceTo(t);
            if(actorTimeline.State.Actor.Frame!=60 || actorTimeline.State.Actor.Locator.CellIndex!=bundle.TargetCell || actorTimeline.PendingTransitions!=0)throw new AssetPreviewException("Between-render original timeline adapter failed.");
            foreach(bool change in new[]{false,true})
            {
                Configure(change);actorTimeline!.Reset(0,new(System.Numerics.Vector3.Zero,true));long now=0;
                for(int i=0;i<30;i++)actorTimeline.AdvanceTo(now+=20_000);
                foreach(var axes in new[]{(F:1f,R:1f),(F:1f,R:-1f),(F:-1f,R:-1f),(F:-1f,R:1f)})
                {
                    actorTimeline.Submit(new(now,PlanarActorInputMapping.Create(owner.TransformDirection(direction),axes.F,axes.R,1.6f,true)));
                    for(int i=0;i<20;i++)actorTimeline.AdvanceTo(now+=20_000);
                }
                if(actorTimeline.State.Actor.TotalTransfers!=2 || actorTimeline.State.Actor.Locator.CellIndex!=bundle.Cell || System.Numerics.Vector3.Distance(actorTimeline.State.Actor.WorldPosition,owner.TransformPoint(approach.Start.LocalPosition))>1e-4)throw new AssetPreviewException("Planar keyboard square and reverse ownership failed.");
                Refresh();var foot=actorTimeline.State.Actor.WorldPosition;if(actor.Position.DistanceTo(new Vector3(foot.X,foot.Y+.9f,-foot.Z))>1e-5)throw new AssetPreviewException("Planar reflected actor placement failed.");
            }
            Configure(true);for(int i=0;i<20;i++)forward.EmitSignal(Button.SignalName.Pressed);
            GD.Print("PASS: continuous actor fixed-step adapter; pause/focus notifications, bounded stall, planar physical key cancellation/release, timestamped event route and diagonal owner round trip verified");
        }
        if(DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        if(args.Contains("--capture") || args.Contains("--asset-self-test"))
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using Image image=GetViewport().GetTexture().GetImage();if(args.Contains("--capture") && image.SavePng(read("--capture"))!=Error.Ok)throw new IOException("Persistent actor capture failed.");
            GD.Print("PASS: persistent original actor GPU rendering.");GetTree().Quit();
        }
    }
}
