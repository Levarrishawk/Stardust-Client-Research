# Owned customization ID dictionary

[CustomizationIdDictionary](../../SWG-Godot-Client/poc/Stardust.Core/Assets/CustomizationIdDictionary.cs) implements only the proven `FORM CIDM / FORM 0001 / DATA` dictionary. It uses the existing owned [IFF reader](../../SWG-Godot-Client/poc/Stardust.Core/Assets/IffDocument.cs), big-endian IFF extents and direct declared-end traversal without external padding. DATA has **no count**: repeat `uint16 little-endian ID`, then NUL-terminated ASCII full name, until DATA is exhausted. A dictionary ID is separate from a palette index, variable value and the packed wire wide-value bit. No packed-value decoder or application-state translation is added.

The [September 12 evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_CUSTOMIZATION_MANAGER_TABLES.md) established consecutive IDs, duplicate-name diagnostics and the seven-bit ceiling. This implementation rejects malformed input atomically: no partially parsed dictionary escapes. It requires one version form and one DATA chunk, nonempty records, IDs consecutive from one and below 128, and unique names under ordinal comparison. Unsupported versions/forms/chunks, truncated IDs/names, extra bytes, invalid names and exceeded limits reject with `InvalidDataException`. Invalid limit settings reject with `ArgumentOutOfRangeException`. Printable ASCII bytes 32..126 and nonempty names are an explicit modern admission policy; diagnostic legacy behavior is not claimed to be exception-identical.

Default limits are 65,536 source bytes, 127 records and 1,024 name bytes. Configurable hard ceilings are 1MiB source bytes and 4,096 name bytes; record maximum remains 127. IFF limits are three nodes and depth two. Limits validate before copying input. Cancellation is checked before IFF parsing, per record and before publication; the bounded existing IFF parser itself has no cancellation callback. Input bytes are copied by IFF parsing; published immutable records and read-only ordered entries/ID/name indices retain only owned strings. The caller can clear its input and close source archives after loading. There are no disposable resources in the returned dictionary.

Names retain exact spelling, case and slash placement. `Entries` preserves table order; `ById` uses the complete `ushort` ID with no masking, and `ByName` uses ordinal comparison. `TryGetValue` on either index provides ordinary unknown-ID/name handling. A raw high-bit wire byte must be decoded by its own codec before lookup; this reader does not strip a bit or interpret values.

`ProjectName(fullName)` returns a `CustomizationNameProjection` with the original `FullName`, an explicit `Kind`, and a nullable existing `BlueprintCustomizationKey`:

| Exact ordinal prefix with nonempty remainder | Result |
| --- | --- |
| `/private/` | remainder as `VariableName`, `FactoryFlag=true` |
| `/shared_owner/` | remainder as `VariableName`, `FactoryFlag=false` |
| Anything else, including empty remainder | `UnsupportedNamespace`, null key, original full name |

This helper performs prefix projection only. It does not normalize, guess a namespace, validate a consumer's variable schema, map a palette index or infer a value. Case changes, missing leading slash and unknown namespaces do not project. The shipped ID 118 name `private/alternate_shader_blade` therefore remains available in both dictionary indices but unprojected.

## Bounded original verification

Readonly configured overlay `swgemu_live.cfg` selects `customization/customization_id_manager.iff` at priority 53 from `stardust_3_01.tre`: 3,697 bytes, SHA-256 `FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2`. All 121 rows, IDs 1..121, exactly agree with an independent Python byte oracle. There are 120 projected rows and one unprojected ID 118. No extracted original table is added to curated data.

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/CustomizationIdDictionaryScenarios.cs) include synthetic positives, ID127, case-distinct names, namespace results, ownership/read-only indices, exact bounds, all prefix truncations and 89 rejection cases including envelope/version/extent, ID/name, budgets and precancellation. Failed loads preserve previously returned dictionaries. Original archives close before parsing; input bytes are cleared afterward. [Independent verifier](../../SWG-Godot-Client/poc/round20-tests/Assets/verify_cidm.py) verifies the configuration/resource pins, effective priority, raw IFF offsets/extents and every original row/projection. Existing original albedo regression passes 16 reader negatives and two initializer budget failures with unchanged 37/39-source routes.

**Confidence:** Confirmed structure/original identities and modern fixture behavior. Namespace projection implements the already established private/shared-owner convention explicitly. Packed value semantics, typed setters, current-incarnation field4 integration and presentation revisions remain separate work. Exact reproduction and the next integration action are in the [Round20 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R20_CIDM_DICTIONARY.md).
