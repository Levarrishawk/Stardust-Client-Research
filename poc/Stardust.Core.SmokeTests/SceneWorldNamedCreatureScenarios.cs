using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldNamedCreatureScenarios
{
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action)where T:Exception{try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    private static ReplicationByteString Raw(params byte[] bytes)=>ReplicationByteString.Create(bytes);
    private static string Hex(ReplicationByteString value)=>Convert.ToHexString(value.CopyBytes());
    private sealed class ReenteringEnvelopes(IReadOnlyList<ReplicationEnvelope> values,Action callback):IReadOnlyList<ReplicationEnvelope>
    {
        public int Count=>values.Count;
        public ReplicationEnvelope this[int index]=>values[index];
        public IEnumerator<ReplicationEnvelope> GetEnumerator(){callback();return values.GetEnumerator();}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    internal static void Run(string[] inputs,IReadOnlyDictionary<string,string> peer,string exportPath)
    {
        checks=0;Check(inputs.Length==4,"root config catalog bank");Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(inputs[1])))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(inputs[2]));using var stream=File.OpenRead(inputs[3]);var bank=SkeletalAnimationBankProfile.Load(stream);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0))}};
        byte[] Bytes(string key)=>Convert.FromHexString(peer[key]);ulong id=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create")).ObjectId;int loaderCalls=0;
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,new(4096,1024,64,16,8192),16,8192,new(4096,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(4096,32,64,64,16),new(16,16));
        SceneWorldCoordinator World(SceneWorldReplicationPolicy? selected=null,SceneWorldChangePolicy? changes=null,int payloadBudget=4096)=>new(catalog,options,(request,token)=>{loaderCalls++;throw new Exception("State fixture must not load assets");},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,Math.Max(8192,payloadBudget*2),16,4096,payloadBudget,selected??policy,changes);
        byte[] Baseline(byte[] value,ulong? target=null)
        {
            var envelope=ReplicationEnvelopeCodec.DecodeBaseline(Bytes("baseline"),4096);var fields=envelope.CopyOpaqueFields();int cursor=4;
            void String(){int n=BinaryPrimitives.ReadUInt16LittleEndian(fields.AsSpan(cursor));cursor+=2;if(n==65535){n=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4;}cursor+=n;}
            String();cursor+=4;String();int units=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4+units*2+4;int start=cursor;String();
            var replacement=new byte[start+2+value.Length+fields.Length-cursor];fields.AsSpan(0,start).CopyTo(replacement);BinaryPrimitives.WriteUInt16LittleEndian(replacement.AsSpan(start),(ushort)value.Length);value.CopyTo(replacement,start+2);fields.AsSpan(cursor).CopyTo(replacement.AsSpan(start+2+value.Length));
            return ReplicationEnvelopeCodec.EncodeBaseline(new(ReplicationEnvelopeKind.Baseline,target??id,envelope.RawTag,3,envelope.DeclaredCount,replacement,4096),4096);
        }
        long Stage(SceneWorldCoordinator w,byte[]? seed=null,bool creatureOnly=false){long g=w.BeginSelection(id).Generation;foreach(string key in (creatureOnly?new[]{"start","create"}:new[]{"start","create","staticCreate"}))w.Accept(g,Bytes(key));w.Accept(g,Baseline(seed??new byte[]{1,2,3,4}));foreach(string key in (creatureOnly?new[]{"end"}:new[]{"staticBaseline","end","staticEnd"}))w.Accept(g,Bytes(key));w.CompleteMinimumSceneBarrier(g,true);return g;}
        SceneWorldObjectSnapshot Current(SceneWorldCoordinator w)=>w.Snapshots().Single(s=>s.Object.Handle.ObjectId==id);
        byte[] Wire(ulong target,ushort first=4,uint tag=CreatureView3BaselineCodec.RawTag,byte view=3,ushort count=65535)=>ReplicationEnvelopeCodec.EncodeDelta(new(ReplicationEnvelopeKind.Delta,target,tag,view,count,new byte[]{(byte)first,(byte)(first>>8),0xAA},4096),4096);
        CreatureView3ExtendedDeltaUpdate R(params byte[] b)=>CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromCustomization(Raw(b)));
        CreatureView3ExtendedDeltaUpdate S(CreatureView3ScalarUpdate v)=>CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromScalar(v));
        CreatureView3NamedDeltaPlan Plan(params CreatureView3ExtendedDeltaUpdate[] values)=>new(id,65535,values.Select(CreatureView3NamedDeltaUpdate.FromExtended).ToArray(),CreatureWoundsApplicationPolicy.LegacyBounded,16,16);
        CreatureView3ExtendedDeltaUpdate T(CreatureView3TangibleScalarUpdate value)=>CreatureView3ExtendedDeltaUpdate.FromTangible(value);
        CreatureView3NamedDeltaUpdate N(CreatureView3NameUpdate value)=>CreatureView3NamedDeltaUpdate.FromName(value);
        CreatureView3NamedDeltaUpdate E(CreatureView3ExtendedDeltaUpdate value)=>CreatureView3NamedDeltaUpdate.FromExtended(value);
        CreatureView3NamedDeltaPlan Named(params CreatureView3NamedDeltaUpdate[] values)=>new(id,65535,values,CreatureWoundsApplicationPolicy.LegacyBounded,16,16);
        var cases=new List<string>();var receipts=new List<object>();
        void Same(SceneWorldCoordinator w,SceneWorldCustomizationStateStore source,SceneWorldObjectSnapshot before,SceneWorldRawCustomizationBatch raw,SceneWorldChangeBatch journal,ulong revision)
        {
            var after=Current(w);Check(ReferenceEquals(before.Replication!.Names,after.Replication!.Names)&&ReferenceEquals(before.Replication.TangibleScalars,after.Replication.TangibleScalars)&&ReferenceEquals(before.Replication.Scalars,after.Replication.Scalars)&&ReferenceEquals(before.Replication.Wounds,after.Replication.Wounds)&&ReferenceEquals(before.Replication.Components,after.Replication.Components)&&before.Replication.Revision==after.Replication.Revision,"Replication unchanged on rejection");
            Check(ReferenceEquals(raw,source.Find(after))&&source.Revision==revision,"Raw receipt/revision unchanged on rejection");
            var pending=w.DrainChanges(w.Generation)!;Check(pending.MutationRevision==journal.MutationRevision&&pending.Entries.Select(e=>(e.ObjectId,e.Kind)).SequenceEqual(journal.Entries.Select(e=>(e.ObjectId,e.Kind))),"World pending batch unchanged on rejection");
            Check(w.State==ZoneSceneTransitionState.AwaitingServerReady,"Rejection does not fail world");
        }
        CreatureView3ExtendedDeltaUpdate W(uint counter,CreatureWoundsApplicationPolicy selected,params (byte Kind,ushort Index,int[] Values)[] commands)
        {
            using var memory=new MemoryStream();using var writer=new BinaryWriter(memory);writer.Write((ushort)17);
            writer.Write((uint)commands.Sum(c=>selected==CreatureWoundsApplicationPolicy.LegacyBounded&&c.Kind==3?c.Values.Length+1:1));writer.Write(counter);
            foreach(var c in commands){writer.Write(c.Kind);if(c.Kind!=4)writer.Write(c.Kind==3?(ushort)c.Values.Length:c.Index);foreach(int v in c.Values)writer.Write(v);}
            var envelope=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,id,CreatureView3BaselineCodec.RawTag,3,65535,memory.ToArray(),4096);
            return CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromWounds(CreatureView3WoundsDeltaCodec.Decode(envelope,new(4096,64,64,64,16),selected).Updates.Single()));
        }
        CreatureView3ExtendedDeltaUpdate C(uint counter,params (byte Kind,int Key)[] commands)
        {
            using var memory=new MemoryStream();using var writer=new BinaryWriter(memory);writer.Write((ushort)5);writer.Write((uint)commands.Length);writer.Write(counter);
            foreach(var c in commands){writer.Write(c.Kind);if(c.Kind!=2)writer.Write(c.Key);}
            var envelope=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,id,CreatureView3BaselineCodec.RawTag,3,65535,memory.ToArray(),4096);
            return CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromComponents(CreatureView3ComponentsDeltaCodec.Decode(envelope,new(4096,64,64,64,16)).Updates.Single()));
        }
        foreach(bool reverse in new[]{false,true})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var baseline=before.Replication!.Baseline;var raw=source.Find(before)!;long seedCharge=w.RetainedReplicationDecodedBytes;
            var file=Raw(1,255,0);var key=Raw(170,0);ushort[] input={65,0xD800,0};var custom=CreatureView3NameUpdate.FromCustomName(input);input[0]=0;
            var values=new[]{N(CreatureView3NameUpdate.FromStringId(file,uint.MaxValue,key)),E(R(8)),N(custom),E(T(CreatureView3TangibleScalarUpdate.FromVisibility(0))),E(S(new CreatureHeightUpdate(3))),E(R(9))};if(reverse)values=values.Reverse().ToArray();
            Check(w.ApplyNamedCreatureDelta(g,Named(values),source),"Ordered names/raw/common/scalar transaction");var after=Current(w);var names=after.Replication!.Names;
            Check(ReferenceEquals(baseline,after.Replication.Baseline)&&names.CustomNameUnits.SequenceEqual(new ushort[]{65,0xD800,0})&&Hex(names.NameFile)=="01FF00"&&Hex(names.NameKey)=="AA00"&&names.NameSpacer==uint.MaxValue,"Raw owned names/history/spacer/unpaired units");
            Check(names.AdditionalDecodedBytes(baseline)==11&&w.RetainedReplicationDecodedBytes==seedCharge+11&&after.Replication.Revision==before.Replication.Revision+1,"Additional current names exact charge/one revision");
            var batch=source.Find(after)!;Check(batch.Updates.Select(u=>Hex(u.Value)).SequenceEqual(reverse?new[]{"01020304","09","08"}:new[]{"01020304","08","09"})&&!source.Acknowledge(raw),"Named raw subsequence and receipt");
            Check(w.ApplyNamedCreatureDelta(g,Named(N(CreatureView3NameUpdate.FromStringId(baseline.NameFile,baseline.NameSpacer,baseline.NameKey))),source)&&ReferenceEquals(batch,source.Find(Current(w)))&&w.RetainedReplicationDecodedBytes==seedCharge+6,"Baseline string aliases charged once/names-only raw receipt");
            var equalFile=ReplicationByteString.Create(baseline.NameFile.CopyBytes());var equalKey=ReplicationByteString.Create(baseline.NameKey.CopyBytes());
            Check(w.ApplyNamedCreatureDelta(g,Named(N(CreatureView3NameUpdate.FromStringId(equalFile,baseline.NameSpacer,equalKey))),source)&&w.RetainedReplicationDecodedBytes==seedCharge+6+equalFile.Length+equalKey.Length,"Equal value owned new storage charges separately");
            var expected=Current(w).Replication!;long retained=w.RetainedReplicationDecodedBytes;var expectedNames=expected.Names;var expectedCommon=expected.TangibleScalars;
            Check(w.ApplyNamedCreatureDelta(g,Named(E(R(6))),source)&&ReferenceEquals(expectedNames,Current(w).Replication!.Names)&&expected.Revision==Current(w).Replication!.Revision,"Named raw-only preserves typed names identity/revision");
            Check(w.ApplyExtendedCreatureDelta(g,new(id,0,new[]{S(new CreatureHeightUpdate(4))},CreatureWoundsApplicationPolicy.LegacyBounded,16,16),source)&&ReferenceEquals(expectedNames,Current(w).Replication!.Names)&&ReferenceEquals(expectedCommon,Current(w).Replication!.TangibleScalars)&&w.RetainedReplicationDecodedBytes==retained,"Old24 name/common/charge carry");
            Check(w.ApplyFullCreatureDelta(g,new(id,0,new[]{CreatureView3FullDeltaUpdate.FromScalar(new CreatureRankUpdate(4))},CreatureWoundsApplicationPolicy.LegacyBounded,16,16),source)&&ReferenceEquals(expectedNames,Current(w).Replication!.Names)&&w.RetainedReplicationDecodedBytes==retained,"Old23 name charge carry");
            Check(w.ApplyCustomizationScalars(g,new(id,0,new[]{CreatureView3CustomizationScalarUpdate.FromScalar(new CreaturePostureUpdate(2))}),source)&&ReferenceEquals(expectedNames,Current(w).Replication!.Names)&&w.RetainedReplicationDecodedBytes==retained,"Old22 name charge carry");
            source.Apply(Current(w),new(id,0,new[]{Raw(7)}));Check(ReferenceEquals(expectedNames,Current(w).Replication!.Names)&&w.RetainedReplicationDecodedBytes==retained,"Old21 name charge carry");
            w.Accept(g,Bytes("composite"));Check(ReferenceEquals(expectedNames,Current(w).Replication!.Names)&&ReferenceEquals(expectedCommon,Current(w).Replication!.TangibleScalars),"Old world composite name/common carry");
            receipts.Add(new{Name=reverse?"names-reverse":"names-forward",File=Hex(names.NameFile),Key=Hex(names.NameKey),names.NameSpacer,Units=names.CustomNameUnits,Additional=names.AdditionalDecodedBytes(baseline),Raw=batch.Updates.Select(u=>Hex(u.Value)),Height=after.Replication.Scalars.Height,Visibility=after.Replication.TangibleScalars.Visibility,EqualBytesCharge=6+equalFile.Length+equalKey.Length,SeedFile=Hex(baseline.NameFile),SeedKey=Hex(baseline.NameKey)});
            w.Accept(g,Baseline(new byte[]{5}));var reset=Current(w).Replication!;Check(reset.Names.AdditionalDecodedBytes(reset.Baseline)==0&&ReferenceEquals(reset.Names.NameFile,reset.Baseline.NameFile),"Genuine baseline resets names aliases/charge");cases.Add(reverse?"names-reverse-alias-carry":"names-forward-alias-carry");
        }
        foreach(string fault in new[]{"string","units","byte-shrink","late-compound","compound-with-name-charge","total-attempts"})
        {
            var settings=policy;if(fault is "byte-shrink" or "compound-with-name-charge"){using var sizing=World();Stage(sizing);settings=settings with {MaximumDecodedBytes=sizing.RetainedReplicationDecodedBytes+(fault=="compound-with-name-charge"?1:0)};}
            if(fault=="total-attempts")settings=settings with {MaximumScalarUpdates=1};using var w=World(settings);long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var batch=source.Find(before)!;var journal=w.DrainChanges(g)!;ulong revision=source.Revision;long charge=w.RetainedReplicationDecodedBytes;
            var values=new List<CreatureView3NamedDeltaUpdate>{E(R(8))};
            if(fault=="string")values.Add(N(CreatureView3NameUpdate.FromStringId(Raw(new byte[1025]),0,Raw())));
            else if(fault=="units")values.Add(N(CreatureView3NameUpdate.FromCustomName(new ushort[65])));
            else if(fault=="byte-shrink"){values.Add(N(CreatureView3NameUpdate.FromCustomName(new ushort[]{1})));values.Add(N(CreatureView3NameUpdate.FromCustomName(Array.Empty<ushort>())));}
            else if(fault=="total-attempts"){values.Add(N(CreatureView3NameUpdate.FromCustomName(Array.Empty<ushort>())));values.Add(E(T(CreatureView3TangibleScalarUpdate.FromVisibility(0))));}
            else {values.Add(N(CreatureView3NameUpdate.FromStringId(Raw(1),0,Raw())));values.Add(E(fault=="late-compound"?W(before.Replication!.Wounds!.Counter+1,CreatureWoundsApplicationPolicy.LegacyBounded,(2,65535,new[]{7})):C(before.Replication!.Components!.Counter+1,(1,123))));}
            Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,Named(values.ToArray()),source));Same(w,source,before,batch,journal,revision);Check(w.RetainedReplicationDecodedBytes==charge,"Name/compound late failure preserves global charge");cases.Add("names-"+fault+"-atomic");
        }
        using(var sizing=World())
        {
            Stage(sizing);long seedCharge=sizing.RetainedReplicationDecodedBytes;using var w=World(policy with {MaximumDecodedBytes=2*seedCharge+1});long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);
            var create=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));ulong other=id+100;w.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(create with {ObjectId=other}));w.Accept(g,Baseline(new byte[]{1,2,3,4},other));w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(other)));
            Check(w.ApplyNamedCreatureDelta(g,Named(N(CreatureView3NameUpdate.FromStringId(Raw(1),0,Raw()))),source),"First actor exact current-name cap");var before=Current(w);var batch=source.Find(before)!;var journal=w.DrainChanges(g)!;ulong revision=source.Revision;var second=w.FindReplication(g,other)!;
            var update=new CreatureView3NamedDeltaPlan(other,0,new[]{N(CreatureView3NameUpdate.FromStringId(Raw(2),0,Raw()))},CreatureWoundsApplicationPolicy.LegacyBounded,16,16);Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,update,source));Same(w,source,before,batch,journal,revision);Check(w.FindReplication(g,other)!.Revision==second.Revision&&w.RetainedReplicationDecodedBytes==2*seedCharge+1,"Multi-actor names byte cap atomic");cases.Add("names-multiactor-global-cap");
        }
        foreach(bool reverse in new[]{false,true})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var seed=source.Find(before)!;var baseline=before.Replication!.Baseline;
            var values=new[]{T(CreatureView3TangibleScalarUpdate.FromComplexity(-0.0f)),R(8),T(CreatureView3TangibleScalarUpdate.FromUnsigned(3,uint.MaxValue)),S(new CreatureHeightUpdate(2)),T(CreatureView3TangibleScalarUpdate.FromUnsigned(6,0xF1020304)),W(before.Replication.Wounds!.Counter+3,CreatureWoundsApplicationPolicy.LegacyBounded,(3,0,new[]{8,9})),T(CreatureView3TangibleScalarUpdate.FromUnsigned(7,42)),C(before.Replication.Components!.Counter+1,(1,123)),T(CreatureView3TangibleScalarUpdate.FromUnsigned(8,uint.MaxValue)),T(CreatureView3TangibleScalarUpdate.FromUnsigned(9,1234)),T(CreatureView3TangibleScalarUpdate.FromVisibility(0)),R(9),S(new CreatureHeightUpdate(4)),T(CreatureView3TangibleScalarUpdate.FromComplexity(1.25f)),T(CreatureView3TangibleScalarUpdate.FromVisibility(255))};
            if(reverse)values=values.Reverse().ToArray();long charge=w.RetainedReplicationDecodedBytes;Check(w.ApplyNamedCreatureDelta(g,Plan(values),source),"Common/full ordered transaction");
            var after=Current(w);var common=after.Replication!.TangibleScalars;Check(common.Volume==uint.MaxValue&&common.Options==0xF1020304&&common.UseCount==42&&common.ConditionDamage==uint.MaxValue&&common.MaximumCondition==1234,"Five exact U32 fields");
            Check(BitConverter.SingleToUInt32Bits(common.Complexity)==(reverse?0x80000000u:0x3FA00000u)&&common.Visibility==(reverse?0:255)&&after.Replication.Scalars.Height==(reverse?2:4),"Finite complexity signed-zero/visibility/scalar order");
            Check(ReferenceEquals(baseline,after.Replication.Baseline)&&after.Replication.Revision==before.Replication.Revision+1&&after.Replication.Wounds!.Values.SequenceEqual(new[]{8,9})&&after.Replication.Components!.Keys.Contains(123),"Historic baseline and compound overlay");
            var raw=source.Find(after)!;Check(raw.Updates.Select(u=>Hex(u.Value)).SequenceEqual(reverse?new[]{"01020304","09","08"}:new[]{"01020304","08","09"})&&!source.Acknowledge(seed),"Extended raw ordered receipt");
            var commonOnly=Plan(T(CreatureView3TangibleScalarUpdate.FromVisibility(1)));long retained=w.RetainedReplicationDecodedBytes;Check(w.ApplyNamedCreatureDelta(g,commonOnly,source)&&ReferenceEquals(raw,source.Find(Current(w)))&&w.RetainedReplicationDecodedBytes==retained,"Common-only raw issued and fixed logical bytes");
            var current=Current(w).Replication!;Check(w.ApplyNamedCreatureDelta(g,Plan(R(7)),source)&&ReferenceEquals(current.TangibleScalars,Current(w).Replication!.TangibleScalars)&&current.Revision==Current(w).Replication!.Revision,"Raw-only common identity/revision unchanged");
            var expected=Current(w).Replication!.TangibleScalars;
            Check(w.ApplyFullCreatureDelta(g,new(id,0,new[]{CreatureView3FullDeltaUpdate.FromScalar(new CreatureHeightUpdate(6))},CreatureWoundsApplicationPolicy.LegacyBounded,16,16),source)&&ReferenceEquals(expected,Current(w).Replication!.TangibleScalars),"Old23 ingress preserves current common");
            Check(w.ApplyCustomizationScalars(g,new(id,0,new[]{CreatureView3CustomizationScalarUpdate.FromScalar(new CreatureRankUpdate(5))}),source)&&ReferenceEquals(expected,Current(w).Replication!.TangibleScalars),"Old22 ingress preserves common");
            source.Apply(Current(w),new CreatureView3CustomizationDeltaPlan(id,0,new[]{Raw(6)}));Check(ReferenceEquals(expected,Current(w).Replication!.TangibleScalars),"Old21 raw ingress preserves common");
            w.Accept(g,Bytes("composite"));Check(ReferenceEquals(expected,Current(w).Replication!.TangibleScalars),"Existing world composite path preserves common");
            receipts.Add(new{Name=reverse?"common-reverse":"common-forward",ComplexityBits=BitConverter.SingleToUInt32Bits(common.Complexity),common.Volume,common.Options,common.UseCount,common.ConditionDamage,common.MaximumCondition,common.Visibility,Height=after.Replication.Scalars.Height,Wounds=after.Replication.Wounds!.Values,Components=after.Replication.Components!.Keys,InitialComponents=before.Replication.Components!.Keys,Raw=raw.Updates.Select(u=>Hex(u.Value))});
            w.Accept(g,Baseline(new byte[]{5}));var reseed=Current(w).Replication!;Check(reseed.TangibleScalars.Volume==reseed.Baseline.Volume&&reseed.TangibleScalars.Visibility==reseed.Baseline.Visibility&&!ReferenceEquals(expected,reseed.TangibleScalars),"Genuine baseline reseeds common overlay");cases.Add(reverse?"common-reverse-preservation":"common-forward-preservation");
        }
        foreach(var mode in new[]{SceneWorldReplicationMode.CreatureView3Scalars,SceneWorldReplicationMode.CreatureView3Mixed})
        using(var w=World(policy with {Mode=mode}))
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);Check(w.ApplyNamedCreatureDelta(g,Plan(T(CreatureView3TangibleScalarUpdate.FromUnsigned(6,123))),source),"Common supports earlier typed modes");var expected=Current(w).Replication!.TangibleScalars;Check(w.ApplyNamedCreatureDelta(g,Named(N(CreatureView3NameUpdate.FromCustomName(new ushort[]{65}))),source),"Names in earlier typed mode");var expectedNames=Current(w).Replication!.Names;long retainedNames=w.RetainedReplicationDecodedBytes;
            using var memory=new MemoryStream();using var writer=new BinaryWriter(memory);writer.Write((ushort)14);writer.Write(9f);
            if(mode==SceneWorldReplicationMode.CreatureView3Mixed){writer.Write((ushort)17);writer.Write(1u);writer.Write(Current(w).Replication!.Wounds!.Counter+1);writer.Write((byte)2);writer.Write((ushort)0);writer.Write(7);}
            var envelope=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,id,CreatureView3BaselineCodec.RawTag,3,0,memory.ToArray(),4096);w.Accept(g,ReplicationEnvelopeCodec.EncodeDelta(envelope,4096));
            Check(ReferenceEquals(expected,Current(w).Replication!.TangibleScalars)&&ReferenceEquals(expectedNames,Current(w).Replication!.Names)&&w.RetainedReplicationDecodedBytes==retainedNames&&Current(w).Replication!.Scalars.Height==9,"Older scalar/mixed codec path carries common");cases.Add("common-mode-preserve-"+mode);
        }
        foreach(string limit in new[]{"scalar-total","field-total"})
        using(var w=World(limit=="scalar-total"?policy with {MaximumScalarUpdates=1}:policy with {MixedLimits=policy.MixedLimits! with {MaximumFields=1}}))
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var raw=source.Find(before)!;var journal=w.DrainChanges(g)!;ulong revision=source.Revision;
            Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,Plan(T(CreatureView3TangibleScalarUpdate.FromVisibility(0)),S(new CreatureRankUpdate(3))),source));Same(w,source,before,raw,journal,revision);cases.Add("common-"+limit+"-atomic");
        }
        Reject<InvalidDataException>(()=>CreatureView3TangibleScalarUpdate.FromComplexity(float.NaN));Reject<InvalidDataException>(()=>CreatureView3TangibleScalarUpdate.FromComplexity(float.PositiveInfinity));Reject<InvalidDataException>(()=>CreatureView3TangibleScalarUpdate.FromUnsigned(1,1));cases.Add("common-frozen-finite-index-guard");
        using(var sizing=World())
        {
            Stage(sizing);long charge=sizing.RetainedReplicationDecodedBytes;
            using var w=World(policy with {MaximumDecodedBytes=charge*2});long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);
            var create=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));ulong other=id+100;w.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(create with {ObjectId=other}));w.Accept(g,Baseline(new byte[]{1,2,3,4},other));w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(other)));
            var before=Current(w);var raw=source.Find(before)!;var journal=w.DrainChanges(g)!;ulong revision=source.Revision;var second=w.FindReplication(g,other)!;
            Check(w.RetainedReplicationDecodedBytes==charge*2,"Two-actor exact global decoded charge");Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,Plan(R(2),C(before.Replication!.Components!.Counter+1,(1,123))),source));
            Same(w,source,before,raw,journal,revision);Check(w.RetainedReplicationDecodedBytes==charge*2&&w.FindReplication(g,other)!.Revision==second.Revision,"Other actor and global charge unchanged");cases.Add("multi-actor-decoded-capacity-atomic");
        }
        foreach(uint remote in new[]{0u,uint.MaxValue,0x80000000u})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w).Replication!;
            var wu=W(remote,CreatureWoundsApplicationPolicy.LegacyBounded,(2,0,new[]{7}));var cu=C(remote,(1,123));
            var plan=Plan(R(2),wu,cu);Check(w.ApplyNamedCreatureDelta(g,plan,source),"Legacy bounded wrap union");var after=Current(w).Replication!;
            receipts.Add(new{Name=$"wrap-{remote}",Remote=remote,InitialWoundCounter=before.Wounds!.Counter,InitialWounds=before.Wounds.Values,InitialComponentCounter=before.Components!.Counter,InitialComponents=before.Components.Keys,WoundCounter=after.Wounds!.Counter,Wounds=after.Wounds.Values,ComponentCounter=after.Components!.Counter,Components=after.Components.Keys});cases.Add($"wrap-{remote}");
        }
        foreach(var selected in new[]{CreatureWoundsApplicationPolicy.StrictInterval,CreatureWoundsApplicationPolicy.LegacyBounded})
        foreach(bool reverse in new[]{false,true})
        using(var w=World(policy with {WoundsPolicy=selected}))
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);source.Find(before);var typed=before.Replication!;
            var assign=W(typed.Wounds!.Counter+(selected==CreatureWoundsApplicationPolicy.StrictInterval?1u:3u),selected,(3,0,new[]{8,9}));
            var component=C(typed.Components!.Counter+1,(1,123));
            var updates=reverse?new[]{S(new CreatureHeightUpdate(6)),component,R(3),assign,R(2)}:new[]{R(2),assign,S(new CreatureHeightUpdate(6)),R(3),component};
            var plan=new CreatureView3NamedDeltaPlan(id,65535,updates.Select(CreatureView3NamedDeltaUpdate.FromExtended).ToArray(),selected,16,16);Check(w.ApplyNamedCreatureDelta(g,plan,source),"Full interleaving supported");
            var after=Current(w);var raw=source.Find(after)!;Check(after.Replication!.Wounds!.Values.SequenceEqual(new[]{8,9})&&after.Replication.Components!.Keys.Contains(123)&&after.Replication.Scalars.Height==6,"Compound and scalar final state");
            Check(ReferenceEquals(typed.Baseline,after.Replication.Baseline)&&after.Replication.Revision==typed.Revision+1,"Full historic baseline/one revision");
            Check(raw.Updates.Select(u=>Hex(u.Value)).SequenceEqual(reverse?new[]{"01020304","03","02"}:new[]{"01020304","02","03"}),"Full raw subsequence");
            var receipt=source.Find(after)!;var nonraw=new CreatureView3NamedDeltaPlan(id,0,new[]{assign,component}.Select(CreatureView3NamedDeltaUpdate.FromExtended).ToArray(),selected,16,16);var woundState=after.Replication.Wounds;
            Check(w.ApplyNamedCreatureDelta(g,nonraw,source)&&ReferenceEquals(receipt,source.Find(Current(w))),"Nonraw replay keeps raw issued receipt");
            Check(Current(w).Replication!.Wounds!.Counter==woundState.Counter&&Current(w).Replication!.Wounds!.Values.SequenceEqual(woundState.Values),"Replay preserves wounds value/counter");
            receipts.Add(new{Name=$"full-{selected}-{reverse}",InitialWounds=typed.Wounds.Values,InitialWoundCounter=typed.Wounds.Counter,InitialComponents=typed.Components.Keys,InitialComponentCounter=typed.Components.Counter,Wounds=after.Replication.Wounds.Values,WoundCounter=after.Replication.Wounds.Counter,Components=after.Replication.Components!.Keys,ComponentCounter=after.Replication.Components!.Counter,Raw=raw.Updates.Select(u=>Hex(u.Value)),Height=after.Replication.Scalars.Height});
            cases.Add($"full-{selected}-{reverse}");
        }
        foreach(string fault in new[]{"wound-index","wound-counter","wound-growth","component-growth","wound-policy","wound-elements","component-elements","wound-disabled","component-disabled","commands","values","keys","bytes-field","bytes-command-wounds","bytes-command-components"})
        {
            var selected=fault=="wound-counter"||fault=="wound-index"?CreatureWoundsApplicationPolicy.StrictInterval:CreatureWoundsApplicationPolicy.LegacyBounded;
            var settings=policy with {WoundsPolicy=selected};
            if(fault=="wound-disabled")settings=settings with {Mode=SceneWorldReplicationMode.CreatureView3Scalars};
            if(fault=="component-disabled")settings=settings with {Mode=SceneWorldReplicationMode.CreatureView3Mixed};
            if(fault=="commands")settings=settings with {MixedLimits=policy.MixedLimits! with {MaximumWoundsCommands=0}};
            if(fault=="values")settings=settings with {MixedLimits=policy.MixedLimits! with {MaximumWoundsValues=0}};
            if(fault=="keys")settings=settings with {ComponentsLimits=policy.ComponentsLimits! with {MaximumKeys=0}};
            // Exact seeded charge independently supplied below, so one added element exceeds byte budget.
            if(fault.StartsWith("bytes")){using var sizing=World();Stage(sizing);settings=settings with {MaximumDecodedBytes=sizing.RetainedReplicationDecodedBytes};}
            using var w=World(settings);long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var raw=source.Find(before)!;var journal=w.DrainChanges(g)!;ulong revision=source.Revision;long bytes=w.RetainedReplicationDecodedBytes;
            uint wc=before.Replication!.Wounds?.Counter??0,cc=before.Replication.Components?.Counter??0;
            CreatureView3ExtendedDeltaUpdate failing;
            if(fault is "component-growth")failing=C(cc+20,Enumerable.Range(0,17).Select(i=>((byte)1,i+100)).ToArray());
            else if(fault=="bytes-command-components")failing=C(cc+2,(1,123),(2,0));
            else if(fault is "component-elements" or "component-disabled" or "keys" or "bytes-field")failing=C(cc+1,(1,123));
            else if(fault=="wound-counter")failing=W(wc+100,selected,(2,0,new[]{8}));
            else if(fault is "wound-index" or "wound-growth")failing=W(wc+1,selected,(2,65535,new[]{8}));
            else if(fault=="bytes-command-wounds")failing=W(wc+2,selected,(1,0,new[]{8}),(4,0,Array.Empty<int>()));
            else failing=W(wc+1,selected,(2,0,new[]{8}));
            var list=new List<CreatureView3ExtendedDeltaUpdate>{R(9),T(CreatureView3TangibleScalarUpdate.FromVisibility(0)),S(new CreatureHeightUpdate(100)),failing};
            // A later clear cannot erase an earlier invalid growth/charge failure.
            if(fault is "component-growth" or "bytes-field")list.Add(C(cc+30,(2,0)));
            if(fault is "wound-growth")list.Add(W(wc+2,selected,(4,0,Array.Empty<int>())));
            var plan=new CreatureView3NamedDeltaPlan(id,65535,list.Select(CreatureView3NamedDeltaUpdate.FromExtended).ToArray(),fault=="wound-policy"?CreatureWoundsApplicationPolicy.StrictInterval:selected,fault=="wound-elements"?15:16,fault=="component-elements"?15:16);
            Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,plan,source));Same(w,source,before,raw,journal,revision);Check(w.RetainedReplicationDecodedBytes==bytes,"Decoded charge unchanged on rejection");cases.Add(fault+"-full-atomic");
        }
        foreach(bool reverse in new[]{false,true})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var seed=source.Find(before)!;var baseline=before.Replication!.Baseline;
            var writes=new[]{R(8),S(new CreatureHeightUpdate(1.25f)),R(9),S(new CreatureHeightUpdate(2.5f)),S(new CreaturePostureUpdate(3)),S(new CreatureRankUpdate(4)),S(new CreatureLinkUpdate(ulong.MaxValue)),S(new CreatureShockUpdate(uint.MaxValue)),S(new CreatureStatesUpdate(0xFEDCBA9876543210))};
            if(reverse)writes= writes.Reverse().ToArray();Check(w.ApplyNamedCreatureDelta(g,Plan(writes),source),"Ordered mixed transaction");
            var after=Current(w);var batch=source.Find(after)!;Check(ReferenceEquals(baseline,after.Replication!.Baseline)&&ReferenceEquals(before.Replication.Wounds,after.Replication.Wounds)&&ReferenceEquals(before.Replication.Components,after.Replication.Components),"Historic baseline/wounds/components preserved");
            Check(after.Replication.Revision==before.Replication.Revision+1&&after.Replication.Scalars.Height==(reverse?1.25f:2.5f)&&after.Replication.Scalars.Posture==3&&after.Replication.Scalars.Rank==4&&after.Replication.Scalars.LinkId==ulong.MaxValue&&after.Replication.Scalars.Shock==uint.MaxValue,"Six scalar records ordered");
            Check(batch.Updates.Select(u=>Hex(u.Value)).SequenceEqual(reverse?new[]{"01020304","09","08"}:new[]{"01020304","08","09"})&&batch.ThroughRevision==3,"Raw subsequence ordered");
            Check(!source.Acknowledge(seed)&&source.Acknowledge(batch),"Mixed commit invalidates prior receipt; current acknowledges");
            var issued=source.Find(Current(w))!;ulong rawRevision=source.Revision;Check(w.ApplyNamedCreatureDelta(g,Plan(S(new CreatureHeightUpdate(7))),source)&&ReferenceEquals(issued,source.Find(Current(w)))&&source.Revision==rawRevision,"Scalar-only preserves issued receipt/raw journal");
            Check(source.Acknowledge(issued),"Scalar-only old receipt still current");var scalar=Current(w).Replication!;
            Check(w.ApplyNamedCreatureDelta(g,Plan(R(),R(0,255),R(0,255)),source),"Opaque repeated empty raw-only");var rawOnly=Current(w);
            Check(ReferenceEquals(scalar.Scalars,rawOnly.Replication!.Scalars)&&scalar.Revision==rawOnly.Replication.Revision,"Raw-only preserves scalar state/revision");
            Check(source.Find(rawOnly)!.Updates.Select(u=>Hex(u.Value)).SequenceEqual(new[]{"","00FF","00FF"}),"Empty/high-byte/repeated raw preserved");
            var journal=w.DrainChanges(g)!;Check(journal.Entries.Single(e=>e.ObjectId==id).Kind.HasFlag(SceneWorldChangeKind.Replication),"Raw-only emits world change");
            receipts.Add(new{Name=reverse?"reverse":"forward",Height=after.Replication.Scalars.Height,Raw=batch.Updates.Select(u=>Hex(u.Value)),RawRevisions=batch.Updates.Select(u=>u.Revision),States=after.Replication.Scalars.States.ToString("X16")});cases.Add(reverse?"reverse-order-six-scalars":"forward-order-six-scalars");
        }
        foreach(string fault in new[]{"raw-count","raw-string","raw-bytes","raw-overflow","scalar-limit","replication-overflow","world-overflow","source-binding","source-closed"})
        using(var w=World(fault=="scalar-limit"?policy with {MaximumScalarUpdates=0}:fault=="replication-overflow"?policy with {RevisionSeed=ulong.MaxValue-1}:policy,
            fault=="world-overflow"?new SceneWorldChangePolicy(4,ulong.MaxValue-3):null))
        {
            long g=Stage(w,creatureOnly:fault=="world-overflow");using var source=new SceneWorldCustomizationStateStore(w,maximumPendingUpdates:fault=="raw-count"?1:64,maximumStringBytes:fault=="raw-string"?4:1536,maximumRetainedBytes:fault=="raw-bytes"?4:1048576,revisionSeed:fault=="raw-overflow"?ulong.MaxValue-2:0);
            var before=Current(w);var batch=source.Find(before)!;var journal=w.DrainChanges(g)!;ulong revision=source.Revision;
            using var foreignWorld=World();Stage(foreignWorld);using var foreign=new SceneWorldCustomizationStateStore(foreignWorld);
            if(fault=="source-closed")foreign.Dispose();
            var selected=fault is "source-binding" or "source-closed"?foreign:source;
            var plan=Plan(R(1,2,3,4,5),T(CreatureView3TangibleScalarUpdate.FromUnsigned(3,99)),S(new CreatureHeightUpdate(4)),R(6));
            if(fault.EndsWith("overflow"))Reject<OverflowException>(()=>w.ApplyNamedCreatureDelta(g,plan,selected));else Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,plan,selected));
            Same(w,source,before,batch,journal,revision);cases.Add(fault+"-atomic");
        }
        using(var w=World(changes:new(1)))
        {
            long g=Stage(w,creatureOnly:true);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var raw=source.Find(before)!;w.AcknowledgeChanges(w.DrainChanges(g)!);w.Accept(g,Bytes("staticCreate"));var journal=w.DrainChanges(g)!;
            Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,Plan(R(2),S(new CreatureHeightUpdate(4))),source));Same(w,source,before,raw,journal,source.Revision);cases.Add("world-journal-capacity-atomic");
        }
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var raw=source.Find(before)!;var journal=w.DrainChanges(g)!;
            Check(!w.ApplyNamedCreatureDelta(g-1,null!,null!),"Stale before plan/source access");Same(w,source,before,raw,journal,source.Revision);
            Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,new(id+900,0,Plan(R(2)).Updates,CreatureWoundsApplicationPolicy.LegacyBounded,16,16),source));Same(w,source,before,raw,journal,source.Revision);
            Exception? error=null;var thread=new Thread(()=>{try{w.ApplyNamedCreatureDelta(g,Plan(R(2)),source);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Foreign owner rejected");Same(w,source,before,raw,journal,source.Revision);cases.Add("stale-target-thread-atomic");
            Check(w.ApplyNamedCreatureDelta(g,Plan(R(8),S(new CreatureHeightUpdate(4))),source),"Lifecycle seed write");w.Accept(g,Baseline(new byte[]{7}));var reseed=source.Find(Current(w))!;Check(reseed.Updates.Select(u=>Hex(u.Value)).SequenceEqual(new[]{"01020304","08","07"})&&!source.Acknowledge(raw),"Reseed historic ordered journal");
            w.Accept(g,Bytes("destroy"));Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,Plan(R(2)),source));w.Accept(g,Bytes("create"));w.Accept(g,Baseline(new byte[]{6}));w.Accept(g,Bytes("end"));
            Check(w.ApplyNamedCreatureDelta(g,Plan(S(new CreatureHeightUpdate(9)),R(5)),source),"New incarnation atomic seed/write");var recreated=source.Find(Current(w))!;Check(recreated.Incarnation!=reseed.Incarnation&&recreated.Updates.Select(u=>Hex(u.Value)).SequenceEqual(new[]{"06","05"})&&!source.Acknowledge(reseed),"New incarnation excludes stale raw state");
            w.BeginSelection(id);Check(!w.ApplyNamedCreatureDelta(g,Plan(R(2)),source)&&source.Count==0,"Generation stale no application");cases.Add("reseed-destroy-recreate-generation");
        }
        foreach(var mode in new[]{SceneWorldReplicationMode.CreatureView3Scalars,SceneWorldReplicationMode.CreatureView3Mixed})
        using(var w=World(policy with {Mode=mode}))
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var baseline=Current(w).Replication!;Check(w.ApplyNamedCreatureDelta(g,Plan(R(2),S(new CreatureHeightUpdate(5))),source),"Mode supports scalar/raw");Check(Current(w).Replication!.Scalars.Height==5,"Mode height");cases.Add("mode-"+mode);
        }
        using(var w=World())
        {
            long g=Stage(w);var before=Current(w);using var unseeded=new SceneWorldCustomizationStateStore(w,maximumPendingUpdates:1);var journal=w.DrainChanges(g)!;
            Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,Plan(S(new CreatureHeightUpdate(7)),R(8)),unseeded));
            Check(unseeded.Count==0&&unseeded.Revision==0&&Current(w).Replication!.Revision==before.Replication!.Revision&&w.IsCurrent(journal),"Unseeded failure has no prefix in either subsystem");
            using var closed=new SceneWorldCustomizationStateStore(w);closed.Dispose();Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,Plan(S(new CreatureHeightUpdate(7))),closed));
            Check(Current(w).Replication!.Revision==before.Replication!.Revision&&w.IsCurrent(journal),"Scalar-only validates closed same-world source before commit");cases.Add("unseeded-closed-source-atomic");
            using var source=new SceneWorldCustomizationStateStore(w,maximumObjects:1);var issued=source.Find(Current(w))!;ulong revision=source.Revision;
            var create=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));ulong other=id+100;
            w.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(create with {ObjectId=other}));w.Accept(g,Baseline(new byte[]{7},other));w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(other)));
            var otherBefore=w.FindReplication(g,other)!;var all=w.DrainChanges(g)!;
            Reject<InvalidDataException>(()=>w.ApplyNamedCreatureDelta(g,new(other,0,Plan(S(new CreatureHeightUpdate(7)),R(8)).Updates,CreatureWoundsApplicationPolicy.LegacyBounded,16,16),source));
            Check(w.FindReplication(g,other)!.Revision==otherBefore.Revision&&w.IsCurrent(all)&&ReferenceEquals(issued,source.Find(Current(w)))&&source.Revision==revision,"Global actor budget rejects both sides atomically");cases.Add("source-actor-capacity-atomic");
        }
        foreach(ushort first in new ushort[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);int decodes=0,downstream=0;
            using var router=new SceneWorldNamedCreatureApplicationRouter(w,source,e=>{decodes++;return Plan(S(new CreatureHeightUpdate(4)),R(8));},(generation,message)=>{downstream++;return w.Accept(generation,message.Span);});
            Check(!router.Accept(g-1,new byte[]{0})&&decodes==0,"Router stale before parse");Check(router.Accept(g,Wire(id,first))&&decodes==1&&Current(w).Replication!.Scalars.Height==4,"Router routes frozen union candidate");
            Check(router.Accept(g,Bytes("staticDelta"))&&downstream==1&&decodes==1,"Other family downstream");router.Dispose();Check(!source.IsClosed&&w.State!=ZoneSceneTransitionState.Closed,"Router only releases borrows");cases.Add("router-first-"+first);
        }
        foreach(string fault in new[]{"throw","null","target","count","world","ack","source","reentry","dispose","generation"})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var seed=source.Find(Current(w))!;var initial=Current(w).Replication!;ulong rawRevision=source.Revision;
            SceneWorldNamedCreatureApplicationRouter? router=null;
            router=new(w,source,e=>{
                if(fault=="throw")throw new InvalidDataException("Injected decoder fault");
                if(fault=="null")return null!;
                if(fault=="world")w.Accept(g,Bytes("composite"));
                if(fault=="ack")w.AcknowledgeChanges(w.DrainChanges(g)!);
                if(fault=="source")source.Clear();
                if(fault=="reentry")try{router!.Accept(g,Wire(id));}catch(InvalidOperationException){}
                if(fault=="dispose")try{router!.Dispose();}catch(InvalidOperationException){}
                if(fault=="generation")w.BeginSelection(id);
                return new(fault=="target"?id+1:id,fault=="count"?(ushort)0:(ushort)65535,Plan(R(9),S(new CreatureHeightUpdate(100))).Updates,CreatureWoundsApplicationPolicy.LegacyBounded,16,16);
            });
            Reject<Exception>(()=>router.Accept(g,Wire(id)));Reject<ObjectDisposedException>(()=>router.Accept(g,Wire(id)));
            Check(!source.IsClosed,"Decoder failure does not close borrowed source");
            if(fault is not ("world" or "generation"))Check(Current(w).Replication!.Revision==initial.Revision&&ReferenceEquals(Current(w).Replication!.Scalars,initial.Scalars),"Failed callback does not commit scalar plan");
            if(fault is not ("source" or "generation"))Check(source.Revision==rawRevision&&ReferenceEquals(seed,source.Find(Current(w))),"Failed callback does not commit raw plan");
            cases.Add("decoder-"+fault);
        }
        foreach(string fault in new[]{"throw","source","reentry","generation"})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);source.Find(Current(w));SceneWorldNamedCreatureApplicationRouter? router=null;
            router=new(w,source,e=>Plan(R(9)),(generation,message)=>{
                if(fault=="throw")throw new InvalidDataException("Downstream fault");
                if(fault=="source")source.Clear();
                if(fault=="reentry")try{router!.Accept(g,Wire(id));}catch(InvalidOperationException){}
                if(fault=="generation")w.BeginSelection(id);
                return false;
            });
            Reject<Exception>(()=>router.Accept(g,Bytes("staticDelta")));Reject<ObjectDisposedException>(()=>router.Accept(g,Wire(id)));Check(!source.IsClosed,"Downstream cleanup borrows");cases.Add("downstream-"+fault);
        }
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);int calls=0;ReadOnlyMemory<byte> memory=default;
            using(var router=new SceneWorldNamedCreatureApplicationRouter(w,source,e=>{calls++;return Plan(R(7));},(generation,message)=>{memory=message;return false;}))
            {
                var original=Bytes("staticDelta");var copy=original.ToArray();Check(!router.Accept(g,original)&&memory.Span.ToArray().All(b=>b==0)&&original.SequenceEqual(copy),"Borrowed downstream memory cleared caller unchanged");
                Exception? error=null;var thread=new Thread(()=>{try{router.Accept(g,Wire(id));}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&router.Accept(g,Wire(id))&&calls==1,"Foreign thread does not close router");
            }
            using var malformed=new SceneWorldNamedCreatureApplicationRouter(w,source,e=>{calls++;return Plan(R(7));});var bad=Wire(id);bad[19]=255;Reject<InvalidDataException>(()=>malformed.Accept(g,bad));Check(calls==1,"Malformed outer before decoder");cases.Add("router-memory-thread-outer");
        }
        foreach(var selected in new[]{CreatureWoundsApplicationPolicy.StrictInterval,CreatureWoundsApplicationPolicy.LegacyBounded})
        using(var w=World(policy with {WoundsPolicy=selected,MixedLimits=policy.MixedLimits! with {MaximumWoundsCommands=4096,MaximumWoundsValues=4096},ComponentsLimits=policy.ComponentsLimits! with {MaximumCommands=4096,MaximumKeys=4096}}))
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var seed=Current(w).Replication!;
            using var memory=new MemoryStream();using var writer=new BinaryWriter(memory);
            writer.Write((ushort)17);writer.Write(4096u);writer.Write(seed.Wounds!.Counter+4096);
            for(int i=0;i<4096;i++){writer.Write((byte)2);writer.Write((ushort)0);writer.Write(7);}
            var envelope=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,id,CreatureView3BaselineCodec.RawTag,3,0,memory.ToArray(),65536);
            var wound=CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromWounds(CreatureView3WoundsDeltaCodec.Decode(envelope,new(65536,1,4096,4096,16),selected).Updates.Single()));
            memory.SetLength(0);writer.Write((ushort)5);writer.Write(4096u);writer.Write(seed.Components!.Counter+4096);
            for(int i=0;i<4096;i++){writer.Write((byte)1);writer.Write(123);}
            envelope=new(ReplicationEnvelopeKind.Delta,id,CreatureView3BaselineCodec.RawTag,3,0,memory.ToArray(),65536);
            var component=CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromComponents(CreatureView3ComponentsDeltaCodec.Decode(envelope,new(65536,1,4096,4096,16)).Updates.Single()));
            var plan=new CreatureView3NamedDeltaPlan(id,0,new[]{R(9),wound,component}.Select(CreatureView3NamedDeltaUpdate.FromExtended).ToArray(),selected,16,16);
            Check(w.ApplyNamedCreatureDelta(g,plan,source),"4096 commands per compound single pass");var state=Current(w).Replication!;
            Check(state.Wounds!.Values[0]==7&&state.Components!.Keys.Contains(123),"Large command effects/no-op membership");
            Check(state.Wounds.Counter==seed.Wounds.Counter+(selected==CreatureWoundsApplicationPolicy.StrictInterval?4096u:1u)&&state.Components!.Counter==seed.Components.Counter+4096,"Large command counters unchanged");
            var raw=source.Find(Current(w))!;Check(w.ApplyNamedCreatureDelta(g,plan,source)&&Current(w).Replication!.Wounds!.Counter==state.Wounds.Counter,"Large stale/no-op replay");Check(!source.Acknowledge(raw),"Repeated raw still invalidates receipt");cases.Add("large-single-pass-"+selected);
        }
        using(var w=World(policy with {BaselineLimits=new(200000,1024,65537,16,300000),MaximumDecodedBytes=300000},payloadBudget:200000))
        {
            var original=ReplicationEnvelopeCodec.DecodeBaseline(Bytes("baseline"),4096);var fields=original.CopyOpaqueFields();int cursor=4;
            void SkipString(){int length=BinaryPrimitives.ReadUInt16LittleEndian(fields.AsSpan(cursor));cursor+=2;if(length==65535){length=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4;}cursor+=length;}
            SkipString();cursor+=4;SkipString();int start=cursor;int units=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4+2*units;
            var replacement=new byte[fields.Length-2*units+2*65537];fields.AsSpan(0,start).CopyTo(replacement);BinaryPrimitives.WriteUInt32LittleEndian(replacement.AsSpan(start),65537);fields.AsSpan(cursor).CopyTo(replacement.AsSpan(start+4+2*65537));
            var envelope=new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline,id,CreatureView3BaselineCodec.RawTag,3,original.DeclaredCount,replacement,200000);
            long g=w.BeginSelection(id).Generation;w.Accept(g,Bytes("start"));w.Accept(g,Bytes("create"));w.Accept(g,ReplicationEnvelopeCodec.EncodeBaseline(envelope,200000));w.Accept(g,Bytes("end"));w.CompleteMinimumSceneBarrier(g,true);
            using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w).Replication!;Check(before.Names.CustomNameUnits.Count==65537&&ReferenceEquals(before.Names.CustomNameUnits,before.Baseline.CustomNameUnits)&&before.Names.AdditionalDecodedBytes(before.Baseline)==0,"Larger legacy baseline aliases without new update cap");
            Check(w.ApplyNamedCreatureDelta(g,Named(E(S(new CreatureHeightUpdate(2)))),source)&&ReferenceEquals(before.Names,Current(w).Replication!.Names),"Older typed fields preserve large baseline names");Reject<InvalidDataException>(()=>CreatureView3NameUpdate.FromCustomName(new ushort[65537]));cases.Add("large-baseline-alias-update-cap-separated");
        }
        Check(loaderCalls==0,"No asset preparation or CPU jobs");
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new {Checks=checks,Cases=cases,LoaderCalls=loaderCalls,Receipts=receipts},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"R25_NAMED_STATE_PASS checks={checks} cases={cases.Count}");
    }
}
