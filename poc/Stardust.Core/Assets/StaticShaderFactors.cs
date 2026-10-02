using System.Buffers.Binary;
using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

/// <summary>Owned TFNS0000 tag/ARGB values; absence is explicit rather than guessed white.</summary>
public static class StaticShaderFactors
{
    public static IReadOnlyDictionary<uint,uint> Decode(StaticShaderReferenceDescription shader)
    {
        ArgumentNullException.ThrowIfNull(shader);var result=new Dictionary<uint,uint>();
        var maps=shader.Metadata.Children[0].Children.Where(n=>n.IsForm("TFNS")).ToArray();Need(maps.Length<=1);
        if(maps.Length==1)
        {
            Need(maps[0].Children.Count==1&&maps[0].Children[0].Tag=="0000");var bytes=maps[0].Children[0].Payload.Span;
            Need(bytes.Length%8==0&&bytes.Length<=512);
            for(int i=0;i<bytes.Length;i+=8)Need(result.TryAdd(BinaryPrimitives.ReadUInt32LittleEndian(bytes[i..]),BinaryPrimitives.ReadUInt32LittleEndian(bytes[(i+4)..])));
        }
        return new ReadOnlyDictionary<uint,uint>(result);
    }
    private static void Need(bool value){if(!value)throw new InvalidDataException("Invalid or unsupported static shader factor map.");}
}
