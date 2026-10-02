using System.Collections.ObjectModel;
using System.Text.Json;

namespace Stardust.Core.Assets;

public readonly record struct BlueprintGeneratedTextureKey(string MeshPath,uint ShaderIndex,uint TextureTag);
public sealed record BlueprintGeneratedTextureBinding(string RendererPath,string ShaderPath);
public sealed record BlueprintGeneratedFaces(IReadOnlyDictionary<string,BlueprintComposedFace> Outputs,
    IReadOnlyDictionary<BlueprintGeneratedTextureKey,BlueprintGeneratedTextureBinding> Bindings,
    IReadOnlyDictionary<BlueprintCustomizationKey,int> Values);

public static class BlueprintGeneratedFaceBuilder
{
    public static BlueprintGeneratedFaces Create(IReadOnlyDictionary<string,SkeletalMeshGeometryDescription> geometries,
        IReadOnlyDictionary<string,BlueprintTextureRendererDescription> blueprints,BlueprintFaceResources resources,
        IReadOnlyDictionary<BlueprintCustomizationKey,int> values,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(geometries);ArgumentNullException.ThrowIfNull(blueprints);ArgumentNullException.ThrowIfNull(resources);ArgumentNullException.ThrowIfNull(values);
        Need(geometries.Count is >0 and <=64&&values.Count<=4096,"Generated face geometry/value budget.");
        Need(blueprints.Count is >0 and <=16,"Generated face count budget.");
        var supplied=values.ToDictionary(p=>p.Key,p=>p.Value);
        var required=blueprints.Values.SelectMany(b=>b.PrepareLists).SelectMany(p=>p).Where(op=>op.VariableName is not null)
            .Select(op=>new BlueprintCustomizationKey(op.VariableName!,op.Kind=="SST1"||op.RawFactoryFlag==1)).ToHashSet();
        Need(required.SetEquals(supplied.Keys),"Generated face requires exactly its explicit customization keys.");
        var bindings=new Dictionary<BlueprintGeneratedTextureKey,BlueprintGeneratedTextureBinding>();var referenced=new HashSet<string>(StringComparer.Ordinal);
        foreach(var mesh in geometries)foreach(var renderer in mesh.Value.TextureRenderers)
        {
            cancellation.ThrowIfCancellationRequested();Need(blueprints.ContainsKey(renderer.Path),"Generated face blueprint absent.");referenced.Add(renderer.Path);
            foreach(var target in renderer.Bindings)
            {
                Need(target.TextureTag==0x4D41494E&&target.ShaderIndex<mesh.Value.Shaders.Count,"Unsupported generated face destination.");
                var key=new BlueprintGeneratedTextureKey(mesh.Key,target.ShaderIndex,target.TextureTag);
                var binding=new BlueprintGeneratedTextureBinding(renderer.Path,mesh.Value.Shaders[(int)target.ShaderIndex].ShaderPath);
                if(bindings.TryGetValue(key,out var previous))Need(previous==binding,"Conflicting generated texture destination.");
                else {Need(bindings.Count<4096,"Generated face binding budget.");bindings.Add(key,binding);}
            }
        }
        Need(bindings.Count>0&&referenced.SetEquals(blueprints.Keys),"Generated face blueprint/binding closure differs.");
        var outputs=new Dictionary<string,BlueprintComposedFace>(StringComparer.Ordinal);long pixels=0;
        foreach(var pair in blueprints)
        {
            pixels+=(long)pair.Value.Width*pair.Value.Height;Need(pixels<=16_777_216,"Generated face total pixel budget.");
            var prepared=BlueprintFacePreparer.Prepare(pair.Value,resources,supplied);
            outputs.Add(pair.Key,BlueprintFaceCompositor.Compose(pair.Value,resources,prepared,cancellation:cancellation));
        }
        return new(new ReadOnlyDictionary<string,BlueprintComposedFace>(outputs),new ReadOnlyDictionary<BlueprintGeneratedTextureKey,BlueprintGeneratedTextureBinding>(bindings),new ReadOnlyDictionary<BlueprintCustomizationKey,int>(supplied));
    }
    private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
}

/// <summary>Strict explicit preview values, not recovered character-creation defaults.</summary>
public static class BlueprintFaceCustomizationProfile
{
    public static IReadOnlyDictionary<BlueprintCustomizationKey,int> Load(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);byte[] bytes=new byte[65537];int count=0;
        while(count<bytes.Length){int read=source.Read(bytes,count,bytes.Length-count);if(read==0)break;count+=read;}
        return Parse(bytes.AsMemory(0,count));
    }
    public static IReadOnlyDictionary<BlueprintCustomizationKey,int> Parse(ReadOnlyMemory<byte> utf8)
    {
        if(utf8.Length is <1 or >65536)throw Invalid();
        try
        {
            using var document=JsonDocument.Parse(utf8,new JsonDocumentOptions{MaxDepth=4});var root=document.RootElement;
            Fields(root,"version","values");if(root.GetProperty("version").GetInt32()!=1)throw Invalid();
            var values=root.GetProperty("values");if(values.ValueKind!=JsonValueKind.Array||values.GetArrayLength() is <1 or >4096)throw Invalid();
            var result=new Dictionary<BlueprintCustomizationKey,int>();
            foreach(var entry in values.EnumerateArray())
            {
                Fields(entry,"variableName","factoryFlag","value");string? name=entry.GetProperty("variableName").GetString();
                if(name is null||name.Length is <1 or >256||name.Any(c=>c<33||c>126))throw Invalid();
                var key=new BlueprintCustomizationKey(name,entry.GetProperty("factoryFlag").GetBoolean());
                if(!result.TryAdd(key,entry.GetProperty("value").GetInt32()))throw Invalid();
            }
            return new ReadOnlyDictionary<BlueprintCustomizationKey,int>(result);
        }
        catch(Exception error)when(error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException){throw Invalid();}
    }
    private static void Fields(JsonElement element,params string[] names)
    {
        if(element.ValueKind!=JsonValueKind.Object)throw Invalid();var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var property in element.EnumerateObject())if(!names.Contains(property.Name,StringComparer.Ordinal)||!seen.Add(property.Name))throw Invalid();
        if(seen.Count!=names.Length)throw Invalid();
    }
    private static InvalidDataException Invalid()=>new("Invalid explicit face customization profile.");
}
