using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

/// <summary>Replacement-only synthetic supplied-circle height patch inspection; no full terrain generation.</summary>
public partial class TerrainCircleHeightPreview : Node
{
    private readonly List<TerrainPatchVisual> visuals=new();
    public override async void _Ready()
    {
        try{await Run();GetTree().Quit();}
        catch(Exception e){GD.PushError("Circle height preview: "+e);Cleanup();GetTree().Quit(1);}
    }
    private static void Need(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
    private async Task Run()
    {
        string[] args=OS.GetCmdlineUserArgs();int at=Array.IndexOf(args,"--fixtures");Need(at>=0,"Explicit synthetic fixtures required.");
        using var doc=JsonDocument.Parse(File.ReadAllText(args[at+1]));var f=doc.RootElement.GetProperty("Cases")[0];
        uint[] Words(string key)=>f.GetProperty(key).EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
        var circle=TerrainBoundaryCircleInput.Decode(Convert.FromHexString(f.GetProperty("CircleHex").GetString()!));
        var height=TerrainGeneratorInput.Decode(Convert.FromHexString(f.GetProperty("GeneratorHex").GetString()!)).Rules.Single().HeightConstant!;
        var patch=TerrainCircleHeightPatch.Build(f.GetProperty("Cells").GetInt32(),Words("First"),Words("Second"),Words("Initial"),circle,height,
            (TerrainHeightArithmeticPrecision)f.GetProperty("CachePrecision").GetInt32(),(TerrainHeightArithmeticPrecision)f.GetProperty("QueryPrecision").GetInt32(),
            (TerrainHeightArithmeticPrecision)f.GetProperty("HeightPrecision").GetInt32());
        bool gpu=DisplayServer.GetName()!="headless";int allocations=TerrainPatchVisual.Allocations,disposals=TerrainPatchVisual.Disposals;
        var holder=new Node3D();AddChild(holder);
        foreach(var p in patch.Patches){var visual=new TerrainPatchVisual(p.Geometry,holder);visuals.Add(visual);visual.VerifyReadback();Need(visual.OwnedResourceCount==3,"Three owned resources per patch.");}
        var camera=new Camera3D{Current=true,Position=new(0,4,3),Fov=45};AddChild(camera);camera.LookAt(new Vector3(0,.4f,0));
        var canvas=new CanvasLayer();AddChild(canvas);canvas.AddChild(new Label{Text="Synthetic circle + AHCN | explicit precision | supplied grid",Position=new(12,12)});
        int front=0,back=0;
        if(gpu)
        {
            var viewport=new SubViewport{Size=new Vector2I(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(viewport);
            TerrainPatchVisual? probe=null;
            try
            {
                probe=new TerrainPatchVisual(patch.Patches[0].Geometry,viewport);probe.VerifyReadback();
                var top=new Camera3D{Current=true,Projection=Camera3D.ProjectionType.Orthogonal,Size=1.2f,Position=new(-.5f,5,.5f)};viewport.AddChild(top);top.LookAt(new Vector3(-.5f,0,.5f),Vector3.Back);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                (int X,int Y,Color Expected)[] points=[(16,16,Colors.Yellow),(48,16,new Color(0,1,0)),(16,48,Colors.Red),(48,48,Colors.Black)];
                using(var image=viewport.GetTexture().GetImage())foreach(var p in points){var v=image.GetPixel(p.X,p.Y);Need(Math.Abs(v.R-p.Expected.R)<.03f&&Math.Abs(v.G-p.Expected.G)<.03f&&Math.Abs(v.B-p.Expected.B)<.03f,"GPU front height patch/UV/color/winding: "+p.X+","+p.Y+"="+v);front++;}
                top.Position=new(-.5f,-5,.5f);top.LookAt(new Vector3(-.5f,0,.5f),Vector3.Back);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using(var image=viewport.GetTexture().GetImage())foreach(var p in points){var v=image.GetPixel(p.X,p.Y);Need(v.R<.03f&&v.G<.03f&&v.B<.03f,"GPU back culling.");back++;}
            }
            finally{probe?.Dispose();viewport.Free();}
            at=Array.IndexOf(args,"--capture");if(at>=0)
            {
                string capture=Path.GetFullPath(args[at+1]);string prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","..","..","local-private"))+Path.DirectorySeparatorChar;
                Need(capture.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&!File.Exists(capture),"Fresh private capture.");
                using var image=GetViewport().GetTexture().GetImage();Need(image.SavePng(capture)==Error.Ok,"Capture write.");
            }
        }
        else await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Need(visuals.All(v=>!v.IsDisposed),"Retention before cleanup.");
        Cleanup();Need(holder.GetChildCount()==0,"Detached owned roots.");holder.Free();camera.Free();canvas.Free();
        Need(TerrainPatchVisual.Allocations-allocations==TerrainPatchVisual.Disposals-disposals,"Balanced resource disposal.");
        GD.Print("circle_height_patch="+JsonSerializer.Serialize(new{gpu,Patches=patch.Patches.Count,Samples=patch.Samples.Count,FrontSamples=front,BackSamples=back,
            Allocations=TerrainPatchVisual.Allocations-allocations,Disposals=TerrainPatchVisual.Disposals-disposals,SyntheticCoordinates=true,OriginalPrecision="Unproven"}));
        GD.Print("PASS: circle height patch teardown");
    }
    private void Cleanup(){for(int i=visuals.Count-1;i>=0;i--){visuals[i].Dispose();visuals[i].Dispose();}visuals.Clear();}
}
