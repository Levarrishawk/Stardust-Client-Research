# Object radial-menu protocol and cache

Scope: `Stardust.exe` client static analysis correlated with the current Stardust `ObjectMenuRequest`, `ObjectMenuResponse`, `ObjectMenuSelect`, `RadialManagerImplementation`, and menu component sources.

## Protocol overview

Object menus use two ObjectController inner messages followed by one ordinary named selection message:

```text
client local/default menu
  -> ObjController inner 0x146 ObjectMenuRequest
  -> server merges authoritative entries
  -> ObjController inner 0x147 ObjectMenuResponse
  -> client counter/cache validation and UI
  -> ObjectMenuSelectMessage 0x7CA18726
  -> server object/component handler
```

## Shared request/response payload

The client registers inner types `0x146` and `0x147` at `0x00AF31D0`. Both use the same `MessageQueueObjectMenuRequest` implementation. Constructor `0x00AF3220`, serializer `0x00AF3510`, and parser/factory `0x00AF35F0` establish this exact nested payload after the outer ObjController scalar:

1. `LE64 targetObjectId`
2. `LE64 playerObjectId`
3. `LE32 itemCount`
4. repeated `itemCount` records:
   - `U8 itemIndex`
   - `U8 parentItemIndex`
   - `U8 radialId`
   - `U8 callbackType`
   - `Unicode16 label`
5. `U8 requestCounter`

The client runtime expands each wire item into a 44-byte record; that is an internal 32-bit layout and must not become a wire-layout assumption in an x64 replacement.

The outbound request path in `CuiRadialMenuManager` is `0x009685B0`, with construction/send at `0x00968D84`-`0x00968DDD`. It resolves the local player ID, serializes the target ID and current client/default entries, uses the current 8-bit counter, and queues inner `0x146` with controller flags/priority `0x23`.

The current server parses the same fields, converts every supplied client record into the response tree, forces those echoed records to callback type `3`, asks the selected object's `fillObjectMenuResponse` chain to add authoritative entries, flattens the tree depth-first, patches the count, appends the same counter, and returns inner `0x147` under ObjController priority `0x0B`.

## Client cache and stale-response behavior

`cui_radial_menu_update_cache_VA_00968250` looks up cache state by target NetworkId. It compares the response/request counter against the desired cache counter and logs/ignores a stale older response. It merges returned records by radial ID, replacing the four control bytes, Unicode label, and auxiliary client command data while retaining the target's cached context. Missing cache data is diagnosed and not fabricated.

`cui_radial_menu_request_or_build_VA_009685B0` combines locally derivable radial entries with the asynchronous server request and marks cache state pending. `cui_radial_menu_update_and_retry_VA_009698C0` advances a manager time accumulator, walks pending requests, and when the configured deadline is exceeded logs the timeout and resends inner `0x146` with the original target/menu/counter. Entries are removed when their target no longer resolves or they no longer require retry.

The request counter is therefore a modulo-256 cache generation token, not a packet sequence number and not SOE transport reliability state.

## Selection

`ObjectMenuSelectMessage` constructor `0x00AF3A30` registers exactly:

- `LE64 targetObjectId`
- `U8 radialId`

Client selection paths at `0x0096B5D0` and `0x0096BDB0` call this constructor and reliably send the named message. The current server registers hash/opcode `0x7CA18726`, resolves the target, applies building/root-parent access checks, calls the object's menu component/virtual `handleObjectMenuSelect(player, radialId)`, then publishes `OBJECTRADIALUSED` to observers. Callback type `1` is the default/client-handled class; callback type `3` invokes a server callback. The client contains substantial local dispatch for default actions, while server-callback selections take the direct message path.

## Security and replacement rules

- Never trust menu entries or selection state as authorization. Stardust resolves and validates the target and its current component state again.
- Keep `itemIndex` and `parentItemIndex` distinct from `radialId`: parentage is expressed by flattened response item indices, while selection sends the semantic radial ID.
- Preserve labels as UTF-16-length-prefixed Unicode on the wire; local `@table:key` strings are resolved by presentation/localization.
- Preserve the U8 generation token and stale-response rejection, but implement cache timeouts independently of the transport retransmission layer.
- Use internal x64-native menu objects; serialize only the fixed legacy widths above.

## Confidence

- Inner type IDs, field order/widths, record flattening, request/response counter, selection schema/opcode and server endpoint: **Confirmed by code**.
- Cache merge, stale rejection and timeout resend: **Confirmed principal behavior**; exact historical timeout constant label is not needed for the protocol contract.
- Complete catalog of every local radial action: outside this block; those actions are tracked as gameplay/UI breadth, not a schema uncertainty.
