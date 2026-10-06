using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Offline;

/// <summary>Explicit offline entry point; user input and restart button drive each fresh preview.</summary>
public partial class PersistentOfflinePlaygroundLauncher : Node
{
    public PersistentOfflinePlayground? Playground { get; private set; }
    public override void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            const string key = "--offline-restart-arguments";
            const string scenarioKey = "--offline-scenario";
            int scenarioIndex = Array.IndexOf(args, scenarioKey);
            if (scenarioIndex >= 0)
            {
                if (args.Length != 2 || scenarioIndex != 0 || args.Count(a => a == scenarioKey) != 1)
                    throw new ArgumentException("One explicit offline scenario path required, without argument overrides");
                var path = LocalScenarioPath(args[scenarioIndex+1]);
                using var scenarioStream = File.OpenRead(path);
                if (scenarioStream.Length is <=0 or >262144)
                    throw new InvalidDataException("Bounded offline scenario file required");
                var bytes = new byte[checked((int)scenarioStream.Length)];
                scenarioStream.ReadExactly(bytes);
                var scenario = OfflineScenarioManifest.Parse(bytes, Path.GetDirectoryName(path)!);
                var scenarioShell = new PersistentOfflinePlayground();
                scenarioShell.Configure(scenario.ArgumentsForRun, _ => new ScenePumpedWorldPreview(), scenario.RunCount);
                Playground = scenarioShell;
                AddChild(scenarioShell);
                return;
            }
            int index = Array.IndexOf(args, key);
            if (index < 0 || index+1 >= args.Length || args.Count(a => a == key) != 1)
                throw new ArgumentException("One explicit offline restart argument file required");
            using var stream = File.OpenRead(args[index+1]);
            if (stream.Length is <=0 or >262144) throw new InvalidDataException("Bounded offline argument file required");
            var runs = JsonSerializer.Deserialize<string[][]>(stream, new JsonSerializerOptions { MaxDepth=3 });
            if (runs is null || runs.Length is <1 or >32 || runs.Any(run => run is null || run.Length is <2 or >256 ||
                run.Any(value => value is null || value.Length >4096)))
                throw new InvalidDataException("One to32 bounded explicit run argument sets required");
            var shell = new PersistentOfflinePlayground();
            shell.Configure(run => runs[run-1], _ => new ScenePumpedWorldPreview(), runs.Length);
            Playground = shell;
            AddChild(shell);
        }
        catch (Exception error) { GD.PushError("Offline playground launcher: " + error); GetTree().Quit(1); }
    }

    private static string LocalScenarioPath(string value)
    {
        int colon=value.IndexOf(':');
        if(value.Length is 0 or >4096 || value.Contains('\0') ||
            value.StartsWith("//",StringComparison.Ordinal) || value.StartsWith(@"\\",StringComparison.Ordinal) ||
            colon>=0 && !(colon==1 && char.IsAsciiLetter(value[0]) && value.Length>2 &&
                (value[2]=='/' || value[2]=='\\') && value.LastIndexOf(':')==colon))
            throw new ArgumentException("Bounded local scenario path required");
        string result=Path.GetFullPath(value);
        if(result.Length>4096)throw new ArgumentException("Resolved scenario path exceeds limit");
        return result;
    }
}
