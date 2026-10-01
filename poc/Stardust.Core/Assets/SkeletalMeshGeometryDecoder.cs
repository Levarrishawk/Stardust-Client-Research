using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record SkeletalTextureCoordinates(int Dimension,IReadOnlyList<float> Values);
public readonly record struct SkeletalMeshTriangle(int SourceIndex,short? OcclusionCombination,uint A,uint B,uint C);
public sealed record SkeletalShaderGeometry(string ShaderPath,IReadOnlyList<uint> PositionIndices,IReadOnlyList<uint>? NormalIndices,
    IReadOnlyList<uint>? TangentIndices,ReadOnlyMemory<byte>? VertexColors,IReadOnlyList<SkeletalTextureCoordinates> Coordinates,
    IReadOnlyList<SkeletalMeshTriangle> SerializedTriangles,IReadOnlyList<SkeletalMeshTriangle> Triangles);
public readonly record struct SkeletalTextureRendererBinding(uint ShaderIndex,uint TextureTag);
public sealed record SkeletalTextureRendererReference(string Path,IReadOnlyList<SkeletalTextureRendererBinding> Bindings);
public sealed record SkeletalMeshGeometryDescription(SkeletalMeshSkinSourceDescription Skin,IReadOnlyList<Vector4> Tangents,
    IReadOnlyList<SkeletalShaderGeometry> Shaders,IReadOnlyList<SkeletalTextureRendererReference> TextureRenderers,
    int SourceTriangles,int DegenerateTriangles,int Triangles);

/// <summary>Deployed MGN shader vertex maps/ITL/OITL geometry. Blend/occlusion execution and texture rendering are separate.</summary>
public static class SkeletalMeshGeometryDecoder
{
    public static SkeletalMeshGeometryDescription Decode(ReadOnlySpan<byte> source,int maximumVertices=262144,int maximumTriangles=262144)
        =>Create(SkeletalMeshSkinSourceDecoder.Decode(source),maximumVertices,maximumTriangles);

