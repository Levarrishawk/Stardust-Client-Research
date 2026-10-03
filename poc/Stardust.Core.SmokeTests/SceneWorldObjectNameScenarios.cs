using System.Text.Json;
using Stardust.Core.Protocol;

namespace Stardust.Core.SmokeTests;

/// <summary>Raw independently packed name baseline fixtures; no display/locale or name-delta claim.</summary>
public static class SceneWorldObjectNameScenarios
{
    public static void Run(string peerPath)
    {
        var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(peerPath))!;int assertions=0;
        void Check(bool value,string why){if(!value)throw new Exception(why);assertions++;}
        var limits=new CreatureView3BaselineLimits(1024,1024,1024,128,16384);
        foreach(string key in new[]{"name_first_baseline","name_latest_baseline","name_hidden_baseline"})
        {
            byte[] bytes=Convert.FromHexString(peer[key]);var baseline=CreatureView3BaselineCodec.Decode(bytes,limits);var names=CreatureView3NameState.FromBaseline(baseline);
            Check(CreatureView3BaselineCodec.Encode(baseline,limits).AsSpan().SequenceEqual(bytes),"exact complete baseline roundtrip");
            Check(names.NameFile.CopyBytes().SequenceEqual("diagnostic"u8.ToArray()),"exact diagnostic file bytes");
            Check(ReferenceEquals(names.NameFile,baseline.NameFile)&&ReferenceEquals(names.CustomNameUnits,baseline.CustomNameUnits)&&names.AdditionalDecodedBytes(baseline)==0,"owned immutable baseline aliases");
            bool first=key=="name_first_baseline";
            Check(names.NameSpacer==(first?0xF1234567u:0xF1234568u)&&names.NameKey.CopyBytes().SequenceEqual(first?"alpha"u8.ToArray():"beta"u8.ToArray()),"spacer/key exact");
            Check(names.CustomNameUnits.SequenceEqual(first?Array.Empty<ushort>():"LATEST CAPTION".Select(c=>(ushort)c)),"custom raw UTF16 exact");
            Check(baseline.Visibility==(key=="name_hidden_baseline"?0:1),"hidden latest baseline");
            Array.Clear(bytes);Check(names.NameFile.Length==10,"input mutation cannot alter owned names");
        }
        Console.WriteLine($"PASS: diagnostic name raw fixtures assertions={assertions} baseline_only=true");
    }
}
