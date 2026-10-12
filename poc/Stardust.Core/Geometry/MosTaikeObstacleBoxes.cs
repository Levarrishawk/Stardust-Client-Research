using System.Numerics;
using Stardust.Core.Assets;

namespace Stardust.Core.Geometry;

public sealed record MosTaikeObstacleBox(int ObjectId,Vector3 Minimum,Vector3 Maximum);
/// <summary>Modern conservative boxes from original mesh vertices. Closed buildings; no recovered collision semantics.</summary>
public sealed class MosTaikeObstacleBoxes
{
    public IReadOnlyList<MosTaikeObstacleBox> Boxes {get;}
    public MosTaikeObstacleBoxes(IReadOnlyList<MosTaikeObstacleBox> boxes)
    {
        if(boxes.Count>128||boxes.Any(b=>!Finite(b.Minimum)||!Finite(b.Maximum)||b.Minimum.X>b.Maximum.X||b.Minimum.Y>b.Maximum.Y||b.Minimum.Z>b.Maximum.Z))throw new InvalidDataException("Finite bounded obstacle boxes required.");
        Boxes=Array.AsReadOnly(boxes.ToArray());
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
    public MosTaikeObstacleBoxes(MosTaikeSceneResources scene)
    {
        var boxes=new List<MosTaikeObstacleBox>();
        foreach(var placement in scene.Placements)
        {
            Vector3 min=new(float.PositiveInfinity),max=new(float.NegativeInfinity);
            void Visit(AppearanceGeometryTemplate t,AppearanceTransform transform)
            {
                if(t.Mesh is {} mesh)foreach(var p in mesh.Primitives)foreach(var v in p.Vertices){var world=transform.TransformPoint(v.Position);min=Vector3.Min(min,world);max=Vector3.Max(max,world);}
                foreach(var c in t.Children)Visit(c.Template,transform.Compose(c.LocalTransform));
            }
            Visit(placement.Appearance.Geometry.Root,placement.Transform);
            if(!float.IsFinite(min.X)||!float.IsFinite(max.X))throw new InvalidDataException("Finite original obstacle geometry required.");
            boxes.Add(new(placement.Node.Id,min,max));
        }
        if(boxes.Count>128)throw new InvalidDataException("Town obstacle budget.");Boxes=boxes.AsReadOnly();
    }
    public bool Blocks(Vector3 from,Vector3 to)
    {
        // Conservative XZ swept radius .35m and vertical body1.8m; actor feet may stand on terrain at a mesh base.
        foreach(var b in Boxes)
        {
            float feet=Math.Min(from.Y,to.Y);
            if(b.Maximum.Y<=feet+.05f||b.Minimum.Y>=Math.Max(from.Y,to.Y)+1.8f)continue;
            var a=new Vector3(from.X,0,from.Z);var d=new Vector3(to.X-from.X,0,to.Z-from.Z);
            if(Intersection(a,d,new(b.Minimum.X-.35f,-1,b.Minimum.Z-.35f),new(b.Maximum.X+.35f,1,b.Maximum.Z+.35f),out _))return true;
        }
        return false;
    }
    public float CameraDistance(Vector3 target,Vector3 desired)
    {
        var delta=desired-target;float distance=delta.Length();if(distance<=0)return 0;float fraction=1;
        foreach(var b in Boxes)if(Intersection(target,delta,b.Minimum-new Vector3(.15f),b.Maximum+new Vector3(.15f),out float hit))fraction=Math.Min(fraction,Math.Max(0,hit-.15f/distance));
        return Math.Max(.25f,distance*fraction);
    }
    private static bool Intersection(Vector3 origin,Vector3 delta,Vector3 min,Vector3 max,out float entry)
    {
        float lo=0,hi=1;var o=new[]{origin.X,origin.Y,origin.Z};var d=new[]{delta.X,delta.Y,delta.Z};var a=new[]{min.X,min.Y,min.Z};var b=new[]{max.X,max.Y,max.Z};
        for(int i=0;i<3;i++)
        {
            if(Math.Abs(d[i])<1e-8f){if(o[i]<a[i]||o[i]>b[i]){entry=0;return false;}}
            else {float first=(a[i]-o[i])/d[i],last=(b[i]-o[i])/d[i];if(first>last)(first,last)=(last,first);lo=Math.Max(lo,first);hi=Math.Min(hi,last);if(lo>hi){entry=0;return false;}}
        }
        entry=lo;return true;
    }
}
