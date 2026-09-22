# UI Message Queue and Command-Parser Bridge (2026-09-22)

## Outcome

`CuiMessageQueueManager` is an internal UI/input adapter. Its `CommandParserRequest` transceiver is not an incoming network packet: it publishes an ASCII command string paired with a Boolean to UI subscribers such as `SwgCuiChatWindow`.

## Event contract

RTTI fixes the event payload as `pair<const basic_string<char>, bool>&` and the event as `CuiMessageQueueManager::Messages::CommandParserRequest`. Constructor `0x00905C70` creates the transceiver; `0x00905D30` is its ordinary cleanup. The chat window owns a typed subscription.

The manager's `ActionListener` vtable routes action activation to `0x00905480` and a message/drag-like translation path to `0x009055B0`.

## Action and input translation

`cui_message_queue_action_performed_VA_00905480` tests two registered UI action identities and delegates to separate command-request helpers. This is the bridge by which UI actions request command-parser/chat handling without directly invoking network send code.

`cui_message_queue_translate_input_item_VA_009055B0` recognizes retained UI payload types 2 and 4. It extracts ordinary input-map commands or macros, resolves an input-map command against the active map, warns with `No such input map command: %s` when absent, and formats the resolved binding/action into the output Unicode presentation. Macro handling is explicitly labeled by the embedded `Macro:` prefix. A second textual path normalizes `+` separators to spaces before presentation.

This manager therefore sits between drag/drop or action UI, the input-command registry, macro manager and chat/command-parser UI. It does not own the authoritative gameplay command queue; server-bound commands still pass through the already mapped command definitions and object-controller inner `0x116` path.

## Replacement-client consequences

- Represent command-parser requests as a typed internal event with an owned string and explicit Boolean policy.
- Keep UI actions, key-bind commands, macros and authoritative gameplay commands as separate concepts.
- Validate input-map lookups and surface a user-facing error; do not silently reinterpret unknown names.
- Never route this parser bridge to an operating-system shell. Server `ExecuteConsoleCommand` is likewise an internal game/UI command source.
- The proof-of-concept network client does not require this manager.

## Confidence

- Class/event identities, payload types and subscriber: **Confirmed by RTTI**.
- Input-map/macro extraction and missing-command warning: **Confirmed by executable code/strings**.
- Separation from authoritative object-controller command transport: **Confirmed by call boundaries and existing command-queue analysis**.
- Friendly names of the two registered UI action identities: **Not recovered; bounded UI naming**.

