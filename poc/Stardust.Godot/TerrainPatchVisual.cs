using System;
using System.Linq;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

public enum TerrainPatchRenderPolicy { DiagnosticUvColor, ModernLitVertexColor, ModernSuppliedMainTexture }

/// <summary>Owned synthetic inspection adapter. Z reflection, original index order, explicit UV/COLOR shader.</summary>
public sealed class TerrainPatchVisual : IDisposable
{
    private readonly int owner = System.Environment.CurrentManagedThreadId;
    private readonly TerrainPatchGeometry source;
    private bool disposed;
    private ArrayMesh? mesh;
    private Shader? shader;
    private ShaderMaterial? material;
    private TerrainSuppliedStaticMaterial? suppliedMaterial;
    private ImageTexture? borrowedTexture;
    public Node3D Root { get; }
    public bool IsDisposed => disposed;
    public TerrainPatchRenderPolicy Policy { get; }
    public int OwnedResourceCount => disposed ? 0 : 3;
    public static int Allocations { get; private set; }
    public static int Disposals { get; private set; }

    public TerrainPatchVisual(TerrainPatchGeometry source, Node parent):this(source,parent,TerrainPatchRenderPolicy.DiagnosticUvColor) { }

    public TerrainPatchVisual(TerrainPatchGeometry source, Node parent,TerrainPatchRenderPolicy policy,
        TerrainSuppliedStaticMaterial? suppliedMaterial=null,ImageTexture? suppliedTexture=null)
    {
        if(policy is not (TerrainPatchRenderPolicy.DiagnosticUvColor or TerrainPatchRenderPolicy.ModernLitVertexColor or TerrainPatchRenderPolicy.ModernSuppliedMainTexture))throw new ArgumentOutOfRangeException(nameof(policy));
        if((policy==TerrainPatchRenderPolicy.ModernSuppliedMainTexture)!=(suppliedMaterial is not null&&suppliedTexture is not null)
            ||policy!=TerrainPatchRenderPolicy.ModernSuppliedMainTexture&&(suppliedMaterial is not null||suppliedTexture is not null))throw new ArgumentException("Explicit material and borrowed texture required together.");
        if (OS.GetThreadCallerId() != OS.GetMainThreadId()) throw new InvalidOperationException("Godot main thread required.");
        if(suppliedTexture is not null&&(!GodotObject.IsInstanceValid(suppliedTexture)||suppliedTexture.GetWidth()!=suppliedMaterial!.Width||suppliedTexture.GetHeight()!=suppliedMaterial.Height))throw new ArgumentException("Actual live matching texture required.");
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(parent);
        if (!GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree()) throw new ArgumentException("Live parent required.");
        if (source.AuxiliarySetCount > 1) throw new ArgumentException("Inspection adapter supports base UV plus at most one auxiliary UV.");
        if (source.UvScale > 16) throw new ArgumentException("Inspection UV scale cap is 16.");
        ValidateWinding(source);
        this.source = source;
        this.suppliedMaterial=suppliedMaterial;borrowedTexture=suppliedTexture;
        Policy=policy;
        Root = new Node3D { Name = "OwnedTerrainPatch" }; Allocations++;
        try
        {
            mesh = new ArrayMesh();
            shader = new Shader { Code = policy==TerrainPatchRenderPolicy.DiagnosticUvColor
                ? "shader_type spatial; render_mode unshaded, cull_back; void fragment() { ALBEDO=vec3(step(0.5,UV.x),step(0.5,UV.y),step(0.5,COLOR.r)); }"
                : policy==TerrainPatchRenderPolicy.ModernLitVertexColor
                ? "shader_type spatial; render_mode cull_back; void fragment() { ALBEDO=COLOR.rgb; ROUGHNESS=1.0; METALLIC=0.0; }"
                : "shader_type spatial; render_mode cull_back; uniform sampler2D main_texture : "+(suppliedMaterial!.ColorSpace==DdsColorSpace.Srgb?"source_color, ":"")+"filter_nearest, repeat_enable; uniform vec4 diffuse_multiplier; void fragment() { ALBEDO=texture(main_texture,UV).rgb*COLOR.rgb*diffuse_multiplier.rgb; ROUGHNESS=1.0; METALLIC=0.0; }" };
            material = new ShaderMaterial { Shader = shader };
            if(suppliedMaterial is not null){var d=suppliedMaterial.DiffuseMultiplier;material.SetShaderParameter("main_texture",suppliedTexture!);material.SetShaderParameter("diffuse_multiplier",new Color(d.X,d.Y,d.Z,d.W));}
            using var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = source.CopyPositions().Select(v => new Vector3(v.X,v.Y,-v.Z)).ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = source.CopyNormals().Select(v => new Vector3(v.X,v.Y,-v.Z).Normalized()).ToArray();
            arrays[(int)Mesh.ArrayType.Color] = source.CopyRgbaColors().Select(v => new Color(v.X,v.Y,v.Z,v.W)).ToArray();
            arrays[(int)Mesh.ArrayType.TexUV] = source.CopyBaseUvs().Select(v => new Vector2(v.X,v.Y)).ToArray();
            if (source.AuxiliarySetCount == 1) arrays[(int)Mesh.ArrayType.TexUV2] = source.CopyAuxiliaryUvs(0).Select(v => new Vector2(v.X,v.Y)).ToArray();
            arrays[(int)Mesh.ArrayType.Index] = source.CopyIndices().Select(i => (int)i).ToArray();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
            mesh.SurfaceSetMaterial(0,material);
            Root.AddChild(new MeshInstance3D { Mesh = mesh });
            parent.AddChild(Root);
        }
        catch { Dispose(); throw; }
    }

