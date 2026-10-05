using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
if(args.Length!=2||File.Exists(args[1])||new FileInfo(args[0]).Length is <1 or >524288)throw new ArgumentException("Bounded fresh fixture/output");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=document.RootElement;
var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(root.GetProperty("GeneratorHex").GetString()!));var binding=TerrainGeneratorFractalFamilies.Bind(generator);
var slice=TerrainGeneratorHeightLayerSlice.Select(generator,binding,3,TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence);
if(slice.Height.HasOperationalInputs||slice.HasSupportedInputs||!slice.Filter.HasOperationalInputs||!slice.IsLocallyEnabled)throw new InvalidDataException("Exact original operational boundary");
bool rejected=false;try{_=TerrainGeneratorHeightLayerSlice.Select(generator,binding,3,TerrainGeneratorHeightLayerSlicePolicy.EnabledSupportedSlice);}catch(InvalidDataException){rejected=true;}
if(!rejected)throw new InvalidDataException("Unsupported original curve family accepted");
File.WriteAllText(args[1],JsonSerializer.Serialize(new{slice.LayerIndex,slice.ParentIndex,slice.Depth,slice.LayerOffset,slice.LayerBytes,slice.LayerSha256,slice.Children,slice.BoundaryInverted,slice.FilterInverted,slice.AdtaWord2Bits,HeightFamilyId=slice.Height.FamilyId,HeightOperationalInputs=slice.Height.HasOperationalInputs,OperationalRejected=rejected,OriginalHeightEvaluated=false})+"\n",new UTF8Encoding(false));
Console.WriteLine("PASS: original layer evidence retained; unsupported height family rejects");
