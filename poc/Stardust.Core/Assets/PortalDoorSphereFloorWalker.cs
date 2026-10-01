using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Precise source/target floor-path adapter for directional permission barriers for an explicit offset sphere and captured physical leaves. Borrows controllers; never advances them.</summary>
public sealed class PortalDoorSphereFloorWalker
{
    private readonly PortalFloorSphereBoxWalker query;
    private readonly Dictionary<PortalFloorLink,PortalBarrierCollider> barriers=new();
    private readonly Dictionary<PortalFloorLink,SingleLeafDoorRuntime> leaves=new();
    private readonly bool allowSeparatingContacts;
    public PortalDoorSphereFloorWalker(PortalLayoutDescription layout,IReadOnlyDictionary<int,FloorMeshDescription> floors,
        IReadOnlyDictionary<int,AppearanceTransform>? poses=null,
        IReadOnlyDictionary<(int Cell,int Portal),SingleLeafDoorRuntime>? doorLeaves=null,bool allowSeparatingContacts=false)
    {
        this.allowSeparatingContacts=allowSeparatingContacts;query=new(layout,floors,poses);
        foreach(var link in query.Links)
        {
            var reference=layout.Cells[link.SourceCell].Portals[link.SourcePortal];
            if(PortalDoorFlags.FromReference(layout.Version,reference).CanCreateDoor)
            {
                var local=PortalBarrierCollider.FromReference(layout,reference);
                var owner=poses is not null && poses.TryGetValue(link.SourceCell,out var supplied)?supplied:AppearanceTransform.Identity;
                barriers.Add(link,new(local.Minimum,local.Maximum,owner.Compose(local.Pose)));
            }
            if(doorLeaves is not null && doorLeaves.TryGetValue((link.SourceCell,link.SourcePortal),out var leaf))
            {
                if(!leaf.AcceptsCell(link.SourceCell) || !barriers.ContainsKey(link))throw new InvalidDataException("Precise leaf owner/eligibility mismatch.");
                leaves.Add(link,leaf);
            }
        }
        if(doorLeaves is not null && doorLeaves.Keys.Any(key=>!leaves.Keys.Any(l=>l.SourceCell==key.Cell && l.SourcePortal==key.Portal)))
            throw new InvalidDataException("Precise door leaf lacks a source floor link.");
    }
    public PortalBarrierFloorResult Walk(CellFloorLocator start,Vector3 desired,Vector3 centerOffset,float radius,
        Func<PortalFloorLink,PortalTransferPermission> permission,int maximumCrossings=4096)
    {
        ArgumentNullException.ThrowIfNull(permission);
        if(!float.IsFinite(centerOffset.X)||!float.IsFinite(centerOffset.Y)||!float.IsFinite(centerOffset.Z)||!float.IsFinite(radius)||radius<=0||radius>100 ||
            !float.IsFinite(desired.X)||!float.IsFinite(desired.Y)||!float.IsFinite(desired.Z))throw new InvalidDataException("Invalid precise door motion/bounds.");
        if(!Bounded(centerOffset)||!Bounded(desired+centerOffset))throw new InvalidDataException("Sphere door center bounds.");
        if(maximumCrossings is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumCrossings));
        var initial=query.ValidateStart(start);if(initial.Status!=CellFloorWalkStatus.Accepted)return new(initial,null,null);
        if(!Bounded(initial.WorldPosition+centerOffset))throw new InvalidDataException("Sphere door initial center bounds.");
        var cells=query.Links.Where(l=>l.SourceCell==start.CellIndex).Select(l=>l.TargetCell).Append(start.CellIndex).ToHashSet();
        int count=barriers.Keys.Count(l=>cells.Contains(l.SourceCell) && (l.SourceCell==start.CellIndex || l.TargetCell!=start.CellIndex))+leaves.Keys.Count(l=>cells.Contains(l.SourceCell));
        if(count>64)throw new InvalidDataException("Precise door collider budget.");
        var snapshots=new Dictionary<SingleLeafDoorRuntime,PortalBarrierCollider>();
        // Capture every physical pose before external permission callbacks; shared controllers have one capture.
        foreach(var leaf in leaves.Where(p=>cells.Contains(p.Key.SourceCell)).Select(p=>p.Value).Distinct())snapshots.Add(leaf,leaf.SnapshotLeafCollider());
        var decisions=new Dictionary<PortalFloorLink,PortalTransferPermission>();
        PortalTransferPermission Current(PortalFloorLink link)
        {
            if(!decisions.TryGetValue(link,out var value))decisions.Add(link,value=permission(link));return value;
        }
        var boxes=new List<PortalFloorBox>();
        var metadata=new Dictionary<int,(PortalFloorLink Link,PortalActorContactKind Kind,PortalTransferPermission? Policy)>();
        foreach(var pair in barriers.Where(p=>cells.Contains(p.Key.SourceCell)))
        {
            var link=pair.Key;
            // The reverse reference controls entry to the source, not entry to the target on this request.
            if(link.SourceCell!=start.CellIndex && link.TargetCell==start.CellIndex)continue;
            var policy=Current(link);bool? allowed=!policy.AllowEntry.HasValue || !policy.BarrierOpen.HasValue?null:policy.AllowEntry.Value && policy.BarrierOpen.Value;
            int id=boxes.Count;boxes.Add(new(link.SourceCell,id,pair.Value,allowed));metadata.Add(id,(link,PortalActorContactKind.PermissionBarrier,policy));
        }
        foreach(var pair in leaves.Where(p=>cells.Contains(p.Key.SourceCell)))
        {
            int id=boxes.Count;boxes.Add(new(pair.Key.SourceCell,id,snapshots[pair.Value],false));metadata.Add(id,(pair.Key,PortalActorContactKind.DoorLeaf,null));
        }
        var result=query.Walk(start,desired,centerOffset,radius,boxes,Current,allowSeparatingContacts,maximumCrossings);
        if(result.Contact is null)return new(result.Movement,null,null,result.Diagnostic,Steps:result.Steps);
        var selected=metadata[result.Contact.ColliderId];var hit=result.Contact.World;
        CellFloorWalkResult movement=result.Movement;
        if(result.Status==FloorBoxWalkStatus.BoxStopped)
        {
            CellFloorWalkStatus status=selected.Kind==PortalActorContactKind.DoorLeaf?CellFloorWalkStatus.PortalBlocked:
                hit.Status==PortalBarrierContactStatus.PermissionUnknown?CellFloorWalkStatus.PermissionUnknown:
                !selected.Policy!.Value.AllowEntry!.Value?CellFloorWalkStatus.EntryDenied:CellFloorWalkStatus.PortalBlocked;
            movement=movement with{Status=status};
        }
        return new(movement,new(hit.Status,hit.Parameter,hit.Center.ToVector3(),hit.Normal),selected.Link,result.Diagnostic,selected.Kind,result.Steps);
    }
    private static bool Bounded(Vector3 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z)&&Math.Abs(p.X)<=1e6f&&Math.Abs(p.Y)<=1e6f&&Math.Abs(p.Z)<=1e6f;
}
