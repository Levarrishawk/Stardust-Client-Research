using System.Threading.Tasks;
public partial class Main
{
    private async Task BuildSceneCreatureVisualPreview(string[] args)
    {
        var preview = new SceneCreatureVisualPreview(); AddChild(preview);
        await preview.InitializeAsync(args);
    }
}
