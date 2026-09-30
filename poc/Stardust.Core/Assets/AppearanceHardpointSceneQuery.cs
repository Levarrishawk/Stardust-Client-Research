using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record AppearanceHardpointPlacement(int InstanceOrdinal,string OwnerPath,IReadOnlyList<int> ChildOrdinals,
    AppearanceHardpoint Hardpoint,AppearanceTransform PlacementTransform);
public sealed record AppearanceHardpointScene(IReadOnlyList<AppearanceHardpointPlacement> Placements,int InstanceCount);

/// <summary>All loaded static instance annotations, including inactive LOD children. Not original virtual attachment lookup.</summary>
public static class AppearanceHardpointSceneQuery
{
    public static AppearanceHardpointScene Collect(AppearanceGeometryGraph graph,AppearanceTransform? rootTransform=null,
        int maximumInstances=4096,int maximumPlacements=32768,int maximumDepth=32)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if(maximumInstances<1 || maximumPlacements<1 || maximumDepth<1)throw new ArgumentOutOfRangeException(nameof(maximumInstances));
        List<AppearanceHardpointPlacement> result=new();int instances=0;
        HashSet<AppearanceGeometryTemplate> active=new(ReferenceEqualityComparer.Instance);
        void Visit(AppearanceGeometryTemplate template,AppearanceTransform world,int[] path)
        {
            if(path.Length>=maximumDepth || instances>=maximumInstances)throw new AppearanceGraphBudgetException("Hardpoint scene instance/depth budget exceeded.");
            if(!active.Add(template))throw new InvalidDataException("Hardpoint scene contains a template cycle.");
            RequireFinite(world);int ordinal=instances++;
            foreach(var hardpoint in template.Hardpoints?.Authored??Array.Empty<AppearanceHardpoint>())
            {
                if(result.Count>=maximumPlacements)throw new AppearanceGraphBudgetException("Hardpoint scene placement budget exceeded.");
                AppearanceTransform composed=world.Compose(hardpoint.LocalTransform);RequireFinite(composed);
                result.Add(new(ordinal,template.Path,Array.AsReadOnly((int[])path.Clone()),hardpoint,composed));
            }
            foreach(var child in template.Children.ToArray())Visit(child.Template,world.Compose(child.LocalTransform),path.Append(child.AuthoredIndex).ToArray());
            active.Remove(template);
        }
        Visit(graph.Root,rootTransform??AppearanceTransform.Identity,Array.Empty<int>());
        return new(result.AsReadOnly(),instances);
    }
    public static IReadOnlyList<AppearanceHardpointPlacement> FindAll(AppearanceHardpointScene scene,int instanceOrdinal,string name)
    {
        ArgumentNullException.ThrowIfNull(scene);AppearanceHardpointDecoder.NameBytes(name);
        return Array.AsReadOnly(scene.Placements.Where(value=>value.InstanceOrdinal==instanceOrdinal && value.Hardpoint.Name==name).ToArray());
    }
    public static bool TryFindUnique(AppearanceHardpointScene scene,int instanceOrdinal,string name,out AppearanceHardpointPlacement? placement)
    {var matches=FindAll(scene,instanceOrdinal,name);placement=matches.Count==1?matches[0]:null;return placement is not null;}
    private static void RequireFinite(AppearanceTransform transform)
    {
        static bool Finite(Vector3 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z);
        if(!Finite(transform.RowX)||!Finite(transform.RowY)||!Finite(transform.RowZ)||!Finite(transform.Translation))throw new InvalidDataException("Nonfinite composed hardpoint placement.");
    }
}
