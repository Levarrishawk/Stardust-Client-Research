using System.Threading.Tasks;

public partial class Main
{
    private async Task BuildAnimationEventPreview(string[] args)
    {
        var preview = new AnimationEventDiagnosticPreview(); AddChild(preview);
        await preview.InitializeAsync(args);
    }
}
