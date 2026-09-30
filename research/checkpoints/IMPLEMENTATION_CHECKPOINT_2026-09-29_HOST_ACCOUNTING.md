# Host accounting and monotonic clock boundary

Date: 2026-09-29. Question: can the host count actual I/O attempts independently
of generated protocol messages and create a pre-send status snapshot without
wall-clock dependencies? Stop at engine-neutral clock/accounting components
and deterministic positive/negative tests.

Evidence: the [status reply checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-29_STATUS_REPLY.md)
records baseline input identity and direct receive-before-validation and
send-after-raw-call accounting. No additional VA claim is introduced here.
Replacement policy is explicit: count send attempts at the I/O boundary,
including retries and failed attempts; count pinned-peer receives before framing
validation; track foreign-source drops separately. Generation alone is not a send.
Failure counts remain separate from attempt counts. These are replacement host
metrics, not proof of exact legacy failed-socket accounting.

`Hosting/DatagramAccounting` uses checked 64-bit values and atomically replaces
an immutable counters record. Byte lengths are bounded by 65,507. Status snapshots
use send attempts/peer receives, preserve low-32 timestamp wrap and exclude their
own subsequent send. `Clear` resets host-owned metrics. No reliable/application
counts, research denominators, endpoint strings or credentials enter this record.

`StopwatchMonotonicClock` supplies process-local elapsed milliseconds.
`MonotonicClockGuard` accepts equal samples and rejects negative/backwards samples
before committing its last observation. Wall-clock epoch is absent. Host code
must sample the guard before protocol mutation and must own the accounting calls.
Both components are single-owner; they are not shared concurrent services.

Validation: `HostAccountingScenarios` covers retries/failed sends, damaged peer
receive accounting, foreign drops, immutable pre-send snapshots, timestamp wrap,
invalid lengths/no mutation, negative/backwards/equal clock values and reset.
Full core smoke suite passes under .NET 8 in a 64-bit process. Tests are our own
offline code; no sockets, executable execution, server contact or publication.

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj
```

Next: bind these components to a single-owner cancellable login host with pinned
peer identity, injected datagram I/O and deterministic completion/failure cleanup.
Session closeout will consolidate workspace/link/diff verification. This checkpoint
does not implement that host, persistent journal, status scheduling or UDP sockets.
