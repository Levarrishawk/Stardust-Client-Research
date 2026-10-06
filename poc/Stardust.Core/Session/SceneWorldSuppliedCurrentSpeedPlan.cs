namespace Stardust.Core.Session;

public readonly record struct SceneWorldSuppliedCurrentSpeedPlanStep(byte RunFlag,CreatureNominalTargetDirection Direction,
    CreatureNominalTargetFactor Factor,uint SecondsBits,CreatureNominalRate? SuppliedRate=null);
public sealed record SceneWorldSuppliedCurrentSpeedPlanLimits(int MaximumSteps=128,int MaximumOutputBytes=112928,
    long MaximumCopyBytes=110592,long MaximumWork=9445504);
public sealed class SceneWorldSuppliedCurrentSpeedPlanReceipt
{
    internal SceneWorldSuppliedCurrentSpeedPlanReceipt(object owner,SceneWorldSuppliedCurrentSpeedReceipt pair,
        CreatureNominalSpeedPlan plan,SceneWorldSuppliedCurrentSpeedPlanStep[] steps,int output,long copy,long work,long ownWork)
    { Owner=owner;Pair=pair;Plan=plan;Steps=Array.AsReadOnly(steps);OutputBytes=output;ReservedCopyBytes=copy;ReservedWork=work;OwnWork=ownWork; }
    internal object Owner { get; }
    public SceneWorldSuppliedCurrentSpeedReceipt Pair { get; }
    public CreatureNominalSpeedPlan Plan { get; }
    public IReadOnlyList<SceneWorldSuppliedCurrentSpeedPlanStep> Steps { get; }
    public long Generation => Pair.Generation;
    public SceneObjectIncarnation Incarnation => Pair.Incarnation;
    public string RootSha256 => Pair.RootSha256;
    public CreatureNominalSpeedPrecision Precision => Pair.Precision;
    public CreatureSuppliedCurrentSpeedPolicy Policy => Pair.Policy;
    public CreatureSuppliedCurrentSpeedProvenance Provenance => Pair.Provenance;
    public uint InitialCurrentBits => Plan.InitialCurrentBits;
    public uint FinalCurrentBits => Plan.FinalCurrentBits;
    public int OutputBytes { get; }
    public long ReservedCopyBytes { get; }
    public long ReservedWork { get; }
    public long OwnWork { get; }
    public long OwnCopyBytes => checked((long)Steps.Count*SceneWorldSuppliedCurrentSpeedPlan.OwnCopyBytesPerStep);
    public long ChildCopyBytes => Plan.ReservedCopyBytes;
    public long ChildReservedWork => Plan.ReservedWork;
    public long ActualChildWork => Plan.ActualWork;
    public int ReaderReloads => 0;
    public int TargetEvaluations => Plan.TargetEvaluations;
    public int TransitionEvaluations => Plan.TransitionEvaluations;
    public int RatesUsed => Plan.RatesUsed;
    public int TargetF32Stores => Plan.TargetF32Stores;
    public int TransitionF32Stores => Plan.TransitionF32Stores;
}
/// <summary>Owner-thread atomic plan over an existing current paired snapshot; never reloads its reader.</summary>
public sealed class SceneWorldSuppliedCurrentSpeedPlan : IDisposable
{
    public const int HardMaximumSteps=128;
    public const int HeaderOutputBytes=256+SceneWorldSuppliedCurrentSpeedInputs.OutputBytes;
    public const int OwnedStepCopyBytes=96;
    public const int MappedStepCopyBytes=128;
    public const int OwnCopyBytesPerStep=OwnedStepCopyBytes+MappedStepCopyBytes;
    public const int TotalCopyBytesPerStep=OwnCopyBytesPerStep+CreatureNominalSpeedPlan.AccountedCopyBytesPerStep;
    public const int HardMaximumOutputBytes=HeaderOutputBytes+HardMaximumSteps*TotalCopyBytesPerStep;
    public const long HardMaximumCopyBytes=(long)HardMaximumSteps*TotalCopyBytesPerStep;
    public const long HeaderWork=128;
    public const long OwnWorkPerStep=64;
    public const long HardMaximumWork=HeaderWork+HardMaximumSteps*OwnWorkPerStep+(long)HardMaximumSteps*(CreatureNominalTargetSpeed.HardMaximumWork+CreatureNominalSpeedPlan.TransitionWorkBoundPerStep);
    private readonly int ownerThread=Environment.CurrentManagedThreadId;
    private readonly object identity=new();
    private readonly SceneWorldSuppliedCurrentSpeedPlanLimits limits;
    private SceneWorldSuppliedCurrentSpeedInputs? adapter;
    private bool busy,closed;
    public SceneWorldSuppliedCurrentSpeedPlan(SceneWorldSuppliedCurrentSpeedInputs adapter,SceneWorldSuppliedCurrentSpeedPlanLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(adapter);var p=limits??new();
        if(p.MaximumSteps is <0 or >HardMaximumSteps||p.MaximumOutputBytes is <0 or >HardMaximumOutputBytes||p.MaximumCopyBytes is <0 or >HardMaximumCopyBytes||p.MaximumWork is <0 or >HardMaximumWork)throw new ArgumentOutOfRangeException(nameof(limits));
        Need(p.MaximumOutputBytes>=HeaderOutputBytes&&p.MaximumWork>=HeaderWork,"Composition fixed header reservation exhausted before borrowed access.");
        this.adapter=adapter;this.limits=p with { };
    }
    private static void Need(bool value,string why) { if(!value)throw new InvalidDataException(why); }
    private static bool Finite(uint b)=>(b&0x7f800000)!=0x7f800000;
    private void Enter()
    { if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Current speed-plan owner thread required.");
      if(busy)throw new InvalidOperationException("Current speed-plan reentry.");busy=true; }
    private static (int Output,long Copy,long Work,long ChildCopy,long ChildWork,long OwnWork) Preflight(
        SceneWorldSuppliedCurrentSpeedReceipt pair,uint initial,ReadOnlySpan<SceneWorldSuppliedCurrentSpeedPlanStep> steps,
        SceneWorldSuppliedCurrentSpeedPlanLimits limits,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();Need(Finite(initial),"Finite initial current required even for an empty plan.");
        Need(pair.Policy==CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly&&pair.Precision==pair.Run.Precision&&pair.Precision==pair.Walk.Precision,"Actual pair policy/precision required.");
        Need(steps.Length<=limits.MaximumSteps,"Composition step cap exhausted before copies.");
        long childCopy=(long)steps.Length*CreatureNominalSpeedPlan.AccountedCopyBytesPerStep;
        long copy=(long)steps.Length*TotalCopyBytesPerStep;int output=checked(HeaderOutputBytes+(int)copy);
        Need(copy<=limits.MaximumCopyBytes&&output<=limits.MaximumOutputBytes,"Composition output/copy reservation exhausted before copies.");
        long childWork=0,ownWork=HeaderWork+OwnWorkPerStep*steps.Length;
        foreach(var s in steps)
        {
            token.ThrowIfCancellationRequested();Need(Finite(s.SecondsBits),"Finite step seconds required before copying/arithmetic.");
            if(s.SuppliedRate is { } r)Need(r.Exponent>=-CreatureNominalSpeedTransition.HardMaximumAbsoluteExponent&&r.Exponent<=CreatureNominalSpeedTransition.HardMaximumAbsoluteExponent,"Supplied rate exponent exceeds frozen profile.");
            int targetWork=CreatureNominalTargetSpeed.GetRequiredWork(pair.Run.ReturnBits,pair.Walk.ReturnBits,s.RunFlag,s.Direction,s.Factor,pair.Precision);
            childWork=checked(childWork+targetWork+CreatureNominalSpeedPlan.TransitionWorkBoundPerStep);
            Need(ownWork+childWork<=limits.MaximumWork,"Aggregate own and child work exhausted before copies/arithmetic.");
        }
        Need(ownWork+childWork<=limits.MaximumWork,"Empty plan own-work reservation exhausted.");
        return (output,copy,ownWork+childWork,childCopy,childWork,ownWork);
    }
    public SceneWorldSuppliedCurrentSpeedPlanReceipt? Build(SceneWorldSuppliedCurrentSpeedReceipt pair,uint initialCurrentBits,
        ReadOnlySpan<SceneWorldSuppliedCurrentSpeedPlanStep> steps,CancellationToken cancellationToken=default)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(closed,this);ArgumentNullException.ThrowIfNull(pair);cancellationToken.ThrowIfCancellationRequested();
            Need(steps.Length<=limits.MaximumSteps,"Composition step count exhausted before borrowed freshness check.");
            if(!adapter!.IsCurrent(pair))return null;
            var initial=Preflight(pair,initialCurrentBits,steps,limits,cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();var owned=steps.ToArray();
            var reservation=Preflight(pair,initialCurrentBits,owned,limits,cancellationToken);
            Need(reservation==initial,"Source reservation changed during copy; concurrent source mutation is unsupported.");
            var mapped=new CreatureNominalSpeedPlanStep[owned.Length];
            for(int i=0;i<owned.Length;i++)
            { cancellationToken.ThrowIfCancellationRequested();var s=owned[i];mapped[i]=new(pair.Run.ReturnBits,pair.Walk.ReturnBits,s.RunFlag,s.Direction,s.Factor,s.SecondsBits,s.SuppliedRate); }
            var child=CreatureNominalSpeedPlan.Build(initialCurrentBits,mapped,pair.Precision,
                new(owned.Length,reservation.ChildCopy,reservation.ChildWork),cancellationToken);
            Need(child.ReservedCopyBytes==reservation.ChildCopy&&child.ReservedWork==reservation.ChildWork,"Actual frozen child reservation invariant.");
            cancellationToken.ThrowIfCancellationRequested();if(!adapter.IsCurrent(pair))return null;
            var result=new SceneWorldSuppliedCurrentSpeedPlanReceipt(identity,pair,child,owned,reservation.Output,reservation.Copy,reservation.Work,reservation.OwnWork);
            cancellationToken.ThrowIfCancellationRequested();return result;
        }finally{busy=false;}
    }
    public bool IsCurrent(SceneWorldSuppliedCurrentSpeedPlanReceipt receipt)
    {
        Enter();try { ArgumentNullException.ThrowIfNull(receipt);return !closed&&ReferenceEquals(receipt.Owner,identity)&&adapter!.IsCurrent(receipt.Pair); }
        finally{busy=false;}
    }
    public void Dispose()
    { Enter();try { if(closed)return;adapter=null;closed=true; }finally{busy=false;} }
}
