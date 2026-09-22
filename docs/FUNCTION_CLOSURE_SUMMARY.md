# Stardust.exe function-closure summary

Updated: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## What the historical ledger means

`UNKNOWN_FUNCTIONS.md` began as a place to assign stable names to stripped
addresses. Its filename now misdescribes its contents: a row remains after its
behavior is solved so later sessions can refer to the same address. Growth is
therefore normally evidence accumulation, not growth of uncertainty.

Current mechanical audit of the authoritative ledgers:

| Metric | Count |
|---|---:|
| address-led Markdown rows | 1,112 |
| distinct Markdown addresses | 1,081 |
| duplicated historical Markdown addresses | 28 |
| normalized machine-readable addresses | 1,611 |
| duplicate machine-readable addresses | 0 |

The Markdown ledger deliberately preserves some successive observations for audit
history. The machine-readable `provisional_names.csv` was normalized on
2026-09-21 so each address has one current row; the newest evidence wins. Neither
ledger is a denominator for whole-client completion.

Reproduce this snapshot with:
`audit_provisional_function_ledger.py UNKNOWN_FUNCTIONS.md`.

## Completeness standard

A clean modern client does not need a clone of every compiler-emitted helper.
It does need every replacement-relevant observable contract:

- every accepted/emitted network message and state transition used by the
  Stardust server;
- every asset format/version and content-driven rule reachable from the
  installed client data;
- lifecycle, ownership, ordering, threading and failure behavior at subsystem
  boundaries;
- enough rendering, animation, input, audio and UI semantics to reproduce
  observable behavior without preserving D3D9/Miles/MSVC implementation
  internals.

The 243,277-object raw Ghidra function inventory contains thunks, CRT/STL and
middleware internals, exception islands, duplicate templates, tiny accessors,
false boundaries and backend machinery. Treating it as a manual naming quota
would conflate binary cloning with behavioral reconstruction.

## Current material queue

The authoritative short queue is `FUNCTION_UNCERTAINTY_AUDIT.md`. At this
snapshot it contains:

1. UI/gameplay long-tail breadth: remaining mediators and optional commands,
   to be closed by workflow rather than by indiscriminate function counting.

ENVM TLOK/SKYB/DIST is closed for target-executable compatibility: schemas are
preserved, while executable-wide tag-immediate analysis proves Stardust.exe has
no active recognizer/renderer for them. A future opt-in enhancement is product
research, not an unresolved Stardust.exe function contract.

The latest ClientDataFile blocks close application order, object attachment,
wearables, clear-flora exclusion circles, animated ship attachments, contrails,
turrets/flags, ship damage/destruction records, point/flicker lights, weapon-barrel
recoil dynamics, and continuous engine-sound interpolation. Remaining uncertainty
is concentrated in exact historical labels and a small number of specialized
presentation scalars, not in the fundamental CDF dispatcher or ownership model.

Any newly discovered material gap must be added to that short queue with a
specific compatibility consequence. Resolved provisional addresses stay in
the historical ledger but must not be reported as unresolved.

## Audit rule for future sessions

For each block:

1. state the concrete wire/asset/lifecycle boundary under analysis;
2. assign stable provisional names to relevant stripped addresses;
3. record inputs, outputs, state mutation, ordering and evidence confidence;
4. close or narrow an existing material item;
5. add a new material item only when evidence exposes a distinct compatibility
   requirement;
6. report address-led coverage separately from overall replacement readiness.
