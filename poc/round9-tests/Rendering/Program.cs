using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Proof;
if(args.Length!=2)throw new ArgumentException("Rendering original-root configuration");
byte[] config=File.ReadAllBytes(args[1]);
if(Convert.ToHexString(SHA256.HashData(config))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new InvalidDataException("Configuration pin differs.");
var borrowed=new List<byte[]>();var counts=new[]{new Dictionary<string,int>(),new Dictionary<string,int>()};int closed=0,owner=System.Environment.CurrentManagedThreadId,worker=owner;
using var load=new OwnedBackgroundAssetLoad<SceneStaticResources[]>(token=>
{
 worker=System.Environment.CurrentManagedThreadId;
 try
 {
  using var text=new StringReader(System.Text.Encoding.UTF8.GetString(config));using var overlay=ConfiguredTreOverlay.Open(args[0],text);var result=new SceneStaticResources[2];
  for(int frame=0;frame<2;frame++)
  {
   int index=frame;
   TreLookupResult Read(string path){counts[index][path]=counts[index].GetValueOrDefault(path)+1;var r=overlay.Lookup(path);if(r.Payload is not null)borrowed.Add(r.Payload);return r;}
   result[frame]=SceneStaticResourceReader.Load(StaticSwitchTexturePreview.CreateFixture(),StaticSwitchTexturePreview.Catalog(),Read,SceneStaticMaterialPolicy.RequireComplete,token,switchTextureFrame:new(StaticSwitchTexturePreview.ShaderPath,StaticSwitchTexturePreview.ShaderPin,frame));
  }
  return result;
 }
 finally{Interlocked.Increment(ref closed);}
});
var bundles=load.TakeAsync().GetAwaiter().GetResult();
if(worker==owner||closed!=1||counts.Any(c=>c.Count!=27||c.Values.Any(n=>n!=1))||bundles.Any(b=>b.ExternalReads!=27||b.SourceBytes!=1401794||b.Appearance.Meshes.Count!=4||b.Profile.Inheritance.Count!=4||b.Appearance.Diagnostics.Count!=0))throw new Exception("Full original object closure/worker/ownership differs.");
var evidence=bundles.Select((b,f)=>JsonSerializer.Serialize(StaticSwitchTexturePreview.FrameEvidence(b,f,counts[f]))).ToArray();
foreach(byte[] bytes in borrowed)Array.Clear(bytes);Array.Clear(config);
for(int frame=0;frame<2;frame++)if(JsonSerializer.Serialize(StaticSwitchTexturePreview.FrameEvidence(bundles[frame],frame,counts[frame]))!=evidence[frame])throw new Exception("Prepared frame borrowed original bytes.");
if(!bundles[0].Appearance.Meshes.All(p=>StaticMeshVerification.ComputeGeometrySha256(p.Value.Mesh)==StaticMeshVerification.ComputeGeometrySha256(bundles[1].Appearance.Meshes[p.Key].Mesh)))throw new Exception("Texture selection changed geometry.");
for(int frame=0;frame<2;frame++)Console.WriteLine("switch_frame="+evidence[frame]);
int rejects=0,preflight=0;
foreach(var choice in new[]{(-1,0),(2,0),(0,-1),(0,4)}){try{StaticSwitchTexturePreview.ValidateChoice(choice.Item1,choice.Item2);throw new Exception("Invalid choice accepted.");}catch(ArgumentOutOfRangeException){rejects++;}}
TreLookupResult Never(string _){preflight++;throw new Exception("Invalid option performed lookup.");}
foreach(var invalid in new[]{new SwitchTextureFrameSelection(StaticSwitchTexturePreview.ShaderPath,"bad",0),new(StaticSwitchTexturePreview.ShaderPath,StaticSwitchTexturePreview.ShaderPin,-1),new(StaticSwitchTexturePreview.ShaderPath,StaticSwitchTexturePreview.ShaderPin,256)})
{try{SceneStaticResourceReader.Load(StaticSwitchTexturePreview.CreateFixture(),StaticSwitchTexturePreview.Catalog(),Never,SceneStaticMaterialPolicy.RequireComplete,switchTextureFrame:invalid);throw new Exception("Invalid selection accepted.");}catch(ArgumentException){rejects++;}}
if(preflight!=0)throw new Exception("Invalid options read archives.");
Console.WriteLine("switch_validation="+JsonSerializer.Serialize(new{rejects,preflightReads=preflight,originalFrames=2,completeMeshesPerFrame=4,ownedBuffers=true,workerClosed=true,gpu=false}));
Console.WriteLine("PASS: native static switch preview; original CPU buffers owned; seven negatives; no native Godot allocation or GPU claim.");
