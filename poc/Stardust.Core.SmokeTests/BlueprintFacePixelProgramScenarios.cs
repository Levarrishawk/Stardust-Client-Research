using System.Text;
using Stardust.Core.Assets;

internal static class BlueprintFacePixelProgramScenarios
{
    // Independently authored instruction fixture for the documented shader-model 1.1 operation sequence.
    internal static byte[] Fixture(uint? changedToken=null,int changedIndex=0)
    {
        uint[] words={0xFFFF0101,0x42,0xB00F0000,0x05,0x80070000,0xB0E40000,0xA0E40003,0x40000001,0x80080000,0xB0FF0000,0xFFFF};
        if(changedToken.HasValue)words[changedIndex]=changedToken.Value;
        return MeshScenarios.Form("PSHP",MeshScenarios.Form("0000",MeshScenarios.Chunk("PSRC",Encoding.ASCII.GetBytes("synthetic face program\0")),
            MeshScenarios.Chunk("PEXE",words.SelectMany(BitConverter.GetBytes).ToArray())));
    }
    internal static void Run()
    {
        byte[] input=Fixture();var program=BlueprintFacePixelProgramDecoder.Decode(input);Array.Clear(input);
        if(program.TextureStage!=0||program.FactorRegister!=3||program.Metadata.Children[0].Children[1].Payload.Length!=44)throw new Exception("Owned face instruction classification differs.");
        byte[] valid=Fixture();for(int i=0;i<valid.Length;i++)HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintFacePixelProgramDecoder.Decode(valid.AsSpan(0,i)));
        for(int i=0;i<11;i++)HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintFacePixelProgramDecoder.Decode(Fixture(0,i)));
        Console.WriteLine("PASS: synthetic face pixel-program instruction classification, source ownership, truncation and every altered-token gate");
    }
}
