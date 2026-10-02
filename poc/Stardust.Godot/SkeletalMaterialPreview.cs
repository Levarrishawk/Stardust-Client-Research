using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

/// <summary>Explicit opaque MAIN/UV0 inspection with optional generated per-mesh face bindings.</summary>
internal sealed class SkeletalMaterialPreview : IDisposable
{
    private readonly PreviewTextureCache textures=new();
    private readonly List<Resource> resources=new();
    private SkeletalMaterialBundle source;
    private readonly Dictionary<string,ShaderMaterial> materials=new(StringComparer.Ordinal);
    private readonly Dictionary<BlueprintGeneratedTextureKey,(string Path,ShaderMaterial Material)> generatedMaterials=new();
    private BlueprintGeneratedFaces? generated;
    private readonly IReadOnlyDictionary<string,SkeletalSpecularMaterial>? specular;
    private readonly SkeletalVertexLitSpecularBundle? vertexLit;
    internal void ApplyCellLighting(CellLightingPreview rig)
    {
        if(specular is null||vertexLit is null)throw new AssetPreviewException("Cell lighting requires retained specular materials.");
        foreach(var pair in materials)CellLightingGpuPreview.Apply(pair.Value,rig,vertexLit.Materials.ContainsKey(pair.Key));
        foreach(var pair in generatedMaterials.Values)CellLightingGpuPreview.Apply(pair.Material,rig,vertexLit.Materials.ContainsKey(pair.Path));
    }
    internal int TextureCount=>textures.Count;
    internal long TextureBytes=>textures.UploadedBytes;
    internal Material Get(string path,string mesh,uint slot)
    {
        if(generatedMaterials.TryGetValue(new(mesh,slot,0x4D41494E),out var material))
        {if(material.Path!=path)throw new AssetPreviewException("Generated face shader identity differs.");return material.Material;}
        return materials[path];
    }
    internal SkeletalMaterialPreview(SkeletalMaterialBundle source,BlueprintGeneratedFaces? generated=null,bool specularPreview=false)
    {
        this.source=source;
        this.generated=generated;
        specular=specularPreview?SkeletalSpecularMaterials.Describe(source):null;
        vertexLit=specularPreview?(source.VertexLitSpecular??throw new AssetPreviewException("Owned vertex-lit specular programs required.")):null;
        if(specularPreview&&(source.TangentLighting is null||!specular!.Keys.ToHashSet().SetEquals(source.TangentLighting.Materials.Keys)))throw new AssetPreviewException("Owned tangent vertex programs required for normal/specular materials.");
        try
        {
            foreach(var texture in source.Textures.Values)
            {
                int required=1;for(int size=Math.Max(texture.Width,texture.Height);size>1;size>>=1)required++;
                if(texture.Mips.Count!=1&&texture.Mips.Count!=required)throw new AssetPreviewException("Skeletal material partial mip chain cannot be uploaded.");
                textures.Get(texture);
            }
            var shaders=new Dictionary<(DdsColorSpace Space,bool Factors,bool Diffuse,bool Normal,bool Specular,bool VertexLit,bool MainMask,bool ScalesBloom),Shader>();
            ShaderMaterial Render(DdsImage image,string path)
            {
                SkeletalAlbedoFactors? factors=null;
                if(source.AlbedoFactors is not null&&!source.AlbedoFactors.Materials.TryGetValue(path,out factors))throw new AssetPreviewException("Skeletal albedo policy absent.");
                bool diffuse=source.CompressedNormals is not null,normal=source.CompressedNormals?.Materials.ContainsKey(path)==true;
                bool hasSpecular=specular?.ContainsKey(path)==true,hasVertexLit=vertexLit?.Materials.ContainsKey(path)==true;bool mainMask=hasSpecular&&specular![path].MaskUsesMainAlpha,scalesBloom=hasVertexLit&&vertexLit!.Programs[vertexLit.Materials[path].PixelProgram].ScalesBloomRgb;var key=(image.ColorSpace,factors is not null,diffuse,normal,hasSpecular,hasVertexLit,mainMask,scalesBloom);
                if(!shaders.TryGetValue(key,out Shader? shader))
                {
                    string hint=image.ColorSpace==DdsColorSpace.Linear?"":"source_color, ";
                    shader=Own(new Shader{Code=hasVertexLit?SkeletalVertexLitSpecularPreview.Code(image.ColorSpace,scalesBloom):hasSpecular?SkeletalSpecularPreview.Code(image.ColorSpace,mainMask,true):diffuse?SkeletalNormalDiffusePreview.Code(image.ColorSpace,normal):factors is not null?SkeletalAlbedoPreview.Code(image.ColorSpace):"shader_type spatial; render_mode unshaded, cull_back; uniform sampler2D albedo_texture : "+hint+
                        "filter_linear_mipmap, repeat_enable; void fragment() { ALBEDO=texture(albedo_texture, UV).rgb; }"});shaders.Add(key,shader);
                }
                var rendered=Own(new ShaderMaterial{Shader=shader});rendered.SetShaderParameter("albedo_texture",textures.Get(image));if(factors is not null)SkeletalAlbedoPreview.Apply(rendered,factors);
                if(diffuse)SkeletalNormalDiffusePreview.ApplyLighting(rendered);
                if(normal)rendered.SetShaderParameter("normal_texture",NormalTexture(path));
                if(hasSpecular||hasVertexLit){var description=SpecularDescription(path)!;SkeletalSpecularPreview.Apply(rendered,description);if(!description.MaskUsesMainAlpha)rendered.SetShaderParameter("specular_texture",SpecularTexture(path));}
                if(hasSpecular){var color=specular![path].Material.EmissiveRgba;rendered.SetShaderParameter("material_emissive",new Vector3(color.X,color.Y,color.Z));}
                if(hasVertexLit){var color=vertexLit!.Materials[path].Material.EmissiveRgba;rendered.SetShaderParameter("emissive_color",new Vector3(color.X,color.Y,color.Z));}return rendered;
            }
            foreach(var pair in source.Materials)
            {
                var material=pair.Value;var main=material.References.Textures.Where(t=>t.Tag==0x4D41494E).ToArray();
                if(main.Length!=1||main[0].SourceFlag!=0||main[0].AssetPath is not string path||!material.Parameters.CoordinateSets.TryGetValue(0x4D41494E,out byte coordinate)||coordinate!=0)
                    throw new AssetPreviewException("Skeletal texture inspection requires one named MAIN and UV0.");
                var effect=source.Effects[material.References.EffectPath];
                bool opaqueMain=effect.Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0]).Any(pass=>
                    pass.Fields[0x19]==1&&pass.Fields[0x1A]==1&&pass.Fields[0x20]==0&&pass.Fields[0x30]==0&&pass.Fields[0x3C]==15&&pass.Fields[0x7C]==0x4D41494E&&
                    (pass.PixelTextures.Any(t=>t.TextureTag==0x4D41494E)||pass.FixedStages.Any(s=>s.TextureTag==0x4D41494E&&s.CoordinateTag==0x4D41494E)));
                if(!opaqueMain)throw new AssetPreviewException("Skeletal material outside opaque MAIN inspection policy.");
                var image=source.Textures[path];if(image.AlphaMode==DdsAlphaMode.Premultiplied)throw new AssetPreviewException("Skeletal MAIN requires straight/opaque RGB.");
                materials.Add(pair.Key,Render(image,pair.Key));
            }
            if(generated is not null)foreach(var binding in generated.Bindings)
            {
                if(binding.Key.TextureTag!=0x4D41494E||!source.Materials.ContainsKey(binding.Value.ShaderPath)||!generated.Outputs.TryGetValue(binding.Value.RendererPath,out var face))throw new AssetPreviewException("Invalid generated skeletal MAIN binding.");
                generatedMaterials.Add(binding.Key,(binding.Value.ShaderPath,Render(face.Image,binding.Value.ShaderPath)));
            }
        }
        catch{Dispose();throw;}
    }
    private ImageTexture NormalTexture(string path)
    {
        var refs=source.TextureSelection?.References[path]??source.Materials[path].References;
        var binding=refs.Textures.Single(t=>t.Tag==0x434E524D);
        return textures.Get(source.Textures[binding.AssetPath!]);
    }
    private SkeletalSpecularMaterial? SpecularDescription(string path)
    {
        if(specular?.TryGetValue(path,out var description)==true)return description;
        if(vertexLit?.Materials.TryGetValue(path,out var vertex)==true)return new(vertex.PixelProgram,vertex.Material,vertex.TextureTag,false);
        return null;
    }
    private ImageTexture SpecularTexture(string path)=>textures.Get(source.Textures[source.Materials[path].References.Textures.Single(t=>t.Tag==SpecularDescription(path)!.SpecularTextureTag).AssetPath!]);
    internal void VerifyUploadedTextures()
    {
        if(source.AlbedoFactors is not null)
        {
            foreach(var material in materials)SkeletalAlbedoPreview.Verify(material.Value,source.AlbedoFactors.Materials[material.Key]);
            foreach(var material in generatedMaterials.Values)SkeletalAlbedoPreview.Verify(material.Material,source.AlbedoFactors.Materials[material.Path]);
        }
        foreach(var pair in generatedMaterials)
        {
            var binding=generated!.Bindings[pair.Key];var expected=textures.Get(generated.Outputs[binding.RendererPath].Image);
            var actual=pair.Value.Material.GetShaderParameter("albedo_texture").AsGodotObject();
            if(actual is not ImageTexture image||image.GetInstanceId()!=expected.GetInstanceId())throw new AssetPreviewException("Generated face material is not bound to its uploaded output.");
        }
        if(source.CompressedNormals is not null)
        {
            void VerifyNormal(string path,ShaderMaterial material)
            {
                if(!source.CompressedNormals.Materials.ContainsKey(path))return;
                if(material.GetShaderParameter("normal_texture").AsGodotObject() is not ImageTexture image||image.GetInstanceId()!=NormalTexture(path).GetInstanceId())throw new AssetPreviewException("Normal material is not bound to its owned selected texture.");
            }
            foreach(var pair in materials)VerifyNormal(pair.Key,pair.Value);foreach(var pair in generatedMaterials.Values)VerifyNormal(pair.Path,pair.Material);
        }
        if(specular is not null)
        {
            void VerifySpecular(string path,ShaderMaterial target)
            {
                var description=SpecularDescription(path);if(description is null)return;SkeletalSpecularPreview.Verify(target,description);
                if(specular.ContainsKey(path))
                {
                    var main=source.Materials[path].References.Textures.Single(t=>t.Tag==0x4D41494E).AssetPath!;var expected=TangentSpecularPreview.Code(source.Textures[main].ColorSpace,description.MaskUsesMainAlpha,true);
                    if(target.Shader.Code!=expected)throw new AssetPreviewException("Retained skeletal shader mask/vertex-stage identity differs.");
                    var e=description.Material.EmissiveRgba;if(target.GetShaderParameter("material_emissive").AsVector3()!=new Vector3(e.X,e.Y,e.Z))throw new AssetPreviewException("Retained skeletal emissive constant differs.");
                }
                if(!description.MaskUsesMainAlpha&&(target.GetShaderParameter("specular_texture").AsGodotObject() is not ImageTexture image||image.GetInstanceId()!=SpecularTexture(path).GetInstanceId()))throw new AssetPreviewException("Specular mask binding differs.");
            }
            foreach(var pair in materials)VerifySpecular(pair.Key,pair.Value);foreach(var pair in generatedMaterials.Values)VerifySpecular(pair.Path,pair.Material);
        }
        foreach(var original in source.Textures.Values.Concat(generated?.Outputs.Values.Select(face=>face.Image)??Enumerable.Empty<DdsImage>()))
        {
            using var uploaded=textures.Get(original).GetImage();
            if(uploaded.GetWidth()!=original.Width||uploaded.GetHeight()!=original.Height||uploaded.GetFormat()!=Image.Format.Rgba8||uploaded.GetMipmapCount()!=original.Mips.Count-1||
                !uploaded.GetData().AsSpan().SequenceEqual(original.Mips.SelectMany(m=>m.Rgba.ToArray()).ToArray()))throw new AssetPreviewException("Skeletal texture/mip GPU readback mismatch.");
        }
    }
    internal void ApplyCustomization(SkeletalMaterialBundle replacement,BlueprintGeneratedFaces faces)
    {
        // Color/style revisions retain the independently selected texture state.
        replacement=replacement with{TextureSelection=source.TextureSelection};
        if(generated is null||source.AlbedoFactors is null||replacement.AlbedoFactors is null||
            !ReferenceEquals(source.Materials,replacement.Materials)||!ReferenceEquals(source.Textures,replacement.Textures)||
            !ReferenceEquals(source.Effects,replacement.Effects)||!ReferenceEquals(source.CompressedNormals,replacement.CompressedNormals)||!ReferenceEquals(source.VertexLitSpecular,replacement.VertexLitSpecular)||!ReferenceEquals(source.TangentLighting,replacement.TangentLighting)||!ReferenceEquals(source.AlbedoFactors.Programs,replacement.AlbedoFactors.Programs)||
            replacement.AlbedoFactors.Materials.Count!=source.AlbedoFactors.Materials.Count||faces.Bindings.Count!=generated.Bindings.Count||faces.Outputs.Count!=generated.Outputs.Count)
            throw new AssetPreviewException("Customization revision changed its owned material/resource closure.");
        foreach(var pair in source.AlbedoFactors.Materials)
            if(!replacement.AlbedoFactors.Materials.TryGetValue(pair.Key,out var next)||next with{FirstArgb=pair.Value.FirstArgb,SecondArgb=pair.Value.SecondArgb}!=pair.Value)
                throw new AssetPreviewException("Customization revision changed its albedo program/factor identity.");
        foreach(var pair in generated.Bindings)
            if(!faces.Bindings.TryGetValue(pair.Key,out var next)||next!=pair.Value)throw new AssetPreviewException("Customization revision changed its generated binding.");
        foreach(var pair in generated.Outputs)
            if(!faces.Outputs.TryGetValue(pair.Key,out var next)||next.Image.Mips.Count!=1||next.Image.Width!=pair.Value.Image.Width||next.Image.Height!=pair.Value.Image.Height||
                next.Image.ColorSpace!=pair.Value.Image.ColorSpace||next.Image.Format!=pair.Value.Image.Format||next.Image.AlphaMode!=pair.Value.Image.AlphaMode||
                next.Image.Mips[0].Rgba.Length!=(long)next.Image.Width*next.Image.Height*4)
                throw new AssetPreviewException("Customization revision changed its generated image shape.");
        var previousImages=generated.Outputs.Values.Select(v=>v.Image).ToHashSet(ReferenceEqualityComparer.Instance);
        var nextImages=faces.Outputs.Values.Select(v=>v.Image).ToHashSet(ReferenceEqualityComparer.Instance);
        try
        {
            // Prepare all uploads before changing any published material; no frame await occurs during commit.
            foreach(var output in faces.Outputs.Values)textures.Get(output.Image);
            foreach(var pair in materials)SkeletalAlbedoPreview.Apply(pair.Value,replacement.AlbedoFactors.Materials[pair.Key]);
            foreach(var pair in generatedMaterials)
            {
                SkeletalAlbedoPreview.Apply(pair.Value.Material,replacement.AlbedoFactors.Materials[pair.Value.Path]);
                pair.Value.Material.SetShaderParameter("albedo_texture",textures.Get(faces.Outputs[faces.Bindings[pair.Key].RendererPath].Image));
            }
        }
        catch
        {
            foreach(var pair in materials)SkeletalAlbedoPreview.Apply(pair.Value,source.AlbedoFactors.Materials[pair.Key]);
            foreach(var pair in generatedMaterials)
            {
                SkeletalAlbedoPreview.Apply(pair.Value.Material,source.AlbedoFactors.Materials[pair.Value.Path]);
                pair.Value.Material.SetShaderParameter("albedo_texture",textures.Get(generated.Outputs[generated.Bindings[pair.Key].RendererPath].Image));
            }
            foreach(var output in faces.Outputs.Values)if(!previousImages.Contains(output.Image))textures.Release(output.Image);
            throw;
        }
        var previous=generated;source=replacement;generated=faces;
        foreach(var output in previous.Outputs.Values)if(!nextImages.Contains(output.Image))textures.Release(output.Image);
    }
    internal int SelectedTextureValue(ShaderCustomizationKey key)=>source.TextureSelection!.Values[key];
    internal void ApplyTextureChoices(SkeletalTextureChoiceSelection selection)
    {
        if(source.CompressedNormals is null)throw new AssetPreviewException("Texture choice display requires the normal diffuse policy.");
        var expected=SkeletalTextureChoices.Apply(source,selection.Values).TextureSelection!;
        if(selection.References.Count!=expected.References.Count||selection.Selections.Count!=expected.Selections.Count)throw new AssetPreviewException("Texture choice mesh set differs.");
        foreach(var pair in expected.References)
            if(!selection.References.TryGetValue(pair.Key,out var next)||!next.Textures.SequenceEqual(pair.Value.Textures)||next.EffectPath!=pair.Value.EffectPath||!ReferenceEquals(next.Metadata,pair.Value.Metadata))throw new AssetPreviewException("Texture choice binding identity differs.");
        foreach(var pair in expected.Selections)
            if(!selection.Selections.TryGetValue(pair.Key,out var next)||!next.SequenceEqual(pair.Value)||next.Any(s=>s.TextureTag!=0x434E524D))throw new AssetPreviewException("Texture choice outside CNRM detail policy.");
        var previous=source;
        var all=materials.Select(p=>(Path:p.Key,Material:p.Value)).Concat(generatedMaterials.Values);
        void Bind()
        {
            foreach(var pair in all)if(source.CompressedNormals.Materials.ContainsKey(pair.Path))pair.Material.SetShaderParameter("normal_texture",NormalTexture(pair.Path));
        }
        try{source=source with{TextureSelection=expected};Bind();}
        catch{source=previous;Bind();throw;}
    }
    private T Own<T>(T value)where T:Resource{resources.Add(value);return value;}
    public void Dispose(){for(int i=resources.Count-1;i>=0;i--)resources[i].Dispose();resources.Clear();materials.Clear();generatedMaterials.Clear();textures.Dispose();}
}
