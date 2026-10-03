using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class TerrainMapMasksScenarios
{
    private const string Config="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524";
    private const string Source="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427";
    private const string Map="D3F7B55C110D6454E328507743FA0CF4A126CD6D8A802DC040752543CC18AD9D";
    private static string Hash(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static void Need(bool value,string why) { if(!value)throw new Exception(why); }
    private static object Metadata(TerrainMapMasks m)=>new {m.Version,m.WorldSizeBits,m.MetersPerBitBits,m.BytesPerRow,m.BitDimension,m.BytesPerMask,WmapSha256=Hash(m.CopyMask(TerrainMapMaskKind.Wmap)),SmapSha256=Hash(m.CopyMask(TerrainMapMaskKind.Smap))};
    private static object Point(TerrainMapMasks m,TerrainMapMaskKind kind,int x,int y)=>new {Kind=kind.ToString(),X=x,Y=y,Value=m.GetBit(kind,x,y)};
    private static object Samples(TerrainMapMasks m)
    {
        var samples=new List<object>();int d=checked((int)m.BitDimension),row=checked((int)m.BytesPerRow);
        foreach(var kind in new[]{TerrainMapMaskKind.Wmap,TerrainMapMaskKind.Smap})
        {
            foreach(var (x,y) in new[]{(-1,0),(0,-1),(0,0),(7,0),(8,0),(9,0),(d-1,d-1),(d,d-1),(1024,1024)})samples.Add(Point(m,kind,x,y));
            var bytes=m.CopyMask(kind);
            for(int i=0;i<bytes.Length;i++)if(bytes[i]!=0)
            { int bit=0;while((bytes[i]&(1<<bit))==0)bit++;int x=(i%row)*8+bit,y=i/row;if(x<d) { samples.Add(Point(m,kind,x,y));break; } }
        }
        return samples;
    }
    internal static void Run(string[] args)
    {
        Need(args.Length==3,"Explicit root/config/fixtures required");Need(Hash(File.ReadAllBytes(args[1]))==Config,"Original configuration pin");
        using var fixtures=JsonDocument.Parse(File.ReadAllBytes(args[2]));var cases=new List<object>();TerrainMapMasks? grid=null;
        foreach(var row in fixtures.RootElement.EnumerateArray())
        {
            string name=row.GetProperty("Name").GetString()!,expected=row.GetProperty("Reject").GetString()!;byte[] bytes=Convert.FromHexString(row.GetProperty("Hex").GetString()!);
            var limits=JsonSerializer.Deserialize<TerrainMapMaskLimits>(row.GetProperty("Limits"))!;string actual="None";object? metadata=null;
            try { var masks=TerrainMapMasks.Decode(bytes,limits);metadata=Metadata(masks);if(name=="exact-padded-nonaligned-grid")grid=masks; }
            catch(InvalidDataException) { actual="Input"; }
            catch(ArgumentOutOfRangeException) { actual="Options"; }
            Need(actual==expected,"Fixture mismatch: "+name);cases.Add(new {Name=name,Reject=actual,Metadata=metadata});
        }
        Need(grid is not null,"Synthetic grid present");var points=new List<object>();var rectangles=new List<object>();
        foreach(var kind in new[]{TerrainMapMaskKind.Wmap,TerrainMapMaskKind.Smap})
        {
            foreach(var (x,y) in new[]{(-1,0),(0,-1),(0,0),(7,0),(8,0),(9,0),(10,0),(9,9),(int.MaxValue,int.MinValue)})points.Add(Point(grid!,kind,x,y));
            foreach(var (x0,y0,x1,y1) in new[]{(0,0,0,0),(7,0,8,0),(9,9,0,0),(-10,-10,3,3),(-10,-10,-1,-1),(10,10,20,20),(int.MinValue,int.MinValue,int.MaxValue,int.MaxValue)})
                rectangles.Add(new {Kind=kind.ToString(),X0=x0,Y0=y0,X1=x1,Y1=y1,Value=grid!.AnyBit(kind,x0,y0,x1,y1,100)});
        }
        int rejections=0;
        void Reject<T>(Action action) where T:Exception { try {action();}catch(T){rejections++;return;}throw new Exception("Query unexpectedly accepted"); }
        Reject<InvalidDataException>(()=>grid!.AnyBit(TerrainMapMaskKind.Wmap,0,0,9,9,99));
        Reject<ArgumentOutOfRangeException>(()=>grid!.GetBit((TerrainMapMaskKind)255,0,0));
        Reject<ArgumentOutOfRangeException>(()=>grid!.AnyBit((TerrainMapMaskKind)255,0,0,0,0));
        Reject<ArgumentOutOfRangeException>(()=>grid!.AnyBit(TerrainMapMaskKind.Wmap,0,0,0,0,0));
        Reject<ArgumentOutOfRangeException>(()=>grid!.AnyBit(TerrainMapMaskKind.Wmap,0,0,0,0,1048577));
        int ownership=0;byte[] originalSynthetic=Convert.FromHexString(fixtures.RootElement[0].GetProperty("Hex").GetString()!);
        var owned=TerrainMapMasks.Decode(originalSynthetic);string before=JsonSerializer.Serialize(Metadata(owned));Array.Clear(originalSynthetic);Need(before==JsonSerializer.Serialize(Metadata(owned)),"Owned source snapshot");ownership++;
        var mask=owned.CopyMask(TerrainMapMaskKind.Wmap);Array.Clear(mask);Need(before==JsonSerializer.Serialize(Metadata(owned)),"Mask clone cannot mutate retained bytes");ownership++;
        var data=owned.CopyDataRecord();Array.Clear(data);Need(owned.WorldSizeBits==0x42F70000&&owned.MetersPerBitBits==0x3F400000&&owned.BytesPerRow==2&&owned.BitDimension==10,"Metadata copy cannot mutate scalar state");ownership++;
        Need(!ReferenceEquals(owned.CopyMask(TerrainMapMaskKind.Smap),owned.CopyMask(TerrainMapMaskKind.Smap))&&typeof(TerrainMapMasks).GetProperties().All(p=>p.SetMethod is null),"Independent copies and get-only metadata");ownership++;
        var empty=TerrainMapMasks.Decode(Convert.FromHexString("464F524D0000000430303030"));Need(empty.IsEmpty&&!empty.GetBit(TerrainMapMaskKind.Wmap,0,0)&&!empty.AnyBit(TerrainMapMaskKind.Smap,int.MinValue,0,int.MaxValue,0)&&empty.CopyDataRecord().Length==0,"Empty map query/copy contract");
        const string path="terrain/tatooine.trn";byte[] terrain;int? priority;
        using(var cfg=File.OpenText(args[1]))using(var overlay=ConfiguredTreOverlay.Open(args[0],cfg))
        { var lookup=overlay.Lookup(path);Need(lookup.Kind==TreLookupKind.Present&&lookup.Payload is not null,"Original terrain present");terrain=lookup.Payload!;priority=lookup.Priority; }
        Need(priority==42&&terrain.Length==1188446&&Hash(terrain)==Source,"Exact original terrain priority/length/hash");
        var root=IffDocument.Parse(terrain,new(2097152,20000,32)).Root;Need(root.IsForm("PTAT")&&root.Children.Count==1&&root.Children[0].IsForm("0014"),"Original PTAT container");
        var node=root.Children[0].Children.Single(n=>n.IsForm("0001"));int mapBytes=checked(node.Payload.Length+12),offset=node.Offset;
        var encoded=terrain.AsSpan(offset,mapBytes).ToArray();Need(offset==139818&&mapBytes==1048628&&Hash(encoded)==Map,"Direct version-form identity, no literal MAPS wrapper");
        var maps=TerrainMapMasks.Decode(encoded,new(mapBytes,2048,256,524288));Array.Clear(terrain);Array.Clear(encoded);
        Need(maps.WorldSize==16384&&maps.MetersPerBit==8&&maps.BytesPerRow==256&&maps.BitDimension==2048&&maps.BytesPerMask==524288,"Original exact metadata");
        var original=new {Root=Path.GetFullPath(args[0]).Replace('\\','/'),Configuration=Path.GetFullPath(args[1]).Replace('\\','/'),ConfigSha256=Config,Path=path,Priority=priority,SourceBytes=1188446,SourceSha256=Source,MapOffset=offset,MapBytes=mapBytes,MapSha256=Map,WorldSize=maps.WorldSize,MetersPerBit=maps.MetersPerBit,Metadata=Metadata(maps),Points=Samples(maps)};
        Console.WriteLine("terrain_masks="+JsonSerializer.Serialize(new {Cases=cases,GridPoints=points,Rectangles=rectangles,QueryRejections=rejections,OwnershipChecks=ownership,Original=original}));
        Console.WriteLine("PASS: bounded owned terrain map version forms, exact masks, raw grid queries and one pinned original sample.");
    }
}
