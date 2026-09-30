using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record AppearanceGeometryChild(int AuthoredIndex,AppearanceTransform LocalTransform,AppearanceGeometryTemplate Template);
public sealed record AppearanceGeometryTemplate(string Path,StaticMeshDescription? Mesh,DetailAppearanceDescription? Detail,
    ComponentAppearanceDescription? Component,IReadOnlyList<AppearanceGeometryChild> Children,IReadOnlyList<string> Diagnostics,string? MeshSourcePath=null)
{
    public AppearanceHardpointTable? Hardpoints=>Mesh?.Hardpoints??Detail?.Hardpoints??Component?.Hardpoints;
}
public sealed record AppearanceGeometryGraph(AppearanceGeometryTemplate Root,IReadOnlyDictionary<string,AssetSourceIdentity> Sources);
public sealed record AppearanceGeometryRequest(string Path,string? ExpectedSha256=null);
public sealed record AppearanceGeometryRoot(AppearanceGeometryRequest Request,AppearanceGeometryTemplate? Template);
public sealed record AppearanceGeometryCollection(IReadOnlyList<AppearanceGeometryRoot> Roots,IReadOnlyDictionary<string,AssetSourceIdentity> Sources,IReadOnlyList<string> Diagnostics);
public sealed record AppearanceGraphLimits(int MaximumResources=256,long MaximumSourceBytes=134_217_728,int MaximumDepth=32,int MaximumEdges=4096);
public sealed class AppearanceGraphBudgetException(string message):IOException(message);

/// <summary>Offline immutable static geometry graph. Components tolerate part-local failure; detail templates require all bands.</summary>
public static class AppearanceGeometryGraphReader
{
    public static AppearanceGeometryGraph Load(Func<string,TreLookupResult> resolve,string path,string expectedSha256,AppearanceGraphLimits? limits=null)
    {
        AppearanceGeometryCollection collection=LoadMany(resolve,new[]{new AppearanceGeometryRequest(path,expectedSha256)},limits);
        return new(collection.Roots[0].Template!,collection.Sources);
    }
    public static AppearanceGeometryCollection LoadMany(Func<string,TreLookupResult> resolve,IReadOnlyList<AppearanceGeometryRequest> requests,
        AppearanceGraphLimits? limits=null,bool allowUnavailableRoots=false)
    {
        ArgumentNullException.ThrowIfNull(resolve);ArgumentNullException.ThrowIfNull(requests);limits??=new();
        if(limits.MaximumResources<1 || limits.MaximumSourceBytes<1 || limits.MaximumDepth<1 || limits.MaximumEdges<1)
            throw new ArgumentOutOfRangeException(nameof(limits));
        if(requests.Count is 0 || requests.Count>limits.MaximumResources)throw new AppearanceGraphBudgetException("Appearance root collection budget exceeded.");
        OwnedAssetSourceCache cache=new(resolve,limits);IReadOnlyDictionary<string,AssetSourceIdentity> sources=cache.Sources;
        Dictionary<string,AppearanceGeometryTemplate> templates=new(StringComparer.Ordinal);HashSet<string> active=new(StringComparer.Ordinal);int edges=0;
        TreLookupResult Read(string resource)=>cache.Read(resource);
        AppearanceGeometryTemplate Visit(string resource,int depth,string? expected=null)
        {
            resource=LegacyAssetPath.Normalize(resource);
            if(depth>limits.MaximumDepth)throw new AppearanceGraphBudgetException("Appearance recursion depth exceeded.");
            if(active.Contains(resource))throw new InvalidDataException("Appearance resource cycle.");
            if(templates.TryGetValue(resource,out AppearanceGeometryTemplate? cached))
            {
                // A repeated root's pin must still be checked even when its template is shared.
                if(expected is not null)AppearanceResolver.FetchStrict(Read,resource,expected);
                return cached;
            }
            active.Add(resource);
            try
            {
                AppearanceResolution resolution=AppearanceResolver.FetchStrict(Read,resource,expected);
                IffNode metadata=resolution.TerminalMetadata;
                StaticMeshDescription? mesh=null;DetailAppearanceDescription? detail=null;ComponentAppearanceDescription? component=null;
                List<AppearanceGeometryChild> children=new();List<string> diagnostics=new();
                void Edge(int index,AppearanceTransform transform,string childPath,bool tolerateFailure)
                {
                    if(++edges>limits.MaximumEdges)throw new AppearanceGraphBudgetException("Appearance edge budget exceeded.");
                    try{children.Add(new(index,transform,Visit(childPath,depth+1)));}
                    catch(InvalidDataException)when(tolerateFailure){diagnostics.Add($"UnavailablePart:{index}:{childPath}");}
                }
                if(metadata.IsForm("MESH"))mesh=StaticMeshDecoder.Decode(resolution.TerminalBytes.Span);
                else if(metadata.IsForm("DTLA"))
                {
                    detail=DetailAppearanceDecoder.Decode(resolution.TerminalBytes.Span);
                    for(int i=0;i<detail.Levels.Count;i++)
                    {
                        string child=detail.Levels[i].ChildPath??throw new InvalidDataException("Detail band has no child.");
                        Edge(i,AppearanceTransform.Identity,child,false);
                    }
                }
                else if(metadata.IsForm("CMPA"))
                {
                    component=ComponentAppearanceDecoder.Decode(resolution.TerminalBytes.Span);
                    for(int i=0;i<component.Parts.Count;i++)
                    {
                        ComponentAppearancePart part=component.Parts[i];Edge(i,part.LocalTransform,part.ChildPath,true);
                    }
                }
                else throw new InvalidDataException("Unsupported appearance geometry family.");
                AppearanceGeometryTemplate value=new(resource,mesh,detail,component,children.AsReadOnly(),diagnostics.AsReadOnly(),mesh is null?null:resolution.TerminalPath);
                templates.Add(resource,value);return value;
            }
            finally{active.Remove(resource);}
        }
        List<AppearanceGeometryRoot> roots=new();List<string> rootDiagnostics=new();
        for(int index=0;index<requests.Count;index++)
        {
            AppearanceGeometryRequest request=requests[index];
            // Explicit identity pins remain fatal even when unsupported unpinned roots are tolerated.
            if(request.ExpectedSha256 is not null)AppearanceResolver.FetchStrict(Read,request.Path,request.ExpectedSha256);
            try{roots.Add(new(request,Visit(request.Path,1)));}
            catch(InvalidDataException failure)when(allowUnavailableRoots){roots.Add(new(request,null));rootDiagnostics.Add($"UnavailableRoot:{index}:{LegacyAssetPath.Normalize(request.Path)}:{failure.Message}");}
        }
        return new(roots.AsReadOnly(),sources,rootDiagnostics.AsReadOnly());
    }
}
