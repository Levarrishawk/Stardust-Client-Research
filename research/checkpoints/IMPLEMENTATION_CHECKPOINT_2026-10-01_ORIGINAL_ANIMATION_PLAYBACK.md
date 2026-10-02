# October 1, 2026 — Bounded original idle playback

## Question and stopping condition

Can one selected original clip advance under an explicit modern preview clock, with validated loop/end/seek/pause/focus and GPU control paths? Stop at bounded inspection playback, without original gameplay controller/state graph/root-motion claims.

## Result and evidence

Added engine-neutral clock and standalone Play idle/frame scrub controls. Native fixed-clock/scale/one-shot/invalid-time gates pass. Headless and GPU original idle fixtures run480 pose uploads/two cycles, periodic complete readback, reverse/endpoint seeks, bind reset and pause/focus holds. GPU actual control signals verify play advance, pause/focus hold and scrub endpoint restoration. Updated frame30 capture inspected: neutral relaxed pose, body/head aligned and head clear of header.

Fifteen unchanged sources/726,134 bytes; unused `rifle` and explicit missing-bind `hold_l`; zero packed repairs. GPU fixture mean2.464 ms per pose update, including deformation/submission and periodic readback. This is a local bounded diagnostic, not an original runtime timing or production performance claim.

`AnimationPreviewClock` loop, focus, stall and finite publication policies are declared modern behavior. Prior direct executable composition/sampling evidence and independent265-joint/8,950-vertex audit remain unchanged. [Durable contract](../../docs/ANIMATION_PREVIEW_PLAYBACK_CONTRACT.md) specifies exact scope, limits and reproduction flags. No original executable/server execution, publication, commit or coverage metric changes.

## Resume

Prepare the same explicit clip in the persistent interior source-cache union, attach clock updates to the existing character visual and verify actor feet stay authoritative while visual root channels remain a separate bounded presentation input.
