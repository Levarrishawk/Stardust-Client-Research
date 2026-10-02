using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public readonly record struct CameraPostureStateHeight(int Posture,int State,float Multiplier);
public readonly record struct CameraPostureHeight(int Posture,float Multiplier);
public sealed record FreeChaseCameraProfile(IReadOnlyList<float> ZoomSettings,float FirstPersonThreshold,float DefaultHeight,
    IReadOnlyList<CameraPostureStateHeight> StateHeights,IReadOnlyList<CameraPostureHeight> PostureHeights)
{
    public int SelectSetting(int persisted)=>persisted>=0&&persisted<ZoomSettings.Count?persisted:ZoomSettings.Count/2;
    public float ZoomDistance(int setting,float maximumZoomMultiplier)
    {
        if(setting<0||setting>=ZoomSettings.Count||!float.IsFinite(maximumZoomMultiplier)||maximumZoomMultiplier<=0||maximumZoomMultiplier>100)throw new InvalidDataException("Invalid explicit camera zoom selection/multiplier.");
        return ZoomSettings[setting]*maximumZoomMultiplier;
    }
    public float TargetHeight(int posture,IReadOnlySet<int> activeStates,float appearanceMetric)
    {
        ArgumentNullException.ThrowIfNull(activeStates);
        if(posture is <0 or >14||activeStates.Count>34||activeStates.Any(s=>s is <0 or >33)||!float.IsFinite(appearanceMetric)||appearanceMetric<=0||appearanceMetric>100)throw new InvalidDataException("Invalid camera target posture/state/appearance metric.");
        var state=StateHeights.Where(h=>h.Posture==posture&&activeStates.Contains(h.State)).OrderBy(h=>h.State).ToArray();
        if(state.Length!=0)return state[0].Multiplier*appearanceMetric;
        var postureHeight=PostureHeights.Where(h=>h.Posture==posture).ToArray();return (postureHeight.Length!=0?postureHeight[0].Multiplier:DefaultHeight)*appearanceMetric;
    }
}

/// <summary>Owned FCHS 0000 profile. Parser limits are explicit; original invalid-index skipping is not reproduced.</summary>
public static class FreeChaseCameraProfileDecoder
{
    public static FreeChaseCameraProfile Decode(ReadOnlySpan<byte> bytes)
    {
        var root=IffDocument.Parse(bytes,new(MaximumBytes:65536,MaximumNodes:16,MaximumDepth:4)).Root;
        Require(root.IsForm("FCHS")&&root.Children.Count==1&&root.Children[0].IsForm("0000"),"Unsupported free-chase profile envelope/version.");
        var parts=root.Children[0].Children;string[] tags={"ZOOM","FRST","DEFH","PSHM","PSTM"};Require(parts.Count==tags.Length,"Free-chase profile chunk count.");
        for(int i=0;i<tags.Length;i++)Require(parts[i].Tag==tags[i]&&parts[i].FormType is null,"Free-chase profile chunk order/type.");
        var zoomRaw=parts[0].Payload.Span;Require(zoomRaw.Length>=4&&zoomRaw.Length<=256&&zoomRaw.Length%4==0,"Camera zoom count/width.");
        var zoom=new float[zoomRaw.Length/4];for(int i=0;i<zoom.Length;i++){zoom[i]=Finite(F32(zoomRaw[(i*4)..]));Require(zoom[i]>=0&&(i==0||zoom[i]>zoom[i-1]),"Camera zoom settings must increase.");}
        Require(parts[1].Payload.Length==4&&parts[2].Payload.Length==4,"Camera scalar width.");float first=Finite(F32(parts[1].Payload.Span)),height=Finite(F32(parts[2].Payload.Span));Require(first>=0,"Camera first-person threshold must be nonnegative.");
        var stateRaw=parts[3].Payload.Span;Require(stateRaw.Length%12==0&&stateRaw.Length/12<=512,"Camera posture-state record width/count.");
        var states=new CameraPostureStateHeight[stateRaw.Length/12];var stateKeys=new HashSet<(int,int)>();
        for(int i=0;i<states.Length;i++)
        {
            var raw=stateRaw[(i*12)..];int posture=I32(raw),state=I32(raw[4..]);Require(posture is >=0 and <=14&&state is >=0 and <=33&&stateKeys.Add((posture,state)),"Invalid/duplicate camera posture-state key.");states[i]=new(posture,state,Finite(F32(raw[8..])));
        }
        var postureRaw=parts[4].Payload.Span;Require(postureRaw.Length%8==0&&postureRaw.Length/8<=15,"Camera posture record width/count.");
        var postures=new CameraPostureHeight[postureRaw.Length/8];var postureKeys=new HashSet<int>();
        for(int i=0;i<postures.Length;i++){var raw=postureRaw[(i*8)..];int posture=I32(raw);Require(posture is >=0 and <=14&&postureKeys.Add(posture),"Invalid/duplicate camera posture key.");postures[i]=new(posture,Finite(F32(raw[4..])));}
        return new(Array.AsReadOnly(zoom),first,height,Array.AsReadOnly(states),Array.AsReadOnly(postures));
    }
    private static int I32(ReadOnlySpan<byte> bytes)=>BinaryPrimitives.ReadInt32LittleEndian(bytes);
    private static float F32(ReadOnlySpan<byte> bytes)=>BitConverter.Int32BitsToSingle(I32(bytes));
    private static float Finite(float value){Require(float.IsFinite(value)&&Math.Abs(value)<=100,"Invalid camera profile value.");return value;}
    private static void Require(bool value,string message)=>SkeletalAppearanceDecoder.Require(value,message);
}
