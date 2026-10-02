using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
public partial class Main
{
    private async Task BuildReplicatedWorldPreview(string[] args)
    {
        byte[]? colors=null;int at=Array.IndexOf(args,"--palette-profile");
        if(at>=0)
        {
            if(args.Count(v=>v=="--palette-profile")!=1 || at+1>=args.Length)throw new ArgumentException("One palette profile path required.");
            var file=new FileInfo(args[at+1]);if(!file.Exists || file.Length is <1 or >65536)throw new ArgumentException("Palette profile must contain 1..65536 bytes.");
            colors=File.ReadAllBytes(file.FullName);if(colors.Length is <1 or >65536)throw new ArgumentException("Palette profile byte budget.");
        }
        var preview=new OfflineReplicatedWorldPreview();AddChild(preview);await preview.InitializeAsync(args,colors);
    }
}
