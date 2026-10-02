using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class ZoneSceneProtocolScenarios
{
    // Hand-authored synthetic vectors: flag A5, high-bit IDs, X=1.5 Z=-2.25 Y=3.75.
    public const string SelectHex = "0200768D09B588776655443322F1";
    public const string SceneHex = "0900AEDFE63AA588776655443322F10300412F420000C03F000010C0000070400300545C4308070605040302FE";
    public static void Run()
    {
        byte[] select = Convert.FromHexString(SelectHex), scene = Convert.FromHexString(SceneHex);
        Check(ZoneSceneProtocolCodec.EncodeSelectCharacter(0xF122334455667788).SequenceEqual(select));
        Check(ZoneSceneProtocolCodec.DecodeSelectCharacter(select).ObjectId == 0xF122334455667788);
        CmdStartSceneMessage dto = ZoneSceneProtocolCodec.DecodeCmdStartScene(scene);
        Check(dto == new CmdStartSceneMessage(0xA5, 0xF122334455667788, "A/B", 1.5f, -2.25f, 3.75f, "T\\C", 0xFE02030405060708));
        Array.Fill(scene, (byte)0); // decoded strings/scalars own their data
        Check(dto.Terrain == "A/B" && dto.Template == "T\\C" && dto.RawFlag == 0xA5);
        foreach (bool isScene in new[] { false, true })
        {
            byte[] golden = Convert.FromHexString(isScene ? SceneHex : SelectHex);
            for (int size = 0; size < golden.Length; size++) Reject(golden[..size], isScene);
            Reject([..golden, 0], isScene);
            foreach (int offset in new[] { 0, 2 }) { byte[] bad = (byte[])golden.Clone(); bad[offset] ^= 1; Reject(bad, isScene); }
        }
        byte[] suffix = [..select, 0, 0, 0, 0]; Reject(suffix, false);
        foreach (int offset in new[] { 15, 32 })
        {
            byte[] bad = Convert.FromHexString(SceneHex); bad[offset] = 255; bad[offset + 1] = 255; Reject(bad, true);
            bad = Convert.FromHexString(SceneHex); bad[offset + 2] = 128; Reject(bad, true);
        }
        foreach (int offset in new[] { 20, 24, 28 })
            foreach (uint bits in new[] { 0x7F800000u, 0xFF800000u, 0x7FC00000u })
            { byte[] bad = Convert.FromHexString(SceneHex); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(offset), bits); Reject(bad, true); }
        // All byte flags are preserved; parsing does not require a prior selected object.
        byte[] alternate = Convert.FromHexString(SceneHex); alternate[6] = 255;
        Check(ZoneSceneProtocolCodec.DecodeCmdStartScene(alternate).RawFlag == 255);
        Check(ZoneSceneProtocolCodec.DecodeSelectCharacter(ZoneSceneProtocolCodec.EncodeSelectCharacter(0)).ObjectId == 0);
        byte[] empty = Convert.FromHexString("0900AEDFE63A000000000000000000000000000000000000000000000000000000000000000000");
        Check(ZoneSceneProtocolCodec.DecodeCmdStartScene(empty).Terrain == "");
        // Maximum length strings and ASCII controls remain raw protocol data.
        byte[] max = new byte[39 + 2 * ushort.MaxValue];
        empty.AsSpan(0, 15).CopyTo(max);
        BinaryPrimitives.WriteUInt16LittleEndian(max.AsSpan(15), ushort.MaxValue);
        max.AsSpan(17, ushort.MaxValue).Fill(65);
        int second = 17 + ushort.MaxValue + 12;
        BinaryPrimitives.WriteUInt16LittleEndian(max.AsSpan(second), ushort.MaxValue);
        max.AsSpan(second + 2, ushort.MaxValue).Fill(66);
        CmdStartSceneMessage longest = ZoneSceneProtocolCodec.DecodeCmdStartScene(max);
        Check(longest.Terrain.Length == ushort.MaxValue && longest.Template.Length == ushort.MaxValue);
        byte[] controls = Convert.FromHexString(SceneHex); controls[17] = 0; controls[34] = 127;
        Check(ZoneSceneProtocolCodec.DecodeCmdStartScene(controls).Terrain[0] == '\0');
        Console.WriteLine("R1-NET-01 positive, malformed, flag and owned DTO scenarios passed.");
    }
    private static void Check(bool condition) { if (!condition) throw new Exception("Zone scene assertion failed."); }
    private static void Reject(byte[] bytes, bool scene)
    {
        try { if (scene) ZoneSceneProtocolCodec.DecodeCmdStartScene(bytes); else ZoneSceneProtocolCodec.DecodeSelectCharacter(bytes); }
        catch (InvalidDataException) { return; }
        throw new Exception("Malformed vector accepted.");
    }
}

