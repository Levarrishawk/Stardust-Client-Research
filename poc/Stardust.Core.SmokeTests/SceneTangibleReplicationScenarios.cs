using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
internal static class SceneTangibleReplicationScenarios
{
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action)where T:Exception{try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    private static ReplicationByteString Raw(params byte[] value)=>ReplicationByteString.Create(value);
    private static string Hex(ReplicationByteString value)=>Convert.ToHexString(value.CopyBytes());
    internal static void Run(string[] inputs,IReadOnlyDictionary<string,string> peer,string exportPath)
    {
        checks=0;Check(inputs.Length==4,"Fixture inputs");Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(inputs[1])))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Read-only config pin");
        byte[] Bytes(string key)=>Convert.FromHexString(peer[key]);var create=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));ulong id=create.ObjectId;
        TangibleView3BaselineSnapshot Baseline(ulong target,params int[] keys)
        {
            var envelope=ReplicationEnvelopeCodec.DecodeBaseline(Bytes("baseline"),4096);var fields=envelope.CopyOpaqueFields();int cursor=4;
            void Skip(){int n=BinaryPrimitives.ReadUInt16LittleEndian(fields.AsSpan(cursor));cursor+=2;if(n==65535){n=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4;}cursor+=n;}
            Skip();cursor+=4;Skip();int units=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4+2*units+4;Skip();int start=cursor;int count=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=8+4*count;
            using var memory=new MemoryStream();using var writer=new BinaryWriter(memory);writer.Write(fields.AsSpan(0,start));writer.Write((uint)keys.Length);writer.Write(0u);foreach(int key in keys)writer.Write(key);writer.Write(fields.AsSpan(cursor));
            var authored=new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline,target,CreatureView3BaselineCodec.RawTag,3,envelope.DeclaredCount,memory.ToArray(),4096);
            return TangibleView3BaselineSnapshot.FromCreatureCommonFields(CreatureView3BaselineCodec.Decode(ReplicationEnvelopeCodec.EncodeBaseline(authored,4096),new(4096,1024,64,16,8192)));
        }
        var baseline=Baseline(id,-1,int.MinValue,-1);
        SceneObjectRegistry Registry()=>new(8,1);
        SceneObjectIncarnation Create(SceneObjectRegistry r,ulong target){return r.Create(r.SceneGeneration,create with {ObjectId=target})!.Incarnation;}
        CreatureView3NamedDeltaUpdate N(CreatureView3NameUpdate v)=>CreatureView3NamedDeltaUpdate.FromName(v);
        CreatureView3NamedDeltaUpdate T(CreatureView3TangibleScalarUpdate v)=>CreatureView3NamedDeltaUpdate.FromExtended(CreatureView3ExtendedDeltaUpdate.FromTangible(v));
        CreatureView3NamedDeltaUpdate R(ReplicationByteString v)=>CreatureView3NamedDeltaUpdate.FromExtended(CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromCustomization(v)));
        CreatureView3NamedDeltaUpdate C(uint counter,params (byte Kind,int Key)[] commands)
        {
            using var memory=new MemoryStream();using var writer=new BinaryWriter(memory);writer.Write((ushort)5);writer.Write((uint)commands.Length);writer.Write(counter);foreach(var c in commands){writer.Write(c.Kind);if(c.Kind!=2)writer.Write(c.Key);}
            var envelope=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,id,CreatureView3BaselineCodec.RawTag,3,0,memory.ToArray(),65536);
            return CreatureView3NamedDeltaUpdate.FromExtended(CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromComponents(CreatureView3ComponentsDeltaCodec.Decode(envelope,new(65536,1,4096,4096,4)).Updates.Single())));
        }
        TangibleView3DeltaPlan Plan(params CreatureView3NamedDeltaUpdate[] values)=>new(id,65535,values,4);
        var cases=new List<string>();var receipts=new List<object>();
        foreach(bool reverse in new[]{false,true})
        {
            var registry=Registry();var token=Create(registry,id);using var store=new SceneTangibleReplicationStore(registry,new(MaximumComponentsElements:4));var seed=store.Seed(token,baseline)!;long seedBytes=store.RetainedDecodedBytes;
            Check(seed.Components.Keys.SequenceEqual(new[]{int.MinValue,-1})&&baseline.Components.Values.SequenceEqual(new[]{-1,int.MinValue,-1}),"Raw historical duplicate/current normalization");
            Check(ReferenceEquals(seed.Customization,baseline.Customization)&&ReferenceEquals(seed.Names.NameFile,baseline.NameFile)&&seed.Names.AdditionalDecodedBytes(baseline)==0,"Seed immutable aliases");
            registry.EndBaselines(1,new(id));Check(store.IsCurrent(seed),"Finalization keeps incarnation current");
            var names=N(CreatureView3NameUpdate.FromStringId(Raw(255,0),uint.MaxValue,Raw(1)));var custom=N(CreatureView3NameUpdate.FromCustomName(new ushort[]{65,0xD800}));
            var values=new[]{R(Raw(8,9)),names,T(CreatureView3TangibleScalarUpdate.FromVisibility(0)),C(2,(1,123),(1,123)),custom,T(CreatureView3TangibleScalarUpdate.FromUnsigned(6,uint.MaxValue)),R(Raw(7)),T(CreatureView3TangibleScalarUpdate.FromVisibility(255))};if(reverse)values=values.Reverse().ToArray();
            var current=store.Apply(seed,Plan(values))!;Check(current.Revision==2&&!store.IsCurrent(seed)&&store.IsCurrent(current),"One global revision/exact current snapshot");
            Check(ReferenceEquals(current.Baseline,baseline)&&current.Names.CustomNameUnits.SequenceEqual(new ushort[]{65,0xD800})&&current.Names.NameSpacer==uint.MaxValue,"Historic seed/unpaired names");
            Check(current.Components.Keys.SequenceEqual(new[]{int.MinValue,-1,123})&&current.Components.Counter==2,"Component duplicate/noop/counter policy");
            Check(current.TangibleScalars.Options==uint.MaxValue&&current.TangibleScalars.Visibility==(reverse?0:255)&&Hex(current.Customization)==(reverse?"0809":"07"),"Ordered scalars/raw current");
            long additional=7+(reverse?2:1);Check(store.RetainedDecodedBytes==seedBytes+4+additional,"Exact current names/raw/component logical charge");
            Check(store.Apply(seed,Plan(R(Raw(1)))) is null&&store.Revision==2,"Superseded snapshot cannot apply");
            var aliases=store.Apply(current,Plan(N(CreatureView3NameUpdate.FromStringId(baseline.NameFile,0,baseline.NameKey)),R(baseline.Customization)))!;Check(store.RetainedDecodedBytes==seedBytes+4+4&&aliases.Names.AdditionalDecodedBytes(baseline)==4,"Historic byte aliases not double charged");
            var equal=store.Apply(aliases,Plan(R(ReplicationByteString.Create(baseline.Customization.CopyBytes()))))!;Check(store.RetainedDecodedBytes==seedBytes+4+4+baseline.Customization.Length,"Equal new raw storage charged");
            var replacement=store.Seed(token,baseline)!;Check(replacement.Revision==5&&ReferenceEquals(replacement.Customization,baseline.Customization)&&store.RetainedDecodedBytes==seedBytes,"Genuine reseed resets overlays/charge");
            receipts.Add(new{Name=reverse?"reverse":"forward",BaselineBytes=baseline.DecodedBytes,SeedBytes=seedBytes,Keys=current.Components.Keys,Counter=current.Components.Counter,Raw=Hex(current.Customization),File=Hex(current.Names.NameFile),Key=Hex(current.Names.NameKey),Units=current.Names.CustomNameUnits,Visibility=current.TangibleScalars.Visibility,Options=current.TangibleScalars.Options,CurrentBytes=seedBytes+4+additional,AliasBytes=seedBytes+8,EqualBytes=seedBytes+8+baseline.Customization.Length});cases.Add(reverse?"reverse-order-aliases":"forward-order-aliases");
        }
        foreach(string fault in new[]{"names","raw","component-growth","component-then-clear","raw-then-empty","name-then-empty","target","elements","updates","commands","units","overflow"})
        {
            var registry=Registry();var token=Create(registry,id);var limits=new SceneTangibleReplicationLimits(MaximumComponentsElements:4);
            long charge=baseline.DecodedBytes+24;
            if(fault is "component-then-clear" or "raw-then-empty" or "name-then-empty")limits=limits with {MaximumDecodedBytes=charge};
            if(fault is "names" or "raw")limits=limits with {MaximumStringBytes=4};
            if(fault=="units")limits=limits with {MaximumUnicodeUnits=3};
            if(fault=="updates")limits=limits with {MaximumUpdates=1};
            if(fault=="commands")limits=limits with {MaximumComponentCommands=0};
            using var store=new SceneTangibleReplicationStore(registry,limits,fault=="overflow"?ulong.MaxValue-1:0);var seed=store.Seed(token,baseline)!;long before=store.RetainedDecodedBytes;ulong revision=store.Revision;
            var values=new List<CreatureView3NamedDeltaUpdate>{T(CreatureView3TangibleScalarUpdate.FromVisibility(0))};
            if(fault=="names")values.Add(N(CreatureView3NameUpdate.FromStringId(Raw(1,2,3,4,5),0,Raw())));
            else if(fault=="raw")values.Add(R(Raw(1,2,3,4,5)));
            else if(fault=="units")values.Add(N(CreatureView3NameUpdate.FromCustomName(new ushort[]{1,2,3,4})));
            else if(fault=="component-growth")values.Add(C(5,(1,123),(1,124),(1,125),(2,0)));
            else if(fault is "component-then-clear" or "commands")values.Add(C(2,(1,123),(2,0)));
            else if(fault=="raw-then-empty"){values.Add(R(Raw(1)));values.Add(R(Raw()));}
            else if(fault=="name-then-empty"){values.Add(N(CreatureView3NameUpdate.FromCustomName(new ushort[]{1})));values.Add(N(CreatureView3NameUpdate.FromCustomName(Array.Empty<ushort>())));}
            else values.Add(R(Raw(1)));
            var plan=new TangibleView3DeltaPlan(fault=="target"?id+1:id,0,values,fault=="elements"?3:4);
            if(fault=="overflow")Reject<OverflowException>(()=>store.Apply(seed,plan));else Reject<InvalidDataException>(()=>store.Apply(seed,plan));
            Check(ReferenceEquals(seed,store.Find(token))&&store.Revision==revision&&store.RetainedDecodedBytes==before,"Rejected full plan no prefix/revision/charge");cases.Add("atomic-"+fault);
        }
        foreach(string fault in new[]{"raw-keys","normalized","bytes","revision"})
        {
            var registry=Registry();var token=Create(registry,id);var limits=new SceneTangibleReplicationLimits(MaximumComponentsElements:4);
            if(fault=="raw-keys")limits=limits with {MaximumRawComponentKeys=2};if(fault=="normalized")limits=limits with {MaximumComponentsElements=1};if(fault=="bytes")limits=limits with {MaximumDecodedBytes=47};
            using var store=new SceneTangibleReplicationStore(registry,limits,fault=="revision"?ulong.MaxValue:0);
            if(fault=="revision")Reject<OverflowException>(()=>store.Seed(token,baseline));else Reject<InvalidDataException>(()=>store.Seed(token,baseline));
            Check(store.Snapshots().Count==0&&store.RetainedDecodedBytes==0,"Rejected seed publishes nothing");cases.Add("seed-"+fault);
        }
        {
            var registry=Registry();var first=Create(registry,id);var second=Create(registry,id+1);long charge=baseline.DecodedBytes+24;
            using var store=new SceneTangibleReplicationStore(registry,new(MaximumDecodedBytes:2*charge,MaximumComponentsElements:4));var a=store.Seed(first,baseline)!;var b=store.Seed(second,Baseline(id+1,-1,int.MinValue,-1))!;
            Reject<InvalidDataException>(()=>store.Apply(a,Plan(R(Raw(1)))));Check(store.IsCurrent(a)&&store.IsCurrent(b)&&store.RetainedDecodedBytes==2*charge,"Global other actor charge preserved");
            registry.Destroy(1,new(id+1,0));Reject<InvalidDataException>(()=>store.Apply(a,Plan(R(Raw(1)))));Check(store.Revision==2,"Rejected Apply does not prune stale actor to make room");
            Check(store.RetainedDecodedBytes==charge&&store.Apply(a,Plan(R(Raw(1)))) is not null,"Read retirement explicit then recovery");cases.Add("global-stale-nonpruning-recovery");
        }
        {
            var registry=Registry();var token=Create(registry,id);using var store=new SceneTangibleReplicationStore(registry,new(Capacity:1,MaximumComponentsElements:4));var seed=store.Seed(token,baseline)!;var second=Create(registry,id+1);
            Reject<InvalidDataException>(()=>store.Seed(second,Baseline(id+1,1)));Check(store.IsCurrent(seed)&&store.Revision==1,"Actor cap seed rejection unchanged");
            registry.Destroy(1,new(id,0));var replacement=Create(registry,id);Check(store.Apply(token,Plan(R(Raw(1)))) is null&&store.Apply(seed,Plan(R(Raw(1)))) is null&&store.Seed(token,baseline) is null,"Stale tokens never reach recreated actor");
            var next=store.Seed(replacement,baseline)!;Check(next.Incarnation!=seed.Incarnation&&next.Revision==2&&!store.IsCurrent(seed),"Recreate genuine seed binding");registry.Reset();Check(store.Snapshots().Count==0&&store.Revision==2,"Generation retirement never rewinds");cases.Add("capacity-recreate-generation");
        }
        foreach(uint remote in new[]{0u,uint.MaxValue,0x80000000u})
        {
            var registry=Registry();var token=Create(registry,id);using var store=new SceneTangibleReplicationStore(registry,new(MaximumComponentsElements:4));var seed=store.Seed(token,baseline)!;var result=store.Apply(seed,Plan(C(remote,(1,123))))!;
            var replay=store.Apply(result,Plan(C(remote,(1,123))))!;Check(replay.Components.Keys.SequenceEqual(result.Components.Keys)&&replay.Components.Counter==result.Components.Counter,"Unsigned skip/wrap/replay kernel preserved");
            receipts.Add(new{Name="wrap-"+remote,Remote=remote,Initial=seed.Components.Keys,InitialCounter=seed.Components.Counter,Keys=result.Components.Keys,Counter=result.Components.Counter});cases.Add("wrap-"+remote);
        }
        {
            var registry=Registry();var token=Create(registry,id);using var store=new SceneTangibleReplicationStore(registry,new(MaximumComponentsElements:4));var seed=store.Seed(token,baseline)!;var commands=Enumerable.Repeat(((byte)1,123),4096).ToArray();var result=store.Apply(seed,Plan(C(4096,commands)))!;
            Check(result.Components.Keys.Count==3&&result.Components.Counter==4096&&store.Apply(result,Plan(C(4096,commands)))!.Components.Counter==4096,"4096 commands single-pass duplicate/stale");cases.Add("large-single-pass");
            var foreignRegistry=Registry();var foreignToken=Create(foreignRegistry,id);using var foreign=new SceneTangibleReplicationStore(foreignRegistry,new(MaximumComponentsElements:4));var foreignSeed=foreign.Seed(foreignToken,baseline)!;
            Check(!store.IsCurrent(foreignSeed)&&store.Apply(foreignSeed,Plan(R(Raw(1)))) is null&&store.Find(foreignToken) is null,"Foreign owner/token rejected");
            Exception? error=null;var thread=new Thread(()=>{try{store.Close();}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&store.Snapshots().Count==1,"Foreign owner rejects before mutation");
            store.Close();store.Close();Check(store.RetainedDecodedBytes==0&&store.Snapshots().Count==0&&!store.IsCurrent(result)&&!registry.IsClosed,"Close releases owned state/borrows registry");Reject<ObjectDisposedException>(()=>store.Seed(token,baseline));Reject<ObjectDisposedException>(()=>store.Apply(token,Plan(R(Raw(1)))));cases.Add("foreign-thread-close");
        }
        {
            var registry=Registry();var token=Create(registry,id);using var store=new SceneTangibleReplicationStore(registry,new(MaximumComponentsElements:4));var seed=store.Seed(token,baseline)!;long charge=store.RetainedDecodedBytes;
            var plan=Plan(T(CreatureView3TangibleScalarUpdate.FromComplexity(1.25f)),T(CreatureView3TangibleScalarUpdate.FromUnsigned(3,uint.MaxValue)),T(CreatureView3TangibleScalarUpdate.FromUnsigned(6,123)),T(CreatureView3TangibleScalarUpdate.FromUnsigned(7,uint.MaxValue)),T(CreatureView3TangibleScalarUpdate.FromUnsigned(8,0x80000000)),T(CreatureView3TangibleScalarUpdate.FromUnsigned(9,17)),T(CreatureView3TangibleScalarUpdate.FromVisibility(0)),T(CreatureView3TangibleScalarUpdate.FromComplexity(-0.0f)),T(CreatureView3TangibleScalarUpdate.FromVisibility(255)));
            var value=store.Apply(seed,plan)!;var common=value.TangibleScalars;Check(BitConverter.SingleToUInt32Bits(common.Complexity)==0x80000000&&common.Volume==uint.MaxValue&&common.Options==123&&common.UseCount==uint.MaxValue&&common.ConditionDamage==0x80000000&&common.MaximumCondition==17&&common.Visibility==255,"All common fields/duplicates/signed zero");
            Check(ReferenceEquals(seed.Names,value.Names)&&ReferenceEquals(seed.Customization,value.Customization)&&ReferenceEquals(seed.Components,value.Components)&&store.RetainedDecodedBytes==charge,"Fixed scalar-only state/charge unchanged");
            Reject<InvalidDataException>(()=>new TangibleView3DeltaPlan(id,0,new[]{CreatureView3NamedDeltaUpdate.FromExtended(CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromScalar(new CreatureRankUpdate(1))))},4));
            receipts.Add(new{Name="common",ComplexityBits=BitConverter.SingleToUInt32Bits(common.Complexity),common.Volume,common.Options,common.UseCount,common.ConditionDamage,common.MaximumCondition,common.Visibility,Retained=charge});cases.Add("all-common-fixed-fields");
        }
        {
            var registry=Registry();var first=Create(registry,id);using var store=new SceneTangibleReplicationStore(registry,new(Capacity:1,MaximumComponentsElements:4));store.Seed(first,baseline);registry.Destroy(1,new(id,0));var second=Create(registry,id+1);
            Reject<InvalidDataException>(()=>store.Seed(second,baseline));Reject<InvalidDataException>(()=>store.Seed(second,Baseline(id+1,1)));Check(store.Revision==1,"Rejected Seed preserves stale capacity/revision");
            Check(store.Snapshots().Count==0&&store.Seed(second,Baseline(id+1,1)) is not null,"Read stale retirement then Seed recovery");registry.Close();Check(store.RetainedDecodedBytes==0&&store.Find(second) is null,"Closed borrowed registry retires owned state");cases.Add("seed-stale-nonpruning-registry-close");
        }
        foreach(var invalid in new[]{new SceneTangibleReplicationLimits(Capacity:0),new(MaximumDecodedBytes:46),new(MaximumRawComponentKeys:-1),new(MaximumComponentsElements:65537),new(MaximumStringBytes:1048577),new(MaximumUnicodeUnits:524289),new(MaximumUpdates:0),new(MaximumComponentCommands:65537)})Reject<ArgumentOutOfRangeException>(()=>new SceneTangibleReplicationStore(Registry(),invalid));cases.Add("constructor-bounds");
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,Cases=cases,Receipts=receipts,ModelProjectionOnly=true,LoaderCalls=0},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R26_TANGIBLE_STATE_PASS checks={checks} cases={cases.Count}");
    }
}
