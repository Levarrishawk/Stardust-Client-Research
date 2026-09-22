# Biography Request and Edit Workflow

Date: 2026-09-22

## Result

The biography workflow is behaviorally closed across the original client and
the current Stardust server. It is a command-queue request/edit feature backed
by one server-to-client object-controller response. It does not require a new
replication view or a separate network service.

## Client evidence

| Address | Provisional name | Evidence | Confidence |
|---|---|---|---|
| `0x0065F6B0` | `client_request_biography_VA_0065F6B0` | lazily resolves the exact command name `requestBiography`, validates the subject, suppresses duplicate self requests, and submits through the common command queue | Confirmed |
| `0x0065F750` | `client_set_biography_VA_0065F750` | compares UTF-16 text with the retained biography and submits the exact command `setBiography` only when changed | Confirmed |
| `0x00BE1950` | `swg_cui_biography_edit_construct_VA_00BE1950` | constructs `SwgCuiBiographyEdit`, binds biography/name/status text and save/cancel controls, and subscribes to `PlayerCreatureController::BiographyRetrieved` | Confirmed |
| `0x00BE1E60` | `swg_cui_biography_edit_button_handler_VA_00BE1E60` | validates the edit target and changed text, queues the save, clears pending state, and closes the mediator | Strongly inferred |

The typed event payload is a pair containing a `NetworkId` and a player-object
reference. This establishes the boundary between controller decoding and UI:
the player creature controller consumes the reply and publishes
`BiographyRetrieved`; the mediator does not parse network bytes itself.

## Wire and server correlation

The current server constructs:

```text
ObjectControllerMessage(target=requesting player, flags=0x1B,
                        inner subtype=0x1DB, reliable=true)
    U64 subject player object ID
    Unicode biography
```

`RequestBiographyCommand` validates the target as a player creature, obtains
its `PlayerObject`, and sends the response only to the requester.
`SetBiographyCommand` validates the caller as a player creature and writes the
Unicode command argument with `PlayerObject::setBiography`.

## End-to-end mapping

```text
Biography UI/open action
  -> requestBiography queued command (target = subject creature ID)
  -> RequestBiographyCommand
  -> ObjController inner 0x1DB {U64 subject, Unicode biography}
  -> player-controller decoder
  -> BiographyRetrieved(NetworkId, PlayerObject*)
  -> SwgCuiBiographyEdit populates name/text/status

Save changed text
  -> setBiography queued command {Unicode biography}
  -> SetBiographyCommand
  -> PlayerObject::setBiography
```

## Replacement-client requirement

Implement the two command names with the normal command-queue envelope,
decode controller subtype `0x1DB` as `U64 + Unicode`, cache the text against
the subject object ID, and surface a typed domain event. The UI must not own
the network decoder. Object IDs remain 64-bit on the wire even though the
replacement process is natively 64-bit.

## Residual uncertainty

The exact anonymous address of the small player-controller `0x1DB` decode
branch has not yet been isolated. This is an address-label gap, not a protocol
or behavior gap: the active server producer fixes the byte layout and the
client's typed subscriber fixes the resulting domain event.
