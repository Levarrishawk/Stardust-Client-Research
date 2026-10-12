using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record WorldSnapshotNode(int Id,int ContainerId,int TemplateIndex,int CellIndex,
    Quaternion Rotation,Vector3 Position,float Radius,uint PortalLayoutCrc,int? ParentId);

/// <summary>Bounded original WSNP0001/NODE0000 input; authored parent-relative transforms only.</summary>
public sealed record WorldSnapshotInput(IReadOnlyList<string> Templates,IReadOnlyList<WorldSnapshotNode> Nodes,string Sha256)
{
    public static WorldSnapshotInput Decode(ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length>8*1024*1024)throw new InvalidDataException("Snapshot byte budget.");
        var root=IffDocument.Parse(bytes,new(8*1024*1024,100000,32)).Root;
        Need(root.IsForm("WSNP")&&root.Children.Count==1&&root.Children[0].IsForm("0001"),"WSNP0001 required.");
        var records=root.Children[0].Children;
        Need(records.Count==2&&records[0].IsForm("NODS")&&records[1].Tag=="OTNL"&&records[1].FormType is null,"Ordered NODS/OTNL required.");
        byte[] names=records[1].Payload.ToArray();int cursor=0;int count=Int(names,ref cursor);
        Need(count is >0 and <=4096,"Template count budget.");var templates=new string[count];
        for(int i=0;i<count;i++)
        {
            int end=names.AsSpan(cursor).IndexOf((byte)0);Need(end is >0 and <=1024,"Bounded template string.");
            var raw=names.AsSpan(cursor,end);foreach(byte b in raw)Need(b is >=32 and <=126,"ASCII template path.");
            string path=Encoding.ASCII.GetString(raw);Need(path==LegacyAssetPath.Normalize(path)&&path.StartsWith("object/")&&path.EndsWith(".iff")&&!path.Split('/').Any(p=>p is "." or ".."),"Normalized object template required.");
            templates[i]=path;cursor+=end+1;
        }
        Need(cursor==names.Length,"Exact OTNL completion.");var nodes=new List<WorldSnapshotNode>();var ids=new HashSet<int>();
        void Visit(IffNode node,int? parent,int depth)
        {
            Need(depth<=24&&nodes.Count<30000,"Snapshot node/depth budget.");
            Need(node.IsForm("NODE")&&node.Children.Count==1&&node.Children[0].IsForm("0000"),"NODE0000 required.");
            var parts=node.Children[0].Children;Need(parts.Count>0&&parts[0].Tag=="DATA"&&parts[0].FormType is null&&parts[0].Payload.Length==52,"Exact NODE DATA52.");
            byte[] raw=parts[0].Payload.ToArray();int at=0;
            int id=Int(raw,ref at),container=Int(raw,ref at),template=Int(raw,ref at),cell=Int(raw,ref at);
            Need(id!=0&&ids.Add(id)&&template>=0&&template<count&&cell>=0,"Unique node identity/template/cell.");
            Need(parent is null||container==parent,"Child containment identity.");
            float w=Float(raw,ref at),x=Float(raw,ref at),y=Float(raw,ref at),z=Float(raw,ref at);
            var rotation=new Quaternion(x,y,z,w); // Retain raw quaternion; operational eligibility belongs to the selected scene.
            var position=new Vector3(Float(raw,ref at),Float(raw,ref at),Float(raw,ref at));float radius=Float(raw,ref at);Need(radius>=0,"Nonnegative snapshot radius.");
            uint crc=unchecked((uint)Int(raw,ref at));nodes.Add(new(id,container,template,cell,rotation,position,radius,crc,parent));
            foreach(var child in parts.Skip(1))Visit(child,id,depth+1);
        }
        foreach(var node in records[0].Children)Visit(node,null,0);
        return new(Array.AsReadOnly(templates),nodes.AsReadOnly(),Convert.ToHexString(SHA256.HashData(bytes)));
    }
    private static int Int(byte[] raw,ref int at){Need(raw.Length-at>=4,"Truncated snapshot word.");int v=BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(at));at+=4;return v;}
    private static float Float(byte[] raw,ref int at){float v=BitConverter.Int32BitsToSingle(Int(raw,ref at));Need(float.IsFinite(v),"Finite snapshot transform.");return v;}
    private static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
}
