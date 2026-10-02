using System.Threading.Tasks;
public partial class Main
{
    private async Task BuildStaticSwitchTexturePreview(string[] args)
    {
        var preview = new StaticSwitchTexturePreview(); AddChild(preview);
        await preview.InitializeAsync(args);
    }
}
