using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class ChaseCameraScenarios
{
    internal static void Run()
    {
        var wall=new[]{new CollisionTriangle(0,new(-10,-10,-2),new(10,-10,-2),new(0,10,-2))};var rig=new ChaseCameraObstruction(wall,.2f,.05f);
        Array.Clear(wall);var result=rig.Query(Vector3.Zero,Vector3.UnitZ,5,AppearanceTransform.Identity,Array.Empty<PortalBarrierCollider>());
        if(result.Obstacle!=ChaseCameraObstacle.RoomTriangle||Math.Abs(result.Distance-1.75f)>1e-6||result.InitialOverlap||Vector3.Distance(result.Position,new(0,0,-1.75f))>1e-6)throw new Exception("Owned camera wall clearance mismatch.");
        var owner=new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9));var transformed=rig.Query(owner.Translation,owner.TransformDirection(Vector3.UnitZ),5,owner,Array.Empty<PortalBarrierCollider>());
        if(Math.Abs(transformed.Distance-result.Distance)>1e-6||Vector3.Distance(transformed.Position,owner.TransformPoint(result.Position))>1e-6)throw new Exception("Camera owner transform mismatch.");
        var door=new PortalBarrierCollider(new(-1,-1,-1),new(1,1,-.8f),AppearanceTransform.Identity);var nearer=rig.Query(Vector3.Zero,Vector3.UnitZ,5,AppearanceTransform.Identity,new[]{door});
        if(nearer.Obstacle!=ChaseCameraObstacle.DoorBox||Math.Abs(nearer.Distance-.55f)>1e-6)throw new Exception("Camera nearest dynamic door mismatch.");
        var open=new PortalBarrierCollider(door.Minimum,door.Maximum,AppearanceTransform.Identity with{Translation=new(4,0,0)});if(rig.Query(Vector3.Zero,Vector3.UnitZ,5,AppearanceTransform.Identity,new[]{open}).Obstacle!=ChaseCameraObstacle.RoomTriangle)throw new Exception("Camera open-door snapshot did not release.");
        var clear=rig.Query(Vector3.Zero,-Vector3.UnitZ,5,AppearanceTransform.Identity,Array.Empty<PortalBarrierCollider>());if(clear.Obstacle!=ChaseCameraObstacle.None||clear.Distance!=5)throw new Exception("Camera clear boom mismatch.");
        var overlap=rig.Query(new(0,0,-2),Vector3.UnitZ,5,AppearanceTransform.Identity,Array.Empty<PortalBarrierCollider>());if(!overlap.InitialOverlap||overlap.Distance!=0||overlap.Position!=overlap.Target)throw new Exception("Camera initial overlap should hold target.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        Reject(()=>rig.Query(Vector3.Zero,Vector3.Zero,5,AppearanceTransform.Identity,Array.Empty<PortalBarrierCollider>()));Reject(()=>rig.Query(Vector3.Zero,Vector3.UnitZ,float.NaN,AppearanceTransform.Identity,Array.Empty<PortalBarrierCollider>()));Reject(()=>new ChaseCameraObstruction(Array.Empty<CollisionTriangle>(),0,0));Reject(()=>rig.Query(Vector3.Zero,Vector3.UnitZ,5,AppearanceTransform.Identity with{RowX=new(2,0,0)},Array.Empty<PortalBarrierCollider>()));
        Console.WriteLine("PASS: owned chase-camera sphere boom, nearest room/door obstruction, dynamic release, rigid owner/reflection-independent position, initial overlap and negative gates");
    }
    internal static void Probe(InteriorSceneContentBundle content)
    {
        var profile=content.CameraProfile??throw new Exception("Original camera profile absent.");var identity=content.Sources["camera/freechasecamera.iff"];FreeChaseCameraScenarios.Dump(profile,identity.Sha256,identity.Bytes);
        var rig=ChaseCameraObstruction.FromInterior(content.TriangleGuard!);var bundle=content.Scene;var start=bundle.Doorways[0].ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true)).Start;
        var geometries=bundle.Doorways.Select(d=>bundle.Layout.Cells[d.Cell].Portals[d.Portal].GeometryIndex).ToArray();int count=0,blocked=0,overlaps=0;
        foreach(bool transformed in new[]{false,true})
        {
            var owner=transformed?new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
            var fallback=content.ActorProfile!.Collision.CreateFallbackSphere();using var scene=bundle.CreateActor(start,owner,holdClosingLeaves:true,actorSphere:new(fallback.LocalCenter,fallback.Radius));var before=scene.State;
            var doors=bundle.Doorways.Select((d,i)=>new PortalBarrierCollider(d.Leaf.Extent.Minimum!.Value,d.Leaf.Extent.Maximum!.Value,before.Doors[geometries[i]].LeafPose)).ToArray();
            Console.WriteLine("original_chase_doors="+JsonSerializer.Serialize(new{transformed,doors=doors.Select(d=>new{minimum=V(d.Minimum),maximum=V(d.Maximum),pose=new[]{V(d.Pose.RowX),V(d.Pose.RowY),V(d.Pose.RowZ),V(d.Pose.Translation)}})}));
            foreach(float yaw in new[]{0f,MathF.PI*.5f,MathF.PI,-MathF.PI*.5f})foreach(float pitch in new[]{-.35f,0,.35f})for(int setting=0;setting<profile.ZoomSettings.Count;setting++)
            {
                var direction=owner.TransformDirection(new ActorViewOrientation(yaw,pitch).LookForward(Vector3.UnitZ));var target=before.WorldPosition+Vector3.UnitY*profile.TargetHeight(0,new HashSet<int>(),1);
                var result=rig.Query(target,direction,profile.ZoomDistance(setting,1),owner,doors);if(!ReferenceEquals(before,scene.State))throw new Exception("Camera query mutated original actor scene.");
                if(!result.InitialOverlap&&rig.Query(result.Position,direction,0,owner,doors).InitialOverlap)throw new Exception("Original resolved camera sphere intersects geometry.");
                if(result.Obstacle!=ChaseCameraObstacle.None)blocked++;if(result.InitialOverlap)overlaps++;count++;
                Console.WriteLine("original_chase_camera="+JsonSerializer.Serialize(new{transformed,yaw,pitch,setting,target=V(target),direction=V(direction),desired=V(result.Desired),position=V(result.Position),requested=result.RequestedDistance,resolved=result.Distance,obstacle=result.Obstacle.ToString(),result.ObstacleIndex,result.InitialOverlap,result.CandidateTriangles}));
            }
        }
        Console.WriteLine($"PASS: original camera boom queries={count} blocked={blocked} initial_overlaps={overlaps} owned_room_triangles={rig.RoomTriangles} no actor state mutation");
    }
    private static float[] V(Vector3 value)=>new[]{value.X,value.Y,value.Z};
}
