using System.Buffers.Binary;
using System.Numerics;
using Stardust.Core.Assets;

internal static class DdsHdrScenarios
{
    internal static void Run()
    {
        // Mode 11 (prefix 00011): six direct ten-bit endpoints, zero indices.
        byte[] Constant(int endpoint)
        {
            BigInteger bits=3;
            for(int component=0;component<6;component++)bits|=(BigInteger)endpoint<<(5+10*component);
            byte[] data=new byte[16];bits.ToByteArray(isUnsigned:true,isBigEndian:false).CopyTo(data,0);return data;
        }
        foreach((uint dxgi,int endpoint,ushort expected) in new[]{(95u,512,(ushort)0x3E0F),(96u,256,(ushort)0x3E1F),(96u,1023,(ushort)0x805D)})
        {
            byte[] input=Fixture(dxgi,4,4,1,Constant(endpoint));DdsHdrImage image=DdsHdrDecoder.Decode(input);
            if(image.DxgiFormat!=dxgi || image.AlphaMode!=DdsAlphaMode.Opaque || image.Mips[0].RgbHalf.Length!=96)throw new Exception("HDR description mismatch.");
            for(int offset=0;offset<96;offset+=2)
                if(BinaryPrimitives.ReadUInt16LittleEndian(image.Mips[0].RgbHalf.Span[offset..])!=expected)throw new Exception("Independent BC6H direct endpoint unquantization mismatch.");
            Array.Clear(input);if(BinaryPrimitives.ReadUInt16LittleEndian(image.Mips[0].RgbHalf.Span)!=expected)throw new Exception("HDR output borrowed source.");
        }
        foreach(byte mode in new byte[]{19,23,27,31})
        {byte[] block=Enumerable.Repeat((byte)255,16).ToArray();block[0]=(byte)((block[0]&~31)|mode);if(DdsHdrDecoder.Decode(Fixture(96,1,1,1,block)).Mips[0].RgbHalf.Span.ContainsAnyExcept((byte)0))throw new Exception("Reserved BC6H mode output mismatch.");}
        byte[] chain=Fixture(95,4,4,3,Constant(512).Concat(Constant(0)).Concat(Constant(1023)).ToArray());
        DdsHdrImage decoded=DdsHdrDecoder.Decode(chain);
        if(!decoded.Mips.Select(mip=>mip.Width).SequenceEqual(new[]{4,2,1}))throw new Exception("HDR mip extents mismatch.");
        for(int prefix=0;prefix<chain.Length;prefix++)Reject(()=>DdsHdrDecoder.Decode(chain.AsSpan(0,prefix)));
        Reject(()=>DdsHdrDecoder.Decode(chain.Concat(new byte[]{0}).ToArray()));
        foreach((int offset,uint value) in new[]{(4,123u),(8,0u),(12,0u),(16,uint.MaxValue),(24,2u),(28,4u),(76,31u),(80,0u),(84,0u),(108,0u),(112,0x200u),(128,94u),(128,98u),(132,4u),(136,4u),(140,2u),(144,5u)})
        {byte[] invalid=(byte[])chain.Clone();Put(invalid,offset,value);Reject(()=>DdsHdrDecoder.Decode(invalid));}
        Reject(()=>DdsHdrDecoder.Decode(chain,new(MaximumDimension:3)));
        Reject(()=>DdsHdrDecoder.Decode(chain,new(MaximumDecodedBytes:125)));
        Reject(()=>DdsHdrDecoder.Decode(chain,new(MaximumInputBytes:148)));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>DdsHdrDecoder.Decode(chain,new(MaximumDecodedBytes:5)));
        // Incomplete declared chains are legal CPU data, but do not imply a complete GPU texture chain.
        if(DdsHdrDecoder.Decode(Fixture(95,4,4,2,Constant(0).Concat(Constant(0)).ToArray())).Mips.Count!=2)throw new Exception("HDR partial chain rejected.");
        byte[] clipped=Constant(512).Concat(Constant(0)).ToArray();
        ReadOnlySpan<byte> edge=DdsHdrDecoder.Decode(Fixture(95,3,5,1,clipped)).Mips[0].RgbHalf.Span;
        if(edge.Length!=90 || BinaryPrimitives.ReadUInt16LittleEndian(edge[66..])!=0x3E0F || BinaryPrimitives.ReadUInt16LittleEndian(edge[72..])!=0)throw new Exception("HDR edge block clipping mismatch.");
        Console.WriteLine("PASS: owned typed BC6H RGB half, signed/HDR endpoint vectors, reserved modes, edges, mips and malformed budgets");
    }
    internal static byte[] Fixture(uint dxgi,int width,int height,int count,byte[] payload)
    {
        byte[] original=DdsScenarios.Fixture("DXT5",width,height,count,payload),output=new byte[original.Length+20];
        original.AsSpan(0,128).CopyTo(output);payload.CopyTo(output,148);
        Put(output,84,0x30315844);Put(output,128,dxgi);Put(output,132,3);Put(output,140,1);Put(output,144,3);return output;
    }
    private static void Put(byte[] bytes,int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset),value);
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
