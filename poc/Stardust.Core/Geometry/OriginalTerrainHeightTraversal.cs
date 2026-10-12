using Stardust.Core.Assets;

namespace Stardust.Core.Geometry;

/// <summary>Bounded original height traversal. Kernel controls are explicit modern choices, not original numeric parity.</summary>
public sealed class OriginalTerrainHeightTraversal
{
    private const TerrainHeightArithmeticPrecision P=TerrainHeightArithmeticPrecision.Binary64;
    private readonly TerrainGeneratorInput generator;
    private readonly TerrainGeneratorFractalFamilies families;
    private readonly Dictionary<int,bool> heightSubtree=new();
    private readonly Dictionary<int,Func<uint,uint,uint?>> boundaries=new();
    public HashSet<int> AppliedHeightLayers {get;}=new();
    public HashSet<int> ZeroInfluenceLayers {get;}=new();
    public OriginalTerrainHeightTraversal(TerrainGeneratorInput generator)
    {
        this.generator=generator;families=TerrainGeneratorFractalFamilies.Bind(generator);
        foreach(var l in generator.Layers.Reverse())heightSubtree[l.Index]=l.Header?.IsEnabled==true&&
            (generator.Rules.Any(r=>r.LayerIndex==l.Index&&IsHeight(r.Section.FormType))||
            generator.Layers.Any(c=>c.ParentIndex==l.Index&&heightSubtree[c.Index]));
        foreach(var r in generator.Rules.Where(r=>r.Section.FormType?.StartsWith('B')==true))
        {
            var s=r.Section;
            boundaries[s.Offset]=s.FormType switch
            {
                "BCIR"=>Circle(TerrainBoundaryCircleInput.Decode(s)),
                "BREC"=>Rectangle(TerrainBoundaryRectangleInput.Decode(s)),
                "BPOL"=>Polygon(TerrainBoundaryPolygonInput.Decode(s)),
                "BPLN"=>Polyline(TerrainBoundaryPolylineInput.Decode(s)),
                _=>throw new InvalidDataException("Unsupported original boundary: "+s.FormType)
            };
        }
    }
    public uint[] Evaluate(TerrainSampleCoordinateGrid grid,CancellationToken token=default)
    {
        var pairs=grid.Layer.CopyPairs();int axis=grid.Layer.Count,n=pairs.Length;
        var requests=pairs.Select(p=>new TerrainConditionalFractalSampler.Request(p.Column,p.Row,p.FirstBits,p.SecondBits)).ToArray();
        var heights=new uint[n];
        TerrainSuppliedFractalSample[] Sample(TerrainFractalRawInput raw)
        {
            if(raw.Fields.FirstCurveEnabled)
                return TerrainFirstCurveFractalBatch.Build(raw.CopyEncodedBytes(),axis,axis,requests,new(53,64,64),
                    TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation,cancellation:token).Cells.Select(c=>c.Approximation.ToSuppliedSample()).ToArray();
            using var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(raw,axis,axis,53,64,1048576,1048576,cancellation:token);
            return sampler.Query(requests,64,2097152,16777216,token).Select(s=>s.Sample).ToArray();
        }
        void Visit(TerrainGeneratorLayer layer,float[] incoming)
        {
            token.ThrowIfCancellationRequested();if(!heightSubtree[layer.Index])return;
            var rules=generator.Rules.Select((r,i)=>(Rule:r,Index:i)).Where(r=>r.Rule.LayerIndex==layer.Index).ToArray();
            var bs=rules.Where(r=>r.Rule.Section.FormType?.StartsWith('B')==true).ToArray();
            var flags=layer.CopyAdtaWordBits();if(flags.Length<2)throw new InvalidDataException("Unsupported layer flags.");
            var weights=new float[n];
            for(int i=0;i<n;i++)
            {
                float amount=0;bool any=false;
                foreach(var r in bs)
                {
                    uint? b=boundaries[r.Rule.Section.Offset](pairs[i].FirstBits,pairs[i].SecondBits);
                    if(b is uint bits){any=true;amount=Math.Max(amount,F(bits));}
                }
                if(!any)amount=1;if(flags[0]!=0)amount=1-amount;
                weights[i]=amount;
            }
            if(!weights.Where((w,i)=>w*incoming[i]>0).Any()){ZeroInfluenceLayers.Add(layer.Index);return;}
            var boundaryPositive=weights.Select(w=>w>0).ToArray();
            foreach(var r in rules.Where(r=>r.Rule.Section.FormType?.StartsWith('F')==true))
            {
                var section=r.Rule.Section;
                if(section.FormType=="FFRA")
                {
                    var f=TerrainGeneratorFractalFilterRule.Select(generator,families,r.Index,TerrainGeneratorFractalFilterRulePolicy.TypedEvidence,token:token);
                    if(!f.IsEnabled)continue;var samples=Sample(f.Family.Raw);
                    for(int i=0;i<n;i++)if(weights[i]>0)weights[i]=Math.Min(weights[i],F(TerrainFilterFractalInfluence.Evaluate(f.Input,samples[i],P).Consumer.InfluenceBits!.Value));
                }
                else if(section.FormType=="FHGT")
                {
                    var f=TerrainFilterHeightInput.Decode(section);if(f.Header?.IsEnabled!=true)continue;
                    for(int i=0;i<n;i++)if(weights[i]>0)weights[i]=Math.Min(weights[i],F(TerrainFilterHeightInfluence.Evaluate(f,heights[i],P).InfluenceBits!.Value));
                }
                else throw new InvalidDataException($"Unsupported applicable original height filter {section.FormType}, layer{layer.Index}.");
            }
            for(int i=0;i<n;i++){if(boundaryPositive[i]&&flags[1]!=0)weights[i]=1-weights[i];weights[i]*=incoming[i];}
            if(!weights.Any(w=>w>0)){ZeroInfluenceLayers.Add(layer.Index);return;}
            foreach(var r in rules.Where(r=>IsHeight(r.Rule.Section.FormType)))
            {
                if(r.Rule.Section.FormType=="AHCN")
                {
                    var h=r.Rule.HeightConstant??throw new InvalidDataException("Unsupported height constant.");if(!h.Header.IsEnabled)continue;
                    AppliedHeightLayers.Add(layer.Index);for(int i=0;i<n;i++)heights[i]=TerrainHeightConstantOperation.Evaluate(h,heights[i],B(weights[i]),P).ResultHeightBits;
                }
                else if(r.Rule.Section.FormType=="AHFR")
                {
                    var h=TerrainGeneratorHeightFractalRule.Select(generator,families,r.Index,TerrainGeneratorHeightFractalRulePolicy.TypedEvidence,token:token);
                    if(!h.IsEnabled)continue;TerrainSuppliedFractalSample[] samples;
                    try{samples=Sample(h.Family.Raw);}catch(InvalidDataException e){throw new InvalidDataException($"Layer{layer.Index} height family{h.FamilyId}: "+e.Message,e);}
                    AppliedHeightLayers.Add(layer.Index);
                    for(int i=0;i<n;i++)heights[i]=TerrainSuppliedHeightFractal.Evaluate(h.Input,heights[i],B(weights[i]),samples[i],P).Consumer.ResultHeightBits;
                }
                else if(r.Rule.Section.FormType=="AROA"&&RoadExcluded(r.Rule.Section,pairs))continue;
                else throw new InvalidDataException($"Unsupported applicable original height writer {r.Rule.Section.FormType}, layer{layer.Index}.");
            }
            foreach(var child in generator.Layers.Where(c=>c.ParentIndex==layer.Index))Visit(child,weights);
        }
        foreach(var layer in generator.Layers.Where(l=>l.ParentIndex is null))Visit(layer,Enumerable.Repeat(1f,n).ToArray());
        return heights;
    }
    private static bool IsHeight(string? tag)=>tag?.StartsWith("AH")==true||tag is "AROA" or "ARIV";
    private static bool RoadExcluded(TerrainGeneratorSection section,TerrainSampleCoordinatePair[] pairs)
    {
        // NGE AROA0005 correlation only: conservative point bounds expanded by full width.
        // A road intersecting the supplied region remains unsupported and is rejected.
        var root=IffDocument.Parse(section.CopyEncodedBytes()).Root;
        if(!root.IsForm("AROA")||root.Children.Count!=1||!root.Children[0].IsForm("0005"))return false;
        var parts=root.Children[0].Children;
        if(parts.Count!=2||!parts[0].IsForm("IHDR")||!parts[1].IsForm("DATA"))return false;
        var data=parts[1].Children.LastOrDefault();if(data is null||data.Tag!="DATA"||data.FormType is not null)return false;
        var raw=data.Payload.Span;if(raw.Length<4)return false;int n=System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw);
        if(n is <2 or >8192||raw.Length!=4+8*n+24)return false;
        float Float(int at)=>BitConverter.Int32BitsToSingle(System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data.Payload.Span[at..]));
        float width=Float(4+8*n);if(!float.IsFinite(width)||width<0)return false;
        float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minZ=minX,maxZ=maxX;
        for(int i=0;i<n;i++){float x=Float(4+8*i),z=Float(8+8*i);if(!float.IsFinite(x)||!float.IsFinite(z))return false;minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minZ=Math.Min(minZ,z);maxZ=Math.Max(maxZ,z);}
        return pairs.All(p=>F(p.FirstBits)<minX-width||F(p.FirstBits)>maxX+width||F(p.SecondBits)<minZ-width||F(p.SecondBits)>maxZ+width);
    }
    private static uint B(float f)=>BitConverter.SingleToUInt32Bits(f);
    private static float F(uint b)=>BitConverter.UInt32BitsToSingle(b);
    private static Func<uint,uint,uint?> Circle(TerrainBoundaryCircleInput input)=>
        (x,z)=>TerrainBoundaryCircleInfluence.Evaluate(input,x,z,P,P).InfluenceBits;
    private static Func<uint,uint,uint?> Rectangle(TerrainBoundaryRectangleInput input)=>
        (x,z)=>TerrainBoundaryRectangleInfluence.Evaluate(input,x,z,P,P).InfluenceBits;
    private static Func<uint,uint,uint?> Polygon(TerrainBoundaryPolygonInput input)=>
        (x,z)=>TerrainBoundaryPolygonInfluence.Evaluate(input,x,z,P,P).InfluenceBits;
    private static Func<uint,uint,uint?> Polyline(TerrainBoundaryPolylineInput input)=>
        (x,z)=>TerrainBoundaryPolylineInfluence.Evaluate(input,x,z,P,P).InfluenceBits;
}