    public static SkeletalMeshGeometryDescription Create(SkeletalMeshSkinSourceDescription skin,int maximumVertices=262144,int maximumTriangles=262144)
    {
        ArgumentNullException.ThrowIfNull(skin);
        if(maximumVertices is <0 or >1048576||maximumTriangles is <0 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumVertices));
        var suffix=skin.UninterpretedSuffix;var dot=suffix.Where(n=>n.Tag=="DOT3").ToArray();Require(dot.Length<=1,"Duplicate global tangent array.");
        Vector4[] tangents=Array.Empty<Vector4>();
        if(dot.Length==1)
        {
            var raw=dot[0].Payload.Span;Require(raw.Length>=4,"Truncated tangent count.");int count=Count(U32(raw),262144);
            Require(raw.Length==4+count*16,"Invalid tangent width.");tangents=new Vector4[count];
            for(int i=0;i<count;i++){var p=raw[(4+i*16)..];tangents[i]=new(F32(p),F32(p[4..]),F32(p[8..]),F32(p[12..]));Require(float.IsFinite(tangents[i].X)&&float.IsFinite(tangents[i].Y)&&float.IsFinite(tangents[i].Z)&&float.IsFinite(tangents[i].W),"Nonfinite tangent.");}
        }
        var psdt=suffix.Where(n=>n.IsForm("PSDT")).ToArray();Require(psdt.Length==skin.InfoWords[7],"Shader count mismatch.");
        List<SkeletalShaderGeometry> shaders=new();int vertices=0,sourceCount=0,degenerate=0;
        foreach(var form in psdt)
        {
            var parts=form.Children;int cursor=0;Require(parts.Count>=3&&parts[cursor].Tag=="NAME","Invalid shader prefix.");
            string path=Name(parts[cursor++].Payload.Span);
            Require(cursor<parts.Count&&parts[cursor].Tag=="PIDX","Missing shader position map.");var raw=parts[cursor++].Payload.Span;Require(raw.Length>=4,"Truncated shader vertex count.");
            int count=Count(U32(raw),maximumVertices-vertices);vertices+=count;var positions=Indices(raw[4..],count,skin.Positions.Count);
            uint[]? normals=null,tangentIndices=null;ReadOnlyMemory<byte>? colors=null;
            if(cursor<parts.Count&&parts[cursor].Tag=="NIDX")normals=Indices(parts[cursor++].Payload.Span,count,skin.Normals.Count);
            if(cursor<parts.Count&&parts[cursor].Tag=="DOT3")tangentIndices=Indices(parts[cursor++].Payload.Span,count,tangents.Length);
            if(cursor<parts.Count&&parts[cursor].Tag=="VDCL"){Require(parts[cursor].Payload.Length==count*4,"Invalid vertex color width.");colors=parts[cursor++].Payload.ToArray();}
            List<SkeletalTextureCoordinates> coordinates=new();
            if(cursor<parts.Count&&parts[cursor].Tag=="TXCI")
            {
                var tx=parts[cursor++].Payload.Span;Require(tx.Length>=4,"Truncated texture set count.");int sets=Count(U32(tx),8);Require(tx.Length==4+sets*4,"Invalid texture set dimensions.");
                Require(cursor<parts.Count&&parts[cursor].IsForm("TCSF"),"Missing coordinate form.");var records=parts[cursor++].Children;Require(records.Count==sets,"Texture coordinate set count mismatch.");
                for(int set=0;set<sets;set++)
                {
                    int dimension=Count(U32(tx[(4+set*4)..]),4);Require(dimension>0&&records[set].Tag=="TCSD"&&records[set].Payload.Length==count*dimension*4,"Invalid texture coordinate width.");
                    var data=records[set].Payload.Span;float[] values=new float[count*dimension];for(int i=0;i<values.Length;i++){values[i]=F32(data[(i*4)..]);Require(float.IsFinite(values[i]),"Nonfinite texture coordinate.");}
                    coordinates.Add(new(dimension,Array.AsReadOnly(values)));
                }
            }
            Require(cursor<parts.Count&&parts[cursor].IsForm("PRIM")&&cursor+1==parts.Count,"Unknown/unordered shader suffix.");var primitives=parts[cursor].Children;
            Require(primitives.Count>=1&&primitives[0].Tag=="INFO"&&primitives[0].Payload.Length==4&&U32(primitives[0].Payload.Span)==primitives.Count-1,"Invalid draw primitive count.");
            List<SkeletalMeshTriangle> serialized=new(),retained=new();
            foreach(var primitive in primitives.Skip(1))
            {
                Require(primitive.Tag is "ITL " or "OITL","Unsupported skeletal draw primitive.");var data=primitive.Payload.Span;Require(data.Length>=4,"Truncated triangle count.");
                int triangles=Count(U32(data),maximumTriangles-sourceCount);int stride=primitive.Tag=="OITL"?14:12;Require(data.Length==4+triangles*stride,"Invalid triangle record width.");
                for(int i=0;i<triangles;i++)
                {
                    var record=data[(4+i*stride)..];short? occlusion=null;if(stride==14){occlusion=BinaryPrimitives.ReadInt16LittleEndian(record);record=record[2..];}
                    uint a=U32(record),b=U32(record[4..]),c=U32(record[8..]);Require(a<count&&b<count&&c<count,"Triangle index outside shader map.");
                    var triangle=new SkeletalMeshTriangle(sourceCount++,occlusion,a,b,c);serialized.Add(triangle);
                    Vector3 pa=skin.Positions[(int)positions[a]],pb=skin.Positions[(int)positions[b]],pc=skin.Positions[(int)positions[c]];
                    double ux=(double)pb.X-pa.X,uy=(double)pb.Y-pa.Y,uz=(double)pb.Z-pa.Z,vx=(double)pc.X-pa.X,vy=(double)pc.Y-pa.Y,vz=(double)pc.Z-pa.Z;
                    bool zero=uy*vz-uz*vy==0&&uz*vx-ux*vz==0&&ux*vy-uy*vx==0;if(zero)degenerate++;else retained.Add(triangle);
                }
            }
            shaders.Add(new(path,Array.AsReadOnly(positions),normals is null?null:Array.AsReadOnly(normals),tangentIndices is null?null:Array.AsReadOnly(tangentIndices),colors,coordinates.AsReadOnly(),serialized.AsReadOnly(),retained.AsReadOnly()));
        }
        List<SkeletalTextureRendererReference> renderers=new();var trts=suffix.Where(n=>n.IsForm("TRTS")).ToArray();Require(trts.Length<=1,"Duplicate texture renderer form.");
        if(trts.Length==1)
        {
            var parts=trts[0].Children;Require(parts.Count>=1&&parts[0].Tag=="INFO"&&parts[0].Payload.Length==8,"Invalid texture renderer INFO.");
            int count=Count(U32(parts[0].Payload.Span),4096),total=Count(U32(parts[0].Payload.Span[4..]),262144);Require(parts.Count==count+1,"Texture renderer count mismatch.");int used=0;
            foreach(var part in parts.Skip(1))
            {
                Require(part.Tag=="TRT ","Invalid texture renderer record.");var raw=part.Payload.Span;int end=raw.IndexOf((byte)0);Require(end>=0&&end<=1024&&raw.Length>=end+5,"Invalid texture renderer path.");string path=Name(raw[..(end+1)]);
                raw=raw[(end+1)..];int bindings=Count(U32(raw),total-used);Require(raw.Length==4+bindings*8,"Invalid texture renderer binding width.");var values=new SkeletalTextureRendererBinding[bindings];
                for(int i=0;i<bindings;i++){uint shader=U32(raw[(4+i*8)..]),tag=U32(raw[(8+i*8)..]);Require(shader<shaders.Count,"Texture renderer shader index outside geometry.");values[i]=new(shader,tag);}used+=bindings;renderers.Add(new(path,Array.AsReadOnly(values)));
            }
            Require(used==total,"Texture renderer total mismatch.");
        }
        return new(skin,Array.AsReadOnly(tangents),shaders.AsReadOnly(),renderers.AsReadOnly(),sourceCount,degenerate,sourceCount-degenerate);
    }
    private static uint[] Indices(ReadOnlySpan<byte> raw,int count,int bound)
    {
        Require(raw.Length==count*4,"Invalid shader index map width.");uint[] result=new uint[count];for(int i=0;i<count;i++){result[i]=U32(raw[(i*4)..]);Require(result[i]<bound,"Shader index outside source array.");}return result;
    }
    private static string Name(ReadOnlySpan<byte> raw){var value=SkeletalAppearanceDecoder.Strings(raw,1)[0];Require(value.Length>0,"Empty shader/renderer path.");return LegacyAssetPath.Normalize(value);}
    private static int Count(uint value,int maximum){Require(maximum>=0&&value<=maximum,"Skeletal geometry count budget.");return (int)value;}
    private static uint U32(ReadOnlySpan<byte> raw)=>BinaryPrimitives.ReadUInt32LittleEndian(raw);
    private static float F32(ReadOnlySpan<byte> raw)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(raw));
    private static void Require(bool value,string message)=>SkeletalAppearanceDecoder.Require(value,message);
}
