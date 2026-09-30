using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record ShaderTextureSelection(uint TextureTag, short FirstChoice, short ChoiceCount,
    string VariableName, byte RawPrivateFlag, short AuthoredDefault);
public sealed record ShaderPaletteFactor(string VariableName, byte RawPrivateFlag, uint FactorTag,
    string PalettePath, int AuthoredDefault);
public sealed record CustomizableShaderDescription(string Version, StaticShaderReferenceDescription InlineShader,
    IReadOnlyList<string> TextureChoices, IReadOnlyList<ShaderTextureSelection> TextureSelections,
    IReadOnlyList<ShaderPaletteFactor> PaletteFactors, IffNode Metadata);

/// <summary>Owned deployed CSHD0001 records. Does not apply customization or invent runtime variable values.</summary>
public static class CustomizableShaderDecoder
{
    public static CustomizableShaderDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode root=IffDocument.Parse(source).Root;
        Require(root.IsForm("CSHD") && root.Children.Count==1 && root.Children[0].IsForm("0001"),"Unsupported customizable shader version.");
        var children=root.Children[0].Children;int cursor=0;
        Require(children.Count>=1 && children[0].IsForm("SSHT"),"Customizable shader requires one inline static shader.");
        var shader=StaticShaderReferences.DecodeOwned(children[cursor++]);
        List<string> choices=new();List<ShaderTextureSelection> selections=new();List<ShaderPaletteFactor> factors=new();
        if(cursor<children.Count && children[cursor].IsForm("TXTR"))
        {
            var texture=children[cursor++].Children;
            Require(texture.Count==2 && texture[0].Tag=="DATA" && texture[1].IsForm("CUST"),"Invalid customizable texture layout.");
            var data=texture[0].Payload.Span;int position=0;short count=I16(data,ref position);
            Require(count is >=0 and <=4096,"Customizable texture choice count exceeded.");
            for(int i=0;i<count;i++)choices.Add(PathName(data,ref position,1024));
            Require(position==data.Length,"Customizable texture choice extent mismatch.");
            Require(texture[1].Children.Count<=4096,"Customizable texture selection count exceeded.");
            foreach(var node in texture[1].Children)
            {
                Require(node.Tag=="TX1D","Unsupported texture customization record.");data=node.Payload.Span;position=0;
                uint tag=U32(data,ref position);short first=I16(data,ref position),length=I16(data,ref position);
                string variable=Name(data,ref position,259);byte flag=U8(data,ref position);short authoredDefault=I16(data,ref position);
                Require(position==data.Length && first>=0 && length>0 && first+length<=choices.Count,"Invalid texture customization range/extent.");
                // Runtime clamps selected values. Preserve out-of-range authored defaults rather than substitute here.
                selections.Add(new(tag,first,length,variable,flag,authoredDefault));
            }
        }
        if(cursor<children.Count && children[cursor].IsForm("TFAC"))
        {
            var palette=children[cursor++].Children;Require(palette.Count<=4096,"Customizable palette factor count exceeded.");
            foreach(var node in palette)
            {
                Require(node.Tag=="PAL ","Unsupported palette customization record.");var data=node.Payload.Span;int position=0;
                string variable=Name(data,ref position,511);byte flag=U8(data,ref position);uint tag=U32(data,ref position);
                string path=PathName(data,ref position,511);int authoredDefault=unchecked((int)U32(data,ref position));
                Require(position==data.Length,"Palette customization extent mismatch.");factors.Add(new(variable,flag,tag,path,authoredDefault));
            }
        }
        Require(cursor==children.Count,"Unsupported customizable shader child/order.");
        return new("0001",shader,choices.AsReadOnly(),selections.AsReadOnly(),factors.AsReadOnly(),root);
    }
    private static string Name(ReadOnlySpan<byte> bytes,ref int cursor,int maximum)
    {
        Require(cursor<=bytes.Length,"Truncated customization name.");int length=bytes[cursor..].IndexOf((byte)0);
        Require(length is >0 && length<=maximum,"Invalid customization name length.");
        foreach(byte value in bytes.Slice(cursor,length))Require(value is >=0x20 and <=0x7E,"Invalid customization name encoding.");
        string name=Encoding.ASCII.GetString(bytes.Slice(cursor,length));cursor+=length+1;return name;
    }
    private static string PathName(ReadOnlySpan<byte> bytes,ref int cursor,int maximum)
    {
        string name=Name(bytes,ref cursor,maximum);
        try{return LegacyAssetPath.Normalize(name);}catch(ArgumentException){throw new InvalidDataException("Invalid customization resource path.");}
    }
    private static byte U8(ReadOnlySpan<byte> bytes,ref int cursor){Require(cursor<bytes.Length,"Truncated customization byte.");return bytes[cursor++];}
    private static short I16(ReadOnlySpan<byte> bytes,ref int cursor){Require(bytes.Length-cursor>=2,"Truncated customization word.");short value=BinaryPrimitives.ReadInt16LittleEndian(bytes[cursor..]);cursor+=2;return value;}
    private static uint U32(ReadOnlySpan<byte> bytes,ref int cursor){Require(bytes.Length-cursor>=4,"Truncated customization integer.");uint value=BinaryPrimitives.ReadUInt32LittleEndian(bytes[cursor..]);cursor+=4;return value;}
    private static void Require(bool valid,string message)=>StaticEffectDecoder.Require(valid,message);
}
