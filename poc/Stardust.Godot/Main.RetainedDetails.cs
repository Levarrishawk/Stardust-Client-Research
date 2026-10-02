using System.Threading.Tasks;
public partial class Main
{
    private async Task BuildRetainedDetailPreview(string[] args)
    {
        var preview = new SkeletalRetainedDetailPreview(); AddChild(preview);
        await preview.InitializeAsync(args);
    }
}
