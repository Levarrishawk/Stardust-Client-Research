using System.Threading.Tasks;
public partial class Main
{
    private async Task BuildAsyncSceneCreaturePreview(string[] args)
    {
        var preview = new AsyncSceneCreaturePreview(); AddChild(preview);
        await preview.InitializeAsync(args);
    }
}
