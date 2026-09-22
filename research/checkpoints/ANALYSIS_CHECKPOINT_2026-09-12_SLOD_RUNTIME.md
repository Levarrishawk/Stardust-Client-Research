# Analysis checkpoint: SLOD runtime contract

Scope is verified `Stardust.exe` SHA-256
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`
and the effective configured archive overlay.

## Confirmed grammar and ownership

`0x00829EB0` registers `SLOD` with factory `0x00829FA0`. Constructor
`0x0082A000` accepts only `0000`; loader `0x0082A1D0` reads:

```text
FORM SLOD
  FORM 0000
    CHUNK INFO
      int16 skeletonCount       // negative clamps to zero
    <inline skeleton template> × skeletonCount
```

Each child is parsed by common skeleton-template factory `0x0082CF60`, so it
is a typed inline IFF object rather than a NAME path. SLOD owns its child vector
at `+0x18..+0x20`; destruction invokes `0x00832290` on each child and frees the
vector.

All 157 effective SLOD files contain SKTM/0002 children:

| Child count | Files |
|---:|---:|
| 1 | 9 |
| 2 | 2 |
| 3 | 20 |
| 4 | 93 |
| 5 | 21 |
| 6 | 9 |
| 7 | 3 |

## Confirmed runtime surface

The SLOD vtable at `0x015E5E8C` begins:

| Slot | Function | Contract |
|---:|---:|---|
| `+0x00` | `0x00829F30` | returns `min(childCount, 4)` |
| `+0x04` | `0x00829F60` | returns indexed child after acquiring a reference through `0x00832280` |
| `+0x08` | `0x0082A0F0` | deleting destructor |

The disk container can therefore exceed the four levels exposed by this
client. `ClientSkeletalAnimation.skipL0` is shared: when enabled and more than
one child exists, loader index zero is skipped and the client emits
`LodSkeletonTemplate: skipping L0`. The loader reserves from the serialized
count but appends children, so this removes L0 and shifts later children down;
it does not create a null hole. The exposed count becomes
`min(serializedCount - 1, 4)` in this mode.

Unlike MLOD, SLOD children are parsed eagerly and owned directly. It has no
name-based lazy cache. SAT LDTB supplies distance/hysteresis policy; SLOD stores
no thresholds.

## Replacement requirements

- Recursively dispatch inline children through the skeleton registry with
  bounded nesting/count checks.
- Retain all children while exposing the original four-level compatibility
  view.
- Preserve shared-reference API semantics without copying templates per fetch.
- Keep optional skip-L0 policy outside serialization.
- Treat distance selection as appearance policy, not part of SLOD.
