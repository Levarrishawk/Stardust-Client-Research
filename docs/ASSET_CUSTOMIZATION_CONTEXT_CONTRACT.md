# Selected owned ACST customization context

[AssetCustomizationContextBuilder](../poc/Stardust.Core/Assets/AssetCustomizationContextBuilder.cs) binds selected descriptions from the accepted [ACST directory](ASSET_CUSTOMIZATION_DIRECTORY_CONTRACT.md), the owned [CIDM dictionary](CUSTOMIZATION_ID_DICTIONARY_CONTRACT.md), and explicitly supplied owned [palette descriptions](../poc/Stardust.Core/Assets/PaletteDecoder.cs). It returns ranges and a complete selected authored-default set suitable for the [ordered local-value projector](CUSTOMIZATION_VARIABLE_PROJECTION_CONTRACT.md). These are asset construction defaults, not recovered remote player state or a live mount graph.

```csharp
AssetCustomizationContext context = AssetCustomizationContextBuilder.Build(
    AssetCustomizationDirectoryQuery directoryQuery,
    CustomizationIdDictionary dictionary,
    IReadOnlyCollection<ShaderCustomizationKey> selectedKeys,
    Func<string, PaletteDescription?> ownedPaletteResolver,
    CancellationToken token = default,
    AssetCustomizationContextLimits? limits = null);
```

Output properties:

- `RangeContext`: existing immutable `CustomizationVariableRangeContext`.
- `AuthoredDefaults`: read-only full selected key/value map, with no implicit defaults.
- `Bindings`: read-only ordered `AssetCustomizationContextBinding` rows containing key, CIDM ID, exact full name, `Kind` (`Ranged`/`Palette`), inclusive minimum, exclusive maximum, authored default and nullable palette path.

Selected keys must be unique, nonempty and valid under the existing key policy. The query must be found with a nonzero root key. Descriptions are copied before callbacks; exact full names map via `/private/` or `/shared_owner/` and the Boolean remains part of the key. A selected descriptor and the identical full-name CIDM entry must both exist. There is no slash/case repair or guessed namespace. Unsupported/absent selected namespaces fail. Unselected names and palette paths are not bound or resolved. Identical duplicate descriptors are tolerated; conflicting duplicates fail before callbacks, including conflicts elsewhere in the supplied query.

## Ranges, palette resolution and modern admission

Ranged descriptions preserve signed ACST minimum/exclusive maximum. Palette descriptions resolve their exact path once per distinct selected path; the range is `[0, actual Entries.Count)`. Neither authored index nor a color-fetch clamp supplies a guessed count. Returned null, zero entries, unsupported palette version/shape, explicit missing-palette fallback diagnostics and exceeded limits fail. All selected authored defaults must lie within the resulting range. This is an explicit modern seed-admission policy: raw metadata, including invalid defaults, remains available in the directory. The builder does not clamp, substitute zero, fetch missing resources or mutate inputs.

Default limits are 4096 selected keys, 4096 query descriptors, 256 distinct palette paths, 1024 entries per palette and 262144 total unique-path palette entries. Configurable hard caps are 4096 keys/descriptors/paths, 1024 entries per palette and 4194304 aggregate entries. Palette snapshots additionally bound diagnostics to 32 strings of at most 256 characters and ignored trailing bytes to 4096. Version high byte must be 3, matching the accepted decoder. Structural collections are copied through bounded enumeration, even if their Count property lies. These are logical admission limits, not peak-memory accounting.

The resolver is borrowed and never retained/disposed; each supplied palette is also borrowed temporarily. Entries/diagnostics/trailing bytes are copied only to verify stability, then discarded. The result retains solely owned ranges/default numbers and immutable names/binding records. Query/selection inputs and previous returned palettes are checked around each callback and before return. Visible mutations reject with `InvalidDataException`; callback exceptions propagate. Input mutations performed by external code are not undone. Inputs must remain stable while being snapshotted; mutation followed by restoration between checks is not claimed detectable.

Same-thread nested builds reject with `InvalidOperationException`; a swallowed nested-build exception still poisons the outer build. The thread-local guard clears in `finally` after all failures. Independent builds on other threads remain valid. Cancellation is checked before reads/snapshots, through key/description/selected traversal, around callbacks and immediately before publication, including after final palette comparisons and result construction. Observed failure/cancellation returns no prefix and does not change a prior context. There are no worker jobs, world ownership, live callbacks or cleanup resources in the result.

## Bounded original human result

Human SAT query CRC `D700D576`, root 11821, has 36 descriptors. Eight explicitly selected face/material profile keys match exact ACST and CIDM records; all authored defaults are 0:

| Selected key | Namespace | Bound range or palette |
| --- | --- | --- |
| index_style_eyebrow | private | `[0,7)` |
| index_style_beard | private | `[0,18)` |
| index_style_freckles | private | `[0,5)` |
| index_color_skin | shared_owner | `palette/pc_skin_hum.pal`, 256 entries |
| index_color_facial_hair | private | `palette/pc_hair_hum.pal`, 256 entries |
| index_color_skin | private | `palette/pc_skin_hum_highlights.pal`, 256 entries |
| index_color_2 | private | `palette/pc_eye_hum.pal`, 256 entries |
| index_color_3 | private | `palette/white.pal`, 2 entries |

The existing 76-source/5,940,390-byte face/albedo bundle owns exactly these five palette paths across face resources/material palette defaults. Equivalent aliases compare without new reads. Five resolver calls borrow the existing descriptions; total selected entries 1026, no missing paths. ACST/CIDM are two separately pinned input resources, not silently charged to that existing bundle. Palette entry counts now come from original bytes; current remote values, full unselected directory defaults and live linked/mounted variable construction remain separate.

[Native scenarios](../poc/Stardust.Core.SmokeTests/AssetCustomizationContextScenarios.cs) and [independent oracle](../poc/round23-tests/Assets/verify_context.py) pass 25 synthetic fixtures (seven positive/eighteen rejection), 17 additional callback/budget/cancellation failures, exact bounds, result ownership/read-only maps, guard recovery and 16 concurrent builds. A lying endless palette list rejects after bounded enumeration; controlled final palette traversal cancellation returns no result. Python independently verifies original ACST selected descriptions, all CIDM IDs, five RIFF palette counts/source identities and zero authored defaults. Fresh ACST/default-albedo regressions pass. **Confidence:** Confirmed selected original metadata/resource binding and modern fixture behavior; no player/live graph claim. [Checkpoint and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R23_ACST_CONTEXT.md) give the precise next boundary.
