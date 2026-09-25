# Resource-container replication scenario fixture

Date: 2026-09-25

## Scenario closure

The derived `RCNO` family now has a reproducible scenario fixture connecting
current server bytes to the previously recovered presentation effect.

Current `ResourceContainerObjectMessage6` makes seven serializer calls while
declaring five fields. `tools/python/verify_rcno_view6_fixture.py` proves the
actual byte alignment against the executable registry:

| RCNO:6 index | Bytes consumed | State/effect |
|---:|---|---|
| inherited 0 | first four zero bytes | common U32 |
| inherited 1 | next eight zero bytes | empty Tangible defender vector count/counter |
| 2 | U32 | maximum stack size |
| 3 | ASCII16 | resource spawn/type identifier |
| 4 | Unicode32/UTF-16LE | display resource name |

The synthetic fixture uses max stack 100,000, type `flora_resources`, and name
`Synthetic Flora`; all seven assertions pass. It demonstrates why counting
server `insert*()` calls or using the declared operand count as a field count
would misalign this live baseline.

View 3 adds quantity U32 at index 11 and resource ID U64 at index 12 after the
inherited Tangible fields. The current server emits only the valid quantity
delta at index 11. Its commented resource-ID index 14 and view-6 setters at
indices 5/6 are outside the executable registry and must remain disabled.

## Event-to-presentation chain

```text
RCNO:6 type/name + RCNO:3 resource ID
  -> registered fields commit
  -> resource-ID callback 0x0070A9E0 marks +0x460 dirty
  -> object update/virtual 0x0070A570
  -> resource class icon resolver 0x004B5480
  -> appearance/ui_res_<class>.spr (fallback: inorganic)
  -> require SpriteAppearance / StaticShader / MAIN texture
  -> bind texture to object appearance tag REP0
  -> clear dirty flag
```

Missing resource-class icons fall back recursively to `inorganic`; malformed
appearance/shader/texture structure warns rather than inventing a texture.
Quantity changes update replicated inventory state but do not independently
select the icon. Generic object destruction owns appearance and callback-field
teardown, so no resource-container-specific asynchronous job survives destroy
or zone removal.

## Replacement contract

- Decode composed view fields by tag/view/index and legacy wire width.
- Commit a complete baseline before firing presentation callbacks.
- Resolve resource presentation through the original asset namespace and
  preserve the `inorganic` fallback.
- Keep quantity U32 and resource ID U64 on wire regardless of native x64 types.
- Reject/log out-of-range legacy delta indices 5, 6 and 14; never reinterpret
  them as adjacent memory.
- Release object-owned appearance and subscriptions on destroy/zone transfer.

## Confidence

Wire alignment, live quantity index, callback dirty flag, icon fallback and
REP0 texture application are **Confirmed** by current server source,
executable registries/callbacks and original-asset lookup code. The exact
historical semantic names of the inherited common U32 remain unnecessary.
