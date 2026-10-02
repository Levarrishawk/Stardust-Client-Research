using System.Text.Json;
using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public static class SkeletalAnimationBankProfile
{
    public static IReadOnlyDictionary<string,LogicalAnimationClipSelection> Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);using var document=JsonDocument.Parse(stream,new JsonDocumentOptions{MaxDepth=8});var root=document.RootElement;
        Properties(root,"version","animations");if(root.GetProperty("version").GetInt32()!=1)throw new InvalidDataException("Unsupported animation-bank profile version.");
        var entries=root.GetProperty("animations");if(entries.ValueKind!=JsonValueKind.Array||entries.GetArrayLength() is <1 or >8)throw new InvalidDataException("Animation-bank count budget.");
        var selections=new Dictionary<string,LogicalAnimationClipSelection>(StringComparer.Ordinal);
        foreach(var entry in entries.EnumerateArray())
        {
            Properties(entry,"key","table","name","sha256","route");string Text(string key)=>entry.GetProperty(key).GetString()??throw new InvalidDataException("Animation-bank string required.");
            string key=Text("key"),name=Text("name"),table=LegacyAssetPath.Normalize(Text("table")),hash=Text("sha256");LogicalAnimationTableDecoder.ValidateName(key);LogicalAnimationTableDecoder.ValidateName(name);
            if(hash.Length!=64||!hash.All(Uri.IsHexDigit)||!table.StartsWith("appearance/lat/",StringComparison.Ordinal))throw new InvalidDataException("Pinned animation-table selection required.");
            var route=entry.GetProperty("route");if(route.ValueKind!=JsonValueKind.Array||route.GetArrayLength()>8)throw new InvalidDataException("Animation-bank route budget.");
            int[] children=route.EnumerateArray().Select(value=>value.GetInt32()).ToArray();if(children.Any(value=>value is <0 or >4095))throw new InvalidDataException("Animation-bank child outside bounds.");
            if(!selections.TryAdd(key,new(table,name,hash.ToUpperInvariant(),Array.AsReadOnly(children))))throw new InvalidDataException("Duplicate animation-bank key.");
        }
        return new ReadOnlyDictionary<string,LogicalAnimationClipSelection>(selections);
    }
    private static void Properties(JsonElement element,params string[] names)
    {
        if(element.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Animation-bank object required.");var properties=element.EnumerateObject().Select(p=>p.Name).ToArray();
        if(properties.Length!=names.Length||properties.Distinct(StringComparer.Ordinal).Count()!=properties.Length||names.Any(name=>!properties.Contains(name,StringComparer.Ordinal)))throw new InvalidDataException("Animation-bank fields mismatch.");
    }
}
