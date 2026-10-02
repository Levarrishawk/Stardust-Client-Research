using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalLowerDetailScenarios
{
    internal static void Run(string[] args)
    {
        SkeletalLodSelectionScenarios.Run(args,3);
        using var config=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],config);
        const string root="object/creature/player/shared_human_male.iff",objectPin="E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243",satPin="951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C";
        void Reject<T>(Action a) where T:Exception {try{a();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
        for(int detail=1;detail<=3;detail++)
        {
            var counts=new Dictionary<string,int>(StringComparer.Ordinal);var borrowed=new List<byte[]>();
            TreLookupResult Read(string p){counts[p]=counts.GetValueOrDefault(p)+1;var r=overlay.Lookup(p);if(r.Payload is not null)borrowed.Add(r.Payload);return r;}
            var bundle=CreatureAppearanceDescriptorBundleReader.Load(Read,root,objectPin,satPin,new(MaximumResources:64,MaximumSourceBytes:16*1024*1024),loadSkeletons:true,loadSkinSources:true,meshDetail:detail,loadGeometry:true,loadLogicalTables:true,animationSelection:LogicalAnimationProxyScenarios.IdleSelection,prepareSelectedAnimation:true,additionalAnimationSelections:SkeletalAnimationBankScenarios.Profile());
            if(counts.Values.Any(n=>n!=1))throw new Exception("Animation closure repeated external lookup.");
            foreach(var source in bundle.Sources.Values)Console.WriteLine("lower_source="+JsonSerializer.Serialize(new{detail,source}));
            Console.WriteLine("lower_descriptor="+JsonSerializer.Serialize(new{detail,bundle.Appearance,counts}));
            var binding=SkeletalJointBindingBuilder.Create(bundle.Appearance,bundle.Skeletons!,bundle.Appearance.Skeletons.ToDictionary(r=>r.Path,_=>0));
            var geometries=bundle.Geometries??throw new Exception("Geometry absent."); var bank=bundle.AdditionalAnimations??throw new Exception("Bank absent.");
            var hashes=geometries.ToDictionary(p=>p.Key,p=>SkeletalGeometryScenarios.CanonicalHash(p.Value));
            var framesByKey=bank.ToDictionary(p=>p.Key,p=>new[]{0d,.5,p.Value.EndFrame/2d,p.Value.EndFrame});
            var retained=bank.ToDictionary(p=>p.Key,p=>p.Value.CreateLocalPose(binding,.5).ToArray());
            foreach(var pair in bank)
            {
                var prepared=pair.Value;var profile=SkeletalAnimationBankScenarios.Profile()[pair.Key];
                Console.WriteLine("lower_clip="+JsonSerializer.Serialize(new{detail,key=pair.Key,path=prepared.ClipPath,sha256=prepared.ClipSha256,prepared.EndFrame,prepared.FrameRate,table=profile.TablePath,name=profile.LogicalName,route=profile.ExplicitChildRoute,prepared.MissingSkeletonTransforms,prepared.UnusedAnimationTransforms,frames=framesByKey[pair.Key]}));
                foreach(double bad in new[]{double.NaN,-1,prepared.EndFrame+1d})Reject<InvalidDataException>(()=>prepared.CreateLocalPose(binding,bad));
                foreach(double frame in framesByKey[pair.Key])
                {
                    var local=prepared.CreateLocalPose(binding,frame);var world=binding.CreateWorldPose(local);
                    for(int joint=0;joint<world.Count;joint++)Console.WriteLine("lower_pose="+JsonSerializer.Serialize(new{detail,key=pair.Key,frame,joint,path=binding.Joints[joint].SkeletonPath,source=binding.Joints[joint].SourceJoint,local=SkeletonBindPoseScenarios.LegacyValues(local[joint]),world=SkeletonBindPoseScenarios.LegacyValues(world[joint])}));
                    foreach(var mesh in geometries)
                    {
                        var skin=mesh.Value.Skin;var mapping=binding.Resolve(skin);var palette=binding.CreatePalette(world,mapping);var positions=SkeletalJointBinding.DeformPositions(skin,palette);
                        Console.WriteLine("lower_positions="+JsonSerializer.Serialize(new{detail,key=pair.Key,frame,path=mesh.Key,positions=positions.Select(p=>new[]{p.X,p.Y,p.Z})}));
                        if(frame==.5)
                        {
                            Reject<InvalidDataException>(()=>SkeletalJointBinding.DeformPositions(skin,palette.Take(palette.Count-1).ToArray()));
                            Reject<InvalidDataException>(()=>binding.Resolve(skin with{TransformNames=new[]{"absent_round2_joint"}}));
                        }
                    }
                }
                foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==prepared.ClipPath?new(kind):overlay.Lookup(p),root,objectPin,satPin,loadSkeletons:true,loadLogicalTables:true,animationSelection:LogicalAnimationProxyScenarios.IdleSelection,prepareSelectedAnimation:true,additionalAnimationSelections:SkeletalAnimationBankScenarios.Profile()));
                if(!retained[pair.Key].SequenceEqual(prepared.CreateLocalPose(binding,.5)))throw new Exception("Rejected/sample calls changed retained pose.");
            }
            foreach(var bytes in borrowed)Array.Clear(bytes);
            foreach(var pair in bank)if(!retained[pair.Key].SequenceEqual(pair.Value.CreateLocalPose(binding,.5)))throw new Exception("Borrowed clip mutation changed prepared pose.");
            foreach(var pair in geometries)if(hashes[pair.Key]!=SkeletalGeometryScenarios.CanonicalHash(pair.Value))throw new Exception("Borrowed mesh mutation changed geometry.");
        }
        Console.WriteLine("PASS R2-ASSETS-01: four explicit mesh closures and lower-detail original walk/run sampled deformation; explicit level0 skeletons, no root motion/events/GPU.");
    }
}
