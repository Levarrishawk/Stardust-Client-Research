# Tangible view6 CachedNetworkId state policy

## Result and boundary

The specialized `Archive::AutoDeltaVector<CachedNetworkId>` establishes exact
counter filtering, index/growth behavior, equality no-ops, assignment extent,
conditional callback order and watcher ownership. This is static evidence for
the pinned executable, not a new state implementation or an executed client
trace. [Accepted consume codec](TANGIBLE_VIEW6_CODEC_CONTRACT.md) remains unchanged.

The reproducible [verifier](../../SWG-Godot-Client/poc/round29-tests/Networking/static_audit.py) rejects
an incorrect executable hash before PE/package imports, then verifies109
instruction anchors, vector/Watcher RTTI and vtable identity. Sixteen small
synthetic reference examples illustrate established rules only. Bulk original
disassembly remains private.

Baseline SHA256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Primary vector vtable`015E0614` has snapshot parser`006421A0`, delta parser
`006423E0`, size getter`00642A20`, clear`00641850` and deleting destructor
`00643740`. Tangible field is object`+2F0`; internal local counter is
field`+18`. These offsets describe the x86 ABI, not replacement memory layout.

## Raw unsigned counter and cursor rules

Let `C` be the current local U32 counter, `U` the incoming U32 counter and `N`
the incoming U32 raw loop extent. The delta parser computes:

```text
skip = min((C - U + N) modulo 2^32, N)   // unsigned comparison
cursor = 0
while cursor < skip: consume one whole command, without state mutation
while cursor < N: consume and conditionally apply one whole command
```

Evidence: `00642445` loads field`+18`;`00642448/0064244D` perform subtract/add;
`0064244F/00642454/00642456` clamp using unsigned `jbe`. Skip comparison is
`0064245B–00642464`; applied-loop entry compares at`00642553/00642556`.
There is no signed half-range classification, no unconditional incoming-counter
assignment and no gap resynchronization in this specialization. Local counter
writes come from successful mutations listed below. Arithmetic increments wrap
as U32 instructions; they do not saturate or throw on overflow.

Both loops advance the raw cursor1 per command. Assignment opcode3 additionally
advances once per assigned ID. The skipped assignment increment is`006424B0`,
with outer advance`00642544`; applied assignment increments at`0064297A`, with
outer advance`00642643`. Entire assignment bodies are consumed before either
loop compares its threshold again. Thus a skipped assignment can cross the
skip threshold and remains wholly skipped; a final assignment can overshoot N.
Original raw cursor storage is U32. Replacement work budgets must prevent
unbounded cursor wrap/consume paths; accepted28 bounded grammar uses a wider
scan cursor and rejects before that legacy wrap is reachable.

Consequences are observable in the established rules: duplicate intervals may
skip all commands; a future gap can also clamp skip to N, leaving C unchanged.
An equal set or invalid remove consumes a raw unit while leaving C unchanged.
Therefore the final local counter may differ from U. Do not repair that by
silently writing U. A full snapshot is a distinct seed operation.

## Applied commands0 through4

All indices are unsigned U16. ID equality compares both raw U32 halves
(`00B2FFF8/00B2FFFF`), ignoring cached watcher pointers. Order and duplicates
remain vector data.

| Opcode | Applied state rule | Local counter change | Conditional notifications |
|---:|---|---:|---|
| 0 remove | If index >= size, no-op. Otherwise retain old value temporarily, queue remove record, erase and shift later elements left. | +1 only for valid removal | owner hook, remove(index, old), general |
| 1 add | If size < index, grow to index with default values, then insert at index. Existing elements at/after index shift right. Append when index==size. | +1 | owner hook, add(index, new), general |
| 2 set | If index < size and ID bits equal, no-op. Otherwise grow to index+1 when needed, retain old value, then replace. | +1 only after equality guard | owner hook, set(index, old, new), general |
| 3 assignment | Replace whole ordered vector, even if empty or equal. Queue assignment marker and N replacement records. | +(N+1) | owner hook and general once; no per-index set/remove/add notifications |
| 4 clear | If empty, no-op. Otherwise queue clear, notify removals in ascending original index, destroy entries and make size0. | +1 only when nonempty | remove for each old entry before truncation, then owner hook and general |

