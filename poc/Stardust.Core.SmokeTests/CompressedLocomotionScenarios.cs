using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class CompressedLocomotionScenarios
{
    internal static void Run()
    {
        var bytes=Fixture();var motion=CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(bytes));Array.Clear(bytes);
        if(motion.AuthoredSpeed!=3||motion.Positions.Count!=3||motion.SamplePosition(2.5)!=new Vector3(1,2,3)||motion.TranslationDelta(2.5,7.5)!=new Vector3(3,4,5))throw new Exception("Owned LOCT linear interpolation/delta mismatch.");
        if(motion.CollectMessages(0,5).Count!=1||motion.CollectMessages(5,10).Count!=1||motion.CollectMessages(10,11).Count!=1||motion.CollectMessages(5,5).Count!=0)throw new Exception("MSGS half-open endpoint collector mismatch.");
        void Reject(Action a)=>HostAccountingScenarios.Reject<InvalidDataException>(a);
        foreach(var frame in new[]{-1.0,11,double.NaN,double.PositiveInfinity})Reject(()=>motion.SamplePosition(frame));
        Reject(()=>motion.CollectMessages(-1,1));Reject(()=>motion.CollectMessages(5,4));Reject(()=>motion.CollectMessages(0,12));
        foreach(var bad in new[]{float.NaN,float.PositiveInfinity,-1f})Reject(()=>Decode(Fixture(speed:bad)));
        Reject(()=>Decode(Fixture(time:5)));Reject(()=>Decode(Fixture(time:11)));Reject(()=>Decode(Fixture(position:float.NaN)));
        Reject(()=>Decode(Fixture(messageCount:2)));Reject(()=>Decode(Fixture(messageFrames:new ushort[]{5,0,10})));Reject(()=>Decode(Fixture(messageFrames:new ushort[]{0,5,11})));
        Reject(()=>Decode(Fixture(extra:true)));Reject(()=>Decode(Fixture(rootRotation:true)));
        byte[] valid=Fixture();for(int i=0;i<valid.Length;i++)Reject(()=>Decode(valid.AsSpan(0,i).ToArray()));
        var empty=Decode(Fixture(noSuffix:true));if(empty.HasTranslation||empty.Messages.Count!=0)throw new Exception("Empty locomotion suffix mismatch.");Reject(()=>empty.SamplePosition(0));
        Console.WriteLine("PASS: owned MSGS/translation-only LOCT, half-open message endpoints, linear positions/deltas and truncation/count/time/float/unsupported-root gates");
    }
    private static CompressedLocomotion Decode(byte[] bytes)=>CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(bytes));
    private static byte[] Fixture(float speed=3,ushort time=10,float position=6,ushort messageCount=1,ushort[]? messageFrames=null,bool extra=false,bool rootRotation=false,bool noSuffix=false)
    {
        byte[] info=BitConverter.GetBytes(30f).Concat(new short[]{10,0,0,0,0,0}.SelectMany(BitConverter.GetBytes)).ToArray();
        var suffix=new List<byte[]>();
        if(!noSuffix)
        {
            var frames=messageFrames??new ushort[]{0,5,10};byte[] mesg=BitConverter.GetBytes((ushort)frames.Length).Concat(Encoding.ASCII.GetBytes("event_footstep\0")).Concat(frames.SelectMany(BitConverter.GetBytes)).ToArray();
            suffix.Add(MeshScenarios.Form("MSGS",MeshScenarios.Chunk("INFO",BitConverter.GetBytes(messageCount)),MeshScenarios.Chunk("MESG",mesg)));
            using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);writer.Write(speed);writer.Write((short)3);
            foreach(var key in new[]{(Frame:(ushort)0,Position:Vector3.Zero),(Frame:(ushort)5,Position:new Vector3(2,4,6)),(Frame:time,Position:new Vector3(position,8,10))}){writer.Write(key.Frame);writer.Write(key.Position.X);writer.Write(key.Position.Y);writer.Write(key.Position.Z);}
            if(extra)writer.Write((byte)0);suffix.Add(MeshScenarios.Chunk("LOCT",stream.ToArray()));
            if(rootRotation)suffix.Add(MeshScenarios.Chunk("QCHN",new byte[7]));
        }
        return MeshScenarios.Form("CKAT",MeshScenarios.Form("0001",new[]{MeshScenarios.Chunk("INFO",info),MeshScenarios.Form("XFRM")}.Concat(suffix).ToArray()));
    }
    internal static void Probe(string[] args)
    {
        if(args.Length!=3)throw new InvalidDataException("Expected compressed clip file and exact SHA256.");
        byte[] bytes=File.ReadAllBytes(args[1]);string hash=Convert.ToHexString(SHA256.HashData(bytes));if(hash!=args[2])throw new InvalidDataException("Compressed clip SHA256 mismatch.");
        var clip=CompressedKeyframeDecoder.Decode(bytes);var motion=CompressedLocomotion.Create(clip);var sampler=new CompressedKeyframeSampler(clip);
        Console.WriteLine("locomotion_clip="+JsonSerializer.Serialize(new{sha256=hash,bytes=bytes.Length,clip.FrameRate,clip.EndFrame,motion.AuthoredSpeed,transforms=clip.Transforms.Count,keys=motion.Positions.Select(k=>new{frame=k.Frame,position=new[]{k.Position.X,k.Position.Y,k.Position.Z}}),messages=motion.Messages.Select(m=>new{name=m.Name,frames=m.Frames})}));
        for(int i=0;i<=clip.EndFrame*4;i++)
        {
            double frame=i*.25;var position=motion.SamplePosition(frame);var pose=sampler.SampleFrame(frame);
            Console.WriteLine("locomotion_sample="+JsonSerializer.Serialize(new{frame,position=new[]{position.X,position.Y,position.Z},pose_transforms=pose.Count}));
        }
    }
}
