# Space all-target overlay: waypoint and turret-marker runtime refinement

## Prior boundary and scope

The [2026-09-22 HUD/target checkpoint](ANALYSIS_CHECKPOINT_2026-09-22_HUD_TARGET_STATUS_ARCHITECTURE.md) already identified `SwgCuiAllTargetsSpace` constructor `0x00EE7EB0`, its `targetLeadIndicator`/`waypointIndicator` widgets, and the projected target-lead/acquisition overlay `0x00EE9D60`. None of those is counted again. This block adds the previously untraced per-frame waypoint, damage-arrow and turret-target helper paths from the original `C:\Stardust-DEV\Stardust.exe`. Decompiled output is reconstructed pseudocode.

## Runtime ordering

The mediator's per-frame virtual method `0x00EE8680` retains elapsed time, ages/retire some owned arrow records, obtains the current scene/player ship, and calls `0x00EE88E0` then `0x00EE9370`. When a space-target state predicate succeeds it additionally calls `0x00EE9480` then `0x00EE9890`. Finally it advances the common target overlay base. This is view maintenance; no direct packet send is present in these inspected methods.

- `0x00EE9370` iterates retained direction-arrow records. It transforms arrow target positions relative to the current object's world transform, computes a yaw-like `fpatan` angle normalized by a retained scale, stores it on an arrow widget, and makes the widget visible. The exact source event for every arrow record was not traced here.
- `0x00EE9480` reconciles a scene/player waypoint-like list from `0x005266F0` with a parallel vector of `waypointIndicator` widgets. It creates/destroys child widgets to match list length, hides entries lacking usable target data, transforms/project eligible positions through `0x00767F60`, and updates screen-relative arrow/distance text (`textDistance`). This is a per-frame projection of client waypoint state, not a new waypoint packet. The semantic identity of every `0x005266F0` list member remains inferred from this consumer and widget name.
- `0x00EE9890` reconciles a different set of space targets against `turretTarget` child widgets. It builds relative world/ship vectors and projects them for per-target markers. Its target-set population and station-specific gating deserve a separate trace if turret-overlay fidelity becomes a priority.
- `0x00EE88E0` is the large main-target geometry/projection routine. It distinguishes ship station/range cases, takes world transforms and camera state, and updates the ordinary target overlay. Only that high-level consumer role is asserted here; its 386-line output is not a closed mathematical specification.

**Confirmed:** caller order, widget vector reconciliation, transforms/projection calls, `textDistance`/`turretTarget` literals, and no direct network send in the inspected methods. **Strongly inferred:** waypoint and turret-marker roles from widget names plus list/projection behavior. **Unknown:** exact upstream ownership of the two target lists, mathematical edge cases near camera/projected bounds and complete marker priority policy. No previously mapped class/lead path is re-counted, and no whole-client percentage is claimed.
