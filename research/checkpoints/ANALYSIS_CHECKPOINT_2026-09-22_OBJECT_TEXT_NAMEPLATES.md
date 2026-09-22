# Object Text and Nameplate Manager Checkpoint (2026-09-22)

## Outcome

`CuiObjectTextManager` is a presentation/cache subsystem, not a network decoder. It derives overhead object text, icons and selection/match state from the current client object model and UI preferences, caches per-object screen/presentation state, and clears that state across scene changes.

## Installation and assets

Install routine `cui_object_text_manager_install_VA_008A58B0` subscribes to `Game::SCENE_CHANGED`, enumerates `/styles.icon.faction`, retains qualifying faction/status icon styles, resolves `/Styles.Icon.Misc.match_group`, and initializes the object-state containers. Shutdown `0x008A5C80` releases the listener, cached styles and state. Scene callback `0x008A5BB0` clears the per-object display cache; `0x008A5D50` clears both internal collections.

## Per-object state

Principal routine `cui_object_text_build_or_update_VA_008A6270` consumes a client `Object`, current player/camera context, object transform, name-display preferences, target/match/group state and replicated identity/state to construct the Unicode label/icon presentation. An explicit `Pilot %I64i` path confirms that 64-bit NetworkIds participate in presentation but are not native pointers.

`cui_object_text_smooth_anchor_VA_008A5EF0` maintains a cached three-float presentation anchor using frame time and separately bounded horizontal/vertical convergence. It warns on time regression. This is presentation smoothing, not authoritative movement.

The two integer pairs are now resolved as current/previous-frame vertical
nameplate layout displacement (`+0x30/+0x34`) and current/previous projected
screen-Y (`+0x38/+0x3C`). Maintenance rolls each current value to previous and
resets the new frame's current value to 0 or sentinel 10000 respectively.
`0x008A8810` separately memoizes a per-object world-query/visibility result and
maintenance invalidates that Boolean/float cache each frame. See
`ANALYSIS_CHECKPOINT_2026-09-22_NAMEPLATE_FRAME_CACHE.md`.

## Replacement-client consequences

- Keep nameplates downstream of the authoritative object model.
- Key caches by lifetime-safe entity handles/NetworkId, never raw `Object*` values.
- Generation-invalidate caches on scene changes.
- Preserve original icon/style resources or deterministic mappings for faction and match-group styles.
- Smoothing, layout and occlusion can be redesigned behind a presentation interface.
- A headless networking proof of concept omits this subsystem.

## Confidence

- Identity, installation, lifetime and style dependencies: **Confirmed**.
- Cached anchor smoothing and invalid-reference maintenance: **Confirmed**.
- Principal builder dependencies: **Confirmed broadly**.
- Historical source member names and every visibility permutation: **Partially
  unresolved; bounded UI fidelity**. The previously unknown frame-cache
  behavior is closed.
