using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneObjectResourceRetentionScenarios
{
    private static void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string Fingerprint(SceneObjectResources r) => Hash(JsonSerializer.SerializeToUtf8Bytes(r.Creature is {} c ? (object)c : r.Static!));
    // Independently authored synthetic protocol fields; no captured messages or original game content.
    private static byte[] Start(ulong id)
    {
        using var s = new MemoryStream(); using var w = new BinaryWriter(s);
        w.Write((ushort)9); w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode); w.Write((byte)0); w.Write(id);
        w.Write((ushort)1); w.Write((byte)'x'); w.Write(0f); w.Write(0f); w.Write(0f); w.Write((ushort)0); w.Write(0UL);
        return s.ToArray();
    }
    private static byte[] Baseline(ulong id, float height)
    {
        using var s = new MemoryStream(); using var w = new BinaryWriter(s);
        w.Write(1f); w.Write((ushort)0); w.Write(0U); w.Write((ushort)0); w.Write(0U); w.Write(0U); w.Write((ushort)0);
        w.Write(0U); w.Write(0U); for (int i=0;i<4;i++) w.Write(0U);
        w.Write((byte)0); w.Write((byte)1); w.Write((byte)2); w.Write(3UL); w.Write(height); w.Write(5U); w.Write(6UL); w.Write(0U); w.Write(0U);
        return ReplicationEnvelopeCodec.EncodeBaseline(new(ReplicationEnvelopeKind.Baseline,id,CreatureView3BaselineCodec.RawTag,3,17,s.ToArray(),1024),1024);
    }
    private static byte[] Height(ulong id, uint bits)
    {
        using var s = new MemoryStream(); using var w = new BinaryWriter(s); w.Write((ushort)14); w.Write(bits);
        return ReplicationEnvelopeCodec.EncodeDelta(new(ReplicationEnvelopeKind.Delta,id,CreatureView3BaselineCodec.RawTag,3,1,s.ToArray(),1024),1024);
    }
    internal static void Run(string[] inputs)
    {
        Check(inputs.Length==4,"Explicit root/config/catalog/bank required");
        Check(Hash(File.ReadAllBytes(inputs[1]))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Installed configuration pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(inputs[2]));
        using var bankFile=File.OpenRead(inputs[3]); var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var co=new SceneCreatureResourceOptions(idle,bank,true);
        var frame=new SwitchTextureFrameSelection("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0);
        var so=new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,frame);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{co.Recipe,co},{so.Recipe,so}};
        var human=new SceneCreateObjectByCrcMessage(101,-0f,0,0,1,10,20,30,2937962913,0);
        var terminal=new SceneCreateObjectByCrcMessage(202,0,0,0,1,40,50,60,2907007746,0);
        int scopes=0; var loads=new List<object>();
        SceneObjectResources Load(SceneCreateObjectByCrcMessage create, SceneObjectResourceOptions option)
        {
            var borrowed=new List<byte[]>(); var reads=new Dictionary<string,int>(); SceneObjectResources result;
            scopes++;
            try { using var cfg=File.OpenText(inputs[1]); using var overlay=ConfiguredTreOverlay.Open(inputs[0],cfg);
                TreLookupResult Read(string path) { reads[path]=reads.GetValueOrDefault(path)+1; var r=overlay.Lookup(path); if(r.Payload is not null)borrowed.Add(r.Payload); return r; }
                result=SceneObjectResourceRouter.Load(create,catalog,option,Read,CancellationToken.None);
                string fp=Fingerprint(result); foreach(var bytes in borrowed)Array.Clear(bytes); Check(fp==Fingerprint(result),"Borrowed-buffer ownership regression");
            } finally { scopes--; }
            Check(scopes==0 && reads.Count==result.ExternalReads && reads.Values.All(n=>n==1),"Closed archive, single reads");
            SceneObjectResourceValidation.Validate(result,create,catalog,option);
            var images=result.Creature?.Creature.MaterialBundle!.Textures ?? result.Static!.Appearance.Meshes.Values.SelectMany(m=>m.Textures).GroupBy(p=>p.Key).ToDictionary(g=>g.Key,g=>g.First().Value);
            loads.Add(new {result.ExternalReads,result.SourceBytes,Wire=Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(create)),Sources=result.Sources.Values.OrderBy(s=>s.Path).ToArray(),Textures=images.OrderBy(p=>p.Key).Select(p=>new {Path=p.Key,Mips=p.Value.Mips.Select(m=>Hash(m.Rgba.Span)).ToArray()}).ToArray(),Fingerprint=Fingerprint(result),ArchiveClosed=scopes==0});
            return result;
        }
        var hr=Load(human,co); var tr=Load(terminal,so);
        Check(hr.ExternalReads==39 && tr.ExternalReads==27,"Original two-recipe closure");
        var tasks=new List<TaskCompletionSource<SceneObjectResources>>(); var requests=new List<SceneObjectResourceRequest>();
        Task<SceneObjectResources> Controlled(SceneObjectResourceRequest q,CancellationToken token) { Check(scopes==0,"Archive closed before transfer"); requests.Add(q);var t=new TaskCompletionSource<SceneObjectResources>();tasks.Add(t);return t.Task; }
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,64,64,16,512),16,1024);
        using var world=new SceneWorldCoordinator(catalog,options,Controlled,SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,32,4096,1024,policy);
        long g=world.BeginSelection(human.ObjectId).Generation;
        world.Accept(g,Start(human.ObjectId)); world.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(human)); world.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(terminal));
        world.Accept(g,Baseline(human.ObjectId,4.5f));
        byte[] Opaque(ReplicationEnvelopeKind kind, byte marker) { var e=new ReplicationEnvelope(kind,terminal.ObjectId,0x54414E4F,3,1,new byte[]{marker,0xFF},1024); return kind==ReplicationEnvelopeKind.Baseline?ReplicationEnvelopeCodec.EncodeBaseline(e,1024):ReplicationEnvelopeCodec.EncodeDelta(e,1024); }
        world.Accept(g,Opaque(ReplicationEnvelopeKind.Baseline,7));
        foreach(var c in new[]{human,terminal})world.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(c.ObjectId)));
        world.CompleteMinimumSceneBarrier(g,true); world.PollObjectResources(); Check(tasks.Count==2,"Two initial loader calls");
        for(int i=0;i<2;i++)tasks[i].SetResult(requests[i].Snapshot.Create.ObjectId==human.ObjectId?hr:tr);
        var publications=world.PollObjectResources().Prepared; Check(publications.Count==2,"Two initial publications");
        var hp=publications.Single(p=>p.Ticket.Handle.ObjectId==human.ObjectId); var tp=publications.Single(p=>p.Ticket.Handle.ObjectId==terminal.ObjectId);
        var initialBatch=world.DrainChanges(g)!; Check(world.AcknowledgeChanges(initialBatch),"Initial acknowledgement");
        string hf=Fingerprint(hr),tf=Fingerprint(tr); var states=new List<object>();
        foreach(uint bits in new[]{0xBFE00000U,0x80000000U})
        {
            world.Accept(g,Height(human.ObjectId,bits)); world.Accept(g,Opaque(ReplicationEnvelopeKind.Delta,(byte)states.Count));
            var state=world.FindReplication(g,human.ObjectId)!; Check(BitConverter.SingleToUInt32Bits(state.Scalars.Height)==bits,"Exact Height bits");
            Check(world.PollObjectResources().Prepared.Count==0 && tasks.Count==2 && loads.Count==2,"No state-only reload");
            Check(world.IsCurrent(hp)&&world.IsCurrent(tp)&&ReferenceEquals(hp.Resources,hr)&&ReferenceEquals(tp.Resources,tr),"Exact retained publications");
            Check(Fingerprint(hr)==hf&&Fingerprint(tr)==tf,"Complete resource fingerprints stable");
            Check(SceneObjectLifecycleCodec.EncodeCreate(world.Snapshots().Single(s=>s.Object.Handle.ObjectId==human.ObjectId).Object.Create).SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(human)),"Height never rewrites raw placement");
            Check(world.Snapshots().Single(s=>s.Object.Handle.ObjectId==terminal.ObjectId).Envelopes.Count==2+states.Count,"Opaque TANO append");
            SceneObjectResourceValidation.Validate(hr,human,catalog,co); SceneObjectResourceValidation.Validate(tr,terminal,catalog,so);
            var batch=world.DrainChanges(g)!; Check(batch.Entries.Count==2&&world.AcknowledgeChanges(batch),"State batch acknowledgement");
            states.Add(new {HeightBits=bits,LoaderCalls=tasks.Count,HumanFingerprint=Fingerprint(hr),TerminalFingerprint=Fingerprint(tr),HumanCurrent=world.IsCurrent(hp),TerminalCurrent=world.IsCurrent(tp)});
        }
        world.Accept(g,SceneObjectLifecycleCodec.EncodeDestroy(new(terminal.ObjectId,0))); Check(!world.IsCurrent(tp)&&world.IsCurrent(hp),"Terminal-only retirement");
        world.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(terminal)); world.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(terminal.ObjectId))); world.PollObjectResources();
        Check(tasks.Count==3&&requests[2].Snapshot.Create==terminal,"Terminal-only replacement call");
        var replacement=Load(terminal,so); tasks[2].SetResult(replacement); var rp=world.PollObjectResources().Prepared.Single();
        Check(rp.Ticket!=tp.Ticket&&!world.IsCurrent(tp)&&world.IsCurrent(rp)&&world.IsCurrent(hp),"Recreation requires distinct current ticket");
        Check(!ReferenceEquals(replacement,tr)&&Fingerprint(replacement)==tf&&ReferenceEquals(hp.Resources,hr)&&hp.Ticket==publications.Single(p=>p.Ticket.Handle.ObjectId==human.ObjectId).Ticket,"Fresh terminal, exact human retained");
        var negatives=new List<string>();
        void Reject(string name,Action action) { try { action(); } catch(InvalidDataException) { negatives.Add(name); return; } throw new Exception("Accepted invalid "+name); }
        foreach(var pair in new[]{(hr,human,(SceneObjectResourceOptions)co),(tr,terminal,(SceneObjectResourceOptions)so)})
        {
            Reject("changed-create-"+pair.Item2.ObjectId,()=>SceneObjectResourceValidation.Validate(pair.Item1,pair.Item2 with {PositionY=31},catalog,pair.Item3));
            Reject("swapped-recipe-"+pair.Item2.ObjectId,()=>SceneObjectResourceValidation.Validate(pair.Item1,pair.Item2,catalog,pair.Item2.ObjectId==human.ObjectId?so:co));
        }
        Reject("changed-creature-option",()=>SceneObjectResourceValidation.Validate(hr,human,catalog,new SceneCreatureResourceOptions(idle,bank,false)));
        Reject("changed-static-option",()=>SceneObjectResourceValidation.Validate(tr,terminal,catalog,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,frame with {FrameIndex=1})));
        var finalBatch=world.DrainChanges(g)!; Check(world.AcknowledgeChanges(finalBatch)&&world.OutstandingJobs==0,"Recreate acknowledged, no outstanding work");
        world.Close(); Check(world.DrainCompletion.IsCompleted&&!world.IsCurrent(hp)&&!world.IsCurrent(rp),"Completed close invalidates publications");
        Console.WriteLine("retention_receipt="+JsonSerializer.Serialize(new {Loads=loads,States=states,Negatives=negatives,LoaderCalls=tasks.Count,ReplacementTicketDistinct=rp.Ticket!=tp.Ticket,OldTerminalRejected=true,HumanRetained=true,CloseDrained=true}));
        Console.WriteLine("PASS: resource retention, exact Height/raw create, opaque TANO, terminal recreation isolation and six gate negatives.");
    }
}