No-op and skipped paths queue no mutation record and invoke none of these
notifications. The conditional slots are described below; this table does not
assert that a particular callback is installed on a live TangibleObject.

Remove proof: size getter via vtable`+1C` at`00642599`, unsigned bounds
`0064259C/0064259F`; queued record`006425CF`; counter write`006425DF/006425E7`;
erase`006425F7`; owner/remove/general calls`006425FE/00642610/00642617`.
Erase helper`00643C80` assigns subsequent entries left and destroys the tail.

Add proof: queue`006426F6`, counter`006426FB`; compare size/index at
`0064270C/0064270E`, resize only for size<index at`00642711`, insertion
`00642728`; owner/add/general`0064272F/0064273B/00642742`. Filler growth emits
no independent add notifications or extra counter increments.

Set proof: existing-index test`006427AE/006427B3`; exact ID comparison helper
`00B2FFF0` and equal exit`006427CB/006427D2`; counter`00642813`; grow to index+1
at`00642828/0064282C`; old copy`0064283D`, replacement`00642850`;
owner/set/general`00642857/0064286A/00642871`. A beyond-end set of raw ID0
still changes state/counter even though newly constructed default values are0,
because equality is checked only against an existing element.

Assignment proof: parser builds a temporary vector of all N IDs then calls
`00641D20` at`006429AB`. Helper queues opcode3/length at`00641D6E`, increments
C at`00641DB0`, replaces vector storage (copy/reuse/reallocation paths), queues
one opcode2/index/value record per new entry and increments C per entry at
`00641F53`, then invokes owner/general at`00641F83/00641F9B`. Equal contents are
not an equality guard. The helper's self-source check`00641DB8` still follows
the initial counter increment; parser assignment uses a separate temporary
vector, so that source-alias branch is not the network assignment case.

Clear proof: empty exit`00641871/00641874`; counter`006418D6`; conditional
per-entry remove call`006418FF` occurs while old entries remain; destructor loop
`00641942`; end pointer publication`00641951`; owner/general
`0064195E/00641973`. Preserve the logical event ordering in any later receipt
specification, while explicitly distinguishing modern callback deferral.

## Default filler and CachedNetworkId ownership

Resize helper`00643CD0` first constructs a default CachedNetworkId at
`00643D06`. The default constructor`00B2FCC0` copies the eight-byte global
identity at`01946EE8` via`00AA4920`, initializes watcher pointer`+0C` to0 at
`00B2FCE5`, and installs `Watcher<Object>` vtable`015DC648`. The identity lies
in the zero-initialized virtual tail of `.data`; initializer`00B2FC70` pushes
0/0 and calls the two-word constructor`00AA4900` on that same global. Default
raw ID bits are therefore **0000000000000000**, not all-ones or uninitialized
memory. This establishes bits, not an additional gameplay meaning for zero.

Growth computes target minus old size at`00643D92`. Its spare-capacity path
copies the default temporary (`00643DB8`), constructs the requested number of
new values (`00643E88` ->`00645800` ->`005E81B0` ->`00B2FE70`), then publishes
end pointers. Reallocation at`00643F13` ->`005E5800` uses the same copy
constructor for existing/default values before destroying old storage. Counter
or callbacks are not inside this resize helper; the parent add/set owns them.

CachedNetworkId is a raw8-byte identity plus an object watcher in this legacy
ABI. It is not an owning reference to the target object:

- Copy constructor`00B2FE70` copies ID bits and cached pointer, registering the
  new watcher with target`+84` at`00B2FEC1` when non-null.
- Assignment`00B2FEE0` copies ID bits, compares old/new cached pointers,
  unregisters old via`00AA37C0` at`00B2FF11`, installs new and registers via
  `00AA36D0` at`00B2FF24`. It does not delete the target object.
- `00AA36D0` appends watcher pointers;`00AA37C0` removes one from that list.
  RTTI independently identifies `Watcher<Object>`; these are observer-list
  operations, not inferred strong reference counts.
- Destructor`00B300E0` unregisters the watcher at`00B30127` and clears its
  pointer at`00B30134`. Raw NetworkId base destruction is trivial.