    private static void ValidateWinding(TerrainPatchGeometry source)
    {
        var p=source.CopyPositions();var n=source.CopyNormals();var ix=source.CopyIndices();
        for (int i=0;i<ix.Length;i+=3)
        {
            var a=p[ix[i]];var b=p[ix[i+1]];var c=p[ix[i+2]];
            double x=(double)(b.Y-a.Y)*(c.Z-a.Z)-(double)(b.Z-a.Z)*(c.Y-a.Y);
            double y=(double)(b.Z-a.Z)*(c.X-a.X)-(double)(b.X-a.X)*(c.Z-a.Z);
            double z=(double)(b.X-a.X)*(c.Y-a.Y)-(double)(b.Y-a.Y)*(c.X-a.X);
            var normal=n[ix[i]]+n[ix[i+1]]+n[ix[i+2]];
            if (x*x+y*y+z*z<1e-20 || x*normal.X+y*normal.Y+z*normal.Z<=0)
                throw new ArgumentException("Inspection requires nondegenerate source CCW triangles coherent with supplied normals.");
        }
    }

    public void VerifyReadback()
    {
        Owner(); if(disposed)throw new ObjectDisposedException(nameof(TerrainPatchVisual));
        using var a=mesh!.SurfaceGetArrays(0);
        if(suppliedMaterial is not null){var d=suppliedMaterial.DiffuseMultiplier;
            if(!ReferenceEquals(material!.GetShaderParameter("main_texture").AsGodotObject(),borrowedTexture)
                ||material.GetShaderParameter("diffuse_multiplier").AsColor()!=new Color(d.X,d.Y,d.Z,d.W)
                ||shader!.Code.Contains("source_color",StringComparison.Ordinal)!=(suppliedMaterial.ColorSpace==DdsColorSpace.Srgb))throw new InvalidOperationException("Actual supplied MAIN material binding readback.");}
        var p=a[(int)Mesh.ArrayType.Vertex].AsVector3Array();var n=a[(int)Mesh.ArrayType.Normal].AsVector3Array();
        var c=a[(int)Mesh.ArrayType.Color].AsColorArray();var uv=a[(int)Mesh.ArrayType.TexUV].AsVector2Array();
        var ix=a[(int)Mesh.ArrayType.Index].AsInt32Array();
        var ep=source.CopyPositions();var en=source.CopyNormals();var ec=source.CopyRgbaColors();var eu=source.CopyBaseUvs();
        if(p.Length!=9||n.Length!=9||c.Length!=9||uv.Length!=9||!ix.SequenceEqual(source.CopyIndices().Select(i=>(int)i)))throw new InvalidOperationException("Terrain cardinality/index readback.");
        for(int i=0;i<9;i++)
            if(p[i].DistanceTo(new(ep[i].X,ep[i].Y,-ep[i].Z))>1e-5f
                ||n[i].DistanceTo(new Vector3(en[i].X,en[i].Y,-en[i].Z).Normalized())>.002f
                ||uv[i].DistanceTo(new(eu[i].X,eu[i].Y))>.002f
                ||Math.Abs(c[i].R-ec[i].X)>2e-6f||Math.Abs(c[i].G-ec[i].Y)>2e-6f||Math.Abs(c[i].B-ec[i].Z)>2e-6f||Math.Abs(c[i].A-ec[i].W)>2e-6f)
                throw new InvalidOperationException("Terrain semantic readback.");
        if(source.AuxiliarySetCount==1)
        {
            var au=a[(int)Mesh.ArrayType.TexUV2].AsVector2Array();var expected=source.CopyAuxiliaryUvs(0);
            if(au.Length!=9||au.Where((v,i)=>v.DistanceTo(new(expected[i].X,expected[i].Y))>.002f).Any())throw new InvalidOperationException("Auxiliary UV readback.");
        }
    }

    public void Dispose()
    {
        Owner(); if(disposed)return; disposed=true;
        if(GodotObject.IsInstanceValid(Root)) { if(Root.GetParent() is Node p)p.RemoveChild(Root); Root.Free(); }
        if(mesh is not null && GodotObject.IsInstanceValid(mesh)){mesh.ClearSurfaces();mesh.Dispose();}
        if(material is not null && GodotObject.IsInstanceValid(material))material.Dispose();
        if(shader is not null && GodotObject.IsInstanceValid(shader))shader.Dispose();
        mesh=null;material=null;shader=null;suppliedMaterial=null;borrowedTexture=null;Disposals++;
    }
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Visual owner thread required.");}
}
