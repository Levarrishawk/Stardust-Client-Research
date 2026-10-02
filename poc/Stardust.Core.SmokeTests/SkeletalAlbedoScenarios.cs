using System.Numerics;
using System.Text;
using Stardust.Core.Assets;

internal static class SkeletalAlbedoScenarios
{
    internal static byte[] Program(bool mask,bool comment=false,bool malformedComment=false,bool alter=false)
    {
        // Authored standard PS1.1 fixture: sample/vertex diffuse, one factor or alpha LRP, specular add, opacity.
        List<uint> words=new(){0xFFFF0101};if(comment)words.AddRange(new uint[]{malformedComment?0x7FFFFFFEu:0x0001FFFEu,0x11223344});
        words.AddRange(new uint[]{0x42,0xB00F0000,0x42,0xB00F0001,5,0x80070000,0xB0E40000,0x90E40000});
        words.AddRange(mask?new uint[]{18,0x80070001,0xB0FF0000,0xA0E40004,0xA0E40003,5,0xB0070000,0xB0FF0001,0x90E40001,4,0x80070000,0x80E40000,0x80E40001,0xB0E40000}:new uint[]{5,0x80070001,0xB0FF0001,0x90E40001,4,0x80070000,0x80E40000,0xA0E40003,0x80E40001});
        words.AddRange(new uint[]{0x40000001,0x80080000,0xA0FF0007,0xFFFF});if(alter)words[^3]^=1;
        return MeshScenarios.Form("PSHP",MeshScenarios.Form("0000",MeshScenarios.Chunk("PSRC",Encoding.ASCII.GetBytes("synthetic albedo program\0")),MeshScenarios.Chunk("PEXE",words.SelectMany(BitConverter.GetBytes).ToArray())));
    }
    internal static void Run()
    {
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        foreach(bool mask in new[]{false,true})foreach(bool comment in new[]{false,true})
        {
            byte[] bytes=Program(mask,comment);var decoded=SkeletalAlbedoProgramDecoder.Decode(bytes);Array.Clear(bytes);
            if(decoded.UsesAlphaHueMask!=mask||decoded.Metadata.FormType!="PSHP")throw new Exception("Albedo program ownership/semantic kind differs.");
            byte[] valid=Program(mask,comment);for(int i=0;i<valid.Length;i++)Reject(()=>SkeletalAlbedoProgramDecoder.Decode(valid.AsSpan(0,i)));
            Reject(()=>SkeletalAlbedoProgramDecoder.Decode(Program(mask,comment,alter:true)));
        }
        Reject(()=>SkeletalAlbedoProgramDecoder.Decode(Program(true,true,malformedComment:true)));
        var factors=new SkeletalAlbedoFactors("synthetic",true,0x4D41494E,0x48554542,0x00800000,0xFF00FF00);
        foreach(var (alpha,expected) in new[]{(0f,new Vector3(64/255f,0,0)),(1f,new Vector3(0,.25f,0)),(.5f,new Vector3(32/255f,.125f,0))})
            if(Vector3.Distance(SkeletalAlbedoEvaluator.Evaluate(new(.5f,.25f,1,alpha),factors),expected)>2e-7)throw new Exception("Hand-calculated alpha hue interpolation differs.");
        if(Vector3.Distance(SkeletalAlbedoEvaluator.Evaluate(new(.5f,.25f,1,1),factors with{UsesAlphaHueMask=false}),new(64/255f,0,0))>2e-7)throw new Exception("Single factor used texture alpha.");
        foreach(var invalid in new[]{new Vector4(float.NaN,0,0,0),new Vector4(0,0,0,-.1f),new Vector4(1.1f,0,0,0)})Reject(()=>SkeletalAlbedoEvaluator.Evaluate(invalid,factors));
        StaticShaderReferenceDescription Shader(byte[] payload,bool duplicate=false)=>StaticShaderReferences.Decode(MeshScenarios.Form("SSHT",MeshScenarios.Form("0000",MeshScenarios.Chunk("NAME",Encoding.ASCII.GetBytes("synthetic.eft\0")),MeshScenarios.Form("TFNS",MeshScenarios.Chunk("0000",duplicate?payload.Concat(payload).ToArray():payload)))));
        byte[] map=BitConverter.GetBytes(0x4D41494Eu).Concat(BitConverter.GetBytes(0x12345678u)).ToArray();var owned=StaticShaderFactors.Decode(Shader(map));Array.Clear(map);
        if(owned[0x4D41494E]!=0x12345678)throw new Exception("Static factor ownership differs.");
        Reject(()=>StaticShaderFactors.Decode(Shader(new byte[7])));Reject(()=>StaticShaderFactors.Decode(Shader(new byte[8],duplicate:true)));
        int reads=0;HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(_=>{reads++;return new(TreLookupKind.Missing);},"object/test.iff",new string('0',64),new string('0',64),loadAlbedoFactors:true));
        if(reads!=0)throw new Exception("Invalid albedo option performed lookup.");
        Console.WriteLine("PASS: owned PS1.1 albedo instruction/comment recognition, truncation/altered-token gates, hand-calculated alpha-mask/single factor, static map ownership/duplicates and invalid-option gate");
    }
}
