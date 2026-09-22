# Stardust.exe Analysis Checkpoint: Client Inactivity Signaling

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

`ClientInactivityMessage` is a client-to-zone-server, one-byte activity-state signal. `Stardust.exe` constructs it at `0x0055E110`; Stardust registers callback opcode `0x0F5D5325`, parses one U8 flag and currently only logs the result. It does not mark the player link-dead, disconnect the session or alter gameplay state in this server implementation.

## Client production path

The executable's `InputActivityManager` installs through `0x0055E050`, reads configuration key `ClientGame/pAinactivityTimerMinutes`, converts minutes for the timer scheduler and registers callback `0x0055E110`. Removal at `0x0055E0F0` cancels the callback and clears installed state.

Constructor/sender `0x0055E110`:

1. requires an active network object (`0x00462C00`);
2. constructs named message `ClientInactivityMessage`;
3. adds one U8 field from its function argument;
4. sends through `0x00462670` with reliability argument one;
5. returns whether a network path existed.

An additional caller in the main input/activity update path (`0x00425E80`, call at `0x00426156`) supplies the current activity-manager Boolean from `0x00AB5810`, showing that state transitions as well as the configured timer can reach the same sender.

## Wire/server contract

| Direction | Opcode | Payload | Server action |
|---|---:|---|---|
| C→Z | `0x0F5D5325` | U8 inactivity/activity flag | log `ClientInactivityMessage with flag ... received` |

Follow-up client-state tracing confirms polarity: input events set/reset the state to `0` and clear elapsed time; expiry of the configured inactivity threshold sets it to `1`. See `ANALYSIS_CHECKPOINT_2026-09-21_ACTIVITY_AND_PERMISSION_SECONDARY_LIST.md`.

## Replacement requirements

- Preserve opcode `0x0F5D5325`, one-byte payload and reliable delivery if implementing parity.
- Source state from platform/input activity tracking, not gameplay simulation.
- Emit on state transition and/or configured inactivity timeout without frame-by-frame spam.
- Do not treat this message as a transport heartbeat; the SOE heartbeat/link-dead mechanism is separate.
- The networking POC may omit it because Stardust has no behavioral dependency beyond logging.
- Keep timer configuration duration explicit and avoid 32-bit millisecond overflow in the x64 implementation.

## Confidence

- **Confirmed:** executable name, constructor/sender, one-byte field, reliable flag, network guard, configuration key/timer installation, update caller, server opcode/parser/log-only behavior.
- **Confirmed:** U8 `0` means active/resumed and U8 `1` means inactive timeout reached. Historical server policy remains irrelevant to current Stardust's log-only handler.

## Progress

One reachable callback moves to documented: **255 documented / 90 reachable gaps / 25 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
