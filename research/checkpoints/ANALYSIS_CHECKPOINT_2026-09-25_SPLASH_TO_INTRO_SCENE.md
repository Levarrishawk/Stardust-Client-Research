# Splash screen to intro-scene handoff

Date: 2026-09-25. Workstreams A09/A04/R01. Target: verified `Stardust.exe` and current configured TRE overlay. Decompiled output is reconstructed pseudocode.

## Result

`SwgCuiSplash` is the reachable, asset-driven startup splash sequencer. It selects a locale/expansion page variant, advances three timed pages using `ClientUserInterface.splashTimeoutSeconds`, and on completion constructs the already recovered `scene/intro.iff` controller. This closes normal startup reachability for that intro scene.

```text
CuiMediatorFactory 0x00BCC360
  -> SwgCuiSplash constructor 0x00CA93C0
  -> activation 0x00CA9670
  -> timed update/listener 0x00CA9770
       page 0 -> page 1 -> page 2 variant
  -> completion 0x00CA9850
       close splash
       activate Backdrop
       construct intro-scene controller 0x0139CA30
       install/switch scene
```

## Construction and asset selection

The factory allocates `0xA0` bytes. The constructor binds two base splash pages plus one of:

- `JTLJapan` when the platform/locale string matches the executable's three-byte Japan discriminator;
- `2withJTL` when startup feature bit `0x10` is set;
- `2noJTL` otherwise.

It also binds `textVersion` and writes the current client version string. The active configured member is `ui/ui_splash_screen.inc` from priority-53 `stardust_3_01.tre`, with a nonempty 13,610-byte payload. The declarative asset contains the named page variants, enable/disable effectors, version label, legacy logos and copyright presentation.

## Activation, timing, and input boundary

Activation `0x00CA9670` restores page opacity, enables the relevant UI/input layers, attaches the listener, resets the page sequence, and computes the first deadline as current UI time plus `splashTimeoutSeconds`. Config install supplies a default of five seconds.

Update/listener `0x00CA9770` postpones the deadline while the vector counted by `0x00896EB0` is nonempty. Xrefs close this as the active `CuiMessageBox` collection: install `0x00895780` allocates it, create path `0x00895AD0` appends a newly constructed message box and exposes the `/Console` page, update `0x008959C0` removes closed entries, and teardown `0x00895960` closes and destroys the collection. Splash timing therefore pauses while a modal/message box is active. Otherwise, when the deadline expires, the sequence increments the page index and enables another page while the index is below three. Once the third page is complete and its fade opacity reaches the required terminal value, completion runs.

Message callback `0x00CA9820` recognizes input/message codes `0..5` and `7..9` and clears a member gate. The precise user-visible skip policy is not fully recoverable from this method alone; it is an input participation boundary, not evidence that every event immediately bypasses all pages.

Deactivation `0x00CA9720` disables UI/input flags, detaches the listener, and unregisters the message subscription. Destructor `0x00CA95D0` clears its page pointers and releases the listener/base mediator state.

## Intro-scene handoff

Completion `0x00CA9850` closes the splash mediator, activates `Backdrop`, allocates `0x454` bytes, calls confirmed intro-scene controller constructor `0x0139CA30`, and passes the result into the scene-install boundary. The latter constructor loads `scene/intro.iff`, previously analyzed independently. This direct caller establishes ordinary startup reachability of that intro scene rather than a merely dormant asset consumer.

No packet construction or network send occurs in the recovered splash-to-intro chain. A replacement may shorten or omit the legacy branding sequence subject to product/legal decisions; protocol compatibility does not depend on it. The local startup state machine must still reach the subsequent login/session state cleanly.

## Confidence

- **Confirmed by code/assets:** factory, page bindings, variant selection conditions, version assignment, timeout getter/default, three-page timed advancement, cleanup, intro controller construction, scene install, and current nonempty asset.
- **Strongly inferred:** friendly lifecycle names and original spelling of the message-box manager methods.
- **Unknown/non-blocking:** exact semantic names of input codes and the complete skip UX.

Ignored reproduction evidence: `local-private/generated-evidence/swg_cui_splash_*`, `ui_splash_screen_effective.inc`, and `local-private/ghidra/decompiled/swg-cui-splash*/`.
