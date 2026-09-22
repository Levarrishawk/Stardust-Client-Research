# Replication Decode Failure Boundary

The inspected receive-to-replication chain contains cleanup exception metadata
but no local recovery handler for an Archive bounds exception. A malformed
baseline/delta is not safely isolated at message or object level by this path.

```text
network queue drain 0x012B3B80
  -> connection receive 0x012B79B0
  -> connection/event dispatch 0x005A8C50 and typed listeners
  -> Baselines/Deltas parser
  -> baseline 0x00554CF0 or delta 0x00554E90
  -> view/field codec -> checked read 0x0118B030
```

The checked reader verifies bounds and throws `read beyond end of buffer`. View
dispatchers establish MSVC exception frames only to release temporary archive
streams; they have no catch/report/continue branch. Connection dispatch and
queue drain likewise contain no decoder recovery. `ExceptionList` setup in the
decompilation is destructor unwinding, not evidence of a catch.

## Replacement policy

Decode from bounded immutable views, construct mutations in temporary state,
and commit only after complete count/index/opcode/payload validation. On error,
log opcode/object/tag/view/index and offset without private content, reject the
message, and normally disconnect or resynchronize rather than keep a partially
mutated graph. This is hardening, not a wire change.

The checked-read throw and absence of local catch through queue dispatch are
**Confirmed by code**. The eventual process-level outcome under every exception
configuration is **Unknown** and irrelevant to the transactional requirement.

