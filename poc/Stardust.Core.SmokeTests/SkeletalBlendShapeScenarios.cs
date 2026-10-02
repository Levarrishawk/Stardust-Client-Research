using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class SkeletalBlendShapeScenarios
{
    internal static byte[] Target(string name="shape",uint index=0,float delta=1,int declared=2,bool tangent=true,bool hardpoints=false)
    {
        byte[] Record(uint i,float value)=>BitConverter.GetBytes(i).Concat(new[]{value,2f,3f}.SelectMany(BitConverter.GetBytes)).ToArray();
        return MeshScenarios.Form("BLT ",new[]{MeshScenarios.Chunk("INFO",BitConverter.GetBytes(declared).Concat(BitConverter.GetBytes(1)).Concat(Encoding.ASCII.GetBytes(name+"\0")).ToArray()),
            MeshScenarios.Chunk("POSN",Record(index,delta).Concat(Record(index,4)).ToArray()),MeshScenarios.Chunk("NORM",Record(0,1))}
            .Concat(tangent?new[]{MeshScenarios.Chunk("DOT3",BitConverter.GetBytes(1).Concat(Record(0,1)).ToArray())}:Array.Empty<byte[]>())
            .Concat(hardpoints?new[]{MeshScenarios.Chunk("HPTS",new byte[]{0,0})}:Array.Empty<byte[]>()).ToArray());
    }
    internal static SkeletalMeshGeometryDescription Geometry(byte[] target,int declared=1)
    {
        var mesh=SkeletalMeshGeometryDecoder.Decode(SkeletalGeometryScenarios.Fixture());var words=mesh.Skin.InfoWords.ToArray();words[8]=declared;
        var form=IffDocument.Parse(MeshScenarios.Form("BLTS",target)).Root;
        return mesh with{Skin=mesh.Skin with{InfoWords=Array.AsReadOnly(words),UninterpretedSuffix=mesh.Skin.UninterpretedSuffix.Append(form).ToArray()},Tangents=new[]{new Vector4(0,0,1,-1)}};
    }
    internal static string CanonicalHash(SkeletalBlendTarget target)
    {
        using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.ASCII,true))
        {
            writer.Write(Encoding.ASCII.GetBytes(target.VariableName));writer.Write((byte)0);
            foreach(var deltas in new[]{target.Positions,target.Normals,target.Tangents})
            {writer.Write(deltas.Count);foreach(var delta in deltas){writer.Write(delta.Index);writer.Write(delta.Delta.X);writer.Write(delta.Delta.Y);writer.Write(delta.Delta.Z);}}
            writer.Write(target.HardpointMetadata?.Payload.Length??-1);if(target.HardpointMetadata is not null)writer.Write(target.HardpointMetadata.Payload.Span);
        }
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
    internal static void Run()
    {
        byte[] bytes=Target(hardpoints:true);var decoded=SkeletalBlendShapeDecoder.Create(Geometry(bytes));Array.Clear(bytes);var target=decoded.Targets.Single();
        if(target.VariableName!="shape"||target.Positions.Count!=2||target.Positions[0].Index!=target.Positions[1].Index||target.Tangents.Count!=1||target.HardpointMetadata?.Payload.Length!=2)throw new Exception("Owned sparse blend order/duplicates/tangent/hardpoint retention differs.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        byte[] valid=Target();for(int i=0;i<valid.Length;i++)Reject(()=>SkeletalBlendShapeDecoder.Create(Geometry(valid[..i])));
        Reject(()=>SkeletalBlendShapeDecoder.Create(Geometry(Target(index:3))));Reject(()=>SkeletalBlendShapeDecoder.Create(Geometry(Target(delta:float.NaN))));
        Reject(()=>SkeletalBlendShapeDecoder.Create(Geometry(Target(declared:-1))));Reject(()=>SkeletalBlendShapeDecoder.Create(Geometry(Target(declared:1))));Reject(()=>SkeletalBlendShapeDecoder.Create(Geometry(Target(),declared:2)));
        Reject(()=>SkeletalBlendShapeDecoder.Create(Geometry(Target()),maximumDeltas:3));Reject(()=>SkeletalBlendShapeDecoder.Create(Geometry(Target(name:""))));
        if(SkeletalBlendShapeDecoder.Create(SkeletalMeshGeometryDecoder.Decode(SkeletalGeometryScenarios.Fixture())).Targets.Count!=0)throw new Exception("Zero blend count requires a spurious form.");
        var geometry=Geometry(Target());var shapes=SkeletalBlendShapeDecoder.Create(geometry);
        var selection=new Dictionary<string,int>{{"shape",2}};var inactive=SkeletalBlendShapeEvaluator.Apply(geometry,shapes,selection);
        if(!inactive.Skin.Positions.SequenceEqual(geometry.Skin.Positions)||!inactive.Skin.Normals.SequenceEqual(geometry.Skin.Normals))throw new Exception("Inactive threshold changed geometry.");
        selection["shape"]=255;var shaped=SkeletalBlendShapeEvaluator.Apply(geometry,shapes,selection);
        if(shaped.Skin.Positions[0]!=geometry.Skin.Positions[0]+new Vector3(5,4,6)||Math.Abs(shaped.Skin.Normals[0].Length()-1)>1e-6||shaped.Tangents[0].W!=-1||!ReferenceEquals(shaped.Shaders,geometry.Shaders))throw new Exception("Ordered shape deltas, normalization, handedness or topology differs.");
        if(geometry.Skin.Positions.SequenceEqual(shaped.Skin.Positions))throw new Exception("Shape fixture did not deform.");
        selection["shape"]=3;var threshold=SkeletalBlendShapeEvaluator.Apply(geometry,shapes,selection);if(threshold.Skin.Positions[0]==geometry.Skin.Positions[0])throw new Exception("Value3 did not activate blending.");
        var rotation=Matrix4x4.CreateRotationZ(MathF.PI/2)*Matrix4x4.CreateTranslation(3,4,5);
        var skinned=SkeletalShaderDeformation.Create(shaped,0,new[]{rotation});
        if(Vector3.Distance(skinned.Positions[0],new(-1,9,11))>2e-5f)throw new Exception("Shape must precede rotating/translating skin transfer.");
        var untouched=geometry with{Skin=geometry.Skin with{Normals=new[]{new Vector3(0,0,2),new Vector3(0,0,4),Vector3.Zero}}};
        var normalized=SkeletalBlendShapeEvaluator.Apply(untouched,shapes,new Dictionary<string,int>{{"shape",255}});
        if(normalized.Skin.Normals[1]!=Vector3.UnitZ||normalized.Skin.Normals[2]!=Vector3.Zero)throw new Exception("Active target must normalize untouched directions and preserve degenerate zeros.");
        Reject(()=>SkeletalBlendShapeEvaluator.Apply(geometry,shapes with{Targets=new[]{shapes.Targets[0] with{Positions=new[]{new SkeletalBlendDelta(99,Vector3.One)}}}},new Dictionary<string,int>{{"shape",0}}));
        Reject(()=>SkeletalBlendShapeEvaluator.Apply(geometry,shapes,new Dictionary<string,int>()));selection["shape"]=256;Reject(()=>SkeletalBlendShapeEvaluator.Apply(geometry,shapes,selection));
        Reject(()=>SkeletalBlendShapeEvaluator.Apply(geometry,decoded,new Dictionary<string,int>{{"shape",0}}));
        HostAccountingScenarios.Reject<OperationCanceledException>(()=>SkeletalBlendShapeEvaluator.Apply(geometry,shapes,new Dictionary<string,int>{{"shape",0}},new CancellationToken(true)));
        var bundle=SkeletalCustomizationScenarios.Fixture() with{Geometries=new Dictionary<string,SkeletalMeshGeometryDescription>{{"a",geometry},{"b",geometry}},BlendShapes=new Dictionary<string,SkeletalBlendShapeDescription>{{"a",shapes},{"b",shapes}}};
        var requested=new Dictionary<string,int>{{"shape",255}};var selected=SkeletalAppearanceShapes.Apply(bundle,requested);requested.Clear();
        if(selected.ShapeSelection!.Values["shape"]!=255||selected.ShapeSelection.Geometries.Count!=2||!ReferenceEquals(selected.Geometries,bundle.Geometries))throw new Exception("Shared mesh shape selection ownership differs.");
        var selectedAgain=SkeletalAppearanceShapes.Apply(selected,new Dictionary<string,int>{{"shape",255}});
        if(!selectedAgain.ShapeSelection!.Geometries["a"].Skin.Positions.SequenceEqual(selected.ShapeSelection.Geometries["a"].Skin.Positions))throw new Exception("Shape selection accumulated prior delta.");
        Reject(()=>SkeletalAppearanceShapes.Apply(bundle,new Dictionary<string,int>{{"shape",255},{"extra",0}}));
        int reads=0;HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(_=>{reads++;return new(TreLookupKind.Missing);},"object/test.iff",new string('0',64),new string('0',64),loadBlendShapes:true));if(reads!=0)throw new Exception("Invalid blend option performed lookup.");
        Console.WriteLine("PASS: owned BLTS/BLT sparse descriptions, ordered duplicate indices, tangent/hardpoint retention, truncation/count/index/nonfinite/budget and invalid-option gates");
    }
}
