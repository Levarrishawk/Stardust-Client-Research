# Network Status and MFD Widget Checkpoint

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Network diagnostics

`SwgCuiNetStatus` constructor `0x00D58490` binds ping, packet loss, bandwidth
and FPS text. Update `0x00D586C0` periodically reads active connection
statistics and local graphics frame rate, formats them and updates the fields.
It is read-only diagnostics; it does not participate in the lag-report packet
or reliability policy.

The replacement should expose the same diagnostics from its transport and
renderer and keep cumulative counters 64-bit internally. Wire counters retain
their documented widths.

## MFD status primitives

`SwgCuiMfdStatus` constructor `0x00E16030` associates a watched game object and
binds a direction arrow. `SwgCuiMfdStatusBar` constructor `0x00E16280` binds
value text; current, current-maximum, recent-maximum, recent-current and
normal-maximum layers; configurable margins; vertical/horizontal orientation;
and an optional tick.

This is a reusable local presentation primitive for HAM/status values. Recent
layers provide delayed damage/heal trails independent of authoritative values.
It consumes replicated object state and introduces no packet handler.

## Replacement guidance

Use explicit current/max/recent samples with time-based interpolation, sourced
from immutable per-frame object snapshots. Preserve orientation, margins,
ticks, styles and images from original UI assets; discard legacy mediator
pointer layout.

## Confidence

Bindings, metric categories, watched-object ownership and status-bar layers are
**confirmed/strongly inferred**. Exact display units and recent-value decay
constants are fidelity tests, not protocol uncertainties.


## October 14 source-client crash correction

User reports Ctrl-O / Show Network Status Window crashes. The exact client-owned fatal names missing textActivity at SwgCuiNetStatus.cpp50 from /GroundHUD.netStatus. Effective ui/ui_pda_net_status.inc, priority53 stardust_3_01.tre,3862 bytes/SHA2566C771709DCAE74164A2A64ECA7AB9FFE25A89232E2A31078E978DBE5904F60D3 binds exactly textPing/textPacketLoss/textBandwidth/textFps, consistent with the original constructor above. Confirmed asset/source mismatch; no asset replacement is necessary for these four diagnostics.

Source SwgCuiNetStatus treats activity as optional only under usePrecuHud, retains required NGE binding otherwise, and requests GalaxyLoopTimes only when its optional display exists. Existing ping/bandwidth/FPS diagnostics and options access remain. Cluster-info lookup is now null-checked before metric reads. Release build passes existing external PDB warnings; deployed Experimental EXEF2375434AC20DB29FD469503D4BBE6A708717F45095BC4C3DE3413244271B8C6 has matching symbols. Live toggle acceptance is the next gate; no protocol/function metric changes or private assets/account data are promoted.
