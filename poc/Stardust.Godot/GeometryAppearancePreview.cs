using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

/// <summary>Neutral geometry inspector: immutable meshes shared, transform/LOD nodes instantiated independently.</summary>
public sealed class GeometryAppearancePreview : IDisposable
{
    private readonly List<Resource> resources=new();
    private readonly Dictionary<string,ArrayMesh> meshes=new(StringComparer.Ordinal);
    private readonly List<StaticMeshPreview> texturedAdapters=new();
    private readonly PreviewTextureCache gpuTextures=new();
    private readonly IReadOnlyDictionary<string,TexturedStaticMesh>? materialBundles;
    private readonly bool allowMainTextureOnly;
    private readonly StaticNormalBundle? normalMaterials;
    private readonly List<string> materialDiagnostics=new();
    private readonly List<(Node3D Owner,Node3D[] Nodes,DetailAppearanceSelector Selector,bool[] Available,byte? Pivot,AppearanceExtent? Extent)> details=new();
    private int instances;
    private int neutralGeometrySurfaces;
    private readonly bool inspectHardpoints;
    private readonly Dictionary<(int Instance,int Authored),AppearanceHardpointPlacement> hardpointPlacements=new();
    private readonly List<(Node3D Marker,AppearanceHardpointPlacement Placement)> hardpointMarkers=new();
    private readonly List<Label3D> hardpointLabels=new();
    private readonly List<Node3D> hardpointGroups=new();
    private ImmediateMesh? hardpointAxes;
    public Node3D Root {get;}
    public int MeshInstances {get;private set;}
    public int Diagnostics {get;private set;}
    public int TexturedMeshTemplates=>texturedAdapters.Count(adapter=>adapter.TexturedSurfaces>0);
    public int NeutralMeshTemplates=>meshes.Count-TexturedMeshTemplates;
    public int TexturedSurfaces=>texturedAdapters.Sum(adapter=>adapter.TexturedSurfaces);
    internal ImageTexture GetOwnedTexture(DdsImage image)=>gpuTextures.Get(image);
    public int UniqueGpuTextures=>gpuTextures.Count;
    public int NeutralSurfaces=>neutralGeometrySurfaces+texturedAdapters.Sum(adapter=>adapter.NeutralSurfaces);
    public int OmittedSurfaces=>texturedAdapters.Sum(adapter=>adapter.OmittedSurfaces);
    public IReadOnlyList<string> MaterialDiagnostics=>materialDiagnostics;
    public int HardpointMarkers=>hardpointMarkers.Count;
    public int VisibleHardpointLabels=>hardpointLabels.Count(label=>label.Visible);
    public IReadOnlyList<string> HardpointDescriptions=>hardpointMarkers.Select(value=>$"{value.Placement.InstanceOrdinal}/{value.Placement.Hardpoint.AuthoredIndex}: {value.Placement.Hardpoint.Name}").ToArray();
    public GeometryAppearancePreview(AppearanceGeometryGraph source,IReadOnlyDictionary<string,TexturedStaticMesh>? materialBundles=null,bool allowMainTextureOnly=false,bool inspectHardpoints=false,StaticNormalBundle? normalMaterials=null)
    {
        this.normalMaterials=normalMaterials;this.materialBundles=materialBundles;this.allowMainTextureOnly=allowMainTextureOnly;this.inspectHardpoints=inspectHardpoints;
        if(inspectHardpoints)foreach(var placement in AppearanceHardpointSceneQuery.Collect(source).Placements)hardpointPlacements.Add((placement.InstanceOrdinal,placement.Hardpoint.AuthoredIndex),placement);
        VerifyTransformBoundary();
        Root=new();
        try{Build(source.Root,Root);Select(0);}
        catch{Root.Free();Dispose();throw;}
    }
    private void Build(AppearanceGeometryTemplate template,Node3D node)
    {
        if(++instances>4096)throw new AssetPreviewException("Component preview instance budget exceeded.");
        int instanceOrdinal=instances-1;
        Diagnostics+=template.Diagnostics.Count;
        if(inspectHardpoints && template.Hardpoints is AppearanceHardpointTable table && table.Authored.Count>0)
        {
            if(hardpointMarkers.Count+table.Authored.Count>32768)throw new AssetPreviewException("Hardpoint marker budget exceeded.");
            hardpointAxes??=CreateHardpointAxes();
            var group=new Node3D{Name="HardpointAnnotations"};group.SetMeta("inspection_annotation",true);node.AddChild(group);hardpointGroups.Add(group);
            foreach(var hardpoint in table.Authored)
            {
                var marker=new Node3D{Transform=GodotAppearanceTransform.ToGodot(hardpoint.LocalTransform)};group.AddChild(marker);
                marker.AddChild(new MeshInstance3D{Mesh=hardpointAxes});
                var label=new Label3D{Text=$"{hardpoint.Name} [{hardpoint.AuthoredIndex}]",Position=new Vector3(0,0.42f,0),FontSize=20,PixelSize=0.0015f,
                    Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,FixedSize=true,OutlineSize=4,Visible=false};marker.AddChild(label);hardpointLabels.Add(label);
                hardpointMarkers.Add((marker,hardpointPlacements[(instanceOrdinal,hardpoint.AuthoredIndex)]));
            }
        }
        if(template.Mesh is StaticMeshDescription description)
        {
            if(!meshes.TryGetValue(template.Path,out ArrayMesh? mesh))
            {
                bool uploadedVerified=false;
                if(materialBundles is not null && materialBundles.TryGetValue(template.Path,out TexturedStaticMesh? bundle))
                {
                    try
                    {
                        StaticMeshPreview adapter=new(bundle,allowMainTextureOnly,true,gpuTextures,normalMaterials?.Materials);texturedAdapters.Add(adapter);
                        adapter.VerifyUploadedAsset(bundle);mesh=adapter.Mesh;
                        uploadedVerified=true;
                        materialDiagnostics.AddRange(adapter.MaterialDiagnostics.Select(diagnostic=>template.Path+": "+diagnostic));
                    }
                    catch(AssetPreviewException failure){materialDiagnostics.Add(template.Path+": "+failure.Message);}
                }
                if(mesh is null)
                {
                    mesh=new();resources.Add(mesh);
                    foreach(MeshPrimitive primitive in description.Primitives)
                    {
                        using Godot.Collections.Array arrays=StaticMeshPreview.CreateArrays(primitive);
                        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arrays);
                        StandardMaterial3D material=new(){ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
                            AlbedoColor=Color.FromHsv((mesh.GetSurfaceCount()*0.137f)%1,0.28f,0.86f)};
                        resources.Add(material);mesh.SurfaceSetMaterial(mesh.GetSurfaceCount()-1,material);
                        neutralGeometrySurfaces++;
                    }
                }
                meshes.Add(template.Path,mesh);
                if(!uploadedVerified)StaticMeshPreview.VerifyUploadedGeometry(mesh,description);
            }
            var instance=new MeshInstance3D{Mesh=mesh};instance.SetMeta("original_mesh_path",template.Path);node.AddChild(instance);MeshInstances++;
        }
        Dictionary<int,Node3D> authored=new();
        foreach(AppearanceGeometryChild child in template.Children)
        {
            Transform3D transform=GodotAppearanceTransform.ToGodot(child.LocalTransform);
            if(!float.IsFinite(transform.Basis.Determinant()) || transform.Basis.Determinant()<=0)
                throw new AssetPreviewException("Neutral component preview requires nonsingular positive-determinant transforms.");
            Node3D instance=new(){Transform=transform};node.AddChild(instance);authored.Add(child.AuthoredIndex,instance);Build(child.Template,instance);
        }
        if(template.Detail is not null)
        {
            DetailAppearanceSelector selector=new(template.Detail);
            Node3D[] nodes=selector.Levels.Select(level=>authored[template.Detail.Levels.ToList().FindIndex(candidate=>candidate.Id==level.Id)]).ToArray();
            AppearanceExtent? extent=AppearanceExtentDecoder.DecodeBase(template.Detail.Metadata.Children[0].Children[0]);
            details.Add((node,nodes,selector,nodes.Select(_=>true).ToArray(),template.Detail.Pivot,extent));
        }
    }
    public void Select(float distance)
    {
        foreach(var detail in details)
        {
            int previous=Array.FindIndex(detail.Nodes,node=>node.Visible);
            int selected=detail.Selector.Select(distance,previous,detail.Available);
            for(int i=0;i<detail.Nodes.Length;i++)detail.Nodes[i].Visible=i==selected;
        }
    }
    public void VerifySwitches()
    {
        foreach(var detail in details)
        {
            for(int i=0;i<detail.Selector.Levels.Count;i++)
            {
                DetailAppearanceLevel level=detail.Selector.Levels[i];
                Select(level.LowerDistance+(level.UpperDistance-level.LowerDistance)*0.5f);
                if(!detail.Nodes[i].Visible || detail.Nodes.Where((_,index)=>index!=i).Any(node=>node.Visible))
                    throw new InvalidOperationException("Component detail visibility switch mismatch.");
            }
        }
        Select(0);
    }
    public int CameraSelectors=>details.Count;
    private ImmediateMesh CreateHardpointAxes()
    {
        var material=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,VertexColorUseAsAlbedo=true,NoDepthTest=true};resources.Add(material);
        var axes=new ImmediateMesh();resources.Add(axes);axes.SurfaceBegin(Godot.Mesh.PrimitiveType.Lines,material);
        foreach(var (color,end) in new[]{(new Color(1,0.2f,0.2f),new Vector3(0.35f,0,0)),(new Color(0.2f,1,0.2f),new Vector3(0,0.35f,0)),(new Color(0.2f,0.5f,1),new Vector3(0,0,-0.35f))})
        {axes.SurfaceSetColor(color);axes.SurfaceAddVertex(Vector3.Zero);axes.SurfaceAddVertex(end);}
        axes.SurfaceEnd();return axes;
    }
    public void SetHardpointsVisible(bool visible){foreach(var group in hardpointGroups)group.Visible=visible;}
    public void SelectHardpointAnnotation(int index)
    {
        if(index<-1 || index>=hardpointLabels.Count)throw new ArgumentOutOfRangeException(nameof(index));
        for(int i=0;i<hardpointLabels.Count;i++)hardpointLabels[i].Visible=i==index;
    }
    public void VerifyHardpointPlacements()
    {
        foreach(var (marker,placement) in hardpointMarkers)
        {
            Transform3D expected=GodotAppearanceTransform.ToGodot(placement.PlacementTransform),actual=marker.GlobalTransform;
            if(!actual.Origin.IsEqualApprox(expected.Origin) || !actual.Basis.X.IsEqualApprox(expected.Basis.X) || !actual.Basis.Y.IsEqualApprox(expected.Basis.Y) || !actual.Basis.Z.IsEqualApprox(expected.Basis.Z))throw new InvalidOperationException("Godot composed hardpoint marker transform mismatch.");
        }
        SetHardpointsVisible(false);if(hardpointGroups.Any(group=>group.Visible))throw new InvalidOperationException("Hardpoint annotation visibility mismatch.");
        SetHardpointsVisible(true);GD.Print($"PASS: Godot hardpoint placements markers={hardpointMarkers.Count} reflected transforms and visibility");
        for(int i=0;i<hardpointLabels.Count;i++){SelectHardpointAnnotation(i);if(hardpointLabels.Count(label=>label.Visible)!=1)throw new InvalidOperationException("Hardpoint annotation selection mismatch.");}
        SelectHardpointAnnotation(hardpointLabels.Count==0?-1:0);
    }
    public void SelectFromCamera(Vector3 cameraWorld)
    {
        foreach(var detail in details)
        {
            float distance=GodotAppearanceTransform.Distance(detail.Pivot,detail.Extent,detail.Owner.GlobalTransform,cameraWorld);
            int previous=Array.FindIndex(detail.Nodes,node=>node.Visible);
            int selected=detail.Selector.Select(distance,previous,detail.Available);
            for(int i=0;i<detail.Nodes.Length;i++)detail.Nodes[i].Visible=i==selected;
        }
    }
    public void VerifyCameraSwitches()
    {
        foreach(var detail in details)
        {
            System.Numerics.Vector3 center=detail.Pivot==1?System.Numerics.Vector3.Zero:detail.Extent?.Center??System.Numerics.Vector3.Zero;
            Vector3 origin=detail.Owner.GlobalTransform*new Vector3(center.X,center.Y,-center.Z);
            for(int i=0;i<detail.Selector.Levels.Count;i++)
            {
                DetailAppearanceLevel level=detail.Selector.Levels[i];float distance=level.LowerDistance+(level.UpperDistance-level.LowerDistance)*0.5f;
                SelectFromCamera(origin+new Vector3(distance,0,0));
                if(!detail.Nodes[i].Visible)throw new InvalidOperationException("Component camera/PIVT detail switch mismatch.");
            }
        }
        Select(0);
    }
    public Aabb Bounds=>BoundsFor(Root);
    public static Aabb BoundsFor(Node3D root)
    {
        Aabb? result=null;
        void Visit(Node3D node,Transform3D parent)
        {
            if(node.HasMeta("inspection_annotation"))return;
            Transform3D world=parent*node.Transform;
            if(node is MeshInstance3D mesh && mesh.Mesh is not null)
            {Aabb bounds=world*mesh.Mesh.GetAabb();result=result is null?bounds:result.Value.Merge(bounds);}
            foreach(Node child in node.GetChildren())if(child is Node3D spatial)Visit(spatial,world);
        }
        Visit(root,Transform3D.Identity);return result??throw new AssetPreviewException("Preview has no supported mesh geometry.");
    }
    public static void VerifyTransformBoundary()
    {
        AppearanceTransform parent=new(new(0,-1,0),new(1,0,0),new(0,0,1),new(10,20,30));
        AppearanceTransform local=new(new(2,0,0),new(0,3,0),new(0,0,4),new(1,2,3));
        Vector3 reflected=new(5,6,-7);
        Vector3 expected=new(-10,31,-61);
        if(GodotAppearanceTransform.ToGodot(parent)*GodotAppearanceTransform.ToGodot(local)*reflected!=expected ||
            GodotAppearanceTransform.ToGodot(parent.Compose(local))*reflected!=expected ||
            GodotAppearanceTransform.ToCore(GodotAppearanceTransform.ToGodot(parent))!=parent ||
            GodotAppearanceTransform.ToCore(GodotAppearanceTransform.ToGodot(local))!=local)
            throw new InvalidOperationException("Component S R S transform fixture failed.");
    }
    public void Dispose(){foreach(StaticMeshPreview adapter in texturedAdapters.AsEnumerable().Reverse())adapter.Dispose();texturedAdapters.Clear();foreach(Resource resource in resources.AsEnumerable().Reverse())resource.Dispose();resources.Clear();gpuTextures.Dispose();meshes.Clear();details.Clear();hardpointMarkers.Clear();hardpointLabels.Clear();hardpointGroups.Clear();hardpointPlacements.Clear();hardpointAxes=null;}
}
