using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class CompressedKeyframeScenarios
{
    internal static void Run()
    {
        byte[] source=Fixture();var clip=CompressedKeyframeDecoder.Decode(source);Array.Clear(source);
        if(clip.FrameRate!=30||clip.EndFrame!=10||clip.Transforms[0].TranslationMask!=8||clip.AnimatedRotations[0].Keys[1].Value!=uint.MaxValue||clip.AnimatedTranslations[0].Keys[1].Value!=2||clip.StaticTranslations.Count!=3)throw new Exception("Owned compressed clip field/bit preservation mismatch.");
        void Reject(Action a)=>HostAccountingScenarios.Reject<InvalidDataException>(a);
        byte[] valid=Fixture();for(int i=0;i<valid.Length;i++)Reject(()=>CompressedKeyframeDecoder.Decode(valid.AsSpan(0,i)));
        Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(rate:float.NaN)));Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(rate:0)));
        Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(rotation:1)));Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(mask:1)));Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(mask:16)));
        Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(lastTime:0)));Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(lastTime:11)));
        Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(value:float.NaN)));Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(version:"0000")));
        Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(count:-1)));Reject(()=>CompressedKeyframeDecoder.Decode(Fixture(tail:true)));
        byte[] message=MeshScenarios.Form("MSGS",MeshScenarios.Chunk("INFO",new byte[]{7}));
        byte[] locomotion=MeshScenarios.Form("LOCT",MeshScenarios.Chunk("INFO",new byte[]{9}));
        byte[] sparse=Sparse(message,locomotion);var retained=CompressedKeyframeDecoder.Decode(sparse);Array.Clear(sparse);
        if(retained.Transforms.Count!=0||retained.UninterpretedSuffix.Count!=2||retained.UninterpretedSuffix[0].Children[0].Payload.Span[0]!=7)throw new Exception("Owned opaque suffix/sparse channels mismatch.");
        Reject(()=>CompressedKeyframeDecoder.Decode(Sparse(message,message)));
        Reject(()=>CompressedKeyframeDecoder.Decode(Sparse(locomotion,message)));
        var identity=PackedQuaternionDecoder.Decode(new(254,254,254,0));
        if(identity.Repaired||identity.Reconstructed.W!=1||identity.X!=0||identity.Y!=0||identity.Z!=0)throw new Exception("Packed identity mismatch.");
        var overshoot=PackedQuaternionDecoder.Decode(new(252,252,232,4192340735));
        if(!overshoot.Repaired||overshoot.Radicand>=0||overshoot.Reconstructed.W!=0||Math.Abs(overshoot.Publication.Length()-1)>1e-6)throw new Exception("Explicit packed overshoot repair mismatch.");
        Reject(()=>PackedQuaternionDecoder.Decode(new(127,254,254,0)));Reject(()=>PackedQuaternionDecoder.Decode(new(254,255,254,0)));
        for(int selector=128;selector<=254;selector++)foreach(uint word in new[]{0u,uint.MaxValue,0x7FEFFC00u})
        {
            var decoded=PackedQuaternionDecoder.Decode(new((byte)selector,(byte)selector,(byte)selector,word));
            if(!float.IsFinite(decoded.Publication.W)||Math.Abs(decoded.Publication.Length()-1)>1e-6)throw new Exception("Selector sweep publication mismatch.");
        }
        Console.WriteLine("PASS: packed quaternion identity, all deployed selector publication gates, explicit negative-radicand diagnostic/repair and non-deployed selector rejection");
        Console.WriteLine("PASS: owned CKAT 0001 counted transforms/channels, preserved packed quaternion bits, finite translation keys, monotonic clip-bounded times and invalid width/version/flag/index/float/tail gates");
    }
    private static byte[] Sparse(params byte[][] suffix)
    {
        byte[] info=BitConverter.GetBytes(30f).Concat(new short[]{10,0,0,0,0,0}.SelectMany(BitConverter.GetBytes)).ToArray();
        return MeshScenarios.Form("CKAT",MeshScenarios.Form("0001",new[]{MeshScenarios.Chunk("INFO",info),MeshScenarios.Form("XFRM")}.Concat(suffix).ToArray()));
    }
    private static byte[] Fixture(float rate=30,ushort rotation=0,byte mask=8,ushort lastTime=10,float value=2,string version="0001",short count=1,bool tail=false)
    {
        byte[] info=BitConverter.GetBytes(rate).Concat(new short[]{10,count,1,1,1,3}.SelectMany(BitConverter.GetBytes)).ToArray();
        byte[] transform=Encoding.ASCII.GetBytes("root\0").Concat(new byte[]{1}).Concat(BitConverter.GetBytes(rotation)).Append(mask).Concat(new ushort[]{0,1,2}.SelectMany(BitConverter.GetBytes)).ToArray();
        byte[] rotationKeys=BitConverter.GetBytes((ushort)2).Concat(new byte[]{0,0,0}).Concat(BitConverter.GetBytes((ushort)0)).Concat(BitConverter.GetBytes(0u)).Concat(BitConverter.GetBytes(lastTime)).Concat(BitConverter.GetBytes(uint.MaxValue)).ToArray();
        byte[] translationKeys=BitConverter.GetBytes((ushort)2).Concat(BitConverter.GetBytes((ushort)0)).Concat(BitConverter.GetBytes(0f)).Concat(BitConverter.GetBytes(lastTime)).Concat(BitConverter.GetBytes(value)).ToArray();
        var parts=new List<byte[]>{MeshScenarios.Chunk("INFO",info),MeshScenarios.Form("XFRM",MeshScenarios.Chunk("XFIN",transform)),MeshScenarios.Form("AROT",MeshScenarios.Chunk("QCHN",rotationKeys)),MeshScenarios.Chunk("SROT",new byte[7]),MeshScenarios.Form("ATRN",MeshScenarios.Chunk("CHNL",translationKeys)),MeshScenarios.Chunk("STRN",new float[]{0,1,2}.SelectMany(BitConverter.GetBytes).ToArray())};
        if(tail)parts.Add(MeshScenarios.Chunk("JUNK",Array.Empty<byte>()));return MeshScenarios.Form("CKAT",MeshScenarios.Form(version,parts.ToArray()));
    }
    internal static string CanonicalHash(CompressedKeyframeDescription clip)
    {
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
        w.Write(clip.FrameRate);w.Write(clip.EndFrame);w.Write(clip.Transforms.Count);
        foreach(var t in clip.Transforms){w.Write(Encoding.ASCII.GetBytes(t.Name));w.Write((byte)0);w.Write(t.AnimatedRotation);w.Write(t.RotationIndex);w.Write(t.TranslationMask);w.Write(t.TranslationX);w.Write(t.TranslationY);w.Write(t.TranslationZ);}
        w.Write(clip.AnimatedRotations.Count);foreach(var channel in clip.AnimatedRotations){w.Write(channel.XSelector);w.Write(channel.YSelector);w.Write(channel.ZSelector);w.Write(channel.Keys.Count);foreach(var key in channel.Keys){w.Write(key.Frame);w.Write(key.Value);}}
        w.Write(clip.StaticRotations.Count);foreach(var q in clip.StaticRotations){w.Write(q.XSelector);w.Write(q.YSelector);w.Write(q.ZSelector);w.Write(q.Value);}
        w.Write(clip.AnimatedTranslations.Count);foreach(var channel in clip.AnimatedTranslations){w.Write(channel.Keys.Count);foreach(var key in channel.Keys){w.Write(key.Frame);w.Write(key.Value);}}
        w.Write(clip.StaticTranslations.Count);foreach(float value in clip.StaticTranslations)w.Write(value);
        w.Write(clip.UninterpretedSuffix.Count);foreach(var node in clip.UninterpretedSuffix){w.Write(Encoding.ASCII.GetBytes(node.FormType??node.Tag));w.Write(node.Payload.Length);w.Write(node.Payload.Span);}
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle)
    {
        var selected=bundle.SelectedAnimation!;var clip=CompressedKeyframeDecoder.Create(selected.Clip);
        Console.WriteLine("compressed_animation_clip="+JsonSerializer.Serialize(new{path=selected.Proxy.ClipPath,clip.FrameRate,clip.EndFrame,transforms=clip.Transforms.Count,animated_rotations=clip.AnimatedRotations.Count,static_rotations=clip.StaticRotations.Count,animated_translations=clip.AnimatedTranslations.Count,static_translations=clip.StaticTranslations.Count,rotation_keys=clip.AnimatedRotations.Sum(c=>c.Keys.Count),translation_keys=clip.AnimatedTranslations.Sum(c=>c.Keys.Count),suffix=clip.UninterpretedSuffix.Select(n=>n.FormType??n.Tag).ToArray(),sha256=CanonicalHash(clip)}));
        var body=bundle.Skeletons![bundle.Appearance.Skeletons[0].Path].ExposedLevels()[0];
        Console.WriteLine("compressed_animation_binding="+JsonSerializer.Serialize(new{matched=clip.Transforms.Count(t=>body.FindTransform(t.Name)>=0),unmatched=clip.Transforms.Where(t=>body.FindTransform(t.Name)<0).Select(t=>t.Name).ToArray(),playback=false}));
        int ordinal=0;
        foreach(var channel in clip.AnimatedRotations)foreach(var key in channel.Keys)Print(new(channel.XSelector,channel.YSelector,channel.ZSelector,key.Value));
        foreach(var value in clip.StaticRotations)Print(value);
        void Print(PackedAnimationQuaternion value)
        {
            var decoded=PackedQuaternionDecoder.Decode(value);
            Console.WriteLine("packed_quaternion="+JsonSerializer.Serialize(new{ordinal=ordinal++,decoded.X,decoded.Y,decoded.Z,decoded.Radicand,W=decoded.Reconstructed.W,publication=new[]{decoded.Publication.X,decoded.Publication.Y,decoded.Publication.Z,decoded.Publication.W},decoded.Repaired}));
        }
    }
}
