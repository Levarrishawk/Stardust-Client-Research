using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public readonly record struct PackedAnimationQuaternion(byte XSelector,byte YSelector,byte ZSelector,uint Value);
public readonly record struct CompressedRotationKey(ushort Frame,uint Value);
public sealed record CompressedRotationChannel(byte XSelector,byte YSelector,byte ZSelector,IReadOnlyList<CompressedRotationKey> Keys);
public readonly record struct AnimationFloatKey(ushort Frame,float Value);
public sealed record AnimationFloatChannel(IReadOnlyList<AnimationFloatKey> Keys);
public sealed record CompressedAnimationTransform(string Name,bool AnimatedRotation,ushort RotationIndex,byte TranslationMask,
    ushort TranslationX,ushort TranslationY,ushort TranslationZ);
public sealed record CompressedKeyframeDescription(float FrameRate,ushort EndFrame,IReadOnlyList<CompressedAnimationTransform> Transforms,
    IReadOnlyList<CompressedRotationChannel> AnimatedRotations,IReadOnlyList<PackedAnimationQuaternion> StaticRotations,
    IReadOnlyList<AnimationFloatChannel> AnimatedTranslations,IReadOnlyList<float> StaticTranslations,IReadOnlyList<IffNode> UninterpretedSuffix);

