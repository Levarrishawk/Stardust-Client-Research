# Optional selected-player ingress policy

## Bounded question and API

Can the selected-player bridge accept an authored owner baseline/lifecycle set while explicitly deferring nearby and unsupported objects, without allocating an unbounded neighbor registry or misreporting baseline completeness? [ReplacementSelectedPlayerIngress](../poc/Stardust.Core/Session/ReplacementSelectedPlayerIngress.cs) implements this modern optional policy, not a recovered global dispatch table.

Namespace `Stardust.Core.Session`:

```csharp
var ingress = new ReplacementSelectedPlayerIngress();
ingress.Bind(long generation, ulong selectedObjectId);
bool admit = ingress.Route(long generation, ReadOnlySpan<byte> application);
bool observed = ingress.RequiredBaselinesReceived;
int deferred = ingress.DeferredApplications;
ingress.Dispose();
```

Bind succeeds once for a positive generation and nonzero ID. Identity/generation cannot be changed afterward. Route and all state observations require the creator thread without reentry. Route errors retire admission; disposal is idempotent. RequiredBaselinesReceived becomes false on failure/disposal. Only primitive identity, lifecycle flags, a three-bit baseline mask and a checked deferred counter are retained. No raw message, neighbor object map, world, session, or asset reference is retained.

The [bridge](REPLACEMENT_SCENE_BRIDGE_CONTRACT.md) adds an optional final constructor parameter `ReplacementSelectedPlayerIngress? selectedIngress = null`. It borrows and binds the policy at BindSelection, routes it before existing WorldMessage classification, copies false results to caller-owned unhandled output, and clears received applications. It never disposes the policy. Existing default routing remains unchanged. A policy error triggers the bridge's documented terminal session/world close boundary.

## Classification and lifecycle rules

Known create/destroy/containment/end messages are decoded with existing exact lifecycle codecs. Baseline/delta messages are decoded as exact replication envelopes, including operand, opcode, declared payload length and body count boundary, before identity classification. A malformed unrelated envelope/lifecycle also fails; a valid envelope does not establish typed field semantics.

| Input | Selected ID | Unrelated ID |
| --- | --- | --- |
| Create | One create only; must precede selected replication | Explicit false/deferred; no catalog lookup |
| CREO3/4/6 baseline | One baseline per view; record observed bit, pass to world | Explicit false/deferred |
| Other baseline family/view, including CREO1 | Well-framed explicit false after selected create; never marks a required bit | Explicit false/deferred |
| Delta | Selected create and finalization required; supported CREO3/4/6 pass, unsupported family/view defer | Explicit false/deferred |
| End-baselines | Once, only after all3/4/6 bits | Explicit false/deferred |
| Containment | Selected create required, then pass | Explicit false/deferred |
| Destroy | Selected create required; retire selected lifecycle | Explicit false/deferred |

After selected destroy, subsequent identity-bearing selected lifecycle/replication/start traffic rejects. Unsupported packets do not fabricate a typed model. CmdStartScene and ready retain the existing bridge/world path with exact codec checks; nonworld messages retain existing explicit unhandled routing. DeferredApplications counts false Route results, not every nonworld notice that the bridge itself returns. It is a cumulative exact primitive counter, not a neighbor registry, and rejects integer overflow. Existing bridge batch/message limits remain in force.

RequiredBaselinesReceived reflects observed supported baseline envelopes only. It does not mean full decoded CREO3/4/6 state, scene readiness, complete server avatar, CREO1, or PLAY semantics. Existing world policies perform exact typed full4/full6 decoding. A required envelope may be admitted while staging and later fail its typed decoder at the minimum barrier. The policy does not inspect application-specific full baseline fields, infer identities from catalog proximity, or admit nearby actors to a one-player presentation scope.

## Independent component evidence

Input accepted84 snapshot4789 public files was synchronized with prior seals/HEAD/index/private preserved. Orientation included frozen START_HERE/latest84, current terminology/ledger, existing bridge/session/lifecycle/replication contracts and accepted full4/full6 fixture authors. No new executable VA, original execution, live I/O, GPU or reverse-engineering probe is involved.

[Fixture author](../poc/round85-tests/Runtime/fixtures.py) reuses accepted84/83 login/zone authors and independent14 SOE framing, full4 fields from accepted38 and full6 fields from accepted32. It independently packs selected/nearby/PLAY envelopes and expected primitive values. CREO1/PLAY and nearby field bodies are explicitly opaque structural test inputs, not complete decoded server baselines. [Program](../poc/round85-tests/Runtime/Program.cs) drives the actual session, optional policy and world with full4/full6 policies; [verifier](../poc/round85-tests/Runtime/verify.py) compares observed models, defer counts and the exact protected ready wire against authored expectations.

Confirmed on 2026-10-06:52 checks/32 rejection cases; warning-free Core and standalone build. The positive delayed-resource path stages selected create/3/4/6/end, returns12 policy-deferred packets plus one nonworld notice, then inspects full4 ListenTo123 and encumbrances1/2/3, and full6 Passive118. One exact ready remains sequence2. No resource loader or visual actor is used. Required views alone do not imply completeness: a valid-size full4 envelope with a missing typed body records its bit, but world barrier replay fails, closes session/world and emits zero ready.

Negatives cover missing each required view, pre-create baselines, duplicate create/baseline/end, pre-finalization deltas, post-destroy traffic, malformed unrelated lifecycle/envelopes, CREO1 malformed envelope, unsupported families, stale/unbound generation, repeated/invalid Bind, foreign thread, disposal and world typed failure. Input mutation after Route cannot change policy state. Borrowed policy remains undisposed after normal bridge disposal.

The first test-world construction failed because default full codec payload budgets exceeded the world's64KiB cap. A subsequent build was blocked by that failed process's locked output and was not accepted as proof; both owned test sessions were interrupted. Final tests use explicit1024-byte full4/full6 codec budgets and a fresh private artifact directory. Failed logs remain private.

## Reproduction and remaining gates

```powershell
python poc/round85-tests/Runtime/fixtures.py --out <fresh-private-fixture.json>
dotnet build poc/round85-tests/Runtime/Runtime.csproj --artifacts-path <private-artifacts> --configfile poc/round85-tests/Runtime/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet <private-artifacts>/bin/Runtime/debug/Runtime.dll <private-fixture.json>
python poc/round85-tests/Runtime/verify.py --fixtures <private-fixture.json> --result <private-test-output.log> --out <fresh-private-receipt.json>
```

Use existing exact offline packages and private APPDATA/NuGet.Config/DOTNET_CLI_HOME; set DOTNET_GENERATE_ASPNET_CERTIFICATE=false. Fixture author emits its own file and accepted84/83 base files under fresh ignored output paths. Capture Program stdout for the verifier. The standalone project references actual Core and imports no unpublished sibling code.

Confidence Confirmed for these authored byte/state/ownership tests. Native original-resource full server-shaped join belongs to lead. Complete CREO1/PLAY semantics, arbitrary nearby publication/avatar variants, live interoperability, movement authority and server-observed disconnect remain unproved. Exact next action: lead joins reviewed server-shaped fixtures with explicit full4/full6 world policies and this optional ingress, verifies missing/malformed variants fail without publication, and independently checks the selected original actor and owned deferred outputs.
