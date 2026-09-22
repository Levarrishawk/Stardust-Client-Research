# CuiConsent boundary

Date: 2026-09-22

## Result

`CuiConsent` is the client's reusable yes/no modal and callback carrier. It is
not evidence for a dedicated network message and must not be conflated with the
server's persistent `/consent`, `/unconsent`, and `/haveconsent` access-list
commands.

## Executable evidence

| Address | Provisional name | Evidence |
|---|---|---|
| `0x00A0B590` | `cui_consent_construct_VA_00A0B590` | Constructs mediator `CuiConsent`; requires code/text data object plus `buttonYes` and `buttonNo`; allocates callback/state storage. |
| `0x00A0B7F0` | `cui_consent_destroy_VA_00A0B7F0` | Runs the concrete destructor and optional object free. |
| `0x00A0B8E0` | `cui_consent_activate_VA_00A0B8E0` | Registers the mediator callback/listener state. |
| `0x00A0B920` | `cui_consent_deactivate_VA_00A0B920` | Unregisters the mediator callback/listener state. |
| `0x00A0B960` | `cui_consent_complete_VA_00A0B960` | Resolves retained callback state, sends only when that callback constructed a message, and closes the mediator. |

The class has two RTTI/vtable views at `0x015E940C` and `0x015E9490`. The
constructor stores the concrete vtables, resolves the UI children, and retains
callback data rather than constructing any fixed wire object. This matches other
client features which invoke a consent window and then independently serialize
their own response (for example the already-mapped group space invite flow).

A binary string/xref search found no `cuiConsentResponse` command literal in
`Stardust.exe`.

## Server correlation

`CuiConsentResponseCommand.h` currently logs that it was called and returns
success; it does not implement a corresponding modal protocol. The ordinary
`ConsentCommand`, `UnconsentCommand`, and `HaveConsentCommand` paths instead
operate on server-owned persistent consent data. Their shared English word is
not a shared message schema.

## Replacement-client rule

Implement a generic asynchronous confirmation service whose result is returned
to the feature that opened it. Each feature owns its response codec and timeout
policy. Implement persistent consent-list commands separately through the normal
command queue. Do not create a speculative `CuiConsentResponse` packet.

## Confidence

- Reusable modal/callback role: **Confirmed**.
- Absence of a fixed modal response packet in this client: **Strongly supported**.
- Persistent server consent-list separation: **Confirmed**.
- Exact Boolean/state interpretation inside every caller callback: **Caller-specific; not claimed here**.
