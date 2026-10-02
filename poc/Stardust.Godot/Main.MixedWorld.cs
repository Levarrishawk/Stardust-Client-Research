using System.Threading.Tasks;
public partial class Main
{
    private async Task BuildMixedWorldPreview()
    { var preview = new SceneMixedWorldPreview(); AddChild(preview); await preview.Completion; }
}
