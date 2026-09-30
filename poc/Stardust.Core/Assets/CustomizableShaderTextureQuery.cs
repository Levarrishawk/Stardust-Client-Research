namespace Stardust.Core.Assets;

public readonly record struct ShaderCustomizationKey(string VariableName,bool Private);
public sealed record SelectedShaderTexture(int AuthoredIndex,uint TextureTag,int RequestedValue,
    int ClampedValue,string AssetPath,bool UsedAuthoredDefault);
public enum CustomizableShaderInspectionMode { Reject, AuthoredTextureDefaults }

/// <summary>Explicit inspection state: supplied exact namespace/name values, otherwise serialized authored defaults.</summary>
public static class CustomizableShaderTextureQuery
{
    public static IReadOnlyList<SelectedShaderTexture> Select(CustomizableShaderDescription shader,
        IReadOnlyDictionary<ShaderCustomizationKey,int>? values=null)
    {
        ArgumentNullException.ThrowIfNull(shader);
        if(values?.Count>4096)throw new InvalidDataException("Customization value budget exceeded.");
        var snapshot=values is null?new Dictionary<ShaderCustomizationKey,int>():values.ToDictionary(pair=>pair.Key,pair=>pair.Value);
        string[] choices=shader.TextureChoices.ToArray();var selections=shader.TextureSelections.ToArray();
        if(choices.Length>4096 || selections.Length>4096)throw new InvalidDataException("Customization selection budget exceeded.");
        List<SelectedShaderTexture> selected=new();
        for(int i=0;i<selections.Length;i++)
        {
            var control=selections[i];
            if(control.FirstChoice<0 || control.ChoiceCount<=0 || control.FirstChoice+control.ChoiceCount>choices.Length)throw new InvalidDataException("Invalid customization selection range.");
            bool supplied=snapshot.TryGetValue(new(control.VariableName,control.RawPrivateFlag!=0),out int requested);
            if(!supplied)requested=control.AuthoredDefault;
            int clamped=Math.Clamp(requested,0,control.ChoiceCount-1);
            selected.Add(new(i,control.TextureTag,requested,clamped,choices[control.FirstChoice+clamped],!supplied));
        }
        return selected.AsReadOnly();
    }
    internal static StaticShaderReferenceDescription ApplyAuthoredDefaults(CustomizableShaderDescription shader)
    {
        var bindings=shader.InlineShader.Textures.ToArray();
        foreach(var selection in Select(shader))
        {
            // Original setter first checks target membership; absent tags do not create bindings.
            int index=Array.FindIndex(bindings,binding=>binding.Tag==selection.TextureTag);
            if(index>=0)bindings[index]=bindings[index] with {SourceFlag=0,AssetPath=selection.AssetPath};
        }
        return shader.InlineShader with {Textures=Array.AsReadOnly(bindings)};
    }
}
