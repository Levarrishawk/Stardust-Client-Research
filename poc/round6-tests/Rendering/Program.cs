using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
using System.Text.Json;
using System.Security.Cryptography;
if(args.Length!=3)throw new ArgumentException("usage: Rendering original-root configuration profile");
using var configuration=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],configuration);using var profile=File.OpenRead(args[2]);
var bank=SkeletalAnimationBankProfile.Load(profile);
var create=new SceneCreateObjectByCrcMessage(0xDEADBEEF00000001,.2f,BitConverter.Int32BitsToSingle(int.MinValue),.3f,1.7f,-2,7,4,0xAF1DC1A1,255);
var catalog=new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(create.TemplateCrc,RetainedSkeletalDetailBundleReader.ObjectPath,RetainedSkeletalDetailBundleReader.ObjectPin,RetainedSkeletalDetailBundleReader.AppearancePin)});
var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
var reads=new Dictionary<string,int>();var raw=new List<byte[]>();
TreLookupResult Read(string p){reads[p]=reads.GetValueOrDefault(p)+1;var r=overlay.Lookup(p);if(r.Payload is not null)raw.Add(r.Payload);return r;}
var source=SceneCreatureResourceReader.Load(create,catalog,idle,bank,Read);
if(reads.Count!=37||reads.Values.Any(n=>n!=1)||source.SourceBytes!=4683848)throw new Exception("Original closure/read-once differs.");
string Fingerprint()
{
 using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
 foreach(var g in source.Creature.Geometries!.Values)foreach(var p in g.Skin.Positions)hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(p,new JsonSerializerOptions{IncludeFields=true}));
 foreach(var t in source.Creature.MaterialBundle!.Textures.Values)foreach(var m in t.Mips)hash.AppendData(m.Rgba.Span);
 var binding=SkeletalJointBindingBuilder.Create(source.Creature.Appearance,source.Creature.Skeletons!,source.Creature.Appearance.Skeletons.ToDictionary(r=>r.Path,_=>0));
 foreach(var clip in source.Creature.AdditionalAnimations!.Values.Append(source.Creature.PreparedAnimation!))hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(clip.CreateLocalPose(binding,clip.EndFrame*.5),new JsonSerializerOptions{IncludeFields=true}));
 return Convert.ToHexString(hash.GetHashAndReset());
}
var before=Fingerprint();foreach(var bytes in raw)Array.Clear(bytes);if(Fingerprint()!=before)throw new Exception("Borrowed bytes changed owned visual inputs.");
var registry=new SceneObjectRegistry(8,1);var handle=registry.Create(1,create)!.Handle;
if(!SceneCreatureVisual.Matches(registry,handle,source))throw new Exception("Exact current source rejected.");
int negatives=0;
foreach(var changed in new[]{create with{QuaternionY=0},create with{RawHyperspaceFlag=0},create with{PositionX=0},create with{TemplateCrc=1},create with{ObjectId=1}})
{if(SceneCreatureVisual.Matches(registry,handle,source with{Create=changed}))throw new Exception("Wrong create accepted.");negatives++;}
foreach(var token in new[]{handle with{RegistryId=Guid.NewGuid()},handle with{SceneGeneration=2},handle with{Revision=0},handle with{ObjectId=1}})
{if(SceneCreatureVisual.Matches(registry,token,source))throw new Exception("Foreign/stale handle accepted.");negatives++;}
SceneCreatureVisual.ValidateEngineTransform(Transform3D.Identity);
SceneCreatureVisual.ValidateEngineTransform(new(new Basis(new Vector3(2,0,0),new(0,.5f,0),new(0,0,1)),new(1,2,3)));
foreach(var transform in new[]{new Transform3D(Basis.Identity,new(float.NaN,0,0)),new(Basis.Identity,new(1e7f,0,0)),new(new Basis(Vector3.Zero,Vector3.Up,Vector3.Back),Vector3.Zero),new(new Basis(Vector3.Right,new(.2f,1,0),Vector3.Back),Vector3.Zero)})
{bool rejected=false;try{SceneCreatureVisual.ValidateEngineTransform(transform);}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Invalid engine placement accepted.");negatives++;}
foreach(string action in new[]{"finalize","contain","replace","destroy","reset","close"})
{
 var owner=new SceneObjectRegistry(8,1);var token=owner.Create(1,create)!.Handle;
 if(action=="finalize")owner.EndBaselines(1,new(create.ObjectId));if(action=="contain")owner.Contain(1,new(create.ObjectId,0,1));if(action=="replace")owner.Replace(1,create);if(action=="destroy")owner.Destroy(1,new(create.ObjectId,0));if(action=="reset")owner.Reset();if(action=="close")owner.Close();
 if(SceneCreatureVisual.Matches(owner,token,source))throw new Exception("Invalidated owner accepted.");negatives++;
}
foreach(var row in source.Creature.Sources.Values)Console.WriteLine("source="+JsonSerializer.Serialize(row));
Console.WriteLine("create="+Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(create)));
Console.WriteLine($"PASS: original scene visual input ownership; native identity/transform negatives={negatives}; no native Godot allocation or GPU claim");
