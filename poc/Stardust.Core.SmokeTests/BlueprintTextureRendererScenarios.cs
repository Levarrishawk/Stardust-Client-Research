using System.Text;
using Stardust.Core.Assets;

internal static class BlueprintTextureRendererScenarios
{
    internal static void Run()
    {
        byte[] Fixture(uint vertex=0,float camera=1,string operation="SSTC")
        {
            byte[] Words(params uint[] words)=>words.SelectMany(BitConverter.GetBytes).ToArray();
            byte[] Info(params uint[] words)=>MeshScenarios.Chunk("INFO",Words(words));
            byte[] Text(string value)=>Encoding.ASCII.GetBytes(value+"\0");
            return MeshScenarios.Form("BTRT",MeshScenarios.Form("0001",Info(1),MeshScenarios.Chunk("DEST",Words(256,256,1,4)),
                MeshScenarios.Form("SHTM",Info(1),MeshScenarios.Chunk("NAME",Text("shader/synthetic.sht"))),
                MeshScenarios.Form("TXTS",MeshScenarios.Chunk("INFO",Words(1).Concat(Text("texture/synthetic.dds")).ToArray())),
                MeshScenarios.Form("VBS ",Info(1),MeshScenarios.Form("VTXA",MeshScenarios.Form("0003",Info(0x1101,3),MeshScenarios.Chunk("DATA",new byte[60])))),
                MeshScenarios.Form("IBS ",Info(0)),MeshScenarios.Form("CAM ",MeshScenarios.Chunk("PCAM",BitConverter.GetBytes(camera))),
                MeshScenarios.Form("RCMS",Info(1),MeshScenarios.Form("SRSC",Info(0,1),MeshScenarios.Chunk("RTFC",Words(vertex)))),
                MeshScenarios.Form("PCMS",Info(1),MeshScenarios.Form("PCMD",MeshScenarios.Form("TOPS",Info(1),MeshScenarios.Chunk(operation,Words(0,0x4D41494E,0)))))));
        }
        byte[] input=Fixture();var owned=BlueprintTextureRendererDecoder.Decode(input);Array.Clear(input);
        if(owned.Width!=256||owned.TexturePaths[0]!="texture/synthetic.dds"||owned.RenderSets[0].VertexArrayIndices[0]!=0||owned.PrepareLists[0][0].SourceIndex!=0||owned.VertexArrays[0].Children[0].Children[1].Payload.Length!=60)throw new Exception("Owned synthetic blueprint mismatch.");
        byte[] valid=Fixture();for(int length=0;length<valid.Length;length++)HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintTextureRendererDecoder.Decode(valid.AsSpan(0,length)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintTextureRendererDecoder.Decode(Fixture(vertex:1)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintTextureRendererDecoder.Decode(Fixture(camera:float.NaN)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintTextureRendererDecoder.Decode(Fixture(operation:"NOP ")));
        var resourcesInput=new Dictionary<string,byte[]> { ["shader/synthetic.sht"]=MaterialScenarios.Shader(),["effect/synthetic.eft"]=MaterialScenarios.Effect(),["texture/synthetic.dds"]=DdsScenarios.Fixture("DXT1",4,4,1,new byte[8]) };
        var reads=new Dictionary<string,int>();
        TreLookupResult Resolve(string path){reads[path]=reads.GetValueOrDefault(path)+1;return resourcesInput.TryGetValue(path,out var bytes)?new(TreLookupKind.Present,bytes,7):new(TreLookupKind.Missing);}
        var resources=BlueprintFaceResourceReader.Load(owned with{ShaderPaths=new[]{"shader/synthetic.sht","shader/synthetic.sht"},TexturePaths=new[]{"texture/synthetic.dds","texture/synthetic.dds"}},Resolve);
        if(resources.Shaders.Count!=1||resources.Textures.Count!=1||reads.Values.Any(n=>n!=1))throw new Exception("Blueprint resource deduplication failed.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintFaceResourceReader.Load(owned,p=>p=="texture/synthetic.dds"?new(TreLookupKind.Masked):Resolve(p)));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>BlueprintFaceResourceReader.Load(owned,Resolve,new(MaximumResources:1)));
        uint tag=0x4D41494E;
        var preparedBlueprint=owned with {PrepareLists=new IReadOnlyList<BlueprintPrepareOperation>[] {new BlueprintPrepareOperation[]{new("SST1",0,tag,0,1,"choice",null,null),new("STFP",0,tag,null,null,"color","palette/synthetic.pal",0)}}};
        var palette=PaletteDecoder.Decode(PaletteScenarios.Fixture(new byte[]{10,20,30,0,100,110,120,255}));
        var preparedResources=resources with {Palettes=new Dictionary<string,PaletteDescription>{{"palette/synthetic.pal",palette}}};
        var values=new Dictionary<BlueprintCustomizationKey,int>{{new("choice",true),int.MinValue},{new("color",false),int.MinValue},{new("color",true),0}};
        var prepared=BlueprintFacePreparer.Prepare(preparedBlueprint,preparedResources,values);
        if(prepared.Operations[0].SelectedValue!=0||prepared.Operations[1].SelectedValue!=1||prepared.ShaderSlots[0].TextureFactors[tag]!=0xFF646E78)throw new Exception("Texture clamp/palette last-entry/factory identity differs.");
        values.Clear();if(prepared.ShaderSlots[0].TextureFactors[tag]!=0xFF646E78)throw new Exception("Prepared result borrowed values.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintFacePreparer.Prepare(preparedBlueprint,preparedResources,values));
        values[new("choice",false)]=0;values[new("color",false)]=0;
        HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintFacePreparer.Prepare(preparedBlueprint,preparedResources,values));
        values[new("choice",true)]=int.MaxValue;values[new("color",false)]=int.MaxValue;
        if(BlueprintFacePreparer.Prepare(preparedBlueprint,preparedResources,values).Operations[1].SelectedValue!=1)throw new Exception("High palette fallback differs.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintFacePreparer.Prepare(preparedBlueprint,preparedResources with{Palettes=new Dictionary<string,PaletteDescription>()},values));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintFacePreparer.Prepare(preparedBlueprint with{PrepareLists=new IReadOnlyList<BlueprintPrepareOperation>[] {new[]{preparedBlueprint.PrepareLists[0][0] with{TargetIndex=2}}}},preparedResources,values));
        Console.WriteLine("PASS: explicit blueprint preparation, distinct factory Booleans, SST1 endpoint clamp/STFP last-entry fallback, ownership and missing-value/resource/target gates");
        foreach(var bytes in resourcesInput.Values)Array.Clear(bytes);
        if(resources.Textures.Single().Value.Mips[0].Rgba.Length!=64||resources.Shaders.Single().Value.Metadata.FormType!="SSHT")throw new Exception("Blueprint resources borrowed source buffers.");
        Console.WriteLine("PASS: synthetic owned BTRT live envelope/command description, truncation, invalid vertex/camera and unsupported operation gates; rendering=false");
    }
}
