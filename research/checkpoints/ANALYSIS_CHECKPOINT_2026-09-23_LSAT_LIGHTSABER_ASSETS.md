# LSAT lightsaber-blade appearance assets

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe` and configured effective TRE overlay.

## Result

`LightsaberAppearanceTemplate` registers appearance tag `LSAT` at
`0x0064FBD0`. Its factory/constructor `0x0064FEC0/0x0064FF20` accepts only
version `0000` and delegates to loader `0x00650170`. The effective overlay
contains **45 live `.lsb` files**, all `LSAT/0000`; no malformed envelope or
unsupported deployed version was found.

The nested grammar is:

```text
FORM LSAT / FORM 0000
  BASE  null-terminated appearance path
  ASND  null-terminated sound path
  FORM BLAD  (one or more)
    SHDR  null-terminated shader path
    LGTH  F32
    WDTH  F32
    OPEN  F32
    CLOS  F32
  FORM LGHT  (optional)
    COLR  four U8 channels
    RANG  two F32
    TIME  two F32
    DAYN  U8 boolean
```

The loader uses defaults for blade values and overrides each with its named
chunk; these are not raw C++ structure dumps. It creates one retained blade
definition per BLAD form and stores light values on the template. In the
effective corpus, 40 files have one BLAD and five have two (50 blade forms);
19 files have LGHT. All 45 have BASE and ASND, and all 50 blade forms contain
each of the five named chunks. Every literal BASE/ASND/SHDR dependency
resolves as a live effective path (30 distinct paths).

## Evidence/confidence

- Registration `0x0064FBD0`, version dispatch `0x0064FF20`, reader
  `0x00650170`: **confirmed by executable code**.
- Corpus counts, field widths, and dependency existence:
  **confirmed by full effective-overlay scan**.
- `LGTH` and `WDTH` as length/width, `OPEN`/`CLOS` as transition values,
  and LGHT as optional authored light: **strongly inferred** from tags and
  runtime defaults. Precise unit/timing semantics await runtime consumers.

## Replacement implication

Lightsabers are a composite original-asset resource: base hilt appearance,
blade shader/ribbon records, looping/activation sound route, and optional
light settings. Do not replace `.lsb` with a hard-coded colored cylinder.
Preserve blade count (including two-blade variants), authored shader paths,
and all numeric fields in an engine-neutral intermediate representation.

Scratch: `local-private/session-2026-09-23-marathon/scan_lsat.py`,
`block43_lsat.txt`, and decompiled reader files.
