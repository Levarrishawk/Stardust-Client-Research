using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Column-vector affine mapping: three rows followed by their translations in the authored 3x4 layout.</summary>
public readonly record struct AppearanceTransform(Vector3 RowX, Vector3 RowY, Vector3 RowZ, Vector3 Translation)
{
    public static AppearanceTransform Identity => new(Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ,Vector3.Zero);
    public Vector3 TransformPoint(Vector3 point) => TransformDirection(point)+Translation;
    public Vector3 TransformDirection(Vector3 value) => new(Vector3.Dot(RowX,value),Vector3.Dot(RowY,value),Vector3.Dot(RowZ,value));
    /// <summary>Apply local first, then this parent. No engine coordinate conversion occurs here.</summary>
    public AppearanceTransform Compose(AppearanceTransform local)
    {
        Vector3 Column(int i) => i switch {0=>new(local.RowX.X,local.RowY.X,local.RowZ.X),1=>new(local.RowX.Y,local.RowY.Y,local.RowZ.Y),_=>new(local.RowX.Z,local.RowY.Z,local.RowZ.Z)};
        Vector3 Row(Vector3 row) => new(Vector3.Dot(row,Column(0)),Vector3.Dot(row,Column(1)),Vector3.Dot(row,Column(2)));
        return new(Row(RowX),Row(RowY),Row(RowZ),TransformPoint(local.Translation));
    }
}

public sealed record ComponentAppearancePart(string ChildPath,AppearanceTransform LocalTransform);
public sealed record ComponentAppearanceDescription(string Version,uint? RadarFlags,IReadOnlyList<ComponentAppearancePart> Parts,IffNode Metadata,DetailVisibilityGeometry? RadarGeometry=null,AppearanceHardpointTable? Hardpoints=null);

/// <summary>Owned deployed CMPA 0003-0005 template structure; base extents and RADR semantics remain metadata.</summary>
public static class ComponentAppearanceDecoder
{
    public static ComponentAppearanceDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode root=IffDocument.Parse(source).Root;
        Require(root.IsForm("CMPA") && root.Children.Count==1,"Invalid component appearance envelope.");
        IffNode version=root.Children[0];
        Require(version.FormType is "0003" or "0004" or "0005","Unsupported component appearance version.");
        IReadOnlyList<IffNode> children=version.Children;
        Require(children.Count>0 && children[0].IsForm("APPR"),"Missing component appearance base.");
        int cursor=1; uint? radar=null;DetailVisibilityGeometry? radarGeometry=null;
        if(version.FormType=="0005")
        {
            Require(cursor<children.Count && children[cursor].IsForm("RADR"),"Missing component RADR policy.");
            IffNode policy=children[cursor++];
            DetailAppearancePolicy parsed=DetailAppearanceDecoder.ReadPolicy(policy);
            radar=parsed.Flags;radarGeometry=parsed.Geometry;
        }
        Require(children.Count-cursor<=1024,"Component part budget exceeded.");
        List<ComponentAppearancePart> parts=new();
        while(cursor<children.Count)
        {
            IffNode part=children[cursor++];
            Require(part.Tag=="PART","Unexpected component appearance child.");
            ReadOnlySpan<byte> bytes=part.Payload.Span;
            int terminator=bytes.IndexOf((byte)0);
            Require(terminator>0 && bytes.Length-terminator-1==48,"Invalid component PART width.");
            string path=StaticEffectDecoder.ReadNamePrefix(bytes[..(terminator+1)],false);
            if(!path.StartsWith("appearance/",StringComparison.Ordinal))path="appearance/"+path;
            ReadOnlySpan<byte> transform=bytes[(terminator+1)..];
            float[] m=new float[12];
            for(int i=0;i<m.Length;i++)
            {
                m[i]=BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(transform[(i*4)..]));
                Require(float.IsFinite(m[i]),"Nonfinite component transform.");
            }
            parts.Add(new(path,new(new(m[0],m[1],m[2]),new(m[4],m[5],m[6]),new(m[8],m[9],m[10]),new(m[3],m[7],m[11]))));
        }
        return new(version.FormType!,radar,parts.AsReadOnly(),root,radarGeometry,AppearanceHardpointDecoder.DecodeBase(children[0]));
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
