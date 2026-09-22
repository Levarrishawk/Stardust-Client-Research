# Analysis checkpoint: `IMPL/0009` forward schema

Date: 2026-09-22  
Scope: effective shader/effect overlay compared with Stardust.exe `IMPL/0007`

## Result

All 82 effective forward implementation forms are `IMPL/0009`; no effective `IMPL/0008` winner remains. Their observable IFF grammar is structurally identical to the executable-supported `IMPL/0007` superset.

The executable rejects `0009` because the dispatcher ends at `0007`, not because the installed `0009` records require a different envelope decoder.

## Shared grammar

```text
FORM IMPL / version
    SCAP                     required capability-tag list
    [OPTN]                   optional option-tag list
    DATA (7 bytes):
        U8  passCount
        TAG material/phase identity
        U8  flag0
        U8  flag1
    repeat passCount:
        FORM PASS
```

The confirmed `0007` loader at `0x0075E120` performs capability filtering, optional option filtering, reads the seven-byte DATA record, resolves the TAG through the existing tag registry, allocates `passCount` pass objects, and parses each child pass.

## Corpus comparison

`IMPL/0007` and effective `IMPL/0009` share every observed child ordering:

- `SCAP;DATA;PASS...`
- `SCAP;OPTN;DATA;PASS...`

Effective `0009` has one or two passes; `0007` additionally demonstrates larger pass counts, so the existing counted loop already generalizes. Both use the same seven-byte DATA width and domains. `0009` introduces observed tag `PREA` alongside established tags such as `MAIN`, `ALPH`, and `WATR`, but the tag field is already generic rather than an enum-limited parser branch.

## Replacement contract

- Accept `IMPL/0009` with the explicit `0007` envelope grammar.
- Continue to preserve the version tag in IR and diagnostics.
- Resolve DATA TAG values generically; do not hard-code only the tags seen by this executable.
- Decode nested `PASS/0010` with its newly aligned one-byte extension.
- Retain strict IFF bounds and pass-count validation.

This closes the forward implementation envelope as a parser blocker. Visual correctness still depends on nested pass semantics, particularly the one new `PASS/0010` Boolean.

## Confidence

- effective winner count/version: **Confirmed**;
- child-form grammar and DATA width/order: **Confirmed across corpus and 0007 code**;
- safe structural alias of 0009 to 0007 envelope: **Strong**, because no effective counterexample exists;
- historical reason for version-number jump: **Unknown and not required for parsing**.

