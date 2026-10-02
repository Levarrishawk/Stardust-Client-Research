using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
if(args.Length!=2)throw new ArgumentException("usage: Rendering <original-root> <configuration>");
byte[] configuration=File.ReadAllBytes(args[1]);
if(Convert.ToHexString(SHA256.HashData(configuration))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new InvalidDataException("Configuration pin differs.");
using var reader=new StringReader(System.Text.Encoding.UTF8.GetString(configuration));using var overlay=ConfiguredTreOverlay.Open(args[0],reader);
var reads=new Dictionary<string,int>(StringComparer.Ordinal);var raw=new List<byte[]>();
TreLookupResult Resolve(string path){reads[path]=reads.GetValueOrDefault(path)+1;var row=overlay.Lookup(path);if(row.Payload is not null)raw.Add(row.Payload);return row;}
var result=RetainedSkeletalDetailBundleReader.Load(Resolve,CancellationToken.None);
if(reads.Values.Any(n=>n!=1)||reads.Count!=result.ExternalReads||result.ExternalReads!=result.Sources.Count)throw new Exception("Union read-once closure differs.");
string Fingerprint(RetainedSkeletalDetailBundle owned)
{
 using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
 foreach(var b in new[]{owned.Detail2,owned.Detail3})
 {
  foreach(var g in b.Geometries!.Values)foreach(var p in g.Skin.Positions){hash.AppendData(BitConverter.GetBytes(p.X));hash.AppendData(BitConverter.GetBytes(p.Y));hash.AppendData(BitConverter.GetBytes(p.Z));}
  var binding=SkeletalJointBindingBuilder.Create(b.Appearance,b.Skeletons!,b.Appearance.Skeletons.ToDictionary(r=>r.Path,_=>0));
  hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(b.PreparedAnimation!.CreateLocalPose(binding,45),new JsonSerializerOptions{IncludeFields=true}));
  foreach(var t in b.MaterialBundle!.Textures.Values)foreach(var m in t.Mips)hash.AppendData(m.Rgba.Span);
 }
 return Convert.ToHexString(hash.GetHashAndReset());
}
string before=Fingerprint(result);foreach(var bytes in raw)Array.Clear(bytes);if(Fingerprint(result)!=before)throw new Exception("Resolver byte mutation changed owned bundles.");
foreach(var pair in new[]{(2,result.Detail2),(3,result.Detail3)})
{
 var bundle=pair.Item2;int triangles=bundle.Geometries!.Values.Sum(g=>g.Shaders.Sum(s=>s.Triangles.Count));
 if(triangles!=(pair.Item1==2?756:190)||bundle.MaterialBundle!.CompressedNormals is not null||bundle.Sources.Count!=25||bundle.MaterialBundle.Textures.Count!=1||bundle.MaterialBundle.Textures.Values.Single().Mips.Count!=9)throw new Exception("Selected detail geometry/material count differs.");
 Console.WriteLine("detail="+JsonSerializer.Serialize(new{detail=pair.Item1,triangles,sources=bundle.Sources.Count,bytes=bundle.Sources.Values.Sum(s=>(long)s.Bytes),mips=bundle.MaterialBundle.Textures.Values.Sum(t=>t.Mips.Count)}));
}
Console.WriteLine("union="+JsonSerializer.Serialize(new{sources=result.Sources.Count,bytes=result.SourceBytes,reads=result.ExternalReads,ownedFingerprint=before}));
foreach(var row in result.Sources.Values)Console.WriteLine("source="+JsonSerializer.Serialize(row));
int negatives=0;
void Reject(Action action){try{action();}catch(Exception e)when(e is InvalidDataException or AppearanceGraphBudgetException or OperationCanceledException or ArgumentOutOfRangeException){negatives++;return;}throw new Exception("Malformed/limited/canceled fixture accepted.");}
Reject(()=>RetainedSkeletalDetailBundleReader.Load(overlay.Lookup,CancellationToken.None,maximumResources:1));
Reject(()=>RetainedSkeletalDetailBundleReader.Load(overlay.Lookup,CancellationToken.None,maximumBytes:1));
Reject(()=>RetainedSkeletalDetailBundleReader.Load(overlay.Lookup,CancellationToken.None,maximumResources:0));
using var canceled=new CancellationTokenSource();canceled.Cancel();int canceledReads=0;
Reject(()=>RetainedSkeletalDetailBundleReader.Load(p=>{canceledReads++;return overlay.Lookup(p);},canceled.Token));if(canceledReads!=0)throw new Exception("Canceled load performed lookup.");
foreach(string target in new[]{RetainedSkeletalDetailBundleReader.ObjectPath,"appearance/mesh/hum_m_head_l3.mgn","texture/hum_m_sm.dds","palette/pc_skin_hum.pal","pixel_program/h_specmap_ps20_nodot3.psh"})
{
 Reject(()=>RetainedSkeletalDetailBundleReader.Load(p=>p==target?new(TreLookupKind.Missing):overlay.Lookup(p),CancellationToken.None));
 Reject(()=>RetainedSkeletalDetailBundleReader.Load(p=>p==target?new(TreLookupKind.Masked):overlay.Lookup(p),CancellationToken.None));
 Reject(()=>RetainedSkeletalDetailBundleReader.Load(p=>p==target?new(TreLookupKind.Present,new byte[]{0,1,2}):overlay.Lookup(p),CancellationToken.None));
}
using var mid=new CancellationTokenSource();int midReads=0;
Reject(()=>RetainedSkeletalDetailBundleReader.Load(p=>{var r=overlay.Lookup(p);if(++midReads==26)mid.Cancel();return r;},mid.Token));
if(midReads!=26||!mid.IsCancellationRequested)throw new Exception("Second-detail cancellation fixture did not reach its intended lookup.");
Console.WriteLine($"PASS: retained original union read-once/owned bytes; negatives={negatives} mid_load_cancel=true no partial publication");
