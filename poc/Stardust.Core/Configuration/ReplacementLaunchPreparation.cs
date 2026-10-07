using Stardust.Core.Assets;

namespace Stardust.Core.Configuration;

/// <summary>Owns installation archives. Caller must drain all borrowing jobs before disposal.</summary>
public sealed class ReplacementLaunchPreparation : IDisposable
{
    private readonly OriginalInstallationInputs inputs;
    private bool disposed;
    public OriginalInstallationInputs Inputs
    {
        get { ObjectDisposedException.ThrowIf(disposed, this); return inputs; }
    }
    public InstalledCreaturePreparation Creature { get; }
    public ReplacementLaunchProfile Profile { get; }

    private ReplacementLaunchPreparation(OriginalInstallationInputs inputs, InstalledCreaturePreparation creature,
        ReplacementLaunchProfile profile)
    { this.inputs = inputs; Creature = creature; Profile = profile; }

    public static ReplacementLaunchPreparation Open(ReplacementLaunchProfile profile)
    {
        ReplacementLaunchProfile copy = ReplacementLaunchProfileCodec.CopyValidated(profile);
        OriginalInstallationInputs inputs = OriginalInstallationInputs.Open(copy.AssetRoot, copy.ConfigurationPath, [copy.ObjectPath]);
        try
        {
            inputs.RequireAllPresent();
            InstalledCreaturePreparation creature = InstalledCreaturePreparation.Create(inputs, copy.ObjectPath, copy.Idle, copy.Walk, copy.Run);
            return new(inputs, creature, copy);
        }
        catch { inputs.Dispose(); throw; }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        inputs.Dispose();
    }
}
