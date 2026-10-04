using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;

public static class CreatureView4SupportedScenarios
{
    private static CreatureView4Limits BaselineLimits=>new(8388608,4096,65536,196608,1048576,4194304,16777216,16777216);
    private static CreatureView4SupportedLimits Message=>new(8388608,4096,65536,1048576,1048576,4194304,16777216,16777216);
    private static CreatureView4StateLimits ModelLimits=>new(4096,65536,196608,1048576,4194304,16777216,262144,33554432,16777216);
    private static CreatureView4SupportedStateLimits StateLimits=>new(ModelLimits,Message);
    private static object Text(ReplicationByteString s)=>new{Hex=Convert.ToHexString(s.CopyBytes()),Extended=s.UsesExtendedLength};
    private static object Model(CreatureView4Baseline m)=>new{m.ObjectId,m.DeclaredCount,FloatBits=m.FloatBits.ToArray(),m.ListenTo,
        Encumbrances=new{m.Encumbrances.Counter,Values=m.Encumbrances.Values.ToArray()},
        SkillMods=new{m.SkillMods.Counter,Values=m.SkillMods.Values.Select(s=>new{s.RawCommand,Name=Text(s.Name),s.BaseValue,s.Bonus}).ToArray()},
        SpaceMissionObjects=new{m.SpaceMissionObjects.Counter,Values=m.SpaceMissionObjects.Values.ToArray()}};
    private static object Row(CreatureView4SupportedState s)=>new{Model=Model(s.Model),s.Revision,s.DecodedBytes};
    private static byte[] BinaryPrimitivesBytes(ushort field){var b=new byte[2];BinaryPrimitives.WriteUInt16LittleEndian(b,field);return b;}
    private static object Event(CreatureView4SupportedEvent e)=>new{e.Kind,e.Field,e.UpdateOrdinal,e.Opcode,e.Index,e.OldBits,e.NewBits,e.OldValue,e.NewValue,Name=e.Name is null?null:Text(e.Name),e.OldBase,e.OldBonus,e.NewBase,e.NewBonus,e.LogicalBytes};
    public static void Run(string fixture,string output)
    {
        if(new FileInfo(fixture).Length>33554432)throw new InvalidDataException("Bounded synthetic composed fixture");
        using var doc=JsonDocument.Parse(File.ReadAllBytes(fixture));var records=new List<object>();int negatives=0,budgets=0,ownership=0;
        void Check(bool value,string why){if(!value)throw new Exception(why);}
        void Reject(Action action,bool budget=false){try{action();}catch(Exception e)when(e is InvalidDataException or ArgumentException or OverflowException){negatives++;if(budget)budgets++;return;}throw new Exception("Expected rejection");}
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;var bw=Convert.FromHexString(c.GetProperty("Baseline").GetString()!);var wire=Convert.FromHexString(c.GetProperty("Hex").GetString()!);
            var baseline=CreatureView4Codec.DecodeBaseline(bw,BaselineLimits);var plan=CreatureView4SupportedCodec.Decode(wire,Message);
            var state=CreatureView4SupportedState.FromBaseline(baseline,StateLimits);string old=JsonSerializer.Serialize(Row(state));
            Check(CreatureView4SupportedCodec.Encode(plan,Message).SequenceEqual(wire),name+" exact union wire");
            var change=state.Apply(plan);Check(change.Current.Revision==2&&change.Current.Model.DeclaredCount==baseline.DeclaredCount&&change.DeclaredCount==65535,name+" distinct metadata/revision");
            Check(ReferenceEquals(baseline.SpaceMissionObjects,change.Current.Model.SpaceMissionObjects),name+" untouched collection identity");
            Check(JsonSerializer.Serialize(Row(state))==old,name+" retained previous full model");ownership+=2;
            var alias=(byte[])wire.Clone();var frozen=CreatureView4SupportedCodec.Decode(alias,Message);Array.Clear(alias);Check(CreatureView4SupportedCodec.Encode(frozen,Message).SequenceEqual(wire),name+" decode ownership");ownership++;
            var updateAlias=plan.Updates.ToArray();var frozenPlan=new CreatureView4SupportedDelta(plan.ObjectId,plan.DeclaredCount,updateAlias);Array.Clear(updateAlias);Check(CreatureView4SupportedCodec.Encode(frozenPlan,Message).SequenceEqual(wire),name+" union ownership");ownership++;
            var envelope=ReplicationEnvelopeCodec.DecodeDelta(wire,Message.MaximumPayloadBytes);
            Check(CreatureView4SupportedCodec.Encode(CreatureView4SupportedCodec.Decode(envelope,Message),Message).SequenceEqual(wire),name+" envelope overload");
            Check(ReplicationEnvelopeCodec.EncodeDelta(CreatureView4SupportedCodec.EncodeEnvelope(plan,Message),Message.MaximumPayloadBytes).SequenceEqual(wire),name+" encode envelope overload");
            var mc=c.GetProperty("CodecCost");int MN(string k)=>mc.GetProperty(k).GetInt32();long ML(string k)=>mc.GetProperty(k).GetInt64();
            var exactMessage=new CreatureView4SupportedLimits(MN("Payload"),MN("Fields"),MN("Commands"),MN("Values"),MN("MaxKey"),MN("Keys"),ML("Decoded"),ML("Work"));
            Check(CreatureView4SupportedCodec.Encode(plan,exactMessage).SequenceEqual(wire),name+" exact message caps");_=CreatureView4SupportedCodec.Decode(wire,exactMessage);budgets+=2;
            var messageLows=new List<CreatureView4SupportedLimits>{exactMessage with{MaximumPayloadBytes=exactMessage.MaximumPayloadBytes-1},exactMessage with{MaximumFields=exactMessage.MaximumFields-1},exactMessage with{MaximumDecodedBytes=exactMessage.MaximumDecodedBytes-1},exactMessage with{MaximumWork=exactMessage.MaximumWork-1}};
            if(exactMessage.MaximumCommands>0)messageLows.Add(exactMessage with{MaximumCommands=exactMessage.MaximumCommands-1});
            if(exactMessage.MaximumValues>0)messageLows.Add(exactMessage with{MaximumValues=exactMessage.MaximumValues-1});
            if(exactMessage.MaximumKeyBytes>0)messageLows.Add(exactMessage with{MaximumKeyBytes=exactMessage.MaximumKeyBytes-1});
            if(exactMessage.MaximumTotalKeyBytes>0)messageLows.Add(exactMessage with{MaximumTotalKeyBytes=exactMessage.MaximumTotalKeyBytes-1});
            foreach(var low in messageLows){Reject(()=>CreatureView4SupportedCodec.Encode(plan,low),true);Reject(()=>CreatureView4SupportedCodec.Decode(wire,low),true);}
            var sc=c.GetProperty("StateCost");int N(string k)=>sc.GetProperty(k).GetInt32();long L(string k)=>sc.GetProperty(k).GetInt64();
            var exactModel=new CreatureView4StateLimits(N("Updates"),N("Elements"),N("TotalElements"),N("MaxString"),N("Strings"),L("StateBytes"),N("Events"),L("OutputBytes"),L("Work"));
            var exact=new CreatureView4SupportedStateLimits(exactModel,exactMessage);var exactChange=state.Apply(plan,exact);
            Check(JsonSerializer.Serialize(Row(exactChange.Current))==JsonSerializer.Serialize(Row(change.Current))&&exactChange.WorkUsed==change.WorkUsed,name+" exact combined caps");budgets++;
            Check(change.WorkUsed==L("Work")&&change.Events.Count==N("Events")&&change.DecodedOutputBytes<=L("OutputBytes"),name+" independent full work/event/peak receipts");
            var lows=new List<CreatureView4StateLimits>{exactModel with{MaximumUpdates=exactModel.MaximumUpdates-1},exactModel with{MaximumStateBytes=exactModel.MaximumStateBytes-1},exactModel with{MaximumOutputBytes=exactModel.MaximumOutputBytes-1},exactModel with{MaximumWork=exactModel.MaximumWork-1}};
            if(exactModel.MaximumElements>0)lows.Add(exactModel with{MaximumElements=exactModel.MaximumElements-1});
            if(exactModel.MaximumTotalElements>0)lows.Add(exactModel with{MaximumTotalElements=exactModel.MaximumTotalElements-1});
            if(exactModel.MaximumEvents>0)lows.Add(exactModel with{MaximumEvents=exactModel.MaximumEvents-1});
            if(exactModel.MaximumStringBytes>0)lows.Add(exactModel with{MaximumStringBytes=exactModel.MaximumStringBytes-1});
            if(exactModel.MaximumTotalStringBytes>0)lows.Add(exactModel with{MaximumTotalStringBytes=exactModel.MaximumTotalStringBytes-1});
            foreach(var low in lows){Reject(()=>state.Apply(plan,new(low,exactMessage)),true);Check(JsonSerializer.Serialize(Row(state))==old,name+" failure retains complete previous state");ownership++;}
            // Message caps are still enforced by Apply, even for already owned plans.
            Reject(()=>state.Apply(plan,new(ModelLimits,exactMessage with{MaximumWork=exactMessage.MaximumWork-1})),true);
            var seedCost=c.GetProperty("SeedCost");int SN(string k)=>seedCost.GetProperty(k).GetInt32();long SL(string k)=>seedCost.GetProperty(k).GetInt64();
            var seedLimits=new CreatureView4StateLimits(4096,SN("Elements"),SN("TotalElements"),SN("MaxString"),SN("Strings"),SL("StateBytes"),0,SL("OutputBytes"),SL("Work"));
            _=CreatureView4SupportedState.FromBaseline(baseline,new(seedLimits,Message));budgets++;
            Reject(()=>CreatureView4SupportedState.FromBaseline(baseline,new(seedLimits with{MaximumWork=seedLimits.MaximumWork-1},Message)),true);
            Reject(()=>CreatureView4SupportedState.FromBaseline(baseline,new(seedLimits with{MaximumStateBytes=seedLimits.MaximumStateBytes-1},Message)),true);
            // Every outer cut; inner cuts for small bodies, deterministic samples for large replacements.
            int increment=wire.Length>2048?Math.Max(1,wire.Length/100):1;
            for(int cut=0;cut<wire.Length;cut+=increment){int at=cut;Reject(()=>CreatureView4SupportedCodec.Decode(wire.AsSpan(0,at),Message));}
            var valid=c.GetProperty("ValidPrefixes").EnumerateArray().Select(v=>v.GetInt32()).ToHashSet();
            for(int cut=25;cut<wire.Length;cut+=increment){var partial=wire[..cut];BinaryPrimitives.WriteUInt32LittleEndian(partial.AsSpan(19),(uint)(cut-23));
                try{var prefix=CreatureView4SupportedCodec.Decode(partial,Message);if(increment==1)Check(valid.Contains(cut),name+" legal prefix");Check(CreatureView4SupportedCodec.Encode(prefix,Message).SequenceEqual(partial),name+" exact legal prefix");}
                catch(InvalidDataException){if(increment==1)Check(!valid.Contains(cut),name+" illegal prefix");negatives++;}}
            var alien=new CreatureView4SupportedDelta(plan.ObjectId^0x8000000000000000UL,plan.DeclaredCount,plan.Updates);Reject(()=>state.Apply(alien));ownership++;
            var wrong=new CreatureView4Baseline(baseline.ObjectId^0x8000000000000000UL,baseline.DeclaredCount,baseline.FloatBits,baseline.ListenTo,baseline.Encumbrances,baseline.SkillMods,baseline.SpaceMissionObjects);Reject(()=>state.Reseed(wrong));
            Reject(()=>CreatureView4SupportedState.FromBaseline(baseline,StateLimits,ulong.MaxValue));
            var last=CreatureView4SupportedState.FromBaseline(baseline,StateLimits,ulong.MaxValue-1);Reject(()=>last.Apply(plan));Reject(()=>last.Reseed(baseline));
            string published=JsonSerializer.Serialize(Row(change.Current));Reject(()=>change.Current.Apply(plan,new(ModelLimits with{MaximumWork=0},Message)));Check(JsonSerializer.Serialize(Row(change.Current))==published,name+" prior successful result survives later failure");ownership++;
            Check(JsonSerializer.Serialize(Row(Task.Run(()=>state.Apply(plan)).GetAwaiter().GetResult().Current))==JsonSerializer.Serialize(Row(change.Current)),name+" concurrent immutable branches");ownership++;
            records.Add(new{Name=name,Hex=Convert.ToHexString(CreatureView4SupportedCodec.Encode(plan,Message)),Initial=Row(state),Current=Row(change.Current),Events=change.Events.Select(Event).ToArray(),change.WorkUsed,change.DeclaredCount,change.DecodedOutputBytes,Reseed=Row(change.Current.Reseed(baseline))});
        }
        foreach(ushort field in new ushort[]{13,14,65535}) {byte[] bad=BinaryPrimitivesBytes(field);Reject(()=>CreatureView4SupportedCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,CreatureView4Codec.RawTag,4,1,bad,8388608),Message));}
        foreach(byte op in Enumerable.Range(3,253).Select(x=>(byte)x)) {byte[] bad=new byte[11];BinaryPrimitives.WriteUInt16LittleEndian(bad,3);BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(2),1);bad[10]=op;Reject(()=>CreatureView4SupportedCodec.Decode(new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,1,CreatureView4Codec.RawTag,4,1,bad,8388608),Message));}
        var emptyBaseline=new CreatureView4Baseline(ulong.MaxValue,0,new uint[10],ulong.MaxValue,new(0,Array.Empty<int>()),new(0,Array.Empty<CreatureView4SkillMod>()),new(0,Array.Empty<CreatureView4MissionPair>()));
        var own=CreatureView4SupportedState.FromBaseline(emptyBaseline,StateLimits);
        var noOp=new CreatureView4SupportedDelta(ulong.MaxValue,0,[new(new CreatureEncumbrancesView4Update(2,0,uint.MaxValue,[]))]);
        var noOpChange=own.Apply(noOp);Check(noOpChange.Current.Revision==2&&noOpChange.Events.Count==0&&ReferenceEquals(noOpChange.Current.Model.Encumbrances,emptyBaseline.Encumbrances),"Zero extent increments full revision without inventing child mutation");ownership++;
        var extra=new CreatureView4SupportedDelta(ulong.MaxValue,0,[new(new CreatureEncumbrancesView4Update(2,0,0,[new(4,0,[])]))]);Reject(()=>own.Apply(extra));
        var missing=new CreatureView4SupportedDelta(ulong.MaxValue,0,[new(new CreatureEncumbrancesView4Update(2,1,0,[]))]);Reject(()=>own.Apply(missing));
        Reject(()=>own.Apply(new(ulong.MaxValue,0,[])));Reject(()=>new CreatureView4SupportedDelta(0,0,[null!]));
        foreach(ushort f in new ushort[]{13,14,65535})Reject(()=>new CreatureView4SupportedUpdate(new CreatureView4Update(f,0)));
        Reject(()=>CreatureView4SupportedCodec.Decode([],Message with{MaximumValues=-1}));Reject(()=>own.Apply(noOp,new(ModelLimits with{MaximumElements=65537},Message)));
        var scalar=new CreatureView4SupportedUpdate(new CreatureView4Update(6,ulong.MaxValue));
        var scalarPlan=new CreatureView4SupportedDelta(ulong.MaxValue,0,[scalar]);var scalarWire=CreatureView4SupportedCodec.Encode(scalarPlan,Message);
        foreach(int offset in new[]{0,2,14,18,19}){var bad=(byte[])scalarWire.Clone();bad[offset]^=0x7f;Reject(()=>CreatureView4SupportedCodec.Decode(bad,Message));}
        var metadata=(byte[])scalarWire.Clone();metadata[23]^=0x7f;Check(CreatureView4SupportedCodec.Decode(metadata,Message).DeclaredCount==127,"Declared count is preserved metadata");
        var commandValues=new[]{7};var command=new CreatureEncumbrancesView4Command(2,0,commandValues);commandValues[0]=99;
        var commandAlias=new[]{command};var vectorUpdate=new CreatureEncumbrancesView4Update(2,1,1,commandAlias);commandAlias[0]=new(4,0,[]);
        var ownedUnion=new CreatureView4SupportedDelta(ulong.MaxValue,0,[new(vectorUpdate)]);
        Check(own.Apply(ownedUnion).Current.Model.Encumbrances.Values[0]==7,"Inherited child ownership survives composed alias mutation");ownership++;
        Reject(()=>new CreatureView4SupportedDelta(0,0,Enumerable.Repeat(scalar,4097)));
        foreach(var policy in new[]{Message with{MaximumPayloadBytes=8388609},Message with{MaximumFields=-1},Message with{MaximumCommands=65537},Message with{MaximumValues=1048577},Message with{MaximumDecodedBytes=16777217},Message with{MaximumWork=-1}})
            Reject(()=>CreatureView4SupportedCodec.Encode(scalarPlan,policy));
        // Late growth failure cannot leak a preceding scalar assignment.
        var fullBaseline=new CreatureView4Baseline(ulong.MaxValue,14,new uint[10],0,new(0,new int[65536]),emptyBaseline.SkillMods,emptyBaseline.SpaceMissionObjects);
        var full=CreatureView4SupportedState.FromBaseline(fullBaseline,StateLimits);
        var late=new CreatureView4SupportedDelta(ulong.MaxValue,0,[new(new CreatureView4Update(0,0xffffffff)),new(new CreatureEncumbrancesView4Update(2,1,1,[new(1,65535,[9])]))]);
        Reject(()=>full.Apply(late),true);Check(full.Model.FloatBits[0]==0&&full.Model.Encumbrances.Values.Count==65536&&full.Revision==1,"Late hard growth rejects entire ordered candidate");ownership++;
        File.WriteAllText(output,JsonSerializer.Serialize(new{Records=records,Negatives=negatives,Budgets=budgets,Ownership=ownership},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"R41_CREO4_SUPPORTED_PASS cases={records.Count} negatives={negatives} budgets={budgets} ownership={ownership}");
    }
}
