using System.Reflection;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class SceneStaticSwitchFrameResourcesScenarios
{
    private const string Shader="shader/anim_mission_terminal.sht", Pin="B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62";
    private static string Hash(ReadOnlySpan<byte> b)=>Convert.ToHexString(SHA256.HashData(b));
    private static void Need(bool b,string why) { if(!b)throw new Exception(why); }
    private static SceneObjectResources Wrap(SceneStaticResources s)=>(SceneObjectResources)typeof(SceneObjectResources).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(SceneStaticResources)},null)!.Invoke(new object[]{s});
    internal static void Run(string[] args)
    {
        Need(args.Length==3,"Explicit root/config/catalog"); Need(Hash(File.ReadAllBytes(args[1]))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Config pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));
        var create=new SceneCreateObjectByCrcMessage(101,0,0,0,1,10,20,30,2907007746u,0);
        var selection=new SwitchTextureFrameSelection(Shader,Pin,0);
        SceneStaticResourceOptions Options(SceneStaticSwitchFramePolicy? policy=null,AppearanceGraphLimits? limits=null,long decoded=134217728,SwitchTextureFrameSelection? frame=null)=>new(SceneStaticMaterialPolicy.RequireComplete,frame??selection,limits,decoded,policy);
        var policy=new SceneStaticSwitchFramePolicy(); var requested=Options(policy); var defaults=Options();
        var raw=new Dictionary<string,TreLookupResult>(StringComparer.Ordinal); var originalCalls=new List<string>(); SceneObjectResources original,prior;
        using(var cfg=File.OpenText(args[1]))using(var overlay=ConfiguredTreOverlay.Open(args[0],cfg))
        {
            original=SceneObjectResourceRouter.Load(create,catalog,requested,p=> { originalCalls.Add(p);var r=overlay.Lookup(p);raw.Add(p,r with {Payload=r.Payload?.ToArray()});return r; });
            prior=SceneObjectResourceRouter.Load(create,catalog,defaults,p=>overlay.Lookup(p));
        }
        var counters=SceneObjectResourceAccounting.Measure(original); var previous=SceneObjectResourceAccounting.Measure(prior);
        Need(originalCalls.Count==34 && originalCalls.Distinct().Count()==34 && original.SourceBytes==1479410,"Exact original34-source union");
        Need(counters.DecodedTextureBytes==2490372 && counters.Mips==126 && counters.DecodedTextures==15 && counters.PendingWeight==3969782 && counters.SwitchFrameDecodedBytes==349536,"Separate retained representation charged");
        Need(previous.Sources==27 && previous.SourceBytes==1401794 && previous.DecodedTextureBytes==2140836 && previous.Mips==62 && previous.PendingWeight==3542630 && previous.SwitchFrames==0,"Default route unchanged");
        var bundle=original.Static!.SwitchFrames!;
        SceneObjectResourceValidation.Validate(original,create,catalog,requested); SceneObjectResourceValidation.Validate(prior,create,catalog,defaults);
        var cases=new List<string>(); int controlled=0;
        void Bad<T>(string name,Action action) where T:Exception { try { action(); } catch(T) { cases.Add(name);return; } throw new Exception("Accepted "+name); }
        SceneObjectResources Replay(SceneStaticResourceOptions? option=null,Func<string,TreLookupResult,TreLookupResult>? change=null,CancellationToken token=default)
        {
            var calls=new HashSet<string>(); return SceneObjectResourceRouter.Load(create,catalog,option??requested,p=> { Need(calls.Add(p),"Duplicate external lookup"); var r=raw[p];return change is null?r:change(p,r); },token);
        }
        void Valid(SceneObjectResources result,SceneStaticResourceOptions options) { SceneObjectResourceValidation.Validate(result,create,catalog,options); controlled++; }
        var exactPolicy=new SceneStaticSwitchFramePolicy(new(8,10,89350,349536,64,475,new(128,11088,43692)),126,3969782);
        var exact=Options(exactPolicy,new(MaximumResources:34,MaximumSourceBytes:1479410),2490372);
        Valid(Replay(exact),exact);
        Bad<InvalidDataException>("decoded-one-less",()=>Replay(Options(exactPolicy,decoded:2490371)));
        Bad<InvalidDataException>("mips-one-less",()=>Replay(Options(exactPolicy with {MaximumRetainedMips=125})));
        Bad<InvalidDataException>("weight-one-less",()=>Replay(Options(exactPolicy with {MaximumPendingWeight=3969781})));
        int growthReads=0;Bad<InvalidDataException>("intermediate-weight-growth",()=>Replay(Options(policy with {MaximumPendingWeight=3542630}),change:(p,r)=> { growthReads++;return r; }));
        Need(growthReads==28,"Intermediate pending growth stops at first additional callback before source copy");controlled++;
        Bad<IOException>("outer-source-bytes-one-less",()=>Replay(Options(exactPolicy,new(MaximumResources:34,MaximumSourceBytes:1479409))));
        Bad<InvalidDataException>("outer-source-count-one-less",()=>Replay(Options(exactPolicy,new(MaximumResources:33))));
        foreach(var item in new[]{"frames","sources","bytes","decoded","mips","shader","dimension","input","ddsdecoded"})
        {
            var l=exactPolicy.Limits!; var d=l.DdsLimits!;
            l=item switch {"frames"=>l with {MaximumFrames=7},"sources"=>l with {MaximumSources=9},"bytes"=>l with {MaximumSourceBytes=89349},"decoded"=>l with {MaximumDecodedBytes=349535},"mips"=>l with {MaximumMips=63},"shader"=>l with {MaximumShaderBytes=474},"dimension"=>l with {DdsLimits=d with {MaximumDimension=127}},"input"=>l with {DdsLimits=d with {MaximumInputBytes=11087}},_=>l with {DdsLimits=d with {MaximumDecodedBytes=43691}}};
            Bad<InvalidDataException>("bundle-one-less-"+item,()=>Replay(Options(exactPolicy with {Limits=l})));
        }
        foreach(string path in bundle.Sources.Keys)
            foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})
                Bad<InvalidDataException>("absent-"+path+kind,()=>Replay(change:(p,r)=>p==path?r with {Kind=kind,Payload=null}:r));
        string late="texture/anim_mission_terminal_08.dds";
        Bad<InvalidDataException>("late-truncation",()=>Replay(change:(p,r)=>p==late?r with {Payload=r.Payload![..127]}:r));
        Bad<InvalidDataException>("late-tail",()=>Replay(change:(p,r)=>p==late?r with {Payload=r.Payload!.Concat(new byte[]{0}).ToArray()}:r));
        foreach(uint word in new uint[]{0,7,9,0xFFFFFFFF})
        {
            var bytes=raw[Shader].Payload!.ToArray();int at=Find(bytes,new byte[]{68,84,83,84})+12; BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at),word);
            Bad<InvalidDataException>("count-"+word,()=>Replay(Options(policy,frame:selection with {ShaderSha256=Hash(bytes)}),(p,r)=>p==Shader?r with {Payload=bytes}:r));
        }
        foreach(uint bits in new uint[]{0,0x80000000,0xBF800000,0x7F800000,0x7FC00000})
        {
            var bytes=raw[Shader].Payload!.ToArray();int at=Find(bytes,new byte[]{68,84,83,84})+16; BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at),bits);
            Bad<InvalidDataException>("interval-"+bits,()=>Replay(Options(policy,frame:selection with {ShaderSha256=Hash(bytes)}),(p,r)=>p==Shader?r with {Payload=bytes}:r));
        }
        Bad<ArgumentException>("no-selection",()=>new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null,switchFrames:policy));
        Bad<ArgumentException>("neutral",()=>new SceneStaticResourceOptions(SceneStaticMaterialPolicy.AllowNeutral,selection,switchFrames:policy));
        Bad<ArgumentOutOfRangeException>("invalid-policy",()=>Options(policy with {MaximumRetainedMips=0}));
        Bad<InvalidDataException>("wrong-pin",()=>Replay(Options(policy,frame:selection with {ShaderSha256=new string('0',64)})));
        Bad<InvalidDataException>("bundle-unrequested",()=>SceneObjectResourceValidation.Validate(original,create,catalog,defaults));
        Bad<InvalidDataException>("bundle-missing",()=>SceneObjectResourceValidation.Validate(prior,create,catalog,requested));
        Bad<InvalidDataException>("with-no-bundle",()=>SceneObjectResourceValidation.Validate(Wrap(original.Static with {SwitchFrames=null}),create,catalog,requested));
        Bad<InvalidDataException>("with-swapped-bundle",()=>SceneObjectResourceValidation.Validate(Wrap(original.Static with {SwitchFrames=Replay().Static!.SwitchFrames}),create,catalog,requested));
        Bad<InvalidDataException>("with-create",()=>SceneObjectResourceValidation.Validate(Wrap(original.Static with {Create=create with {PositionX=100}}),create,catalog,requested));
        Bad<InvalidDataException>("changed-policy",()=>SceneObjectResourceValidation.Validate(original,create,catalog,Options(policy with {MaximumRetainedMips=8192})));
        var sources=original.Sources.ToDictionary(p=>p.Key,p=>p.Value);sources[late]=sources[late] with {Sha256=new string('0',64)};
        Bad<InvalidDataException>("with-source-identity",()=>SceneObjectResourceValidation.Validate(Wrap(original.Static with {Sources=sources}),create,catalog,requested));
        var meshes=original.Static.Appearance.Meshes.ToDictionary(p=>p.Key,p=>p.Value); var target=meshes.First(p=>p.Value.Materials.ContainsKey(Shader));
        var materials=target.Value.Materials.ToDictionary(p=>p.Key,p=>p.Value); var mat=materials[Shader];
        materials[Shader]=mat with {References=mat.References with {Textures=mat.References.Textures.Concat(mat.References.Textures).ToArray()}};
        meshes[target.Key]=target.Value with {Materials=materials};
        Bad<InvalidDataException>("with-contradictory-bindings",()=>SceneObjectResourceValidation.Validate(Wrap(original.Static with {Appearance=original.Static.Appearance with {Meshes=meshes}}),create,catalog,requested));
        Parallel.For(0,8,_=>SceneObjectResourceValidation.Validate(Replay(),create,catalog,requested));controlled++;
        foreach(var field in new[]{"count","interval","base","tag","path"})
        {
            var changedDesc=mat.SwitchTexture!.Description;
            changedDesc=field switch {"count"=>changedDesc with {RawDtstWord0=7},"interval"=>changedDesc with {RawDtstWord1=0},"base"=>changedDesc with {BaseShaderPath=Shader},"tag"=>changedDesc with {Frames=changedDesc.Frames.Select((f,i)=>i==7?f with {TextureTag=1}:f).ToArray()},_=>changedDesc with {Frames=changedDesc.Frames.Select((f,i)=>i==7?f with {TexturePath=bundle.Frames[0].TexturePath}:f).ToArray()}};
            var map=target.Value.Materials.ToDictionary(p=>p.Key,p=>p.Value);map[Shader]=mat with {SwitchTexture=mat.SwitchTexture with {Description=changedDesc}};
            var graph=original.Static.Appearance.Meshes.ToDictionary(p=>p.Key,p=>p.Value);graph[target.Key]=target.Value with {Materials=map};
            Bad<InvalidDataException>("with-authored-"+field,()=>SceneObjectResourceValidation.Validate(Wrap(original.Static with {Appearance=original.Static.Appearance with {Meshes=graph}}),create,catalog,requested));
        }
        var chosen=original.Static.Appearance.Meshes.Values.SelectMany(m=>m.Textures).First(p=>p.Key==bundle.Frames[0].TexturePath).Value;
        Need(MemoryMarshal.TryGetArray(chosen.Mips[0].Rgba,out var segment),"Backing memory fixture"); byte saved=segment.Array![segment.Offset];segment.Array[segment.Offset]^=255;
        Bad<InvalidDataException>("appearance-backing-mutation",()=>SceneObjectResourceValidation.Validate(original,create,catalog,requested));segment.Array[segment.Offset]=saved;
        byte[] clone=bundle.Frames[0].Image.Mips[0].CopyRgba();
        Array.Clear(clone); Valid(original,requested);
        using(var cancel=new CancellationTokenSource()) { cancel.Cancel();Bad<OperationCanceledException>("pre-cancel",()=>Replay(token:cancel.Token)); }
        foreach(string stop in new[]{raw.Keys.First(),late})using(var cancel=new CancellationTokenSource())
            Bad<OperationCanceledException>("callback-cancel-"+stop,()=>Replay(change:(p,r)=> { if(p==stop)cancel.Cancel();return r; },token:cancel.Token));
        foreach(bool swallow in new[]{false,true})
            Bad<InvalidOperationException>("reentry-"+swallow,()=>Replay(change:(p,r)=> { try { Replay(); } catch(InvalidOperationException) { if(!swallow)throw; }return r; }));
        var callbackError=new IOException("bounded callback");try {Replay(change:(p,r)=>p==late?throw callbackError:r);throw new Exception("No callback failure");}catch(IOException e){Need(ReferenceEquals(e,callbackError),"Callback identity");controlled++;}
        Valid(Replay(),requested);Valid(original,requested);Valid(prior,defaults);
        var aliasBytes=raw[Shader].Payload!.ToArray();var old=System.Text.Encoding.ASCII.GetBytes(late);int aliasAt=Find(aliasBytes,old);aliasBytes[aliasAt+late.Length-5]=(byte)'1';
        var aliasOption=Options(policy,frame:selection with {ShaderSha256=Hash(aliasBytes)});
        var alias=Replay(aliasOption,(p,r)=>p==Shader?r with {Payload=aliasBytes}:r);Valid(alias,aliasOption);
        Need(alias.Sources.Count==33 && alias.Static!.SwitchFrames!.Images.Count==7 && ReferenceEquals(alias.Static.SwitchFrames.Frames[0].Image,alias.Static.SwitchFrames.Frames[7].Image),"Exact path alias");
        var equal=Replay(change:(p,r)=>p==late?r with {Payload=raw[bundle.Frames[0].TexturePath].Payload}:r);Valid(equal,requested);
        Need(equal.Sources.Count==34 && equal.Static!.SwitchFrames!.Images.Count==8 && !ReferenceEquals(equal.Static.SwitchFrames.Frames[0].Image,equal.Static.SwitchFrames.Frames[7].Image),"Equal bytes remain distinct");
        var firstHashes=bundle.Images.ToDictionary(p=>p.Key,p=>p.Value.Mips.Select(m=>Hash(m.CopyRgba())).ToArray());foreach(var r in raw.Values)if(r.Payload is {} b)Array.Clear(b);
        Valid(original,requested);Need(firstHashes.All(p=>p.Value.SequenceEqual(bundle.Images[p.Key].Mips.Select(m=>Hash(m.CopyRgba())))),"Borrowed cleanup preserves bundle");
        Console.WriteLine("static_switch_resources="+JsonSerializer.Serialize(new {Sources=original.Sources.Values,Calls=originalCalls,Receipt=counters,DefaultReceipt=previous,Frames=bundle.Frames.Select(f=>new {f.Index,f.TextureTag,f.TexturePath}),Images=bundle.Images.Select(p=>new {Path=p.Key,Mips=p.Value.Mips.Select(m=>new {m.Width,m.Height,Bytes=m.DecodedBytes,Sha256=Hash(m.CopyRgba())})}),Cases=cases,Controlled=controlled,ArchivesClosed=true,NoTickDecoding=true}));
        Console.WriteLine("PASS: explicit all-frame scene resources, exact union, duplicate retained charge, budgets, immutable consistency and atomic failures.");
    }
    private static int Find(byte[] bytes,byte[] needle) { for(int i=0;i<=bytes.Length-needle.Length;i++)if(bytes.AsSpan(i,needle.Length).SequenceEqual(needle))return i;throw new Exception("Needle absent"); }
}
