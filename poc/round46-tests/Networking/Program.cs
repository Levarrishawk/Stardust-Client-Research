using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
try
{
    if (args.Length != 4) throw new ArgumentException("asset-root configuration synthetic-fixtures private-receipt");
    var synthetic = SharedCreatureMovementInputScenarios.Run(args[2]);
    string config = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1])));
    if (config != "22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524") throw new InvalidDataException("Configuration identity mismatch.");
    using var text = File.OpenText(args[1]); using var overlay = ConfiguredTreOverlay.Open(args[0], text);
    var original = SharedCreatureMovementInputProfile.Load(overlay.Lookup, "object/creature/player/shared_human_male.iff",
        "E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243");
    File.WriteAllText(args[3], JsonSerializer.Serialize(new { Synthetic = synthetic, Mounts = overlay.MountCount,
        Original = SharedCreatureMovementInputScenarios.Export(original) }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("R46_MOVEMENT_PROFILE_PASS synthetic/original owned profile; runtime values unclaimed");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
