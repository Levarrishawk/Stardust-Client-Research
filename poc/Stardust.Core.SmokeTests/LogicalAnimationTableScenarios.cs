using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class LogicalAnimationTableScenarios
{
    internal static void Run()
    {
        byte[] source=Fixture();var table=LogicalAnimationTableDecoder.Decode(source);Array.Clear(source);
        if(table.AssociatedHierarchy!="appearance/ash/body.ash"||table.SerializedEntries.Count!=2||table.SerializedEntries[0].SerializedName!="walk  "||table.Find("WALK").Name!="walk"||table.Find("missing")!=table.DefaultEntry||!table.DefaultEntry.Synthetic||table.DefaultEntry.Template is not null||table.Find("walk").Template!.FormType!="PXAT")throw new Exception("Owned logical table/index/fallback mismatch.");
        var authored=LogicalAnimationTableDecoder.Decode(Fixture(first:"default"));if(authored.DefaultEntry.Synthetic||authored.LookupEntries.Count!=2||authored.DefaultEntry.Template is null)throw new Exception("Authored logical default mismatch.");
        void Reject(Action a)=>HostAccountingScenarios.Reject<InvalidDataException>(a);
        byte[] valid=Fixture();for(int i=0;i<valid.Length;i++)Reject(()=>LogicalAnimationTableDecoder.Decode(valid.AsSpan(0,i)));
        Reject(()=>LogicalAnimationTableDecoder.Decode(Fixture(count:-1)));Reject(()=>LogicalAnimationTableDecoder.Decode(Fixture(count:4097)));
        var duplicate=LogicalAnimationTableDecoder.Decode(Fixture(first:"idle"));if(duplicate.SerializedEntries.Count!=2)throw new Exception("Duplicate serialized entries lost.");Reject(()=>duplicate.Find("idle"));
        var upperDefault=LogicalAnimationTableDecoder.Decode(Fixture(first:"DEFAULT"));if(!upperDefault.DefaultEntry.Synthetic)throw new Exception("Default literal detection must precede folding.");Reject(()=>upperDefault.Find("default"));
        Reject(()=>LogicalAnimationTableDecoder.Decode(Fixture(first:"   ")));Reject(()=>LogicalAnimationTableDecoder.Decode(Fixture(first:"wa\0lk")));
        Reject(()=>LogicalAnimationTableDecoder.Decode(Fixture(tail:true)));Reject(()=>LogicalAnimationTableDecoder.Decode(Fixture(version:"0001")));
        int reads=0;HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(_=>{reads++;return new(TreLookupKind.Missing);},"object/test.iff",new string('0',64),new string('0',64),loadLogicalTables:true));
        if(reads!=0)throw new Exception("Invalid logical table dependency reached source lookup.");
        Console.WriteLine("PASS: owned LATT 0000 entries/opaque typed forms, trailing ASCII spaces, authored/synthetic default, explicit folded lookup and malformed/dependency gates");
    }
    private static byte[] Fixture(short count=2,string first="walk  ",bool tail=false,string version="0000")
    {
        byte[] Text(string value)=>Encoding.ASCII.GetBytes(value+"\0");
        byte[] Entry(string name)=>MeshScenarios.Form("ANIM",MeshScenarios.Chunk("INFO",Text(name)),MeshScenarios.Form("PXAT",MeshScenarios.Form("0000",MeshScenarios.Chunk("INFO",Text("appearance/animation/walk.ans")))));
        var parts=new List<byte[]>{MeshScenarios.Chunk("INFO",Text("appearance/ash/body.ash").Concat(BitConverter.GetBytes(count)).ToArray()),Entry(first),Entry("idle")};if(tail)parts.Add(MeshScenarios.Chunk("JUNK",Array.Empty<byte>()));
        return MeshScenarios.Form("LATT",MeshScenarios.Form(version,parts.ToArray()));
    }
    internal static string CanonicalHash(LogicalAnimationTableDescription table)
    {
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
        void Text(string value){w.Write(Encoding.ASCII.GetBytes(value));w.Write((byte)0);}
        void Node(IffNode node){Text(node.Tag);Text(node.FormType??"");w.Write(node.Payload.Length);w.Write(node.Payload.Span);w.Write(node.Children.Count);foreach(var child in node.Children)Node(child);}
        Text(table.AssociatedHierarchy);w.Write(table.SerializedEntries.Count);
        foreach(var entry in table.SerializedEntries){Text(entry.SerializedName);Text(entry.Name);Node(entry.Template!);}
        w.Write(table.DefaultEntry.Synthetic);return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle)
    {
        foreach(var pair in bundle.LogicalTables!)
        {
            var table=pair.Value;var families=table.SerializedEntries.GroupBy(e=>e.Template!.FormType!).ToDictionary(g=>g.Key,g=>g.Count());
            var duplicates=table.SerializedEntries.GroupBy(e=>e.Name,StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1).Count();
            Console.WriteLine("logical_animation_table="+JsonSerializer.Serialize(new{path=pair.Key,table.AssociatedHierarchy,entries=table.SerializedEntries.Count,lookup=table.LookupEntries.Count,synthetic_default=table.DefaultEntry.Synthetic,duplicates,families,sha256=CanonicalHash(table)}));
            if(table.Find("__missing_probe__")!=table.DefaultEntry)throw new Exception("Original logical default lookup mismatch.");
        }
    }
}
