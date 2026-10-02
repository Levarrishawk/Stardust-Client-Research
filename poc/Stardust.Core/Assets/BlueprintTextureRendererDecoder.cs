using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record BlueprintRenderSet(uint ShaderIndex,IReadOnlyList<uint> VertexArrayIndices);
public sealed record BlueprintPrepareOperation(string Kind,uint TargetIndex,uint TextureTag,uint? SourceIndex,
    uint? SourceCount,string? VariableName,string? PalettePath,byte? RawFactoryFlag);
public sealed record BlueprintTextureRendererDescription(uint TemplateValue,uint Width,uint Height,
    IReadOnlyList<uint> DestinationFormatIds,IReadOnlyList<string> ShaderPaths,IReadOnlyList<string> TexturePaths,
    IReadOnlyList<IffNode> VertexArrays,float CameraScalar,IReadOnlyList<BlueprintRenderSet> RenderSets,
    IReadOnlyList<IReadOnlyList<BlueprintPrepareOperation>> PrepareLists);

/// <summary>Bounded deployed BTRT0001 face description; no resource execution or generated texture.</summary>
public static class BlueprintTextureRendererDecoder
{
    public static BlueprintTextureRendererDescription Decode(ReadOnlySpan<byte> source)
    {
        var root=IffDocument.Parse(source,new(MaximumBytes:1_048_576,MaximumNodes:16384,MaximumDepth:16)).Root;
        Need(root.IsForm("BTRT")&&root.Children.Count==1&&root.Children[0].IsForm("0001"));
        var parts=root.Children[0].Children;
        string[] order={"INFO","DEST","SHTM","TXTS","VBS ","IBS ","CAM ","RCMS","PCMS"};
        Need(parts.Count==order.Length);
        for(int i=0;i<parts.Count;i++)Need(i<2?parts[i].Tag==order[i]:parts[i].IsForm(order[i]));
        uint template=Word(parts[0]);var destination=parts[1].Payload.Span;
        Need(destination.Length>=12);uint width=U32(destination),height=U32(destination[4..]),formats=U32(destination[8..]);
        Need(width is >0 and <=4096&&height is >0 and <=4096&&formats<=32&&destination.Length==12+formats*4);
        var formatIds=new uint[formats];for(int i=0;i<formatIds.Length;i++)formatIds[i]=U32(destination[(12+i*4)..]);
        var shaderNodes=Table(parts[2],256);var shaders=shaderNodes.Select(n=>{Need(n.Tag=="NAME");int at=0;string p=Text(n.Payload.Span,ref at);Need(at==n.Payload.Length);return LegacyAssetPath.Normalize(p);}).ToArray();
        Need(parts[3].Children.Count==1&&parts[3].Children[0].Tag=="INFO");var textureData=parts[3].Children[0].Payload.Span;Need(textureData.Length>=4);
        uint count=U32(textureData);Need(count<=4096);var textures=new string[count];int cursor=4;
        for(int i=0;i<textures.Length;i++)textures[i]=LegacyAssetPath.Normalize(Text(textureData,ref cursor));Need(cursor==textureData.Length);
        var vertices=Table(parts[4],256);
        foreach(var vertex in vertices)
        {
            Need(vertex.IsForm("VTXA")&&vertex.Children.Count==1&&vertex.Children[0].IsForm("0003"));var chunks=vertex.Children[0].Children;
            Need(chunks.Count==2&&chunks[0].Tag=="INFO"&&chunks[0].Payload.Length==8&&chunks[1].Tag=="DATA");var info=chunks[0].Payload.Span;
            uint declaration=U32(info),size=U32(info[4..]);Need(declaration==0x1101&&size is >=3 and <=262144&&chunks[1].Payload.Length==(long)size*20);
            var bytes=chunks[1].Payload.Span;for(int i=0;i<bytes.Length;i+=4)Need(float.IsFinite(F32(bytes[i..])));
        }
        Need(Table(parts[5],256).Count==0); // Indexed/dormant command paths are outside this live slice.
        Need(parts[6].Children.Count==1&&parts[6].Children[0].Tag=="PCAM");float camera=F32Exact(parts[6].Children[0]);Need(float.IsFinite(camera)&&camera>0);
        var renders=new List<BlueprintRenderSet>();
        foreach(var render in Table(parts[7],256))
        {
            Need(render.IsForm("SRSC")&&render.Children.Count>=1&&render.Children[0].Tag=="INFO"&&render.Children[0].Payload.Length==8);
            var info=render.Children[0].Payload.Span;uint shader=U32(info),commands=U32(info[4..]);Need(shader<shaders.Length&&commands<=256&&render.Children.Count==commands+1);
            var indices=new List<uint>();foreach(var command in render.Children.Skip(1)){Need(command.Tag=="RTFC");uint index=Word(command);Need(index<vertices.Count);indices.Add(index);}
            renders.Add(new(shader,indices.AsReadOnly()));
        }
        var prepares=new List<IReadOnlyList<BlueprintPrepareOperation>>();
        foreach(var prepare in Table(parts[8],1024))
        {
            Need(prepare.IsForm("PCMD")&&prepare.Children.Count==1&&prepare.Children[0].IsForm("TOPS"));var operations=new List<BlueprintPrepareOperation>();
            foreach(var operation in Table(prepare.Children[0],256))
            {
                var bytes=operation.Payload.Span;uint target,tag;uint? first=null,range=null;string? variable=null,palette=null;byte? flag=null;
                if(operation.Tag=="SSTC"){Need(bytes.Length==12);target=U32(bytes);tag=U32(bytes[4..]);first=U32(bytes[8..]);Need(first<textures.Length);}
                else if(operation.Tag=="SST1")
                {
                    Need(bytes.Length>=17);target=U32(bytes);tag=U32(bytes[4..]);first=U32(bytes[8..]);int at=12;variable=Text(bytes,ref at);Need(bytes.Length-at==4);range=U32(bytes[at..]);
                    Need(range>0&&(ulong)first.Value+range.Value<=(ulong)textures.Length);
                }
                else if(operation.Tag=="STFP")
                {
                    Need(bytes.Length>=9);target=BinaryPrimitives.ReadUInt16LittleEndian(bytes);tag=U32(bytes[2..]);int at=6;palette=LegacyAssetPath.Normalize(Text(bytes,ref at));variable=Text(bytes,ref at);Need(bytes.Length-at==1);flag=bytes[at];Need(flag<=1);
                }
                else throw new InvalidDataException("Unsupported blueprint prepare operation.");
                Need(target<shaders.Length);operations.Add(new(operation.Tag,target,tag,first,range,variable,palette,flag));
            }
            prepares.Add(operations.AsReadOnly());
        }
        return new(template,width,height,Array.AsReadOnly(formatIds),Array.AsReadOnly(shaders),Array.AsReadOnly(textures),vertices,camera,renders.AsReadOnly(),prepares.AsReadOnly());
    }
    private static IReadOnlyList<IffNode> Table(IffNode node,uint maximum)
    {Need(node.Children.Count>=1&&node.Children[0].Tag=="INFO");uint count=Word(node.Children[0]);Need(count<=maximum&&node.Children.Count==count+1);return Array.AsReadOnly(node.Children.Skip(1).ToArray());}
    private static uint Word(IffNode node){Need(node.Payload.Length==4);return U32(node.Payload.Span);}
    private static float F32Exact(IffNode node){Need(node.Payload.Length==4);return F32(node.Payload.Span);}
    private static uint U32(ReadOnlySpan<byte> bytes)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    private static float F32(ReadOnlySpan<byte> bytes)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));
    private static string Text(ReadOnlySpan<byte> bytes,ref int at)
    {Need(at<=bytes.Length);int length=bytes[at..].IndexOf((byte)0);Need(length is >0 and <=1024);var text=bytes.Slice(at,length);foreach(byte b in text)Need(b is >=32 and <=126);at+=length+1;return Encoding.ASCII.GetString(text);}
    private static void Need(bool condition){if(!condition)throw new InvalidDataException("Invalid or unsupported BTRT live face description.");}
}
