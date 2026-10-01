using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SharedCreatureTemplateScenarios
{
    internal static void Run()
    {
        var sources=Fixtures();string root="object/root.iff";string pin=Convert.ToHexString(SHA256.HashData(sources[root]));int reads=0;
        TreLookupResult Resolve(string p){reads++;return sources.TryGetValue(p,out var bytes)?new(TreLookupKind.Present,bytes,3):new(TreLookupKind.Missing);}
        var profile=SharedCreatureTemplateProfileReader.Load(Resolve,root,pin);
        if(reads!=2||profile.Sources.Count!=2||profile.AppearancePath!="appearance/test.sat"||profile.Collision.StepHeight!=.5f||profile.Collision.Radius!=.25f||profile.Fields["stepHeight"].SourcePath!=root||profile.Fields["collisionRadius"].SourcePath!="object/base.iff")throw new Exception("Template inheritance precedence/provenance mismatch.");
        var sphere=profile.Collision.CreateFallbackSphere(2);
        if(sphere.LocalCenter!=new System.Numerics.Vector3(2,1.5f,-2)||sphere.Radius!=.5f)throw new Exception("Recovered creature fallback geometry mismatch.");
        var special=profile.Collision.CreateFallbackSphere(2,true);
        if(special.LocalCenter.Y!=3||special.Radius!=1)throw new Exception("Special fallback dimensions mismatch.");
        foreach(var bytes in sources.Values)Array.Clear(bytes);
        if(profile.Collision.Radius!=.25f||profile.Sources[root].Sha256!=pin)throw new Exception("Template profile borrowed source.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        sources=Fixtures();Reject(()=>SharedCreatureTemplateProfileReader.Load(Resolve,root,new string('0',64)));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>SharedCreatureTemplateProfileReader.Load(Resolve,root,pin,1));
        Reject(()=>SharedCreatureTemplateProfileReader.Load(p=>p==root?new(TreLookupKind.Present,sources[root],3):new(TreLookupKind.Masked,Priority:4),root,pin));
        var changed=Fixtures(rootParent:root);Reject(()=>SharedCreatureTemplateProfileReader.Load(p=>new(TreLookupKind.Present,changed[p],3),root,Convert.ToHexString(SHA256.HashData(changed[root]))));
        foreach(byte[] mode in new[]{new byte[]{3,32,0,0,0,0,0,0,128,63},new byte[]{1,43,0,0,0,63},new byte[]{0,32,0},new byte[]{1,32,0,0,128,127}})
        {
            var unsupported=Fixtures(rootStep:mode);string hash=Convert.ToHexString(SHA256.HashData(unsupported[root]));
            Reject(()=>SharedCreatureTemplateProfileReader.Load(p=>new(TreLookupKind.Present,unsupported[p],3),root,hash));
        }
        byte[] valid=Fixtures()[root];for(int n=0;n<valid.Length;n++)Reject(()=>SharedObjectTemplateDecoder.Decode(valid.AsSpan(0,n)));
        byte[] malformed=Template("object/base.iff",new[]{("stepHeight",new byte[]{0,32}),("stepHeight",new byte[]{0,32})},Array.Empty<(string,byte[])>());
        Reject(()=>SharedObjectTemplateDecoder.Decode(malformed));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>profile.Collision.CreateFallbackSphere(0));
        Console.WriteLine("PASS: owned SCOT/STOT/SHOT structure, deterministic inheritance/provenance, scaled creature sphere and negative identity/cycle/mode/mask/depth/truncation gates");
    }
    private static Dictionary<string,byte[]> Fixtures(string rootParent="object/base.iff",byte[]? rootStep=null)
    {
        byte[] Scalar(float value){using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);writer.Write((byte)1);writer.Write((byte)32);writer.Write(value);return stream.ToArray();}
        return new(StringComparer.Ordinal)
        {
            ["object/root.iff"]=Template(rootParent,new[]{("stepHeight",rootStep??Scalar(.5f)),("collisionRadius",new byte[]{0,32})},new[]{("appearanceFilename",new byte[]{1}.Concat(Encoding.ASCII.GetBytes("appearance/test.sat\0")).ToArray())}),
            ["object/base.iff"]=Template(null,new[]{("stepHeight",Scalar(.25f)),("collisionHeight",Scalar(1.8f)),("collisionRadius",Scalar(.25f)),("collisionOffsetX",Scalar(1)),("collisionOffsetZ",Scalar(-1)),("collisionLength",Scalar(1.5f)),("warpTolerance",Scalar(17))},Array.Empty<(string,byte[])>())
        };
    }
    private static byte[] Template(string? parent,(string Name,byte[] Value)[] creature,(string Name,byte[] Value)[] shared)
    {
        byte[] Layer(string family,string version,(string Name,byte[] Value)[] values,byte[]? child)
        {
            List<byte[]> records=new();if(parent is not null)records.Add(MeshScenarios.Form("DERV",MeshScenarios.Chunk("XXXX",Encoding.ASCII.GetBytes(parent+"\0"))));
            byte[] count=BitConverter.GetBytes(values.Length);
            records.Add(MeshScenarios.Form(version,new[]{MeshScenarios.Chunk("PCNT",count)}.Concat(values.Select(v=>MeshScenarios.Chunk("XXXX",Encoding.ASCII.GetBytes(v.Name+"\0").Concat(v.Value).ToArray()))).ToArray()));
            if(child is not null)records.Add(child);return MeshScenarios.Form(family,records.ToArray());
        }
        return Layer("SCOT","0013",creature,Layer("STOT","0009",Array.Empty<(string,byte[])>(),Layer("SHOT","0010",shared,null)));
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8||args[2]!="--configuration"||args[4]!="--member"||args[6]!="--expected-sha256")throw new ArgumentException("Pinned original creature template required.");
        using var config=File.OpenText(args[3]);using var overlay=ConfiguredTreOverlay.Open(args[1],config);
        var profile=SharedCreatureTemplateProfileReader.Load(overlay.Lookup,args[5],args[7]);var sphere=profile.Collision.CreateFallbackSphere();
        foreach(var source in profile.Sources.Values)Console.WriteLine("creature_template_source="+JsonSerializer.Serialize(source));
        Console.WriteLine("creature_template_profile="+JsonSerializer.Serialize(new{profile.RootPath,profile.AppearancePath,profile.Collision,profile.Fields,sphere=new{center=InteriorDoorScenarios.V(sphere.LocalCenter),sphere.Radius}}));
        Console.WriteLine($"PASS original creature template sources={profile.Sources.Count}");
    }
}
