using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneResourceOptionPreservationScenarios
{
    private static void Need(bool b,string why) { if(!b)throw new Exception(why); }
    private static string Hash(ReadOnlySpan<byte> b)=>Convert.ToHexString(SHA256.HashData(b));
    private static bool Null(JsonElement e)=>e.ValueKind==JsonValueKind.Null;
    private static int Int(JsonElement e,string k)=>e.GetProperty(k).GetInt32();
    private static long Long(JsonElement e,string k)=>e.GetProperty(k).GetInt64();
    private static string Str(JsonElement e,string k)=>e.GetProperty(k).GetString()!;
    private static object Clip(LogicalAnimationClipSelection c)=>new {Path=c.TablePath,Name=c.LogicalName,Pin=c.ClipSha256,Route=c.ExplicitChildRoute};
    private static object Project(SceneObjectResourceOptions option)
    {
        if(option is SceneStaticResourceOptions s)
        {
            var p=s.SwitchFrames;var l=p?.Limits;var d=l?.DdsLimits;
            return new {Kind=s.Recipe.ToString(),Material=(int)s.MaterialPolicy,Selection=s.SwitchTextureFrame is {} f?new {Path=f.ShaderPath,Pin=f.ShaderSha256,Index=f.FrameIndex}:null,
                Graph=new {Resources=s.Limits.MaximumResources,SourceBytes=s.Limits.MaximumSourceBytes,Depth=s.Limits.MaximumDepth,Edges=s.Limits.MaximumEdges},Decoded=s.MaximumDecodedBytes,
                Policy=p is null?null:new {Limits=l is null?null:new {Frames=l.MaximumFrames,Sources=l.MaximumSources,SourceBytes=l.MaximumSourceBytes,Decoded=l.MaximumDecodedBytes,Mips=l.MaximumMips,ShaderBytes=l.MaximumShaderBytes,Dds=d is null?null:new {Dimension=d.MaximumDimension,Input=d.MaximumInputBytes,Decoded=d.MaximumDecodedBytes,Tail=d.AllowLegacyTerminalBlockTail}},RetainedMips=p.MaximumRetainedMips,PendingWeight=p.MaximumPendingWeight}};
        }
        var c=(SceneCreatureResourceOptions)option;
        return new {Kind=c.Recipe.ToString(),Idle=Clip(c.Idle),Movement=new {walk=Clip(c.Movement["walk"]),run=Clip(c.Movement["run"])},Albedo=c.LoadAlbedoFactors,Resources=c.MaximumResources,SourceBytes=c.MaximumSourceBytes,
            Face=c.FaceInspection is {} face?new {Decoded=face.MaximumDecodedBytes,Values=face.Values.OrderBy(p=>p.Key.VariableName,StringComparer.Ordinal).Select(p=>new {Name=p.Key.VariableName,FactoryFlag=p.Key.FactoryFlag,Value=p.Value})}:null};
    }
    private static void Expected(SceneObjectResourceOptions options,JsonElement expected)
        =>Need(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(Project(options)),JsonNode.Parse(expected.GetRawText())),"Literal independent option projection differs");
    private static SceneObjectResourceOptions Build(JsonElement input,out Action clearBorrowed)
    {
        clearBorrowed=()=>{};
        if(Str(input,"Kind")=="StaticAppearance")
        {
            var f=input.GetProperty("Selection");var g=input.GetProperty("Graph");var p=input.GetProperty("Policy");var decoded=input.GetProperty("Decoded");
            SwitchTextureFrameSelection? frame=Null(f)?null:new(Str(f,"Path"),Str(f,"Pin"),Int(f,"Index"));
            AppearanceGraphLimits? graph=Null(g)?null:new(Int(g,"Resources"),Long(g,"SourceBytes"),Int(g,"Depth"),Int(g,"Edges"));
            SceneStaticSwitchFramePolicy? policy=null;
            if(!Null(p))
            {
                var l=p.GetProperty("Limits");SwitchTextureFrameBundleLimits? limits=null;
                if(!Null(l)) { var d=l.GetProperty("Dds");limits=new(Int(l,"Frames"),Int(l,"Sources"),Long(l,"SourceBytes"),Long(l,"Decoded"),Int(l,"Mips"),Int(l,"ShaderBytes"),Null(d)?null:new(Int(d,"Dimension"),Int(d,"Input"),Int(d,"Decoded"),d.GetProperty("Tail").GetBoolean())); }
                policy=new(limits,Int(p,"RetainedMips"),Long(p,"PendingWeight"));
            }
            return new SceneStaticResourceOptions((SceneStaticMaterialPolicy)Int(input,"Material"),frame,graph,Null(decoded)?134217728:decoded.GetInt64(),policy);
        }
        var routes=new List<int[]>();
        LogicalAnimationClipSelection Selection(JsonElement e)
        { var r=e.GetProperty("Route");int[]? route=Null(r)?null:r.EnumerateArray().Select(x=>x.GetInt32()).ToArray();if(route is not null)routes.Add(route);return new(Str(e,"Path"),Str(e,"Name"),Str(e,"Pin"),route); }
        var movement=input.GetProperty("Movement");var bank=new Dictionary<string,LogicalAnimationClipSelection>{{"walk",Selection(movement.GetProperty("walk"))},{"run",Selection(movement.GetProperty("run"))}};
        var values=new Dictionary<BlueprintCustomizationKey,int>();var face=input.GetProperty("Face");SceneCreatureFaceInspectionPolicy? facePolicy=null;
        if(!Null(face)) { foreach(var row in face.GetProperty("Values").EnumerateArray())values.Add(new(Str(row,"Name"),row.GetProperty("FactoryFlag").GetBoolean()),Int(row,"Value"));facePolicy=new(values,Long(face,"Decoded")); }
        var option=new SceneCreatureResourceOptions(Selection(input.GetProperty("Idle")),bank,input.GetProperty("Albedo").GetBoolean(),Int(input,"Resources"),Long(input,"SourceBytes"),facePolicy);
        clearBorrowed=()=> { foreach(var r in routes)Array.Fill(r,4095);bank.Clear();values.Clear(); };return option;
    }
    private static void Ownership(SceneObjectResourceOptions source,SceneObjectResourceOptions copy)
    {
        Need(!ReferenceEquals(source,copy),"Owned option object");
        if(source is SceneStaticResourceOptions a && copy is SceneStaticResourceOptions b)
        {
            Need(!ReferenceEquals(a.Limits,b.Limits),"Owned graph limits");
            if(a.SwitchTextureFrame is not null)Need(!ReferenceEquals(a.SwitchTextureFrame,b.SwitchTextureFrame),"Owned selected-frame record");
            if(a.SwitchFrames is {} p)
            { Need(b.SwitchFrames is not null&&!ReferenceEquals(p,b.SwitchFrames),"Owned all-frame policy");if(p.Limits is {} l) { Need(b.SwitchFrames!.Limits is not null&&!ReferenceEquals(l,b.SwitchFrames.Limits),"Owned nested bundle limits");if(l.DdsLimits is {} d)Need(b.SwitchFrames.Limits!.DdsLimits is not null&&!ReferenceEquals(d,b.SwitchFrames.Limits.DdsLimits),"Owned nested DDS limits"); } }
        }
        else if(source is SceneCreatureResourceOptions c && copy is SceneCreatureResourceOptions d)
        {
            Need(!ReferenceEquals(c.Idle,d.Idle)&&!ReferenceEquals(c.Movement,d.Movement),"Owned clip and motion map");
            foreach(var key in new[]{"walk","run"})Need(!ReferenceEquals(c.Movement[key],d.Movement[key]),"Owned motion record");
            foreach(var pair in new[]{(c.Idle,d.Idle),(c.Movement["walk"],d.Movement["walk"]),(c.Movement["run"],d.Movement["run"])})
                if(pair.Item1.ExplicitChildRoute is not null)Need(!ReferenceEquals(pair.Item1.ExplicitChildRoute,pair.Item2.ExplicitChildRoute),"Owned route arrays");
            Need(ReferenceEquals(c.FaceInspection,d.FaceInspection),"Sealed immutable face policy may be shared");
            bool readOnly=false;try { ((IDictionary<string,LogicalAnimationClipSelection>)d.Movement).Clear(); }catch(NotSupportedException){readOnly=true;}Need(readOnly,"Read-only copied motion map");
            if(d.FaceInspection is {} face) { readOnly=false;try { ((IDictionary<BlueprintCustomizationKey,int>)face.Values).Clear(); }catch(NotSupportedException){readOnly=true;}Need(readOnly,"Read-only immutable face map"); }
        }
    }
    private static SceneObjectResources Wrap(SceneStaticResources s)=>(SceneObjectResources)typeof(SceneObjectResources).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(SceneStaticResources)},null)!.Invoke(new object[]{s});
    internal static void Run(string[] args)
    {
        Need(args.Length==4,"Explicit root/config/catalog/fixtures");Need(Hash(File.ReadAllBytes(args[1]))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Pinned config");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));using var fixture=JsonDocument.Parse(File.ReadAllBytes(args[3]));
        var views=new List<object>();var completions=new List<object>();SceneStaticResourceOptions? exact=null;SceneCreatureResourceOptions? creature=null;
        var create=new SceneCreateObjectByCrcMessage(101,-0f,.25f,-.5f,2,123.5f,-45.25f,float.Epsilon,2907007746u,255);
        foreach(var row in fixture.RootElement.EnumerateArray())
        {
            string name=Str(row,"Name");var option=Build(row.GetProperty("Input"),out var clear);var expected=row.GetProperty("Expected");
            var reg=new SceneObjectRegistry(1,1);var snapshot=reg.Create(1,create with {TemplateCrc=option.Recipe==SceneObjectResourceRecipe.StaticAppearance?2907007746u:2937962913u})!;
            var request=new SceneObjectResourceRequest(snapshot,catalog,option);Ownership(option,request.Options);Need(!ReferenceEquals(snapshot,request.Snapshot)&&!ReferenceEquals(snapshot.Create,request.Snapshot.Create),"Owned snapshot/create");
            clear();Expected(option,expected);Expected(request.Options,expected);
            object? callbackView=null;CancellationToken callbackToken=default;var pending=new TaskCompletionSource<SceneObjectResources>();
            var init=new SceneObjectResourceInitializer(reg,(q,t)=> { Need(ReferenceEquals(q,request),"Initializer retains owned request");Expected(q.Options,expected);callbackView=Project(q.Options);callbackToken=t;return pending.Task; });
            var ticket=init.Start(request);Need(callbackView is not null&&!callbackToken.IsCancellationRequested,"Actual loader callback received copied options");
            Need(init.Cancel(ticket)&&init.OutstandingJobs==1,"Cancel retains unfinished job charge");pending.SetCanceled();var canceled=init.Poll().Single();Need(canceled.Kind==SceneObjectResourceCompletionKind.Canceled&&canceled.Resources is null&&init.OutstandingJobs==0,"Canceled synthetic option job drain");init.Dispose();init.DrainCompletion.GetAwaiter().GetResult();
            views.Add(new {Name=name,Request=Project(request.Options),Callback=callbackView,Owned=true,Canceled=true,Drained=true});
            if(name=="static-all-original-exact")exact=(SceneStaticResourceOptions)option;if(option is SceneCreatureResourceOptions c)creature=c;
        }
        Need(exact is not null&&creature is not null,"Exact fixture and creature choices");
        var registry=new SceneObjectRegistry(1,1);var snap=registry.Create(1,create)!;var allRequest=new SceneObjectResourceRequest(snap,catalog,exact!);
        bool omissionDetected=false;try { Expected(new SceneStaticResourceOptions(exact!.MaterialPolicy,exact.SwitchTextureFrame,exact.Limits,exact.MaximumDecodedBytes),fixture.RootElement.EnumerateArray().Single(r=>Str(r,"Name")=="static-all-original-exact").GetProperty("Expected")); }catch(Exception){omissionDetected=true;}Need(omissionDetected,"Known omitted SwitchFrames copy must fail projection");
        void Reject<T>(Action action) where T:Exception { try {action();}catch(T){return;}throw new Exception("Expected rejection"); }
        Reject<ArgumentNullException>(()=>new SceneObjectResourceRequest(snap,catalog,null!));
        Reject<InvalidDataException>(()=>new SceneObjectResourceRequest(snap,catalog,creature!));
        var humanRegistry=new SceneObjectRegistry(1,1);var human=humanRegistry.Create(1,create with {TemplateCrc=2937962913u})!;
        Reject<InvalidDataException>(()=>new SceneObjectResourceRequest(human,catalog,exact!));
        Reject<ArgumentException>(()=>new ObjectTemplateCatalog(catalog.Entries.Values.Select(e=>e with {Recipe=(SceneObjectResourceRecipe)255})));
        Need(typeof(SceneObjectResourceOptions).IsAbstract&&typeof(SceneObjectResourceOptions).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single().IsFamilyAndAssembly&&typeof(SceneCreatureResourceOptions).IsSealed&&typeof(SceneStaticResourceOptions).IsSealed,"Unsupported option subclass unavailable through public API");
        int owner=Environment.CurrentManagedThreadId,worker=0;var raw=new Dictionary<string,TreLookupResult>();var calls=new List<string>();Task<SceneObjectResources>? load=null;
        var originalInit=new SceneObjectResourceInitializer(registry,(q,t)=>load=Task.Factory.StartNew(()=> {
            worker=Environment.CurrentManagedThreadId;Expected(q.Options,fixture.RootElement.EnumerateArray().Single(r=>Str(r,"Name")=="static-all-original-exact").GetProperty("Expected"));
            var borrowed=new List<byte[]>();SceneObjectResources result;
            using(var cfg=File.OpenText(args[1]))using(var overlay=ConfiguredTreOverlay.Open(args[0],cfg))
            { result=SceneObjectResourceRouter.Load(q.Snapshot.Create,q.Catalog,q.Options,p=> { calls.Add(p);var r=overlay.Lookup(p);raw.Add(p,r with {Payload=r.Payload?.ToArray()});if(r.Payload is {} b)borrowed.Add(b);return r; },t); }
            foreach(var b in borrowed)Array.Clear(b);return result;
        },CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default));
        originalInit.Start(allRequest);var original=load!.GetAwaiter().GetResult();var transferred=originalInit.Poll().Single();Need(transferred.Kind==SceneObjectResourceCompletionKind.Prepared&&ReferenceEquals(transferred.Resources,original)&&originalInit.OutstandingJobs==0,"Actual original matching transfer");
        void Record(string name,SceneObjectResourceCompletion c,int outstanding) { completions.Add(new {Name=name,Kind=c.Kind.ToString(),Resources=c.Resources is not null,Outstanding=outstanding}); }
        Record("matching",transferred,originalInit.OutstandingJobs);Need(originalInit.Poll().Count==0,"Transfer once");originalInit.Dispose();originalInit.DrainCompletion.GetAwaiter().GetResult();
        var defaultOptions=new SceneStaticResourceOptions(exact!.MaterialPolicy,exact.SwitchTextureFrame,exact.Limits,exact.MaximumDecodedBytes);
        var defaults=SceneObjectResourceRouter.Load(create,catalog,defaultOptions,p=>raw[p]);
        var alteredOptions=new SceneStaticResourceOptions(exact.MaterialPolicy,exact.SwitchTextureFrame,exact.Limits,exact.MaximumDecodedBytes,exact.SwitchFrames! with {MaximumPendingWeight=exact.SwitchFrames!.MaximumPendingWeight+1});
        var altered=SceneObjectResourceRouter.Load(create,catalog,alteredOptions,p=>raw[p]);
        var missing=Wrap(original.Static! with {SwitchFrames=null});
        foreach(var pair in new[]{("missing-bundle",missing),("default-completion",defaults),("altered-policy",altered)})
        {
            Reject<InvalidDataException>(()=>SceneObjectResourceValidation.Validate(pair.Item2,create,catalog,exact));
            using var init=new SceneObjectResourceInitializer(registry,(q,t)=>Task.FromResult(pair.Item2));init.Start(allRequest);var c=init.Poll().Single();Need(c.Kind==SceneObjectResourceCompletionKind.Faulted&&c.Resources is null&&c.Error is InvalidDataException,"Validator fault through initializer");Record(pair.Item1,c,init.OutstandingJobs);
        }
        foreach(string mode in new[]{"canceled","stale","registry-closed"})
        {
            var reg=new SceneObjectRegistry(1,1);var current=reg.Create(1,create)!;var task=new TaskCompletionSource<SceneObjectResources>();CancellationToken token=default;
            using var init=new SceneObjectResourceInitializer(reg,(q,t)=> { token=t;Need(((SceneStaticResourceOptions)q.Options).SwitchFrames is not null,"Lifecycle retains all-frame policy");return task.Task; });
            var ticket=init.Start(new(current,catalog,exact));if(mode=="canceled")Need(init.Cancel(ticket),"Explicit cancel");else if(mode=="stale")reg.Contain(1,new(create.ObjectId,0,42));else reg.Close();
            task.SetResult(original);var c=init.Poll().Single();Need(c.Resources is null&&token.IsCancellationRequested&&c.Kind==(mode=="canceled"?SceneObjectResourceCompletionKind.Canceled:SceneObjectResourceCompletionKind.Stale),"Retired result suppressed");Record(mode,c,init.OutstandingJobs);
        }
        bool lateFault=false;
        foreach(bool fault in new[]{false,true})
        {
            var reg=new SceneObjectRegistry(1,1);var current=reg.Create(1,create)!;var task=new TaskCompletionSource<SceneObjectResources>();CancellationToken token=default;
            var init=new SceneObjectResourceInitializer(reg,(q,t)=> { token=t;return task.Task; });init.Start(new(current,catalog,exact));init.Dispose();
            Need(token.IsCancellationRequested&&init.OutstandingJobs==1&&!init.DrainCompletion.IsCompleted,"Closed initializer keeps unfinished charge");
            if(fault)task.SetException(new InvalidDataException("Synthetic late fault"));else task.SetResult(original);
            init.DrainCompletion.GetAwaiter().GetResult();Need(init.OutstandingJobs==0,"Closed continuation drains");if(fault)lateFault=task.Task.Exception is not null;
            Reject<ObjectDisposedException>(()=>init.Poll());init.Dispose();
        }
        foreach(var r in raw.Values)if(r.Payload is {} b)Array.Clear(b);SceneObjectResourceValidation.Validate(original,create,catalog,exact);
        var bundle=original.Static!.SwitchFrames!;Need(original.ExternalReads==34&&calls.Count==34&&calls.Distinct().Count()==34&&defaults.ExternalReads==27,"Bounded original/read-once proof");
        Console.WriteLine("option_preservation="+JsonSerializer.Serialize(new {Options=views,Completions=completions,AllFrameOmissionDetected=omissionDetected,NullOptionRejected=true,UnsupportedRecipeRejected=true,MismatchedRecipesRejected=2,ClosedDrains=2,LateFaultObserved=lateFault,
            Original=new {OwnerThread=owner,WorkerThread=worker,ArchiveClosed=true,Sources=original.Sources.Values,Calls=calls,Receipt=SceneObjectResourceAccounting.Measure(original),Frames=bundle.Frames.Select(f=>new {f.Index,f.TextureTag,f.TexturePath}),Images=bundle.Images.Select(p=>new {Path=p.Key,Mips=p.Value.Mips.Select(m=>new {m.Width,m.Height,Bytes=m.DecodedBytes,Sha256=Hash(m.CopyRgba())})})}}));
        Console.WriteLine("PASS: focused request option ownership, actual initializer delivery, original all-frame completion and deterministic retirement.");
    }
}
