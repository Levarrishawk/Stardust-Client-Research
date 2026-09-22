# Evening Multi-Block Analysis Session

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
Canonical workspace: `D:\Development\Experimental-Client`

## Outcome

This session completed seventeen coherent closures without starting replacement
client implementation. It concentrated on asset-runtime semantics, asynchronous
ownership, server/client schema contradictions, camera profiles, and a complete
inventory of original input assets.

## Completed blocks

1. **Ship destruction DSEF fields** — split-delay seed, effect reference lengths,
   spatial audio coupling and spawned-object template/appearance/hardpoint roles.
2. **Contrail override keys** — child-first override-set behavior and corrected
   the former hardpoint-remap interpretation.
3. **Static flora RNG** — per-cell shuffled Park–Miller seed/sample order and a
   corrected transform-yaw call classification.
4. **CDF extension policy** — PALV/OBJT/VLEF are tolerated/skipped unsupported
   extensions for this executable, not hidden active behavior.
5. **File-streamer ownership** — asynchronous worker with a caller-synchronous
   enqueue/wait public read and bounded borrowed storage.
6. **Async-loader lifecycle** — callback/context cancellation, main-thread
   publication, sentinel drain, join and cleanup ordering.
7. **Procedural-terrain lifecycle** — dirty envelope, exclusive generation,
   worker publication, main-thread install/retire and stop/join teardown.
8. **STAG coordinate generation** — effective enum values 0/3/4/5 closed as
   ordinary, reflection, scroll profile 1 and scroll profile 2.
9. **Current server schema audit** — active quest counter/timer and player-name
   response mismatches separated from dormant hazards.
10. **FreeChase profile semantics** — FRST/DEFH/PSHM/PSTM exact values,
    precedence and appearance-relative application.
11. **Space-camera ownership** — CPIT grammar/cockpit frame lifecycle and turret
    station/mount/zoom/collision behavior.
12. **CPIT corpus** — all 36 effective profiles validated; missing/tombstoned
    cockpit geometry proven to be normal fallback state.
13. **Input command/action corpus** — all 436 commands, 1,744 slots, 525
    populated actions and 120 numeric interpreter IDs inventoried.
14. **Input physical-binding corpus** — all 14 effective maps and 1,939 bindings
    inventoried; 24 stale references and two duplicate tuples made explicit.
15. **Input device-mask policy** — corrected the command field to device-family
    bits 1/2/4/8 and separated it from MAP INFO shift state.
16. **Input MAP runtime policy** — closed state-zero inheritance, exact-state
    selection and serialized last-wins duplicate precedence.
17. **Input action transitions** — closed the four fixed slots as press,
    release, held/repeat, and forced reset/cancel.

The scene-transition chain was also re-audited and found already closed from
start-scene through baselines/end-baselines, scene-ready echo and in-world
state; no duplicate checkpoint was created.

## High-value findings

- Original input assets are directly consumable through a bounded importer.
  Their inconsistencies are finite and reproducible, not a reason to replace
  the format.
- Numeric input action IDs are operation selectors. ID 324 carries 311 distinct
  command strings, so a one-ID/one-command enum would lose behavior.
- The current Stardust source still contains two active byte-compatibility
  risks despite packet inventory closure: quest counter/timer IDs and the
  malformed VerifyPlayerName Unicode response.
- Cockpit geometry absence is supported by the deployed asset overlay; camera
  operation must not depend on a live cockpit appearance.
- Both asynchronous loader families now have explicit cancellation/publication
  and shutdown contracts suitable for safe x64 redesign.

## Evidence produced

- Seventeen dated checkpoint documents.
- `SERVER_CLIENT_COMPATIBILITY_RISKS.md`.
- `cpit_corpus_inventory.csv`.
- `input_command_actions.csv`.
- `input_numeric_action_map.csv`.
- `input_physical_bindings.csv`.
- Reproducible corpus extractors and focused Ghidra decompilation evidence.

## Coverage and ledger state

- Protocol inventory: **345 documented / 0 reachable gaps**.
- Stable provisional-name CSV: **1,611 unique addresses**, with superseded
  aliases removed from the machine-readable file and retained in the
  chronological evidence ledger.
- Chronological function ledger: **1,112 address rows / 1,081 distinct primary
  addresses / zero rows whose status is merely Unknown**. Historical duplicate
  rows are retained as superseded evidence and are not name collisions.
- Comprehensive replacement-relevant coverage: conservatively raised from
  **82% to 83%**. The large number of closures is real, but most are bounded
  residuals; a one-point increase avoids overstating breadth.
- Engine/POC decision readiness remains **93%**; the user has elected to finish
  client analysis before returning to Godot/build work.

## Resume point

Next work should continue with one of the remaining high-value breadth areas:

1. long-tail UI/gameplay workflows and local semantic-action consumers;
2. rare active asset/appearance/particle formats not yet fixture-complete;
3. remaining lifecycle/ownership review outside the now-closed loader and
   terrain worker paths;
4. exact MAP flag and duplicate-binding precedence (bounded, low risk).

No build, GitHub publication, shutdown, DRM bypass, or dynamic authentication
interference was performed.
