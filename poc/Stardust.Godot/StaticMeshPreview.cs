using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

public sealed class AssetPreviewException(string message) : InvalidOperationException(message);

/// <summary>Godot boundary only: Z reflection and winding reversal once; explicit opaque/unlit texture preview.</summary>
public sealed class StaticMeshPreview : IDisposable
{
    private readonly List<Resource> resources = new();
    private readonly Dictionary<string,ImageTexture> uploadedTextures = new(StringComparer.Ordinal);
    private readonly List<string> materialDiagnostics=new();
    public IReadOnlyList<string> MaterialDiagnostics=>materialDiagnostics;
    public int TexturedSurfaces {get;private set;}
    public int NeutralSurfaces {get;private set;}
    public ArrayMesh Mesh { get; }
    public StaticMeshPreview(TexturedStaticMesh asset,bool allowMainTextureOnly=false,bool allowNeutralMaterialFallback=false)
        :this(asset,allowMainTextureOnly,allowNeutralMaterialFallback,null){}
    internal StaticMeshPreview(TexturedStaticMesh asset,bool allowMainTextureOnly,bool allowNeutralMaterialFallback,PreviewTextureCache? sharedTextures)
    {
        Mesh = Own(new ArrayMesh());
        try
        {
            if(asset.Mesh.Primitives.Count==0 || asset.Mesh.VertexCount==0 || asset.Mesh.TriangleCount==0)
                throw new AssetPreviewException("Preview requires nonempty geometry.");
            Dictionary<string,ImageTexture> textures = uploadedTextures;
            Dictionary<DdsColorSpace,Shader> shaders = new();
            Dictionary<string,ShaderMaterial> materials = new(StringComparer.Ordinal);
            HashSet<string> neutralPaths=new(StringComparer.Ordinal);
            foreach ((string path,DdsImage texture) in asset.Textures)
            {
                int required=1; for(int d=Math.Max(texture.Width,texture.Height);d>1;d>>=1)required++;
                if(texture.Mips.Count!=required && texture.Mips.Count!=1)throw new AssetPreviewException("Preview cannot upload a partial Godot mip chain.");
                if(sharedTextures is not null)textures.Add(path,sharedTextures.Get(texture));
                else
                {
                    byte[] pixels=texture.Mips.SelectMany(mip=>mip.Rgba.ToArray()).ToArray();
                    using Image image=Image.CreateFromData(texture.Width,texture.Height,texture.Mips.Count>1,Image.Format.Rgba8,pixels);
                    textures.Add(path,Own(ImageTexture.CreateFromImage(image)));
                }
            }
            foreach((string path,DdsHdrImage texture) in asset.HdrTextures)
            {
                int required=1;for(int d=Math.Max(texture.Width,texture.Height);d>1;d>>=1)required++;
                if(texture.Mips.Count!=required && texture.Mips.Count!=1)throw new AssetPreviewException("Preview cannot upload a partial Godot HDR mip chain.");
                if(sharedTextures is not null)textures.Add(path,sharedTextures.Get(texture));
                else
                {
                    byte[] pixels=texture.Mips.SelectMany(mip=>mip.RgbHalf.ToArray()).ToArray();
                    using Image image=Image.CreateFromData(texture.Width,texture.Height,texture.Mips.Count>1,Image.Format.Rgbh,pixels);
                    textures.Add(path,Own(ImageTexture.CreateFromImage(image)));
                }
            }
            foreach ((string path,StaticMeshMaterial material) in asset.Materials)
            {
                try
                {
                    if(material.Customization is not null && !allowMainTextureOnly)throw new AssetPreviewException("Customizable material inspection requires explicit texture-only mode; palette colors remain untranslated.");
                    ShaderTextureReference[] mainBindings=material.References.Textures.Where(binding=>binding.Tag==0x4D41494E).ToArray();
                    if(mainBindings.Length!=1 || mainBindings[0].SourceFlag!=0 || mainBindings[0].AssetPath is not string texturePath ||
                        !material.Parameters.CoordinateSets.TryGetValue(0x4D41494E,out byte coordinate) || coordinate!=0)
                        throw new AssetPreviewException("Preview requires one named MAIN texture and MAIN UV0.");
                    StaticEffectDescription effect=asset.Effects[material.References.EffectPath];
                    if(effect.ContainsPrecalculatedVertexLighting && !allowMainTextureOnly)
                        throw new AssetPreviewException("Preview does not translate precalculated lighting.");
                    // Explicit inspection policy: use an authored opaque one-stage MAIN fallback.
                    // This does not select the original capability branch or translate the other candidates.
                    StaticEffectPass? fallback=effect.Implementations.Where(impl=>impl.Passes.Count==1).Select(impl=>impl.Passes[0])
                        .LastOrDefault(pass=>pass.PixelProgram is null && pass.Fields[0x19]==1 && pass.Fields[0x1A]==1 &&
                            pass.Fields[0x20]==0 && pass.Fields[0x30]==0 && pass.Fields[0x3C]==15 && pass.Fields[0x7C]==0x4D41494E &&
                            pass.FixedStages.Count==1 && pass.FixedStages[0].TextureTag==0x4D41494E && pass.FixedStages[0].CoordinateTag==0x4D41494E);
                    if(fallback is null)
                    {
                        bool opaqueMain=effect.Implementations.Where(impl=>impl.Passes.Count==1).Select(impl=>impl.Passes[0]).Any(pass=>
                            pass.Fields[0x19]==1 && pass.Fields[0x1A]==1 && pass.Fields[0x20]==0 && pass.Fields[0x30]==0 && pass.Fields[0x3C]==15 &&
                            pass.Fields[0x7C]==0x4D41494E && (pass.PixelTextures.Any(binding=>binding.TextureTag==0x4D41494E) ||
                            pass.FixedStages.Any(stage=>stage.TextureTag==0x4D41494E && stage.CoordinateTag==0x4D41494E)));
                        if(!allowMainTextureOnly) throw new AssetPreviewException("Preview has no authored opaque single-MAIN fallback; explicit texture-only mode is required for an opaque MAIN candidate.");
                        if(!opaqueMain)throw new AssetPreviewException("Material is outside opaque MAIN inspection policy.");
                    }
                    if(asset.Textures.TryGetValue(texturePath,out DdsImage? rgba) && rgba.AlphaMode==DdsAlphaMode.Premultiplied)throw new AssetPreviewException("MAIN inspection requires straight/opaque albedo; premultiplied RGB is retained by the CPU decoder.");
                    DdsColorSpace space=rgba?.ColorSpace??DdsColorSpace.Linear;
                    if(!shaders.TryGetValue(space,out Shader? shader))
                    {
                        string colorHint=space==DdsColorSpace.Linear?"":"source_color, ";
                        shader=Own(new Shader{Code="shader_type spatial; render_mode unshaded, cull_back; uniform sampler2D albedo_texture : "+colorHint+
                            "filter_linear_mipmap, repeat_enable; void fragment() { ALBEDO = texture(albedo_texture, UV).rgb; }"});
                        shaders.Add(space,shader);
                    }
                    ShaderMaterial preview=Own(new ShaderMaterial{Shader=shader});preview.SetShaderParameter("albedo_texture",textures[texturePath]);
                    materials.Add(path,preview);
                }
                catch(AssetPreviewException failure)when(allowNeutralMaterialFallback)
                {
                    Shader shader=Own(new Shader{Code="shader_type spatial; render_mode unshaded, cull_back; void fragment() { ALBEDO=vec3(0.48,0.52,0.58); }"});
                    materials.Add(path,Own(new ShaderMaterial{Shader=shader}));neutralPaths.Add(path);materialDiagnostics.Add(path+": "+failure.Message);
                }
            }
            foreach(MeshPrimitive primitive in asset.Mesh.Primitives)
            {
                using Godot.Collections.Array arrays=CreateArrays(primitive);
                Mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arrays);
                Mesh.SurfaceSetMaterial(Mesh.GetSurfaceCount()-1,materials[primitive.ShaderPath]);
                if(neutralPaths.Contains(primitive.ShaderPath))NeutralSurfaces++;else TexturedSurfaces++;
            }
        }
        catch{Dispose();throw;}
    }

    internal static Godot.Collections.Array CreateArrays(MeshPrimitive primitive)
    {
        Vector3[] positions=primitive.Vertices.Select(vertex=>new Vector3(vertex.Position.X,vertex.Position.Y,-vertex.Position.Z)).ToArray();
        Vector3[] normals=primitive.Vertices.Select(vertex=>new Vector3(vertex.Normal.X,vertex.Normal.Y,-vertex.Normal.Z)).ToArray();
        Vector2[] uvs=primitive.Vertices.Select(vertex=>new Vector2(vertex.TextureCoordinate.X,vertex.TextureCoordinate.Y)).ToArray();
        int[] indices=primitive.Indices.Select(index=>(int)index).ToArray();
        for(int i=0;i<indices.Length;i+=3)(indices[i+1],indices[i+2])=(indices[i+2],indices[i+1]);
        Godot.Collections.Array arrays=new();arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex]=positions;arrays[(int)Godot.Mesh.ArrayType.Normal]=normals;
        arrays[(int)Godot.Mesh.ArrayType.TexUV]=uvs;arrays[(int)Godot.Mesh.ArrayType.Index]=indices;return arrays;
    }

    public static void VerifyCoordinateBoundary()
    {
        System.Numerics.Vector3 a=new(1,2,3),b=new(4,2,5),c=new(1,6,7);
        System.Numerics.Vector3 normal=System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(b-a,c-a));
        MeshVertex[] vertices={new(a,normal,new(0.1f,0.2f)),new(b,normal,new(0.3f,0.4f)),new(c,normal,new(0.5f,0.6f))};
        MeshPrimitive primitive=new("synthetic","0001",vertices,new ushort[]{0,1,2});
        using Godot.Collections.Array arrays=CreateArrays(primitive);
        Vector3[] p=arrays[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array();
        Vector3[] n=arrays[(int)Godot.Mesh.ArrayType.Normal].AsVector3Array();
        Vector2[] uv=arrays[(int)Godot.Mesh.ArrayType.TexUV].AsVector2Array();
        int[] index=arrays[(int)Godot.Mesh.ArrayType.Index].AsInt32Array();
        if(p[0]!=new Vector3(1,2,-3) || !index.SequenceEqual(new[]{0,2,1}) || uv[2]!=new Vector2(0.5f,0.6f) ||
            n[0]!=new Vector3(normal.X,normal.Y,-normal.Z) || (p[2]-p[0]).Cross(p[1]-p[0]).Normalized().Dot(n[0])<0.999f ||
            primitive.Vertices[0].Position!=a || primitive.Indices[1]!=1)
            throw new InvalidOperationException("Asymmetric coordinate/winding/UV fixture failed.");
    }

    public void VerifyUploadedAsset(TexturedStaticMesh source)
    {
        VerifyUploadedGeometry(Mesh,source.Mesh);
        foreach((string path,DdsImage original) in source.Textures)
        {
            using Image uploaded=uploadedTextures[path].GetImage();
            byte[] expected=original.Mips.SelectMany(mip=>mip.Rgba.ToArray()).ToArray();
            if(uploaded.GetWidth()!=original.Width || uploaded.GetHeight()!=original.Height ||
                uploaded.GetMipmapCount()!=original.Mips.Count-1 || !uploaded.GetData().AsSpan().SequenceEqual(expected))
                throw new InvalidOperationException("Godot RGBA/mip upload readback mismatch.");
        }
        foreach((string path,DdsHdrImage original) in source.HdrTextures)
        {
            using Image uploaded=uploadedTextures[path].GetImage();
            byte[] expected=original.Mips.SelectMany(mip=>mip.RgbHalf.ToArray()).ToArray();
            if(uploaded.GetFormat()!=Image.Format.Rgbh || uploaded.GetWidth()!=original.Width || uploaded.GetHeight()!=original.Height ||
                uploaded.GetMipmapCount()!=original.Mips.Count-1 || !uploaded.GetData().AsSpan().SequenceEqual(expected))throw new InvalidOperationException("Godot RGB-half material upload readback mismatch.");
        }
    }
    internal static void VerifyUploadedGeometry(ArrayMesh mesh,StaticMeshDescription source)
    {
        if(mesh.GetSurfaceCount()!=source.Primitives.Count)throw new InvalidOperationException("Godot surface count mismatch.");
        for(int surface=0;surface<mesh.GetSurfaceCount();surface++)
        {
            using Godot.Collections.Array arrays=mesh.SurfaceGetArrays(surface);
            Vector3[] vertices=arrays[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array();
            Vector3[] normals=arrays[(int)Godot.Mesh.ArrayType.Normal].AsVector3Array();
            Vector2[] uv=arrays[(int)Godot.Mesh.ArrayType.TexUV].AsVector2Array();
            int[] indices=arrays[(int)Godot.Mesh.ArrayType.Index].AsInt32Array();
            MeshPrimitive original=source.Primitives[surface];
            if(vertices.Length!=original.Vertices.Count || normals.Length!=vertices.Length || uv.Length!=vertices.Length || indices.Length!=original.Indices.Count)throw new InvalidOperationException("Godot uploaded cardinality mismatch.");
            for(int i=0;i<vertices.Length;i++)
            {
                var p=original.Vertices[i].Position;var n=original.Vertices[i].Normal;var t=original.Vertices[i].TextureCoordinate;
                if(vertices[i].DistanceTo(new(p.X,p.Y,-p.Z))>1e-5f || normals[i].DistanceTo(new(n.X,n.Y,-n.Z))>0.002f ||
                    uv[i].DistanceTo(new(t.X,t.Y))>0.002f)throw new InvalidOperationException("Godot position/normal/UV readback mismatch.");
            }
            for(int i=0;i<indices.Length;i+=3)
                if(indices[i]!=original.Indices[i] || indices[i+1]!=original.Indices[i+2] || indices[i+2]!=original.Indices[i+1])
                    throw new InvalidOperationException("Godot index readback mismatch.");
        }
    }
    public static async System.Threading.Tasks.Task VerifyGpuUvBoundary(Node owner)
    {
        using ArrayMesh mesh=new();using Shader shader=new(){Code="shader_type spatial; render_mode unshaded, cull_back; uniform sampler2D sample_texture : source_color, filter_nearest, repeat_disable; void fragment() { ALBEDO=texture(sample_texture,UV).rgb; }"};
        using Image image=Image.CreateFromData(2,2,false,Image.Format.Rgba8,new byte[]{255,0,0,255,0,255,0,255,0,0,255,255,255,255,0,255});
        using ImageTexture texture=ImageTexture.CreateFromImage(image);using ShaderMaterial material=new(){Shader=shader};material.SetShaderParameter("sample_texture",texture);
        System.Numerics.Vector3 normal=new(0,0,1);
        MeshVertex[] vertices={new(new(-1,-1,0),normal,new(0,1)),new(new(-1,1,0),normal,new(0,0)),
            new(new(1,1,0),normal,new(1,0)),new(new(1,-1,0),normal,new(1,1))};
        using Godot.Collections.Array arrays=CreateArrays(new("synthetic","0001",vertices,new ushort[]{0,1,2,0,2,3}));
        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arrays);mesh.SurfaceSetMaterial(0,material);
        SubViewport viewport=new(){Size=new Vector2I(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};
        owner.AddChild(viewport);
        try
        {
            viewport.AddChild(new MeshInstance3D{Mesh=mesh});
            Camera3D camera=new(){Projection=Camera3D.ProjectionType.Orthogonal,Size=2,Position=new Vector3(0,0,-3),Current=true};
            viewport.AddChild(camera);camera.LookAt(Vector3.Zero);
            await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using Image result=viewport.GetTexture().GetImage();
            (int X,int Y,Color Expected)[] samples={(16,16,Colors.Green),(48,16,Colors.Red),(16,48,Colors.Yellow),(48,48,Colors.Blue)};
            foreach(var sample in samples)
            {
                Color actual=result.GetPixel(sample.X,sample.Y);
                if(Math.Abs(actual.R-sample.Expected.R)>0.02f || Math.Abs(actual.G-sample.Expected.G)>0.02f || Math.Abs(actual.B-sample.Expected.B)>0.02f)
                    throw new InvalidOperationException($"GPU winding/UV fixture differs at {sample.X},{sample.Y}: {actual}.");
            }
        }
        finally{viewport.Free();}
    }
    private T Own<T>(T resource) where T:Resource {resources.Add(resource);return resource;}
    public void Dispose(){for(int i=resources.Count-1;i>=0;i--)resources[i].Dispose();resources.Clear();uploadedTextures.Clear();}
}
