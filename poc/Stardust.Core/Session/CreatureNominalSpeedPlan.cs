using System.Numerics;
namespace Stardust.Core.Session;

public readonly record struct CreatureNominalSpeedPlanStep(uint RunBits,uint WalkBits,byte RunFlag,
    CreatureNominalTargetDirection Direction,CreatureNominalTargetFactor Factor,uint SecondsBits,
    CreatureNominalRate? SuppliedRate=null);
public sealed record CreatureNominalSpeedPlanLimits(int MaximumSteps=4096,long MaximumCopyBytes=4194304,long MaximumWork=268435456);
public sealed class CreatureNominalSpeedPlanReceipt
{
    internal CreatureNominalSpeedPlanReceipt(int index,CreatureNominalSpeedPlanStep source,uint before,
        CreatureNominalTargetSpeedResult target,CreatureNominalSpeedResult transition)
    { Index=index;Source=source;CurrentBeforeBits=before;Target=target;Transition=transition; }
    public int Index { get; }
    public CreatureNominalSpeedPlanStep Source { get; }
    public uint CurrentBeforeBits { get; }
    public CreatureNominalTargetSpeedResult Target { get; }
    public CreatureNominalSpeedResult Transition { get; }
    public uint CurrentAfterBits => Transition.StoredBits;
}
/// <summary>Owned atomic nominal plan; no driver, getter, I/O or partial publication.</summary>
public sealed class CreatureNominalSpeedPlan
{
    public const int HardMaximumSteps=65536;
    public const long HardMaximumCopyBytes=67108864;
    public const long HardMaximumWork=4294967296;
    public const int AccountedCopyBytesPerStep=640;
    public const int TransitionWorkBoundPerStep=CreatureNominalSpeedTransition.HardMaximumWork;
    private CreatureNominalSpeedPlan(uint initial,uint final,CreatureNominalSpeedPrecision precision,
        CreatureNominalSpeedPlanReceipt[] receipts,long copy,long reserved,long targetWork,long transitionWork,
        int rates,int equals,int clamps)
    { InitialCurrentBits=initial;FinalCurrentBits=final;Precision=precision;Steps=Array.AsReadOnly(receipts);
      ReservedCopyBytes=copy;ReservedWork=reserved;ActualTargetWork=targetWork;ActualTransitionWork=transitionWork;
      RatesUsed=rates;Equalities=equals;Clamps=clamps; }
    public uint InitialCurrentBits { get; }
    public uint FinalCurrentBits { get; }
    public CreatureNominalSpeedPrecision Precision { get; }
    public IReadOnlyList<CreatureNominalSpeedPlanReceipt> Steps { get; }
    public int StepCount => Steps.Count;
    public int TargetEvaluations => StepCount;
    public int TransitionEvaluations => StepCount;
    public int RatesUsed { get; }
    public int Equalities { get; }
    public int Clamps { get; }
    public int TargetF32Stores => checked(2*StepCount);
    public int TransitionF32Stores => checked(StepCount-Equalities+Clamps);
    public long ReservedCopyBytes { get; }
    public long ReservedWork { get; }
    public long ActualTargetWork { get; }
    public long ActualTransitionWork { get; }
    public long ActualWork => checked(ActualTargetWork+ActualTransitionWork);
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static (long Copy,long Work) Preflight(uint initial,ReadOnlySpan<CreatureNominalSpeedPlanStep> steps,
        CreatureNominalSpeedPrecision precision,CreatureNominalSpeedPlanLimits? limits,CancellationToken token)
    {
        limits??=new();token.ThrowIfCancellationRequested();
        if(precision is not (CreatureNominalSpeedPrecision.NearestEven24 or CreatureNominalSpeedPrecision.NearestEven53 or CreatureNominalSpeedPrecision.NearestEven64))throw new ArgumentOutOfRangeException(nameof(precision));
        if(limits.MaximumSteps is <0 or >HardMaximumSteps||limits.MaximumCopyBytes is <0 or >HardMaximumCopyBytes||limits.MaximumWork is <0 or >HardMaximumWork)throw new ArgumentOutOfRangeException(nameof(limits));
        Need(Finite(initial),"Finite initial current required, including for an empty plan.");
        Need(steps.Length<=limits.MaximumSteps,"Nominal plan step count exceeds profile before copy.");
        long copy=checked((long)steps.Length*AccountedCopyBytesPerStep);
        Need(copy<=limits.MaximumCopyBytes,"Nominal plan copy reservation exceeds profile before copy.");
        long work=0;
        foreach(var s in steps)
        {
            token.ThrowIfCancellationRequested();
            Need(Finite(s.SecondsBits),"Finite caller seconds required before copy/arithmetic.");
            if(s.SuppliedRate is { } r)
            {
                Need(r.Exponent>=-CreatureNominalSpeedTransition.HardMaximumAbsoluteExponent&&r.Exponent<=CreatureNominalSpeedTransition.HardMaximumAbsoluteExponent,"Supplied transition rate exponent exceeds frozen profile.");
                int width=r.Magnitude==0?0:BitOperations.Log2(r.Magnitude)+1;
                Need(width<=CreatureNominalSpeedTransition.HardMaximumMagnitudeBits,"Supplied transition rate width exceeds frozen profile.");
            }
            int target=CreatureNominalTargetSpeed.GetRequiredWork(s.RunBits,s.WalkBits,s.RunFlag,s.Direction,s.Factor,precision);
            work=checked(work+target+TransitionWorkBoundPerStep);
            Need(work<=limits.MaximumWork,"Nominal plan total work reservation exceeds profile before copy/arithmetic.");
        }
        return (copy,work);
    }
    /// <summary>Primitive-only reservation; optional missing rates may fail an ordered computed transition later.</summary>
    public static (long CopyBytes,long Work) GetRequiredReservations(uint initialCurrentBits,ReadOnlySpan<CreatureNominalSpeedPlanStep> steps,
        CreatureNominalSpeedPrecision precision,CreatureNominalSpeedPlanLimits? limits=null,CancellationToken cancellationToken=default)
    { var r=Preflight(initialCurrentBits,steps,precision,limits,cancellationToken);return (r.Copy,r.Work); }
    public static CreatureNominalSpeedPlan Build(uint initialCurrentBits,ReadOnlySpan<CreatureNominalSpeedPlanStep> steps,
        CreatureNominalSpeedPrecision precision,CreatureNominalSpeedPlanLimits? limits=null,CancellationToken cancellationToken=default)
    {
        var initialReservation=Preflight(initialCurrentBits,steps,precision,limits,cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var owned=steps.ToArray();
        // Only this captured owned source is used; revalidate it before any arithmetic.
        var reservation=Preflight(initialCurrentBits,owned,precision,limits,cancellationToken);
        Need(reservation==initialReservation,"Source reservation changed during snapshot; caller must not mutate concurrently.");
        var receipts=new CreatureNominalSpeedPlanReceipt[owned.Length];
        uint current=initialCurrentBits;long targetWork=0,transitionWork=0;int rates=0,equals=0,clamps=0;
        for(int i=0;i<owned.Length;i++)
        {
            cancellationToken.ThrowIfCancellationRequested();var s=owned[i];
            var target=CreatureNominalTargetSpeed.Evaluate(s.RunBits,s.WalkBits,s.RunFlag,s.Direction,s.Factor,precision,cancellationToken:cancellationToken);
            var transition=CreatureNominalSpeedTransition.Evaluate(current,target.TargetBits,s.SecondsBits,precision,s.SuppliedRate,cancellationToken:cancellationToken);
            receipts[i]=new(i,s,current,target,transition);current=transition.StoredBits;
            targetWork=checked(targetWork+target.ReservedWork);transitionWork=checked(transitionWork+transition.ReservedWork);
            rates+=transition.RatesUsed;if(transition.Branch==CreatureNominalSpeedBranch.Equal)equals++;if(transition.Clamped)clamps++;
        }
        Need(targetWork+transitionWork<=reservation.Work,"Frozen transition work bound invariant.");
        cancellationToken.ThrowIfCancellationRequested();
        return new(initialCurrentBits,current,precision,receipts,reservation.Copy,reservation.Work,targetWork,transitionWork,rates,equals,clamps);
    }
}
