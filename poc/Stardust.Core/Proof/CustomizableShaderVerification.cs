using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

namespace Stardust.Core.Proof;

public static class CustomizableShaderVerification
{
    public static string ComputeSelectionsSha256(CustomizableShaderDescription shader,int? forcedValue=null)
    {
        var values=forcedValue.HasValue?shader.TextureSelections.Select(control=>new ShaderCustomizationKey(control.VariableName,control.RawPrivateFlag!=0)).Distinct().ToDictionary(key=>key,_=>forcedValue.Value):null;
        var selected=CustomizableShaderTextureQuery.Select(shader,values);
        using MemoryStream stream=new();using BinaryWriter writer=new(stream,Encoding.UTF8,true);writer.Write(selected.Count);
        foreach(var item in selected)
        {writer.Write(item.AuthoredIndex);writer.Write(item.TextureTag);writer.Write(item.RequestedValue);writer.Write(item.ClampedValue);byte[] name=Encoding.UTF8.GetBytes(item.AssetPath);writer.Write(name.Length);writer.Write(name);writer.Write(item.UsedAuthoredDefault);}
        writer.Flush();return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
    public static string ComputeSha256(CustomizableShaderDescription shader)
    {
        using MemoryStream stream=new();using BinaryWriter writer=new(stream,Encoding.UTF8,true);
        void Name(string value){byte[] bytes=Encoding.UTF8.GetBytes(value);writer.Write(bytes.Length);writer.Write(bytes);}
        writer.Write(shader.TextureChoices.Count);foreach(string path in shader.TextureChoices)Name(path);
        writer.Write(shader.TextureSelections.Count);
        foreach(var selection in shader.TextureSelections)
        {writer.Write(selection.TextureTag);writer.Write(selection.FirstChoice);writer.Write(selection.ChoiceCount);Name(selection.VariableName);writer.Write(selection.RawPrivateFlag);writer.Write(selection.AuthoredDefault);}
        writer.Write(shader.PaletteFactors.Count);
        foreach(var factor in shader.PaletteFactors)
        {Name(factor.VariableName);writer.Write(factor.RawPrivateFlag);writer.Write(factor.FactorTag);Name(factor.PalettePath);writer.Write(factor.AuthoredDefault);}
        writer.Flush();return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
}
