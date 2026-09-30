using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;
using Stardust.Core.Assets;

internal static class DdsHdrVerification
{
    internal static void Verify(string directory,bool gpu,bool materials=false)
    {
        string[] files=Directory.GetFiles(directory,"*.dds").OrderBy(value=>value,StringComparer.Ordinal).ToArray();
        if(files.Length is 0 or >256)throw new InvalidDataException("HDR fixture count limit.");
        int mips=0;long negative=0,aboveOne=0,negativeZero=0,negativeInfinity=0;
        int templates=0;
        foreach(string path in files)
        {
            if(new FileInfo(path).Length>67_108_864)throw new InvalidDataException("HDR fixture size limit.");
            byte[] source=File.ReadAllBytes(path);DdsHdrImage decoded=DdsHdrDecoder.Decode(source);
            int cursor=148;
            foreach(DdsHdrMip mip in decoded.Mips)
            {
                int length=((mip.Width+3)/4)*((mip.Height+3)/4)*16;
                using Image reference=Image.CreateFromData(mip.Width,mip.Height,false,
                    decoded.Format==DdsHdrFormat.Bc6Signed?Image.Format.BptcRgbf:Image.Format.BptcRgbfu,source.AsSpan(cursor,length).ToArray());
                cursor+=length;
                if(reference.Decompress()!=Error.Ok || reference.GetFormat()!=Image.Format.Rgbh)throw new InvalidOperationException("Independent BC6H decoder unavailable.");
                byte[] expected=reference.GetData(),actual=mip.RgbHalf.ToArray();
                for(int component=0;component<actual.Length;component+=2)
                {
                    ushort bits=BinaryPrimitives.ReadUInt16LittleEndian(actual.AsSpan(component));
                    if((bits&0x8000)!=0 && (bits&0x7fff)!=0)negative++;
                    if(bits>0x3c00 && bits<0x8000)aboveOne++;
                    if(bits==0x8000)negativeZero++;
                    if(bits==0xfc00)negativeInfinity++;
                }
                if(!expected.AsSpan().SequenceEqual(actual))
                {
                    int mismatch=Enumerable.Range(0,actual.Length/2).First(index=>!actual.AsSpan(index*2,2).SequenceEqual(expected.AsSpan(index*2,2)));
                    throw new InvalidOperationException($"BC6H reference mismatch {Path.GetFileName(path)} mip={mips} half={mismatch} core={BinaryPrimitives.ReadUInt16LittleEndian(actual.AsSpan(mismatch*2)):X4} ref={BinaryPrimitives.ReadUInt16LittleEndian(expected.AsSpan(mismatch*2)):X4}");
                }
                mips++;
            }
            if(gpu)
            {
                int required=1;for(int size=Math.Max(decoded.Width,decoded.Height);size>1;size>>=1)required++;
                if(decoded.Mips.Count!=1 && decoded.Mips.Count!=required)throw new InvalidDataException("HDR GPU gate requires complete chain or one mip.");
                byte[] data=decoded.Mips.SelectMany(mip=>mip.RgbHalf.ToArray()).ToArray();
                using Image image=Image.CreateFromData(decoded.Width,decoded.Height,decoded.Mips.Count>1,Image.Format.Rgbh,data);
                using ImageTexture texture=ImageTexture.CreateFromImage(image);using Image downloaded=texture.GetImage();
                if(downloaded.GetFormat()!=Image.Format.Rgbh || !downloaded.GetData().AsSpan().SequenceEqual(data))throw new InvalidOperationException("HDR GPU half readback mismatch.");
            }
            if(materials)
            {
                byte[] component=File.ReadAllBytes(Path.Combine(directory,"synthetic.cmp"));
                byte[] mesh=File.ReadAllBytes(Path.Combine(directory,"synthetic.msh"));
                byte[] shader=File.ReadAllBytes(Path.Combine(directory,"synthetic.sht"));byte[] effect=File.ReadAllBytes(Path.Combine(directory,"synthetic.eft"));
                TreLookupResult Resolve(string member)=>member switch
                {
                    "appearance/synthetic.cmp"=>new(TreLookupKind.Present,component),
                    "appearance/mesh/first.msh" or "appearance/mesh/second.msh"=>new(TreLookupKind.Present,mesh),
                    "shader/synthetic.sht"=>new(TreLookupKind.Present,shader),"effect/synthetic.eft"=>new(TreLookupKind.Present,effect),
                    "texture/synthetic.dds"=>new(TreLookupKind.Present,source),_=>new(TreLookupKind.Missing)
                };
                var graph=TexturedAppearanceGraphReader.Load(Resolve,"appearance/synthetic.cmp",Convert.ToHexString(SHA256.HashData(component)));
                if(graph.Meshes.Count!=2 || graph.Diagnostics.Count!=0 || graph.Sources.Count!=6)throw new InvalidOperationException("HDR material fixture graph scope mismatch.");
                var first=graph.Meshes.Values.First();var second=graph.Meshes.Values.Last();
                if(first.Textures.Count!=0 || !ReferenceEquals(first.HdrTextures.Values.Single(),second.HdrTextures.Values.Single()))throw new InvalidOperationException("HDR material decode sharing mismatch.");
                var image=first.HdrTextures.Values.Single();
                for(int level=0;level<decoded.Mips.Count;level++)if(!image.Mips[level].RgbHalf.Span.SequenceEqual(decoded.Mips[level].RgbHalf.Span))throw new InvalidOperationException("HDR bundle changed half pixels.");
                using GeometryAppearancePreview preview=new(graph.Geometry,graph.Meshes);preview.VerifySwitches();
                try
                {
                    if(preview.TexturedMeshTemplates!=2 || preview.TexturedSurfaces!=2 || preview.NeutralSurfaces!=0 || preview.UniqueGpuTextures!=1)throw new InvalidOperationException("HDR material/shared GPU binding mismatch.");
                    ImageTexture? previous=null;
                    for(int instance=0;instance<2;instance++)
                    {
                        var node=(MeshInstance3D)preview.Root.GetChild(instance).GetChild(0);
                        var material=(ShaderMaterial)node.Mesh.SurfaceGetMaterial(0);
                        if(material.Shader.Code.Contains("source_color",StringComparison.Ordinal) || material.GetShaderParameter("albedo_texture").AsGodotObject() is not ImageTexture bound)throw new InvalidOperationException("HDR MAIN material lost its linear half-float sampler.");
                        if(previous is not null && !ReferenceEquals(previous,bound))throw new InvalidOperationException("HDR MAIN material sampler did not share its texture.");
                        using Image readback=bound.GetImage();
                        if(readback.GetFormat()!=Image.Format.Rgbh || !readback.GetData().AsSpan().SequenceEqual(image.Mips.SelectMany(mip=>mip.RgbHalf.ToArray()).ToArray()))throw new InvalidOperationException("HDR material sampler readback mismatch.");
                        previous=bound;
                    }
                    templates+=preview.TexturedMeshTemplates;
                }
                finally{preview.Root.Free();}
            }
            GD.Print($"HDR fixture={Path.GetFileName(path)} sha256={Convert.ToHexString(SHA256.HashData(source))} mips={decoded.Mips.Count}");
        }
        GD.Print($"HDR components negative={negative} above_one={aboveOne} negative_zero={negativeZero} negative_infinity={negativeInfinity}");
        GD.Print($"PASS: independent BC6H half agreement fixtures={files.Length} mips={mips} gpu={gpu}");
        if(materials)GD.Print($"PASS: shared BC6H material bundles fixtures={files.Length} mips={mips} templates={templates} gpu={gpu}");
    }
}
