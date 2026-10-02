using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SkeletalTextureChoiceSelection(IReadOnlyDictionary<ShaderCustomizationKey,int> Values,
    IReadOnlyDictionary<string,IReadOnlyList<SelectedShaderTexture>> Selections,
    IReadOnlyDictionary<string,StaticShaderReferenceDescription> References);

/// <summary>Explicit texture choice state over a complete owned choice closure; retains serialized metadata.</summary>
public static class SkeletalTextureChoices
{
    public static SkeletalMaterialBundle Apply(SkeletalMaterialBundle source,IReadOnlyDictionary<ShaderCustomizationKey,int> values,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(values);cancellationToken.ThrowIfCancellationRequested();
        if(source.Materials.Count>64||values.Count>4096)throw new InvalidDataException("Texture choice material/value budget.");
        var snapshot=values.ToDictionary(p=>p.Key,p=>p.Value);
        var keys=source.Materials.Values.Where(m=>m.Customization is not null).SelectMany(m=>m.Customization!.TextureSelections)
            .Select(s=>new ShaderCustomizationKey(s.VariableName,s.RawPrivateFlag!=0)).ToHashSet();
        if(!keys.SetEquals(snapshot.Keys))throw new InvalidDataException("Texture selection requires exact explicit namespace keys.");
        var selections=new Dictionary<string,IReadOnlyList<SelectedShaderTexture>>(StringComparer.Ordinal);
        var references=new Dictionary<string,StaticShaderReferenceDescription>(StringComparer.Ordinal);
        foreach(var pair in source.Materials)
        {
            cancellationToken.ThrowIfCancellationRequested();var material=pair.Value;
            var bindings=(material.Customization?.InlineShader??material.References).Textures.ToArray();
            var selected=material.Customization is null?Array.Empty<SelectedShaderTexture>():CustomizableShaderTextureQuery.Select(material.Customization,snapshot);
            foreach(var choice in selected)
            {
                if(choice.UsedAuthoredDefault)throw new InvalidDataException("Implicit texture-selection default.");
                // Match original target-membership semantics: absent tags do not create bindings.
                int index=Array.FindIndex(bindings,b=>b.Tag==choice.TextureTag);
                if(index>=0)
                {
                    if(!source.Textures.ContainsKey(choice.AssetPath))throw new InvalidDataException("Selected texture choice is not owned.");
                    bindings[index]=bindings[index] with{SourceFlag=0,AssetPath=choice.AssetPath};
                }
            }
            selections.Add(pair.Key,selected);references.Add(pair.Key,material.References with{Textures=Array.AsReadOnly(bindings)});
        }
        cancellationToken.ThrowIfCancellationRequested();
        return source with{TextureSelection=new(new ReadOnlyDictionary<ShaderCustomizationKey,int>(snapshot),
            new ReadOnlyDictionary<string,IReadOnlyList<SelectedShaderTexture>>(selections),new ReadOnlyDictionary<string,StaticShaderReferenceDescription>(references))};
    }
}
