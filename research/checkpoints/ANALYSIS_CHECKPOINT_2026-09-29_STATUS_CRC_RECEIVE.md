# Status CRC receive rule: no opcode-08 exception

Date: 2026-09-29, America/New_York. Question: does the documented client baseline
accept CRC-disabled `00 08` responses through an opcode-specific exception?
Stopping condition: verify entry, bypass discriminators, CRC comparison,
mismatch exits and downstream transforms/dispatch directly from instructions;
exercise matching replacement acceptance/rejection fixtures.

## Input identity and tools

Executable `C:\Stardust-DEV\Stardust.exe` SHA-256 was checked before address use:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Addresses below are baseline VAs. Existing `tools/python/disasm_range.py` uses
private installed pefile/Capstone packages. New reusable verifier
`tools/python/verify_status_receive_policy.py` checks identity and 18 selected
instruction guards; it reads the PE without executing it. Package absence is a
reproduction prerequisite, not permission to claim verification by inspection.

Server correlation remains HEAD `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`,
as recorded in the [preceding checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-29_STATUS_BOUNDARY.md).
No original client execution, network activity, captures, commits or publication.

## Direct observations

| VA / condition | Observed control flow | Confidence |
|---|---|---|
| `0x012C376A`–`0x012C3773` | Leading-zero opcode `06` returns before normal CRC path | Confirmed; existing ping behavior |
| `0x012C383E`–`0x012C3864` | Leading-zero opcodes `01`, `02`, `1D`, `1E` jump to downstream dispatch at `0x012C3A2F`; `08` is absent | Confirmed |
| `0x012C386A`–`0x012C3881` | Requires connection state at `+0x30`; reads trailer width `+0x120`; width <= 0 jumps past CRC checking | Confirmed; configuration-level skip, not opcode exception |
| `0x012C3887`–`0x012C38B0` | Rejects insufficient trailer length; computes CRC over bytes before trailer using seed `+0x11C`, calling `0x012C8D30` | Confirmed |
| `0x012C38BE`–`0x012C3927` | Widths 1–4 decode/mask the big-endian trailer, compare against computed CRC; equality enters normal path | Confirmed |
| `0x012C3929`–`0x012C398E` | Mismatch increments accounting, optionally notifies callback slot `+0x0C`, then returns; absent callback also returns | Confirmed; callback internals not investigated here |
| `0x012C3991`–`0x012C3A33` | Successful validation removes trailer length, applies configured inverse callbacks, then calls `0x012C3B60` | Confirmed |

**Interpretation:** with the deployed positive two-byte trailer width, standalone
opcode `00 08` has no CRC exception in this receive function. The server's
zero-trailer response therefore fails unless its computed truncated CRC happens
to equal zero. A zero trailer is not intrinsically invalid: equality remains
the rule. This conclusion covers this entry path and baseline; it is not a
claim about every configuration, future server build, callback side effect,
or embedded status packet inside a CRC-validated outer packet.

**Implementation consequence:** retain the replacement's normal CRC gate.
Do not add a zero-trailer fallback. Server construction and client receive
behavior are contradictory compatibility evidence, not grounds to rewrite
client behavior. Original ping ordering differs from the replacement's current
protected-ping policy; no ping change is included here.

## Fixtures and verification

The previous independent Python server-model fixture has a zero-trailer
43-byte response and is rejected. Repairing only its trailer with a computed
CRC makes the same body accepted and observable. Single-bit corruption at each
transformed body/marker/trailer byte is rejected without changing the prior
status observation. No status reply, ACK or application delivery is generated.

```powershell
python tools/python/verify_status_receive_policy.py C:\Stardust-DEV\Stardust.exe
python tools/python/verify_status_vectors.py
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj
python tools/python/validate_organized_workspace.py .
git diff --check
```

Direct verifier, independent framing model and full smoke suite pass. Workspace
validation after consolidation: 704 Markdown files, zero broken/private links,
2,585 provisional rows with zero duplicates and 345 packet rows. Diff whitespace
check passes; prior dirty work is preserved, with nothing staged or committed.
No registry row or semantic name is added; the
existing receive function entry is refined. Research scores remain unchanged.

## Exact next probe

Recover the full status request/reply wire fields at `0x012C3FF7`, their
construction/counter ownership and send preparation at `0x012C5980`. Determine
how the minimal four-byte server request should be handled safely by a replacement
before automatic replies. Periodic probes, matched RTT observations, idle expiry
and adaptive retry remain unimplemented. Live evidence for the source/client
status mismatch would require separately authorized bounded network work.
