using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record DecodedPackedQuaternion(float X,float Y,float Z,double Radicand,
    ScalarFirstQuaternion Reconstructed,Quaternion Publication,bool Repaired);

/// <summary>Deployed selectors 128..254; explicit finite publication policy for negative radicands.</summary>
public static class PackedQuaternionDecoder
{
    private static readonly int[] SelectorStarts={254,252,248,240,224,192,128};
    public static DecodedPackedQuaternion Decode(PackedAnimationQuaternion packed)
    {
        float x=Component((packed.Value>>21)&2047,packed.XSelector,false);
        float y=Component((packed.Value>>10)&2047,packed.YSelector,false);
        float z=Component(packed.Value&1023,packed.ZSelector,true);
        double radicand=1-((double)x*x+(double)y*y+(double)z*z);
        bool repaired=radicand<0;
        // Original ordinary animated evaluation propagates NaN here. Publication intentionally repairs it.
        float w=(float)Math.Sqrt(Math.Max(0,radicand));
        var reconstructed=new ScalarFirstQuaternion(w,x,y,z);
        var publication=Quaternion.Normalize(new Quaternion(x,y,z,w));
        return new(x,y,z,radicand,reconstructed,publication,repaired);
    }

    private static float Component(uint field,byte selector,bool tenBits)
    {
        if(selector<128||selector==255)throw new InvalidDataException("Unsupported non-deployed quaternion selector.");
        int level=Array.FindIndex(SelectorStarts,start=>selector>=start);
        int ordinal=selector-SelectorStarts[level];
        float width=(float)(2d/((1<<level)+1));
        float basis=(float)((ordinal+1)*(double)width-1);
        float reciprocal=(float)(1d/(tenBits?511:1023));
        float scale=(float)((double)width*reciprocal);
        uint sign=tenBits?512u:1024u;
        double offset=(field&(sign-1))*(double)scale;
        return (float)((field&sign)!=0?basis-offset:basis+offset);
    }
}
