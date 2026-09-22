# ENVM extension-chunk compatibility audit — 2026-09-12

## Result

The effective ground-environment corpus contains three extension shapes outside the flat chunks explicitly recognized by Stardust.exe's ENVM/0000 loader: `TLOK`, `SKYB`, and nested `FORM/DIST/0000`. Exact deployed payloads and server-side parser correlations are now recorded. The executable has no literal tag recognizer for these tags, and its ENVM default branch skips unknown leaf chunks without producing GroundEnvironment state.

Two contracts must remain separate: behavioral compatibility with Stardust.exe means tolerating/preserving or skipping these extensions; full original-asset capability means parsing them into extension IR so a later renderer can implement them deliberately.

## Corpus occurrence

Across 63 effective `terrain/environment/*.iff` members:

| Shape | Count | Members |
|---|---:|---|
| `TLOK` leaf chunk, 5 bytes | 7 | six Kashyyyk environments and Mustafar |
| `SKYB` leaf chunk, 13 bytes | 1 | `kashyyyk_pob_dungeons.iff` |
| `FORM/DIST/0000`, 87-byte FORM payload | 4 | `mustafar.iff` |

Exact member/archive/priority/payload rows are in `effective_envm_chunks.tsv`.

## TLOK

Every deployed payload is `U8 enabled` followed by little-endian `F32 normalizedCyclePosition`. All enabled bytes are 1. Values are 0.15, 0.25, 0.30, or 0.90. The tag and normalized range strongly identify a time-of-day lock, but Stardust.exe contains no `TLOK` immediate and its ENVM loader does not assign these bytes to state.

Schema is Confirmed; the time-lock role is Strongly inferred; active behavior in this executable is Confirmed absent. Replacement IR should retain `{enabled, normalizedCyclePosition}` without applying it until policy is separately validated.

## SKYB

The sole payload is `U8 modeOrEnabled = 0` plus `cstring name = "nebula_kash"`. Current server source `SpaceTerrainAppearance::parseSKYB` independently confirms the byte-plus-string body schema for a `FORM/SKYB/0000` variant. The ENVM copy is a flat `SKYB` chunk with the same body. Stardust.exe has no `SKYB` immediate and skips it as unknown, so the string does not reach GroundEnvironment state in this build.

The byte's exact enum meaning and resource-name expansion remain unknown. Preserve it as raw mode plus logical skybox identifier.

## DIST

Mustafar contains four nested forms:

```text
FORM DIST
  0000
    cstring appearancePath
    F32 x
    F32 z
    F32 y
    F32 angle1Degrees
    F32 angle2Degrees
    F32 angle3Degrees
```

`SpaceTerrainAppearance::parseDIST` in current server source confirms the string, X/Z/Y position, and three degree-angle schema. The records reference `appearance/pt_mustafar_distant_volcano_01.prt`, `_02.prt`, and `_03.prt` with distinct positions.

Stardust.exe contains no `DIST` immediate. Its ENVM switch receives a form's type through the IFF current-name helper but has no matching case. The default path is designed for an unknown chunk, not a well-formed nested-FORM parser. Static evidence supports “unsupported by this GroundEnvironment loader”; exact release-build recovery after encountering the form is not claimed. A replacement should parse it rather than emulate that fragile behavior.

## Executable-wide tag search

No executable-code immediate references were found for `TLOK` (`0x544C4F4B`), `SKYB` (`0x534B5942`), or `DIST` (`0x44495354`). By contrast, the base loader directly compares `NSKY`, `STAR`, `SUN `, `SSUN`, `MOON`, `SMOO`, and `CELS`. This negative evidence is strong because this codebase consistently represents IFF tags as literal 32-bit immediates.

## Replacement requirements

1. Support flat leaf chunks and nested forms without alignment assumptions.
2. Retain/report unknown extension data rather than discard it in the asset compiler.
3. Parse TLOK into raw enabled/mode plus normalized value; defer behavior.
4. Parse SKYB into raw byte plus identifier; defer identifier-to-resource policy.
5. Parse DIST into appearance path, X/Z/Y position, and three degree angles in source order.
6. Treat extension rendering as optional capability, not a reason to alter confirmed base behavior.

## Confidence

- **Confirmed:** occurrence counts, exact bytes, TLOK field types, SKYB field types, DIST field order, absent Stardust.exe tag immediates, and unknown-leaf skip behavior.
- **Strongly inferred:** TLOK is an enabled normalized time lock; SKYB names a skybox/nebula preset; DIST is intended distant appearance placement.
- **Unknown:** TLOK precedence relative to server time, SKYB byte enum/resource lookup, and whether a historical newer client rendered the DIST extensions.
