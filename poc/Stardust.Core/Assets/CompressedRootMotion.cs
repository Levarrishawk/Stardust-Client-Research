using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

public enum CompressedRootMotionMode { None,Rotation,Translation,Combined }
public readonly record struct RootMotionDelta(Quaternion Rotation,Vector3 Translation,bool RotationRepaired);

/// <summary>Owned root-track computation. Output algebra has no actor/world authority.</summary>
public sealed class CompressedRootMotion
{
    public CompressedRootRotation RotationTrack{get;}
    public string SourceSha256=>RotationTrack.SourceSha256;
    public float FrameRate=>RotationTrack.FrameRate;
    public ushort EndFrame=>RotationTrack.EndFrame;
    public float AuthoredSpeed{get;}
    public IReadOnlyList<LocomotionPositionKey> Positions{get;}
    public bool HasRotation=>RotationTrack.HasRotation;
    public bool HasTranslation=>Positions.Count!=0;
    public CompressedRootMotionMode Mode=>HasRotation?(HasTranslation?CompressedRootMotionMode.Combined:CompressedRootMotionMode.Rotation):
        (HasTranslation?CompressedRootMotionMode.Translation:CompressedRootMotionMode.None);

    private CompressedRootMotion(byte[] source,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();RotationTrack=CompressedRootRotation.Create(source);
        token.ThrowIfCancellationRequested();var clip=CompressedKeyframeDecoder.Decode(source);
        token.ThrowIfCancellationRequested();
        var nodes=clip.UninterpretedSuffix.Where(n=>n.Tag=="LOCT"||n.IsForm("LOCT")).ToArray();
        Require(nodes.Length<=1,"Duplicate root translation channel.");
        if(nodes.Length==0){Positions=Array.AsReadOnly(Array.Empty<LocomotionPositionKey>());return;}
        var node=nodes[0];Require(node.Tag=="LOCT"&&node.FormType is null&&node.Children.Count==0,"Root translation must be a chunk.");
        var raw=node.Payload.Span;Require(raw.Length>=6,"Truncated root translation header.");
        AuthoredSpeed=F32(raw);int count=BinaryPrimitives.ReadInt16LittleEndian(raw[4..]);
        Require(float.IsFinite(AuthoredSpeed)&&AuthoredSpeed>=0&&AuthoredSpeed<=1e6&&count>=2&&raw.Length==6+count*14,"Invalid root translation speed/count/width.");
        var keys=new LocomotionPositionKey[count];
        for(int i=0;i<count;i++)
        {
            if((i&255)==0)token.ThrowIfCancellationRequested();var record=raw[(6+i*14)..];
            ushort frame=BinaryPrimitives.ReadUInt16LittleEndian(record);var value=new Vector3(F32(record[2..]),F32(record[6..]),F32(record[10..]));
            Require(frame<=EndFrame&&(i==0||frame>keys[i-1].Frame)&&Finite(value),"Invalid root translation time/position.");keys[i]=new(frame,value);
        }
        token.ThrowIfCancellationRequested();Positions=Array.AsReadOnly(keys);
    }

    public static CompressedRootMotion Create(ReadOnlySpan<byte> source,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();Require(source.Length<=8*1024*1024,"Root motion source exceeds the byte limit.");
        return new(source.ToArray(),token);
    }

    public RootMotionDelta SampleDelta(double fromFrame,double toFrame,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();CheckFrame(fromFrame);CheckFrame(toFrame);
        var first=HasRotation?RotationTrack.SampleRotation(fromFrame):new(Quaternion.Identity,false);
        var second=HasRotation?RotationTrack.SampleRotation(toFrame):new(Quaternion.Identity,false);
        Quaternion relative=HasRotation?Quaternion.Normalize(second.Rotation*Quaternion.Conjugate(first.Rotation)):Quaternion.Identity;
        Vector3 delta=HasTranslation?Position(toFrame)-Position(fromFrame):Vector3.Zero;
        if(HasRotation&&HasTranslation)delta=CombinedTranslation(second.Rotation,delta);
        token.ThrowIfCancellationRequested();return new(relative,delta,first.Repaired||second.Repaired);
    }

    private Vector3 Position(double frame)
    {
        if(frame<=Positions[0].Frame)return Positions[0].Position;
        if(frame>=Positions[^1].Frame)return Positions[^1].Position;
        int low=0,high=Positions.Count-1;
        while(high-low>1){int mid=(low+high)/2;if(Positions[mid].Frame<=frame)low=mid;else high=mid;}
        var a=Positions[low];var b=Positions[high];float fraction=(float)((frame-a.Frame)/(b.Frame-a.Frame));
        return a.Position+(b.Position-a.Position)*fraction;
    }

    private static Vector3 CombinedTranslation(Quaternion q,Vector3 d)
    {
        // Canonical column-vector quaternion matrix, independently expressed in scalar algebra.
        // Recorded output columns are 2,1,0. System.Numerics matrix storage is not substituted.
        double x=q.X,y=q.Y,z=q.Z,w=q.W;
        double m02=2*(x*z+y*w),m12=2*(y*z-x*w),m22=1-2*(x*x+y*y);
        double m01=2*(x*y-z*w),m11=1-2*(x*x+z*z),m21=2*(y*z+x*w);
        double m00=1-2*(y*y+z*z),m10=2*(x*y+z*w),m20=2*(x*z-y*w);
        return new((float)((m02*d.X+m12*d.Y)+m22*d.Z),(float)((m01*d.X+m11*d.Y)+m21*d.Z),
            (float)((m00*d.X+m10*d.Y)+m20*d.Z));
    }
    private void CheckFrame(double frame)=>Require(double.IsFinite(frame)&&frame>=0&&frame<=EndFrame,"Root motion sample outside clip.");
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs(v.X)<=1e6&&Math.Abs(v.Y)<=1e6&&Math.Abs(v.Z)<=1e6;
    private static float F32(ReadOnlySpan<byte> raw)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(raw));
    private static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
}
