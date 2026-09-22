# Machine Options IFF

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## File lifecycle

`0x00B8D930` installs the local machine-option manager and loads
`swgemu_machineoptions.iff`; `0x00B8D9F0` writes it. The name is embedded in the
analyzed `Stardust.exe`. The manager registers shutdown cleanup and keeps typed
entries keyed by a pair of ASCII strings (configuration section and key).

## Exact deployed grammar

The file is ordinary SWG IFF:

```text
FORM OPTN
  FORM 0002
    zero or more typed chunks
```

The writer at `0x00B9C230` emits version `0002`. Each entry is one chunk:

| Chunk | Value | Following fields |
|---|---|---|
| `BOOL` | U8 Boolean | key C-string, section C-string |
| `FLT ` | 4 raw bytes / IEEE-754 float | key C-string, section C-string |
| `INT ` | signed/raw 32-bit integer | key C-string, section C-string |
| `STDS` | narrow C-string | key C-string, section C-string |
| `UNIS` | Unicode string through the IFF Unicode codec | key C-string, section C-string |

The value precedes the key and section. The reader `0x00B9C400` accepts these
five chunk types, skips unknown chunks, and rebuilds both the generic entry list
and type-specific lookup collections. The outer loader recognizes `0002`; an
older `0000` is detected and warned about rather than parsed as current data.

Typed lookup (for example Boolean lookup at `0x00B9BAC0`) first searches loaded
entries by exact section and key. If no matching loaded entry exists, it falls
back to `ConfigFile::getKeyBool(section,key,default)`, confirming the precedence
chain used by `CuiPreferences`.

## Replacement policy

The modern client may use a safer internal store, but an import/export adapter
for `OPTN/0002` is straightforward and useful for preserving existing user
profiles. Parsing must enforce chunk/string/file-size limits and use explicit
fixed-width values. Unknown chunks should be retained or safely skipped; they
must never be interpreted as native x64 structures.

## Confidence

All grammar and field-order statements above are **Confirmed by code** in
`0x00B9C160`, `0x00B9C230` and `0x00B9C400`. Friendly semantics for individual
keys come from their typed registration sites, chiefly `0x0090FA10`.
