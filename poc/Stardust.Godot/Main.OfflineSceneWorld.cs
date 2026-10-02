using System.Threading.Tasks;
public partial class Main
{
    private async Task BuildOfflineSceneWorldPreview(string[] args)
    {
        var preview = new OfflineSceneWorldPreview(); AddChild(preview);
        await preview.InitializeAsync(args);
    }
}
