# Customizable shader decoder contract

## Owned deployed structure

`CustomizableShaderDecoder` accepts CSHD0001 only. One inline SSHT must appear first, followed by optional TXTR then optional TFAC. Other versions, MATR, unknown customization records and reordered/duplicate outer forms remain rejected. Original constructor 0077C7D0 dispatches 0000/0001; version 0000 is baseline-supported but absent from the current installed overlay and deferred.

TXTR contains DATA then FORM CUST. DATA is LE **I16 count**, followed by that many NUL-terminated texture logical paths. Each CUST child is TX1D with **U32 texture tag, I16 first-choice index, I16 choice count, NUL exact-case variable name, U8 raw private flag, I16 authored default**. Version1 loader0077CE70 reads the signed count at 0077CFCD/0077CFF3 and resource names at 0077D04F; record loader0077BD80 reads each field at 0077BDBF/0077BDD1/0077BDE5/0077BDFF/0077BE0C/0077BE24. The modern decoder requires a positive bounded selection range contained in the table, but preserves signed authored defaults rather than clamping during parsing.

TFAC contains repeated PAL chunks: **NUL exact-case variable name, U8 raw private flag, U32 factor tag, NUL palette logical path, I32 authored default**. Loader0077C020 consumes these at 0077C065/0077C072/0077C08A/0077C0A2/0077C0AF. Authored order, duplicate variable uses and all raw flags/defaults remain intact. Names are printable ASCII; logical paths normalize through the existing asset path policy. Variable names retain case and are not resource paths.

Default IFF limits remain16 MiB/100,000 nodes/depth32. Each choice/selection/factor table caps at 4,096 records. TX1D variable names cap259 characters, PAL variable/palette names511, choice paths1,024. Immutable descriptions retain the owned original metadata, inline static references and all choices, rather than resolving every possible texture eagerly.

## Runtime evidence and confidence

**Confirmed:** the raw private flag is normalized to a Boolean by the record loaders. Variable identity in0077D640 matches exact name bytes plus that Boolean. Runtime variable lookup007859B0 chooses `/private/` for true and `/shared_owner/` for false; constructors00785340/00785450 initialize those exact prefixes from strings188F570/186B880. Preserve these namespaces separately.

**Confirmed:** texture application0077BEE0 clamps a selected value to0..choiceCount-1, adds first-choice index and fetches the resulting table entry through0077C550 before setting the static shader texture. Palette application0077BFB0 fetches a palette color for the current variable and sets the target factor. CSHD template application0077C4A0 visits material, texture and factor operations in order.

**Open:** complete variable registration, conflict-resolution policy, palette error policy and creation/update lifecycle. Runtime instance constructor007857D0 initially zeroes its variable vector; authored defaults are separately retained by variable-template construction. Do not equate inline shader values, zero-initialized instance values, serialized authored defaults and applied customization state.

## Effective corpus and implementation consequence

All **5,173 live CSHD resources** from 48 mounts are version 0001. Shapes:4,663 inline+TFAC;485 inline+TXTR+TFAC;23 inline+TXTR;2 inline-only. They contain5,446 choices,856 TX1D selections and 7,908 PAL factors referencing243 palette logical names. Texture flags:677 zero/179 one. Palette flags:1,124 zero/6,784 one. Authored texture defaults0..1, palette defaults0..255; all current texture defaults lie in range. Two shaders reuse the same variable/default0 with selection counts 7 and 6; preserve both ranges rather than requiring equal counts. No duplicate texture target tags occur in this corpus. Acceptance beyond that corpus is governed by explicit parser limits.

The YT-1300 inline shader names patterna/patternb_s06; its authored selection default0 refers to different entries in the choice table. Unqualified flattening would erase a meaningful compatibility boundary. Current decoder exposes structure only; default/authored-state preview must be explicitly labeled and must not claim original runtime state or palette shading.

## Reproduction

`verify_customizable_shaders.py ROOT CONFIG PRIVATE_OUTPUT --core-dll DLL` scans effective shader winners with independent raw-name TRE lookup, writes a private pinned manifest, and compares every ordered field plus inline payload identity. Manifest SHA `9B2FEE105ABF0CABC41E7A79DADE3526BF612D888A41712A8B49C331F274576A`. Native `--cshd-list` requires that exact member-list hash. Synthetic ownership, exact case, signed/raw preservation, complete truncation, range/version/type negatives pass. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CUSTOMIZABLE_SHADER_READER.md).

Explicit authored texture inspection is now specified [separately](AUTHORED_CUSTOMIZATION_TEXTURE_CONTRACT.md); full dynamic customization remains open.
