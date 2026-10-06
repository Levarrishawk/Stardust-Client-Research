using System.Numerics;

namespace Stardust.Core.Session;

/// <summary>Historical modern destination decision, containing only owned value fields.</summary>
public sealed record TerrainOfflineDestinationReceipt(Vector3 ProposedPosition, Vector3 AcceptedVelocity,
    bool HeldAtMissingDestination);
