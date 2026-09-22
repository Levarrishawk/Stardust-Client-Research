# Analysis session report — 2026-09-22 to 13:00

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

This session continued static analysis only. No replacement-client code, server
code, Git publication, shutdown command or client-data mutation was performed.

## Result

Three bounded areas were completed:

| Area | Material result | Confidence |
|---|---|---|
| BTRT destination formats | Exact client `TextureFormat` names, file-ID mapping, s207 D3DFORMAT mapping, capability probe and ordered first-success fallback | Confirmed except historic sentinel-16 spelling |
| Options dialog architecture | Eleven-page parent composition, typed binding families, live previews, entry snapshots, OK/Cancel transaction, Defaults confirmation and final machine-options save | Confirmed; combo role Strong |
| Specialized options controllers | Chat Colors word/sentence/remove flow, Alarm preview/add/remove/mode controls, AFK enable/default behavior | Confirmed, including custom-page Cancel asymmetry |

## BTRT/graphics backend findings

The client dynamically loads `s2%02d_r.dll`; the installed backend is
`s207_r.dll` (SHA-256
`D3A38E096B050F0219826A54908963C04F4ACDEDE03C5A42708C2E0C5F60A53D`).
Its exported `GetApi` returns the graphics API table and installs texture-data
creation at slot `+0x114`.

The 14 client formats are `TF_ARGB_8888`, `TF_ARGB_4444`, `TF_ARGB_1555`,
`TF_XRGB_8888`, `TF_RGB_888`, `TF_RGB_565`, `TF_RGB_555`, `TF_DXT1` through
`TF_DXT5`, `TF_A_8` and `TF_P_8`. BTRT file IDs map
`0..11 -> [6,5,2,1,4,3,0,16,16,16,7,8]`. Sentinel 16 is invalid/unsupported.
The backend walks the ordered candidates, skips unsupported formats and accepts
the first successful 2D/cube texture creation. This closes the replacement
format-negotiation rule without relying on D3D9 internally.

## Options transaction model

The parent `SwgCuiOpt` constructs pages in order AFK, Alarm, Chat, Chat Colors,
Combat, Controls, Graphics, Misc, Sound, Terrain and UI. Scalar controls are
typed bindings containing current-value getter, setter, default getter, bounds,
widget and optional dependent callback.

Control changes call their setter immediately so render/audio/input/UI changes
preview live. On dialog activation, every page captures its current values. OK
keeps the live values; Cancel invokes every binding's restore path. Common
deactivation then writes the typed machine-options store. Defaults is separately
confirmation-gated and dispatches each binding's default getter/setter.

This corrects the page-local intermediate inference that live application meant
there was no Cancel rollback. The parent mediator proves the transaction.

## Replacement implications

- Retain a toolkit-neutral, typed settings registry with getters, setters,
  defaults, validation and change notifications.
- Support live previews plus a dialog-local snapshot/rollback transaction.
- Preserve the original numeric ranges and asset-backed UI palette behavior,
  while adapting obsolete hardware/provider choices behind capability adapters.
- Treat Chat Colors, Alarm and AFK as controllers over collections/services,
  not as scalar schema rows.
- The original `OPTN/0002` importer/exporter remains useful for legacy profile
  migration, but no native x86 structure should cross the x64 boundary.

## Remaining uncertainty and next focus

- Decide whether a modern client should preserve or deliberately improve the
  confirmed legacy asymmetry: scalar pages roll back on Cancel, while Chat
  Colors/Alarm collection edits and AFK custom state do not.
- Continue long-tail optional UI/gameplay controllers where behavior affects
  assets, state or user-visible compatibility.
- Continue the stale-unknown contradiction audit so historical interim language
  is clearly superseded by later closures.
- Preserve the external public TCP gateway/deployment question separately from
  the already mapped direct UDP/SOE developer-server path.

## Progress accounting

- Implementation-grade replacement-relevant research: **96%**.
- Expanded comprehensive whole-client research: **90%**.
- Server packet inventory: **345 documented / 0 reachable gaps**.
- Stable provisional-name registry: **2,151 distinct addresses / 0 duplicate
  address rows**.

The percentages do not advance this session: these blocks materially deepen UI
and texture fidelity but do not justify rounding the broad denominator upward.

## Resume-quality verification

- All **452** top-level private-workspace Markdown files were checked for local
  Markdown references: **0 broken `.md` targets**.
- `server_packet_gap_matrix.csv` mechanically contains **345 rows**, all with
  status `documented`.
- The comprehensive ledger's stale opening `82%/93%` wording was corrected to
  the current `90%/96%`; its old workstream table is now explicitly labeled as
  a historical 2026-09-21 snapshot.
- Seven untracked BTRT decompiler outputs were moved intact from the Git
  repository root into the ignored `.codex-stardust-tools` analysis directory.
  The Git worktree is clean; no research artifact is staged for GitHub.
- Final synchronization uses only `D:\Development\Experimental-Client` and
  verifies staging/canonical SHA-256 parity.
