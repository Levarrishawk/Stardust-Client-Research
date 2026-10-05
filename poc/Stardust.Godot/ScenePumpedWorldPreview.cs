using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Original resources through the continuous modern scene pump; offline fixture only.</summary>
public partial class ScenePumpedWorldPreview : Node
{
    private SceneWorldCoordinator? world;
    private SceneWorldPresentationPump? pump;
    private SceneWorldPresentationHost? host;
    private SceneWorldObjectMotionStore? motions;
    private SceneWorldMotionApplicationRouter? router;
    private SceneWorldLocalInputHost? inputHost;
    private SceneWorldLocomotionVisual? locomotion;
    private bool localInput, inputLoss, customizationRevisions, customizationClosePending;
    private int customizationHold, customizationEntered, customizationClosed;
    private bool customizationWire;
    private bool customizationScalars;
    private bool customizationFull;
    private bool customizationNamed;
    private JsonDocument? tangibleFixture;
    private SceneWorldTangibleApplicationRouter? tangibleRouter;
    private SceneWorldStaticStateVisual? tangibleVisual;
    private ScenePreparedObjectPublication? retainedTerminal;
    private string? retainedTerminalFingerprint;
    private Transform3D retainedTerminalTransform;
    private int tangibleIndex;
    private JsonDocument? tangible6Fixture;
    private SceneWorldTangibleView6ApplicationRouter? tangible6Router;
    private int tangible6Index;
    private JsonDocument? creature6Fixture;
    private SceneWorldCreatureView6ApplicationRouter? creature6Router;
    private SceneWorldCreatureView6CompositeApplicationRouter? creature6CompositeRouter;
    private bool creature6Composite;
    private bool creature6Full;
    private SceneWorldCreatureView6FullApplicationRouter? creature6FullRouter;
    private int creature6Index;
    private Transform3D retainedCreature6Transform;
    private bool terminalAnimationEnabled;
    private bool terminalClockEnabled;
    private bool terminalClockFaultEnabled;
    private SceneWorldSwitchTexturePresentation? terminalAnimation;
    private readonly Dictionary<string,string[]> terminalMipHashes=new(StringComparer.Ordinal);
    private readonly Dictionary<int,ulong> terminalTextureIds=new();
    private int terminalAnimationStep;
    private string? terminalAnimationStructure;
    private SceneWorldNamedCreatureApplicationRouter? namedCreatureRouter;
    private SceneWorldObjectNameVisual? nameVisual;
    private Dictionary<string,LocalizedStringTable>? nameTables;
    private string namePrimary="string/native_missing",nameFallback="string/en";
    private bool customizationExtended;
    private SceneWorldExtendedCreatureApplicationRouter? extendedCreatureRouter;
    private AssetCustomizationDirectory? customizationDirectory;
    private AssetCustomizationContext? authoredContext;
    private Dictionary<string,string>? wirePeer;
    private CustomizationIdDictionary? customizationDictionary;
    private IReadOnlyDictionary<ShaderCustomizationKey,CustomizationVariableRange>? customizationRanges;
    private SceneWorldCustomizationStateStore? rawCustomizations;
    private SceneWorldCustomizationApplicationRouter? customizationRouter;
    private SceneWorldCustomizationScalarApplicationRouter? customizationScalarRouter;
    private SceneWorldFullCreatureApplicationRouter? fullCreatureRouter;
    private SceneWorldCreatureCustomizationSourceAdapter? customizationAdapter;
    private sealed class CheckedCustomizationSource(SceneWorldCreatureCustomizationSourceAdapter adapter) : ISceneWorldCreatureCustomizationSource {
        public SceneWorldCreatureCustomizationSnapshot? Find(SceneWorldObjectSnapshot snapshot) {
            var result=adapter.Find(snapshot);
            if(adapter.LastFailure is not null)throw new InvalidDataException("Wire customization batch rejected",adapter.LastFailure);
            return result;
        }
    }
    private SceneWorldCreatureCustomizationVisual? customizationVisual;
    private readonly SuppliedCustomizationSource customizationSource = new();
    private IReadOnlyDictionary<ShaderCustomizationKey,int>? customizationZero, customizationVaried;
    private Transform3D retainedCustomizationTransform;
    private double retainedCustomizationFrame;
    private sealed class SuppliedCustomizationSource : ISceneWorldCreatureCustomizationSource {
        public SceneWorldCreatureCustomizationSnapshot? Current;
        public SceneWorldCreatureCustomizationSnapshot? Find(SceneWorldObjectSnapshot snapshot) => Current;
    }
    private SceneCreatureFaceInspectionPolicy? faceInspection;
    private string? faceProfileSha256;
    private long localClock;
    private System.Numerics.Vector3 suspendedPosition;
    private readonly Dictionary<ulong,SceneWorldObjectVisual> visuals=new();
    private Dictionary<string,string>? motionPeer;
    private string? motionCapture;
    private readonly List<Task<SceneObjectResources>> jobs = new();
    private Dictionary<string,string> peer = new();
    private byte[] cfg = Array.Empty<byte>();
    private long generation;
    private ulong humanId, terminalId;
    private int phase, frames, calls, closed, entered;
    private bool finishing, cancel;
    private readonly int owner = System.Environment.CurrentManagedThreadId;
    private string? humanIds; private double firstIdleFrame;
    private ScenePreparedObjectPublication? originalHuman;
    private Godot.Environment? environment;

