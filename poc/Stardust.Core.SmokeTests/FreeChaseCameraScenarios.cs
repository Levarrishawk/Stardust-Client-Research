using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class FreeChaseCameraScenarios
{
    internal static void Run()
    {
        byte[] source=Fixture();var profile=FreeChaseCameraProfileDecoder.Decode(source);Array.Clear(source);
        if(!profile.ZoomSettings.SequenceEqual(new[]{0f,2,4})||profile.SelectSetting(-1)!=1||profile.SelectSetting(99)!=1||profile.SelectSetting(2)!=2||profile.ZoomDistance(2,2)!=8)throw new Exception("Owned camera zoom selection mismatch.");
        if(profile.TargetHeight(8,new HashSet<int>{19},2)!=2.2f||profile.TargetHeight(8,new HashSet<int>(),2)!=1.4f||profile.TargetHeight(0,new HashSet<int>(),2)!=3.4f||profile.TargetHeight(8,new HashSet<int>{19,2},2)!=1)throw new Exception("Camera state/posture/default precedence mismatch.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        byte[] valid=Fixture();for(int i=0;i<valid.Length;i++)Reject(()=>FreeChaseCameraProfileDecoder.Decode(valid.AsSpan(0,i)));
        Reject(()=>FreeChaseCameraProfileDecoder.Decode(Fixture(version:"0001")));Reject(()=>FreeChaseCameraProfileDecoder.Decode(Fixture(zoom:new[]{0f,0f})));Reject(()=>FreeChaseCameraProfileDecoder.Decode(Fixture(zoom:new[]{float.NaN})));Reject(()=>FreeChaseCameraProfileDecoder.Decode(Fixture(posture:15)));Reject(()=>FreeChaseCameraProfileDecoder.Decode(Fixture(state:34)));Reject(()=>FreeChaseCameraProfileDecoder.Decode(Fixture(duplicate:true)));Reject(()=>FreeChaseCameraProfileDecoder.Decode(Fixture(extra:true)));
        Reject(()=>profile.ZoomDistance(-1,1));Reject(()=>profile.ZoomDistance(0,float.NaN));Reject(()=>profile.TargetHeight(0,new HashSet<int>{34},1));Reject(()=>profile.TargetHeight(0,new HashSet<int>(),0));
        Console.WriteLine("PASS: owned FCHS camera settings/state-posture-default precedence, middle-setting fallback and malformed width/version/count/index/float/duplicate gates");
    }
    private static byte[] Fixture(string version="0000",float[]? zoom=null,int posture=8,int state=19,bool duplicate=false,bool extra=false)
    {
        var records=new List<byte>();foreach(var entry in new[]{(Posture:posture,State:state,Value:1.1f),(Posture:8,State:duplicate?state:2,Value:.5f)}){records.AddRange(BitConverter.GetBytes(entry.Posture));records.AddRange(BitConverter.GetBytes(entry.State));records.AddRange(BitConverter.GetBytes(entry.Value));}
        var parts=new List<byte[]>{MeshScenarios.Chunk("ZOOM",(zoom??new[]{0f,2,4}).SelectMany(BitConverter.GetBytes).ToArray()),MeshScenarios.Chunk("FRST",BitConverter.GetBytes(.5f)),MeshScenarios.Chunk("DEFH",BitConverter.GetBytes(1.7f)),MeshScenarios.Chunk("PSHM",records.ToArray()),MeshScenarios.Chunk("PSTM",BitConverter.GetBytes(8).Concat(BitConverter.GetBytes(.7f)).ToArray())};
        if(extra)parts.Add(MeshScenarios.Chunk("MORE",Array.Empty<byte>()));return MeshScenarios.Form("FCHS",MeshScenarios.Form(version,parts.ToArray()));
    }
    internal static void Probe(string[] args)
    {
        if(args.Length!=3)throw new InvalidDataException("Expected owned camera profile and exact SHA256.");byte[] bytes=File.ReadAllBytes(args[1]);string hash=Convert.ToHexString(SHA256.HashData(bytes));if(hash!=args[2])throw new InvalidDataException("Camera profile hash mismatch.");var profile=FreeChaseCameraProfileDecoder.Decode(bytes);
        Dump(profile,hash,bytes.Length);
    }
    internal static void Dump(FreeChaseCameraProfile profile,string hash,int bytes)
    {
        Console.WriteLine("free_chase_profile="+JsonSerializer.Serialize(new{sha256=hash,bytes,profile.ZoomSettings,profile.FirstPersonThreshold,profile.DefaultHeight,profile.StateHeights,profile.PostureHeights,invalid_setting_fallback=profile.SelectSetting(-1)}));
        foreach(int posture in Enumerable.Range(0,15))foreach(int state in new[]{-1,19,27})foreach(float metric in new[]{.5f,1f,2f})Console.WriteLine("free_chase_height="+JsonSerializer.Serialize(new{posture,state,metric,height=profile.TargetHeight(posture,state<0?new HashSet<int>():new HashSet<int>{state},metric)}));
    }
}
