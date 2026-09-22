# Analysis Checkpoint: PFFP Fixed-Function Vertex Lighting

## Structural result

PFFP is the fixed-function vertex-lighting description paired with fixed-function STAG texture combiners. It is not itself the pixel combiner.

| Version | Serialized payload | Effective records | Distinct payloads |
|---|---|---:|---:|
| 0000 | U8 enable ×3, U32 material tag, U8 material source ×4 | 1 | 1 |
| 0001 | U8 enable ×3, U8 material source ×4 | 704 | 11 |

Loader addresses are `0x00762360` and `0x00762440`. Both normalize the first three bytes to booleans and widen the final four bytes into enum-sized runtime members.

## Material-tag migration

The only effective v0000 payload is:

```text
01 00 00 4e 49 41 4d 00 00 00 00
```

The U32 is IFF tag `MAIN`. Later PASS versions expose the material tag at PASS runtime `+0x7C`, and PFFP v0001 removes this U32. This confirms that the old field selected the shader material parameter and that the role migrated upward into PASS.

## Semantic model

The seven v0001 bytes match the complete D3D9 fixed-function vertex-lighting controls required by this asset branch:

- lighting enable;
- specular enable;
- vertex-color/material-source enable;
- four material-source selectors for diffuse, ambient, emissive and specular channels.

Corpus values are only 0/1. For material sources these correspond coherently to material constants versus the primary vertex color. Payload patterns also show source selectors becoming one principally when the third enable is set.

The broad roles are **Strongly inferred** from the exact D3D9 contract, version migration and value correlation. The original source-level order of the four source channels is not independently symbol-confirmed. The importer must preserve serialized order so fixtures can lock the final channel mapping without reparsing assets.

## Replacement consequence

Represent PFFP as a versioned DTO and compile it to a small vertex-shader variant. Keep lighting/specular/color-source decisions separate from the STAG fragment expression graph. For modern GPUs there is no need to reproduce D3D9 fixed-function device states; behavioral equivalence comes from generated shader inputs and material-source selection.

## Reproducibility

`shader_version_payload_inventory.py` now records PFFP version chunks as well as STAG. Its configured-overlay run found 23,286 live shader/effect winners and the 705 PFFP records summarized above.
