using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Configuration;

/// <summary>Packaged offline entry over the existing installed-resource and skeletal host paths.</summary>
public static class OfflineCharacterLaunch
{
    public static async Task<string[]> ExpandAsync(string[] arguments)
    {
        bool packaged = OS.HasFeature("stardust_offline_character");
        int index = Array.IndexOf(arguments, "--offline-character-profile");
        if (!packaged && index < 0) return arguments;
        var extras = new List<string>();
        string profilePath = Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, "character-launch.json");
        for (int i = 0; i < arguments.Length; i++)
        {
            switch (arguments[i])
            {
                case "--offline-character-profile":
                    if (i != index || i + 1 >= arguments.Length) throw new ArgumentException("One offline profile required.");
                    profilePath = arguments[++i];
                    break;
                case "--capture": case "--offline-animation-proof":
                    if (extras.Contains(arguments[i]) || i + 1 >= arguments.Length) throw new ArgumentException("One output option required.");
                    extras.Add(arguments[i]); extras.Add(arguments[++i]);
                    break;
                case "--asset-self-test": case "--require-d3d12": case "--cancel-creature-load":
                    if (extras.Contains(arguments[i])) throw new ArgumentException("Duplicate offline option.");
                    extras.Add(arguments[i]); break;
                default: throw new ArgumentException("Unsupported packaged offline option.");
            }
        }
        if (!Path.IsPathFullyQualified(profilePath) || new FileInfo(profilePath).Length is < 1 or > 65536)
            throw new InvalidDataException("Fully qualified bounded offline profile required.");
        ReplacementLaunchProfile profile = ReplacementLaunchProfileCodec.Decode(File.ReadAllBytes(profilePath));
        // Preparation owns/closes archives before the existing cancellation-aware visual owner opens its closure.
        string[] selected = await Task.Run(() =>
        {
            using var preparation = ReplacementLaunchPreparation.Open(profile);
            var catalog = preparation.Creature.Catalog.Entries.Values.Single();
            var idle = preparation.Creature.Options.Idle;
            return new[] { "--asset-root", profile.AssetRoot, "--asset-config", profile.ConfigurationPath,
                "--asset-member", profile.ObjectPath, "--asset-sha256", catalog.ObjectSha256,
                "--asset-creature", "--creature-appearance-sha256", catalog.AppearanceSha256,
                "--creature-animation-table", idle.TablePath, "--creature-animation-name", idle.LogicalName,
                "--creature-animation-sha256", idle.ClipSha256, "--creature-animation-frame", "0",
                "--creature-animation-play", "--creature-material-defaults", "--creature-palette-defaults",
                "--creature-albedo-factors", "--creature-normal-diffuse", "--creature-specular" };
        });
        if (DisplayServer.GetName() != "headless")
        { if (!extras.Contains("--require-d3d12")) extras.Add("--require-d3d12"); }
        else if (!extras.Contains("--asset-self-test") && !extras.Contains("--cancel-creature-load"))
            throw new InvalidOperationException("Headless packaged entry requires an explicit validation mode.");
        GD.Print("offline_character_entry=true synthetic_login=true live_transport=false");
        return selected.Concat(extras).ToArray();
    }
}
