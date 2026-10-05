namespace Stardust.Core.Session;

public sealed record SceneWorldSuppliedCurrentSpeedLimits(int MaximumOutputBytes=2080,int MaximumCopyBytes=256,
    int MaximumWork=16384,int MaximumReaders=2,int MaximumReaderCopyBytes=128,int MaximumReaderWork=8192);
public sealed class SceneWorldSuppliedCurrentSpeedReceipt
{
    internal SceneWorldSuppliedCurrentSpeedReceipt(object owner,SceneWorldCreatureMovementCurrentReceipt view,
        SceneCreatureMovementCurrentField runBase,SceneCreatureMovementCurrentField walkBase,
        SceneCreatureMovementCurrentField first,SceneCreatureMovementCurrentField second,
        CreatureSuppliedCurrentSpeedResult run,CreatureSuppliedCurrentSpeedResult walk)
    { Owner=owner;View=view;RunBase=runBase;WalkBase=walkBase;FirstModifier=first;SecondModifier=second;Run=run;Walk=walk; }
    internal object Owner { get; }
    public SceneWorldCreatureMovementCurrentReceipt View { get; }
    public SceneCreatureMovementCurrentField RunBase { get; }
    public SceneCreatureMovementCurrentField WalkBase { get; }
    public SceneCreatureMovementCurrentField FirstModifier { get; }
    public SceneCreatureMovementCurrentField SecondModifier { get; }
    public CreatureSuppliedCurrentSpeedResult Run { get; }
    public CreatureSuppliedCurrentSpeedResult Walk { get; }
    public long Generation => View.Generation;
    public SceneObjectIncarnation Incarnation => View.Incarnation;
    public string RootSha256 => View.RootSha256;
    public CreatureNominalSpeedPrecision Precision => Run.Precision;
    public CreatureSuppliedCurrentSpeedPolicy Policy => Run.Policy;
    public CreatureSuppliedCurrentSpeedProvenance Provenance => Run.Provenance;
    public int OutputBytes => SceneWorldSuppliedCurrentSpeedInputs.OutputBytes;
    public int ReservedCopyBytes => SceneWorldSuppliedCurrentSpeedInputs.CopyBytes;
    public int ReservedWork => SceneWorldSuppliedCurrentSpeedInputs.WorkBound;
    public int ActualWork => checked(Run.ReservedWork+Walk.ReservedWork);
    public int ReaderCalls => 2;
    public int SuppliedWords => Run.SuppliedWords+Walk.SuppliedWords;
    public int Multiplications => Run.Multiplications+Walk.Multiplications;
    public int CallerF32Stores => Run.CallerF32Stores+Walk.CallerF32Stores;
}
/// <summary>Owner-thread paired snapshot over an existing borrowed current-input reader.</summary>
public sealed class SceneWorldSuppliedCurrentSpeedInputs : IDisposable
{
    public const int OutputBytes=2080;
    public const int CopyBytes=2*CreatureSuppliedCurrentSpeedReader.AccountedCopyBytes;
    public const int WorkBound=2*CreatureSuppliedCurrentSpeedReader.HardMaximumWork;
    private readonly int ownerThread=Environment.CurrentManagedThreadId;
    private readonly object identity=new();
    private readonly CreatureSuppliedCurrentSpeedPolicy policy;
    private readonly CreatureNominalSpeedPrecision precision;
    private readonly SceneWorldSuppliedCurrentSpeedLimits limits;
    private SceneWorldCreatureMovementCurrentInputs? reader;
    private bool busy,closed;
    public SceneWorldSuppliedCurrentSpeedInputs(SceneWorldCreatureMovementCurrentInputs reader,
        CreatureSuppliedCurrentSpeedPolicy policy,CreatureNominalSpeedPrecision precision,
        SceneWorldSuppliedCurrentSpeedLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(reader);var p=limits??new();
        if(policy!=CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly)throw new ArgumentOutOfRangeException(nameof(policy));
        if(precision is not (CreatureNominalSpeedPrecision.NearestEven24 or CreatureNominalSpeedPrecision.NearestEven53 or CreatureNominalSpeedPrecision.NearestEven64))throw new ArgumentOutOfRangeException(nameof(precision));
        if(p.MaximumOutputBytes is <0 or >65536||p.MaximumCopyBytes is <0 or >CopyBytes||p.MaximumWork is <0 or >WorkBound||p.MaximumReaders is <0 or >2||p.MaximumReaderCopyBytes is <0 or >128||p.MaximumReaderWork is <0 or >8192)throw new ArgumentOutOfRangeException(nameof(limits));
        Reserve(p);this.reader=reader;this.policy=policy;this.precision=precision;this.limits=p with { };
    }
    private static void Need(bool v,string why) { if(!v)throw new InvalidDataException(why); }
    private static void Reserve(SceneWorldSuppliedCurrentSpeedLimits p)
    { Need(p.MaximumOutputBytes>=OutputBytes&&p.MaximumCopyBytes>=CopyBytes&&p.MaximumWork>=WorkBound&&p.MaximumReaders>=2&&p.MaximumReaderCopyBytes>=128,"Paired current-speed fixed reservation exhausted before borrowed read."); }
    private void Enter()
    { if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Paired current-speed owner thread required.");
      if(busy)throw new InvalidOperationException("Paired current-speed reentry.");busy=true; }
    private static uint Bits(SceneCreatureMovementCurrentField f)
    { Need(f.Eligibility==SceneCreatureMovementCurrentEligibility.FiniteCurrent&&f.RawBits is uint,"All four actual fields require FiniteCurrent raw words; no fallback.");return f.RawBits!.Value; }
    public SceneWorldSuppliedCurrentSpeedReceipt? Read(long requestedGeneration,CancellationToken cancellationToken=default)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(closed,this);cancellationToken.ThrowIfCancellationRequested();Reserve(limits);
            var view=reader!.Read(requestedGeneration);cancellationToken.ThrowIfCancellationRequested();
            if(view is null||!view.IsOperationalContext||!reader.IsCurrent(view))return null;
            var runBase=view.Get(7);var walkBase=view.Get(11);var first=view.Get(4);var second=view.Get(5);
            uint rb=Bits(runBase),wb=Bits(walkBase),fb=Bits(first),sb=Bits(second);
            var perReader=new CreatureSuppliedCurrentSpeedLimits(limits.MaximumReaderCopyBytes,limits.MaximumReaderWork);
            var runReserve=CreatureSuppliedCurrentSpeedReader.GetRequiredReservations(rb,fb,sb,policy,precision,perReader,cancellationToken);
            var walkReserve=CreatureSuppliedCurrentSpeedReader.GetRequiredReservations(wb,fb,sb,policy,precision,perReader,cancellationToken);
            Need(runReserve.Work+walkReserve.Work<=WorkBound&&runReserve.CopyBytes+walkReserve.CopyBytes<=CopyBytes,"Frozen paired reader reservation invariant.");
            var run=CreatureSuppliedCurrentSpeedReader.Evaluate(rb,fb,sb,policy,precision,perReader,cancellationToken);
            var walk=CreatureSuppliedCurrentSpeedReader.Evaluate(wb,fb,sb,policy,precision,perReader,cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();if(!reader.IsCurrent(view))return null;
            var result=new SceneWorldSuppliedCurrentSpeedReceipt(identity,view,runBase,walkBase,first,second,run,walk);
            cancellationToken.ThrowIfCancellationRequested();return result;
        }finally{busy=false;}
    }
    public bool IsCurrent(SceneWorldSuppliedCurrentSpeedReceipt receipt)
    {
        Enter();try
        { ArgumentNullException.ThrowIfNull(receipt);return !closed&&ReferenceEquals(receipt.Owner,identity)&&reader!.IsCurrent(receipt.View); }
        finally{busy=false;}
    }
    public void Dispose()
    { Enter();try { if(closed)return;reader=null;closed=true; }finally{busy=false;} }
}
