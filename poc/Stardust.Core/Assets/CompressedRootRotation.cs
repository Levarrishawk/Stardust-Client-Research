using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public readonly record struct RootRotationSample(Quaternion Rotation,bool Repaired);

/// <summary>Owned absolute top-level QCHN preview curve; no relative motion or actor authority.</summary>
public sealed class CompressedRootRotation
{
    private readonly Quaternion[] values;
    private readonly bool[] repairs;
    public string SourceSha256{get;}
    public float FrameRate{get;}
    public ushort EndFrame{get;}
    public bool HasRotation=>Keys.Count!=0;
    public byte XSelector{get;}
    public byte YSelector{get;}
    public byte ZSelector{get;}
    public IReadOnlyList<CompressedRotationKey> Keys{get;}
    public int RepairedPackedValues{get;}

    private CompressedRootRotation(byte[] source)
    {
        var clip=CompressedKeyframeDecoder.Decode(source);
        SourceSha256=Convert.ToHexString(SHA256.HashData(source));FrameRate=clip.FrameRate;EndFrame=clip.EndFrame;
        var nodes=clip.UninterpretedSuffix.Where(n=>n.Tag=="QCHN"||n.IsForm("QCHN")).ToArray();
        Require(nodes.Length<=1,"Duplicate root rotation channel.");
        if(nodes.Length==0){Keys=Array.AsReadOnly(Array.Empty<CompressedRotationKey>());values=Array.Empty<Quaternion>();repairs=Array.Empty<bool>();return;}
        var node=nodes[0];Require(node.Tag=="QCHN"&&node.FormType is null&&node.Children.Count==0,"Root rotation must be a top-level chunk.");
        var raw=node.Payload.Span;Require(raw.Length>=5,"Truncated root rotation header.");
        int count=BinaryPrimitives.ReadUInt16LittleEndian(raw);
        Require(count>=2&&raw.Length==5+count*6,"Invalid root rotation count/width.");
        XSelector=raw[2];YSelector=raw[3];ZSelector=raw[4];
        var keys=new CompressedRotationKey[count];values=new Quaternion[count];repairs=new bool[count];
        for(int i=0;i<count;i++)
        {
            var record=raw[(5+i*6)..];ushort frame=BinaryPrimitives.ReadUInt16LittleEndian(record);
            Require(frame<=EndFrame&&(i==0||frame>keys[i-1].Frame),"Root rotation times must increase within clip end.");
            uint bits=BinaryPrimitives.ReadUInt32LittleEndian(record[2..]);keys[i]=new(frame,bits);
            var decoded=PackedQuaternionDecoder.Decode(new(XSelector,YSelector,ZSelector,bits));
            values[i]=decoded.Publication;repairs[i]=decoded.Repaired;
        }
        Keys=Array.AsReadOnly(keys);RepairedPackedValues=repairs.Count(r=>r);
    }

    public static CompressedRootRotation Create(ReadOnlySpan<byte> source)
    {
        Require(source.Length<=8*1024*1024,"Root rotation source exceeds the byte limit.");
        return new(source.ToArray());
    }

    public RootRotationSample SampleRotation(double frame)
    {
        Require(double.IsFinite(frame)&&frame>=0&&frame<=EndFrame,"Root rotation sample outside clip.");
        Require(HasRotation,"No top-level QCHN rotation track.");
        int last=Keys.Count-1;
        if(frame<=Keys[0].Frame)return new(values[0],repairs[0]);
        if(frame>=Keys[last].Frame)return new(values[last],repairs[last]);
        int low=0,high=last;
        while(high-low>1){int mid=(low+high)/2;if(Keys[mid].Frame<=frame)low=mid;else high=mid;}
        if(frame==Keys[low].Frame)return new(values[low],repairs[low]);
        float fraction=(float)((frame-Keys[low].Frame)/(Keys[high].Frame-Keys[low].Frame));
        return new(Quaternion.Normalize(Quaternion.Slerp(values[low],values[high],fraction)),repairs[low]||repairs[high]);
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
}
