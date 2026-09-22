# Legacy UI Event and Expression Corpus

## Outcome

The effective UI event surface is now preserved as a reproducible 2,720-row
fixture (`ui_event_values.tsv`), adding actual expression values to the earlier
tag/property inventory. This materially constrains an original-asset-compatible
UI interpreter.

## Effective corpus

Across 251 winning `.ui`/`.inc` records, event-like attributes total 2,720.
Principal counts are: OnShow 590, OnEnableEffector 444, OnHide 408,
OnDisableEffector 383, OnEnable 248, OnDisable 236, OnPress 106, OnSet 75,
OnActivate 45, OnDeactivate 44, OnShowEffector 35, OnHoverIn 24, OnHoverOut
23, OnUnset 19, OnRunScript 14, OnSliderChange 10, plus rarer variants.

The extractor preserves effective archive provenance, source path, element,
property spelling and normalized value without modifying any asset.

## Expression surface observed

Event bodies are whitespace-separated assignment programs, not arbitrary
native scripts in the effective corpus. They require:

- relative object paths (`parent`, repeated `parent`, child names);
- chained property paths and selected data-row fields;
- assignment to Boolean, integer, float, string/style and object-property
  values;
- copying a property from another node;
- equality comparison, demonstrated by Boolean toggles such as
  `enabled=...enabled==false`;
- arithmetic `+`, `-`, `*`, `/` and statement ordering;
- case-tolerant property/Boolean spelling (`true`, `false`, and legacy `t`);
- synthetic press and activation assignments that trigger ordinary widget
  callbacks;
- absolute and relative named-path resolution for effectors.

All 14 effective `OnRunScript` instances are the same two-statement layout
calculation in `ui/ui_scripted.inc`:

```text
sizeY = parent.sizeY * Value / 100
locationY = parent.sizeY - sizeY
```

This sharply bounds the deployed scripting requirement. A general-purpose
language VM is not justified for initial compatibility; a small deterministic
assignment/expression evaluator covers the observed corpus.

Effector attributes usually name reusable objects such as
`/effectors.opacity.fast.fadefull` rather than embedding expressions. Misspellings,
relative names, and malformed legacy values occur and must remain diagnosable.

## Replacement rules

1. Parse event bodies into an AST; never execute them through `eval` or a host
   scripting language.
2. Resolve all paths inside the legacy UI object tree with explicit parent and
   absolute-root rules.
3. Evaluate statements in source order because many swap temporary properties
   and then consume the new values.
4. Keep property typing/coercion in the widget-property adapter and preserve
   unknown properties losslessly.
5. Cap statement count, path depth, and expression depth for hostile/corrupt
   assets.
6. Bind event-generated press/activation changes through the same event queue
   as physical input to prevent re-entrant lifetime bugs.

## Confidence

- Counts, values, operators and effective-file provenance: **Confirmed by
  read-only extraction of the configured TRE overlay**.
- Minimal evaluator feature set: **Strongly inferred** from complete effective
  corpus coverage.
- Exact legacy coercion/error behavior and precedence in every malformed case:
  **Unknown**; retain compatibility fixtures and prefer deterministic safe
  diagnostics.

