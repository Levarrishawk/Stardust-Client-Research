# CDF tolerated-extension policy: PALV, OBJT, and VLEF

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The rare wearable `PALV`/`OBJT` records and vehicle `VLEF` records are asset extensions tolerated but not consumed by this executable. Their residual field meanings are therefore not missing behavior from `Stardust.exe`; enabling them in a replacement would be a separately validated enhancement.

## Wearable loader proof

`WearableData_ctorAndLoad_VA_006FF150` iterates every child of `FORM WEAR`. It has exactly two recognized cases:

- `MESH`: one skeletal-mesh resource path;
- `WCSI`: customization variable name plus 32-bit value.

Every other child tag follows one generic branch:

1. convert the tag to printable diagnostic characters;
2. set the owning CDF's unknown-data flag at `+0x1A`;
3. call the archive skip/exit routine;
4. continue to the next WEAR child.

There is no executable immediate/reference for `PALV` (`0x50414C56`) or `OBJT` (`0x4F424A54`). Neither record is retained in `WearableData`, prepared, nor applied by `0x006FEEC0`. Consequently:

- PALV's observed string/string/U16 grammar is corpus evidence for a newer extension, not an active Stardust.exe customization path;
- OBJT's observed path/U32 grammar is likewise an inactive extension;
- the OBJT trailing U32 being zero in all eight effective records has no client behavior to recover from this executable.

## Vehicle link-effect proof

The top-level ClientDataFile dispatcher has no `VLEF` (`0x564C4546`) case or executable immediate/reference. Its generic unknown-form branch skips the entire form safely. No retained vector or later runtime consumer exists in the analyzed CDF object.

The sixteen effective `VLEF` records still establish a useful future extension grammar—source hardpoint, destination hardpoint, and `.ltn` appearance—but original-client parity requires ignoring them, not rendering them.

## Replacement policy

The original-compatible default should:

1. recognize container boundaries generically;
2. retain unknown extension bytes/tags in diagnostic asset metadata when practical;
3. skip `PALV`, `OBJT`, and `VLEF` behavior by default for Stardust.exe parity;
4. expose typed parsers only behind an explicit enhancement capability;
5. never make these records a networking, world-loading, or original-asset acceptance failure.

If a future client intentionally enables them, treat the observed schemas as versioned extension contracts and validate resulting appearance/customization behavior independently. Do not attribute that behavior to this executable.

## Confidence

- Only MESH/WCSI recognized inside WEAR: **Confirmed by complete loader control flow**.
- PALV/OBJT absent from executable tag references and discarded by generic branch: **Confirmed**.
- VLEF absent from dispatcher and discarded as an unknown form: **Confirmed**.
- Observed extension grammars: **Confirmed from effective corpus bytes**.
- Intended behavior in another/newer client: **Unknown and outside Stardust.exe compatibility**.

