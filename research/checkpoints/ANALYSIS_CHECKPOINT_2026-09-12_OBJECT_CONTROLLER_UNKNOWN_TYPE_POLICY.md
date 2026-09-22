# ObjectController unknown-type policy

Date: 2026-09-12  
Subject: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Confirmed legacy behavior

`obj_controller_inner_factory_VA_00B0D190` searches the inbound factory map. If
the registry is not installed it emits `ControllerMessageFactory::unpack not
installed`. If the numeric type is absent it emits:

```text
ControllerMessageFactory::unpack received a message %d which had no registered unpack function.
```

It then returns null. The outer parser `0x00AC9660` retains that null at message
`+0x38`, records the per-type telemetry key `recv.ObjControllerMessage.%lu`, and
returns the otherwise valid outer message.

Dispatcher `obj_controller_dispatch_VA_00462F60` resolves the addressed object and
controller. If either cannot be resolved, it destroys a non-null nested payload
and returns. Crucially, a null factory result is passed through to the controller
queue only when a target controller exists; the inspected queue boundary accepts
the pointer value rather than dereferencing it in this caller. No parser exception,
connection close, or immediate process termination is issued solely for an absent
inner-type factory.

## Replacement policy

Do not reproduce the nullable legacy queue contract. A replacement should:

1. validate the fixed outer envelope;
2. look up a direction-specific inner codec;
3. if absent, log the numeric type, object ID and bounded payload length;
4. discard that complete ObjectController message without mutating object state;
5. optionally disconnect after a configurable repeated-protocol-error threshold.

This is behaviorally compatible for ordinary traffic while making unsupported
types explicit and memory-safe. It also closes the last function-led “unknown
payload behavior” item in the address ledger.
