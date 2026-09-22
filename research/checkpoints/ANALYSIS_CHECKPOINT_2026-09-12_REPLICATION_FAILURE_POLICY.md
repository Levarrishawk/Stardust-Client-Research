# Analysis Checkpoint: Replication Failure and Partial Commit

## Legacy behavior

The malformed baseline/delta behavior is sufficiently closed for architectural decisions:

1. Fixed-width reads pass through `byte_stream_read_checked_VA_0118B030`, which verifies the requested span and throws an Archive exception on overrun.
2. Delta decoder `autodelta_byte_stream_delta_decode_VA_0118AA00` reads and ignores the declared update count, then iterates until payload exhaustion.
3. Each delta item begins with a U16 field ordinal. The decoder indexes the local field registry without first checking that ordinal against registry size.
4. Field/container decoders mutate live object state in place. There is no staging snapshot or rollback.
5. Changed fields are queued in a deduplicating ordered set. `delta_view_dispatch_VA_00554E90` calls commit only after the decoder returns normally.
6. If a later field throws, earlier state mutation remains and the normal callback commit is skipped.
7. The received-message service catches the Archive exception, logs `Unhandled Archive read error: %s`, and rethrows. No higher application catch recovers; the exception reaches the MSVC CRT and terminates the process.

Because the process does not continue, there is no supported legacy “next message after partial failure” contract. The transient dirty set and partial state are observable only during unwinding/crash diagnostics.

## Confidence

- Bounds-check throw, unchecked ordinal lookup, mutation order, deferred commit and outer catch/rethrow: **Confirmed by code**.
- Process-fatal result: **Confirmed by the complete caller/unwind chain to the CRT**.
- Exact visual state during crash unwinding: irrelevant/unspecified.

## Replacement policy

The modern client should deliberately diverge for safety:

- validate object existence, view, declared sizes, field ordinals, container operations, element counts and allocation caps before mutation;
- decode into temporary typed values or a transaction/change-set;
- commit all fields atomically only after complete validation;
- publish callbacks after state commit in deterministic registry order;
- on failure, discard the entire application message, log the object/tag/view/field and bounded byte offset, then disconnect or quarantine the session under an explicit policy;
- never terminate the process because a server datagram is malformed.

This is behavioral hardening, not a wire-format change.
