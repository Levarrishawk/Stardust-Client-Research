using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class SceneCreatureBodyEnvelopeScenarios
{
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    private static string Hash(ReadOnlySpan<byte> b)=>Convert.ToHexString(SHA256.HashData(b));
    private static SceneObjectResources Wrap<T>(T p)=>(SceneObjectResources)typeof(SceneObjectResources).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(T)},null)!.Invoke(new object?[]{p});
    private static object Export(SceneCreatureBodyEnvelopeReceipt r)=>new {r.Height,r.SpecialDimensions,LocalCenter=new[]{r.LocalCenter.X,r.LocalCenter.Y,r.LocalCenter.Z},r.Radius,r.StepHeight,r.CollisionHeight,r.Length,r.WarpTolerance,r.RootPath,r.AppearancePath,r.RootSource,r.AppearanceSource,r.Fields,r.ProfileSources};
    internal static void Run(string[] args)
    {
        Check(args.Length==4,"Explicit root/config/catalog/bank required");
        Check(Hash(File.ReadAllBytes(args[1]))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Config pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));using var bankFile=File.OpenRead(args[3]);var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new SceneCreatureResourceOptions(idle,bank,true);
        var create=new SceneCreateObjectByCrcMessage(101,0,.5f,0,.8660254f,10,20,30,2937962913,0);
        int reads=0,scopes=0;var borrowed=new List<byte[]>();SceneObjectResources resources;scopes++;
        try{using var cfg=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],cfg);
            resources=SceneObjectResourceRouter.Load(create,catalog,options,p=>{reads++;var r=overlay.Lookup(p);if(r.Payload is not null)borrowed.Add(r.Payload);return r;});foreach(var bytes in borrowed)Array.Clear(bytes);
        }finally{scopes--;}
        Check(reads==39&&scopes==0,"Closed original39-source closure");SceneObjectResourceValidation.Validate(resources,create,catalog,options);
        var c=resources.Creature!;var p=c.Creature.Profile;
        Check(p.Sources.Count==5&&p.Sources.Values.Sum(s=>s.Bytes)==8290,"Accepted five-source profile");
        string original=Hash(JsonSerializer.SerializeToUtf8Bytes(resources));int beforeReads=reads;
        var samples=new List<object>();
        foreach(float h in new[]{1f,1.4f,.25f,2f})foreach(bool special in new[]{false,true})
        {
            var body=SceneCreatureBodyEnvelope.Create(resources,h,special);var sphere=p.Collision.CreateFallbackSphere(h,special);
            Check(body.LocalCenter==sphere.LocalCenter&&body.Radius==sphere.Radius&&body.StepHeight==(special?1:p.Collision.StepHeight)*h,"Recovered local fallback directly reused");
            Check(body.LocalCenter.X==0&&body.LocalCenter.Z==0,"No world pose baked into receipt");samples.Add(Export(body));
        }
        var rotated=Wrap(c with {Create=create with {QuaternionX=.5f,QuaternionY=0,QuaternionW=.8660254f,PositionX=-100,PositionZ=700,PositionY=-300}});
        Check(JsonSerializer.Serialize(Export(SceneCreatureBodyEnvelope.Create(resources,1.4f)))==JsonSerializer.Serialize(Export(SceneCreatureBodyEnvelope.Create(rotated,1.4f))),"Rotation/translation neutral local geometry");
        var worker=Task.Factory.StartNew(()=>SceneCreatureBodyEnvelope.Create(resources,1.4f),CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default).GetAwaiter().GetResult();
        Check(JsonSerializer.Serialize(Export(worker))==JsonSerializer.Serialize(samples[2]),"Worker-independent pure receipt");
        var negatives=new List<string>();
        void Reject<T>(string name,Action action) where T:Exception {try{action();}catch(T){negatives.Add(name);return;}throw new Exception("Accepted invalid "+name);}
        Reject<ArgumentNullException>("null-resources",()=>SceneCreatureBodyEnvelope.Create(null!,1));
        foreach(float h in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,0f,-1f,.249f,2.001f})Reject<ArgumentOutOfRangeException>("height-"+BitConverter.SingleToUInt32Bits(h),()=>SceneCreatureBodyEnvelope.Create(resources,h));
        SceneObjectResources Profile(SharedCreatureTemplateProfile profile)=>Wrap(c with {Creature=c.Creature with {Profile=profile}});
        SceneObjectResources Collision(SharedCreatureCollisionProfile collision)=>Profile(p with {Collision=collision});
        Reject<InvalidDataException>("static",()=>SceneCreatureBodyEnvelope.Create(Wrap(new SceneStaticResources(create,c.Recipe with {Recipe=SceneObjectResourceRecipe.StaticAppearance},null!,null!,new Dictionary<string,AssetSourceIdentity>(),0,0,SceneStaticMaterialPolicy.AllowNeutral)),1));
        Reject<InvalidDataException>("missing-profile",()=>SceneCreatureBodyEnvelope.Create(Profile(null!),1));
        Reject<InvalidDataException>("missing-collision",()=>SceneCreatureBodyEnvelope.Create(Profile(p with {Collision=null!}),1));
        Reject<InvalidDataException>("root-path",()=>SceneCreatureBodyEnvelope.Create(Profile(p with {RootPath="object/wrong.iff"}),1));
        Reject<InvalidDataException>("appearance-path",()=>SceneCreatureBodyEnvelope.Create(Profile(p with {AppearancePath="appearance/wrong.sat"}),1));
        Reject<InvalidDataException>("recipe-crc",()=>SceneCreatureBodyEnvelope.Create(Wrap(c with {Recipe=c.Recipe with {TemplateCrc=1}}),1));
        Reject<InvalidDataException>("root-pin",()=>SceneCreatureBodyEnvelope.Create(Wrap(c with {Recipe=c.Recipe with {ObjectSha256=new string('0',64)}}),1));
        Reject<InvalidDataException>("appearance-pin",()=>SceneCreatureBodyEnvelope.Create(Wrap(c with {Recipe=c.Recipe with {AppearanceSha256=new string('0',64)}}),1));
        Reject<InvalidDataException>("missing-fields",()=>SceneCreatureBodyEnvelope.Create(Profile(p with {Fields=new Dictionary<string,ResolvedTemplateField>()}),1));
        var fields=p.Fields.ToDictionary(k=>k.Key,k=>k.Value);fields["stepHeight"]=fields["stepHeight"] with {SourcePath="object/missing.iff"};
        Reject<InvalidDataException>("field-source",()=>SceneCreatureBodyEnvelope.Create(Profile(p with {Fields=fields}),1));
        fields["stepHeight"]=p.Fields["stepHeight"] with {SourcePath=null!};
        Reject<InvalidDataException>("null-field-source",()=>SceneCreatureBodyEnvelope.Create(Profile(p with {Fields=fields}),1));
        var sources=p.Sources.ToDictionary(k=>k.Key,k=>k.Value);string key=sources.Keys.First();sources[key]=sources[key] with {Bytes=1};
        Reject<InvalidDataException>("profile-source",()=>SceneCreatureBodyEnvelope.Create(Profile(p with {Sources=sources}),1));
        foreach(var pair in new[]{("negative-step",p.Collision with {StepHeight=-1}), ("zero-radius",p.Collision with {Radius=0}), ("negative-radius",p.Collision with {Radius=-1}),
            ("zero-height",p.Collision with {CollisionHeight=0}), ("zero-length",p.Collision with {Length=0}), ("negative-warp",p.Collision with {WarpTolerance=-1}),
            ("nan-offset",p.Collision with {OffsetX=float.NaN}), ("infinite-radius",p.Collision with {Radius=float.PositiveInfinity})})Reject<InvalidDataException>(pair.Item1,()=>SceneCreatureBodyEnvelope.Create(Collision(pair.Item2),1));
        foreach(var pair in new[]{("center-overflow",p.Collision with {StepHeight=float.MaxValue,Radius=float.MaxValue}), ("height-overflow",p.Collision with {CollisionHeight=float.MaxValue}),
            ("length-overflow",p.Collision with {Length=float.MaxValue}), ("warp-overflow",p.Collision with {WarpTolerance=float.MaxValue}), ("offset-overflow",p.Collision with {OffsetZ=float.MaxValue}), ("radius-underflow",p.Collision with {Radius=float.Epsilon})})
            Reject<InvalidDataException>(pair.Item1,()=>SceneCreatureBodyEnvelope.Create(Collision(pair.Item2),pair.Item1=="radius-underflow"?.25f:2));
        // Signed local offsets remain in native coordinates, including Z; special mode only overrides step/radius.
        var offset=Collision(p.Collision with {OffsetX=2,OffsetZ=-3});var ob=SceneCreatureBodyEnvelope.Create(offset,1.4f);Check(ob.LocalCenter.X==2*1.4f&&ob.LocalCenter.Z==-3*1.4f,"No ad hoc Z reflection");
        var specialZero=SceneCreatureBodyEnvelope.Create(Collision(p.Collision with {Radius=0}),1,true);Check(specialZero.Radius==.5f,"Special positive volume uses recovered substitute");
        // Receipt dictionaries are detached from publicly representable mutable input dictionaries.
        fields=p.Fields.ToDictionary(k=>k.Key,k=>k.Value);sources=p.Sources.ToDictionary(k=>k.Key,k=>k.Value);
        var owned=SceneCreatureBodyEnvelope.Create(Profile(p with {Fields=fields,Sources=sources}),1);string ownedBefore=JsonSerializer.Serialize(Export(owned));fields.Clear();sources.Clear();
        Check(JsonSerializer.Serialize(Export(owned))==ownedBefore&&owned.Fields.Count==8&&owned.ProfileSources.Count==5,"Detached provenance ownership");
        Reject<NotSupportedException>("receipt-fields-readonly",()=>((IDictionary<string,ResolvedTemplateField>)owned.Fields).Clear());
        Reject<NotSupportedException>("receipt-sources-readonly",()=>((IDictionary<string,AssetSourceIdentity>)owned.ProfileSources).Clear());
        Check(typeof(SceneCreatureBodyEnvelopeReceipt).GetProperties().All(v=>v.SetMethod is null),"Get-only receipt");
        Check(original==Hash(JsonSerializer.SerializeToUtf8Bytes(resources))&&reads==beforeReads&&scopes==0,"Original profile/resources unchanged; no further reads");
        Console.WriteLine("body_receipt="+JsonSerializer.Serialize(new {Samples=samples,Offset=Export(ob),Negatives=negatives,Reads=reads,ProfileSources=p.Sources.Count,ProfileSourceBytes=p.Sources.Values.Sum(s=>s.Bytes),Unchanged=true,NoReads=true,ArchiveClosed=true,Detached=true,RotationNeutral=true}));
        Console.WriteLine("PASS: original local body envelopes, Height/special bounds, identity/field/overflow negatives, local axes and detached ownership.");
    }
}
