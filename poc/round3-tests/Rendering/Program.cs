using System.Text.Json;
using Stardust.Core.Assets;
if(args.Length!=2)throw new ArgumentException("Explicit original root/configuration required.");
using var config=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],config);
for(int detail=1;detail<=3;detail++)
{
 var reads=new Dictionary<string,int>();TreLookupResult Read(string path){reads[path]=reads.GetValueOrDefault(path)+1;return overlay.Lookup(path);}
 var bundle=CreatureAppearanceDescriptorBundleReader.Load(Read,"object/creature/player/shared_human_male.iff","E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243","951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C",meshDetail:detail,loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadMaterialDefaults:true,loadPaletteDefaults:true,loadAlbedoFactors:true,loadTextureChoices:detail==1,loadCompressedNormals:detail==1,loadVertexLitSpecular:true,loadLogicalTables:true,animationSelection:new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),prepareSelectedAnimation:true);
 var expectedTriangles=new[]{0,1904,756,190}[detail];
 if(bundle.Geometries!.Values.Sum(g=>g.Shaders.Sum(s=>s.Triangles.Count))!=expectedTriangles)throw new Exception("Original selected-detail triangle count differs.");
 if(detail>=2&&(bundle.MaterialBundle!.CompressedNormals is not null||bundle.MaterialBundle.VertexLitSpecular!.Materials.Count!=1||bundle.MaterialBundle.VertexLitSpecular.Programs.Values.Any(p=>p.UsesAlphaHueMask||!p.ScalesBloomRgb)))throw new Exception("Small-mesh nodot3 family differs.");
 if(reads.Values.Any(n=>n!=1)||bundle.MaterialBundle!.VertexLitSpecular is null)throw new Exception("Read-once/material closure failed.");
 Console.WriteLine("detail="+JsonSerializer.Serialize(new{detail,sources=bundle.Sources.Count,bytes=bundle.Sources.Values.Sum(s=>(long)s.Bytes),meshes=bundle.Geometries!.Keys,triangles=bundle.Geometries.Values.Sum(g=>g.Shaders.Sum(s=>s.Triangles.Count)),vertexMaterials=bundle.MaterialBundle.VertexLitSpecular.Materials,programs=bundle.MaterialBundle.VertexLitSpecular.Programs.Select(p=>new{p.Key,p.Value.UsesAlphaHueMask,p.Value.ScalesBloomRgb})}));
 foreach(var row in bundle.Sources.Values)Console.WriteLine("source="+JsonSerializer.Serialize(new{detail,row}));
}
Console.WriteLine("PASS: original lower details existing palette/albedo/nodot3 readers and idle closure");