Remove/set keep old CachedNetworkId temporaries alive through item/general
notifications and release afterward. Assignment's parsed temporary vector is
released after replacement (`006429BA`, entry destruction`006429D4`, storage
release`006429EA`). Replacement reuse calls cached-ID assignment; shrinking
and reallocation destroy discarded entries. Vector destructor`006414C0`,
reached through`00643740`, frees callback-holder allocations, destroys queued
command CachedNetworkIds, frees command storage, then destroys current vector
entries and frees vector storage before its base destructor. Precise anchors
include`006414F4/0064150F`, `00641533/0064156E` and`00641592/006415BC`.

A native replacement should store owned U64 IDs and resolve lifecycle-safe
handles separately. It need not reproduce watcher-memory layout or keep
pointers to temporary callback arguments.

## Snapshot and callback boundaries

Snapshot`006421A0` destroys the old vector before reading replacement headers
(`00642207`), clears pending mutation records through virtual slot`+0C`
(`00642221`), reads incoming counter directly into field`+18`
(`00642261/00642265`), and appends every raw ID in order, preserving duplicates.
It invokes the conditional general slot at`00642333` after the full loop;
there are no per-item add/remove/set calls in this snapshot parser. Unlike
incremental updates, the supplied snapshot counter becomes local C directly.
Malformed-input/allocation failures can occur after old state has been changed;
these instructions do not prove whole-message rollback.

Conditional vector slot`+28` is general, `+2C` remove, `+30` add and`+34` set;
wrappers`00644E70/00644E90/00644EB0/00644ED0` guard both holder and owner before
calling. The common owner hook is`0043E860` ->`0118A7C0` when field owner`+08`
is non-null. That owner/registry notification is distinct from these optional
item/general slots and from higher-level deferred commit handling.

Tangible construction zeros all four optional holders at
`0063EEE7/0063EEEA/0063EEED/0063EEF0`. Thus the original construction does not
by itself establish a gameplay callback recipient. Later callback installation,
specific defender/combat presentation and all downstream effects are outside
this bounded policy. Do not infer them from current server notifications.

## Source correlation and confidence

Current server `MMOCoreORB/src/server/zone/objects/scene/variables/DeltaVector.h`
`set/add/remove/removeAll` updates its source counter and emits opcodes2/1/0/4.
Its source counter arithmetic and container behavior are **server-only**;
they do not override the client no-op/index rules above.
`MMOCoreORB/src/server/zone/objects/tangible/TangibleObjectImplementation.cpp`
`setDefender` emits two replacements after field1, confirming ordered primary
selection as server behavior. Original state closure here is based on the
CachedNetworkId specialization, not the integer wounds implementation or
analogous server container semantics.

Observation: hash-gated instructions establish the counter arithmetic, all
five applied mutations, default bits, conditional call order and ownership
operations. Interpretation: a bounded raw-ID state model can now be specified.
Confidence: **Confirmed** for those local mechanics; **Unknown** for live
packet occurrence, later callback recipients and full-message failure rollback.
Implementation consequence: preserve local no-op/counter/growth behavior under
an explicit bounded policy; keep external reactions outside owned raw state.

## Reproduction and next implementation probe

```powershell
python poc/round29-tests/Networking/static_audit.py C:/Stardust-DEV/Stardust.exe --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages
```

The verifier includes16 labeled synthetic models: contiguous/duplicate/gap/
overlap/wrap intervals, invalid remove, equal set counter drift, zero-filled
add/set growth, clear-empty/nonempty, equal/empty assignment, assignment
overshoot and skipped-assignment threshold crossing, plus zero extent.
These execute independently authored Python models, never the original client.

Next implementation probe: an engine-neutral owned U64 defender state with a
named bounded legacy policy, maximum element/operation/assignment/decoded
budgets, immutable candidate/receipt, modulo-U32 local counter, zero-filled
indexed growth, and exact no-op/assignment/skip rules above. Test each known
case plus intermediate growth followed by clear, global/element cap rollback,
stale incarnation and caller/source ownership. Choose atomic whole-plan
publication and post-publication callback receipts explicitly as modern policy;
original local instructions do not establish that stronger transaction guarantee.
No codec/world/UI changes are authorized or made by this research milestone.
