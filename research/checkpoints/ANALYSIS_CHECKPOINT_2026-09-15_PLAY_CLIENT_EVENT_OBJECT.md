# Stardust.exe Analysis Checkpoint: PlayClientEventObjectMessage

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

`PlayClientEventObjectMessage` (`0xAF83C3F2`) is a dormant server definition unsupported by this exact `Stardust.exe`. Its message name, RTTI, opcode literal, descriptor and listener are all absent, and the current Stardust tree has no constructor call site. It must not be confused with the active, already-mapped `PlayClientEffectObjectMessage` path.

## Confirmed wire contract

The server constructor emits operand count five followed by:

1. ASCII16 `file`;
2. ASCII16 `aux`;
3. LE64 target object ID.

Field names suggest an appearance/client-event resource plus auxiliary or hardpoint selector, but without a client consumer or active server producer the semantic interpretation is only tentative.

## Distinction from active client effects

`PlayClientEffectObjectMessage` is positively registered and resolves an object before creating/attaching a named effect. The event message uses another opcode and has no corresponding client class in this image. A replacement must not alias the two merely because their payload shapes are similar.

## Replacement requirements

- Do not require this message for the networking POC or current Stardust interoperability.
- Reserve opcode `0xAF83C3F2` and preserve its exact byte schema in the protocol catalogue.
- If later enabled by server content or another client revision, resolve the target object safely and route event identifiers only through a data-driven whitelist/appearance-event registry.
- Never treat `file` as an unrestricted native filesystem path.
- Keep object IDs U64 on the wire irrespective of x64 object representation.

## Confidence

- **Confirmed:** server bytes, absence from executable and adjacent binaries, absence of active server producer.
- **Tentative:** `file` names a client event definition and `aux` selects a hardpoint/auxiliary event target.
- **Unknown:** original consumer semantics, valid resource type and historical producer.

## Progress

One reachable packet moves to documented: **254 documented / 91 reachable gaps / 25 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
