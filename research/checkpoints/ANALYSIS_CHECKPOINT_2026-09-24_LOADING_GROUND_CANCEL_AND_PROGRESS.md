# Analysis checkpoint — ground loading screen progress and cancellation

Question A09 / W10, correlated to W03/W02. Target: verified original `Stardust.exe`; no client process or server was run. The existing registry had already identified the `0x00BA8180` constructor, so this block does **not** recount constructor discovery as new coverage. It resolves the previously undocumented progress/cancel side effects.

`SwgCuiLoadingGround` RTTI points to main vtable `0x015EE138`. Its constructor `0x00BA8180` binds `progressbar`, description and planet text, screenshot and image widgets, `buttonPrev`, and five readiness indicators (`checkserverobjects`, `checkfilecaching`, `checkworldloaded`, `checkterraingenerated`, `checkplayerready`). The last two lookups are fatal if absent from the UI page. The constructor conditionally hides diagnostic indicators depending on `0x009B3F40` and initializes the Previous button. Image/tip setup in `0x00BA8790` is presentation; no packet serialization is present in that path.

The progress update routine `0x00BA8EC0` reads `0x0093E260`, a getter for `DAT_0192F0E0`. When this value is nonnegative, it scales that percent against the progress widget width and updates the percentage text. When negative, it oscillates a fallback progress value between approximately 0 and 49 rather than claiming load completion. Five boolean getters (`0x0093EB50`, `0x0093EB70`, `0x0093EB80`, `0x0093EB90`, `0x0093EB60`) feed the bound readiness widgets. The exact producer/timing semantics of those five globals have **not** been proven; the UI labels are not proof of server-side completion.

The Previous-button callback `0x00BA8750` compares the triggering widget to the bound button at its subobject `+0x40`, then calls the common mediator hide/deactivate path `0x009C0BA0`, clears GameNetwork's local connection-related flag via `0x00462CF0(0)`, closes the server connection via `0x00462330`, sets the loading-manager fullscreen flag false via `0x0093E0F0(0)`, and invokes another non-null manager virtual at slot `+0x0C`. The analogous `SwgCuiLoadingSpace` callback `0x00BA98F0` has the same teardown chain. `0x0093E0F0` changes fullscreen loading state only on a transition, resets loading progress/text and emits a named loading-manager event; it is not itself a network packet. The exact meaning of the final manager virtual and the post-cancel destination scene remain open.

| Input or event | Client path | Result | Confidence |
|---|---|---|---|
| Loading-manager progress/readiness state | `0x00BA8EC0` -> `0x0093E260` and five readiness getters | Progress bar/text and diagnostic widgets | Confirmed by code for reads/UI writes; producer semantics unknown |
| Ground loading Previous button | `0x00BA8750` -> `0x009C0BA0`, `0x00462CF0`, `0x00462330`, `0x0093E0F0` | Hide mediator, clear connection flag, disconnect, leave fullscreen loading | Confirmed by code; subsequent destination unknown |
| Space loading Back button | `0x00BA98F0` -> same common chain | Same high-level cancellation, different bound button | Confirmed by code |

This is not a server message handler: **no new opcode or wire format** is established. For a replacement, loading display is optional presentation, but cancellation must remain an explicit session-lifecycle action with a clean socket close; do not infer readiness from a displayed check mark. A normal loading-complete transition and the five readiness-state producers require separate tracing.

Evidence: RTTI/vtables `0x015EE138`, `0x015EE208`; executable instructions and read-only decompiler output at the addresses above plus `0x00BA8180`, `0x00BA8790`, `0x00BA9050`, `0x0093E0F0`. Outcome: a new cancel/progress sub-boundary of W10, not full UI or loading-manager closure; no percentage change.
