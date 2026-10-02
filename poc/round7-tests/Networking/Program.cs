using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
CreatureView3ScalarDeltaScenarios.Run();
if (args.Length == 2 && args[0] == "--export")
{
    Directory.CreateDirectory(args[1]);
    var plan=CreatureView3ScalarDeltaCodec.Decode(Convert.FromHexString(CreatureView3ScalarDeltaScenarios.GoldenHex),49,8);
    File.WriteAllBytes(Path.Combine(args[1],"delta.bin"),CreatureView3ScalarDeltaCodec.Encode(plan,49,8));
    byte[] high=Convert.FromHexString(CreatureView3ScalarDeltaScenarios.GoldenHex); high[23]=255; high[24]=255;
    File.WriteAllBytes(Path.Combine(args[1],"delta-count65535.bin"),CreatureView3ScalarDeltaCodec.Encode(CreatureView3ScalarDeltaCodec.Decode(high,49,8),49,8));
    object Update(CreatureView3ScalarUpdate u) => new { u.Index, RawBits=u switch {CreaturePostureUpdate v=>(ulong)v.Value,CreatureRankUpdate v=>v.Value,CreatureLinkUpdate v=>v.Value,CreatureHeightUpdate v=>BitConverter.SingleToUInt32Bits(v.Value),CreatureShockUpdate v=>v.Value,CreatureStatesUpdate v=>v.Value,_=>throw new Exception()} };
    object State(CreatureView3ScalarState s) => new {s.Posture,s.Rank,s.LinkId,HeightBits=BitConverter.SingleToUInt32Bits(s.Height),s.Shock,s.States};
    File.WriteAllText(Path.Combine(args[1],"plan-state.json"),JsonSerializer.Serialize(new {plan.ObjectId,plan.DeclaredCount,Updates=plan.Updates.Select(Update),Initial=State(CreatureView3ScalarDeltaScenarios.Initial()),Applied=State(plan.Apply(CreatureView3ScalarDeltaScenarios.Initial()))}));
}
else if(args.Length!=0) throw new ArgumentException("Usage: [--export directory]");

