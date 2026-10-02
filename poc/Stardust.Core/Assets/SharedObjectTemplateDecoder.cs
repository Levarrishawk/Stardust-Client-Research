using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record SharedTemplateLayer(string Family,string Version,string? ParentPath,IReadOnlyDictionary<string,ReadOnlyMemory<byte>> Parameters);
public sealed record SharedObjectTemplateDescription(IReadOnlyList<SharedTemplateLayer> Layers,string? ParentPath);

/// <summary>Owned bounded SCOT/STOT/SHOT structure; parameter payloads retain their distinct codecs.</summary>
public static class SharedObjectTemplateDecoder
{
    public static SharedObjectTemplateDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode root=IffDocument.Parse(source,new(MaximumBytes:1048576,MaximumNodes:4096,MaximumDepth:16)).Root;
        List<SharedTemplateLayer> layers=new();string? parent=null;bool first=true;
        void Visit(IffNode family)
        {
            Require(family.FormType is "SCOT" or "STOT" or "SHOT","Unsupported shared template family.");
            int cursor=0;string? inherited=null;
            if(family.Children.Count>0 && family.Children[0].IsForm("DERV"))
            {
                IffNode derv=family.Children[cursor++];Require(derv.Children.Count==1 && derv.Children[0].Tag=="XXXX","Invalid template derivation record.");
                string value=Text(derv.Children[0].Payload.Span,1024);inherited=value.Length==0?null:LegacyAssetPath.Normalize(value);
            }
            if(first){parent=inherited;first=false;}else Require(inherited==parent,"Different nested derivation paths are outside this template slice.");
            Require(cursor<family.Children.Count,"Missing shared template version.");IffNode version=family.Children[cursor++];
            bool supported=family.FormType switch{"SCOT"=>version.FormType is "0012" or "0013","STOT"=>version.FormType is "0007" or "0009","SHOT"=>version.FormType is "0007" or "0008" or "0009" or "0010",_=>false};
            Require(supported,"Unsupported shared template version.");
            var fields=version.Children;Require(fields.Count>0 && fields[0].Tag=="PCNT" && fields[0].Payload.Length==4,"Missing template parameter count.");
            uint count=BinaryPrimitives.ReadUInt32LittleEndian(fields[0].Payload.Span);Require(count<=512 && count==fields.Count-1,"Template parameter count/budget mismatch.");
            Dictionary<string,ReadOnlyMemory<byte>> parameters=new(StringComparer.Ordinal);
            foreach(IffNode field in fields.Skip(1))
            {
                Require(field.Tag=="XXXX" && field.FormType is null,"Invalid template parameter record.");ReadOnlySpan<byte> bytes=field.Payload.Span;
                int end=bytes.IndexOf((byte)0);Require(end is >0 and <=128,"Invalid template parameter name.");
                string name=Text(bytes[..(end+1)],128);Require(parameters.TryAdd(name,bytes[(end+1)..].ToArray()),"Duplicate template parameter.");
            }
            layers.Add(new(family.FormType!,version.FormType!,inherited,new ReadOnlyDictionary<string,ReadOnlyMemory<byte>>(parameters)));
            string? baseFamily=family.FormType switch{"SCOT"=>"STOT","STOT"=>"SHOT",_=>null};
            if(baseFamily is null)Require(cursor==family.Children.Count,"Unexpected root object template data.");
            else
            {
                Require(cursor==family.Children.Count-1 && family.Children[cursor].IsForm(baseFamily),"Missing or unexpected shared base family.");
                Visit(family.Children[cursor]);
            }
        }
        Visit(root);return new(layers.AsReadOnly(),parent);
    }
    internal static string Text(ReadOnlySpan<byte> bytes,int maximum)
    {
        Require(bytes.Length is >0 && bytes.Length<=maximum+1 && bytes[^1]==0 && bytes[..^1].IndexOf((byte)0)<0,"Invalid terminated template string.");
        foreach(byte value in bytes[..^1])Require(value is >=32 and <=126,"Template string outside ASCII slice.");
        return Encoding.ASCII.GetString(bytes[..^1]);
    }
    internal static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
}
