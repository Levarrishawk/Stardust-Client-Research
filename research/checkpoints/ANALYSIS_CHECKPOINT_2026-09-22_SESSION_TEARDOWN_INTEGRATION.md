# Analysis checkpoint: session and process teardown integration

Date: 2026-09-22  
Scope: cross-check of scene/session owners, worker publication boundaries and process exit ordering

## Result

The replacement needs three distinct teardown scopes. Treating “disconnect,” “zone transition,” and “exit” as the same destructor path would either leak session state into the next scene or destroy services still needed by returning login/selection UI.

## Scope model

| Scope | Trigger | Must release | Must remain alive |
|---|---|---|---|
| scene reset | zone/scene change | world objects, scene UI, SUI registry, map/region/object-text caches, terrain generation and scene-bound effects | authenticated session, network transport, global VFS/assets, shell UI |
| session disconnect | user disconnect, server loss, fatal protocol/session error | scene reset plus player/session replication, pending commands, chat/session state and zone connection | platform/window, VFS/assets needed by login/selection, renderer/audio/input, diagnostics |
| process exit | menu Exit, window/process termination | session disconnect plus all workers, asset services, renderer/audio/input, platform/foundation | only diagnostics needed to finish shutdown |

The original `Game::cleanupScene` is separate from `Game::remove`; SUI also has separate scene-drain and manager-teardown paths. This directly supports the three-scope model.

## Publication and worker ordering

Confirmed worker families publish results back to the main/game thread. Safe replacement ordering is therefore:

```text
1 stop accepting new gameplay/UI commands
2 invalidate scene/session generation tokens
3 stop network delivery into the session queue
4 deactivate mediators and detach object/model callbacks
5 cancel pending asset callbacks for destroyed owners
6 signal terrain/asset/file workers and drain or discard their results
7 join workers that can still reference scene/resource owners
8 destroy world/replication/session objects
9 on process exit only: destroy shared assets/VFS, renderer/audio/input and platform
10 flush diagnostics and finish foundation teardown
```

Generation invalidation belongs before drains so a completion that was already produced cannot resurrect an old scene. GPU/resource finalization must run on its owning thread before renderer destruction.

## Legacy mechanisms not to copy

- The ExitChain is a priority-sorted callback list, not a reliable reverse-construction stack.
- It permits registration while draining.
- Original malformed network decode can escape to process-fatal handling.
- Several subsystems assume ordinary callers have quiesced instead of exposing a universal cancellation pass.

The replacement should use explicit typed owners and a dependency graph, bounded message/result queues, cancellation handles and idempotent scope teardown.

## Required invariants

1. Every callback/subscription is removed before its target object is destroyed.
2. No worker publishes a mutable world/UI object directly.
3. Every queued completion carries a scene/session generation or cancellable owner handle.
4. Scene reset is repeatable without rebuilding process-global asset services.
5. Session disconnect can return to shell UI without restarting the process.
6. Process exit is idempotent after a partial startup failure.
7. Logs remain available until all other shutdown diagnostics have been emitted.

## Confidence

- separation of scene cleanup, subsystem removal and process ExitChain: **Confirmed**;
- dedicated worker/result boundaries and join requirements: **Confirmed**;
- exact legacy global order of every registered callback: **not exhaustively enumerated**;
- proposed replacement phase order: **Recommended architecture derived from confirmed ownership constraints**.

