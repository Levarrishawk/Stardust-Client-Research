# Offline zone scene transition contract

R2-NET-01, October 1, 2026. Engine-neutral, single serialized owner; no connection,
scene construction, replication interpretation, payload application or callback.

## Recovered boundary versus modern policy

[Login/zone specification](LOGIN_AND_ZONE_STATE_MACHINE.md), lines 120–146,
records selected-object checking in the automation path and the direct client
asynchronous scene-start/deferred acknowledgement/later server-ready boundary.
[Packet map](PACKET_HANDLER_MAP.md), lines 24–26, records client readiness
predicates, one-shot acknowledgement and header-only constructor. Baseline
Stardust.exe SHA-256 was checked:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Direct evidence names the sender `00462790`, constructor `00AC99F0`, and later
inbound branch `0051C206`. Their roles are **Confirmed**; the identities of all
notified scene subsystems remain unresolved.

Static server correlation: sibling Stardust-3
`MMOCoreORB/src/server/zone/packets/zone/CmdSceneReady.h`, constructor lines 12–16,
writes U16=1/U32=43FD1C22. The callback has no body fields. The unused helper
static parse method is not evidence of an application payload.

The implemented header is exactly `01 00 22 1C FD 43`: little-endian U16 operand
count and U32 opcode, no payload. Wrong length (including truncation/trailing),
count or opcode throws `InvalidDataException`. Encode returns fresh owned bytes.
The same exact header applies to outgoing readiness and inbound server echo.
The independent Python struct oracle compares an actual C# encoded export.
Fixtures are hand-authored synthetic data.

**Modern policy:** generation routing, queue budgets, explicit replacement,
rejection, recovery and ownership rules below are replacement choices, not claims
about the original scheduler, terrain barrier or replication processing. The
old abbreviated sequence diagram is not used for timing: initial object stream
may arrive before outgoing ready, and post-ready traffic follows server echo.

## API and transitions

`ZoneSceneTransition(maximumCount, maximumBytes)` requires nonnegative limits.
State initially `Idle`, generation 0. Mutators require one serialized caller;
there is no internal locking or asynchronous task ownership.

| API | Successful effect | Rejection / stale behavior |
|---|---|---|
| BeginSelection(objectId) | Increments generation, drops coordinator-owned prior state/queue, selects U64, enters Selecting; returns token | Closed throws InvalidOperationException; checked generation overflow leaves state unchanged |
| AcceptStart(token, DTO) | Matching selected object enters Initializing, retains immutable R1 DTO | Stale false; current wrong state throws; wrong ID or invalid DTO throws InvalidDataException without mutation |
| Stage(token, bytes) | Copies opaque bytes and appends in order while Initializing | Stale false; current wrong state throws; count/byte budget excess throws InvalidDataException atomically |
| CompleteMinimumResources(token) | Current Initializing barrier publishes one result with owned ready header and staged messages; clears queue and enters AwaitingServerReady | Stale or any other state returns null, so repeated completion cannot publish twice |
| AcceptServerReady(token, header) | Valid current AwaitingServerReady enters Ready | Stale false without parsing; malformed current header throws; valid duplicate in Ready false; premature/failed/closed/idle throws InvalidOperationException |
| Fail(token) | Active current transition clears queue/scene/selection, enters Failed | Stale, Idle, Failed or Closed false; no ready publication |
| Reset() | Increments generation, clears everything coordinator-owned, enters Idle | Closed throws |
| Close() | Increments generation, clears owned state, enters terminal Closed | Repeated Close is idempotent |

Replacement is always explicit `BeginSelection`, including replacement during
initialization or awaiting echo. A second CmdStartScene in one Selecting-derived
transition is rejected; it does not silently start another scene. Recovery from
Failed requires BeginSelection or Reset. Ready can be failed/reset/replaced;
previously transferred payloads cannot be recalled.

Selected-object comparison is separate from R1 byte decoding. Manually authored
DTOs are additionally checked for non-null ASCII16 strings within U16 lengths
and finite X/Z/Y. Unknown flag values, raw paths, high-bit IDs/time, zero ID and
ASCII controls are preserved. No account/galaxy ownership is inferred.

## Ownership, budgets and stale work

Stage accepts zero-byte payloads, which still consume one count. Byte usage
uses subtraction-based bounds, so addition cannot overflow the configured int
budget. Budget checks precede copying and mutation. Rejection preserves every
already staged message and the Initializing state. It neither applies nor
inspects opaque payload contents. Staging before scene start or after publication
is rejected; the caller owns traffic in later states and must define its next
boundary separately.

Successful completion allocates the result before changing state. The queue's
owned byte arrays move into a `ZoneScenePublication` holding generation and
private data. `CopyReadyMessage()` and `CopyStagedPayloads()` return defensive
copies; mutating copies cannot change subsequent reads. The coordinator returns
one publication per generation, even though its owner may inspect/copy it many
times. Reset/replacement/failure/close release coordinator references but do not
alter a publication already owned by the caller. No external resource, task,
consumer or callback is retained; no stale task cancellation is claimed.

Callbacks must carry the token captured when their work started. Stale completion
cannot expose the old queue or an old outgoing acknowledgement. Inbound headers
contain **no generation ID**: the caller must supply a token from its own
connection/dispatch context. A delayed old echo incorrectly tagged as current is
indistinguishable on these six bytes. This coordinator cannot solve that wire
ambiguity; a later transport policy must handle it. Consumers must similarly
check publication.Generation before using a result after a later replacement.

## Verification and integration

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R2_ZONE_SCENE_TRANSITION.md)
records inputs/commands/results. Scenarios cover positive ordered delivery,
stale work, wrong object, early/duplicate/malformed ready, count/byte boundaries,
atomic rejection, owned input/output, reset/replacement/failure/close and zero
budgets. Proposed shared runner registration: `ZoneSceneTransitionScenarios.Run()`.
No existing shared runner, R1 codec, login state or transport file is edited.

Next bounded probe: offline transport transcript that demonstrates correct token
routing and publication consumption across replacement. No live contact implied.

## Round3 integration follow-up

A composed reliable-transport proof now exercises this coordinator in [the offline transcript contract](OFFLINE_ZONE_SCENE_TRANSCRIPT_CONTRACT.md). Its staged create/baseline bytes are synthetic; actual initialization and replication parsing remain open.
