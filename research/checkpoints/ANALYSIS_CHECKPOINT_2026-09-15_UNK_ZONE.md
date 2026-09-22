# Stardust.exe Analysis Checkpoint: `unkZone` Test Packet

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

The provisionally named server class `unkZone` (`0x58C07F21`) is a dormant test artifact, not a recovered client subsystem. `Stardust.exe` contains no opcode literal, message name, RTTI, descriptor or listener for it; adjacent binaries also provide no identity. The source labels the file `//test`, hard-codes all four payload bytes and has no constructor call site.

## Wire bytes

After operand count two and opcode `0x58C07F21`, the constructor writes exactly:

```text
44 05 00 00
```

Those bytes could be viewed as LE32 `0x00000544`, but the source deliberately emits four individual bytes. No semantic field type is asserted.

## Replacement requirements

- Exclude this packet from required login, zone transition and networking POC behavior.
- Reserve the opcode in the compatibility catalogue and log/ignore it safely if encountered.
- Do not name it as a zone-state operation or decode the constant without new evidence.
- Preserve the literal four bytes only for an optional legacy/test compatibility fixture.

## Confidence

- **Confirmed:** exact bytes, test annotation, no current server producer and no executable consumer.
- **Unknown:** historical experiment or intended meaning of `0x00000544`.

## Progress

One reachable packet moves to documented: **258 documented / 87 reachable gaps / 24 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
