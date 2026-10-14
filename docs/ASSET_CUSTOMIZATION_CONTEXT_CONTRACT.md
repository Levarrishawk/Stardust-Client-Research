# Selected owned ACST customization context

AssetCustomizationContextBuilder (external historical reference) binds selected descriptions from the accepted [ACST directory](ASSET_CUSTOMIZATION_DIRECTORY_CONTRACT.md), the owned [CIDM dictionary](CUSTOMIZATION_ID_DICTIONARY_CONTRACT.md), and explicitly supplied owned palette descriptions (external historical reference). It returns ranges and a complete selected authored-default set suitable for the [ordered local-value projector](CUSTOMIZATION_VARIABLE_PROJECTION_CONTRACT.md). These are asset construction defaults, not recovered remote player state or a live mount graph.

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

Ranged descriptions preserve signed ACST minimum/exclusive maximum. Palette descriptions resolve their exact path once per distinct selected path; the range is `the separate implementation record and independent oracle (external historical reference) pass 25 synthetic fixtures (seven positive/eighteen rejection), 17 additional callback/budget/cancellation failures, exact bounds, result ownership/read-only maps, guard recovery and 16 concurrent builds. A lying endless palette list rejects after bounded enumeration; controlled final palette traversal cancellation returns no result. Python independently verifies original ACST selected descriptions, all CIDM IDs, five RIFF palette counts/source identities and zero authored defaults. Fresh ACST/default-albedo regressions pass. **Confidence:** Confirmed selected original metadata/resource binding and modern fixture behavior; no player/live graph claim. [Checkpoint and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R23_ACST_CONTEXT.md) give the precise next boundary.
