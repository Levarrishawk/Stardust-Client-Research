using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

public readonly record struct LocomotionPositionKey(ushort Frame,Vector3 Position);
public sealed record AnimationMessageGroup(string Name,IReadOnlyList<ushort> Frames);

/// <summary>Owned MSGS and translation-only LOCT. Root rotation and event dispatch are deliberately separate.</summary>
public sealed class CompressedLocomotion
{
    public float AuthoredSpeed{get;}
    public ushort EndFrame{get;}
    public IReadOnlyList<LocomotionPositionKey> Positions{get;}
    public IReadOnlyList<AnimationMessageGroup> Messages{get;}
    public bool HasTranslation=>Positions.Count!=0;

    private CompressedLocomotion(float speed,ushort end,LocomotionPositionKey[] positions,AnimationMessageGroup[] messages)
    {AuthoredSpeed=speed;EndFrame=end;Positions=Array.AsReadOnly(positions);Messages=Array.AsReadOnly(messages);}

    public static CompressedLocomotion Create(CompressedKeyframeDescription clip)
    {
        ArgumentNullException.ThrowIfNull(clip);Require(clip.EndFrame>0,"Invalid locomotion duration.");
        var keys=Array.Empty<LocomotionPositionKey>();var messages=new List<AnimationMessageGroup>();float speed=0;int previous=-1,total=0;
        foreach(var node in clip.UninterpretedSuffix)
        {
            int order=node.IsForm("MSGS")?0:node.Tag=="LOCT"?1:-1;
            Require(order>previous,"Unsupported/duplicate locomotion suffix (root rotation requires another evaluator).");previous=order;
            if(order==0)
            {
                var children=node.Children;Require(children.Count>0&&children[0].Tag=="INFO"&&children[0].Payload.Length==2,"Invalid MSGS header.");
                Require(U16(children[0].Payload.Span)==children.Count-1&&children.Count<=4097,"Invalid MSGS group count.");
                foreach(var child in children.Skip(1))
                {
                    var raw=child.Payload.Span;Require(child.Tag=="MESG"&&raw.Length>=4,"Invalid MESG record.");int count=U16(raw),stop=raw[2..].IndexOf((byte)0)+2;
                    Require(stop>2&&stop<=1026&&raw.Length==stop+1+count*2,"Invalid MESG name/count/width.");
                    string name=SkeletalAppearanceDecoder.Strings(raw.Slice(2,stop-1),1)[0];var frames=new ushort[count];
                    Require((total+=count)<=262144,"Animation message occurrence budget.");
                    for(int i=0;i<count;i++){frames[i]=U16(raw[(stop+1+i*2)..]);Require(frames[i]<=clip.EndFrame&&(i==0||frames[i]>=frames[i-1]),"Invalid message occurrence order/bounds.");}
                    messages.Add(new(name,Array.AsReadOnly(frames)));
                }
            }
            else
            {
                var raw=node.Payload.Span;Require(raw.Length>=6,"Truncated LOCT record.");speed=F32(raw);int count=BinaryPrimitives.ReadInt16LittleEndian(raw[4..]);
                Require(float.IsFinite(speed)&&speed>=0&&speed<=1e6&&count>=2&&raw.Length==6+count*14,"Invalid LOCT speed/count/width.");keys=new LocomotionPositionKey[count];
                for(int i=0;i<count;i++)
                {
                    var value=raw[(6+i*14)..];ushort frame=U16(value);var position=new Vector3(F32(value[2..]),F32(value[6..]),F32(value[10..]));
                    Require(frame<=clip.EndFrame&&(i==0||frame>keys[i-1].Frame)&&Finite(position),"Invalid LOCT position/time.");keys[i]=new(frame,position);
                }
            }
        }
        return new(speed,clip.EndFrame,keys,messages.ToArray());
    }

    public Vector3 SamplePosition(double frame)
    {
        Require(double.IsFinite(frame)&&frame>=0&&frame<=EndFrame,"Locomotion sample outside clip.");Require(HasTranslation,"No LOCT translation track.");
        if(frame<=Positions[0].Frame)return Positions[0].Position;
        if(frame>=Positions[^1].Frame)return Positions[^1].Position;
        int low=0,high=Positions.Count-1;while(high-low>1){int mid=(low+high)/2;if(Positions[mid].Frame<=frame)low=mid;else high=mid;}
        var first=Positions[low];var second=Positions[high];float fraction=(float)((frame-first.Frame)/(second.Frame-first.Frame));
        return first.Position+(second.Position-first.Position)*fraction;
    }

    public Vector3 TranslationDelta(double first,double second)=>SamplePosition(second)-SamplePosition(first);

    /// <summary>Recovered integer half-open collector; higher-level looping and callback scheduling are not supplied.</summary>
    public IReadOnlyList<(string Name,ushort Frame)> CollectMessages(int startFrame,int endFrame)
    {
        Require(startFrame>=0&&endFrame>=startFrame&&endFrame<=EndFrame+1,"Invalid message collection interval.");
        return Array.AsReadOnly(Messages.SelectMany(group=>group.Frames.Where(frame=>frame>=startFrame&&frame<endFrame).Select(frame=>(group.Name,frame))).ToArray());
    }
    private static bool Finite(Vector3 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z)&&Math.Abs(value.X)<=1e6&&Math.Abs(value.Y)<=1e6&&Math.Abs(value.Z)<=1e6;
    private static ushort U16(ReadOnlySpan<byte> value)=>BinaryPrimitives.ReadUInt16LittleEndian(value);
    private static float F32(ReadOnlySpan<byte> value)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(value));
    private static void Require(bool value,string message)=>SkeletalAppearanceDecoder.Require(value,message);
}
