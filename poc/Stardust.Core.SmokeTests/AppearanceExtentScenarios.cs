using System.Numerics;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class AppearanceExtentScenarios
{
    internal static void Run()
    {
        byte[] bytes=Fixture();AppearanceExtent extent=AppearanceExtentDecoder.DecodeBase(IffDocument.Parse(bytes).Root)!;
        if(extent.Center!=new Vector3(2,3,4) || extent.Radius!=7 || extent.Minimum!=new Vector3(-10,-20,-30) || extent.Maximum!=new Vector3(10,20,30))
            throw new Exception("Authored sphere center or maximum/minimum box order mismatch.");
        // Sphere center deliberately differs from the box midpoint.
        AppearanceTransform world=new(new(0,-1,0),new(1,0,0),new(0,0,1),new(10,20,30));
        Vector3 camera=new(7,22,44);
        if(DetailAppearanceDistance.Measure(0,extent,world,camera)!=10 || DetailAppearanceDistance.Measure(null,extent,world,camera)!=10 ||
            Math.Abs(DetailAppearanceDistance.Measure(1,extent,world,camera)-Math.Sqrt(209))>1e-12 ||
            DetailAppearanceDistance.Measure(0,null,AppearanceTransform.Identity,new(3,4,0))!=5)
            throw new Exception("PIVT camera-origin distance mismatch.");
        if(AppearanceExtentDecoder.DecodeBase(IffDocument.Parse(Fixture(nullExtent:true)).Root) is not null)throw new Exception("NULL extent fabricated bounds.");
        for(int n=0;n<bytes.Length;n++)Reject(()=>AppearanceExtentDecoder.DecodeBase(IffDocument.Parse(bytes.AsSpan(0,n)).Root));
        Array.Clear(bytes);if(extent.Center.Z!=4)throw new Exception("Extent borrowed source data.");
        Reject(()=>AppearanceExtentDecoder.DecodeBase(IffDocument.Parse(Fixture(nonfinite:true)).Root));
        Reject(()=>AppearanceExtentDecoder.DecodeBase(IffDocument.Parse(Fixture(negativeRadius:true)).Root));
        Reject(()=>AppearanceExtentDecoder.DecodeBase(IffDocument.Parse(Fixture(reversed:true)).Root));
        CollisionExtentCases();
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>DetailAppearanceDistance.Measure(2,extent,world,camera));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>DetailAppearanceDistance.Measure(0,extent,world,new(float.NaN,0,0)));
        Console.WriteLine("PASS: owned APPR primary bounds, authored sphere center, maximum/minimum order and PIVT world-camera origins");
    }
    private static void CollisionExtentCases()
    {
        IffNode primary=IffDocument.Parse(Fixture()).Root.Children[0].Children[0];
        byte[] primaryBytes=Fixture();
        // Extracting the fixture's single extent preserves its deliberately asymmetric authored values.
        byte[] raw=primaryBytes.AsSpan(24).ToArray();
        byte[] Envelope(byte[] secondary)=>MeshScenarios.Form("APPR",MeshScenarios.Form("0003",raw,secondary));
        IffNode noCollision=IffDocument.Parse(Envelope(MeshScenarios.Form("NULL"))).Root;
        if(AppearanceExtentDecoder.DecodeCollisionBase(noCollision) is not null || AppearanceExtentDecoder.DecodeBase(noCollision)!=AppearanceExtentDecoder.DecodeExtent(primary))
            throw new Exception("Secondary NULL substituted primary display bounds.");
        AppearanceExtent? authored=AppearanceExtentDecoder.DecodeCollisionBase(IffDocument.Parse(Envelope(raw)).Root);
        if(authored!=AppearanceExtentDecoder.DecodeExtent(primary))throw new Exception("Secondary primitive extent changed authored values.");
        byte[] sphere=raw.AsSpan(24,48).ToArray();
        // EXBX header + version header precedes the nested EXSP fixture.
        AppearanceExtent? spherical=AppearanceExtentDecoder.DecodeCollisionBase(IffDocument.Parse(Envelope(sphere)).Root);
        if(spherical?.Kind!="EXSP" || spherical.Center!=new Vector3(2,3,4) || spherical.Radius!=7 || spherical.Minimum is not null)
            throw new Exception("Secondary sphere extent mismatch.");
        Reject(()=>AppearanceExtentDecoder.DecodeCollisionBase(IffDocument.Parse(Fixture()).Root));
        Reject(()=>AppearanceExtentDecoder.DecodeCollisionBase(IffDocument.Parse(Envelope(MeshScenarios.Form("CMSH"))).Root));
        Reject(()=>AppearanceExtentDecoder.DecodeCollisionBase(IffDocument.Parse(Envelope(MeshScenarios.Form("NULL",MeshScenarios.Chunk("DATA",new byte[]{0})))).Root));
        byte[] invalid=raw.ToArray();System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(invalid.AsSpan(68),BitConverter.SingleToInt32Bits(-1));
        Reject(()=>AppearanceExtentDecoder.DecodeCollisionBase(IffDocument.Parse(Envelope(invalid)).Root));
        Console.WriteLine("PASS: separate authored secondary NULL/sphere/box extents, no primary substitution and explicit missing/unsupported/malformed rejection");
    }
    internal static byte[] Fixture(bool nullExtent=false,bool nonfinite=false,bool negativeRadius=false,bool reversed=false)
    {
        byte[] Floats(params float[] values)
        {using MemoryStream bytes=new();using BinaryWriter writer=new(bytes);foreach(float value in values)writer.Write(value);return bytes.ToArray();}
        byte[] extent=nullExtent?MeshScenarios.Form("NULL"):MeshScenarios.Form("EXBX",MeshScenarios.Form("0001",
            MeshScenarios.Form("EXSP",MeshScenarios.Form("0001",MeshScenarios.Chunk("SPHR",Floats(nonfinite?float.NaN:2,3,4,negativeRadius?-1:7)))),
            MeshScenarios.Chunk("BOX ",Floats(reversed?-11:10,20,30,-10,-20,-30))));
        return MeshScenarios.Form("APPR",MeshScenarios.Form("0003",extent));
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--members" || args[6]!="--expected-members-sha256")
            throw new ArgumentException("Extent inventory requires explicit configuration, member-list and list identity.");
        if(new FileInfo(args[5]).Length>1_048_576)throw new InvalidDataException("Extent member-list budget exceeded.");
        byte[] listBytes=File.ReadAllBytes(args[5]);
        if(!Convert.ToHexString(SHA256.HashData(listBytes)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Extent member-list identity mismatch.");
        string[] members=File.ReadAllLines(args[5]);
        if(members.Length is 0 or >10_000 || members.Distinct(StringComparer.Ordinal).Count()!=members.Length)throw new InvalidDataException("Extent member-list count/duplicate limit.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        foreach(string member in members)
        {
            TreLookupResult selected=overlay.Lookup(member);
            if(selected.Kind!=TreLookupKind.Present || selected.Payload is null)throw new InvalidDataException("Extent inventory input missing/masked.");
            IffNode root=IffDocument.Parse(selected.Payload).Root;
            if(!root.IsForm("DTLA") || root.Children.Count!=1 || root.Children[0].Children.Count==0)throw new InvalidDataException("Extent inventory requires DTLA input.");
            IffNode version=root.Children[0];AppearanceExtent? extent=AppearanceExtentDecoder.DecodeBase(version.Children[0]);
            string Bits(params float[] values)=>string.Concat(values.Select(value=>BitConverter.SingleToUInt32Bits(value).ToString("X8")));
            string sphere=extent is null?"none":Bits(extent.Center.X,extent.Center.Y,extent.Center.Z,extent.Radius);
            string box=extent?.Maximum is Vector3 maximum && extent.Minimum is Vector3 minimum?Bits(maximum.X,maximum.Y,maximum.Z,minimum.X,minimum.Y,minimum.Z):"none";
            IffNode? pivot=version.Children.FirstOrDefault(node=>node.Tag=="PIVT");
            if(pivot is not null && (pivot.Payload.Length!=1 || pivot.Payload.Span[0]>1))throw new InvalidDataException("Invalid extent inventory pivot.");
            Console.WriteLine($"extent_member={member} priority={selected.Priority} sha256={Convert.ToHexString(SHA256.HashData(selected.Payload))} kind={extent?.Kind??"NULL"} sphere_bits={sphere} box_bits={box} pivot={(pivot is null?"absent":pivot.Payload.Span[0].ToString())}");
        }
        Console.WriteLine($"PASS extent_inventory members={members.Length}");
    }
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