/// <summary>Owned CKAT 0001 channel structure; packed quaternions and optional event/root suffix are not evaluated.</summary>
public static class CompressedKeyframeDecoder
{
    public static CompressedKeyframeDescription Decode(ReadOnlySpan<byte> source)=>Create(IffDocument.Parse(source,new(MaximumBytes:8*1024*1024,MaximumNodes:100000,MaximumDepth:16)).Root);
    public static CompressedKeyframeDescription Create(IffNode root)
    {
        ArgumentNullException.ThrowIfNull(root);Require(root.IsForm("CKAT")&&root.Children.Count==1&&root.Children[0].IsForm("0001"),"Unsupported compressed keyframe envelope/version.");
        var parts=root.Children[0].Children;Require(parts.Count>=2&&parts[0].Tag=="INFO"&&parts[0].Payload.Length==16&&parts[1].IsForm("XFRM"),"Invalid compressed clip prefix.");
        var info=parts[0].Payload.Span;float rate=F32(info);Require(float.IsFinite(rate)&&rate>0&&rate<=1000,"Invalid clip frame rate.");
        int[] counts=new int[6];for(int i=0;i<6;i++){counts[i]=BinaryPrimitives.ReadInt16LittleEndian(info[(4+i*2)..]);Require(counts[i]>=0,"Negative compressed clip count.");}
        Require(counts[0]>0&&counts[1]<=2048&&counts.Skip(2).All(c=>c<=4096),"Compressed clip count budget.");ushort end=(ushort)counts[0];
        var transformNodes=parts[1].Children;Require(transformNodes.Count==counts[1],"Compressed transform count mismatch.");
        var transforms=new CompressedAnimationTransform[counts[1]];var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for(int i=0;i<transforms.Length;i++)
        {
            var node=transformNodes[i];Require(node.Tag=="XFIN","Invalid transform record tag.");var raw=node.Payload.Span;int stop=raw.IndexOf((byte)0);
            Require(stop>0&&raw.Length==stop+11,"Invalid transform record width.");string name=SkeletalAppearanceDecoder.Strings(raw[..(stop+1)],1)[0];Require(names.Add(name),"Ambiguous animation transform name.");
            var fields=raw[(stop+1)..];Require(fields[0]<=1&&(fields[3]&~56)==0,"Invalid animation transform flags.");
            transforms[i]=new(name,fields[0]!=0,U16(fields[1..]),fields[3],U16(fields[4..]),U16(fields[6..]),U16(fields[8..]));
        }
        int next=2,totalKeys=0;
        IffNode? Optional(string tag,bool form)
        {if(next>=parts.Count||!(form?parts[next].IsForm(tag):parts[next].Tag==tag))return null;return parts[next++];}
        var arot=Optional("AROT",true);Require((arot?.Children.Count??0)==counts[2],"Animated rotation count mismatch.");
        var rotations=new CompressedRotationChannel[counts[2]];
        for(int i=0;i<rotations.Length;i++)
        {
            var node=arot!.Children[i];Require(node.Tag=="QCHN","Invalid animated quaternion tag.");var raw=node.Payload.Span;Require(raw.Length>=5,"Truncated animated quaternion channel.");
            int count=U16(raw);Require(count>=2&&raw.Length==5+count*6,"Invalid quaternion key count/width.");Budget(count);
            var keys=new CompressedRotationKey[count];for(int key=0;key<count;key++){var value=raw[(5+key*6)..];keys[key]=new(U16(value),BinaryPrimitives.ReadUInt32LittleEndian(value[2..]));Time(keys[key].Frame,key==0?-1:keys[key-1].Frame);}
            rotations[i]=new(raw[2],raw[3],raw[4],Array.AsReadOnly(keys));
        }
        var srot=Optional("SROT",false);Require((srot?.Payload.Length??0)==counts[3]*7,"Static quaternion width mismatch.");var staticRotations=new PackedAnimationQuaternion[counts[3]];
        for(int i=0;i<staticRotations.Length;i++){var raw=srot!.Payload.Span[(i*7)..];staticRotations[i]=new(raw[0],raw[1],raw[2],BinaryPrimitives.ReadUInt32LittleEndian(raw[3..]));}
        var atrn=Optional("ATRN",true);Require((atrn?.Children.Count??0)==counts[4],"Animated translation count mismatch.");var translations=new AnimationFloatChannel[counts[4]];
        for(int i=0;i<translations.Length;i++)
        {
            var node=atrn!.Children[i];Require(node.Tag=="CHNL","Invalid animated translation tag.");var raw=node.Payload.Span;Require(raw.Length>=2,"Truncated translation channel.");int count=U16(raw);
            Require(count>=2&&raw.Length==2+count*6,"Invalid translation key count/width.");Budget(count);var keys=new AnimationFloatKey[count];
            for(int key=0;key<count;key++){var value=raw[(2+key*6)..];keys[key]=new(U16(value),Finite(F32(value[2..])));Time(keys[key].Frame,key==0?-1:keys[key-1].Frame);}
            translations[i]=new(Array.AsReadOnly(keys));
        }
        var strn=Optional("STRN",false);Require((strn?.Payload.Length??0)==counts[5]*4,"Static translation width mismatch.");var staticTranslations=new float[counts[5]];
        for(int i=0;i<staticTranslations.Length;i++)staticTranslations[i]=Finite(F32(strn!.Payload.Span[(i*4)..]));
        foreach(var transform in transforms)
        {
            Require(transform.RotationIndex<(transform.AnimatedRotation?rotations.Length:staticRotations.Length),"Animation rotation index out of range.");
            ushort[] indices={transform.TranslationX,transform.TranslationY,transform.TranslationZ};for(int axis=0;axis<3;axis++)Require(indices[axis]<((transform.TranslationMask&(8<<axis))!=0?translations.Length:staticTranslations.Length),"Animation translation index out of range.");
        }
        string[] suffix={"MSGS","LOCT","QCHN"};int previous=-1;
        foreach(var node in parts.Skip(next)){int order=Array.IndexOf(suffix,node.FormType??node.Tag);Require(order>previous,"Unsupported/duplicate/out-of-order compressed clip suffix.");previous=order;}
        return new(rate,end,Array.AsReadOnly(transforms),Array.AsReadOnly(rotations),Array.AsReadOnly(staticRotations),Array.AsReadOnly(translations),Array.AsReadOnly(staticTranslations),Array.AsReadOnly(parts.Skip(next).ToArray()));
        void Budget(int count){totalKeys=checked(totalKeys+count);Require(totalKeys<=262144,"Compressed clip key budget.");}
        void Time(int frame,int previous){Require(frame>previous&&frame<=end,"Animation key times must increase within clip end.");}
    }
    private static ushort U16(ReadOnlySpan<byte> raw)=>BinaryPrimitives.ReadUInt16LittleEndian(raw);
    private static float F32(ReadOnlySpan<byte> raw)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(raw));
    private static float Finite(float value){Require(float.IsFinite(value)&&Math.Abs(value)<=1e6,"Invalid animation translation value.");return value;}
    private static void Require(bool value,string message)=>SkeletalAppearanceDecoder.Require(value,message);
}
