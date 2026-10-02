using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalVertexLitSourceScenarios
{
    internal static void Run(Func<string,TreLookupResult> resolve,StaticEffectDescription effect)
    {
        const string pixel="pixel_program/h_color2_specmap_ps20_nodot3.psh";
        var raw=SkeletalVertexLitLightingSources.ExpectedSha256.Keys.ToDictionary(p=>p,p=>resolve(p).Payload!.ToArray());
        var reads=new Dictionary<string,int>();
        var identity=SkeletalVertexLitLightingSources.Load(effect,pixel,p=>{reads[p]=reads.GetValueOrDefault(p)+1;return new(TreLookupKind.Present,raw[p]);});
        if(reads.Count!=4||reads.Values.Any(n=>n!=1))throw new Exception("Source reads differ.");
        Console.WriteLine("mouth_vertex_sources="+JsonSerializer.Serialize(identity.Sha256));
        foreach(var path in raw.Keys)
        {
            foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})Reject(()=>SkeletalVertexLitLightingSources.Load(effect,pixel,p=>p==path?new(kind):new(TreLookupKind.Present,raw[p])));
            foreach(var bytes in new[]{Array.Empty<byte>(),raw[path][..^1],new byte[65537]})Reject(()=>SkeletalVertexLitLightingSources.Load(effect,pixel,p=>new(TreLookupKind.Present,p==path?bytes:raw[p])));
            var changed=raw[path].ToArray();changed[^1]^=1;Reject(()=>SkeletalVertexLitLightingSources.Load(effect,pixel,p=>new(TreLookupKind.Present,p==path?changed:raw[p])));
        }
        Reject(()=>SkeletalVertexLitLightingSources.Load(effect with{ContainsPrecalculatedVertexLighting=true},pixel,resolve));
        Reject(()=>SkeletalVertexLitLightingSources.Load(effect,"pixel_program/unselected.psh",resolve));
        Reject(()=>SkeletalVertexLitLightingSources.Load(effect with{Implementations=effect.Implementations.Concat(effect.Implementations).ToArray()},pixel,resolve));
        var wrong=effect with{Implementations=effect.Implementations.Select(i=>i with{Passes=i.Passes.Select(p=>p with{VertexProgram=TangentLightingSourcesReader.VertexPath}).ToArray()}).ToArray()};
        Reject(()=>SkeletalVertexLitLightingSources.Load(wrong,pixel,resolve));
        foreach(var bytes in raw.Values)Array.Clear(bytes);
        if(identity.Sha256!=SkeletalVertexLitLightingSources.ExpectedSha256)throw new Exception("Source metadata not retained.");
        try{((IDictionary<string,string>)identity.Sha256).Clear();throw new Exception("Mutable source identity.");}catch(NotSupportedException){}
        RunMath();
        Console.WriteLine("PASS: mouth source binding, four-node gates, read-once and metadata ownership; modern literal-power channel scenarios");
    }
    internal static void RunMath()
    {
        var forward=new AppearanceTransform(-Vector3.UnitX,Vector3.UnitY,-Vector3.UnitZ,Vector3.Zero);
        PortalCellLight Light(bool point,Vector3 d,Vector3 s)=>new(point?PortalCellLightKind.Point:PortalCellLightKind.Directional,new(d,1),new(s,1),forward,forward with{Translation=point?new(0,0,2):Vector3.Zero},new(0,0,1));
        var rows=new[]{Light(false,new(.4f,0,0),new(.8f,0,0)),Light(false,new(0,.2f,0),new(0,.1f,0)),Light(true,new(0,0,.6f),new(0,0,.7f)),Light(true,new(.2f,.3f,.4f),new(.01f))};
        var ambient=new Vector3(.1f,.2f,.3f);var material=new StaticShaderMaterial(new(8,9,10,1),new(5,6,7,1),new(.05f,.06f,.07f,1),new(.5f,.25f,.125f,1),300);
        var lighting=CellLightingPreview.FromLegacyCell(rows,AppearanceTransform.Identity,ambient,scaleLightColors:false);
        var result=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,Vector3.Zero,Vector3.UnitZ,new(0,0,5),material);
        Close(result.Diffuse,new(.6f,.535f,.62f));Close(result.Specular,new(.4f,0,.021875f));
        var changed=material with{AmbientRgba=Vector4.Zero,DiffuseRgba=Vector4.Zero};
        if(result!=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,Vector3.Zero,Vector3.UnitZ,new(0,0,5),changed))throw new Exception("Unused material channels changed output.");
        var baked=CellLightingPreview.FromLegacyCell(rows,AppearanceTransform.Identity,ambient,precalculated:true,scaleLightColors:false);
        var bakedResult=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(baked,Vector3.Zero,Vector3.UnitZ,new(0,0,5),material);
        Close(bakedResult.Diffuse,ambient+new Vector3(.05f,.06f,.07f));Close(bakedResult.Specular,result.Specular);
        var back=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,Vector3.Zero,-Vector3.UnitZ,new(0,0,5),material);Close(back.Diffuse,ambient+new Vector3(.05f,.06f,.07f));Close(back.Specular,Vector3.Zero);
        var coincident=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,new(0,0,2),Vector3.UnitZ,new(0,0,5),material);Close(coincident.Diffuse,new(.55f,.46f,.37f));Close(coincident.Specular,new(.4f,0,0));
        var angled=new AppearanceTransform(new(.5f,0,-MathF.Sqrt(.75f)),Vector3.UnitY,new(MathF.Sqrt(.75f),0,.5f),Vector3.Zero);
        var oblique=rows[0] with{RuntimeTransform=angled};
        var obliqueLighting=CellLightingPreview.FromLegacyCell(new[]{oblique},AppearanceTransform.Identity,Vector3.Zero,scaleLightColors:false);
        var facing=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(obliqueLighting,Vector3.Zero,-Vector3.UnitZ,new(0,0,-5),material with{EmissiveRgba=Vector4.Zero,SpecularPower=2});
        Close(facing.Diffuse,new(.2f,0,0));Close(facing.Specular,new(.3f,0,0));
        var literal=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(obliqueLighting,Vector3.Zero,-Vector3.UnitZ,new(0,0,-5),material with{EmissiveRgba=Vector4.Zero,SpecularPower=300});
        Close(literal.Specular,new(.4f*MathF.Pow(.75f,150),0,0));
        var snapshot=result;rows[0]=rows[0] with{DiffuseRgba=Vector4.Zero};
        if(snapshot!=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,Vector3.Zero,Vector3.UnitZ,new(0,0,5),material))throw new Exception("Lighting retained borrowed array.");
        Reject(()=>SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(new CellLightingPreview(rows,AppearanceTransform.Identity,ambient),Vector3.Zero,Vector3.UnitZ,new(0,0,5),material));
        Reject(()=>SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,Vector3.Zero,Vector3.Zero,new(0,0,5),material));
        Reject(()=>SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,new(float.NaN,0,0),Vector3.UnitZ,new(0,0,5),material));
        Reject(()=>SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,Vector3.Zero,Vector3.UnitZ,new(0,0,5),material with{EmissiveRgba=new(float.NaN,0,0,1)}));
        Reject(()=>SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,Vector3.Zero,Vector3.UnitZ,new(0,0,5),material with{SpecularRgba=new(-1,0,0,1)}));
        foreach(float power in new[]{-1f,1025f,float.NaN})
        {
            try{SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,Vector3.Zero,Vector3.UnitZ,new(0,0,5),material with{SpecularPower=power});throw new Exception("Invalid power accepted.");}catch(ArgumentOutOfRangeException){}
        }
        for(int i=0;i<64;i++)
        {
            var position=new Vector3((i%4)*.07f,0,-(i%3)*.11f);var normal=Vector3.Normalize(new Vector3((i%5)*.15f,0,1));var viewer=new Vector3((i%7)*.3f,.2f,3);var mat=material with{SpecularPower=new float[]{0,2,20,300}[i%4]};
            var output=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(lighting,position,normal,viewer,mat);
            float[] V(Vector3 v)=>new[]{v.X,v.Y,v.Z};
            var lights=new[]{lighting.Primary!}.Concat(lighting.Additional).Select(l=>new{l.Point,vector=V(l.DirectionOrPosition),diffuse=V(l.Diffuse),specular=V(l.Specular),attenuation=V(l.Attenuation)});
            Console.WriteLine("mouth_vertex_case="+JsonSerializer.Serialize(new{position=V(position),normal=V(normal),viewer=V(viewer),ambient=V(ambient),emissive=new[]{.05f,.06f,.07f},materialSpecular=new[]{.5f,.25f,.125f},power=mat.SpecularPower,lights,diffuse=V(output.Diffuse),specular=V(output.Specular),powerPolicy="modern-literal"}));
        }
    }
    private static void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("Expected rejection.");}
    private static void Close(Vector3 actual,Vector3 expected){if(Vector3.Distance(actual,expected)>1e-6f)throw new Exception($"Lighting mismatch {actual} / {expected}");}
}
