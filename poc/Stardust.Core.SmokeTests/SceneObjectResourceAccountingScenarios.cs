using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class SceneObjectResourceAccountingScenarios
{
    private static void Check(bool value,string reason) { if(!value)throw new Exception(reason); }
    private static string Hash(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    // Test-only use of sealed internal constructors, as in the accepted R14 gate fixture.
    private static SceneObjectResources Wrap<T>(T payload)=>(SceneObjectResources)typeof(SceneObjectResources).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(T)},null)!.Invoke(new object?[]{payload});
    internal static void Run(string[] args)
    {
        Check(args.Length==4,"Explicit root/config/catalog/bank required");
        Check(Hash(File.ReadAllBytes(args[1]))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2])); using var stream=File.OpenRead(args[3]); var bank=SkeletalAnimationBankProfile.Load(stream);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var co=new SceneCreatureResourceOptions(idle,bank,true);
        var so=new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0));
        var originals=new List<object>(); var negatives=new List<string>(); int reads=0,scopes=0;
        void Reject<T>(string name,Action action) where T:Exception { try{action();}catch(T){negatives.Add(name);return;}throw new Exception("Accepted invalid "+name); }
        Reject<ArgumentNullException>("null",()=>SceneObjectResourceAccounting.Measure(null!));
        foreach(var option in new SceneObjectResourceOptions[]{co,so})
        {
            var create=new SceneCreateObjectByCrcMessage(101,0,0,0,1,10,20,30,option==co?2937962913u:2907007746u,0);
            SceneObjectResources result; var borrowed=new List<byte[]>();scopes++;
            try { using var cfg=File.OpenText(args[1]); using var overlay=ConfiguredTreOverlay.Open(args[0],cfg);
                result=SceneObjectResourceRouter.Load(create,catalog,option,p=>{reads++;var r=overlay.Lookup(p);if(r.Payload is not null)borrowed.Add(r.Payload);return r;});
                foreach(var bytes in borrowed)Array.Clear(bytes);
            } finally { scopes--; }
            Check(scopes==0,"Archives closed before accounting"); SceneObjectResourceValidation.Validate(result,create,catalog,option);
            int beforeReads=reads; string before=Hash(JsonSerializer.SerializeToUtf8Bytes(result));
            var receipt=SceneObjectResourceAccounting.Measure(result);
            var worker=Task.Factory.StartNew(()=>SceneObjectResourceAccounting.Measure(result),CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default).GetAwaiter().GetResult();
            Check(JsonSerializer.Serialize(receipt)==JsonSerializer.Serialize(worker),"Pure worker repeat");
            Check(beforeReads==reads&&before==Hash(JsonSerializer.SerializeToUtf8Bytes(result)),"No I/O or mutation");
            var images=result.Creature?.Creature.MaterialBundle!.Textures??result.Static!.Appearance.Meshes.Values.SelectMany(m=>m.Textures).GroupBy(p=>p.Key).ToDictionary(g=>g.Key,g=>g.First().Value);
            originals.Add(new {Kind=result.Kind.ToString(),Receipt=receipt,Sources=result.Sources.Values.OrderBy(s=>s.Path),Textures=images.OrderBy(p=>p.Key).Select(p=>new {Path=p.Key,Mips=p.Value.Mips.Select(m=>new {m.Width,m.Height,Bytes=m.Rgba.Length,Sha256=Hash(m.Rgba.Span)})}),ArchiveClosed=true,Unchanged=true,NoReads=true});
            if(result.Creature is {} c)
            {
                SceneObjectResources Changed(DdsImage? image) { var map=c.Creature.MaterialBundle!.Textures.ToDictionary(p=>p.Key,p=>p.Value);map[map.Keys.First()]=image!; return Wrap(c with {Creature=c.Creature with {MaterialBundle=c.Creature.MaterialBundle with {Textures=map}}}); }
                var image=images.First().Value;var mip=image.Mips[0];
                Reject<InvalidDataException>("null-image",()=>SceneObjectResourceAccounting.Measure(Changed(null)));
                Reject<InvalidDataException>("empty-mips",()=>SceneObjectResourceAccounting.Measure(Changed(image with {Mips=Array.Empty<DdsMip>()})));
                Reject<InvalidDataException>("null-mip",()=>SceneObjectResourceAccounting.Measure(Changed(image with {Mips=new DdsMip[]{null!}})));
                Reject<InvalidDataException>("mip-length",()=>SceneObjectResourceAccounting.Measure(Changed(image with {Mips=new[]{mip with {Rgba=new byte[4]}}})));
                Reject<InvalidDataException>("mip-dimensions",()=>SceneObjectResourceAccounting.Measure(Changed(image with {Mips=new[]{mip with {Width=0}}})));
                Reject<OverflowException>("dimension-overflow",()=>SceneObjectResourceAccounting.Measure(Changed(image with {Mips=new[]{mip with {Width=int.MaxValue,Height=int.MaxValue}}})));
                Reject<InvalidDataException>("source-total",()=>SceneObjectResourceAccounting.Measure(Wrap(c with {SourceBytes=long.MaxValue})));
                Reject<InvalidDataException>("source-count",()=>SceneObjectResourceAccounting.Measure(Wrap(c with {ExternalReads=0})));
                var sources=c.Creature.Sources.ToDictionary(p=>p.Key,p=>p.Value);string key=sources.Keys.First();sources[key]=sources[key] with {Bytes=-1};
                Reject<InvalidDataException>("negative-source",()=>SceneObjectResourceAccounting.Measure(Wrap(c with {Creature=c.Creature with {Sources=sources}})));
                sources[key]=result.Sources[key] with {Path="wrong"};
                Reject<InvalidDataException>("source-path",()=>SceneObjectResourceAccounting.Measure(Wrap(c with {Creature=c.Creature with {Sources=sources}})));
                // Mutation of payload bytes cannot change a length-based budget or the detached receipt.
                var synthetic=new byte[4];var measured=SceneObjectResourceAccounting.Measure(Changed(image with {Mips=new[]{new DdsMip(1,1,synthetic)}}));
                synthetic[0]=255;Check(measured.PendingWeight==SceneObjectResourceAccounting.Measure(Changed(image with {Mips=new[]{new DdsMip(1,1,synthetic)}})).PendingWeight,"Byte-content mutation retains length-only budget");
            }
            else
            {
                var s=result.Static!;var mesh=s.Appearance.Meshes.First().Value;
                SceneObjectResources Graph(TexturedStaticMesh a,TexturedStaticMesh b)=>Wrap(s with {Appearance=s.Appearance with {Meshes=new Dictionary<string,TexturedStaticMesh>{{"one",a},{"two",b}}}});
                Check(JsonSerializer.Serialize(SceneObjectResourceAccounting.Measure(Graph(mesh,mesh)))==JsonSerializer.Serialize(SceneObjectResourceAccounting.Measure(Graph(mesh,mesh with {}))),"Shared image paths deduplicate across mesh descriptors");
                var map=mesh.Textures.ToDictionary(p=>p.Key,p=>p.Value);string path=map.Keys.First();map[path]=map[path] with {};
                Reject<InvalidDataException>("distinct-shared-image",()=>SceneObjectResourceAccounting.Measure(Graph(mesh,mesh with {Textures=map})));
                map[path]=map[path] with {Mips=new[]{new DdsMip(1,1,new byte[4])}};
                Reject<InvalidDataException>("inconsistent-shared-metadata",()=>SceneObjectResourceAccounting.Measure(Graph(mesh,mesh with {Textures=map})));
                map.Remove(path);map.Add("texture/not-in-source.dds",mesh.Textures[path]);
                Reject<InvalidDataException>("missing-texture-source",()=>SceneObjectResourceAccounting.Measure(Graph(mesh,mesh with {Textures=map})));
                var hdr=mesh with {Textures=new Dictionary<string,DdsImage>(),HdrTextures=new Dictionary<string,DdsHdrImage>{{path,new(DdsHdrFormat.Bc6Unsigned,new[]{new DdsHdrMip(1,1,new byte[6])},DdsAlphaMode.Unknown,95)}}};
                var hdrReceipt=SceneObjectResourceAccounting.Measure(Graph(hdr,hdr));Check(hdrReceipt.DecodedTextures==1&&hdrReceipt.Mips==1&&hdrReceipt.DecodedTextureBytes==6,"Synthetic existing HDR descriptor length only");
                Reject<InvalidDataException>("rgba-hdr-path-collision",()=>SceneObjectResourceAccounting.Measure(Graph(mesh,hdr)));
            }
            Check(before==Hash(JsonSerializer.SerializeToUtf8Bytes(result)),"Negative fixtures preserve originals");
        }
        Check(reads==66&&scopes==0,"Closed original66-read closure");
        Check(typeof(SceneObjectResourceAccountingReceipt).GetProperties().All(p=>p.SetMethod is null),"Receipt get-only immutable");
        Console.WriteLine("accounting_receipt="+JsonSerializer.Serialize(new {Originals=originals,Negatives=negatives,Reads=reads,ArchiveScopes=scopes,ImmutableReceipt=true}));
        Console.WriteLine("PASS: original logical accounting, deduplication, malformed descriptors, overflow, mutation isolation and closed archives.");
    }
}
