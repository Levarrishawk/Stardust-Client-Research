# Commodity item-type hierarchy closure

Date: 2026-09-20  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

`CommoditiesItemTypeListResponse` (`0xD4E937FC`) is now structurally byte-complete, including the dormant nonempty case. The listener branch at `0x006A85BC` builds an Archive response object whose custom container decoder is `0x006A8D00`; its nested hierarchy decoder is `0x006A9030`.

After the response's leading ASCII catalog/version string, the item hierarchy is:

```text
U32 outerCount
repeat outerCount:
    U32 outerTypeId
    U32 childCount
    repeat childCount:
        U32 childTypeId
        ASCII stringTable
        ASCII stringKey
        U32 value
```

The leaf decoder is `0x012B31A0`. It reads two Archive ASCII strings (compact U16 length, with `0xFFFF` escape to U32 length, followed by raw bytes) and one U32. The two strings form a localization identity; the final U32's precise published business label is not established. The decoder inserts leaves into an inner ordered map keyed by `childTypeId`, then inserts that map into an outer ordered map keyed by `outerTypeId`.

Current Stardust sends `ASCII "Core3.0"` followed by `U32 0`, so no leaf is reachable from the current server. The client nevertheless has a complete decoder and publishes the resulting catalog through `0x004CF5D0` to auction/filter consumers.

## Replacement consequence

A replacement should implement the hierarchy even though the current server sends it empty, because it is part of the original client contract and may be activated later. IDs and the leaf value remain fixed-width U32 values on x64. The two text fields must use the Archive ASCII codec and should be retained as a localization table/key pair; do not flatten them into display text during wire decode.

## Confidence

- Opcode, listener, decoder and apply route: **Confirmed by executable code**.
- Nested counts, keys and two-ASCII-plus-U32 leaf grammar: **Confirmed by executable code**.
- Localization table/key interpretation: **Strongly inferred** from the pair representation and auction UI use.
- Final leaf U32 historical label: **Unknown, preserve verbatim**.
- Current empty Stardust response: **Confirmed by server source**.

## Evidence

- listener `0x006A85BC..0x006A87D5`
- custom Archive variable vtable `0x015E126C`, decoder `0x006A8D00`
- nested hierarchy decoder `0x006A9030`
- leaf decoder/encoder `0x012B31A0` / `0x012B33C0`
- applicator `0x004CF5D0`
- `CommoditiesItemTypeListResponse.h`
