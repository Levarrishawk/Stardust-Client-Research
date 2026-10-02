using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Proof;
using System.Diagnostics;

internal static class OriginalInteriorSphereScenarios
{
    internal static void ProbeOptional(string[] args,bool loadContent=false,bool guardTriangles=false,bool loadCharacter=false,bool animateCharacter=false,bool characterMaterials=false,bool paletteDefaults=false,bool generatedAppearance=false,bool customized=false,bool shaped=false,bool normalDiffuse=false,bool vertexLitSpecular=false,bool locomotionBank=false,bool chaseCamera=false)
    {
        if(args.Length!=(normalDiffuse?14:shaped?12:customized?10:8)||normalDiffuse&&args[12]!="--texture-profile"||shaped&&args[10]!="--shape-profile"||customized&&args[8]!="--customization-profile"||args[2]!="--configuration"||args[4]!="--scene-spec"||args[6]!="--expected-sha256")throw new ArgumentException("Pinned original sphere scene specification required.");
        if(new FileInfo(args[5]).Length>16384)throw new InvalidDataException("Sphere scene specification budget.");
        var bytes=File.ReadAllBytes(args[5]);if(Convert.ToHexString(SHA256.HashData(bytes))!=args[7].ToUpperInvariant())throw new InvalidDataException("Sphere scene specification identity.");
        using var configuration=File.OpenText(args[3]);using var overlay=ConfiguredTreOverlay.Open(args[1],configuration);
        const string tablePin="C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7";
        const string actorPath="object/creature/player/shared_human_male.iff",actorPin="E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243";
        var counts=new Dictionary<string,int>();
        TreLookupResult Resolve(string path){counts[path]=counts.GetValueOrDefault(path)+1;return overlay.Lookup(path);}
        IReadOnlyDictionary<BlueprintCustomizationKey,int>? faceValues=null;
        if(generatedAppearance){using var faceProfile=File.OpenRead("poc/preview-profiles/human-face-zero.json");faceValues=BlueprintFaceCustomizationProfile.Load(faceProfile);}
        IReadOnlyDictionary<ShaderCustomizationKey,int>? customizationValues=null;
        if(customized){using var profileInput=File.OpenRead(args[9]);customizationValues=SkeletalAppearanceCustomization.LoadProfile(profileInput);faceValues=null;}
        IReadOnlyDictionary<string,int>? shapeValues=null;if(shaped){using var shapeInput=File.OpenRead(args[11]);shapeValues=SkeletalAppearanceShapes.LoadProfile(shapeInput);}
        IReadOnlyDictionary<ShaderCustomizationKey,int>? textureValues=null;if(normalDiffuse){using var textureInput=File.OpenRead(args[13]);textureValues=SkeletalAppearanceCustomization.LoadProfile(textureInput);}
        var content=loadContent?InteriorSceneContentBundleReader.Load(Resolve,bytes,args[7],tablePin,
            "appearance/thm_must_mining_outpost.pob","79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818",true,true,
            actorTemplatePath:actorPath,actorTemplateSha256:actorPin,createTriangleGuard:guardTriangles,maximumCollisionTriangles:65536,
            characterAppearanceSha256:loadCharacter?"951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C":null,
            characterAnimationSelection:animateCharacter?LogicalAnimationProxyScenarios.IdleSelection:null,characterMaterialDefaults:characterMaterials,characterPaletteDefaults:paletteDefaults,characterAlbedoFactors:generatedAppearance,characterFaceValues:faceValues,characterCustomizationValues:customizationValues,characterShapeValues:shapeValues,characterNormalDiffuse:normalDiffuse,characterTextureValues:textureValues,characterVertexLitSpecular:vertexLitSpecular,characterAdditionalAnimations:locomotionBank?SkeletalAnimationBankScenarios.Profile():null,cameraProfileSha256:chaseCamera?"01A19A5B18E81C9366B86B5F313C31D8F0C1961F8230DB4535AA5C50E95C543D":null,loadCellLights:chaseCamera,loadRoomLighting:chaseCamera,loadStaticNormals:chaseCamera):null;
        var bundle=content?.Scene??InteriorDoorSceneBundleReader.Load(Resolve,bytes,args[7],tablePin);
        var profile=content?.ActorProfile??SharedCreatureTemplateProfileReader.Load(Resolve,actorPath,actorPin);
        if(counts.Values.Any(c=>c!=1))throw new InvalidDataException("Sphere scene source resolved more than once.");
        if(content is not null)
        {
            foreach(var source in content.Sources.Values)Console.WriteLine("interior_sphere_content_source="+JsonSerializer.Serialize(source));
            Console.WriteLine("interior_sphere_content_counts="+JsonSerializer.Serialize(counts));
            if(loadCharacter)
            {
                if(content.Character?.Geometries is null)throw new InvalidDataException("Requested character absent from content.");
                foreach(var source in content.Character.Geometries)Console.WriteLine("interior_character_geometry="+JsonSerializer.Serialize(new{path=source.Key,source=source.Value.SourceTriangles,degenerate=source.Value.DegenerateTriangles,triangles=source.Value.Triangles,sha256=SkeletalGeometryScenarios.CanonicalHash(source.Value)}));
                if(locomotionBank)SkeletalAnimationBankScenarios.Probe(content.Character,overlay.Lookup,actorPath,actorPin,"951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C");
                if(characterMaterials)SkeletalDescriptorScenarios.DumpMaterials(content.Character.MaterialBundle!);
                if(customized)Console.WriteLine("skeletal_customization_values="+JsonSerializer.Serialize(content.Character.CustomizationValues!.Select(p=>new{p.Key.VariableName,p.Key.Private,p.Value})));
                if(generatedAppearance)SkeletalDescriptorScenarios.DumpGeneratedAppearance(content.Character);
                if(shaped)SkeletalDescriptorScenarios.DumpBlendShapes(content.Character);
                if(normalDiffuse)
                {
                    SkeletalCompressedNormalScenarios.Probe(content.Character,overlay.Lookup);
                    if(vertexLitSpecular){SkeletalSpecularScenarios.Probe(content.Character);SkeletalVertexLitSpecularScenarios.Probe(content.Character,overlay.Lookup);}
                    var mb=content.Character.MaterialBundle!;
                    foreach(int requested in new[]{int.MinValue,0,1,2,3,int.MaxValue})
                    {
                        var selected=SkeletalTextureChoices.Apply(mb,textureValues!.Keys.ToDictionary(k=>k,_=>requested));
                        foreach(var pair in selected.TextureSelection!.Selections)Console.WriteLine("skeletal_texture_choice="+JsonSerializer.Serialize(new{path=pair.Key,requested,selections=pair.Value,textures=selected.TextureSelection.References[pair.Key].Textures}));
                    }
                }
                if(animateCharacter)
                {
                    var animation=content.Character.PreparedAnimation??throw new InvalidDataException("Prepared scene animation absent.");var selection=content.Character.SelectedAnimation!;
                    Console.WriteLine("interior_character_animation="+JsonSerializer.Serialize(new{table=selection.Selection.TablePath,name=selection.Selection.LogicalName,path=selection.Proxy.ClipPath,animation.FrameRate,animation.EndFrame,unused=animation.UnusedAnimationTransforms,missing=animation.MissingSkeletonTransforms,animation.RepairedPackedValues}));
                    HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(_=>throw new Exception("Invalid animation selection reached lookup."),bytes,args[7],tablePin,bundle.LayoutPath,bundle.Sources[bundle.LayoutPath].Sha256,characterAnimationSelection:LogicalAnimationProxyScenarios.IdleSelection));
                }
                HostAccountingScenarios.Reject<InvalidDataException>(()=>InteriorSceneContentBundleReader.Load(overlay.Lookup,bytes,args[7],tablePin,bundle.LayoutPath,bundle.Sources[bundle.LayoutPath].Sha256,
                    actorTemplatePath:actorPath,actorTemplateSha256:actorPin,characterAppearanceSha256:new string('0',64)));
            }
            HostAccountingScenarios.Reject<InvalidDataException>(()=>InteriorSceneContentBundleReader.Load(overlay.Lookup,bytes,args[7],tablePin,
                bundle.LayoutPath,bundle.Sources[bundle.LayoutPath].Sha256,actorTemplatePath:actorPath,actorTemplateSha256:new string('0',64)));
        }
        if(chaseCamera){ChaseCameraScenarios.Probe(content!);PortalCameraCellScenarios.Probe(content!);PortalCellLightScenarios.Probe(content!);}
        var guard=content?.TriangleGuard;
        if(guardTriangles!=(guard is not null))throw new InvalidDataException("Requested guard was not delivered.");
        var triangles=guard?.Geometry.ToDictionary(p=>p.Key,p=>p.Value.Triangles);
        if(guard is not null)
        {
            foreach(var pair in guard.Geometry)Console.WriteLine("interior_sphere_guard_geometry="+JsonSerializer.Serialize(new{cell=pair.Key,selector=guard.Selector,
                source=pair.Value.SourceTriangleCount,eligible=pair.Value.EligibleTriangleCount,degenerate=pair.Value.DegenerateTriangleCount,triangles=pair.Value.Triangles.Count,
                sha256=StaticMeshCollisionVerification.ComputeSha256(pair.Value)}));
        }
        var fallback=profile.Collision.CreateFallbackSphere();var volume=new ActorSphereVolume(fallback.LocalCenter,fallback.Radius);
        if(bundle.Doorways.Count!=2)throw new InvalidDataException("Sphere route requires two explicit doorways.");
        var first=bundle.Doorways[0].ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true));
        var last=bundle.Doorways[1].ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true));
        foreach(var source in bundle.Sources.Values)Console.WriteLine("interior_sphere_source="+JsonSerializer.Serialize(source));
        foreach(var source in profile.Sources.Values)Console.WriteLine("interior_sphere_template_source="+JsonSerializer.Serialize(source));
        Console.WriteLine("interior_sphere_volume="+JsonSerializer.Serialize(new{center_offset=InteriorDoorScenarios.V(volume.WorldCenterOffset),radius=volume.Radius}));
        int totalTicks=0,totalBlocks=0;long totalTransfers=0;var timings=new List<double>();
        foreach(bool transformed in new[]{false,true})
        {
            var owner=transformed?new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
            using var scene=new InteriorDoorActorScene(bundle.Layout,bundle.Floors,bundle.Bindings,first.Start,owner,holdClosingLeaves:true,actorSphere:volume,
                staticTriangles:triangles,maximumTriangles:65536);
            int second=scene.Links.First(l=>l.SourceCell==bundle.Doorways[1].Cell&&l.SourcePortal==bundle.Doorways[1].Portal).GeometryIndex;
            void Phase(string phase,int count,Vector3 target,bool? allowed,bool reset=false)
            {
                if(reset)scene.Reset();
                for(int i=0;i<count;i++)
                {
                    Vector3 local=target-scene.State.Locator.LocalPosition;local.Y=0;float length=local.Length();if(length>.08f)local*=.08f/length;
                    long began=Stopwatch.GetTimestamp();
                    var state=scene.Tick(.05f,owner.TransformDirection(local),l=>new(l.GeometryIndex==second?allowed:true,true));
                    timings.Add(Stopwatch.GetElapsedTime(began).TotalMilliseconds);
                    if(state.Movement!.Movement.Status==CellFloorWalkStatus.InvalidStart||state.Movement.Steps!=0||state.Movement.Movement.Transfers>1)throw new InvalidDataException("Sphere scene reusable bounded tick.");
                    if(state.Movement.Contact is not null)totalBlocks++;totalTicks++;totalTransfers+=state.Movement.Movement.Transfers;
                    Console.WriteLine("interior_sphere_tick="+JsonSerializer.Serialize(new{phase,transformed,index=i,allowed,cell=state.Locator.CellIndex,triangle=state.Locator.TriangleIndex,local=InteriorDoorScenarios.V(state.Locator.LocalPosition),world=InteriorDoorScenarios.V(state.WorldPosition),parameter=state.Movement.Movement.Parameter,transfers=state.Movement.Movement.Transfers,status=state.Movement.Movement.Status.ToString(),kind=state.Movement.ContactKind?.ToString(),diagnostic=state.Movement.Diagnostic,doors=state.Doors.OrderBy(p=>p.Key).Select(p=>new{geometry=p.Key,fraction=p.Value.Controller.Fraction,held=p.Value.ClosingHeld,retained=p.Value.RetainedActors})}));
                }
            }
            void Reached(int cell,Vector3 point)
            {if(scene.State.Locator.CellIndex!=cell||Vector3.Distance(scene.State.Locator.LocalPosition,point)>1e-4)throw new InvalidDataException("Original sphere route did not reach requested owner/endpoint: "+scene.State.Locator);}
            for(int trip=0;trip<2;trip++)
            {Phase("forward",200,last.End,true);Reached(bundle.Doorways[1].TargetCell,last.End);Phase("reverse",200,first.Start.LocalPosition,true);Reached(first.Start.CellIndex,first.Start.LocalPosition);}
            Phase("deny",200,last.End,false,true);
            if(scene.State.Locator.CellIndex!=bundle.Doorways[1].Cell||scene.State.TotalTransfers!=1)throw new InvalidDataException("Original sphere denial gained target entry.");
            Phase("retreat",20,last.Start.LocalPosition,false);
            Phase("unknown",20,last.End,null);
            if(scene.State.Locator.CellIndex!=bundle.Doorways[1].Cell||scene.State.TotalTransfers!=1)throw new InvalidDataException("Original sphere unknown permission gained entry.");
            Phase("recover",200,last.End,true);Reached(bundle.Doorways[1].TargetCell,last.End);
            Phase("return",200,first.Start.LocalPosition,true);Reached(first.Start.CellIndex,first.Start.LocalPosition);
        }
        if(guard is not null)
        {
            bool checkedBudget=false;var traceQuery=new PortalFloorTransferQuery(bundle.Layout,bundle.Floors);
            foreach(float z in new[]{48f,50f,52f,54f,56f,57f,58f})
            {
                var floor=bundle.Floors[first.Start.CellIndex];
                var probe=new FloorSurfaceQuery(floor).IntersectSegment(new(-28.349918f,20,z),new(-28.349918f,16,z));if(probe is null)continue;
                var initial=new CellFloorLocator(first.Start.CellIndex,probe.TriangleIndex,probe.Position);
                var path=traceQuery.Trace(initial,last.End,_=>new(true,true));int segments=path.Floors.Sum(p=>p.Path.Segments.Count);
                if((long)segments*guard.Triangles<=262144)continue;
                using var bounded=new InteriorDoorActorScene(bundle.Layout,bundle.Floors,bundle.Bindings,initial,actorSphere:volume,staticTriangles:triangles,maximumTriangles:65536);
                var before=bounded.State;
                HostAccountingScenarios.Reject<InvalidDataException>(()=>bounded.Tick(.05f,last.End-initial.LocalPosition,_=>new(true,true)));
                if(!ReferenceEquals(before,bounded.State)||bounded.State.Doors.Values.Any(d=>d.Controller.Fraction!=0||d.RetainedActors!=0))throw new InvalidDataException("Guard work failure mutated scene.");
                Console.WriteLine($"PASS original sphere guard budget rejected before mutation segments={segments} triangles={guard.Triangles}");checkedBudget=true;break;
            }
            if(!checkedBudget)throw new InvalidDataException("Original guard work-prefix fixture unavailable.");
            timings.Sort();Console.WriteLine("interior_sphere_guard_timing="+JsonSerializer.Serialize(new{ticks=timings.Count,mean_ms=timings.Average(),p95_ms=timings[(int)(timings.Count*.95)],maximum_ms=timings[^1],includes_controller_and_joint_landing=true}));
        }
        Console.WriteLine($"PASS original persistent sphere ticks={totalTicks} transfers={totalTransfers} contacts={totalBlocks} owner_poses=2 closing_guard=enclosing_cube");
    }
}