    public override void _Ready()
    {
        try
        {
            GetTree().AutoAcceptQuit=false;
            var args=OS.GetCmdlineUserArgs();
            LoadSourcedInputArguments(args);
            LoadNominalSpeedArguments(args);
            string Arg(string name) { int i=Array.IndexOf(args,name);if(i<0 || i+1>=args.Length || args.Count(a=>a==name)!=1)throw new ArgumentException(name);return args[i+1]; }
            string root=Arg("--asset-root");
            cfg=File.ReadAllBytes(Arg("--asset-config"));
            if(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(cfg))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new InvalidDataException("Configuration identity");
            peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(Arg("--mixed-peer")))!;
            if(peer.Count!=14 || peer.Values.Any(v=>v.Length>4096))throw new InvalidDataException("Bounded peer");
            humanId=SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer["human_create"])).ObjectId;
            terminalId=SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer["terminal_create"])).ObjectId;
            var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(Arg("--catalog-profile")));
            using var bankFile=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-locomotion.json"));
            var bank=SkeletalAnimationBankProfile.Load(bankFile);
            var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
            if(args.Contains("--face-profile")) {
                byte[] profile=File.ReadAllBytes(Arg("--face-profile"));
                try {
                    faceProfileSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(profile));
                    faceInspection=new(BlueprintFaceCustomizationProfile.Parse(profile));
                } finally {Array.Clear(profile);}
            }
            terminalAnimationEnabled=args.Contains("--terminal-animation");
            terminalClockEnabled=args.Contains("--terminal-clock");
            terminalClockFaultEnabled=args.Contains("--terminal-clock-fault");
            if(terminalClockFaultEnabled&&!terminalClockEnabled)throw new ArgumentException("Clock fault proof requires object clock");
            if(terminalClockEnabled&&!terminalAnimationEnabled)throw new ArgumentException("Terminal clock requires animation");
            if(terminalAnimationEnabled&&!args.Contains("--tangible-peer"))throw new ArgumentException("Terminal animation requires typed tangible proof");
            var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions> {
                [SceneObjectResourceRecipe.CreatureDetail0]=new SceneCreatureResourceOptions(idle,bank,true,faceInspection is null?64:128,faceInspection is null?16777216:33554432,faceInspection,loadAuthoredMovementInputs:sourcedRequested||nominalCurrentRequested),
                [SceneObjectResourceRecipe.StaticAppearance]=new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0),switchFrames:terminalAnimationEnabled?new SceneStaticSwitchFramePolicy():null) };
            cancel=args.Contains("--pump-cancel");
            localInput=args.Contains("--local-input");
            inputLoss=args.Contains("--input-loss");
            customizationWire=args.Contains("--customization-wire");
            customizationScalars=args.Contains("--customization-scalars");
            customizationFull=args.Contains("--customization-full");
            customizationNamed=args.Contains("--customization-named");
            customizationExtended=args.Contains("--customization-extended");
            if(customizationNamed && !customizationExtended)throw new ArgumentException("Named creature mode requires extended mode");
            if(customizationNamed)LoadNameContext(Arg("--customization-name-context"));
            if(args.Contains("--tangible-peer")) {
                Check(customizationNamed,"Typed tangible proof requires the supported mixed named host");
                tangibleFixture=JsonDocument.Parse(File.ReadAllBytes(Arg("--tangible-peer")));
                var tangibleData=tangibleFixture.RootElement;
                Check(tangibleData.GetProperty("Version").GetInt32()==1 && tangibleData.GetProperty("Synthetic").GetBoolean() &&
                    tangibleData.GetProperty("ObjectId").GetUInt64()==terminalId && tangibleData.GetProperty("Updates").GetArrayLength()==4,"Bounded synthetic tangible fixture");
                peer["terminal_baseline"]=tangibleData.GetProperty("Baseline").GetString()!;
                peer["terminal_delta"]=tangibleData.GetProperty("Noop").GetString()!;
            }
            if(args.Contains("--tangible6-peer")) {
                Check(tangibleFixture is not null && terminalClockEnabled && !terminalClockFaultEnabled,"TANO6 requires normal typed clock proof");
                tangible6Fixture=JsonDocument.Parse(File.ReadAllBytes(Arg("--tangible6-peer")));
                var data6=tangible6Fixture.RootElement;
                Check(data6.GetProperty("Version").GetInt32()==1 && data6.GetProperty("Synthetic").GetBoolean() && data6.GetProperty("ObjectId").GetUInt64()==terminalId && data6.GetProperty("Steps").GetArrayLength()==5,"Bounded synthetic TANO6 peer");
                peer["terminal_view6_baseline"]=data6.GetProperty("Baseline").GetString()!;
            }
            Check(new[]{"--creature4-full-peer","--creature4-partial-peer"}.Count(args.Contains)<=1,"Choose one CREO4 world policy");
            if(args.Contains("--creature4-partial-peer")){Check(args.Contains("--creature6-full-peer"),"Partial host requires full6 regression");LoadCreature4PartialPeer(Arg("--creature4-partial-peer"));}
            if(args.Contains("--creature4-full-peer")) {
                Check(args.Contains("--creature6-full-peer"),"Private-view host proof requires complete view6 regression");
                LoadCreature4FullPeer(Arg("--creature4-full-peer"));
            }
            Check(new[]{"--creature6-peer","--creature6-composite-peer","--creature6-full-peer"}.Count(args.Contains)<=1,"Choose one CREO6 world partition");
            creature6Full=args.Contains("--creature6-full-peer");
            creature6Composite=args.Contains("--creature6-composite-peer");
            if(args.Contains("--creature6-peer") || creature6Composite || creature6Full) {
                Check(tangible6Fixture is not null,"CREO6 requires the normal joined TANO6 proof");
                string path6=Arg(creature6Full?"--creature6-full-peer":creature6Composite?"--creature6-composite-peer":"--creature6-peer");
                Check(new FileInfo(path6).Length<=32768,"Bounded CREO6 peer file");
                creature6Fixture=JsonDocument.Parse(File.ReadAllBytes(path6));
                var data6=creature6Fixture.RootElement;
                Check(data6.GetProperty("Version").GetInt32()==1 && data6.GetProperty("Synthetic").GetBoolean() && data6.GetProperty("ObjectId").GetUInt64()==humanId && data6.GetProperty("Steps").GetArrayLength()==5,"Explicit synthetic CREO6 peer");
                Check(data6.GetProperty("Baseline").GetString()!.Length<=2048 && data6.GetProperty("Steps").EnumerateArray().All(step=>step.GetProperty("Hex").GetString()!.Length<=2048 && step.GetProperty("ModelHex").GetString()!.Length<=2048),"CREO6 peer wire bounds");
                peer["human_view6_baseline"]=data6.GetProperty("Baseline").GetString()!;
            }
            if(customizationExtended && !customizationFull)throw new ArgumentException("Extended creature mode requires full context mode");
            if(customizationFull && !customizationScalars)throw new ArgumentException("Full creature mode requires mixed scalar wire mode");
            if(customizationScalars && !customizationWire)throw new ArgumentException("Mixed scalars require wire customization mode");
            if(customizationWire) {
                using var document=JsonDocument.Parse(File.ReadAllBytes(Arg("--customization-peer")));
                var data=document.RootElement;
                Check(data.GetProperty("version").GetInt32()==1 && data.GetProperty("synthetic").GetBoolean() && data.GetProperty("ObjectId").GetUInt64()==humanId,"Explicit synthetic wire peer identity");
                wirePeer=data.GetProperty("Messages").EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.GetString()!);
                Check(wirePeer.Count==10 && wirePeer.Values.All(v=>v.Length<=8192),"Bounded customization wire fixture");
                if(customizationScalars)Check(data.GetProperty("MixedScalars").GetBoolean(),"Explicit mixed scalar wire fixture");
                if(customizationFull)Check(data.GetProperty("FullFields").GetBoolean() && data.GetProperty("WoundsPolicy").GetString()=="LegacyBounded","Explicit full creature fixture policy");
                if(customizationExtended)Check(data.GetProperty("ExtendedFields").GetBoolean(),"Explicit extended creature fixture");
                if(customizationNamed)Check(data.GetProperty("NamedFields").GetBoolean(),"Explicit named creature fixture");
                byte[] dictionary=File.ReadAllBytes(Arg("--customization-dictionary"));
                try {
                    Check(dictionary.Length==3697 && Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(dictionary))=="FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2","Original CIDM preload identity");
                    customizationDictionary=CustomizationIdDictionary.Load(dictionary);
                    if(customizationFull) {
                        byte[] directory=File.ReadAllBytes(Arg("--customization-asset-context"));
                        try {
                            Check(directory.Length==458340 && Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(directory))=="5A4F052E92B5E29CD3001CA1E0FE0438358CAA45E201E72E062BE65A6886DA20","Original ACST preload identity");
                            customizationDirectory=AssetCustomizationDirectory.Load(directory);
                        }finally{Array.Clear(directory);}
                    }
                } finally {Array.Clear(dictionary);}
            }
            customizationRevisions=args.Contains("--customization-revisions");
            customizationClosePending=args.Contains("--customization-close-pending");
            if(customizationClosePending && !customizationRevisions)throw new ArgumentException("Pending customization close requires revisions mode");
            if(customizationRevisions || customizationWire) {
                if(customizationRevisions && customizationWire)throw new ArgumentException("Separate supplied and wire customization modes");
                if(!localInput || faceInspection is null || inputLoss || cancel)throw new ArgumentException("Customization revisions require separate local-input face mode");
                using var zero=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-customization-zero.json"));
                using var varied=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-customization-varied.json"));
                customizationZero=SkeletalAppearanceCustomization.LoadProfile(zero);
                customizationVaried=SkeletalAppearanceCustomization.LoadProfile(varied);
            }
            if(inputLoss && (!localInput || cancel))throw new ArgumentException("Input loss requires separate local-input mode");
            if(localInput && !args.Contains("--motion-peer"))throw new ArgumentException("Local input requires motion peer");
            Task<SceneObjectResources> Load(SceneObjectResourceRequest request,CancellationToken token)
            {
                calls++; byte[] copy=cfg.ToArray();
                var task=Task.Run(() => {
                    try {
                        using var text=new StringReader(Encoding.UTF8.GetString(copy));
                        using var overlay=ConfiguredTreOverlay.Open(root,text);
                        var reads=new HashSet<string>();
                        TreLookupResult Resolve(string path) {
                            token.ThrowIfCancellationRequested();
                            if(cancel) {
                                Interlocked.Increment(ref entered);
                                if(!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15)))throw new InvalidDataException("Cancellation deadline");
                                token.ThrowIfCancellationRequested();
                            }
                            if(!reads.Add(path))throw new InvalidDataException("Duplicate source");
                            return overlay.Lookup(path);
                        }
                        var result=SceneObjectResourceRouter.Load(request.Snapshot.Create,request.Catalog,request.Options,Resolve,token);
                        if(System.Environment.CurrentManagedThreadId==owner || reads.Count!=result.ExternalReads)throw new InvalidDataException("Worker/source ownership");
                        return result;
                    } finally { Array.Clear(copy);Interlocked.Increment(ref closed); }
                },token);
                jobs.Add(task);return task;
            }
            world=new(catalog,options,Load,SceneCreateZeroQuaternionPolicy.Reject,maximumObjects:2,maximumOutstandingJobs:2,maximumStagedMessages:creature4Fixture is null&&creature4PartialFixture is null?8:9,maximumPayloadBytes:1024,maximumEnvelopes:4,maximumEnvelopeBytes:1024,
                replicationPolicy:customizationFull?new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,1024,1024,128,16384),16,16384,
                    MixedLimits:new(1024,16,16,64,256,64),WoundsPolicy:CreatureWoundsApplicationPolicy.LegacyBounded,
                    ComponentsLimits:new(1024,16,64,256,64),ComponentsPolicy:new(128,64)):new(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,1024,1024,128,16384),16,16384),
                tangibleReplicationPolicy:tangibleFixture is null?null:new(BaselineLimits:new(1024,1024,128,64,4096),
                    DeltaLimits:new(1024,32,32,32,1024,4096,64,64,64,32,1024,128),
                    StoreLimits:new(2,512,64,64,1024,128,32,64)),
                tangibleView6Policy:tangible6Fixture is null?null:new(new(MaximumPayloadBytes:1024),new(),new(MaximumObjects:2)),
                creatureView6Policy:creature6Fixture is null || creature6Composite || creature6Full?null:new(new(MaximumPayloadBytes:1024),new(),new(MaximumObjects:2)),
                creatureView6CompositePolicy:creature6Fixture is null || !creature6Composite?null:new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new(MaximumObjects:2)),
                creatureView6FullPolicy:creature6Fixture is null || !creature6Full?null:new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new(MaximumObjects:2)),
                creatureView4FullPolicy:creature4Fixture is null?null:new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new(MaximumObjects:2)),
                creatureView4PartialPolicy:creature4PartialFixture is null?null:new(new(MessageLimits:new(MaximumPayloadBytes:1024)),new(MaximumObjects:2)));
            generation=world.BeginSelection(humanId).Generation;
            if(args.Contains("--motion-peer")) {
                motionPeer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(Arg("--motion-peer")))!;
                if(motionPeer.Count!=3 || motionPeer.Values.Any(v=>v.Length>128))throw new InvalidDataException("Bounded motion fixture");
                motions=new(world,maximumObjects:2);
                if(args.Contains("--motion-capture")) {
                    motionCapture=Path.GetFullPath(Arg("--motion-capture"));
                    string prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","..","local-private"))+Path.DirectorySeparatorChar;
                    if(DisplayServer.GetName()=="headless" || !motionCapture.StartsWith(prefix,StringComparison.OrdinalIgnoreCase) || Path.GetExtension(motionCapture)!=".png" || File.Exists(motionCapture) || !Directory.Exists(Path.GetDirectoryName(motionCapture)))throw new ArgumentException("Fresh private GPU motion PNG");
                }
            }
            if(localInput)router=new(world,motions!, (message,snapshot)=> {
                Check(message.ParentId is null && message.DirectionRaw==0 && message.SpeedRaw>=0,"Explicit diagnostic world projection");
                return(AppearanceTransform.Identity with {Translation=message.NativePosition},(float)message.SpeedRaw);
            });
            if(customizationWire) {
                rawCustomizations=new(world,maximumObjects:2);
                var limits=new CreatureView3CustomizationDeltaLimits(4096,16,1536,24576);
                if(customizationNamed) {
                    var namedLimits=new CreatureView3NamedDeltaLimits(new(new(4096,16,16,16,1536,24576,64,256,64,64,256,64),16),16,1024,128);
                    namedCreatureRouter=new(world,rawCustomizations,e=>CreatureView3NamedDeltaCodec.Decode(e,namedLimits,CreatureWoundsApplicationPolicy.LegacyBounded),(g,bytes)=>router!.Accept(g,bytes.Span),maximumPayloadBytes:4096);
                } else if(customizationExtended) {
                    var extendedLimits=new CreatureView3ExtendedDeltaLimits(new(4096,16,16,16,1536,24576,64,256,64,64,256,64),16);
                    extendedCreatureRouter=new(world,rawCustomizations,e=>CreatureView3ExtendedDeltaCodec.Decode(e,extendedLimits,CreatureWoundsApplicationPolicy.LegacyBounded),(g,bytes)=>router!.Accept(g,bytes.Span),maximumPayloadBytes:4096);
                } else if(customizationFull) {
                    var fullLimits=new CreatureView3FullDeltaLimits(4096,16,16,16,1536,24576,64,256,64,64,256,64);
                    fullCreatureRouter=new(world,rawCustomizations,e=>CreatureView3FullDeltaCodec.Decode(e,fullLimits,CreatureWoundsApplicationPolicy.LegacyBounded),(g,bytes)=>router!.Accept(g,bytes.Span),maximumPayloadBytes:4096);
                } else if(customizationScalars) {
                    var mixedLimits=new CreatureView3CustomizationScalarDeltaLimits(4096,16,16,16,1536,24576);
                    customizationScalarRouter=new(world,rawCustomizations,e=>CreatureView3CustomizationScalarDeltaCodec.Decode(e,mixedLimits),(g,bytes)=>router!.Accept(g,bytes.Span),maximumPayloadBytes:4096);
                } else customizationRouter=new(world,rawCustomizations,e=>CreatureView3CustomizationDeltaCodec.Decode(e,limits),(g,bytes)=>router!.Accept(g,bytes.Span),maximumPayloadBytes:4096);
                customizationAdapter=new(world,rawCustomizations,snapshot=> {
                    Check(snapshot.Object.Handle.ObjectId==humanId,"One explicit human consumer context");return customizationZero!;
                },(packed,current)=>CustomizationVariableProjection.Apply(customizationDictionary!,customizationRanges!,current,packed).Values,maxObjects:2);
            }
            var factory=new SceneWorldObjectVisualFactory(world,this,terminalFrame0Inspection:true);
            if(tangibleFixture is not null)tangibleRouter=new(world,e=>TangibleView3DeltaCodec.Decode(e,new(1024,32,32,32,1024,4096,64,64,64,32,1024,128)),
                (g,bytes)=>namedCreatureRouter!.Accept(g,bytes),maximumPayloadBytes:4096);
            ISceneWorldObjectPresentation? CreateVisual(ScenePreparedObjectPublication publication,SceneWorldObjectSnapshot snapshot) {
                var inner=(SceneWorldObjectVisual?)factory.Create(publication,snapshot);
                if(inner is null)return null;
                ISceneWorldObjectPresentation result=inner;
                try {
                    result=motions is null?inner:new SceneWorldObjectMotionVisual(world,inner,motions);
                    if(localInput && inner.CreatureVisual is not null)result=locomotion=new SceneWorldLocomotionVisual(world,(SceneWorldObjectMotionVisual)result,motions!);
                    if(customizationWire && inner.CreatureVisual is not null) {
                        var appearance=publication.Resources.Creature!.Creature;
                        if(customizationFull) {
                            const string contextPath="appearance/hum_m.sat";
                            var query=customizationDirectory!.Query(LegacyAssetPath.Checksum(contextPath),normalizedAssetPath:contextPath);
                            PaletteDescription? ResolveOwnedPalette(string path) {
                                var found=new List<PaletteDescription>();
                                if(appearance.FaceResources!.Palettes.TryGetValue(path,out var facePalette))found.Add(facePalette);
                                foreach(var palette in appearance.MaterialBundle!.PaletteDefaults!.Values)if(palette.Palettes.TryGetValue(path,out var value))found.Add(value);
                                Check(found.Count>0 && found.All(p=>p.Version==found[0].Version && p.Entries.SequenceEqual(found[0].Entries)),"Consistent existing owned selected palettes");return found[0];
                            }
                            authoredContext=AssetCustomizationContextBuilder.Build(query,customizationDictionary!,SkeletalAppearanceCustomization.DescribeControls(appearance).Select(c=>c.Key).ToArray(),ResolveOwnedPalette);
                            customizationZero=authoredContext.AuthoredDefaults;customizationRanges=authoredContext.RangeContext.Ranges;
                            Check(query.Variables.Count==36 && authoredContext.Bindings.Count==8 && authoredContext.AuthoredDefaults.Values.All(v=>v==0),"Original selected authored context, not remote player state");
                            GD.Print("asset_customization_context="+JsonSerializer.Serialize(new{DirectoryBytes=458340,CidmBytes=3697,Descriptors=query.Variables.Count,Selected=authoredContext.Bindings.Count,PaletteBindings=authoredContext.Bindings.Count(b=>b.Kind==AssetCustomizationContextBindingKind.Palette),AuthoredDefaultsZero=true,LivePlayerState=false}));
                        } else customizationRanges=SkeletalAppearanceCustomization.DescribeControls(appearance).ToDictionary(c=>c.Key,c=>new CustomizationVariableRange(0,checked(c.MaximumValue+1)));
                    }
                    if((customizationRevisions || customizationWire) && inner.CreatureVisual is not null)result=customizationVisual=new SceneWorldCreatureCustomizationVisual(world,locomotion!,customizationWire?new CheckedCustomizationSource(customizationAdapter!):customizationSource,(basis,values,token)=> {
                        if(Volatile.Read(ref customizationHold)!=0) {
                            Interlocked.Increment(ref customizationEntered);
                            try {
                                if(!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15)))throw new InvalidDataException("Owned customization cancellation deadline");
                                token.ThrowIfCancellationRequested();
                            } finally {Interlocked.Increment(ref customizationClosed);}
                        }
                        return SkeletalAppearanceCustomization.Apply(basis,values,token);
                    });
                    if(customizationNamed && inner.CreatureVisual is not null)
                        result=nameVisual=new SceneWorldObjectNameVisual(world,result,inner,names=>ResolveName(names).DisplayText);
                    if(tangibleFixture is not null && inner.StaticVisual is not null)
                        result=tangibleVisual=new SceneWorldStaticStateVisual(world,result,inner,s=> {
                            var state=s.TangibleReplication;
                            return state is not null && world.IsCurrent(state)?new(state.Incarnation,state.Revision,state.Names,state.TangibleScalars.Visibility):null;
                        },names=>ResolveName(names).DisplayText);
                    if(terminalAnimationEnabled&&inner.StaticVisual is not null) {
                        var bundle=publication.Resources.Static!.SwitchFrames!;
                        terminalMipHashes.Clear();
                        // Diagnostic hashes are prepared once at publication, outside playback updates.
                        foreach(var pair in bundle.Images)terminalMipHashes.Add(pair.Key,pair.Value.Mips.Select(m=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(m.CopyRgba()))).ToArray());
                        result=terminalAnimation=new SceneWorldSwitchTexturePresentation(result,inner,bundle.Interval,terminalClockEnabled);
                        var accounting=SceneObjectResourceAccounting.Measure(publication.Resources);
                        Check(jobs.Any(j=>j.IsCompletedSuccessfully&&ReferenceEquals(j.Result,publication.Resources)),"Terminal archive job completed before publication");
                        GD.Print("terminal_animation_resource="+JsonSerializer.Serialize(new {publication.Resources.ExternalReads,publication.Resources.SourceBytes,ReadsOnce=true,ArchivesClosed=true,BundleSources=bundle.Sources.Count,BundleDecodedBytes=bundle.DecodedBytes,BundleMips=bundle.MipCount,RetainedDecodedBytes=accounting.DecodedTextureBytes,accounting.PendingWeight,DefaultFrame0DecodedSeparately=true}));
                    }
                    visuals[publication.Ticket.Handle.ObjectId]=inner;return result;
                } catch {result.Dispose();throw;}
            }
            if(tangible6Fixture is not null)tangible6Router=new(world,downstream:(g,bytes)=>tangibleRouter is not null?tangibleRouter.Accept(g,bytes):world.Accept(g,bytes.Span),maximumPayloadBytes:1024);
            if(creature6Fixture is not null && !creature6Composite && !creature6Full)creature6Router=new(world,downstream:(g,bytes)=>tangible6Router!.Accept(g,bytes),maximumPayloadBytes:1024);
            if(creature6Fixture is not null && creature6Composite)creature6CompositeRouter=new(world,downstream:(g,bytes)=>tangible6Router!.Accept(g,bytes),maximumPayloadBytes:1024);
            if(creature6Fixture is not null && creature6Full)creature6FullRouter=new(world,downstream:(g,bytes)=>tangible6Router!.Accept(g,bytes),maximumPayloadBytes:1024);
            if(creature4Fixture is not null)creature4FullRouter=new(world,downstream:(g,bytes)=>creature6FullRouter!.Accept(g,bytes),maximumPayloadBytes:4096);
            if(creature4PartialFixture is not null)creature4PartialRouter=new(world,downstream:(g,bytes)=>creature6FullRouter!.Accept(g,bytes),maximumPayloadBytes:4096);
            pump=new(world,CreateVisual,new(MaximumQueuedMessages:16,MaximumQueuedBytes:16384,MaximumMessageBytes:4096,MaximumMessagesPerStep:2,MaximumPendingPublications:2,MaximumPublicationsPerStep:1,MaximumActors:2,MaximumPendingResourceWeight:faceInspection is null?67108864:134217728),
                r=>SceneObjectResourceAccounting.Measure(r).PendingWeight,
                applicationReceiver:creature4PartialRouter is not null?(g,bytes)=>creature4PartialRouter.Accept(g,bytes):creature4FullRouter is not null?(g,bytes)=>creature4FullRouter.Accept(g,bytes):creature6FullRouter is not null?(g,bytes)=>creature6FullRouter.Accept(g,bytes):creature6CompositeRouter is not null?(g,bytes)=>creature6CompositeRouter.Accept(g,bytes):creature6Router is not null?(g,bytes)=>creature6Router.Accept(g,bytes):tangible6Router is not null?(g,bytes)=>tangible6Router.Accept(g,bytes):tangibleRouter is not null?(g,bytes)=>tangibleRouter.Accept(g,bytes):namedCreatureRouter is not null?(g,bytes)=>namedCreatureRouter.Accept(g,bytes):extendedCreatureRouter is not null?(g,bytes)=>extendedCreatureRouter.Accept(g,bytes):fullCreatureRouter is not null?(g,bytes)=>fullCreatureRouter.Accept(g,bytes):customizationScalarRouter is not null?(g,bytes)=>customizationScalarRouter.Accept(g,bytes):customizationRouter is not null?(g,bytes)=>customizationRouter.Accept(g,bytes):router is null?null:(g,bytes)=>router.Accept(g,bytes.Span));
            Queue("start");
            foreach(string prefix in new[]{"human","terminal"})foreach(string suffix in new[]{"create","baseline","end"}) {
                Queue(prefix+"_"+suffix);
                if(prefix=="terminal" && suffix=="baseline" && tangible6Fixture is not null)Queue("terminal_view6_baseline");
                if(prefix=="human" && suffix=="baseline" && creature4PartialFixture is not null)Queue("human_view4_partial");
                if(prefix=="human" && suffix=="baseline" && creature6Fixture is not null)Queue("human_view6_baseline");
                if(prefix=="human" && suffix=="baseline" && creature4Fixture is not null)Queue("human_view4_baseline");
            }
            environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};
            AddChild(new WorldEnvironment{Environment=environment});
            var camera=new Camera3D{Current=true,Fov=40,Position=new Vector3(0,1.3f,-9)};
            AddChild(camera);camera.LookAt(new Vector3(0,1.3f,0));
            if(args.Contains("--terrain-peer")) {
                Check(localInput && !cancel && !inputLoss,"Supplied terrain requires bounded local-input mode");
                LoadSuppliedTerrain(Arg("--terrain-peer"));
                camera.Position=suppliedLayerMotion is null?new Vector3(0,4,-9):new Vector3(0,6,-10);
                camera.LookAt(suppliedLayerMotion is null?new Vector3(0,.7f,0):new Vector3(0,2,0));
                if(heightFilteredTerrain)
                {
                    camera.Projection=Camera3D.ProjectionType.Orthogonal;camera.Size=22;
                    camera.Position=new Vector3(14,18,-20);camera.LookAt(new Vector3(0,4,0));
                }
                ConfigureFirstCurveTerrainCamera(camera);
            }
            host=new();host.FrameCompleted+=Frame;AddChild(host);host.Configure(pump,Before);
            _=ObserveHost();
        }
        catch(Exception error) { GD.PushError(error.ToString()); _=Fail(error); }
    }

    private LocalizedObjectNameResult ResolveName(CreatureView3NameState names) =>
        LocalizedObjectNameResolver.Resolve(names,path=>nameTables!.GetValueOrDefault(path),namePrimary,nameFallback);
    private void LoadNameContext(string path)
    {
        byte[] input=File.ReadAllBytes(path);
        try {
            Check(input.Length<=32768,"Bounded private name context");
            using var document=JsonDocument.Parse(input);var data=document.RootElement;
            Check(data.GetProperty("Version").GetInt32()==1 && !data.GetProperty("LiveLocaleConfiguration").GetBoolean(),"Explicit diagnostic locale policy");
            Check(data.GetProperty("PrimaryDirectory").GetString()==namePrimary && data.GetProperty("FallbackDirectory").GetString()==nameFallback,"Explicit primary/fallback directories");
            var pins=new Dictionary<string,(int Bytes,string Sha,int Records)> {
                ["string/en/obj_n.stf"]=(2527,"3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054",40),
                ["string/en/terminal_name.stf"]=(4610,"CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603",57)};
            var sources=new Dictionary<string,byte[]>(StringComparer.Ordinal);int bytesRead=0;
            try {
            foreach(var row in data.GetProperty("Tables").EnumerateArray()) {
                string resource=row.GetProperty("Path").GetString()!;Check(pins.TryGetValue(resource,out var pin),"Pinned original table path");
                byte[] bytes=Convert.FromHexString(row.GetProperty("Hex").GetString()!);
                try {
                    Check(row.GetProperty("Priority").GetInt32()==53 && row.GetProperty("Bytes").GetInt32()==pin.Bytes && row.GetProperty("Sha256").GetString()==pin.Sha && bytes.Length==pin.Bytes && Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))==pin.Sha,"Original STF context identity");
                    Check(row.GetProperty("Records").GetInt32()==pin.Records,"Original STF declared context count");
                    sources.Add(resource,bytes);bytesRead+=bytes.Length;bytes=Array.Empty<byte>();
                } finally {Array.Clear(bytes);}
            }
            Check(sources.Count==2 && bytesRead==7137,"Exactly two original table sources");
            int preloadCalls=0;
            ReadOnlyMemory<byte>? ReadOwned(string resource) {preloadCalls++;if(sources.TryGetValue(resource,out var bytes))return new ReadOnlyMemory<byte>(bytes);return null;}
            var set=LocalizedStringTableSet.Build(pins.Keys,ReadOwned,new(2,7137,97,1724,1723));
            Check(set.MissingPaths.Count==0 && set.TotalRecords==97 && preloadCalls==2 && set.Tables.All(p=>p.Value.Records.Count==pins[p.Key].Records),"Atomic bounded owned original table preload");
            nameTables=set.Tables.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);
            Check(nameTables.Count==2 && bytesRead==7137,"Exactly two original name tables preloaded");
            GD.Print("object_name_context="+JsonSerializer.Serialize(new{Tables=2,Records=97,Bytes=bytesRead,LiveLocaleConfiguration=false}));
            GD.Print("object_name_preload="+JsonSerializer.Serialize(new{preloadCalls,set.SourceBytes,set.TotalRecords,set.TotalValueUnits,set.TotalKeyBytes,Missing=set.MissingPaths.Count,FrameIo=false}));
            } finally {foreach(var bytes in sources.Values)Array.Clear(bytes);sources.Clear();}
        } finally {Array.Clear(input);}
    }
    private void QueueCreature6(int index) {
        byte[] bytes=Convert.FromHexString(creature6Fixture!.RootElement.GetProperty("Steps")[index].GetProperty("Hex").GetString()!);
        try {pump!.QueueApplication(generation,bytes);} finally {Array.Clear(bytes);}
    }
    private void Creature6State(int index) {
        if(creature6Full){Creature6FullState(index);return;}
        if(creature6Composite){Creature6CompositeState(index);return;}
        var snapshot=world!.FindCreatureView6(generation,humanId)!;
        Check(snapshot is not null && world.IsCurrent(snapshot),"Current CREO6 publication");
        var state=snapshot!.State;
        var expected=index==0?creature6Fixture!.RootElement.GetProperty("Seed"):creature6Fixture!.RootElement.GetProperty("Steps")[index-1];
        string modelHex=Convert.ToHexString(CreatureView6Codec.EncodeBaseline(state.Model,new(MaximumPayloadBytes:1024)));
        Check(modelHex==expected.GetProperty("ModelHex").GetString(),"Exact full CREO6 raw model/vector/counter retention");
        Check(ReferenceEquals(visuals[humanId].Publication,originalHuman) && visuals[humanId].CreatureVisual!.Root.Transform.IsEqualApprox(retainedCreature6Transform) && calls==3 && closed==3,"CREO6 preserves original visual/transform and performs no archive I/O");
        var events=world.DrainCreatureView6Events(generation)!;
        int count=index==0?0:expected.GetProperty("Events").GetInt32();
        Check(events.Events.Count==count && Enumerable.Range(0,count).All(i=>world.IsCurrentCreatureView6Event(events,i)),"Current finalized CREO6 event recipients");
        Check(world.AcknowledgeCreatureView6Events(events) && !world.AcknowledgeCreatureView6Events(events),"Exact CREO6 event acknowledgment once");
        Check(world.RetainedCreatureView6Bytes==state.DecodedBytes && state.DecodedBytes==expected.GetProperty("StateBytes").GetInt64(),"CREO6 retained logical charge after ack");
        GD.Print("creature6_state="+JsonSerializer.Serialize(new{Index=index,Name=index==0?"seed":expected.GetProperty("Name").GetString(),state.ObjectId,ModelHex=modelHex,StateRevision=state.Revision,StoreRevision=snapshot.Revision,RetainedBytes=world.RetainedCreatureView6Bytes,Events=count,Indices=events.Events.Select(e=>e.Event.Index).ToArray(),Current=true,AcknowledgedOnce=true,VisualRetained=true,Archives=closed}));
    }
    private void Creature6CompositeState(int index) {
        var snapshot=world!.FindCreatureView6Composite(generation,humanId)!;
        Check(snapshot is not null && world.IsCurrent(snapshot),"Current composite CREO6 publication");
        var state=snapshot!.State;
        var expected=index==0?creature6Fixture!.RootElement.GetProperty("Seed"):creature6Fixture!.RootElement.GetProperty("Steps")[index-1];
        string modelHex=Convert.ToHexString(CreatureView6Codec.EncodeBaseline(state.Model,new(MaximumPayloadBytes:1024)));
        Check(modelHex==expected.GetProperty("ModelHex").GetString(),"Exact scalar/HAM full model, raw counters and untouched vectors");
        Check(ReferenceEquals(visuals[humanId].Publication,originalHuman) && visuals[humanId].CreatureVisual!.Root.Transform.IsEqualApprox(retainedCreature6Transform) && calls==3 && closed==3,"Composite CREO6 retains original visual/transform without archive I/O");
        var batch=world.DrainCreatureView6CompositeEvents(generation)!;
        var events=batch.Events.Select(record=>new{Kind=record.Event.Kind.ToString(),record.Event.Field,record.Event.UpdateOrdinal,record.Event.Opcode,record.Event.ElementIndex,record.Event.OldValue,record.Event.NewValue}).ToArray();
        Check(Enumerable.Range(0,events.Length).All(i=>world.IsCurrentCreatureView6CompositeEvent(batch,i)),"Current finalized composite event recipients");
        Check(index==0?events.Length==0:JsonSerializer.Serialize(events)==JsonSerializer.Serialize(expected.GetProperty("Events")),"Independent exact ordered assignment/HAM event metadata");
        long pendingBytes=world.RetainedCreatureView6CompositeBytes;
        Check(pendingBytes==state.DecodedBytes+24L*events.Length,"Composite pending journal charge");
        Check(world.AcknowledgeCreatureView6CompositeEvents(batch) && !world.AcknowledgeCreatureView6CompositeEvents(batch),"Exact composite journal acknowledgment once");
        Check(world.RetainedCreatureView6CompositeBytes==state.DecodedBytes && state.DecodedBytes==expected.GetProperty("StateBytes").GetInt64(),"Composite current charge after ack");
        GD.Print("creature6_composite_state="+JsonSerializer.Serialize(new{Index=index,Name=index==0?"seed":expected.GetProperty("Name").GetString(),state.ObjectId,ModelHex=modelHex,StateRevision=state.Revision,StoreRevision=snapshot.Revision,RetainedBytes=world.RetainedCreatureView6CompositeBytes,Events=events,PendingBytesBeforeAck=pendingBytes,Current=true,AcknowledgedOnce=true,VisualRetained=true,Archives=closed}));
    }
    private void Creature6FullState(int index) {
        var snapshot=world!.FindCreatureView6Full(generation,humanId)!;
        Check(snapshot is not null && world.IsCurrent(snapshot),"Current full CREO6 publication");
        var state=snapshot!.State;
        var expected=index==0?creature6Fixture!.RootElement.GetProperty("Seed"):creature6Fixture!.RootElement.GetProperty("Steps")[index-1];
        string modelHex=Convert.ToHexString(CreatureView6Codec.EncodeBaseline(state.Model,new(MaximumPayloadBytes:1024)));
        Check(modelHex==expected.GetProperty("ModelHex").GetString(),"Exact all18 model, raw counters and ordered vectors");
        Check(ReferenceEquals(visuals[humanId].Publication,originalHuman) && visuals[humanId].CreatureVisual!.Root.Transform.IsEqualApprox(retainedCreature6Transform) && calls==3 && closed==3,"Full CREO6 retains original visual/transform without archive I/O");
        var batch=world.DrainCreatureView6FullEvents(generation)!;
        var events=batch.Events.Select(record=>new{Kind=record.Event.Kind.ToString(),record.Event.Field,record.Event.UpdateOrdinal,record.Event.Opcode,record.Event.Index,record.Event.OldId,record.Event.NewId,record.Event.OldValue,record.Event.NewValue}).ToArray();
        Check(Enumerable.Range(0,events.Length).All(i=>world.IsCurrentCreatureView6FullEvent(batch,i)),"Current finalized full event recipients");
        Check(index==0?events.Length==0:JsonSerializer.Serialize(events)==JsonSerializer.Serialize(expected.GetProperty("Events")),"Independent exact full ordered event metadata");
        long pendingBytes=world.RetainedCreatureView6FullBytes;
        Check(pendingBytes==state.DecodedBytes+48L*events.Length,"Full pending journal charge");
        Check(world.AcknowledgeCreatureView6FullEvents(batch) && !world.AcknowledgeCreatureView6FullEvents(batch),"Exact full journal acknowledgment once");
        Check(world.RetainedCreatureView6FullBytes==state.DecodedBytes && state.DecodedBytes==expected.GetProperty("StateBytes").GetInt64(),"Full current charge after ack");
        GD.Print("creature6_full_state="+JsonSerializer.Serialize(new{Index=index,Name=index==0?"seed":expected.GetProperty("Name").GetString(),state.ObjectId,ModelHex=modelHex,StateRevision=state.Revision,StoreRevision=snapshot.Revision,RetainedBytes=world.RetainedCreatureView6FullBytes,Events=events,PendingBytesBeforeAck=pendingBytes,Current=true,AcknowledgedOnce=true,VisualRetained=true,Archives=closed}));
    }
    private void QueueTangible6(int index)
    {
        byte[] bytes=Convert.FromHexString(tangible6Fixture!.RootElement.GetProperty("Steps")[index].GetProperty("Hex").GetString()!);
        try {pump!.QueueApplication(generation,bytes);}finally{Array.Clear(bytes);}
    }
    private void Tangible6State(int index)
    {
        var snapshot=world!.FindTangibleView6(generation,terminalId)!;
        var state=snapshot.State;
        var expected=index==0?tangible6Fixture!.RootElement.GetProperty("Seed"):tangible6Fixture!.RootElement.GetProperty("Steps")[index-1];
        Check(world.IsCurrent(snapshot) && state.Passive==expected.GetProperty("Passive").GetUInt32() && state.Counter==expected.GetProperty("Counter").GetUInt32() && state.Ids.SequenceEqual(expected.GetProperty("Ids").EnumerateArray().Select(v=>v.GetUInt64())),"Current TANO6 literal state");
        var visual=visuals[terminalId].StaticVisual!;
        bool gpu=DisplayServer.GetName()!="headless";
        Check(ReferenceEquals(visuals[terminalId].Publication,retainedTerminal) && visual.Root.Transform==retainedTerminalTransform && visual.Fingerprint(gpu)==retainedTerminalFingerprint,"TANO6 retains original visual and resource publication");
        var events=world.DrainTangibleView6Events(generation)!;
        Check(Enumerable.Range(0,events.Events.Count).All(i=>world.IsCurrentTangibleView6Event(events,i)),"Current finalized TANO6 event recipients");
        Check(world.AcknowledgeTangibleView6Events(events) && !world.AcknowledgeTangibleView6Events(events),"Exact TANO6 event acknowledgment once");
        Check(world.RetainedTangibleView6Bytes==state.DecodedBytes,"TANO6 retained logical accounting after ack");
        GD.Print("tangible6_state="+JsonSerializer.Serialize(new{Index=index,Name=index==0?"seed":expected.GetProperty("Name").GetString(),state.ObjectId,state.Passive,state.Counter,Ids=state.Ids.ToArray(),StateRevision=state.Revision,StoreRevision=snapshot.Revision,RetainedBytes=world.RetainedTangibleView6Bytes,Events=events.Events.Count,Current=true,AcknowledgedOnce=true,VisualRetained=true,Archives=closed}));
    }
    private void Queue(string key)
    {
        byte[] bytes=Convert.FromHexString(peer[key]);
        try { pump!.QueueApplication(generation,bytes); } finally { Array.Clear(bytes); }
    }
    private void Before(double seconds)
    {
        AdvanceSourcedInput();
        AdvanceNominalSpeed();
        if(++frames>1500)throw new InvalidDataException("Frame deadline");
        if(phase==0 && pump!.QueuedMessageCount==0 && world!.State==ZoneSceneTransitionState.Initializing) {
            Check(world.CompleteMinimumSceneBarrier(generation,true)is not null,"Explicit host barrier");
            Queue("ready");phase=1;
        }
        if(cancel && Volatile.Read(ref entered)==2 && !finishing)ScheduleFinish();
        if(inputHost is not null && !finishing) {
            localClock+=phase is 10 or 11 or 14?250000:50001;
            var state=inputHost.Advance(localClock);
            Check(state.FaultType is null,"Healthy local driver");
        }
    }
    private void Frame(SceneWorldPresentationPumpStep receipt)
    {
        Check(receipt.AcceptedMessages+receipt.DroppedMessages<=2 && receipt.PublishedResources<=1,"Per-frame work budget");
        GD.Print("pump_frame="+JsonSerializer.Serialize(new{frames,receipt,actors=pump!.Consumer.Count}));
        if(cancel || finishing)return;
        if(sourcedStarted && !sourcedComplete){FrameSourcedInput();return;}
        if(nominalStarted && !nominalComplete){FrameNominalSpeed();return;}
        if(phase==1 && pump!.Consumer.Count==2) {
            var h=visuals[humanId];
            var t=visuals[terminalId];
            Check(h.CreatureVisual!.Verify(DisplayServer.GetName()!="headless") && t.StaticVisual!.Verify(DisplayServer.GetName()!="headless"),"Visual boundary");
            originalHuman=h.Publication;humanIds=h.CreatureVisual.Fingerprint();firstIdleFrame=h.CreatureVisual.IdleFrame;
            Check(calls==2 && closed==2 && t.Publication.Resources.ExternalReads==(terminalAnimationEnabled?34:27),"Original closed resource jobs");
            if(faceInspection is null)Check(h.Publication.Resources.ExternalReads==39,"Default human closure");
            else {
                var resource=h.Publication.Resources;var generated=resource.Creature!.Creature.GeneratedFaces!;
                var accounting=SceneObjectResourceAccounting.Measure(resource);
                Check(generated.Bindings.Count==1 && generated.Outputs.Count==1 && accounting.GeneratedFaces==1,"Explicit generated face closure");
                Check(generated.Bindings.Keys.Single()==new BlueprintGeneratedTextureKey("appearance/mesh/hum_m_head_l0.mgn",0,0x4D41494E),"Exact generated MAIN destination");
                Check(generated.Values.Count==faceInspection.Values.Count && generated.Values.All(p=>faceInspection.Values.TryGetValue(p.Key,out var v)&&v==p.Value),"Copied supplied inspection snapshot");
                GD.Print("face_resource="+JsonSerializer.Serialize(new {
                    faceProfileSha256,resource.ExternalReads,resource.SourceBytes,Accounting=accounting,
                    Sources=resource.Sources.Values.OrderBy(s=>s.Path).ToArray(),
                    Bindings=generated.Bindings.Select(p=>new{p.Key.MeshPath,p.Key.ShaderIndex,p.Key.TextureTag,p.Value.RendererPath,p.Value.ShaderPath}).ToArray(),
                    Outputs=generated.Outputs.Select(p=>new{Renderer=p.Key,p.Value.Image.Width,p.Value.Image.Height,Bytes=p.Value.Image.Mips.Sum(m=>m.Rgba.Length),Sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(p.Value.Image.Mips[0].Rgba.Span))}).ToArray()
                }));
            }
            GD.Print("pump_state="+JsonSerializer.Serialize(new{stage="initial",frames,calls,closed,actors=pump.Consumer.Count}));
            Queue("human_height");Queue("terminal_delta");
            if(motions is not null)Check(RouteMotion("move_a"),"First authoritative pose");
            phase=2;
        } else if(phase==2 && pump!.QueuedMessageCount==0) {
            Check(BitConverter.SingleToUInt32Bits(world!.FindReplication(generation,humanId)!.Scalars.Height)==BitConverter.SingleToUInt32Bits(1.4f),"Latest Height");
            Check(calls==2 && ReferenceEquals(visuals[humanId].Publication,originalHuman),"No state reload");
            if(motions is not null)MotionState("moved",new System.Numerics.Vector3(-.5f,0,1));
            Queue("terminal_destroy");phase=3;
        } else if(phase==3 && pump!.Consumer.Count==1) {
            Check(pump.Consumer.Find(terminalId)is null,"Only terminal retired");
            Queue("terminal_create");if(tangibleFixture is not null)Queue("terminal_baseline");if(tangible6Fixture is not null)Queue("terminal_view6_baseline");Queue("terminal_end");phase=4;
        } else if(phase==4 && pump!.Consumer.Count==2 && calls==3) {
            Check(closed==3 && visuals[humanId].CreatureVisual!.Fingerprint()==humanIds &&
                ReferenceEquals(visuals[humanId].Publication,originalHuman),"Human retained through static reload");
            Check(visuals[humanId].CreatureVisual!.IdleFrame>firstIdleFrame,"Continuous idle tick");
            GD.Print("pump_state="+JsonSerializer.Serialize(new{stage="recreated",frames,calls,closed,actors=pump.Consumer.Count}));
            if(motions is null)ScheduleFinish();
            else if(localInput) {
                inputHost=new(()=>localClock);AddChild(inputHost);
                inputHost.Configure(CreateLocalDriver());
                KeyInput(Key.W,true);phase=8;
            }
            else {Check(RouteMotion("move_b") && !RouteMotion("move_stale"),"Wrap and stale motion");phase=5;}
        } else if(phase==5) {
            MotionState("wrapped",new System.Numerics.Vector3(-1.5f,0,-1));
            motions!.Clear();phase=6;
        } else if(phase==6) {
            var original=originalHuman!.Placement.Transform.Translation;
            Check(visuals[humanId].CreatureVisual!.Root.Position.DistanceTo(new Vector3(original.X,original.Y,-original.Z))<1e-5f,"Missing motion restores create pose");
            var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);
            Check(motions!.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new System.Numerics.Vector3(-.75f,0,.5f)},1),"Republish local after clear");
            phase=7;
        } else if(phase==7) {
            MotionState("local",new System.Numerics.Vector3(-.75f,0,.5f));
            ScheduleFinish();
        } else if(phase==8) {
            LocalState("walk",new(-.5f,0,1.100002f),2,"walk");KeyInput(Key.Shift,true);phase=9;
        } else if(phase==9) {
            LocalState("run",new(-.5f,0,1.400008f),6,"run");
            suspendedPosition=inputHost!.LastState!.Transform.Translation;
            inputHost.SetPaused(true);locomotion!.SetPlaying(false);phase=10;
        } else if(phase==10) {
            LocalState("paused",suspendedPosition,6,"run");
            RouteMotion("move_b");RouteMotion("move_stale");phase=11;
        } else if(phase==11 && pump!.QueuedMessageCount==0 && inputHost!.LastState!.Transform.Translation==new System.Numerics.Vector3(-1.5f,0,-1)) {
            LocalState("corrected_paused",new(-1.5f,0,-1),1,"walk");
            Check(motions!.Find(world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId))!.MovementCounter==0,"Counter wrap/stale rejection through queue");
            inputHost!.SetPaused(false);inputHost.Advance(localClock);locomotion!.SetPlaying(true);KeyInput(Key.W,true);KeyInput(Key.Shift,true);phase=12;
        } else if(phase==12) {
            LocalState("resumed",new(-1.5f,0,-.699994f),6,"run");KeyInput(Key.W,false);KeyInput(Key.Shift,false);phase=13;
        } else if(phase==13) {
            LocalState("idle",new(-1.5f,0,-.699994f),0,"idle");
            KeyInput(Key.W,true);inputHost!.SetFocused(false);locomotion!.SetFocused(false);phase=14;
        } else if(phase==14) {
            LocalState("unfocused",new(-1.5f,0,-.699994f),0,"idle");
            inputHost!.SetFocused(true);inputHost.Advance(localClock);locomotion!.SetFocused(true);phase=15;
        } else if(phase==15) {
            LocalState("focus_resumed",new(-1.5f,0,-.699994f),0,"idle");
            inputHost!.Reset(localClock);locomotion!.Reset();phase=16;
        } else if(phase==16) {
            LocalState("reset",originalHuman!.Placement.Transform.Translation,0,"idle");
            if(inputLoss){Queue("human_destroy");phase=17;}
            else if(customizationWire) {
                inputHost!.SetPaused(true);locomotion!.SetPlaying(false);
                retainedCustomizationTransform=visuals[humanId].CreatureVisual!.Root.Transform;retainedCustomizationFrame=locomotion.Frame;
                QueueWire("wire_zero");phase=31;
            }
            else if(customizationRevisions) {
                inputHost!.SetPaused(true);locomotion!.SetPlaying(false);
                retainedCustomizationTransform=visuals[humanId].CreatureVisual!.Root.Transform;
                retainedCustomizationFrame=locomotion.Frame;
                SupplyCustomization(1,customizationZero!);phase=20;
            } else ScheduleFinish();
        } else if(phase==20 && customizationVisual!.LastAppliedRevision==1) {
            CustomizationState("zero",1);
            SupplyCustomization(2,customizationVaried!);SupplyCustomization(3,customizationZero!);SupplyCustomization(4,customizationVaried!);phase=22;
        } else if(phase==22 && customizationVisual!.LastAppliedRevision==4) {
            CustomizationState("latest_varied",4);
            var invalid=customizationZero!.Skip(1).ToDictionary(p=>p.Key,p=>p.Value);
            SupplyCustomization(5,invalid);phase=23;
        } else if(phase==23 && customizationVisual!.LastFailure is not null) {
            Check(customizationVisual.LastAppliedRevision==4,"Failed composition retains appearance");
            CustomizationState("failed_retained",4);SupplyCustomization(6,customizationZero!);phase=24;
        } else if(phase==24 && customizationVisual!.LastAppliedRevision==6) {
            CustomizationState("restored_zero",6);customizationSource.Current=null;phase=25;
        } else if(phase==25) {
            CustomizationState("missing_retained",6);
            if(customizationClosePending) {
                Volatile.Write(ref customizationHold,1);SupplyCustomization(7,customizationVaried!);
                Check(customizationVisual!.Tick(0),"Held customization accepted by current adapter");phase=27;
            } else {phase=26;ScheduleFinish();}
        } else if(phase==27 && Volatile.Read(ref customizationEntered)==1) {
            Check(!customizationVisual!.DrainCompletion.IsCompleted && customizationVisual.LastAppliedRevision==6,"Real customization worker pending before close");
            phase=28;ScheduleFinish();
        } else if(phase==31 && WireApplied()) {
            WireState("zero","zero");QueueWire("wire_varied");QueueWire("wire_invalid_skin");phase=32;
        } else if(phase==32 && WireApplied()) {
            WireState("ordered_valid_then_invalid","varied");QueueWire("wire_duplicate");phase=33;
        } else if(phase==33 && WireApplied()) {
            WireState("duplicate_retains_prior_valid","varied");QueueWire("wire_empty");QueueWire("wire_count_zero");phase=34;
        } else if(phase==34 && pump!.QueuedMessageCount==0) {
            WireState("empty_forms_retain","varied");QueueWire("wire_extended_zero");phase=36;
        } else if(phase==36 && WireApplied()) {
            WireState("extended_zero_restore","zero");phase=37;
            if(tangibleFixture is null)ScheduleFinish();
            else {
                retainedTerminal=visuals[terminalId].Publication;
                retainedTerminalFingerprint=visuals[terminalId].StaticVisual!.Fingerprint(DisplayServer.GetName()!="headless");
                retainedTerminalTransform=visuals[terminalId].StaticVisual!.Root.Transform;
                TangibleState(0);QueueTangible(0);tangibleIndex=1;phase=40;
            }
        } else if(phase==40 && pump!.QueuedMessageCount==0) {
            TangibleState(tangibleIndex);
            if(tangibleIndex<4){QueueTangible(tangibleIndex);tangibleIndex++;}
            else if(tangible6Fixture is not null && tangible6Index==0) {
                Tangible6State(0);QueueTangible6(0);tangible6Index=1;phase=50;
            }
            else if(terminalAnimationEnabled) {
                terminalAnimationStructure=visuals[terminalId].StaticVisual!.StructureFingerprint(DisplayServer.GetName()!="headless");
                terminalTextureIds.Clear();AnimationState("initial",0);terminalAnimation!.SetPlaying(true);terminalAnimationStep=0;phase=42;
            }
            else {phase=41;ScheduleFinish();}
        } else if(phase==50 && pump!.QueuedMessageCount==0) {
            Tangible6State(tangible6Index);
            if(tangible6Index<5){QueueTangible6(tangible6Index);tangible6Index++;}
            else if(creature6Fixture is not null) {
                retainedCreature6Transform=visuals[humanId].CreatureVisual!.Root.Transform;
                Creature6State(0);QueueCreature6(0);creature6Index=1;phase=51;
            }
            else {
                terminalAnimationStructure=visuals[terminalId].StaticVisual!.StructureFingerprint(DisplayServer.GetName()!="headless");
                terminalTextureIds.Clear();AnimationState("initial",0);terminalAnimation!.SetPlaying(true);terminalAnimationStep=0;phase=42;
            }
        } else if(phase==51 && pump!.QueuedMessageCount==0) {
            Creature6State(creature6Index);
            if(creature6Index<5){QueueCreature6(creature6Index);creature6Index++;}
            else if(creature4PartialFixture is not null){Creature4PartialState(0);QueueCreature4Partial(0);creature4PartialIndex=1;phase=53;}
            else if(creature4Fixture is not null) {Creature4FullState(0);QueueCreature4Full(0);creature4Index=1;phase=52;}
            else {
                terminalAnimationStructure=visuals[terminalId].StaticVisual!.StructureFingerprint(DisplayServer.GetName()!="headless");
                terminalTextureIds.Clear();AnimationState("initial",0);terminalAnimation!.SetPlaying(true);terminalAnimationStep=0;phase=42;
            }
        } else if(phase==52 && pump!.QueuedMessageCount==0) {
            Creature4FullState(creature4Index);
            if(creature4Index<5){QueueCreature4Full(creature4Index);creature4Index++;}
            else {
                terminalAnimationStructure=visuals[terminalId].StaticVisual!.StructureFingerprint(DisplayServer.GetName()!="headless");
                terminalTextureIds.Clear();AnimationState("initial",0);terminalAnimation!.SetPlaying(true);terminalAnimationStep=0;phase=42;
            }
        } else if(phase==53 && pump!.QueuedMessageCount==0) {
            Creature4PartialState(creature4PartialIndex);
            if(creature4PartialIndex<5){QueueCreature4Partial(creature4PartialIndex);creature4PartialIndex++;}
            else {terminalAnimationStructure=visuals[terminalId].StaticVisual!.StructureFingerprint(DisplayServer.GetName()!="headless");terminalTextureIds.Clear();AnimationState("initial",0);terminalAnimation!.SetPlaying(true);terminalAnimationStep=0;phase=42;}
        } else if(phase==42) {
            terminalAnimationStep++;
            AnimationState("frame"+terminalAnimationStep,BitConverter.SingleToUInt32Bits(visuals[terminalId].Publication.Resources.Static!.SwitchFrames!.Interval));
            if(terminalAnimationStep==8){terminalAnimation!.SetPlaying(false);phase=43;}
        } else if(phase==43) {
            Check(terminalAnimation!.AdvancePlayback(.3f),"Multiple supplied frame steps");AnimationState("multi",BitConverter.SingleToUInt32Bits(.3f));phase=44;
        } else if(phase==44) {
            if(terminalClockFaultEnabled) {
                var before=terminalAnimation!.Animation.Snapshot;ulong tick=terminalAnimation.Playback!.ClockSnapshot.Tick;bool rejected=false;
                try{terminalAnimation.AdvancePlayback(500f);}catch(InvalidDataException){rejected=true;}
                bool retained=ReferenceEquals(before,terminalAnimation.Animation.Snapshot);
                Check(rejected&&retained&&terminalAnimation.IsRetired&&terminalAnimation.Animation.IsRetired&&terminalAnimation.Playback.IsFaulted&&terminalAnimation.Playback.IsClosed&&terminalAnimation.Playback.ClockSnapshot.RegisteredCount==0,"Consumed clock service faults and retires visual without retry");
                GD.Print("terminal_clock_fault="+JsonSerializer.Serialize(new{BeforeTick=tick,AfterTick=terminalAnimation.Playback.ClockSnapshot.Tick,ClockConsumed=terminalAnimation.Playback.ClockSnapshot.Tick>tick,Faulted=true,Retired=true,CursorRetained=retained,Registered=terminalAnimation.Playback.ClockSnapshot.RegisteredCount,NoRetry=true}));
                phase=48;ScheduleFinish();return;
            }
            Check(terminalAnimation!.AdvancePlayback(0),"Zero supplied delta");AnimationState("zero",0);
            if(terminalClockEnabled){var before=terminalAnimation.Animation.Snapshot;Check(terminalAnimation.ServiceSameTick()&&ReferenceEquals(before,terminalAnimation.Animation.Snapshot),"Same tick does not replay elapsed seconds");AnimationState("same_tick",0);}
            phase=45;
        } else if(phase==45) {
            Check(terminalAnimation!.ResetPlayback(),"Reset supplied cursor");AnimationState("reset",0);phase=46;
        } else if(phase==46) {
            var before=terminalAnimation!.Animation.Snapshot;ulong texture=terminalAnimation.Animation.SelectedTextureId;bool rejected=false;
            var clockBefore=terminalAnimation.Playback?.ClockSnapshot;
            try{terminalAnimation.AdvancePlayback(-1);}catch(ArgumentOutOfRangeException){rejected=true;}
            Check(rejected&&ReferenceEquals(before,terminalAnimation.Animation.Snapshot)&&texture==terminalAnimation.Animation.SelectedTextureId,"Invalid supplied delta preserves state/binding");
            Check(!terminalClockEnabled||ReferenceEquals(clockBefore,terminalAnimation.Playback!.ClockSnapshot),"Invalid clock input retains receipt");
            AnimationState("invalid_retained",BitConverter.SingleToUInt32Bits(-1f));phase=47;ScheduleFinish();
        }
    }
    private void AnimationState(string name,uint deltaBits) {
        var animation=terminalAnimation!.Animation;var cursor=animation.Snapshot;var visual=visuals[terminalId].StaticVisual!;
        bool gpu=DisplayServer.GetName()!="headless";
        var state=world!.FindTangibleReplication(generation,terminalId)!;
        var bundle=visuals[terminalId].Publication.Resources.Static!.SwitchFrames!;
        var clock=terminalAnimation.Playback?.Find(terminalId)?.Clock;
        Check(terminalAnimation.VerifyClockCursor(),"Clock and cached selector agree");
        string path=bundle.Frames[cursor.Index].TexturePath;
        Check(animation.VerifySelected(gpu)&&animation.CursorsSynchronized&&visual.StructureFingerprint(gpu)==terminalAnimationStructure&&visual.Root.Transform==retainedTerminalTransform&&ReferenceEquals(visuals[terminalId].Publication,retainedTerminal),"Animated binding preserves geometry/pose/publication");
        Check(calls==3&&closed==3&&world.IsCurrent(state)&&tangibleVisual!.Text==ResolveName(state.Names).DisplayText,"Animation preserves typed caption/state without archive I/O");
        if(terminalTextureIds.TryGetValue(cursor.Index,out var previous))Check(previous==animation.SelectedTextureId,"One upload per selected frame");else terminalTextureIds.Add(cursor.Index,animation.SelectedTextureId);
        GD.Print("terminal_animation_state="+JsonSerializer.Serialize(new{Name=name,DeltaBits=deltaBits,cursor.Index,ResidualBits=BitConverter.SingleToUInt32Bits(cursor.Residual),cursor.Revision,Advances=cursor.CumulativeAdvances,Path=path,MipHashes=terminalMipHashes[path],Structure=terminalAnimationStructure,Pose=visual.Root.Transform.ToString(),Publication=visuals[terminalId].Publication.Ticket.Handle.ObjectId,Calls=calls,Closed=closed,Copies=animation.ImageCopies,Uploads=animation.UniqueUploads,ScreenBindings=2,TextureId=animation.SelectedTextureId,Gpu=gpu,GpuVerified=gpu,CaptionRetained=true,TypedStateCurrent=true,cursor.NumericPolicy,OriginalParity=false,TimePolicy=terminalClockEnabled?"synthetic_fixed_seconds_through_owned_clock":"synthetic_fixed_seconds",ClockTick=terminalAnimation.Playback?.ClockSnapshot.Tick,ClockFrom=clock?.FromTick,ClockThrough=clock?.ThroughTick,ClockElapsedBits=clock is null?(uint?)null:BitConverter.SingleToUInt32Bits(clock.ElapsedSeconds),ClockToken=clock?.RegistrationToken,ClockSynchronized=terminalAnimation.VerifyClockCursor()}));
    }
    private void QueueTangible(int index) {
        byte[] bytes=Convert.FromHexString(tangibleFixture!.RootElement.GetProperty("Updates")[index].GetProperty("Hex").GetString()!);
        try{pump!.QueueApplication(generation,bytes);}finally{Array.Clear(bytes);}
    }
    private void TangibleState(int index) {
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==terminalId);
        var state=snapshot.TangibleReplication!;var expected=tangibleFixture!.RootElement.GetProperty("Expected")[index];
        var visual=visuals[terminalId].StaticVisual!;var name=ResolveName(state.Names);
        Check(world.IsCurrent(state) && tangibleVisual is not null && tangibleVisual.LastFailure is null &&
            tangibleVisual.Text==name.DisplayText && tangibleVisual.Revision==state.Revision,"Current typed tangible caption");
        Check(state.TangibleScalars.Visibility==expected.GetProperty("Visibility").GetByte() && visual.Root.Visible==(state.TangibleScalars.Visibility!=0) &&
            tangibleVisual!.LabelVisible==(state.TangibleScalars.Visibility!=0),"Typed visibility hides body and caption");
        Check(ReferenceEquals(visuals[terminalId].Publication,retainedTerminal) && visual.Fingerprint(DisplayServer.GetName()!="headless")==retainedTerminalFingerprint &&
            visual.Root.Transform==retainedTerminalTransform && calls==3 && closed==3,"Tangible updates preserve original publication, texture readback and pose without I/O");
        GD.Print("tangible_world_state="+JsonSerializer.Serialize(new {
            Name=expected.GetProperty("Name").GetString(),state.Generation,state.Revision,Incarnation=state.Incarnation,
            NameFileHex=Convert.ToHexString(state.Names.NameFile.CopyBytes()),NameKeyHex=Convert.ToHexString(state.Names.NameKey.CopyBytes()),
            NameSpacer=state.Names.NameSpacer,NameCustomUnits=state.Names.CustomNameUnits,NameFileExtended=state.Names.NameFile.UsesExtendedLength,NameKeyExtended=state.Names.NameKey.UsesExtendedLength,
            AdditionalNames=state.Names.AdditionalDecodedBytes(state.Baseline),CustomizationHex=Convert.ToHexString(state.Customization.CopyBytes()),CustomizationExtended=state.Customization.UsesExtendedLength,
            Components=state.Components.Keys,ComponentCounter=state.Components.Counter,state.TangibleScalars.Visibility,
            ComplexityBits=BitConverter.SingleToUInt32Bits(state.TangibleScalars.Complexity).ToString("X8"),state.TangibleScalars.ConditionDamage,state.TangibleScalars.UseCount,
            RetainedDecodedBytes=world.RetainedTangibleReplicationDecodedBytes,DisplayText=name.DisplayText,Origin=name.Origin.ToString(),LabelText=tangibleVisual!.Text,
            LabelVisible=tangibleVisual.LabelVisible,RootVisible=visual.Root.Visible,PublicationRetained=true,TextureReadbackRetained=true,PoseRetained=true,calls,closed}));
    }
    private void QueueWire(string key) {
        byte[] bytes=Convert.FromHexString(wirePeer![key]);
        try {pump!.QueueApplication(generation,bytes);}finally{Array.Clear(bytes);}
    }
    private SceneWorldRawCustomizationBatch WireBatch() => rawCustomizations!.Find(world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId))!;
    private bool WireApplied() => pump!.QueuedMessageCount==0 && customizationVisual!.LastAppliedRevision==WireBatch().ThroughRevision;
    private void WireState(string name,string expected) {
        var batch=WireBatch();var visual=visuals[humanId].CreatureVisual!;
        string hash=expected=="zero"?"A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808":"60C109DF85346F25B814E303B6D022D7D2D94459234790B6337ABB1877E7B8F5";
        Check(customizationVisual!.GeneratedFaceSha256==hash && batch.Updates.Count==0,"Ordered packet projection hash and acknowledged journal");
        float expectedHeight=customizationScalars?name switch {"ordered_valid_then_invalid"=>1.7f,"duplicate_retains_prior_valid"=>1.2f,"empty_forms_retain"=>1.3f,_=>1.4f}:1.4f;
        var currentReplication=world!.FindReplication(generation,humanId)!;var currentScalars=currentReplication.Scalars;
        var tangible=currentReplication.TangibleScalars;
        var names=currentReplication.Names;
        var nameResult=customizationNamed?ResolveName(names):null;
        if(customizationNamed)Check(nameVisual!.Text==nameResult!.DisplayText && nameVisual.IsVisibleInTree==visual.Root.Visible,"Current names and inherited visibility are presented");
        if(customizationExtended)Check(visual.Root.Visible==(tangible.Visibility!=0),"Current raw common visibility is presented");
        var expectedTransform=customizationScalars?new Transform3D(retainedCustomizationTransform.Basis.Scaled(Vector3.One*(expectedHeight/1.4f)),retainedCustomizationTransform.Origin):retainedCustomizationTransform;
        Check(visual.Root.Transform.IsEqualApprox(expectedTransform) && currentScalars.Height==expectedHeight && locomotion!.Frame==retainedCustomizationFrame && ReferenceEquals(visuals[humanId].Publication,originalHuman),"Wire updates retain native pose clock/publication and apply latest Height exactly once");
        Check(calls==3 && closed==3 && customizationDictionary!.Entries.Count==121 && customizationRanges!.Count==8 && visual.Verify(DisplayServer.GetName()!="headless"),"Owned original dictionary/context upload and no archive reload");
        if(name=="extended_zero_restore")Check(batch.Current.UsesExtendedLength,"Noncanonical extended field preserved");
        GD.Print("wire_customization_state="+JsonSerializer.Serialize(new{Name=name,Applied=customizationVisual.LastAppliedRevision,Raw=batch.ThroughRevision,Pending=batch.Updates.Count,RawBytes=batch.Current.Length,Extended=batch.Current.UsesExtendedLength,FaceSha256=customizationVisual.GeneratedFaceSha256,Frame=locomotion!.Frame,Height=currentScalars.Height,Mixed=customizationScalars,Full=customizationFull,ExtendedFields=customizationExtended,NamedFields=customizationNamed,NameFileHex=Convert.ToHexString(names.NameFile.CopyBytes()),NameKeyHex=Convert.ToHexString(names.NameKey.CopyBytes()),NameSpacer=names.NameSpacer,NameCustomUnits=names.CustomNameUnits,NameFileExtended=names.NameFile.UsesExtendedLength,NameKeyExtended=names.NameKey.UsesExtendedLength,NameAdditionalBytes=names.AdditionalDecodedBytes(currentReplication.Baseline),NameDisplay=nameResult?.DisplayText,NameOrigin=nameResult?.Origin.ToString(),NameMatchedId=nameResult?.MatchedId,NameLabelText=nameVisual?.Text,NameLabelVisible=nameVisual?.IsVisibleInTree,Tangible=tangible,ComplexityBits=BitConverter.SingleToUInt32Bits(tangible.Complexity).ToString("X8"),RootVisible=visual.Root.Visible,AuthoredContext=authoredContext is not null,SourceBindings=authoredContext?.Bindings.Count??0,Components=currentReplication.Components?.Keys,ComponentCounter=currentReplication.Components?.Counter,Wounds=currentReplication.Wounds?.Values,WoundsCounter=currentReplication.Wounds?.Counter,Scalars=currentScalars,BasisLengths=new[]{visual.Root.Transform.Basis.X.Length(),visual.Root.Transform.Basis.Y.Length(),visual.Root.Transform.Basis.Z.Length()},calls,closed}));
    }
    private void SupplyCustomization(ulong revision,IReadOnlyDictionary<ShaderCustomizationKey,int> values) {
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);
        customizationSource.Current=new(snapshot.Object.Incarnation,revision,values);
    }
    private void CustomizationState(string name,ulong revision) {
        var visual=visuals[humanId].CreatureVisual!;
        Check(customizationVisual!.LastAppliedRevision==revision && visual.Root.Transform==retainedCustomizationTransform && locomotion!.Frame==retainedCustomizationFrame,"Customization retains native placement Height and clock");
        Check(ReferenceEquals(visuals[humanId].Publication,originalHuman) && calls==3 && closed==3,"Customization preserves publication and performs no archive I/O");
        Check(visual.Verify(DisplayServer.GetName()!="headless"),"Customization actual upload readback");
        GD.Print("customization_state="+JsonSerializer.Serialize(new{Name=name,Revision=revision,Frame=locomotion!.Frame,calls,closed,Height=1.4f,FaceSha256=customizationVisual.GeneratedFaceSha256,Fingerprint=visual.Fingerprint(gpu:DisplayServer.GetName()!="headless")}));
    }
    private void KeyInput(Key key,bool pressed) {
        using var input=new InputEventKey{PhysicalKeycode=key,Pressed=pressed};
        Check(inputHost!.HandleInput(input),"Accepted key transition");
    }
    private void LocalState(string name,System.Numerics.Vector3 expected,float speed,string clip) {
        expected=TerrainExpected(name,expected);
        var state=inputHost!.LastState!;var position=state.Transform.Translation;
        var visual=visuals[humanId].CreatureVisual!;var origin=visual.Root.Position;
        Check(System.Numerics.Vector3.Distance(position,expected)<2e-5f && MathF.Abs(state.Speed-speed)<1e-5f,"Integer-clock local pose/speed");
        Check(origin.DistanceTo(new Vector3(expected.X,expected.Y,-expected.Z))<2e-5f,"Local native/engine boundary");
        Check(locomotion!.SelectedClip==clip && visual.Fingerprint()==humanIds && ReferenceEquals(visuals[humanId].Publication,originalHuman),"Clip and resource retention");
        Check(MathF.Abs(visual.Root.Transform.Basis.X.Length()-1.4f)<1e-5f && calls==3 && closed==3,"Height once and no motion reload");
        GD.Print("local_state="+JsonSerializer.Serialize(new{Name=name,Native=new[]{position.X,position.Y,position.Z},Engine=new[]{origin.X,origin.Y,origin.Z},state.Speed,state.TimestampMicroseconds,state.StepsThisUpdate,state.DroppedMicroseconds,state.SuspendedMicroseconds,state.Active,Clip=clip,Frame=locomotion.Frame,calls,closed,Height=1.4f}));
    }
    private bool RouteMotion(string name)
    {
        byte[] bytes=Convert.FromHexString(motionPeer![name]);
        try {
            if(localInput){pump!.QueueApplication(generation,bytes);return true;}
            var update=CompactSceneTransformCodec.Decode(bytes);
            Check(update.ObjectId==humanId && update.ParentId is null && update.DirectionRaw==0,"Explicit world-space zero-yaw diagnostic");
            var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==update.ObjectId);
            var pose=AppearanceTransform.Identity with {Translation=update.NativePosition};
            return motions!.PublishAuthoritative(snapshot,pose,update.SpeedRaw,update.MovementCounter);
        } finally {Array.Clear(bytes);}
    }
    private void MotionState(string name,System.Numerics.Vector3 expected)
    {
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);
        var motion=motions!.Find(snapshot)!;var visual=visuals[humanId].CreatureVisual!;
        Check(motion.Transform.Translation==expected && motions.IsCurrent(motion),"Current native pose");
        var origin=visual.Root.Transform.Origin;
        Check(origin.DistanceTo(new Vector3(expected.X,expected.Y,-expected.Z))<1e-5f,"Exactly one engine Z reflection");
        Check(MathF.Abs(visual.Root.Transform.Basis.X.Length()-1.4f)<1e-5f,"Height composes once");
        Check(ReferenceEquals(visuals[humanId].Publication,originalHuman),"Motion preserves resource identity");
        var body=SceneCreatureBodyEnvelope.Create(originalHuman!.Resources,1.4f);
        GD.Print("motion_body="+JsonSerializer.Serialize(body,new JsonSerializerOptions{IncludeFields=true}));
        GD.Print("motion_state="+JsonSerializer.Serialize(new{Name=name,motion.Revision,motion.MovementCounter,
            Native=new[]{expected.X,expected.Y,expected.Z},Engine=new[]{origin.X,origin.Y,origin.Z},Height=1.4f,
            CreateHex=Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(originalHuman.Resources.Create)),calls}));
    }
    private void ScheduleFinish(){if(nominalRequested&&!nominalStarted&&phase==47){BeginNominalSpeed();return;}if(sourcedRequested && !sourcedStarted && phase==47){BeginSourcedInput();return;}finishing=true;CallDeferred(MethodName.Finish);}
    public async void Finish()
    {
        try {
            Check(cancel?calls==2 && entered==2:(phase==4 || phase==7 || phase==16 || phase==26 || phase==28 || phase==37 || phase==41 || phase==47 || phase==48 || phase==64 || phase==71) && frames>=7,"Continuous frame/lifecycle proof");
            if(motionCapture is not null) {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=GetViewport().GetTexture().GetImage();
                if(image.SavePng(motionCapture)!=Error.Ok)throw new IOException("Motion capture");
            }
            inputHost?.Close();CloseSourcedInput();CloseNominalSpeed();await host!.CloseAsync();router?.Dispose();CloseSuppliedTerrain();
            if(creature4Fixture is not null) {
                Check(world!.RetainedCreatureView4FullBytes==0,"Private world state/journal drained");
                GD.Print("creature4_full_cleanup="+JsonSerializer.Serialize(new{StateAndJournalBytes=0,Drained=true}));
            }
            if(creature4PartialFixture is not null){Check(world!.RetainedCreatureView4PartialBytes==0,"Partial state/journal drained");GD.Print("creature4_partial_cleanup="+JsonSerializer.Serialize(new{StateAndJournalBytes=0,Drained=true}));}
            CloseCreature4Partial();CloseCreature4Full();creature6FullRouter?.Dispose();creature6CompositeRouter?.Dispose();creature6Router?.Dispose();creature6Fixture?.Dispose();creature6Fixture=null;tangible6Router?.Dispose();tangible6Fixture?.Dispose();tangible6Fixture=null;tangibleRouter?.Dispose();tangibleFixture?.Dispose();tangibleFixture=null;
            if(tangibleVisual is not null)Check(tangibleVisual.DrainCompletion.IsCompleted && tangibleVisual.IsRetired && world!.RetainedTangibleReplicationDecodedBytes==0,"Typed tangible shutdown drains state and caption");
            if(terminalAnimation is not null) {
                Check(terminalAnimation.IsRetired&&terminalAnimation.DrainCompletion.IsCompleted&&terminalAnimation.Animation.IsRetired&&terminalAnimation.Animation.SelectedTextureId==0&&terminalTextureIds.Count==8&&terminalTextureIds.Values.All(id=>GodotObject.InstanceFromId(id)is null),"Animated caches and inner presentation drain");
                bool clockDrained=terminalAnimation.Playback is null||(terminalAnimation.Playback.IsClosed&&terminalAnimation.Playback.IsFaulted==terminalClockFaultEnabled&&terminalAnimation.Playback.ClockSnapshot.RegisteredCount==0&&terminalAnimation.Playback.Snapshots().Count==0);
                Check(clockDrained,"Clock owner drains registrations");
                GD.Print("terminal_animation_cleanup="+JsonSerializer.Serialize(new{Retired=true,CachesReleased=true,Copies=terminalAnimation.Animation.ImageCopies,Uploads=terminalAnimation.Animation.UniqueUploads,ClockDrained=clockDrained}));terminalMipHashes.Clear();terminalTextureIds.Clear();
            }
            if(customizationRevisions || customizationWire)Check(customizationVisual!.DrainCompletion.IsCompleted,"Host observes customization worker drain");
            customizationSource.Current=null;
            if(customizationClosePending) {
                Check(customizationEntered==1 && customizationClosed==1 && customizationVisual!.LastAppliedRevision==6,"Owned pending customization canceled and never published");
                GD.Print("customization_drain="+JsonSerializer.Serialize(new{customizationEntered,customizationClosed,Applied=customizationVisual!.LastAppliedRevision,Complete=customizationVisual.DrainCompletion.IsCompletedSuccessfully}));
            }
            Check(world!.OutstandingJobs==0 && closed==calls && jobs.All(j=>j.IsCompleted),"Close drains every archive job");
            Check(pump!.QueuedMessageCount==0 && pump.PendingPublicationCount==0 && pump.Consumer.Count==0,"Owned queues/actors cleared");
            customizationAdapter?.Dispose();customizationRouter?.Dispose();customizationScalarRouter?.Dispose();fullCreatureRouter?.Dispose();extendedCreatureRouter?.Dispose();namedCreatureRouter?.Dispose();nameTables?.Clear();nameTables=null;rawCustomizations?.Dispose();wirePeer?.Clear();customizationDictionary=null;customizationRanges=null;customizationDirectory=null;authoredContext=null;
            motions?.Clear();motions?.Dispose();visuals.Clear();motionPeer?.Clear();
            Array.Clear(cfg);peer.Clear();jobs.Clear();environment?.Dispose();environment=null;
            GD.Print(cancel?"PASS: pumped canceled archives=2 publications=0 drained=true":"PASS: pumped world continuous=true budgets=true original_recipes=2 static_only_reload=true drained=true");
            if(motionPeer is not null && !cancel && !localInput)GD.Print("PASS: pumped motion native_pose=true height_once=true resource_retained=true stale_rejected=true body_bridge=true");
            if(localInput && !cancel)GD.Print("PASS: pumped local_input=true walk_run=true bounded_motion_ingress=true correction=true suspension=true reset=true retained=true drained=true");
            if(faceInspection is not null && !cancel)GD.Print("PASS: pumped generated_face=true explicit_values=true exact_main=true accounted=true retained=true drained=true");
            if(customizationRevisions && !cancel)GD.Print("PASS: pumped customization revisions=true latest_only=true failure_retained=true pose_clock_retained=true no_asset_io=true drained=true");
            if(customizationFull)GD.Print("PASS: pumped full_creature=true containers_retained=true authored_context=true raw_order=true scalar_height_once=true no_asset_reload=true drained=true");
            if(customizationNamed)GD.Print("PASS: pumped named_creature=true current_names=true original_table_resolution=true");
            if(creature6Index==5)GD.Print(creature6Full?"PASS: pumped creature_view6_full=true current_state=true exact_ack=true visual_retained=true":creature6Composite?"PASS: pumped creature_view6_composite=true current_state=true exact_ack=true visual_retained=true":"PASS: pumped creature_view6=true current_state=true exact_ack=true visual_retained=true");
            if(tangible6Index==5)GD.Print("PASS: pumped tangible_view6=true current_state=true exact_ack=true visual_retained=true");
            if(creature4PartialIndex==5)GD.Print("PASS: pumped creature_view4_partial=true current_state=true exact_ack=true visual_retained=true no_default_seeds=true");
            if(creature4Index==5)GD.Print("PASS: pumped creature_view4_full=true current_state=true exact_ack=true visual_retained=true");
            if(terminalAnimationEnabled)GD.Print("PASS: pumped terminal_animation=true frames=8 bounded=true no_update_io=true drained=true");
            if(customizationExtended)GD.Print("PASS: pumped extended_creature=true common_state=true hidden_composition=true raw_visibility_retained=true no_asset_reload=true drained=true");
            if(customizationScalars)GD.Print("PASS: pumped mixed_customization=true scalar_height_once=true ordered=true raw_empty_retains_display=true pose_clock_retained=true no_asset_reload=true drained=true");
            if(customizationWire)GD.Print("PASS: pumped wire_customization=true outer_owned=true ordered=true range_rejection=true empty_retention=true extended_preserved=true pose_clock_retained=true no_asset_reload=true drained=true");
            if(nominalComplete)GD.Print("PASS: pumped nominal_speed=true supplied_rate=true retained_clamp=true original_resource_retained=true stopped=true drained=true");
            if(sourcedComplete)GD.Print("PASS: pumped sourced_input=true explicit_source=true original_resource_chain=true stopped=true no_update_io=true drained=true");
            GetTree().Quit();
        } catch(Exception error) { await Fail(error); }
    }
    private async Task ObserveHost(){try{await host!.Completion;}catch(Exception error){await Fail(error);}}
    private async Task Fail(Exception error)
    {
        if(!finishing)finishing=true;
        GD.PushError("Pumped world failed: "+error);
        foreach(var closeInput in new Action[]{()=>inputHost?.Close(),()=>CloseSourcedInput(),()=>CloseNominalSpeed()})
            try{closeInput();}catch(Exception cleanup){GD.PushError("Input cleanup: "+cleanup);}
        try{CloseSuppliedTerrain();}catch(Exception cleanup){GD.PushError("Terrain cleanup: "+cleanup);}
        try{if(host is not null)await host.CloseAsync();else{pump?.Close();world?.Close();if(world is not null)await world.DrainCompletion;}}
        catch(Exception cleanup){GD.PushError("Pumped cleanup: "+cleanup);}
        bool expectedInputLoss=inputLoss && phase==17 && inputHost?.LastState?.FaultType=="InvalidOperationException";
        bool drained=world is null || world.OutstandingJobs==0;
        drained=drained && closed==calls && jobs.All(j=>j.IsCompleted);
        GD.Print("pump_failure_cleanup="+JsonSerializer.Serialize(new{calls,closed,drained,expectedInputLoss,DriverFault=inputHost?.LastState?.FaultType,actors=pump?.Consumer.Count??0}));
        CloseCreature4Partial();CloseCreature4Full();creature6FullRouter?.Dispose();creature6CompositeRouter?.Dispose();creature6Router?.Dispose();creature6Fixture?.Dispose();creature6Fixture=null;tangible6Router?.Dispose();tangible6Fixture?.Dispose();tangible6Fixture=null;tangibleRouter?.Dispose();tangibleFixture?.Dispose();tangibleFixture=null;
        customizationAdapter?.Dispose();customizationRouter?.Dispose();customizationScalarRouter?.Dispose();fullCreatureRouter?.Dispose();extendedCreatureRouter?.Dispose();namedCreatureRouter?.Dispose();nameTables?.Clear();nameTables=null;rawCustomizations?.Dispose();wirePeer?.Clear();customizationDictionary=null;customizationRanges=null;customizationDirectory=null;authoredContext=null;router?.Dispose();motions?.Dispose();visuals.Clear();motionPeer?.Clear();Array.Clear(cfg);environment?.Dispose();environment=null;
        if(expectedInputLoss && drained){GD.Print("PASS: pumped input_loss=true driver_stopped=true cleanup_attempted=true drained=true");GetTree().Quit();}
        else GetTree().Quit(1);
    }
    private static void Check(bool value,string name){if(!value)throw new InvalidDataException(name);}
    public override void _Notification(int what){if(what==NotificationWMCloseRequest && !finishing)_=Fail(new OperationCanceledException("Window close"));}
    public override void _ExitTree(){
        foreach(var close in new Action[]{()=>inputHost?.Close(),()=>CloseSourcedInput(),()=>CloseNominalSpeed(),()=>pump?.Close(),()=>world?.Close(),()=>CloseCreature4Partial(),()=>CloseCreature4Full(),()=>creature6FullRouter?.Dispose(),()=>creature6CompositeRouter?.Dispose(),()=>creature6Router?.Dispose(),()=>creature6Fixture?.Dispose(),()=>tangible6Router?.Dispose(),()=>tangible6Fixture?.Dispose(),()=>tangibleRouter?.Dispose(),()=>tangibleFixture?.Dispose(),()=>customizationAdapter?.Dispose(),()=>customizationRouter?.Dispose(),()=>customizationScalarRouter?.Dispose(),()=>fullCreatureRouter?.Dispose(),()=>extendedCreatureRouter?.Dispose(),()=>namedCreatureRouter?.Dispose(),()=>nameTables?.Clear(),()=>rawCustomizations?.Dispose(),()=>router?.Dispose(),()=>motions?.Dispose()})
            try{close();}catch(Exception cleanup){GD.PushError("Exit cleanup: "+cleanup);}
        CloseSuppliedTerrain();visuals.Clear();motionPeer?.Clear();Array.Clear(cfg);environment?.Dispose();environment=null;
    }
}
