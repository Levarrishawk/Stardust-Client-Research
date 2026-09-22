# TCP Framing and Configuration Precedence — 2026-09-11

## Scope

Static analysis of `C:\Stardust-DEV\Stardust.exe` only, hash `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, plus read-only inspection of installed CFG files and Stardust server source. No executable or network service was contacted.

## TCP wire envelope

`tcp_frame_queue_VA_012BE8F0` appends a four-byte payload length followed by the payload bytes to the per-connection send buffer. The integer is copied directly from native memory, so this x86 build emits a little-endian unsigned 32-bit length. When queued bytes reach `SharedNetwork.tcpMinimumFrame` (default 1000), it immediately submits an overlapped `WSASend`; otherwise the shared pump later flushes pending bytes.

Receive completion `0x012BE7F0` immediately posts the next overlapped `WSARecv`, then forwards completed bytes into `tcp_receive_frame_reassembler_VA_012B7840`. That routine accumulates four header bytes, reads the little-endian length, accumulates exactly that payload length across receives, and forwards the completed payload to `received_frame_dispatch_VA_012B4120`. Multiple complete frames in one receive are processed serially.

No explicit maximum frame-length validation was found between decoding the U32 length and growing the accumulation buffer. A malicious length may therefore cause excessive allocation or arithmetic/state failure. This is a confirmed legacy validation gap; the replacement must impose a configured hard cap before allocation.

## TCP lifecycle

The stream object uses IOCP operation tag `2` for send and `3` for receive. `0x012BE990` drains completions nonblocking, continues after send completions and routes successful nonempty receive completions to the reassembler. It separately polls `FD_CLOSE`; successful close and socket-error paths converge on teardown/callback logic. `TCP_NODELAY` is enabled.

## Configuration precedence

`app_entry_VA_00401050` chooses `swgemu.cfg` (or `test.cfg` only when the executable path contains `testswgclient`) and supplies it to `SetupSharedFoundation::install`/`0x00A9F970`. The configuration map is initialized, the selected root file is loaded, and only afterward is the retained command line processed.

`swgemu.cfg` includes, in order:

1. `swgemu_login.cfg`
2. `swgemu_live.cfg`
3. `swgemu_preload.cfg`
4. `options.cfg`
5. `user.cfg`

The parser stores repeated values under a key. Scalar `ConfigFile::getKeyInt/getKeyBool` obtains the value count and requests index `count-1`, proving that the last definition wins. Command-line `section.key=value` entries are parsed after the root file and append another value, so they override included files. Command-line `@filename` also loads another CFG at that later point; `-s section` changes the active section for following assignments.

None of the five installed include files defines `SharedNetwork.useTcp`. Therefore, absent an external launch argument not available in static filesystem evidence, its effective configured value is the executable default `true`.

## Client/server discrepancy

The TCP path is fully functional and frames application messages directly; it does not wrap them in the UDP SOE session/reliability envelope. The inspected Stardust login and zone implementations instantiate `DatagramServiceThread`. Static evidence therefore cannot yet explain a successful direct TCP connection to those services. Plausible explanations remain an external TCP bridge/front-end or a launch-time `SharedNetwork.useTcp=false` override. These remain hypotheses, not findings.

## Progress

Comprehensive whole-client coverage advances from **39% to 40%**. Configuration/startup coverage moves to 70% and transport coverage to 80%.

## Next unit

Trace the TCP wrapper callbacks into common connection open/message/close notifications and audit the complete login → cluster selection → ConnectionServer → zone transition against the UDP and TCP alternatives. Preserve the topology discrepancy as an explicit branch until external deployment evidence exists.
