using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class DdsScenarios
{
    internal static void Run()
    {
        byte[] color = ColorBlock(0xF800, 0x001F, 0xE4E4E4E4);
        byte[] fixture = Fixture("DXT1", 4, 4, 1, color);
        DdsImage image = DdsDecoder.Decode(fixture);
        byte[] palette = { 255, 0, 0, 255, 0, 0, 255, 255, 170, 0, 85, 255, 85, 0, 170, 255 };
        if (!image.Mips[0].Rgba.Span.SequenceEqual(Enumerable.Range(0, 4).SelectMany(_ => palette).ToArray()))
            throw new Exception("BC1 four-color row order/palette mismatch.");
        Array.Clear(fixture);
        if (image.Mips[0].Rgba.Span[0] != 255) throw new Exception("DDS output borrowed source storage.");
        CheckPixel(DdsDecoder.Decode(Fixture("DXT1", 4, 4, 1, ColorBlock(0x001F, 0xF800, 0xE4E4E4E4))), 2, new byte[] { 127, 0, 127, 255 });
        CheckPixel(DdsDecoder.Decode(Fixture("DXT1", 4, 4, 1, ColorBlock(0x001F, 0xF800, 0xE4E4E4E4))), 3, new byte[] { 0, 0, 0, 0 });
        byte[] bc2 = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(bc2, 0xFEDCBA9876543210);
        ColorBlock(0x001F, 0xF800, 0xFFFFFFFF).CopyTo(bc2, 8);
        DdsImage explicitAlpha = DdsDecoder.Decode(Fixture("DXT3", 4, 4, 1, bc2));
        for (int pixel = 0; pixel < 16; pixel++) CheckPixel(explicitAlpha, pixel, new byte[] { 170, 0, 85, (byte)(pixel * 17) });
        foreach ((byte first, byte second, byte[] expected) in new[]
            { ((byte)255, (byte)0, new byte[] { 255, 0, 218, 182, 145, 109, 72, 36 }),
              ((byte)0, (byte)255, new byte[] { 0, 255, 51, 102, 153, 204, 0, 255 }) })
        {
            byte[] bc3 = new byte[16]; bc3[0] = first; bc3[1] = second;
            ulong indices = 0;
            for (int pixel = 0; pixel < 16; pixel++) indices |= (ulong)(pixel % 8) << (pixel * 3);
            for (int octet = 0; octet < 6; octet++) bc3[2 + octet] = (byte)(indices >> (octet * 8));
            ColorBlock(0x001F, 0xF800, 0xFFFFFFFF).CopyTo(bc3, 8);
            DdsImage interpolatedAlpha = DdsDecoder.Decode(Fixture("DXT5", 4, 4, 1, bc3));
            for (int pixel = 0; pixel < 16; pixel++) CheckPixel(interpolatedAlpha, pixel, new byte[] { 170, 0, 85, expected[pixel % 8] });
        }
        DdsImage clipped = DdsDecoder.Decode(Fixture("DXT1", 3, 5, 1,
            ColorBlock(0xF800, 0, 0).Concat(ColorBlock(0x001F, 0, 0)).ToArray()));
        if (clipped.Mips[0].Rgba.Length != 60) throw new Exception("Partial BC block clipping size mismatch.");
        CheckPixel(clipped, 11, new byte[] { 255, 0, 0, 255 }); CheckPixel(clipped, 12, new byte[] { 0, 0, 255, 255 });
        byte[] legacyTail=Fixture("DXT3",4,2,2,bc2.Concat(bc2).Concat(bc2).ToArray());Put(legacyTail,8,0x21007);
        Reject(()=>DdsDecoder.Decode(legacyTail));
        var retained=DdsDecoder.Decode(legacyTail,new(AllowLegacyTerminalBlockTail:true));
        if(retained.Mips.Count!=2 || retained.RetainedTerminalBlock.Length!=16 || !retained.RetainedTerminalBlock.Span.SequenceEqual(bc2))throw new Exception("Declared mip/owned terminal block policy differs.");
        Array.Clear(legacyTail);if(retained.RetainedTerminalBlock.Span[7]!=bc2[7])throw new Exception("Terminal block borrowed caller storage.");
        foreach(int tail in new[]{1,15,17,32})Reject(()=>DdsDecoder.Decode(Fixture("DXT3",4,2,2,new byte[32+tail]),new(AllowLegacyTerminalBlockTail:true)));
        Reject(()=>DdsDecoder.Decode(Fixture("DXT3",4,2,2,new byte[48]),new(AllowLegacyTerminalBlockTail:true)));
        Reject(()=>DdsDecoder.Decode(Fixture("DXT5",4,2,2,new byte[48]),new(AllowLegacyTerminalBlockTail:true)));
        byte[] mips = Fixture("DXT1", 4, 4, 3, color.Concat(color).Concat(color).ToArray());
        DdsImage chain = DdsDecoder.Decode(mips);
        if (!chain.Mips.Select(mip => mip.Width).SequenceEqual(new[] { 4, 2, 1 }) || chain.Mips[2].Rgba.Length != 4)
            throw new Exception("DDS mip chain mismatch.");
        for (int length = 0; length < mips.Length; length++) Reject(() => DdsDecoder.Decode(mips.AsSpan(0, length)));
        Reject(() => DdsDecoder.Decode(mips.Concat(new byte[] { 0 }).ToArray()));
        foreach ((int offset, uint value) in new[] { (4, 123u), (76, 31u), (8, 0u), (16, 0u), (12, uint.MaxValue),
            (28, 4u), (24, 2u), (112, 0x200u), (84, 0x30315844u), (80, 0x104u), (108, 0u) })
        {
            byte[] invalid = (byte[])mips.Clone(); Put(invalid, offset, value); Reject(() => DdsDecoder.Decode(invalid));
        }
        Reject(() => DdsDecoder.Decode(mips, new(MaximumDecodedBytes: 64)));
        Reject(() => DdsDecoder.Decode(mips, new(MaximumInputBytes: 128)));
        Reject(() => DdsDecoder.Decode(mips, new(MaximumDimension: 3)));
        byte[] bgra = Fixture("RGBA", 2, 1, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        CheckPixel(DdsDecoder.Decode(bgra), 0, new byte[] { 3, 2, 1, 4 });
        CheckPixel(DdsDecoder.Decode(bgra), 1, new byte[] { 7, 6, 5, 8 });
        byte[] padded = (byte[])bgra.Clone(); Put(padded, 8, 0x100F); Put(padded, 20, 12); Reject(() => DdsDecoder.Decode(padded));
        byte[] channel = new byte[] { 0, 255, 0x88, 0xC6, 0xFA, 0x88, 0xC6, 0xFA };
        byte[] expectedChannel = { 0, 255, 51, 102, 153, 204, 0, 255 };
        foreach (string encoding in new[] { "ATI1", "BC4U" })
        {
            DdsImage single = DdsDecoder.Decode(Fixture(encoding, 4, 4, 1, channel));
            for (int pixel = 0; pixel < 16; pixel++) CheckPixel(single, pixel, new byte[] { expectedChannel[pixel % 8], 0, 0, 255 });
        }
        foreach (string encoding in new[] { "ATI2", "BC5U" })
        {
            DdsImage dual = DdsDecoder.Decode(Fixture(encoding, 4, 4, 1, channel.Concat(channel).ToArray()));
            for (int pixel = 0; pixel < 16; pixel++) CheckPixel(dual, pixel, new byte[] { expectedChannel[pixel % 8], expectedChannel[pixel % 8], 0, 255 });
        }
        byte[] bc7Constant=Convert.FromHexString("402010000081FE7F0000000000000000");
        byte[] bc7Gradient=Convert.FromHexString("40C01F000401007F1032547698BADCFE");
        DdsImage constant7=DdsDecoder.Decode(Dx10(98,bc7Constant));
        for(int pixel=0;pixel<16;pixel++)CheckPixel(constant7,pixel,new byte[]{128,0,64,254});
        DdsImage gradient7=DdsDecoder.Decode(Dx10(99,bc7Gradient));
        int[] weights={0,4,9,13,17,21,26,30,34,38,43,47,51,55,60,64};
        for(int pixel=0;pixel<16;pixel++)
        {
            int weight=weights[pixel];
            CheckPixel(gradient7,pixel,new byte[]{(byte)((weight*254+32)>>6),(byte)((weight*128+32)>>6),
                (byte)(((64-weight)*64+32)>>6),(byte)((weight*254+32)>>6)});
        }
        DdsImage reserved7=DdsDecoder.Decode(Dx10(98,new byte[16]));
        if(reserved7.Mips[0].Rgba.Span.IndexOfAnyExcept((byte)0)>=0)throw new Exception("Reserved BC7 mode must produce zero RGBA.");
        byte[] edge7=Dx10(98,bc7Constant);Put(edge7,12,1);Put(edge7,16,1);
        CheckPixel(DdsDecoder.Decode(edge7),0,new byte[]{128,0,64,254});
        byte[] mips7=Dx10(98,bc7Constant.Concat(bc7Constant).Concat(bc7Constant).ToArray());Put(mips7,28,3);
        if(DdsDecoder.Decode(mips7).Mips.Count!=3)throw new Exception("BC7 small mip blocks lost.");
        Reject(()=>DdsDecoder.Decode(mips7,new(MaximumDecodedBytes:64)));
        Console.WriteLine("PASS: BC7 independent constant/interpolation fixtures, reserved zero mode, clipped pixels and mip budget");
        foreach (uint dxgi in new uint[] { 71, 72, 74, 75, 77, 78, 80, 83, 28, 29, 87, 91, 49, 61, 98, 99 })
        {
            byte[] payload = dxgi switch { 71 or 72 => color, 74 or 75 => bc2,
                77 or 78 => new byte[16], 98 or 99 => bc7Constant, 80 => channel, 83 => channel.Concat(channel).ToArray(),
                49 => new byte[32], 61 => new byte[16], _ => new byte[64] };
            byte[] modern = Dx10(dxgi, payload);
            DdsImage decoded = DdsDecoder.Decode(modern);
            DdsColorSpace space = dxgi is 29 or 72 or 75 or 78 or 91 or 99 ? DdsColorSpace.Srgb : DdsColorSpace.Linear;
            if (decoded.DxgiFormat != dxgi || decoded.ColorSpace != space || decoded.AlphaMode != DdsAlphaMode.Premultiplied)
                throw new Exception("DDS DX10 metadata lost or transformed.");
            if (dxgi == 80) CheckPixel(decoded, 3, new byte[] { 102, 0, 0, 255 });
            if (dxgi == 83) CheckPixel(decoded, 3, new byte[] { 102, 102, 0, 255 });
            for (int length = 0; length < modern.Length; length++) Reject(() => DdsDecoder.Decode(modern.AsSpan(0, length)));
        }
        foreach ((int offset, uint value) in new[] { (128, 97u), (128, 95u), (128, 81u), (128, 70u),
            (132, 4u), (136, 4u), (140, 0u), (140, 2u), (144, 5u), (144, 0x100u) })
        { byte[] invalid = Dx10(80, channel); Put(invalid, offset, value); Reject(() => DdsDecoder.Decode(invalid)); }
        Console.WriteLine("PASS: owned DDS BC1/BC2/BC3/BGRA pixels, alpha modes, clipped blocks, mips and malformed budgets");
        Console.WriteLine("PASS: DX10 typed BC1-5/RGBA/BGRA/R/RG, sRGB and alpha metadata, ATI aliases; unsupported surface/format gates");
    }

    internal static void ProbeOptional(string[] args)
    {
        byte[] bytes; string expected;
        if (args.Length == 4 && args[0] == "--dds-file" && args[2] == "--expected-sha256")
        { bytes = File.ReadAllBytes(args[1]); expected = args[3]; }
        else if (args.Length == 6 && args[2] == "--member" && args[4] == "--expected-sha256")
        { using TreArchive archive = new(File.OpenRead(args[1])); bytes = archive.Read(args[3]); expected = args[5]; }
        else throw new ArgumentException("Invalid DDS probe arguments.");
        string hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (!hash.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new Exception("DDS input identity mismatch.");
        DdsImage image = DdsDecoder.Decode(bytes);
        Console.WriteLine($"dds_sha256={hash} format={image.Format} width={image.Width} height={image.Height} mips={image.Mips.Count} color_space={image.ColorSpace} alpha_mode={image.AlphaMode}");
        for (int level = 0; level < image.Mips.Count; level++)
            Console.WriteLine($"mip={level} rgba_sha256={Convert.ToHexString(SHA256.HashData(image.Mips[level].Rgba.Span))}");
    }
    private static byte[] Dx10(uint dxgi, byte[] payload)
    {
        byte[] original = Fixture("DX10", 4, 4, 1, Array.Empty<byte>());
        byte[] result = new byte[148 + payload.Length]; original.CopyTo(result, 0);
        Put(result, 128, dxgi); Put(result, 132, 3); Put(result, 140, 1); Put(result, 144, 2);
        payload.CopyTo(result, 148); return result;
    }
    internal static byte[] Fixture(string format, int width, int height, int mips, byte[] data)
    {
        byte[] result = new byte[128 + data.Length]; "DDS "u8.CopyTo(result);
        Put(result, 4, 124); Put(result, 8, 0x1007); Put(result, 12, (uint)height); Put(result, 16, (uint)width);
        Put(result, 28, (uint)mips); Put(result, 76, 32); Put(result, 108, 0x1000);
        if (format == "RGBA")
        {
            Put(result, 80, 0x41); Put(result, 88, 32); Put(result, 92, 0x00FF0000);
            Put(result, 96, 0x0000FF00); Put(result, 100, 0x000000FF); Put(result, 104, 0xFF000000);
        }
        else { Put(result, 80, 4); System.Text.Encoding.ASCII.GetBytes(format).CopyTo(result, 84); }
        data.CopyTo(result, 128); return result;
    }
    private static byte[] ColorBlock(ushort first, ushort second, uint selectors)
    {
        byte[] result = new byte[8]; BinaryPrimitives.WriteUInt16LittleEndian(result, first);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(2), second); Put(result, 4, selectors); return result;
    }
    private static void CheckPixel(DdsImage image, int pixel, byte[] expected)
    {
        if (!image.Mips[0].Rgba.Span.Slice(pixel * 4, 4).SequenceEqual(expected)) throw new Exception("DDS pixel mismatch.");
    }
    private static void Put(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static void Reject(Action action) => HostAccountingScenarios.Reject<InvalidDataException>(action);
}
