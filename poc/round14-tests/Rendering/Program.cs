using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
if(args.Length!=3)throw new ArgumentException("asset-root configuration fixture-json");
var configuration=File.ReadAllBytes(args[1]);if(Convert.ToHexString(SHA256.HashData(configuration))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new Exception("Configuration pin differs.");var fixture=SceneStaticVisualPreview.ReadFixture(args[2]);var a=SceneStaticVisualPreview.Create(fixture,"a");var b=SceneStaticVisualPreview.Create(fixture,"b");
var counts=new[]{new Dictionary<string,int>(),new Dictionary<string,int>()};var borrowed=new List<byte[]>();int ownerThread=Environment.CurrentManagedThreadId,thread=ownerThread,closed=0;
var sources=Task.Run(()=>
{
 thread=Environment.CurrentManagedThreadId;try
 {
  using var text=new StringReader(System.Text.Encoding.UTF8.GetString(configuration));using var overlay=ConfiguredTreOverlay.Open(args[0],text);
  var result=new List<SceneStaticResources>();int index=0;
  foreach(var create in new[]{a,b})
  {
   var seen=counts[index++];TreLookupResult Read(string path){seen[path]=seen.GetValueOrDefault(path)+1;var value=overlay.Lookup(path);if(value.Payload is not null)borrowed.Add(value.Payload);return value;}
   result.Add(SceneStaticResourceReader.Load(create,StaticSwitchTexturePreview.Catalog(),Read,SceneStaticMaterialPolicy.RequireComplete,switchTextureFrame:new(StaticSwitchTexturePreview.ShaderPath,StaticSwitchTexturePreview.ShaderPin,0)));
  }
  return result.ToArray();
 }
 finally{closed++;}
}).GetAwaiter().GetResult();
if(thread==ownerThread || closed!=1 || counts.Any(c=>c.Count!=27 || c.Values.Any(v=>v!=1)))throw new Exception("Original worker/source closure.");
var evidence=sources.Select((s,i)=>JsonSerializer.Serialize(StaticSwitchTexturePreview.FrameEvidence(s,0,counts[i]))).ToArray();foreach(var bytes in borrowed)Array.Clear(bytes);Array.Clear(configuration);
for(int i=0;i<2;i++){if(JsonSerializer.Serialize(StaticSwitchTexturePreview.FrameEvidence(sources[i],0,counts[i]))!=evidence[i])throw new Exception("Borrowed buffers changed owned closure.");SceneStaticVisual.ValidateDescriptor(sources[i],true);Console.WriteLine("switch_frame="+evidence[i]);}
var registry=new SceneObjectRegistry(capacity:2,sceneGeneration:1);var ah=SceneStaticVisualPreview.Finalize(registry,a);var bh=SceneStaticVisualPreview.Finalize(registry,b);
if(!SceneStaticVisual.Matches(registry,ah,sources[0]) || !SceneStaticVisual.Matches(registry,bh,sources[1]))throw new Exception("Current matches failed.");
var placement=SceneCreatePlacementBuilder.Create(a,SceneCreateZeroQuaternionPolicy.Reject);Console.WriteLine("static_native_pose="+JsonSerializer.Serialize(new{rows=new[]{placement.Transform.RowX,placement.Transform.RowY,placement.Transform.RowZ}.Select(v=>new[]{v.X,v.Y,v.Z}),origin=new[]{placement.Transform.Translation.X,placement.Transform.Translation.Y,placement.Transform.Translation.Z},placement.AppliedNormalization,placement.InputQuaternionNorm}));
int negatives=0;
void Reject(Action action){bool rejected=false;try{action();}catch(Exception e)when(e is InvalidDataException or ArgumentException){rejected=true;}if(!rejected)throw new Exception("Negative accepted.");negatives++;}
foreach(var limits in new[]{new SceneStaticVisualLimits(0,64,256),new(4097,64,256),new(3,64,256),new(64,0,256),new(64,6,256),new(64,64,0),new(64,64,1)})Reject(()=>SceneStaticVisual.ValidateDescriptor(sources[0],true,limits));
Reject(()=>SceneStaticVisual.ValidateDescriptor(sources[0] with{MaterialPolicy=SceneStaticMaterialPolicy.AllowNeutral},true));
Reject(()=>SceneStaticVisual.ValidateDescriptor(sources[0] with{Recipe=sources[0].Recipe with{Recipe=SceneObjectResourceRecipe.CreatureDetail0}},true));
Reject(()=>SceneStaticVisual.ValidateDescriptor(sources[0] with{Create=a with{TemplateCrc=1}},true));
var foreign=new SceneObjectRegistry(capacity:2,sceneGeneration:1);var fh=SceneStaticVisualPreview.Finalize(foreign,a);
if(SceneStaticVisual.Matches(registry,fh,sources[0]) || SceneStaticVisual.Matches(foreign,ah,sources[0]) || SceneStaticVisual.Matches(registry,bh,sources[0]))throw new Exception("Foreign owner/source matches.");negatives+=3;foreign.Close();
var pending=new SceneObjectRegistry(capacity:1,sceneGeneration:1);var ph=pending.Create(1,a)!.Handle;if(SceneStaticVisual.Matches(pending,ph,sources[0]))throw new Exception("Pending baselines matches.");negatives++;Reject(()=>pending.Create(1,b));pending.Close();
byte[] create=Convert.FromHexString(fixture["a_create"]);for(int length=0;length<create.Length;length++)Reject(()=>SceneObjectLifecycleCodec.DecodeCreate(create.AsSpan(0,length)));
registry.Destroy(1,SceneObjectLifecycleCodec.DecodeDestroy(Convert.FromHexString(fixture["a_destroy"])));if(SceneStaticVisual.Matches(registry,ah,sources[0]) || !SceneStaticVisual.Matches(registry,bh,sources[1]))throw new Exception("Destroy retired other/retained old.");negatives++;
var fresh=SceneStaticVisualPreview.Finalize(registry,a);if(fresh==ah || SceneStaticVisual.Matches(registry,ah,sources[0]) || !SceneStaticVisual.Matches(registry,fresh,sources[0]) || !SceneStaticVisual.Matches(registry,bh,sources[1]))throw new Exception("Recreate freshness differs.");negatives++;
registry.Reset(2);if(SceneStaticVisual.Matches(registry,fresh,sources[0]) || SceneStaticVisual.Matches(registry,bh,sources[1]))throw new Exception("Reset retained identity.");negatives++;
registry.Close();if(SceneStaticVisual.Matches(registry,fresh,sources[0]))throw new Exception("Closed matches.");negatives++;
Reject(()=>SceneCreatePlacementBuilder.Create(a with{QuaternionX=0,QuaternionY=0,QuaternionZ=0,QuaternionW=0},SceneCreateZeroQuaternionPolicy.Reject));
Reject(()=>SceneCreatePlacementBuilder.Create(a with{PositionX=float.NaN},SceneCreateZeroQuaternionPolicy.Reject));
Console.WriteLine("static_native_validation="+JsonSerializer.Serialize(new{negatives,truncations=create.Length,ownedBuffers=borrowed.Count,strictCpu=true,frame=0,workerOffOwner=true,closed=1,parsedSourcesUnchanged=true,gpu=false}));
Console.WriteLine("PASS: native static visual guard/placement/limits/lifecycle; owned original27/62mips per actor; no Godot allocation");
