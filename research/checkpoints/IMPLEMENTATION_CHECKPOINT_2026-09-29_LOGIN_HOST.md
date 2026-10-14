# Cancellable login host and explicit UDP adapter

Date: 2026-09-29. Question: can one owner complete login with injected async I/O,
while rejecting unrelated senders, keeping deadlines live during a pending
receive, and cleaning up all owned buffers on completion/failure/cancellation?
Stop at deterministic host scenarios, a compiled concrete adapter and a prototype host
headless offline run. Actual socket execution is outside this checkpoint.

## Inputs and policy

Use the existing [login specification](../../docs/LOGIN_AND_ZONE_STATE_MACHINE.md),
[status/accounting evidence](IMPLEMENTATION_CHECKPOINT_2026-09-29_STATUS_REPLY.md)
and [clock/accounting component](IMPLEMENTATION_CHECKPOINT_2026-09-29_HOST_ACCOUNTING.md).
No new original-executable address claim is made. Fixed login bodies and peer
addresses are synthetic. Original peer fixtures supply independent CRC/XOR
encoding; the prototype host proof is integration evidence, not an independent wire oracle.

Replacement host policy: one run, one outstanding receive, serial protocol
mutation, exact immutable IPv4 address/port pinning, configurable local deadlines
and 25-ms default polling. Timers do not cancel/restart an outstanding receive.
Foreign datagrams are counted separately and filtered before protocol handling;
peer datagrams count before CRC validation. Bad handshake responses are discarded
while handshake retry/deadline remains active. Application parse/fatal/timeout,
I/O, invalid clock and cancellation outcomes close the owner deterministically.
This is implementation policy, not recovered thread/scheduling identity.

## Implementation

- `Hosting/DatagramPort.cs`: immutable numeric IPv4 identity and injected I/O/tick
  contracts. Send borrows bytes through completion; receive transfers an owned
  byte array. Ports must honor cancellation and dispose deterministically.
- `LoginDatagramHost`: single owning run, automatic authentication after valid
  negotiation, all reliability controls sent, retries polled during idle receive,
  guarded monotonic samples, actual send-attempt accounting. Concurrent second
  runs are rejected; lifecycle/disposal is synchronized.
- `LoginConnectionResult`: copies one completed credential and immutable list
  containers to the caller, then the host closes its session/port. Result disposal
  zeros its credential; caller-owned copies and immutable input strings cannot
  be erased by host cleanup. Safe summaries omit identities/endpoints.
- Failure exceptions retain only a typed category; socket/endpoint/error text and
  inner exceptions are not propagated. Diagnostics include allowlisted host
  attempts, foreign drops and terminal outcome. Cleanup failures are counted.
- `UdpDatagramPort`: explicit construction binds IPv4 locally, borrowed-memory
  async send, bounded receive, truncation rejection and receive/disposal guards.
  No DNS, endpoint discovery or connection is implicit. **Compiled only**: no
  UDP instance was constructed or executed in this session.
- `OfflineHostedLoginScenario` drives the same host using an async fixture port;
  Prototype host `_Ready` awaits it and catches startup exceptions with type-only output.
  Prototype host still runs offline by default and creates no socket.

## Validation and limitations

`LoginHostScenarios` completes wrong-IP/wrong-port, malformed handshake,
CRC-damaged token and reordered enumeration fixtures; result counters are six
sends, seven pinned receives and two foreign drops. Owned receive/send buffers
are zero after exit. It covers blocked-receive cancellation, exact handshake
retry, handshake/partial-login deadlines, fatal/malformed application failure,
sanitized send failure and invalid-clock-before-send. At most one receive is
active, including ticks during a blocked receive.

Follow-up lifecycle cases also pass: blocked-send cancellation; receive plus
dispose failure with the primary typed outcome preserved; unused/idempotent
disposal; invalid ASCII credentials before I/O; concurrent second-run rejection;
and a protected matching peer disconnect. Cleanup failures are counted separately.

Core and prototype host build with zero warnings/errors. The complete smoke suite passes.
Prototype host headless exits 0 with six sends, seven peer receives, one foreign drop and
18 safe journal events. Its pre-existing sandbox root-certificate-store error
still prints; this does not establish an error-free startup.

> Historical prototype-specific command or code example omitted from the research-only documentation.


Full end-to-end socket behavior remains unverified. No server contact, capture,
original execution, commits or publication. Research scores and registries are
unchanged. Workspace/link/diff closeout is consolidated in the session report.

Next: recover/select explicit short-status policy before automatic liveness;
bind a separately authorized numeric peer/account in a bounded real-I/O test.
The existing offline host is ready for further fixture-backed lifecycle probes.
