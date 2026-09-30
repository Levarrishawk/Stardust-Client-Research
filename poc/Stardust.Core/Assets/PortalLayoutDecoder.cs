using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record PortalGeometry(IReadOnlyList<Vector3> Vertices,IReadOnlyList<uint> Indices,bool FromPolygon);
public sealed record PortalReference(string Version,byte? LeadingByte,byte SolidByte,byte WindingByte,int GeometryIndex,int TargetCell,
    string DoorName,byte? TransformFlag,AppearanceTransform? DoorTransform);
public sealed record PortalCell(string Version,string Name,byte RawFlag,string AppearancePath,string? FloorPath,
    IffNode? Bounds,IReadOnlyList<IffNode> Lights,IReadOnlyList<PortalReference> Portals);
public sealed record PortalSide(int CellIndex,int ReferenceIndex);
public sealed record PortalConnection(int GeometryIndex,IReadOnlyList<PortalSide> Sides);
public sealed record PortalLayoutDescription(string Version,IReadOnlyList<PortalGeometry> Geometry,IReadOnlyList<PortalCell> Cells,
    uint Crc,IffNode? PathGraph,IffNode Metadata,IReadOnlyList<PortalConnection> Connections,IReadOnlyList<string> Diagnostics);

/// <summary>Owned deployed PRTO structure; lights, bounds and path graph retain separate opaque metadata.</summary>
public static class PortalLayoutDecoder
{
    public static PortalLayoutDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode root=IffDocument.Parse(source).Root;
        Require(root.IsForm("PRTO") && root.Children.Count==1,"Invalid PRTO envelope.");
        IffNode version=root.Children[0];Require(version.FormType is "0001" or "0003" or "0004","Unsupported PRTO version.");
        IReadOnlyList<IffNode> parts=version.Children;
        Require(parts.Count is 4 or 5 && parts[0].Tag=="DATA" && parts[0].Payload.Length==8 && parts[1].IsForm("PRTS") && parts[2].IsForm("CELS"),"Invalid PRTO child layout.");
        uint geometries=U32(parts[0].Payload.Span),cells=U32(parts[0].Payload.Span[4..]);
        Require(geometries<=4096 && cells is >0 and <=4096 && geometries==parts[1].Children.Count && cells==parts[2].Children.Count,"PRTO count mismatch/budget.");
        List<PortalGeometry> geometry=new();long vertices=0,indices=0;
        foreach(IffNode entry in parts[1].Children)
        {
            PortalGeometry decoded=ReadGeometry(entry,version.FormType=="0004");vertices+=decoded.Vertices.Count;indices+=decoded.Indices.Count;
            Require(vertices<=1_000_000 && indices<=3_000_000,"PRTO aggregate geometry budget.");geometry.Add(decoded);
        }
        List<PortalCell> cellList=new();long references=0;
        foreach(IffNode entry in parts[2].Children)
        {PortalCell decoded=ReadCell(entry,(int)geometries,(int)cells);references+=decoded.Portals.Count;Require(references<=32768,"PRTO reference budget.");cellList.Add(decoded);}
        int cursor=3;IffNode? graph=null;
        if(parts[cursor].IsForm("PGRF")){Require(version.FormType!="0001","Path graph outside PRTO/0001 slice.");graph=parts[cursor++];}
        Require(cursor==parts.Count-1 && parts[cursor].Tag=="CRC " && parts[cursor].Payload.Length==4,"Invalid PRTO CRC/trailing data.");
        List<PortalConnection> connections=new();List<string> diagnostics=new();
        List<PortalSide>[] sideGroups=Enumerable.Range(0,geometry.Count).Select(_=>new List<PortalSide>()).ToArray();
        for(int cell=0;cell<cellList.Count;cell++)for(int portal=0;portal<cellList[cell].Portals.Count;portal++)
            sideGroups[cellList[cell].Portals[portal].GeometryIndex].Add(new(cell,portal));
        for(int index=0;index<geometry.Count;index++)
        {
            List<PortalSide> sides=sideGroups[index];
            if(sides.Count!=2)diagnostics.Add($"UnpairedGeometry:{index}:{sides.Count}");
            else for(int side=0;side<2;side++)
                if(cellList[sides[side].CellIndex].Portals[sides[side].ReferenceIndex].TargetCell!=sides[1-side].CellIndex)diagnostics.Add($"PortalTargetMismatch:{index}:{side}");
            connections.Add(new(index,sides.AsReadOnly()));
        }
        return new(version.FormType!,geometry.AsReadOnly(),cellList.AsReadOnly(),U32(parts[cursor].Payload.Span),graph,root,connections.AsReadOnly(),diagnostics.AsReadOnly());
    }
    private static PortalGeometry ReadGeometry(IffNode entry,bool indexed)
    {
        ReadOnlySpan<byte> raw;uint[] indices;
        if(indexed)
        {
            Require(entry.IsForm("IDTL") && entry.Children.Count==1 && entry.Children[0].IsForm("0000"),"Unsupported portal IDTL.");
            IReadOnlyList<IffNode> fields=entry.Children[0].Children;
            Require(fields.Count==2 && fields[0].Tag=="VERT" && fields[1].Tag=="INDX" && fields[1].Payload.Length%12==0,"Invalid indexed portal geometry.");
            raw=fields[0].Payload.Span;Require(fields[1].Payload.Length/4<=300_000,"Portal index budget.");indices=new uint[fields[1].Payload.Length/4];
            for(int index=0;index<indices.Length;index++)indices[index]=U32(fields[1].Payload.Span[(index*4)..]);
        }
        else
        {
            Require(entry.Tag=="PRTL" && entry.Payload.Length>=4,"Invalid polygon portal geometry.");uint count=U32(entry.Payload.Span);
            Require(count is >=3 and <=100_000 && entry.Payload.Length-4==(long)count*12,"Polygon count/extent mismatch.");raw=entry.Payload.Span[4..];
            indices=new uint[(count-2)*3];for(uint vertex=2;vertex<count;vertex++){int offset=(int)(vertex-2)*3;indices[offset]=0;indices[offset+1]=vertex-1;indices[offset+2]=vertex;}
        }
        Require(raw.Length%12==0 && raw.Length/12 is >=3 and <=100_000,"Portal vertex extent/budget.");Vector3[] vertices=new Vector3[raw.Length/12];
        for(int index=0;index<vertices.Length;index++)vertices[index]=new(Float(raw[(index*12)..]),Float(raw[(index*12+4)..]),Float(raw[(index*12+8)..]));
        Require(indices.All(index=>index<vertices.Length),"Portal index out of range.");return new(Array.AsReadOnly(vertices),Array.AsReadOnly(indices),!indexed);
    }
    private static PortalCell ReadCell(IffNode cell,int geometryCount,int cellCount)
    {
        Require(cell.IsForm("CELL") && cell.Children.Count==1 && cell.Children[0].FormType is "0004" or "0005","Unsupported CELL envelope/version.");
        IffNode version=cell.Children[0];IReadOnlyList<IffNode> fields=version.Children;
        Require(fields.Count>0 && fields[0].Tag=="DATA","Missing CELL DATA.");Reader data=new(fields[0].Payload.Span);
        uint count=data.UInt();Require(count<=4096,"CELL portal budget.");byte flag=data.Byte();string name=data.String(),appearance=data.String();byte floorFlag=data.Byte();
        string? floor=floorFlag!=0?data.String():null;data.End();int cursor=1;IffNode? bounds=null;
        if(version.FormType=="0005" && cursor<fields.Count && !fields[cursor].IsForm("PRTL") && fields[cursor].Tag!="LGHT")
        {Require(fields[cursor].Tag=="FORM","Unsupported CELL extent record.");bounds=fields[cursor++];}
        List<PortalReference> portals=new();
        for(int index=0;index<count;index++)
        {
            Require(cursor<fields.Count && fields[cursor].IsForm("PRTL") && fields[cursor].Children.Count==1,"Missing CELL portal.");IffNode record=fields[cursor++].Children[0];
            Require(record.Tag is "0002" or "0004" or "0005" && record.FormType is null,"Unsupported PRTL version chunk.");Reader portal=new(record.Payload.Span);
            byte? leading=record.Tag=="0005"?portal.Byte():null;byte solid=portal.Byte();int geometry=portal.Int();byte winding=portal.Byte();int target=portal.Int();
            Require(geometry>=0 && geometry<geometryCount && target>=0 && target<cellCount,"Portal geometry/target index out of range.");
            string door="";byte? transformFlag=null;AppearanceTransform? transform=null;
            if(record.Tag!="0002"){door=portal.String();transformFlag=portal.Byte();transform=portal.Transform();}
            portal.End();portals.Add(new(record.Tag,leading,solid,winding,geometry,target,door,transformFlag,transform));
        }
        List<IffNode> lights=new();while(cursor<fields.Count){Require(fields[cursor].Tag=="LGHT","Unexpected CELL trailing record.");lights.Add(fields[cursor++]);}
        return new(version.FormType!,name,flag,appearance,floor,bounds,lights.AsReadOnly(),portals.AsReadOnly());
    }
    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private ReadOnlySpan<byte> remaining=bytes;
        public byte Byte(){Require(remaining.Length>=1,"Truncated portal byte.");byte value=remaining[0];remaining=remaining[1..];return value;}
        public uint UInt(){Require(remaining.Length>=4,"Truncated portal integer.");uint value=U32(remaining);remaining=remaining[4..];return value;}
        public int Int()=>unchecked((int)UInt());
        public string String()
        {
            int end=remaining.IndexOf((byte)0);Require(end is >=0 and <=1024,"Invalid portal string extent.");
            foreach(byte value in remaining[..end])Require(value is >=32 and <=126,"Portal string outside ASCII slice.");
            string result=Encoding.ASCII.GetString(remaining[..end]);remaining=remaining[(end+1)..];return result;
        }
        public AppearanceTransform Transform()
        {
            Require(remaining.Length>=48,"Truncated portal transform.");float[] m=new float[12];for(int index=0;index<12;index++)m[index]=Float(remaining[(index*4)..]);remaining=remaining[48..];
            return new(new(m[0],m[1],m[2]),new(m[4],m[5],m[6]),new(m[8],m[9],m[10]),new(m[3],m[7],m[11]));
        }
        public void End()=>Require(remaining.IsEmpty,"Portal record trailing bytes.");
    }
    private static uint U32(ReadOnlySpan<byte> bytes)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    private static float Float(ReadOnlySpan<byte> bytes){float value=BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));Require(float.IsFinite(value),"Nonfinite portal geometry/transform.");return value;}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
