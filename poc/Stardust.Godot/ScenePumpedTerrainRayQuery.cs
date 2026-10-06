using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Geometry;

public partial class ScenePumpedWorldPreview
{
    private TerrainModernPatchSetRayQuery? suppliedTerrainRayQuery;
    private static TerrainModernPatchSetRay ProbeRay(double x,double y,double z,double dx,double dy,double dz,double maximum)
        => new(new(x,y,z),new(dx,dy,dz),0,maximum);
    private static object RayObservation(TerrainModernPatchSetRay ray,TerrainModernPatchSetRayHit? hit)
        => new { Origin=new[]{ray.Origin.X,ray.Origin.Y,ray.Origin.Z},Direction=new[]{ray.Direction.X,ray.Direction.Y,ray.Direction.Z},
            Minimum=ray.MinimumT,Maximum=ray.MaximumT,Hit=hit is { } h ? new {
                h.ChunkIndex,h.PatchIndex,h.TriangleIndex,h.VertexA,h.VertexB,h.VertexC,Parameter=h.T,
                Position=new[]{h.Position.X,h.Position.Y,h.Position.Z},Weights=new[]{h.WeightA,h.WeightB,h.WeightC},
                Normal=new[]{h.Normal.X,h.Normal.Y,h.Normal.Z},ActualReferences=ReferenceEquals(h.Geometry,h.Patch.Geometry)
                    &&ReferenceEquals(h.Child,h.Membership.Query)&&ReferenceEquals(h.Chunk,h.Source.Source.Patches[h.ChunkIndex]),
                OriginalCollisionParity=false } : null };
    private void InspectTerrainRays(Camera3D camera)
    {
        if(suppliedTerrainSetQuery is null)return;
        var source=suppliedTerrainSetQuery;
        var query=TerrainModernPatchSetRayQuery.Build(source);
        suppliedTerrainRayQuery=query;
        GD.Print("modern_set_ray_query_ready="+JsonSerializer.Serialize(new{query.ChunkCount,query.PatchCount,query.TotalTriangles,
            query.OwnCopyBytes,query.OwnArrayBytes,query.OwnWork,query.BorrowedSurfaceArrayReservation,
            query.GeometryBuilds,query.NormalBuilds,query.SurfaceBuilds,query.SetAdmissions,
            ActualSource=ReferenceEquals(query.Source,source),OriginalCollisionParity=false}));
        var rays=new List<TerrainModernPatchSetRay>();
        for(int z=-8;z<=8;z+=2)for(int x=-8;x<=8;x+=2)rays.Add(ProbeRay(x,20,z,0,-1,0,40));
        foreach(int z in new[]{-6,0,6})foreach(int x in new[]{-6,0,6})rays.Add(ProbeRay(x,-20,z,0,2,0,20));
        foreach(int x in new[]{-6,-2,2,6})foreach(int z in new[]{-6,2})rays.Add(ProbeRay(x,20,z,.125,-2,.25,20));
        foreach(var p in new[]{(-10,0),(10,0),(0,-10),(0,10)})rays.Add(ProbeRay(p.Item1,20,p.Item2,0,-1,0,40));
        rays.Add(ProbeRay(0,20,0,1,0,0,40));rays.Add(ProbeRay(0,20,0,0,-1,0,1));
        var hits=query.QueryBatch(rays.ToArray());
        Check(hits.Count==104,"Complete bounded terrain ray probes");
        GD.Print("modern_set_ray_query_samples="+JsonSerializer.Serialize(rays.Select((ray,i)=>RayObservation(ray,hits[i]))));
        // The camera supplies engine-space rays; reflect Z once at the adapter boundary.
        var center=GetViewport().GetVisibleRect().Size/2;
        var origin=camera.ProjectRayOrigin(center);var direction=camera.ProjectRayNormal(center);
        var cameraRay=ProbeRay(origin.X,origin.Y,-origin.Z,direction.X,direction.Y,-direction.Z,1000);
        var cameraHit=query.Query(cameraRay);
        GD.Print("modern_terrain_camera_ray="+JsonSerializer.Serialize(new{EngineOrigin=new[]{origin.X,origin.Y,origin.Z},
            EngineDirection=new[]{direction.X,direction.Y,direction.Z},Native=RayObservation(cameraRay,cameraHit),
            ActualCamera=true,OriginalCollisionParity=false}));
    }
}
