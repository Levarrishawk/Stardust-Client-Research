using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainSeededNoiseKernelScenarios
{
    private static void Need(bool value) { if(!value) throw new Exception("Seeded kernel assertion."); }
    private static string Hash(IEnumerable<uint> words)
    { uint[] all = words.ToArray(); byte[] bytes = new byte[all.Length*4]; for(int i=0;i<all.Length;i++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i*4),all[i]); return Convert.ToHexString(SHA256.HashData(bytes)); }
    private static string Fraction(TerrainSeededNoiseKernel.Sample s)
    { BigInteger n = s.Magnitude, d = BigInteger.One; if(s.Negative)n=-n;if(s.Exponent>=0)n<<=s.Exponent;else d<<=-s.Exponent;BigInteger g=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/g}/{d/g}"; }
    public static object Run(string fixturePath)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixturePath));var output=new List<object>();
        var kernels=new Dictionary<(int,int),TerrainSeededNoiseKernel>();
        try
        {
            foreach(var f in document.RootElement.EnumerateArray())
            {
                string name=f.GetProperty("Name").GetString()!;int seed=unchecked((int)f.GetProperty("Seed").GetUInt32()),tp=f.GetProperty("TablePrecision").GetInt32();
                try
                {
                    if(!kernels.TryGetValue((seed,tp),out var kernel)) {kernel=TerrainSeededNoiseKernel.Create(seed,tp,TerrainSeededNoiseKernel.TableBytes,TerrainSeededNoiseKernel.BuildWork);kernels.Add((seed,tp),kernel);}
                    if(f.GetProperty("Kind").GetString()=="Tables")
                    {var t=kernel.CopyTables(514*16);output.Add(new{Name=name,Reject="None",PermutationSha=Hash(t.Permutation),Gradient1Sha=Hash(t.Scalar),Gradient2Sha=Hash(t.GradientPairs),RngDraws=t.Draws,RngSteps=t.Steps});}
                    else
                    {
                        var result=kernel.Query([new(f.GetProperty("X").GetUInt32(),f.GetProperty("Z").GetUInt32())],f.GetProperty("Precision").GetInt32(),40,256,true)[0];
                        output.Add(new{Name=name,Reject="None",result.Magnitude,result.Negative,result.Exponent,Retained=Fraction(result),NegativeZero=result.Magnitude==0&&result.Negative,Projection=result.InspectionBits});
                    }
                }
                catch(ArgumentException){output.Add(new{Name=name,Reject="Input"});}
                catch(InvalidDataException){output.Add(new{Name=name,Reject="Input"});}
            }
        }
        finally{foreach(var k in kernels.Values)k.Dispose();}
        int negatives=0;
        void Reject(Action action)
        {try{action();throw new Exception("Expected explicit failure.");}catch(Exception e)when(e is ArgumentException or InvalidDataException or InvalidOperationException or OperationCanceledException or NotSupportedException){negatives++;}}
        foreach(int p in new[]{0,23,65})Reject(()=>TerrainSeededNoiseKernel.Create(1,p,10000,20000));
        Reject(()=>TerrainSeededNoiseKernel.Create(int.MaxValue,53,10000,20000));
        Reject(()=>TerrainSeededNoiseKernel.Create(1,53,0,20000));Reject(()=>TerrainSeededNoiseKernel.Create(1,53,10000,0));
        Reject(()=>TerrainSeededNoiseKernel.Create(1,53,int.MaxValue,20000));Reject(()=>TerrainSeededNoiseKernel.Create(1,53,10000,int.MaxValue));
        using var cancel=new CancellationTokenSource();cancel.Cancel();Reject(()=>TerrainSeededNoiseKernel.Create(1,53,10000,20000,cancel.Token));
        using var owned=TerrainSeededNoiseKernel.Create(47,53,10000,20000);
        var point=new TerrainSeededNoiseKernel.Coordinate(0x3f000000,0x3e800000);var before=owned.Query([point],64,40,256,true)[0];
        Reject(()=>owned.Query([point],23,40,256));Reject(()=>owned.Query([point],53,39,256));Reject(()=>owned.Query([point],53,40,255));
        Reject(()=>owned.Query(new TerrainSeededNoiseKernel.Coordinate[4097],53,4097*40,4097*256));
        Reject(()=>owned.Query([point,new(0x7fc12345,0)],53,80,512));Reject(()=>owned.Query([point,new(0x4f800000,0)],53,80,512));
        Reject(()=>owned.Query([point],53,40,256,false,cancel.Token));Reject(()=>owned.CopyTables(0));Reject(()=>owned.CopyTables(514*16,cancel.Token));
        var snapshot=owned.CopyTables(514*16);Reject(()=>((IList<uint>)snapshot.Permutation)[0]=0);
        Need(owned.Query([point],64,40,256,true)[0]==before);
        Need(owned.Query([point],64,40,256,false)[0].InspectionBits is null);
        Need(owned.Query([],53,0,0).Count==0);
        foreach(int s in new[]{0,-1,int.MinValue}){using var a=TerrainSeededNoiseKernel.Create(s,53,10000,20000);using var b=TerrainSeededNoiseKernel.Create(1,53,10000,20000);Need(Hash(a.CopyTables(514*16).GradientPairs)==Hash(b.CopyTables(514*16).GradientPairs));}
        var thread=new Thread(()=>{Reject(()=>owned.Query([point],53,40,256));Reject(()=>owned.CopyTables(514*16));Reject(()=>owned.Dispose());});thread.Start();thread.Join();
        Need(owned.Query([point],64,40,256,true)[0]==before);
        owned.Dispose();owned.Dispose();Reject(()=>owned.Query([point],53,40,256));Reject(()=>owned.CopyTables(514*16));Need(snapshot.Permutation.Count==514);
        return new{Cases=output,NegativeChecks=negatives,AtomicLateFailure=true,ImmutableSnapshots=true,OwnerRecovery=true,Reentry="No callbacks or external enumerators during an operation; active-operation guard precedes every public operation."};
    }
}
