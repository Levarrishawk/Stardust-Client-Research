using System.Text;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class AppearanceResolutionScenarios
{
    internal static void Run()
    {
        byte[] redirect=Redirect("appearance/mesh/synthetic.msh"),mesh=MeshScenarios.Mesh("0001");
        Dictionary<string,byte[]> input=new(){["appearance/synthetic.apt"]=redirect,["appearance/mesh/synthetic.msh"]=mesh,
            ["appearance/defaultappearance.apt"]=Redirect("appearance/mesh/synthetic.msh")};
        TreLookupResult Resolve(string path)=>input.TryGetValue(path,out byte[]? bytes)?new(TreLookupKind.Present,bytes,9):new(TreLookupKind.Missing);
        string digest=Convert.ToHexString(SHA256.HashData(redirect));
        AppearanceResolution value=AppearanceResolver.FetchStrict(Resolve,"appearance/synthetic.apt",digest);
        if(value.UsedDefault || value.TerminalPath!="appearance/mesh/synthetic.msh" || value.Sources.Count!=2 || !value.TerminalMetadata.IsForm("MESH"))
            throw new Exception("Strict appearance redirect mismatch.");
        Array.Clear(mesh);
        if(StaticMeshDecoder.Decode(value.TerminalBytes.Span).VertexCount!=3) throw new Exception("Appearance resolver borrowed source bytes.");
        input["appearance/mesh/synthetic.msh"]=MeshScenarios.Mesh("0001");
        for(int n=0;n<redirect.Length;n++)
        {
            byte[] shortRoot=redirect[..n];
            Reject(()=>AppearanceResolver.FetchStrict(path=>path=="appearance/synthetic.apt"?new(TreLookupKind.Present,shortRoot):Resolve(path),"appearance/synthetic.apt"));
        }
        Reject(()=>AppearanceResolver.FetchStrict(Resolve,"appearance/missing.apt"));
        Reject(()=>AppearanceResolver.FetchStrict(Resolve,"appearance/synthetic.apt",new string('0',64)));
        AppearanceResolution missing=AppearanceResolver.FetchOrDefault(Resolve,"appearance/missing.apt");
        if(!missing.UsedDefault || missing.SelectedRootPath!="appearance/defaultappearance.apt") throw new Exception("Missing requested path did not use explicit default boundary.");
        if(!AppearanceResolver.FetchOrDefault(Resolve,null).UsedDefault)throw new Exception("Empty appearance did not select explicit default.");
        TreLookupResult Mask(string path)=>path=="appearance/synthetic.apt"?new(TreLookupKind.Masked):Resolve(path);
        if(!AppearanceResolver.FetchOrDefault(Mask,"appearance/synthetic.apt").UsedDefault)throw new Exception("Masked root did not select default.");
        input["appearance/synthetic.apt"]=Redirect("appearance/missing.msh");
        Reject(()=>AppearanceResolver.FetchOrDefault(Resolve,"appearance/synthetic.apt"));
        input["appearance/synthetic.apt"]=Redirect("appearance/other.apt");
        Reject(()=>AppearanceResolver.FetchStrict(Resolve,"appearance/synthetic.apt"));
        input["appearance/synthetic.apt"]=Redirect("appearance/renamed.msh");input["appearance/renamed.msh"]=Redirect("appearance/mesh/synthetic.msh");
        Reject(()=>AppearanceResolver.FetchStrict(Resolve,"appearance/synthetic.apt"));
        Console.WriteLine("PASS: strict APT resolution, explicit missing/masked default, broken target rejection and owned bytes");
    }
    internal static byte[] Redirect(string target)=>MeshScenarios.Form("APT ",MeshScenarios.Form("0000",MeshScenarios.Chunk("NAME",Encoding.ASCII.GetBytes(target+"\0"))));
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
