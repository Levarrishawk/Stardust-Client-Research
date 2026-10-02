using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SkeletalCustomizationControl(ShaderCustomizationKey Key,int MaximumValue);

/// <summary>Explicit color/style selection over owned assets. Texture-choice controls and shape morphs remain separate.</summary>
public static class SkeletalAppearanceCustomization
{
    public static CreatureAppearanceDescriptorBundle Apply(CreatureAppearanceDescriptorBundle bundle,
        IReadOnlyDictionary<ShaderCustomizationKey,int> values,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(bundle);ArgumentNullException.ThrowIfNull(values);cancellation.ThrowIfCancellationRequested();
        if(values.Count>4096||bundle.Geometries is null||bundle.FaceBlueprints is null||bundle.FaceResources is null||
            bundle.MaterialBundle?.PaletteDefaults is null||bundle.MaterialBundle.AlbedoFactors is null)
            throw new InvalidDataException("Customization requires owned face/material/palette/program resources.");
        var snapshot=values.ToDictionary(p=>p.Key,p=>p.Value);var material=bundle.MaterialBundle;
        var faceKeys=bundle.FaceBlueprints.Values.SelectMany(b=>b.PrepareLists).SelectMany(p=>p).Where(op=>op.VariableName is not null)
            .Select(op=>new ShaderCustomizationKey(op.VariableName!,op.Kind=="SST1"||op.RawFactoryFlag==1)).ToHashSet();
        var required=faceKeys.Concat(material.PaletteDefaults.Values.SelectMany(p=>p.Shader.PaletteFactors)
            .Select(f=>new ShaderCustomizationKey(f.VariableName,f.RawPrivateFlag!=0))).ToHashSet();
        if(!required.SetEquals(snapshot.Keys))throw new InvalidDataException("Customization requires exactly its color/style namespace keys.");
        var palettes=new Dictionary<string,CustomizableShaderPaletteBundle>(StringComparer.Ordinal);
        foreach(var pair in material.PaletteDefaults)
        {
            cancellation.ThrowIfCancellationRequested();var original=pair.Value;
            PaletteDescription? Resolve(string path)=>original.MissingPalettePaths.Contains(path)?null:original.Palettes.TryGetValue(path,out var p)?p:throw new InvalidDataException("Owned customization palette absent.");
            var selected=CustomizableShaderPaletteQuery.Select(original.Shader,Resolve,snapshot);
            if(selected.Any(f=>f.UsedAuthoredDefault))throw new InvalidDataException("Implicit customization palette default.");
            // Recompute selection diagnostics while preserving asset-decoder diagnostics.
            var diagnostics=original.Diagnostics.Where(d=>!d.StartsWith("PaletteIndexFallback:",StringComparison.Ordinal)&&!d.StartsWith("EmptyPalette:",StringComparison.Ordinal)).ToList();
            foreach(var factor in selected)
            {
                if(factor.Color is null)diagnostics.Add($"EmptyPalette:{factor.PalettePath}:{factor.AuthoredIndex}");
                else if(factor.Color.SelectedIndex!=factor.RequestedIndex)diagnostics.Add($"PaletteIndexFallback:{factor.AuthoredIndex}:{factor.RequestedIndex}:0");
            }
            palettes.Add(pair.Key,original with{AuthoredFactors=selected,Diagnostics=diagnostics.AsReadOnly()});
        }
        var factors=new Dictionary<string,SkeletalAlbedoFactors>(StringComparer.Ordinal);
        foreach(var pair in material.AlbedoFactors.Materials)
        {
            palettes.TryGetValue(pair.Key,out var palette);
            factors.Add(pair.Key,SkeletalAlbedoFactorReader.WithPalette(material.Materials[pair.Key],palette,pair.Value));
        }
        var faceValues=faceKeys.ToDictionary(key=>new BlueprintCustomizationKey(key.VariableName,key.Private),key=>snapshot[key]);
        var generated=BlueprintGeneratedFaceBuilder.Create(bundle.Geometries,bundle.FaceBlueprints,bundle.FaceResources,faceValues,cancellation);
        var updated=material with{PaletteDefaults=new ReadOnlyDictionary<string,CustomizableShaderPaletteBundle>(palettes),
            AlbedoFactors=material.AlbedoFactors with{Materials=new ReadOnlyDictionary<string,SkeletalAlbedoFactors>(factors)}};
        return bundle with{MaterialBundle=updated,GeneratedFaces=generated,CustomizationValues=new ReadOnlyDictionary<ShaderCustomizationKey,int>(snapshot)};
    }

    public static IReadOnlyList<SkeletalCustomizationControl> DescribeControls(CreatureAppearanceDescriptorBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        if(bundle.FaceBlueprints is null||bundle.FaceResources is null||bundle.MaterialBundle?.PaletteDefaults is null)
            throw new InvalidDataException("Customization controls require owned face/palette descriptions.");
        var ranges=new Dictionary<ShaderCustomizationKey,int>();
        void Add(ShaderCustomizationKey key,int count)
        {
            if(count<1)throw new InvalidDataException("Empty customization control range.");
            ranges[key]=Math.Min(ranges.GetValueOrDefault(key,int.MaxValue),count-1);
        }
        foreach(var palette in bundle.MaterialBundle.PaletteDefaults.Values)foreach(var factor in palette.Shader.PaletteFactors)
            Add(new(factor.VariableName,factor.RawPrivateFlag!=0),palette.Palettes[factor.PalettePath].Entries.Count);
        foreach(var blueprint in bundle.FaceBlueprints.Values)foreach(var op in blueprint.PrepareLists.SelectMany(p=>p).Where(op=>op.VariableName is not null))
            Add(new(op.VariableName!,op.Kind=="SST1"||op.RawFactoryFlag==1),op.Kind=="SST1"?checked((int)op.SourceCount!.Value):bundle.FaceResources.Palettes[op.PalettePath!].Entries.Count);
        return Array.AsReadOnly(ranges.Select(p=>new SkeletalCustomizationControl(p.Key,p.Value)).ToArray());
    }

    public static IReadOnlyDictionary<ShaderCustomizationKey,int> LoadProfile(Stream source)
    {
        var values=BlueprintFaceCustomizationProfile.Load(source);
        return new ReadOnlyDictionary<ShaderCustomizationKey,int>(values.ToDictionary(p=>new ShaderCustomizationKey(p.Key.VariableName,p.Key.FactoryFlag),p=>p.Value));
    }
}
