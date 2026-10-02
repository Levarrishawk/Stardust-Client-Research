using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public readonly record struct BlueprintCustomizationKey(string VariableName,bool FactoryFlag);
public sealed record BlueprintPreparedShader(uint Slot,string ShaderPath,IReadOnlyDictionary<uint,string> TextureBindings,
    IReadOnlyDictionary<uint,uint> TextureFactors);
public sealed record BlueprintSelectedOperation(string Kind,uint TargetIndex,uint TextureTag,int? RequestedValue,
    int? SelectedValue,string ResourcePath,uint? Argb);
public sealed record BlueprintFacePreparation(IReadOnlyList<BlueprintPreparedShader> ShaderSlots,
    IReadOnlyList<BlueprintSelectedOperation> Operations);

/// <summary>Executes only the deployed unconditional SSTC/SST1/STFP preparation over owned dependencies and explicit values.</summary>
public static class BlueprintFacePreparer
{
    public static BlueprintFacePreparation Prepare(BlueprintTextureRendererDescription blueprint,BlueprintFaceResources resources,
        IReadOnlyDictionary<BlueprintCustomizationKey,int> values)
    {
        ArgumentNullException.ThrowIfNull(blueprint);ArgumentNullException.ThrowIfNull(resources);ArgumentNullException.ThrowIfNull(values);
        Need(values.Count<=4096 && blueprint.ShaderPaths.Count<=256,"Preparation budget.");
        var supplied=values.ToDictionary(pair=>pair.Key,pair=>pair.Value);
        var textures=new List<Dictionary<uint,string>>();var factors=new List<Dictionary<uint,uint>>();
        foreach(string path in blueprint.ShaderPaths)
        {
            Need(resources.Shaders.TryGetValue(path,out var shader),"Prepared shader dependency absent.");
            var bindings=new Dictionary<uint,string>();var colors=new Dictionary<uint,uint>();
            foreach(var binding in shader!.Textures)
                if(binding.AssetPath is string texture) { Need(resources.Textures.ContainsKey(texture),"Named shader texture absent.");bindings.Add(binding.Tag,texture); }
            foreach(var pair in StaticShaderFactors.Decode(shader))colors.Add(pair.Key,pair.Value);
            textures.Add(bindings);factors.Add(colors);
        }
        var selected=new List<BlueprintSelectedOperation>();
        foreach(var op in blueprint.PrepareLists.SelectMany(list=>list))
        {
            Need(selected.Count<4096 && op.TargetIndex<textures.Count,"Invalid preparation target/count.");int target=(int)op.TargetIndex;
            if(op.Kind=="SSTC" || op.Kind=="SST1")
            {
                Need(op.SourceIndex.HasValue,"Missing source index.");uint source=op.SourceIndex!.Value;int? request=null,choice=null;
                if(op.Kind=="SST1")
                {
                    Need(op.VariableName is not null && op.SourceCount is >0 and <=4096,"Invalid texture variable/range.");
                    Need(supplied.TryGetValue(new(op.VariableName!,true),out int value),"Explicit texture customization value required.");
                    request=value;choice=Math.Clamp(value,0,(int)op.SourceCount!.Value-1);Need((long)source+choice.Value<blueprint.TexturePaths.Count,"Prepared texture range out of bounds.");source+=(uint)choice.Value;
                }
                Need(source<blueprint.TexturePaths.Count,"Prepared source out of range.");string path=blueprint.TexturePaths[(int)source];
                Need(resources.Textures.ContainsKey(path),"Prepared texture absent.");textures[target][op.TextureTag]=path;
                selected.Add(new(op.Kind,op.TargetIndex,op.TextureTag,request,choice,path,null));
            }
            else if(op.Kind=="STFP")
            {
                Need(op.VariableName is not null && op.RawFactoryFlag is 0 or 1 && op.PalettePath is not null,"Invalid palette operation.");
                Need(supplied.TryGetValue(new(op.VariableName!,op.RawFactoryFlag==1),out int value),"Explicit palette customization value required.");
                Need(resources.Palettes.TryGetValue(op.PalettePath!,out var palette) && palette.Entries.Count>0,"Prepared palette absent/empty.");
                int index=value<0 || value>=palette!.Entries.Count?palette!.Entries.Count-1:value;
                uint color=palette!.Entries[index].OpaqueArgb;factors[target][op.TextureTag]=color;
                selected.Add(new(op.Kind,op.TargetIndex,op.TextureTag,value,index,op.PalettePath!,color));
            }
            else throw new InvalidDataException("Unsupported blueprint prepare operation.");
        }
        var slots=new List<BlueprintPreparedShader>();
        for(int i=0;i<textures.Count;i++)
        {
            Need(resources.Shaders[blueprint.ShaderPaths[i]].Textures.All(binding=>textures[i].ContainsKey(binding.Tag)),"Unbound prepared shader texture.");
            slots.Add(new((uint)i,blueprint.ShaderPaths[i],new ReadOnlyDictionary<uint,string>(textures[i]),new ReadOnlyDictionary<uint,uint>(factors[i])));
        }
        return new(slots.AsReadOnly(),selected.AsReadOnly());
    }
    private static void Need(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
