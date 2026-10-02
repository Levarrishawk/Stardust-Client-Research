using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalDetailSelectionScenarios
{
    internal static void Run()
    {
        void Reject<T>(Action a) where T:Exception {try{a();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
        var supplied=new[]{240d,120,60};var policy=new SkeletalDetailSelectionPolicy(supplied,.1);supplied[0]=1;
        if(policy.DescendingThresholds[0]!=240)throw new Exception("Policy borrowed caller thresholds.");
        Reject<NotSupportedException>(()=>((IList<double>)policy.DescendingThresholds)[0]=1);
        foreach(var invalid in new[]{Array.Empty<double>(),new[]{240d,120},new[]{240d,120,120},new[]{60d,120,240},new[]{double.NaN,120,60},new[]{double.PositiveInfinity,120,60},new[]{240d,120,0},new[]{1e10,120,60}})
            Reject<ArgumentException>(()=>new SkeletalDetailSelectionPolicy(invalid,.1));
        foreach(double h in new[]{double.NaN,double.PositiveInfinity,-.1,.5})Reject<ArgumentOutOfRangeException>(()=>new SkeletalDetailSelectionPolicy(new[]{240d,120,60},h));
        var selector=new SkeletalDetailSelector(policy);
        void Reset(string group){selector.Reset();if(selector.Current is not null)throw new Exception("Reset retained selection.");Console.WriteLine("detail_reset="+JsonSerializer.Serialize(new{group}));}
        void Select(string group,SkeletalDetailProjectionSample sample)
        {
            var result=selector.Select(sample);if(selector.Current!=result)throw new Exception("Published selection differs.");
            Console.WriteLine("detail_sample="+JsonSerializer.Serialize(new{group,sample,result}));
        }
        // FOV90/height1024/radius1: diameter ~=1024/depth. These are supplied projection fixtures.
        foreach(string group in new[]{"descending","ascending"})
        {
            Reset(group);var pixels=new[]{600d,300,264,240,216,215,132,120,108,107,66,60,54,53,30};
            if(group=="ascending")Array.Reverse(pixels);
            foreach(double p in pixels)Select(group,new(1,1024/p,90,1024));
        }
        Reset("jitter");Select("jitter",new(1,1024/230d,90,1024));
        for(int i=0;i<24;i++)Select("jitter",new(1,1024/(i%2==0?239d:241d),90,1024));
        var unchanged=selector.Current;for(int i=0;i<4;i++)Select("repeat",new(1,1024/241d,90,1024));if(selector.Current!=unchanged)throw new Exception("Repeated sample differs.");
        foreach(var sample in new[]{new SkeletalDetailProjectionSample(1,0,90,1024),new(1,.5,90,1024),new(1,1,90,1024),new(1,1.000001,90,1024),new(2,10,45,720),new(.01,1e6,179,1),new(1e6,1e9,1,32768)})Select("projection",sample);
        var valid=new SkeletalDetailProjectionSample(1,10,90,1024);
        foreach(var sample in new[]{valid with{BoundingRadius=0},valid with{BoundingRadius=-1},valid with{BoundingRadius=double.NaN},valid with{BoundingRadius=double.PositiveInfinity},valid with{BoundingRadius=1e6+1},valid with{BoundingRadius=double.Epsilon,CenterDepth=1e9},valid with{CenterDepth=-1},valid with{CenterDepth=double.NaN},valid with{CenterDepth=double.PositiveInfinity},valid with{CenterDepth=1e9+1},valid with{VerticalFovDegrees=0},valid with{VerticalFovDegrees=180},valid with{VerticalFovDegrees=double.NaN},valid with{ViewportHeight=0},valid with{ViewportHeight=32769}})
        {
            var before=selector.Current;Reject<ArgumentOutOfRangeException>(()=>selector.Select(sample));if(selector.Current!=before)throw new Exception("Invalid sample mutated current.");
        }
        selector.Reset();Reject<ArgumentOutOfRangeException>(()=>selector.Select(valid with{CenterDepth=-1}));if(selector.Current is not null)throw new Exception("Invalid first sample initialized selector.");
        // Exact arithmetic construction: threshold equals the projection output for this sample.
        var exactSample=new SkeletalDetailProjectionSample(1,8,90,1024);double exact=1024d/8/Math.Tan(Math.PI/4);
        var exactSelector=new SkeletalDetailSelector(new(new[]{exact,exact/2,exact/4},0));
        if(exactSelector.Select(exactSample).Detail!=0)throw new Exception("Exact initial boundary did not prefer finer detail.");
        exactSelector.Select(exactSample with{CenterDepth=16});if(exactSelector.Select(exactSample).Detail!=0)throw new Exception("Exact upward boundary did not refine.");
        Console.WriteLine("detail_exact="+JsonSerializer.Serialize(new{sample=exactSample,threshold=exact,detail=exactSelector.Current!.Value.Detail}));
        foreach(bool upward in new[]{false,true})
        {
            double threshold=exact/(upward?1.25:.75);if(threshold*(upward?1.25:.75)!=exact)throw new Exception("Hysteresis fixture is not an exact boundary.");
            var boundary=new SkeletalDetailSelector(new(new[]{threshold,threshold/2,threshold/4},.25));
            boundary.Select(exactSample with{CenterDepth=upward?64:2});
            if(upward)boundary.Select(exactSample with{CenterDepth=double.BitIncrement(8)});
            int before=boundary.Current!.Value.Detail;var result=boundary.Select(exactSample);if(result.Detail!=0)throw new Exception("Exact hysteresis boundary differs.");
            Console.WriteLine("detail_hysteresis_boundary="+JsonSerializer.Serialize(new{upward,threshold,before,sample=exactSample,result}));
            if(!upward&&boundary.Select(exactSample with{CenterDepth=double.BitIncrement(8)}).Detail!=1)throw new Exception("Just below lower gate did not coarsen.");
        }
        foreach(double h in new[]{0d,.49})
        {
            var s=new SkeletalDetailSelector(new(new[]{240d,120,60},h));
            foreach(double depth in new[]{2d,8,32,8,2})Console.WriteLine("detail_alternate="+JsonSerializer.Serialize(new{h,sample=new SkeletalDetailProjectionSample(1,depth,90,1024),result=s.Select(new(1,depth,90,1024))}));
        }
        Console.WriteLine("PASS R4-ASSETS-01: modern projection/hysteresis sweeps, equality, near/inside, repeated/reset, bounded atomic rejection and immutable policy ownership.");
    }
}
