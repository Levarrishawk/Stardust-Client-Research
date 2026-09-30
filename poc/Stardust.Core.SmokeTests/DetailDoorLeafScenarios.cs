using System.Numerics;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class DetailDoorLeafScenarios
{
    internal static void Run()
    {
        byte[] table=DoorLeafScenarios.Fixture(),detail=DetailAppearanceScenarios.Fixture("0007",appearanceBase:AppearanceExtentScenarios.Fixture()),apt=AppearanceResolutionScenarios.Redirect("appearance/door.lod");
        Dictionary<string,byte[]> resources=new(){[SingleLeafDoorAssetReader.TablePath]=table,["appearance/test.apt"]=apt,["appearance/door.lod"]=detail,["appearance/mesh/far.msh"]=MeshScenarios.Mesh("0001"),["appearance/mesh/near.msh"]=MeshScenarios.Mesh("0001")};
        TreLookupResult Resolve(string path)=>resources.TryGetValue(path,out var bytes)?new(TreLookupKind.Present,bytes,3):new(TreLookupKind.Missing);
        string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
        var asset=SingleLeafDoorAssetReader.Load(Resolve,"synthetic",Hash(table),Hash(apt));
        Require(asset.Geometry.Root.Detail is not null && asset.Geometry.Root.Children.Count==2 && asset.Extent.Minimum==new Vector3(-10,-20,-30) && asset.Sources.Count==5,"Root authored box and complete bands loaded");
        var root=asset.Geometry.Root;var extent=asset.Extent;
        var smaller=root.Children.Select(c=>c with{Template=c.Template with{Mesh=c.Template.Mesh! with{Primitives=c.Template.Mesh.Primitives.Select(p=>p with{Vertices=p.Vertices.Select(v=>v with{Position=v.Position*.1f}).ToArray()}).ToArray()}}}).ToArray();
        Require(SingleLeafDoorAssetReader.ReadLeafExtent(root with{Children=smaller})==extent,"Physical extent stable despite visual band geometry");
        Reject(()=>SingleLeafDoorAssetReader.ReadLeafExtent(root with{Children=root.Children.Take(1).ToArray()}));
        Reject(()=>SingleLeafDoorAssetReader.ReadLeafExtent(root with{Diagnostics=new[]{"synthetic omission"}}));
        Reject(()=>SingleLeafDoorAssetReader.ReadLeafExtent(root with{Detail=root.Detail! with{Diagnostics=new[]{"synthetic reversed band"}}}));
        Reject(()=>SingleLeafDoorAssetReader.ReadLeafExtent(root with{Children=new[]{root.Children[0] with{LocalTransform=AppearanceTransform.Identity with{Translation=Vector3.UnitX}},root.Children[1]}}));
        var mesh=root.Children[0].Template.Mesh!;
        var outside=mesh with{Primitives=mesh.Primitives.Select(p=>p with{Vertices=p.Vertices.Select(v=>v with{Position=new Vector3(100,0,0)}).ToArray()}).ToArray()};
        Reject(()=>SingleLeafDoorAssetReader.ReadLeafExtent(root with{Children=new[]{root.Children[0] with{Template=root.Children[0].Template with{Mesh=outside}},root.Children[1]}}));
        resources.Remove("appearance/mesh/near.msh");Reject(()=>SingleLeafDoorAssetReader.Load(Resolve,"synthetic",Hash(table),Hash(apt)));
        resources["appearance/mesh/near.msh"]=MeshScenarios.Mesh("0001");resources["appearance/door.lod"]=DetailAppearanceScenarios.Fixture("0007",appearanceBase:AppearanceExtentScenarios.Fixture(nullExtent:true));Reject(()=>SingleLeafDoorAssetReader.Load(Resolve,"synthetic",Hash(table),Hash(apt)));
        Console.WriteLine("PASS: single normal DTLA leaf, fixed root envelope across bands, incomplete/malformed/oversized geometry and missing dependency rejection");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Detail door: "+label);}
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
