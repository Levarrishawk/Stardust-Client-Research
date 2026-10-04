using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneCreatureMovementResourceScenarios
{
    internal static object Run(string root,string config,string catalogPath,string bankPath)
    {
        string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
        if(Hash(File.ReadAllBytes(config))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new InvalidDataException("Configuration pin.");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(catalogPath));var recipe=catalog.Require(2937962913);
        using var bankStream=File.OpenRead(bankPath);var bank=SkeletalAnimationBankProfile.Load(bankStream);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var create=new SceneCreateObjectByCrcMessage(0xFEDCBA9876543210,-0f,.25f,-.5f,2,123.5f,-45.25f,float.Epsilon,recipe.TemplateCrc,255);
        using var text=File.OpenText(config);using var overlay=ConfiguredTreOverlay.Open(root,text);
        var reads=new Dictionary<string,int>();var borrowed=new Dictionary<string,byte[]>();
        TreLookupResult Resolve(string path)
        { reads[path]=reads.GetValueOrDefault(path)+1;var result=overlay.Lookup(path);if(result.Payload is not null)borrowed[path]=result.Payload;return result; }
        int checks=0,negative=0;
        void Check(bool okay,string why) { if(!okay)throw new Exception(why);checks++; }
        void Reject<T>(Action action) where T:Exception {try { action();throw new Exception("Expected "+typeof(T).Name); }catch(T) { negative++; } }
        SceneCreatureResourceOptions Options(bool movement=true,CreatureMovementResourceLimits? limits=null,int sources=64,long bytes=16777216)
            =>new(idle,bank,true,sources,bytes,loadAuthoredMovementInputs:movement,movementLimits:limits);
        var baseline=SceneObjectResourceRouter.Load(create,catalog,Options(false),Resolve);
        Check(baseline.Creature!.Creature.AuthoredMovement is null,"Default branch must omit movement.");
        string Fingerprint(CreatureAppearanceDescriptorBundle b)=>Hash(JsonSerializer.SerializeToUtf8Bytes(new {
            b.Profile,b.Appearance,b.LodMeshes,b.Sources,b.SelectedAnimation,b.PreparedAnimation,b.AdditionalAnimations,
            Textures=b.MaterialBundle!.Textures.ToDictionary(p=>p.Key,p=>p.Value.Mips.Select(m=>Hash(m.Rgba.ToArray())).ToArray())}));
        string baselineFingerprint=Fingerprint(baseline.Creature.Creature);
        int oldReads=baseline.ExternalReads;long oldBytes=baseline.SourceBytes;reads.Clear();borrowed.Clear();
        var loaded=SceneObjectResourceRouter.Load(create,catalog,Options(),Resolve);var creature=loaded.Creature!.Creature;var movement=creature.AuthoredMovement!;
        Check(movement is not null,"Opt-in absent.");
        Check(Fingerprint(creature)==baselineFingerprint,"Frozen descriptors/textures/prepared closure changed.");
        Check(loaded.ExternalReads==oldReads&&loaded.SourceBytes==oldBytes&&reads.Count==oldReads&&reads.Values.All(n=>n==1),"Duplicate movement I/O/accounting.");
        Check(movement!.Profile.Sources.Count==5&&movement.Profile.SourceBytes==8290,"Necessary five-parent chain.");
        Check(movement.DecodedBytes==movement.Profile.DecodedBytes+movement.Inputs.SnapshotBytes,"Retained decoded accounting.");
        foreach(var pair in movement.Profile.Sources)Check(loaded.Sources[pair.Key]==pair.Value&&reads[pair.Key]==1,"Source/cache identity mismatch.");
        var winning=movement.Profile.Inputs["speed[0]"].Field!.SourcePath;
        byte[] changedParent=overlay.Lookup(winning).Payload!.ToArray();
        var parameter=SharedObjectTemplateDecoder.Decode(changedParent).Layers.First(l=>l.Family=="SCOT").Parameters["speed"];
        int start=changedParent.AsSpan().IndexOf(parameter.Span);
        Check(start>=0&&changedParent.AsSpan(start+1).IndexOf(parameter.Span)<0,"Unique synthetic mutation location.");
        changedParent[start+5]=0x21; // Synthetic non-space relative byte for first counted speed entry.
        var unsupported=SceneObjectResourceRouter.Load(create,catalog,Options(),p=>p==winning?overlay.Lookup(p) with{Payload=changedParent}:overlay.Lookup(p));
        Check(unsupported.Creature!.Creature.AuthoredMovement!.Inputs.Speed0.Kind==SharedCreatureMovementInputKind.Unsupported
            &&unsupported.Creature.Creature.AuthoredMovement.Inputs.Speed0.Bits is null,"Unsupported authored value silently defaulted.");
        var snapshot=new SceneObjectSnapshot(new(Guid.NewGuid(),1,create.ObjectId,1),create,SceneObjectPhase.PendingBaselines,0,0);
        var options=Options();var request=new SceneObjectResourceRequest(snapshot,catalog,options);var copied=(SceneCreatureResourceOptions)request.Options;
        Check(copied.LoadAuthoredMovementInputs&&copied.MovementLimits==options.MovementLimits&&!ReferenceEquals(copied.MovementLimits,options.MovementLimits),"Request dropped opt-in/policy.");
        var copiedLoaded=SceneObjectResourceRouter.Load(request.Snapshot.Create,request.Catalog,request.Options,overlay.Lookup);
        Check(copiedLoaded.Creature!.Creature.AuthoredMovement is not null,"Copied route dropped evidence.");
        int before=reads.Values.Sum();
        foreach(var policy in new[]{new CreatureMovementResourceLimits(MaximumDecodedBytes:0),new(MaximumDecodedBytes:71303169),
            new(ProfileLimits:new(MaximumDepth:0)),new(ProfileLimits:new(MaximumResources:17)),new(ProfileLimits:new(MaximumWork:0)),
            new(SnapshotLimits:new(MaximumEvidence:0)),new(SnapshotLimits:new(MaximumSnapshotBytes:0))})
            Reject<ArgumentOutOfRangeException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(limits:policy),Resolve));
        Reject<ArgumentException>(()=>Options(false,new()));Check(reads.Values.Sum()==before,"Invalid policy reached resolver.");
        var paths=movement.Profile.Visits.Select(v=>v.SourcePath).ToArray();string last=paths[^1];byte[] first=overlay.Lookup(paths[0]).Payload!;
        foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})
            Reject<InvalidDataException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(),p=>p==last?new(kind,null,0):overlay.Lookup(p)));
        Reject<InvalidDataException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(),p=>p==last?new(TreLookupKind.Present,first,0):overlay.Lookup(p)));
        var wrong=new ObjectTemplateCatalog(new[]{recipe with{ObjectSha256=new string('0',64)}});
        Reject<InvalidDataException>(()=>SceneObjectResourceRouter.Load(create,wrong,Options(),overlay.Lookup));
        Reject<AppearanceGraphBudgetException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(sources:oldReads-1),overlay.Lookup));
        Reject<AppearanceGraphBudgetException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(bytes:oldBytes-1),overlay.Lookup));
        var exact=SceneObjectResourceRouter.Load(create,catalog,Options(sources:oldReads,bytes:oldBytes),overlay.Lookup);
        Check(exact.SourceBytes==oldBytes&&exact.ExternalReads==oldReads,"Exact source caps.");
        var exactMovement=new CreatureMovementResourceLimits(MaximumDecodedBytes:movement.DecodedBytes);
        var retained=SceneObjectResourceRouter.Load(create,catalog,Options(limits:exactMovement),overlay.Lookup);
        Check(retained.Creature!.Creature.AuthoredMovement!.DecodedBytes==movement.DecodedBytes,"Exact retained caps.");
        Reject<AppearanceGraphBudgetException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(limits:exactMovement with{MaximumDecodedBytes=movement.DecodedBytes-1}),overlay.Lookup));
        Reject<AppearanceGraphBudgetException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(limits:new(ProfileLimits:new(MaximumDecodedBytes:1))),overlay.Lookup));
        Reject<AppearanceGraphBudgetException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(limits:new(SnapshotLimits:new(MaximumSnapshotBytes:1))),overlay.Lookup));
        using var canceled=new CancellationTokenSource();canceled.Cancel();int cancelReads=0;
        Reject<OperationCanceledException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(),p=>{cancelReads++;return overlay.Lookup(p);},canceled.Token));Check(cancelReads==0,"Pre-cancel I/O.");
        using var during=new CancellationTokenSource();int count=0;
        Reject<OperationCanceledException>(()=>SceneObjectResourceRouter.Load(create,catalog,Options(),p=>{var s=overlay.Lookup(p);if(++count==5)during.Cancel();return s;},during.Token));Check(count==5,"Late parent cancellation.");
        string profileBefore=JsonSerializer.Serialize(SharedCreatureMovementInputScenarios.Export(movement.Profile));
        string inputsBefore=JsonSerializer.Serialize(SharedCreatureAuthoredMovementInputScenarios.Export(movement.Inputs));
        foreach(var bytes in borrowed.Values)Array.Clear(bytes);
        foreach(var visit in movement.Profile.Visits)foreach(var value in visit.Parameters.Values)if(value is not null)Array.Clear(value.CopyEncoding());
        foreach(var slot in Enum.GetValues<AuthoredMovementSlot>())foreach(var evidence in movement.Inputs[slot].Evidence)if(evidence.CopyEncoding() is {} bytes)Array.Clear(bytes);
        Check(JsonSerializer.Serialize(SharedCreatureMovementInputScenarios.Export(movement.Profile))==profileBefore&&JsonSerializer.Serialize(SharedCreatureAuthoredMovementInputScenarios.Export(movement.Inputs))==inputsBefore,"Raw snapshot mutation.");
        Check(Fingerprint(creature)==baselineFingerprint,"Borrowed source mutation affected graphics.");
        return new {Mounts=overlay.MountCount,Checks=checks,Negatives=negative,ExternalReads=loaded.ExternalReads,SourceBytes=loaded.SourceBytes,Sources=loaded.Sources,
            MovementDecodedBytes=movement.DecodedBytes,MovementWork=movement.Work,OriginalProfile=SharedCreatureMovementInputScenarios.Export(movement.Profile),
            OriginalInputs=SharedCreatureAuthoredMovementInputScenarios.Export(movement.Inputs),DefaultUnchanged=true,ExtraMovementExternalReads=0,OptionCopyPreserved=true};
    }
}
