using System.Threading.Tasks;
public partial class Main
{
    // Lead hook: if(args.Contains("--two-creature-world-preview")){await BuildTwoCreatureWorldPreview(args);return;}
    private async Task BuildTwoCreatureWorldPreview(string[] args)
    {var preview=new TwoCreatureWorldPreview();AddChild(preview);await preview.Completion;}
}
