# Offline movement and disconnect join

Runs login → character list → selection → typed selected-player scene → original human → modern local movement and authoritative corrections → protected disconnect send → drained cleanup. Independent peers are injected; no sockets, DNS, original executable or live account is used. This is executable offline preparation, not proof of server acceptance or release.

```powershell
& $python .\poc\round87-tests\Integration\run_selected_player.py --dotnet $dotnet --godot $godot --packages $packages --asset-root $assetRoot --config $mountConfig --output $freshOutput --gpu
```

All arguments are absolute explicit local inputs; output is a fresh ignored repository directory. The cache and Godot .NET SDK must already exist. Omit `--gpu` for headless-only execution. The command runs success, cancellation before ready, minimum-provider failure, disconnect send error and disconnect send timeout; `--gpu` adds original-actor success on GPU. Child processes are bounded and source hashes retained.

The complete independently authored Round86 movement prefix is retained. A disconnect is eight decoded bytes: opcode00 05, admitted zone connection ID big-endian, raw trailing0006. Existing zone protection applies; no reliable sequence or ACK is invented. Raw reason semantics remain unknown. Captured attempted wire, request, submission, successful send completion and retirement are separate observations. A synchronous send error or never-completing cancellable send must fail without claiming completion, then close ports/jobs/actor/session. Successful local send alone does not establish server receipt or session release.

Original preparation,39 loaded source identities and terrain hash are independently audited. The actor remains an uncustomized human, minimum PTAT readiness remains a modern bounded policy, and velocity/flags/counters/timestamp bits are authored fixture policy. CREO1/PLAY gameplay, nearby actors, general avatar readiness, complete procedural surface, original prediction/cadence/collision and live compatibility remain open. The selected actor's instance is preserved through corrections and retired during cleanup. Preserve close → await completion → dispose borrowed motion store/free/quit.

See [disconnect contract](../../../docs/REPLACEMENT_CLIENT_DISCONNECT_CONTRACT.md), [disconnect profile](../../../docs/REPLACEMENT_CLIENT_DISCONNECT_PROFILE.md), [milestone plan](../../../docs/REPLACEMENT_CLIENT_MILESTONE_PLAN.md) and [controlled validation plan](../../../docs/CONTROLLED_SERVER_VALIDATION_PLAN.md).
