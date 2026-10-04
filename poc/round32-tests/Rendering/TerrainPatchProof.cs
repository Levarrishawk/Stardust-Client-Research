using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Geometry;
using NVector3 = System.Numerics.Vector3;

public partial class TerrainPatchProof : Node
{
    private readonly List<TerrainPatchVisual> visuals = new();
    public override async void _Ready()
    {
        try { await Run(); GetTree().Quit(); }
        catch(Exception e) { GD.PushError("Terrain proof: "+e); Cleanup(); GetTree().Quit(1); }
    }
    private static void Need(bool v,string why){if(!v)throw new InvalidOperationException(why);}
    private static TerrainPatchGeometry Cell(int pattern,int[]? selectors=null,bool flat=false,float scale=1)
    {
        (float U,float V)[] uv=[(.5f,.5f),(0,.5f),(0,0),(.5f,0),(1,0),(1,.5f),(1,1),(.5f,1),(0,1)];
        return new(uv.Select(v=>new NVector3(2*v.U-1,flat?0:.15f*v.U+.1f*v.V,1-2*v.V)).ToArray(),
            Enumerable.Repeat(new NVector3(0,1,0),9).ToArray(),Enumerable.Repeat(0xFFFF0000u,9).ToArray(),scale,pattern,selectors??[]);
    }
    private async Task Run()
    {
        bool gpu=DisplayServer.GetName()!="headless";var args=OS.GetCmdlineUserArgs();
        string? capture=null;int at=Array.IndexOf(args,"--capture");if(at>=0)capture=Path.GetFullPath(args[at+1]);
        if(capture is not null)
        {
            string prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","..","..","local-private"))+Path.DirectorySeparatorChar;
            Need(gpu&&capture.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&!File.Exists(capture),"Fresh private GPU capture");
        }
        var holder=new Node3D();AddChild(holder);
        for(int pattern=0;pattern<9;pattern++)
        {
            var visual=new TerrainPatchVisual(Cell(pattern,[pattern%4]),holder);visuals.Add(visual);
            visual.Root.Position=new((pattern%3-1)*2.5f,0,(pattern/3-1)*2.5f);
            visual.VerifyReadback();Need(visual.OwnedResourceCount==3,"Owned visual resources");
        }
        var colorCell=Cell(0,flat:true);
        using(var colorVisual=new TerrainPatchVisual(new TerrainPatchGeometry(colorCell.CopyPositions(),colorCell.CopyNormals(),
            new uint[]{0xFFFF0000,0x80402010,0,0xFFFFFFFF,0x01020304,0xFF0000FF,0xFF00FF00,0x12345678,0xFFEEDDCC},1,0),holder))
        { colorVisual.VerifyReadback(); }
        int before=TerrainPatchVisual.Allocations,negatives=0;
        try{using var invalid=new TerrainPatchVisual(Cell(0,[0,1]),holder);throw new Exception("Auxiliary omission accepted");}catch(ArgumentException){negatives++;}
        try{using var invalid=new TerrainPatchVisual(Cell(0,scale:32),holder);throw new Exception("Engine UV cap accepted");}catch(ArgumentException){negatives++;}
        var invalidParent=new Node();
        try{using var invalid=new TerrainPatchVisual(Cell(0),invalidParent);throw new Exception("Detached parent accepted");}catch(ArgumentException){negatives++;}finally{invalidParent.Free();}
        var flat=Cell(0,flat:true);var p=flat.CopyPositions();var n=flat.CopyNormals();var colors=flat.CopyArgbColors();
        try{using var invalid=new TerrainPatchVisual(new TerrainPatchGeometry(new NVector3[9],n,colors,1,0),holder);throw new Exception("Degeneracy accepted");}catch(ArgumentException){negatives++;}
        try{using var invalid=new TerrainPatchVisual(new TerrainPatchGeometry(p,n.Select(v=>-v).ToArray(),colors,1,0),holder);throw new Exception("Normal disagreement accepted");}catch(ArgumentException){negatives++;}
        Need(before==TerrainPatchVisual.Allocations,"All preflight rejects precede allocation");
        bool wrongOwner=await Task.Run(()=>{try{visuals[0].VerifyReadback();return false;}catch(InvalidOperationException){return true;}});
        Need(wrongOwner,"Owner-thread guard");negatives++;
        var environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#10202e")};
        var world=new WorldEnvironment{Environment=environment};AddChild(world);
        var camera=new Camera3D{Current=true,Fov=50,Position=new(0,9,7)};AddChild(camera);camera.LookAt(Vector3.Zero);
        var canvas=new CanvasLayer();AddChild(canvas);canvas.AddChild(new Label{Text="Synthetic heights | nine exact topology patterns | UV/COLOR inspection",Position=new(16,16)});
        if(gpu)
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            if(capture is not null){using var image=GetViewport().GetTexture().GetImage();Need(image.SavePng(capture)==Error.Ok,"Capture");}
            await VerifyGpu();
        }
        else { await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
        Need(visuals.All(v=>!v.IsDisposed),"Retained until drain");
        Cleanup();Need(holder.GetChildCount()==0&&TerrainPatchVisual.Allocations==TerrainPatchVisual.Disposals,"Owned deterministic drain");
        holder.Free();world.Free();environment.Dispose();camera.Free();canvas.Free();
        GD.Print("terrain_geometry="+JsonSerializer.Serialize(new{gpu,Patterns=9,AuxiliarySelectors=4,NativeReadbackVertices=81,VariedArgbReadbackVertices=9,NegativeCases=negatives,
            TerrainPatchVisual.Allocations,TerrainPatchVisual.Disposals,SyntheticHeights=true,OriginalMaterialParity=false,GpuFrontSamples=gpu?4:0,GpuBackSamples=gpu?4:0}));
        GD.Print("PASS: terrain geometry teardown");
    }

    private async Task VerifyGpu()
    {
        var viewport=new SubViewport{Size=new Vector2I(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(viewport);
        TerrainPatchVisual? visual=null;
        try
        {
            visual=new TerrainPatchVisual(Cell(0,[1],flat:true),viewport);visual.VerifyReadback();
            var camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=2.4f,Position=new(0,3,0),Current=true};viewport.AddChild(camera);camera.LookAt(Vector3.Zero,Vector3.Back);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            (int X,int Y,Color Expected)[] samples=[(16,16,Colors.White),(48,16,Colors.Cyan),(16,48,Colors.Magenta),(48,48,Colors.Blue)];
            using(var image=viewport.GetTexture().GetImage())
            {
                foreach(var sample in samples)
                {
                    var actual=image.GetPixel(sample.X,sample.Y);
                    Need(Math.Abs(actual.R-sample.Expected.R)<.03f&&Math.Abs(actual.G-sample.Expected.G)<.03f&&Math.Abs(actual.B-sample.Expected.B)<.03f,"GPU front winding/base UV/color: "+sample.X+","+sample.Y+"="+actual);
                }
            }
            camera.Position=new(0,-3,0);camera.LookAt(Vector3.Zero,Vector3.Back);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using(var image=viewport.GetTexture().GetImage())
                foreach(var sample in samples){var actual=image.GetPixel(sample.X,sample.Y);Need(actual.R<.03f&&actual.G<.03f&&actual.B<.03f,"GPU back culling");}
        }
        finally{visual?.Dispose();viewport.Free();}
    }
    private void Cleanup(){for(int i=visuals.Count-1;i>=0;i--){visuals[i].Dispose();visuals[i].Dispose();}visuals.Clear();}
}
