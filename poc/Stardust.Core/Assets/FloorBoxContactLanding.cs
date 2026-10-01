using System.Numerics;

namespace Stardust.Core.Assets;

internal readonly record struct FloorBoxContactLandingResult(bool Accepted,FloorWalkResult Floor);

/// <summary>Shared bounded local contact quantization; callers supply an already validated floor prefix.</summary>
internal static class FloorBoxContactLanding
{
    internal static FloorBoxContactLandingResult Resolve(SingleCellFloorWalker floor,FloorWalkPath path,
        int triangle,Vector3 start,FloorPathPoint feet,Vector3 normal,int segmentIndex,double parameter,int maximumCrossings,Func<Vector3,bool>? additionalValidation=null)
    {
        FloorWalkResult Held()=>floor.Walk(triangle,start,start) with{Parameter=0,Crossings=0};
        if(parameter==0)return new(true,Held());
        FloorWalkResult landing=floor.Walk(triangle,start,feet.ToVector3(),maximumCrossings);
        bool Valid(FloorWalkResult candidate)
        {
            double dx=candidate.Position.X-feet.X,dy=candidate.Position.Y-feet.Y,dz=candidate.Position.Z-feet.Z;
            return candidate.Status==FloorWalkStatus.Accepted && dx*dx+dy*dy+dz*dz<=1e-8 &&
                dx*normal.X+dy*normal.Y+dz*normal.Z>=-1e-6 &&
                floor.Walk(candidate.TriangleIndex,candidate.Position,candidate.Position).Status==FloorWalkStatus.Accepted &&
                (additionalValidation is null || additionalValidation(candidate.Position));
        }
        if(!Valid(landing))
        {
            var segment=path.Segments[segmentIndex];
            (double X,double Z)[] directions={(normal.X,normal.Z),(segment.Start.X-segment.End.X,segment.Start.Z-segment.End.Z)};
            bool found=false;
            foreach(var direction in directions)
            {
                double length=Math.Sqrt(direction.X*direction.X+direction.Z*direction.Z);if(length==0)continue;
                foreach(double distance in new[]{1e-6,2e-6,5e-6,1e-5,2e-5,5e-5,1e-4})
                {
                    FloorPathPoint biased=feet with{X=feet.X+direction.X/length*distance,Z=feet.Z+direction.Z/length*distance};
                    var candidate=floor.Walk(triangle,start,biased.ToVector3(),maximumCrossings);
                    if(!Valid(candidate))continue;landing=candidate;found=true;break;
                }
                if(found)break;
            }
        }
        return Valid(landing)?new(true,landing with{Parameter=parameter}):new(false,Held());
    }
}
