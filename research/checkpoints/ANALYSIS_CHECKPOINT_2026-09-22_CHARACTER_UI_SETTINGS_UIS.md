# Character UI Settings (`.uis`)

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe` plus four deployed profile samples

## Identity and path

`0x009CB820` constructs a per-character profile path under `profiles/` from the
active login/server/character identity and the `.uis` suffix. Deployed files
confirm `profiles/<account>/<cluster>/<characterNetworkId>.uis`. The character
identifier is the decimal legacy U64 NetworkId, not a pointer-sized value.

## Exact current grammar

The writer `0x009CCA80` emits:

```text
FORM UIST
  FORM 0003
    RESO { U32 little-endian width, U32 little-endian height }
    FORM OWNE ... repeated
```

Each owner record written by `0x009CC840` is:

```text
FORM OWNE
  NAME { owner/mediator ASCII C-string }
  [SIZE { S32 width, S32 height }]
  [LOCA { S32 x, S32 y }]
  FORM STRS { DATA { key ASCII C-string, value Unicode string } ... }
  FORM DATS { DATA { key ASCII C-string, U32 byteCount, opaque bytes } ... }
  FORM INTS { DATA { key ASCII C-string, S32 value } ... }
```

`0x009CBC40` performs the matching decode and warns on duplicate keys.
`0x009CCC20` accepts only `UIST/0003`; versions below 0003 are explicitly no
longer supported and other versions are invalid. `0x009CCEA0` is the guarded
save path. On load, `0x009CC310` compares `RESO` with current resolution. A
mismatch resets geometry applicability while retaining owner-state processing.

## Deployed corpus validation

| Character ID | Bytes | Owners | STRS | DATS | INTS | Resolution |
|---|---:|---:|---:|---:|---:|---|
| `281474999168520` | 129,357 | 33 | 9 | 78 | 110 | 1680x1050 |
| `281475002490227` | 112,158 | 36 | 13 | 81 | 115 | 1920x1200 |
| `281475005085310` | 275,579 | 51 | 15 | 101 | 168 | 1920x1200 |
| `281475013610071` | 17,578 | 15 | 10 | 78 | 60 | 1920x1200 |

Observed owners include chat windows, toolbar, radar, inventory instances,
skill manager, character sheet, waypoint monitor and space-prefixed variants.
Opaque DATS values include mediator-defined state such as Unicode chat history;
INTS include layout flags, sort selections, channel IDs and radar range/mode.

## Replacement policy

Provide a bounded `UIStateStore` keyed by character U64 and owner string.
Preserve Unicode string, opaque byte blob and signed 32-bit domains plus optional
geometry. Treat owner blobs as mediator-owned versioned data. Imported chat
history and other user text must never be executed as UI script.

The grammar, version gate, geometry behavior, path scope and corpus counts are
**Confirmed**. Individual opaque DATS payloads remain mediator-local fidelity
tasks.
