using Stardust.Core.Assets;
if(args.Length==0){SkeletalVertexLitSourceScenarios.RunMath();Console.WriteLine("PASS: modern mouth synthetic color staging");return;}
if(args.Length!=2)throw new ArgumentException("Expected archive root and explicit configuration path.");
using var config=File.OpenText(args[1]);
using var overlay=ConfiguredTreOverlay.Open(args[0],config);
var effect=StaticEffectDecoder.Decode(overlay.Lookup("effect/h_color2_specmap.eft").Payload!);
SkeletalVertexLitSourceScenarios.Run(overlay.Lookup,effect);
overlay.Dispose();
try{overlay.Lookup("effect/h_color2_specmap.eft");throw new Exception("Disposed overlay accepted lookup.");}catch(ObjectDisposedException){}
Console.WriteLine("PASS: original overlay disposed deterministically");
