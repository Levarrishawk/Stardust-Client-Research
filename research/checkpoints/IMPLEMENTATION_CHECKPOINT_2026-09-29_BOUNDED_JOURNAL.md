# Bounded persisted diagnostic journal

Date: 2026-09-29. Question: can typed diagnostic events be persisted with fixed
resource bounds, no raw private values, and I/O failures isolated from login?
Stop at the stream/file sink, negative tests and injected host failure integration.

Evidence: existing allowlisted `LoginDiagnostics.Encode`, strict configuration,
and [login host](IMPLEMENTATION_CHECKPOINT_2026-09-29_LOGIN_HOST.md). This adds
replacement persistence policy, not original-client logging behavior.

`BoundedLoginJournal` writes six-key UTF-8 JSONL records from typed events only:
kind, state, outcome, datagram bytes, control count and application count.
Defaults: 262,144 bytes / 4,096 events. Limits are checked before writing a
whole record. Dropped-event and I/O-failure counters are visible without storing
exception text. Writes/flush/dispose are serialized. A partial failed write marks
an uncertain final tail and permanently stops further records; the caller may
read complete preceding lines but must not treat the tail as a valid event.

`CreateNew` takes an explicit path, validates limits before opening and refuses
to overwrite. No directory discovery, implicit destination or private payload
argument exists. Seekable injected streams must start empty. Caller controls
stream ownership with `leaveOpen`; sink failures cannot change protocol state.
The synchronous sink can add storage latency; it is intended for a bounded local
journal, not an untrusted blocking remote stream. Durable fsync/power-loss
guarantees, rotation and an async spool are outside this checkpoint.

Validation uses synthetic events/credentials and faulting streams: exact JSONL,
byte/event caps without split records, tiny cap/no write, partial-write stop,
flush failure, empty-stream requirement, persisted temporary-file round trip,
existing-file no-overwrite and host I/O failure classification without original
exception/endpoint/username/password strings. The temporary file is deleted.
Every emitted record's keys are checked against the exact allowlist. Core smoke
suite passes; prototype host's offline host proof writes 18 safe events to an injected
memory journal with zero failures.

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj
```

No original binary/asset bytes are persisted by this sink. No sockets or server
contact were used; no publication or research-score/registry change. Session
closeout consolidates workspace/link/diff validation. Next: a caller explicitly
opens an ignored local destination for an authorized operational run; UI may
surface dropped-event/failure counts without exposing raw exception messages.
