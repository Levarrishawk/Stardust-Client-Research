using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class PortalLayoutScenarios
{
    internal static void Run()
    {
        foreach(string version in new[]{"0001","0003","0004"})foreach(string cellVersion in new[]{"0004","0005"})foreach(string portalVersion in new[]{"0002","0004","0005"})
        {
            byte[] bytes=Fixture(version,cellVersion,portalVersion);PortalLayoutDescription layout=PortalLayoutDecoder.Decode(bytes);
            if(layout.Cells.Count!=2 || layout.Geometry.Count!=1 || layout.Crc!=0x12345678 || layout.Diagnostics.Count!=0 ||
                layout.Connections[0].Sides.Count!=2 || layout.Cells[1].Portals[0].TargetCell!=0 || layout.Cells[0].Name!="r0" || layout.Cells[1].FloorPath!="appearance/collision/test.flr")throw new Exception("Portal named-cell/adjacency fixture mismatch.");
            if(portalVersion!="0002" && (layout.Cells[1].Portals[0].TransformFlag!=0 || layout.Cells[1].Portals[0].DoorTransform?.TransformPoint(new(1,2,3))!=new Vector3(0,4,7)))throw new Exception("Portal transform stored despite false flag mismatch.");
            Array.Clear(bytes);if(layout.Geometry[0].Vertices[1]!=new Vector3(1,-1,0))throw new Exception("Portal geometry borrowed source.");
        }
        byte[] input=Fixture();for(int length=0;length<input.Length;length++)Reject(()=>PortalLayoutDecoder.Decode(input.AsSpan(0,length)));
        Reject(()=>PortalLayoutDecoder.Decode(input.Concat(new byte[]{0}).ToArray()));
        Reject(()=>PortalLayoutDecoder.Decode(Fixture(badIndex:true)));
        Reject(()=>PortalLayoutDecoder.Decode(Fixture(badTarget:true)));
        Reject(()=>PortalLayoutDecoder.Decode(Fixture(nonfinite:true)));
        Reject(()=>PortalLayoutDecoder.Decode(Fixture(badCount:true)));
        if(PortalLayoutDecoder.Decode(Fixture(targetMismatch:true)).Diagnostics.Count!=1)throw new Exception("Portal target inconsistency was silently ignored.");
        Console.WriteLine("PASS: owned PRTO polygon/indexed geometry, named cells, optional bounds, portal/door fields, paired adjacency and malformed fixtures");
    }
    internal static byte[] Fixture(string version="0004",string cellVersion="0005",string portalVersion="0005",bool badIndex=false,bool badTarget=false,bool nonfinite=false,bool badCount=false,bool targetMismatch=false,bool actualGraph=false)
    {
        byte[] Payload(Action<BinaryWriter> write){using MemoryStream stream=new();using BinaryWriter writer=new(stream);write(writer);return stream.ToArray();}
        void String(BinaryWriter writer,string value){writer.Write(Encoding.ASCII.GetBytes(value));writer.Write((byte)0);}
        byte[] positions=Payload(writer=>{foreach(float value in new[]{-1f,-1,0,1,-1,0,0,nonfinite?float.NaN:1,0})writer.Write(value);});
        byte[] geometry=version=="0004"?MeshScenarios.Form("IDTL",MeshScenarios.Form("0000",MeshScenarios.Chunk("VERT",positions),MeshScenarios.Chunk("INDX",Payload(writer=>{writer.Write(0u);writer.Write(1u);writer.Write(badIndex?3u:2u);})))):
            MeshScenarios.Chunk("PRTL",Payload(writer=>{writer.Write(3u);writer.Write(positions);}));
        byte[] Cell(int index)
        {
            byte[] data=MeshScenarios.Chunk("DATA",Payload(writer=>{writer.Write(1u);writer.Write((byte)3);String(writer,index==0?"r0":"hall");String(writer,"appearance/test.apt");writer.Write((byte)(index==0?0:1));if(index!=0)String(writer,"appearance/collision/test.flr");}));
            byte[] portal=MeshScenarios.Form("PRTL",MeshScenarios.Chunk(portalVersion,Payload(writer=>
            {
                if(portalVersion=="0005")writer.Write((byte)1);writer.Write((byte)2);writer.Write(0);writer.Write((byte)1);writer.Write(badTarget?2:targetMismatch&&index==0?0:1-index);
                if(portalVersion!="0002"){String(writer,"style");writer.Write((byte)0);foreach(float value in new[]{0f,-1,0,2,1,0,0,3,0,0,1,4})writer.Write(value);}
            })));
            // Exercise v0005 both with and without an explicit bound.
            byte[][] children=cellVersion=="0005" && index==1?new[]{data,MeshScenarios.Form("NULL"),portal,MeshScenarios.Chunk("LGHT",new byte[4])}:new[]{data,portal,MeshScenarios.Chunk("LGHT",new byte[4])};
            return MeshScenarios.Form("CELL",MeshScenarios.Form(cellVersion,children));
        }
        List<byte[]> children=new(){MeshScenarios.Chunk("DATA",Payload(writer=>{writer.Write(badCount?2u:1u);writer.Write(2u);})),MeshScenarios.Form("PRTS",geometry),MeshScenarios.Form("CELS",Cell(0),Cell(1))};
        if(version!="0001")children.Add(actualGraph?PathGraphScenarios.Fixture():MeshScenarios.Form("PGRF",MeshScenarios.Form("0001")));
        children.Add(MeshScenarios.Chunk("CRC ",Payload(writer=>writer.Write(0x12345678u))));return MeshScenarios.Form("PRTO",MeshScenarios.Form(version,children.ToArray()));
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--members" || args[6]!="--expected-members-sha256")throw new ArgumentException("Portal probe needs pinned member list/configuration.");
        if(new FileInfo(args[5]).Length>1_048_576)throw new InvalidDataException("Portal list budget.");
        byte[] manifest=File.ReadAllBytes(args[5]);if(!Convert.ToHexString(SHA256.HashData(manifest)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Portal member-list SHA mismatch.");
        string[] members=File.ReadAllLines(args[5]);if(members.Length is 0 or >1000 || members.Distinct(StringComparer.Ordinal).Count()!=members.Length)throw new InvalidDataException("Portal list count/duplicates.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        string? Hash(IffNode? node)=>node is null?null:Convert.ToHexString(SHA256.HashData(node.Payload.Span));
        string Floats(IEnumerable<float> fields){using MemoryStream stream=new();using BinaryWriter writer=new(stream);foreach(float field in fields)writer.Write(field);return Convert.ToHexString(stream.ToArray());}
        string? Transform(AppearanceTransform? t)=>t is null?null:Floats(new[]{t.Value.RowX.X,t.Value.RowX.Y,t.Value.RowX.Z,t.Value.Translation.X,t.Value.RowY.X,t.Value.RowY.Y,t.Value.RowY.Z,t.Value.Translation.Y,t.Value.RowZ.X,t.Value.RowZ.Y,t.Value.RowZ.Z,t.Value.Translation.Z});
        foreach(string member in members)
        {
            TreLookupResult selected=overlay.Lookup(member);if(selected.Kind!=TreLookupKind.Present || selected.Payload is null)throw new InvalidDataException("Portal source missing/masked.");
            PortalLayoutDescription layout=PortalLayoutDecoder.Decode(selected.Payload);
            Console.WriteLine("portal="+JsonSerializer.Serialize(new{member,priority=selected.Priority,sha256=Convert.ToHexString(SHA256.HashData(selected.Payload)),version=layout.Version,crc=layout.Crc,
                geometries=layout.Geometry.Select(value=>new{vertex_hex=Floats(value.Vertices.SelectMany(p=>new[]{p.X,p.Y,p.Z})),indices=value.Indices}),
                cells=layout.Cells.Select(cell=>new{version=cell.Version,name=cell.Name,raw_flag=cell.RawFlag,appearance=cell.AppearancePath,floor=cell.FloorPath,bounds_sha256=Hash(cell.Bounds),
                    lights_sha256=cell.Lights.Select(light=>Hash(light)),portals=cell.Portals.Select(portal=>new{version=portal.Version,leading_byte=portal.LeadingByte,solid_byte=portal.SolidByte,winding_byte=portal.WindingByte,
                        geometry_index=portal.GeometryIndex,target_cell=portal.TargetCell,door_name=portal.DoorName,transform_flag=portal.TransformFlag,transform_hex=Transform(portal.DoorTransform)})}),path_graph_sha256=Hash(layout.PathGraph)}));
        }
        Console.WriteLine($"PASS portal_inventory members={members.Length}");
    }
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
