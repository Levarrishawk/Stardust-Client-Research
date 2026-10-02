using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class LogicalAnimationProxyScenarios
{
    internal const string IdleTable="appearance/lat/all_m.lat",IdleName="emt_standing_idle_ambient_1_ag",IdleSha="798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B";
    internal static LogicalAnimationClipSelection IdleSelection=>new(IdleTable,IdleName,IdleSha);
    internal static void Run()
    {
        byte[] proxy=Proxy();var root=IffDocument.Parse(proxy).Root;var plain=LogicalAnimationProxyDecoder.Decode(root);Array.Clear(proxy);
        if(plain.ClipPath!="appearance/animation/test.ans"||plain.TimeScales.Count!=0||plain.PriorityMetadata!.Tag!="PUNF")throw new Exception("Owned animation proxy mismatch.");
        var scaled=LogicalAnimationProxyDecoder.Decode(IffDocument.Parse(Scale(2,Scale(.5f,Proxy()))).Root);
        if(!scaled.TimeScales.SequenceEqual(new[]{2f,.5f})||scaled.EffectiveTimeScale!=1)throw new Exception("Nested time-scale descriptor mismatch.");
        void Reject(Action a)=>HostAccountingScenarios.Reject<InvalidDataException>(a);
        foreach(float bad in new[]{0,-1,float.NaN,float.PositiveInfinity,1001})Reject(()=>LogicalAnimationProxyDecoder.Decode(IffDocument.Parse(Scale(bad,Proxy())).Root));
        Reject(()=>LogicalAnimationProxyDecoder.Decode(IffDocument.Parse(Proxy(version:"0001")).Root));Reject(()=>LogicalAnimationProxyDecoder.Decode(IffDocument.Parse(Proxy(path:"object/test.iff")).Root));
        byte[] deep=Proxy();for(int i=0;i<9;i++)deep=Scale(1,deep);Reject(()=>LogicalAnimationProxyDecoder.Decode(IffDocument.Parse(deep).Root));
        Reject(()=>LogicalAnimationProxyDecoder.Decode(IffDocument.Parse(MeshScenarios.Form("SSAT",MeshScenarios.Form("0000"))).Root));
        byte[] spat=MeshScenarios.Form("SPAT",MeshScenarios.Form("0000",MeshScenarios.Chunk("INFO",new byte[]{2}),Proxy(),Proxy("appearance/animation/second.ans")));
        byte[] ssat=MeshScenarios.Form("SSAT",MeshScenarios.Form("0000",MeshScenarios.Chunk("INFO",Encoding.ASCII.GetBytes("gender\0")),MeshScenarios.Form("ANMS",MeshScenarios.Chunk("INFO",new byte[]{1,0}),spat)));
        var nested=IffDocument.Parse(ssat).Root;
        if(LogicalAnimationProxyDecoder.Decode(nested,new[]{0,1}).ClipPath!="appearance/animation/second.ans")throw new Exception("Explicit counted SSAT/SPAT route mismatch.");
        foreach(var bad in new[]{new[]{-1},new[]{1},new[]{0,2},new[]{0,0,0},Enumerable.Repeat(0,9).ToArray()})Reject(()=>LogicalAnimationProxyDecoder.Decode(nested,bad));
        Console.WriteLine("PASS: explicit counted SSAT/SPAT child routes; selector state and speed blending are not inferred");
        Console.WriteLine("PASS: owned PXAT 0000/TSCL 0000 selected descriptors, retained priority metadata, scale composition and invalid path/version/float/depth/family gates");
    }
    private static byte[] Proxy(string path="appearance/animation/test.ans",string version="0000")=>MeshScenarios.Form("PXAT",MeshScenarios.Form(version,MeshScenarios.Chunk("INFO",Encoding.ASCII.GetBytes(path+"\0")),MeshScenarios.Chunk("PUNF",Encoding.ASCII.GetBytes("test\0test\0"))));
    private static byte[] Scale(float value,byte[] child)=>MeshScenarios.Form("TSCL",MeshScenarios.Form("0000",MeshScenarios.Chunk("INFO",BitConverter.GetBytes(value)),child));
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle)
    {
        var selected=bundle.SelectedAnimation??throw new Exception("Selected original clip missing.");
        Console.WriteLine("selected_logical_clip="+JsonSerializer.Serialize(new{table=selected.Selection.TablePath,name=selected.Selection.LogicalName,path=selected.Proxy.ClipPath,scales=selected.Proxy.TimeScales,metadata=selected.Proxy.PriorityMetadata?.Tag,kind=selected.Clip.FormType,version=selected.Clip.Children[0].FormType}));
    }
}
