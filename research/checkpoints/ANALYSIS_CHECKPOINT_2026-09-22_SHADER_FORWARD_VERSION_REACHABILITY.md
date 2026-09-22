# Shader Forward-Version Reachability

Date: 2026-09-22

## Result

The configured effective TRE overlay contains **27 live winning shader/effect
paths** whose payloads use `IMPL/0009` and `PASS/0010`. This exact
`Stardust.exe` does not implement either version: its implementation dispatcher
accepts `0000..0007`, and its pass dispatcher accepts `0000..0009` then routes
all other tags to the unsupported-version diagnostic path.

This is an asset-compatibility risk for a replacement, not evidence that the
new client should reproduce the rejection. Original-asset compatibility requires
a forward-version decoder or a verified content-normalization path.

## Effective overlay facts

- 27 affected winning paths exist: three `.eft` records and 24 `.sht` records.
- Three originate in `mtg_patch_011_files_01.tre`; 24 originate in
  `mtg_patch_018.tre`.
- `IMPL/0009` DATA is seven bytes and retains the count/phase/flag envelope
  already established for older implementations.
- `PASS/0010` DATA is 57 bytes versus 56 bytes for `PASS/0009`.
- The additional PASS byte is in the scalar prefix. Its semantic label remains
  unknown and must be preserved explicitly, not guessed.
- No affected winner has a lower-priority same-name version suitable for direct
  semantic differencing.

## Effective reference reachability

A complete scan resolved 203,617 configured winner records and searched their
decompressed payloads. Eight of the 24 affected shader paths have live incoming
references from 39 distinct effective assets (40 target/referrer rows including
the Mustafar environment path):

| Shader | Distinct external referrers | Observed role |
|---|---:|---|
| `shader/pt_artillerystrike_splashring_no_z.sht` | 1 | particle effect |
| `shader/pt_burst.sht` | 6 | profession/TCG particle effects |
| `shader/pt_circle_shimmer.sht` | 1 | particle effect |
| `shader/pt_explosion_glow_burst_no_z.sht` | 21 | combat, profession, TCG and world particle effects |
| `shader/pt_explosion_lg02_no_z.sht` | 1 | Restuss particle effect |
| `shader/pt_mustafar_distant_volcano_01.sht` | 4 | three particle appearances plus `terrain/environment/mustafar.iff` |
| `shader/pt_proton_grenade_ring_no_z.sht` | 5 | particle effects |
| `shader/pt_red_rays_no_z.sht` | 1 | particle effect |

The other 19 affected paths had no literal incoming reference in effective
winner payloads. That does not prove runtime impossibility: some may be selected
by code, UI or dynamically composed names. It does bound the directly authored
asset graph.

## Executable evidence

`shader_pass_version_dispatch_VA_0075E840` enters `FORM PASS`, switches on the
child version, calls exact loaders through `0009`, and invokes the version-error
formatter for every other value. `shader_pass_load_v0008_VA_007613C0` and
`shader_pass_load_v0009_VA_007619D0` prove that `0009` added five prefix fields
relative to `0008`; there is no subsequent `0010` loader or switch case.

`shader_implementation_version_dispatch_VA_0075CD00` independently ends at
`IMPL/0007`. Therefore an affected asset is rejected at its implementation
envelope before its nested `PASS/0010` can be materialized.

## Replacement contract

- Preserve the overlay winners and decode `IMPL/0009` and `PASS/0010` as
  explicit versioned records.
- Retain the extra PASS byte in renderer-neutral IR until its meaning is closed.
- Do not silently reinterpret `PASS/0010` as `0009`; that would shift every
  subsequent field by one byte.
- Build fixtures from all 27 winners and require lossless parse/re-emit or an
  equivalently strict field-layout test before rendering them.
- Prioritize visual validation of the eight directly referenced shader paths,
  especially Mustafar environment/volcano content and the heavily reused
  explosion-glow shader.
- If a temporary compatibility mode is needed, diagnose unsupported behavior
  per asset and substitute an obvious error material; do not crash or silently
  render with shifted state.

## Confidence

- Effective winner count, archive ownership, payload widths and reference rows:
  **Confirmed by corpus parsing**.
- Exact executable version limits and rejection path: **Confirmed by code**.
- Eight directly authored active-reference families: **Confirmed by effective
  decompressed asset graph**.
- Runtime encounter frequency in ordinary gameplay: **Unknown** without
  instrumentation or complete gameplay-content root traversal.
- Exact meaning of the added PASS byte: **Unknown / material Class C item**.

## Evidence

- `shader_overlay_winners.csv`
- `shader_version_payloads.csv`
- `unsupported_shader_effective_references.csv`
- `scan_effective_asset_references.py`
- `0x0075CD00`, `0x0075E840`, `0x007613C0`, `0x007619D0`
