using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

public enum PortalCellLightKind { Ambient=0,Directional=1,Point=2 }
public sealed record PortalCellLight(PortalCellLightKind Kind,Vector4 DiffuseRgba,Vector4 SpecularRgba,
    AppearanceTransform AuthoredTransform,AppearanceTransform RuntimeTransform,Vector3 Attenuation)
{
    public int RuntimeLightType=>Kind==PortalCellLightKind.Point?3:(int)Kind;
    public bool HasSpecularRgb=>SpecularRgba.X!=0||SpecularRgba.Y!=0||SpecularRgba.Z!=0;
    public Vector3 TowardLight=>-RuntimeTransform.TransformDirection(Vector3.UnitZ);
}

/// <summary>Owned CELL/0004 and /0005 LGHT records; raw HDR colors/coefficients and authored pose are preserved.</summary>
public static class PortalCellLightsDecoder
{
    public static IReadOnlyList<PortalCellLight> Decode(PortalCell cell,int maximumLights=64)
    {
        ArgumentNullException.ThrowIfNull(cell);
        if(cell.Version is not ("0004" or "0005")||cell.Lights.Count>1)throw new InvalidDataException("Unsupported typed cell-light envelope.");
        if(maximumLights is <0 or >1024)throw new ArgumentOutOfRangeException(nameof(maximumLights));
        if(cell.Lights.Count==0)return Array.AsReadOnly(Array.Empty<PortalCellLight>());
        var chunk=cell.Lights[0];if(chunk.Tag!="LGHT"||chunk.Payload.Length<4)throw new InvalidDataException("Invalid LGHT chunk.");
        var raw=chunk.Payload.Span;int count=BinaryPrimitives.ReadInt32LittleEndian(raw);
        if(count<0||count>maximumLights||raw.Length!=4+(long)count*93)throw new InvalidDataException("Cell-light count/width/budget.");
        var result=new PortalCellLight[count];
        float angle=MathF.PI,c=MathF.Cos(angle),s=MathF.Sin(angle);
        var correction=new AppearanceTransform(new(c,0,s),Vector3.UnitY,new(-s,0,c),Vector3.Zero);
        for(int i=0;i<count;i++)
        {
            var row=raw.Slice(4+i*93,93);int kind=unchecked((sbyte)row[0]);
            if(kind is <0 or >2)throw new InvalidDataException("Unsupported original cell-light type.");
            var values=new float[23];for(int j=0;j<values.Length;j++)
            {
                values[j]=BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(row[(1+j*4)..]));
                if(!float.IsFinite(values[j])||Math.Abs(values[j])>1e6)throw new InvalidDataException("Bounded finite cell-light scalar required.");
            }
            Vector4 Color(int offset)=>new(values[offset+1],values[offset+2],values[offset+3],values[offset]);
            var authored=new AppearanceTransform(new(values[8],values[9],values[10]),new(values[12],values[13],values[14]),new(values[16],values[17],values[18]),new(values[11],values[15],values[19]));
            result[i]=new((PortalCellLightKind)kind,Color(0),Color(4),authored,authored.Compose(correction),new(values[20],values[21],values[22]));
        }
        return Array.AsReadOnly(result);
    }
}
