using System.Threading.Tasks;
public partial class Main
{
    private async Task BuildCreatureStatePreview(string[] args)
    {
        var preview = new CreatureStatePreview(); AddChild(preview);
        await preview.InitializeAsync(args);
    }
}
