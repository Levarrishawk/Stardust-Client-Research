# Combat playback-script variable consumption (2026-09-23, block 2 of 3)

## End-to-end confirmed example: `fire_single`

The source/asset/executable chain now has a concrete variable-level bridge:

```text
server CombatAction animation CRC for "fire_single"
  -> client CBTM/0002 exact-key selector
  -> fire_single DDSP/DEPS leaf
  -> SNGL playback/fire_projectiles.pst with STRN TCAA="fire_single"
  -> typed string filler writes TCAA on playback instance
  -> PBSC/0002 PSKA/0002 INFO names variable tag TCAA
  -> PSKA runtime string-variable lookup -> skeletal animation path
```

The `fire_projectiles.pst` effective entry comes from priority-40 `mtg_patch_011_files_01.tre` and is 256 bytes. Its `FORM VAR ` declares string tags `TCAA`, `CAHD`, `CAMD` (all empty by default), float `LEVP`, and integers `GMAD`, `SPEA`, `SPED`, `LRTP`. It has two threads: one `PSKA/0002` whose seven-byte `INFO` names `TCAA`, and one `FISU/0002`. The parser and runtime for PSKA's tag-to-string-to-skeletal-animation path were established in `ANALYSIS_CHECKPOINT_2026-09-22_PSKA_FORWARD_VERSION_AND_RUNTIME.md`; this block supplies the matching manager-side binding. **Confirmed by combined asset bytes and executable code.** The `FISU` consumer of `CAHD`/`CAMD`, if any, is not claimed.

## Second example: reaction variants

`playback/fire_projectiles_reflect.pst` is a 640-byte effective script with five action tags across four threads. Its `FORM VAR ` declares `TCAA`, `CAHD`, `CAMD`, `CARA` plus other typed values. Two `PSKA/0002` actions explicitly name `CAMD` and `CARA`, while another names `TCAA`. The read-only `RAND` fixture under `fire_defender_posture_change_up` supplies `CAMD=block_high_center`, `block_mid_left`, or `block_mid_right` to this script. This establishes a file-driven branch-to-animation-name channel, without needing to infer the exact probability of a live branch or the side effects of the other action tags.

## Force/lightning script as an asset-dependency example

`playback/use_lightning_appearance.pst` is a 494-byte effective script used by 26 combat-manager keys. Its `FORM VAR ` includes `TCAA`/`TCAD`, appearance-like path `NTPA=appearance/force_lightning.ltn`, sound-like path tags `BDNS`/`LDNS`/`EDNS`, hardpoint-like `TPHE`/`2PHE`, float tags such as `LTTA`/`CRFG`/`CRFD`, and integer `LVEL`. A `PSKA/0002` action explicitly requests `TCAD` and another requests `TCAA`; another action is `ULAT/0002` and its 52-byte `INFO` contains the variable tags `NTPA`, `TPHE`, `2PHE`, `LVEL`, `LTTA`, `CRFG`, `CRFD`, `BDNS`, `LDNS`, `EDNS`, `SUDR`, `ETAR`. This **confirms encoded dependencies on these typed variable names**. Exact `ULAT` runtime semantics, resource lifetime, effect placement and audio timing remain to be decompiled; the role labels are inferred from asset paths and tags.

The effective combat manager references **24 distinct `.pst` paths**. The previous overlay audit found the four problematic `PSKA/0003`/`FIPL`/`PPRL` scripts have **no manager reference**; none of those four appears in this 24-path set. That is a scope distinction, not a claim they can never be reached through another mechanism.

## Replacement-client rule

Preserve the original `CBTM -> PBSC` data link. A selector's typed `VARS` payload must overwrite or supplement script-template defaults before action execution. Playback actions should look up values by original four-byte tag and type. `PSKA` demonstrates why using only the manager key or only the `.pst` filename is insufficient: the same script can play different animation names depending on the selector leaf and combat context. Preserve original path references to appearances, sounds and object templates for later asset-loader work; do not replace them with server packet fields.

No executable, TRE, server file, build, or commit was changed. The nested asset excerpts came from the configured effective archive overlay and are recorded in ignored read-only scratch under `local-private/session-2026-09-23-combat-selection/`.
