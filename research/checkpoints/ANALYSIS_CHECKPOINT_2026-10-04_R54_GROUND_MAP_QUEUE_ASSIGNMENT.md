# Round 54: one GroundScene map/controller queue assignment

Date: 2026-10-04. Question: where does one selected GroundScene acquire non-null map `+4C`, and does that exact map's queue `+0C` receive controller queue `+4`? Stop after one lifecycle path or a fresh 20-candidate limit.

## Inputs and tools

Executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; static PE mapping, Capstone instruction checks, RTTI/vtable pointers, direct calls, and register/data-flow evidence. No original execution or live contact.

Frozen54: 3,691 files; manifest SHA-256 `08df3c05dd838854b14d34c235d22ce2be0b566e34fa91f8366a07abe70179dc`; ZIP SHA-256 `66bc9a7d2ce0c88bf7c89a28291bfafba804cadb6bbbce273104d6cc835b157f`; main source HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`. Worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`. Actual own53 final handoff hashes were crosschecked with the lead's reviewed four changes before overlay. All members, archive identity, names/case/hash/size, lexical root/ancestors/link/reparse state, containment, and destination hashes were preflighted before writes; all 3,691 postflight hashes and HEAD/index/unrelated files were preserved.

## Findings and confidence

Canonical observations and consequences are in [the durable contract](../../docs/GROUND_SCENE_INPUT_MAP_QUEUE_ASSIGNMENT_CONTRACT.md). **Confirmed:** constructor `00519C40 -> 00518EB0`, successful player controller construction/installation at `00519238 -> 00B22BB0`, scene object store `00519259`, selected binder call `00519616 -> 0051FAD0`, acquisition/store `0051FAF4/0051FAFC`, and queue assignment `0051FB0A` establish:

`scene[0x4C][0x0C] = scene[0x1C][0x2C][0x04]`

The called helper `00B3D340` has exactly the same body as getter `0062A9B0`. This closes selected pointer-expression identity. It is not evidence that every later rebind has the same dynamic controller type, that any physical binding is active, or that a runtime occurrence was observed.

**Confirmed lifecycle structure:** existing map queue disconnect precedes release; successful acquisition returns shared global `0191D214` with non-null reference increment; release decrements global count and calls cleanup helpers on the global at zero (destruction/free role Strongly inferred). Binder overwrites the acquired template's initially null queue. **Unknown:** queue allocation/non-nullness, transitive intervening mutations, effective configuration, shared-map concurrency, full shutdown and null/error policy. Binder has no new-map/object/controller null guards; zero-return acquisition branches exist if prior error helpers return. Do not infer a crash or recovery behavior without closing those helper/precondition paths.

## Bounded investigation and corrections

Exactly three fresh candidate direct-call references were considered: `0051FAF4` to acquisition `006E1E70`, plus `00519616` and `00BAA610` to binder `0051FAD0`. Only the constructor/setup path was followed. The receive-message lead's inventory extent was one byte and was not accepted as a full function; no claim depends on it. An exploratory getter window included neighboring methods; final evidence pins only its exact four-byte body. Initial orientation used a wrong daytime-record location and a Windows-invalid wildcard path; discovery identified the sessions path and subsequent reads/searches used explicit paths. These did not become public behavioral claims.

## Reproduction and validation

Verifier (external historical reference) requires explicit `--exe`, `--packages`, and fresh ignored `--receipt`. Independent runner (external historical reference) requires explicit `--exe`, `--packages`, and fresh ignored `--output-dir`. SHA validation precedes VA use. Positive checks cover 60 anchors/nine complete bodies/two RTTI chains, plus independent 14 byte witnesses/seven calls/two RTTI strings/two pointers and separately parsed instance expression. Eighteen rejection checks include wrong map offset, wrong queue field/getter agreement, candidate omission, identity/evidence/output errors, pre-resolution link/reparse rejection, and cap overflow. Fresh nested output succeeds; rejected link/reparse ancestors are not created. No frozen normalized producer audit or native arbitration model is replayed.

Final handoff checks workspace links/organization, UTF-8/LF/Python syntax, offline locked Core/SmokeTests restore/build, whitespace, all 3,691 frozen hashes, unchanged HEAD and empty index, and only four assigned additions. Exact results and hashes are recorded in the ignored final receipt. No unrelated smoke execution, download, canonical registry/status edit, main edit, staging, or publication is claimed.

## Exact resume point

Trace player constructor base call `006385C0` to controller `+4` queue construction/destruction and selected allocation/error preconditions. Keep one effective physical profile/binding occurrence separate. Shared reconciliation belongs to the lead; existing dated53 claims remain historical, with this contract providing the later assignment closure.
