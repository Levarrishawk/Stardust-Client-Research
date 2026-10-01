using System.Numerics;
using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;
internal static class FloorWalkScenarios
{
    private static FloorMeshDescription Fixture()
    {
        FloorMeshDescription basis=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());
        Vector3[] vertices={new(0,0,0),new(2,2,0),new(0,0,2),new(2,2,2)};
        FloorTriangle a=new(new[]{0,1,2},1,new[]{-1,1,-1},Vector3.UnitY,new byte[]{0,1,0},0,0,new[]{-1,-1,-1});
        FloorTriangle b=new(new[]{1,3,2},2,new[]{-1,-1,0},Vector3.UnitY,new byte[]{0,0,1},0,0,new[]{-1,-1,-1});
        return basis with{Vertices=vertices,Triangles=new[]{a,b}};
    }
    internal static void Run()
    {
        FloorMeshDescription floor=Fixture();Vector3 start=new(.25f,.25f,.25f),end=new(1.75f,900,1.75f);
        SingleCellFloorWalker walker=new(floor);FloorWalkResult result=walker.Walk(0,start,end);
        Require(result.Status==FloorWalkStatus.Accepted && result.TriangleIndex==1 && result.Crossings==1 && result.Position==new Vector3(1.75f,1.75f,1.75f),"Adjacency/slope projection");
        Require(walker.Walk(1,result.Position,start).Status==FloorWalkStatus.Accepted,"Reverse adjacency");
        Require(walker.Walk(0,start,new(-1,0,.25f)).Status==FloorWalkStatus.BlockedEdge,"Blocked boundary");
        Require(walker.Walk(0,start,new(1,0,1)).Status==FloorWalkStatus.Accepted,"Closed edge endpoint");
        Require(walker.Walk(0,new(1,1,1),end).Status==FloorWalkStatus.Accepted,"Zero parameter crossing");
        Require(walker.Walk(0,start,end,0).Status==FloorWalkStatus.TraversalLimit,"Bounded crossing");
        Require(walker.Walk(0,start,new(-1,0,-1)).Status==FloorWalkStatus.AmbiguousVertex,"Vertex ambiguity");
        Require(walker.Walk(0,start with{Y=5},end).Status==FloorWalkStatus.InvalidStart,"Off-plane start");
        Require(walker.Walk(-1,start,end).Status==FloorWalkStatus.InvalidStart,"Missing locator");
        FloorTriangle a=floor.Triangles[0];
        FloorWalkResult Modified(FloorTriangle changed)=>new SingleCellFloorWalker(floor with{Triangles=new[]{changed,floor.Triangles[1]}}).Walk(0,start,end);
        Require(Modified(a with{EdgeFlags=new byte[]{0,0,0}}).Status==FloorWalkStatus.BlockedEdge,"Interior blocked edge");
        Require(Modified(a with{EdgeFlags=new byte[]{0,0,0},PortalIds=new[]{-1,17,-1}}).Status==FloorWalkStatus.Accepted,"Portal priority with same-floor neighbor");
        result=Modified(a with{EdgeFlags=new byte[]{0,0,0},PortalIds=new[]{-1,17,-1},Neighbors=new[]{-1,-1,-1}});
        Require(result.Status==FloorWalkStatus.PortalBoundary && result.PortalId==17 && Math.Abs(result.Parameter-.5)<0.0001 && result.Position==new Vector3(1,1,1),"Portal priority/partial stop");
        Require(Modified(a with{EdgeFlags=new byte[]{0,2,0}}).Status==FloorWalkStatus.UnsupportedEdgePolicy,"Special edge continuation");
        Require(Modified(a with{Neighbors=new[]{-1,-1,-1}}).Status==FloorWalkStatus.OpenBoundary,"Open floor boundary");
        Require(Modified(a with{Neighbors=new[]{-1,0,-1}}).Status==FloorWalkStatus.InvalidTopology,"Self-neighbor");
        Require(Modified(a with{NonSolidByte=1}).Status==FloorWalkStatus.UnsupportedSurface,"Non-solid floor");
        FloorTriangle bad=floor.Triangles[1] with{Neighbors=new[]{-1,-1,-1}};
        Require(new SingleCellFloorWalker(floor with{Triangles=new[]{a,bad}}).Walk(0,start,end).Status==FloorWalkStatus.InvalidTopology,"Missing reciprocal edge");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(0,new(float.NaN,0,0),end));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>walker.Walk(0,start,end,4097));
        int[] mutable={-1,1,-1};walker=new(floor with{Triangles=new[]{a with{Neighbors=mutable},floor.Triangles[1]}});mutable[1]=-1;
        Require(walker.Walk(0,start,end).Status==FloorWalkStatus.Accepted,"Owned topology");
        Require(walker.Walk(0,start,start).Position==start,"Deterministic no-op");
        Require(walker.Walk(0,new(8,8,8),end).Status==FloorWalkStatus.InvalidStart,"Outside start");
        Require(new SingleCellFloorWalker(floor with{Triangles=new[]{a with{VertexIndices=new[]{0,0,0},Neighbors=new[]{-1,-1,-1}}}}).Walk(0,start,end).Status==FloorWalkStatus.UnsupportedSurface,"Degenerate height field");
        Require(new SingleCellFloorWalker(floor with{Vertices=new Vector3[]{new(0,0,0),new(0,1,0),new(1,0,0)},Triangles=new[]{a with{Neighbors=new[]{-1,-1,-1}}}}).Walk(0,Vector3.Zero,end).Status==FloorWalkStatus.UnsupportedSurface,"Vertical height field");
        Require(new SingleCellFloorWalker(floor with{Vertices=new Vector3[]{new(0,0,0),new(0,1,1e-6f),new(1,0,0)},Triangles=new[]{a with{Neighbors=new[]{-1,-1,-1}}}}).Walk(0,Vector3.Zero,end).Status==FloorWalkStatus.UnsupportedSurface,"Near-vertical height field");
        List<Vector3> gridVertices=new();for(int z=0;z<=8;z++)for(int x=0;x<=8;x++)gridVertices.Add(new(x,x*.5f,z));
        List<FloorTriangle> gridTriangles=new();Dictionary<(int,int),(int,int)> edges=new();
        for(int z=0;z<8;z++)for(int x=0;x<8;x++)
        {
            int i=z*9+x;
            foreach(int[] indices in new[]{new[]{i,i+1,i+9},new[]{i+1,i+10,i+9}})
                gridTriangles.Add(a with{Id=(uint)gridTriangles.Count,VertexIndices=indices,Neighbors=new[]{-1,-1,-1},EdgeFlags=new byte[3],PortalIds=new[]{-1,-1,-1}});
        }
        for(int i=0;i<gridTriangles.Count;i++)for(int e=0;e<3;e++)edges.Add((gridTriangles[i].VertexIndices[e],gridTriangles[i].VertexIndices[(e+1)%3]),(i,e));
        for(int i=0;i<gridTriangles.Count;i++)for(int e=0;e<3;e++)
            if(edges.TryGetValue((gridTriangles[i].VertexIndices[(e+1)%3],gridTriangles[i].VertexIndices[e]),out var other)){((int[])gridTriangles[i].Neighbors)[e]=other.Item1;((byte[])gridTriangles[i].EdgeFlags)[e]=1;}
        walker=new(floor with{Vertices=gridVertices,Triangles=gridTriangles});Random random=new(20260930);int multiple=0;
        for(int i=0;i<500;i++)
        {
            float x=(float)(random.NextDouble()*8),z=(float)(random.NextDouble()*8),tx=(float)(random.NextDouble()*8),tz=(float)(random.NextDouble()*8);
            int triangle=((int)z*8+(int)x)*2+(x-(int)x+z-(int)z>1?1:0);
            result=walker.Walk(triangle,new(x,x*.5f,z),new(tx,0,tz));
            Require(result.Status==FloorWalkStatus.Accepted && result.Position==new Vector3(tx,tx*.5f,tz),"Seeded multi-triangle analytic plane");multiple+=result.Crossings>1?1:0;
        }
        Require(multiple>400,"Multi-triangle coverage");
        Console.WriteLine("PASS: single-cell floor movement, adjacency, slopes, boundary/portal priority, unsupported continuation, malformed topology and owned cleanup-free results");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Floor walk: "+label);}
    internal static void ProbeOptional(string[] args,bool trace=false,bool boxes=false,bool steps=false,bool stepMovement=false,bool localRay=false)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--fixtures" || args[6]!="--expected-sha256")throw new ArgumentException("Floor walks require pinned fixtures/configuration.");
        if(new FileInfo(args[5]).Length>32_000_000)throw new InvalidDataException("Floor walk fixture budget.");byte[] bytes=File.ReadAllBytes(args[5]);
        Require(Convert.ToHexString(SHA256.HashData(bytes)).Equals(args[7],StringComparison.OrdinalIgnoreCase),"Fixture identity");
        using JsonDocument document=JsonDocument.Parse(bytes);Require(document.RootElement.GetArrayLength()<=10000,"Fixture count");
        using StreamReader configuration=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],configuration);
        Vector3 Vector(JsonElement row)=>new(row[0].GetSingle(),row[1].GetSingle(),row[2].GetSingle());
        foreach(JsonElement fixture in document.RootElement.EnumerateArray())
        {
            string member=fixture.GetProperty("member").GetString()!;TreLookupResult source=overlay.Lookup(member);
            Require(source.Kind==TreLookupKind.Present && source.Payload is not null,"Source availability");
            Require(Convert.ToHexString(SHA256.HashData(source.Payload!))==fixture.GetProperty("sha256").GetString(),"Source identity");
            FloorMeshDescription description=FloorMeshDecoder.Decode(source.Payload!);SingleCellFloorWalker walker=new(description);int index=0;
            Require(fixture.GetProperty("queries").GetArrayLength()<=100,"Query count");
            foreach(JsonElement query in fixture.GetProperty("queries").EnumerateArray())
            {
                int initialTriangle=query.GetProperty("triangle").GetInt32();Vector3 start=Vector(query.GetProperty("start")),end=Vector(query.GetProperty("end"));
                FloorWalkPath? path=trace||steps?walker.Trace(initialTriangle,start,end):null;
                FloorWalkResult result=path?.Result??walker.Walk(initialTriangle,start,end);
                if(path is not null)Require(result==walker.Walk(initialTriangle,start,end),"Trace result equivalence");
                FloorWalkResult resumed=walker.Walk(result.TriangleIndex,result.Position,result.Position);
                Console.WriteLine("walk="+JsonSerializer.Serialize(new{member,index=index++,status=result.Status.ToString(),triangle=result.TriangleIndex,parameter=result.Parameter,edge=result.EdgeIndex,portal=result.PortalId,position=new[]{result.Position.X,result.Position.Y,result.Position.Z},resume_status=resumed.Status.ToString()}));
                if(path is not null)Console.WriteLine("floor_path="+JsonSerializer.Serialize(new{member,index=index-1,status=result.Status.ToString(),parameter=result.Parameter,position=InteriorDoorScenarios.V(result.Position),segments=path.Segments.Select(s=>new{triangle=s.TriangleIndex,start_parameter=s.StartParameter,end_parameter=s.EndParameter,start=new[]{s.Start.X,s.Start.Y,s.Start.Z},end=new[]{s.End.X,s.End.Y,s.End.Z}})}));
                if(steps)
                {
                    var stepPolicy=new FloorStepPolicy(UseLocalRayCoverage:localRay);var step=new FloorStepTransitionQuery(description).Probe(path!,end,stepPolicy);
                    var reuse=walker.Walk(step.TargetTriangle,step.Landing,step.Landing);
                    Console.WriteLine("step_proposal="+JsonSerializer.Serialize(new{member,index=index-1,status=step.Status.ToString(),triangle=step.TargetTriangle,landing=InteriorDoorScenarios.V(step.Landing),source_contact=new[]{step.SourceContact.X,step.SourceContact.Y,step.SourceContact.Z},target_contact=new[]{step.TargetContact.X,step.TargetContact.Y,step.TargetContact.Z},strip=step.StripTriangles,resume_status=reuse.Status.ToString()}));
                    if(stepMovement && step.Status==FloorStepStatus.Accepted)
                    {
                        Vector3 half=new(.25f,.9f,.25f);var sourceFeet=step.SourceContact.ToVector3();double rise=step.TargetContact.Y-step.SourceContact.Y;
                        float y=(float)(step.SourceContact.Y+rise*.5+(rise>0?half.Y*2:0));
                        var blocker=new PortalBarrierCollider(new(sourceFeet.X-.1f,y-(rise>0?0:.01f),sourceFeet.Z-.1f),new(sourceFeet.X+.1f,y+(rise>0?.01f:0),sourceFeet.Z+.1f),AppearanceTransform.Identity);
                        foreach(string mode in new[]{"clear","blocked","unknown","permitted"})
                        {
                            var colliders=mode=="clear"?Array.Empty<PortalBarrierCollider>():new[]{blocker};var mover=new SingleCellFloorStepBoxWalker(description,colliders,stepPolicy);
                            var moved=mover.Walk(initialTriangle,start,end,half,_=>mode=="unknown"?null:mode=="permitted",true);
                            var floorResult=moved.Movement.Floor;var reusable=walker.Walk(floorResult.TriangleIndex,floorResult.Position,floorResult.Position);
                            double drift=0;for(int repeat=0;repeat<20 && moved.Steps==0;repeat++)
                            {
                                var again=mover.Walk(floorResult.TriangleIndex,floorResult.Position,end,half,_=>mode=="unknown"?null:mode=="permitted",true);
                                drift=Math.Max(drift,Vector3.Distance(again.Movement.Floor.Position,floorResult.Position));
                            }
                            Console.WriteLine("step_move="+JsonSerializer.Serialize(new{member,index=index-1,mode,steps=moved.Steps,status=moved.Movement.Status.ToString(),floor_status=floorResult.Status.ToString(),triangle=floorResult.TriangleIndex,parameter=floorResult.Parameter,position=InteriorDoorScenarios.V(floorResult.Position),resume_status=reusable.Status.ToString(),diagnostic=moved.Diagnostic,repeat_drift=drift,contact_status=moved.Movement.Contact?.Status.ToString(),minimum=InteriorDoorScenarios.V(blocker.Minimum),maximum=InteriorDoorScenarios.V(blocker.Maximum)}));
                        }
                        var roundtrip=new SingleCellFloorStepBoxWalker(description,Array.Empty<PortalBarrierCollider>(),stepPolicy);int stepCount=0;double returnDistance=0;bool valid=true;var current=new FloorWalkResult(FloorWalkStatus.Accepted,initialTriangle,start,1,-1,-1,0);
                        for(int trip=0;trip<20;trip++)
                        {
                            var outbound=roundtrip.Walk(current.TriangleIndex,current.Position,end,half,_=>true,true);stepCount+=outbound.Steps;
                            var inbound=roundtrip.Walk(outbound.Movement.Floor.TriangleIndex,outbound.Movement.Floor.Position,start,half,_=>true,true);stepCount+=inbound.Steps;current=inbound.Movement.Floor;
                            valid&=outbound.Movement.Status==FloorBoxWalkStatus.Accepted&&inbound.Movement.Status==FloorBoxWalkStatus.Accepted&&walker.Walk(current.TriangleIndex,current.Position,current.Position).Status==FloorWalkStatus.Accepted;
                            returnDistance=Math.Max(returnDistance,Vector3.Distance(current.Position,start));
                        }
                        Console.WriteLine("step_roundtrip="+JsonSerializer.Serialize(new{member,index=index-1,trips=20,steps=stepCount,valid,return_distance=returnDistance}));
                    }
                }
                if(boxes)
                {
                    var row=query.GetProperty("box");var p=row.GetProperty("pose");var pose=new AppearanceTransform(Vector(p[0]),Vector(p[1]),Vector(p[2]),Vector(p[3]));
                    var collider=new PortalBarrierCollider(Vector(row.GetProperty("minimum")),Vector(row.GetProperty("maximum")),pose);
                    Vector3 half=Vector(row.GetProperty("half"));var boxWalker=new SingleCellFloorBoxWalker(description,new[]{collider});var boxed=boxWalker.Walk(initialTriangle,start,end,half,_=>false,true);
                    var resume=walker.Walk(boxed.Floor.TriangleIndex,boxed.Floor.Position,boxed.Floor.Position);
                    var repeated=boxed;double repeatDistance=0;bool repeatReusable=true;
                    for(int step=0;step<20;step++)
                    {
                        repeated=boxWalker.Walk(repeated.Floor.TriangleIndex,repeated.Floor.Position,end,half,_=>false,true);
                        repeatDistance=Math.Max(repeatDistance,Vector3.Distance(repeated.Floor.Position,boxed.Floor.Position));
                        repeatReusable &= walker.Walk(repeated.Floor.TriangleIndex,repeated.Floor.Position,repeated.Floor.Position).Status==FloorWalkStatus.Accepted;
                    }
                    var retreat=boxWalker.Walk(repeated.Floor.TriangleIndex,repeated.Floor.Position,start,half,_=>false,true);
                    Console.WriteLine("floor_box_corpus="+JsonSerializer.Serialize(new{member,index=index-1,status=boxed.Status.ToString(),floor_status=boxed.Floor.Status.ToString(),triangle=boxed.Floor.TriangleIndex,parameter=boxed.Floor.Parameter,position=InteriorDoorScenarios.V(boxed.Floor.Position),resume_status=resume.Status.ToString(),diagnostic=boxed.Diagnostic,
                        repeated_status=repeated.Status.ToString(),repeat_distance=repeatDistance,repeat_reusable=repeatReusable,retreat_status=retreat.Status.ToString(),retreat_source_distance=Vector3.Distance(retreat.Floor.Position,start),
                        contact=boxed.Contact is null?null:new{status=boxed.Contact.Status.ToString(),parameter=boxed.Contact.Parameter,segment=boxed.Contact.SegmentIndex,center=new[]{boxed.Contact.Center.X,boxed.Contact.Center.Y,boxed.Contact.Center.Z},normal=InteriorDoorScenarios.V(boxed.Contact.Normal)}}));
                }
            }
        }
    }
}
