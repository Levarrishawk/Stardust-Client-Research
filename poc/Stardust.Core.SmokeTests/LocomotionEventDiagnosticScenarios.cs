using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using Stardust.Core.Assets;

internal static class LocomotionEventDiagnosticScenarios
{
    internal static void Run()
    {
        var idle=Fixture(8,0,"idle",new ushort[]{0,3,8});var walk=Decode(FixtureBytes(8,2,"step",new ushort[]{0,2,2,5,8},true));var run=Fixture(4,8,"run",new ushort[]{0,1,3,4});
        LocomotionEventPreviewClock Create()=>new(new(16,8,1,0),new(16,8,1,2),new(16,4,1,8),4,idle,walk,run);
        var duplicateClock=Create();var duplicateLog=new LocomotionEventDiagnosticLog(duplicateClock,3);
        var duplicateBatch=duplicateClock.Tick(.25,new(0,0,.5f));Check(duplicateBatch.Count==3&&duplicateBatch.All(e=>e.Frame==2),"Duplicate authored frames");duplicateLog.Deliver(duplicateBatch);Check(duplicateLog.Count==3,"Distinct occurrence indices retained");
        foreach(int capacity in new[]{1,3,4096})
        {
            var clock=Create();var log=new LocomotionEventDiagnosticLog(clock,capacity);var foreign=Create();
            string Snapshot()=>$"{log.Generation}/{log.DeliveredTotal}/{log.GenerationDeliveredTotal}/{string.Join(';',log.Recent)}";
            void Reject(Action action){string before=Snapshot();try{action();throw new Exception("Expected rejection");}catch(InvalidDataException){}Check(before==Snapshot(),"Atomic rejection");}
            Reject(()=>log.Deliver(clock.Tick(0,Vector3.Zero)));Reject(()=>log.Deliver(foreign.Tick(.125,Vector3.Zero)));
            var first=clock.Tick(.125,Vector3.Zero);log.Deliver(first);var retained=log.Recent;log.ClearRecent();Reject(()=>log.Deliver(first));log.SynchronizeGeneration();Reject(()=>log.Deliver(first));
            var walking=clock.Tick(.125,new(0,0,.25f));log.SynchronizeGeneration();Check(log.Count==0&&log.GenerationDeliveredTotal==0&&log.DeliveredTotal==1,"Switch clear lifetime");if(walking.Count>0)log.Deliver(walking);
            var earlier=clock.Tick(.125,new(0,0,.25f));var later=clock.Tick(.125,new(0,0,.25f));log.Deliver(later);Reject(()=>log.Deliver(earlier));Reject(()=>log.Deliver(later));
            long total=log.DeliveredTotal;clock.Reset(Vector3.UnitX);Reject(()=>log.Deliver(later));Reject(()=>log.Deliver(clock.Tick(0,Vector3.Zero)));log.SynchronizeGeneration();Check(log.Count==0&&log.DeliveredTotal==total&&log.GenerationDeliveredTotal==0,"Reset lifetime");
            Check(retained.Count==1&&retained[0].Occurrence.Name=="idle","Retained owned snapshot");
            for(int i=0;i<2000;i++){var batch=clock.Tick(.125,new(0,0,2));if(batch.Count>0)log.Deliver(batch);}
            Check(log.Count==capacity&&log.DeliveredTotal>3000,"Bounded long ring");var recent=log.Recent;Check(recent.Zip(recent.Skip(1),(a,b)=>b.Sequence>a.Sequence).All(v=>v),"Lifetime sequence");
            total=log.DeliveredTotal;var inactive=clock.Tick(.125,Vector3.UnitZ,false);Reject(()=>log.Deliver(inactive));Check(log.DeliveredTotal==total,"Inactive no delivery");
            var blocked=clock.Tick(.125,Vector3.Zero);log.SynchronizeGeneration();if(blocked.Count>0)log.Deliver(blocked);Check(log.Recent.All(r=>r.Occurrence.Name=="idle"),"Blocked moving history cleared");
        }
        foreach(int bad in new[]{0,4097})try{new LocomotionEventDiagnosticLog(Create(),bad);throw new Exception("Capacity accepted");}catch(InvalidDataException){}
        Console.WriteLine("PASS: accepted-movement sink capacities1/3/4096;2000 ticks each, atomic foreign/stale/empty/replay/backward rejection; clear/switch/reset lifetime and ownership");
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static CompressedLocomotion Decode(byte[] bytes)=>CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(bytes));
    private static CompressedLocomotion Fixture(ushort end,float speed,string name,ushort[] frames)=>Decode(FixtureBytes(end,speed,name,frames));
    private static byte[] FixtureBytes(ushort end,float speed,string name,ushort[] frames,bool second=false)
    {
        byte[] Chunk(string tag,byte[] payload){byte[] length=new byte[4];BinaryPrimitives.WriteInt32BigEndian(length,payload.Length);return Encoding.ASCII.GetBytes(tag).Concat(length).Concat(payload).ToArray();}
        byte[] Form(string tag,params byte[][] children)=>Chunk("FORM",Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(c=>c)).ToArray());
        byte[] Group(ushort[] values)=>Chunk("MESG",BitConverter.GetBytes((ushort)values.Length).Concat(Encoding.ASCII.GetBytes(name+"\0")).Concat(values.SelectMany(BitConverter.GetBytes)).ToArray());
        var children=new List<byte[]>{Chunk("INFO",BitConverter.GetBytes((ushort)(second?2:1))),Group(frames)};if(second)children.Add(Group(new ushort[]{2}));
        byte[] info=BitConverter.GetBytes(16f).Concat(new short[]{(short)end,0,0,0,0,0}.SelectMany(BitConverter.GetBytes)).ToArray();
        using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.UTF8,true)){writer.Write(speed);writer.Write((short)2);writer.Write((ushort)0);writer.Write(0f);writer.Write(0f);writer.Write(0f);writer.Write(end);writer.Write(0f);writer.Write(0f);writer.Write(0f);}
        return Form("CKAT",Form("0001",Chunk("INFO",info),Form("XFRM"),Form("MSGS",children.ToArray()),Chunk("LOCT",stream.ToArray())));
    }
}
