# Overnight client analysis — completed 2026-09-23 05:00 EDT

Target: original `C:/Stardust-DEV/Stardust.exe` and effective configured assets, with server repository read-only where needed. The requested 2026-09-23 05:00 EDT (09:00 UTC) stop was reached. This completed recovery index accompanies `ANALYSIS_SESSION_2026-09-22_TO_2026-09-23_0500_REPORT.md`.

Completed and separately checkpointed:

1. FireProjectile runtime: object selection, visual projectile creation, orientation, speed, and distance-based lifetime.
2. ULAT tag lookup and effect phases, including typed-tag anomalies that need explicit validation.
3. `SNGL` priority byte corpus and `ClientGame`/`ClientAnimation` configuration sources.
4. `WFBO` actor-distance/`LEVP` time-of-flight wait.
5. `PPRT` authored appearance, hardpoint, duration, and optional sound lifecycle.
6. `CPSA` integer-variable-to-`CreatureObject` posture application.
7. `TSTR`/`TSTP` retained swoosh/trail creation and stop cleanup.
8. `TOAT` temporary object-template construction and skeletal alternate-wearable attachment.
9. `AMSG` skeletal animation-message subscription and match gate.
10. `STOP` targeted thread/action stop and index advancement (not whole-script cancellation).
11. `FACE` target-facing and `SCAL` uniform-scale transitions.
12. `ATAT` authored two-actor visual arc construction and controller handoff.
13. `FTAT` installation/turret action, integer-tag control, and action-index handoff.
14. `HPTA` hardpoint-target object and `HTAT` two-actor homing/interpolation object.
15. `GRLB` grenade throw state machine plus a confirmed template-version gap for effective `0003` assets.
16. `WATR` watcher gate, `TIMR` playback timer, `ALHP` hardpoint transform alignment, and `CWAT` clone/alternate-wearable attachment.
17. `THSW` throw-sword object choreography, thresholds, animation message, and cleanup.
18. Effective playback compatibility matrix: 124 actions, nine direct registration/version gaps confined to four likely dormant scripts.
19. `PLNT/0000` planet appearance: complete 23-file effective grammar, shader references, and geometry/halo construction route.
20. `STAT/0000` space terrain: complete 16-file eleven-child-form corpus and executable reader field widths; runtime rendering remains open.
21. `STAT/PLAN` client/server source reconciliation: one additional client-read degree value, shared terminal zero, and confirmed `STAT -> PLNT` references.
22. `STAT` client runtime: appearance factory, object allocation, contained-object update, light/skybox/star/planet construction route.
23. Inventory mediator activation/deactivation, typed selection transceiver, target-range gating, and an unidentified conditional send.
24. Effective PRTO corpus: 291 live layouts, 3,470 cells, 1,061 portal geometry records, optional path graphs, and one mask.
25. Inventory-close tutorial edge: `NewbieTutorialResponse("closeInventory")` reliable send to server observer, resolving the prior unidentified conditional send.
26. Item-transfer command selection: volume/slotted helpers, Armor versus hold-slot Weapon versus Misc, queue formatting, and server callbacks.
27. Equipment-transfer callers: live inventory manager swap path versus template restoration and offline/debug fixture paths.
28. Effective FLOR corpus: 4,730 live floors, four deployed versions, optional BTRE/PGRF combinations and 46 masks.
29. PRTO→FLOR closure: 3,442 cell floor references, 3,429 resolved, 13 missing in one v0004 police-station layout.
30. Shadow-volume policy: live shader/render path and a legacy <375 MB memory-budget auto-disable, not unconditional disable.
31. Auction bid/buy UI: distinct mediator gates, shared BidAuctionMessage request, typed response listeners, and success-close lifecycle.
32. Persistent-mail UI: composer recipient/send bridge, shared attachment listener lifetime, and browser typed-event refresh by selected ID.
33. Craft draft selection: mediator Next/tree selection through manager index validation and `selectDraftSchematic` command to current server session callback.
34. Craft assembly UI: typed events and slot-removal controller route, plus `nextCraftingStage` client counter and server state-machine boundary.
35. Ingredient chooser: assembly slot choice through manager pending-state gate to controller `0x107`; corrected server-discarded extra integer field.
36. Experiment UI: positive row/point allocations to controller `0x106`, distinct stage-advance path, and manager response subscriptions.
37. Craft customization UI: bounded schematic count and stage-4 `0x15A` sender, with current server pair-count parser.
38. Final craft commands: `createPrototype` versus `createManfSchematic` identities and a documented client/server state-number reconciliation item.
39. Craft final-state timeline: reconciled client-visible PLAY state 5 with server-private session state 6 across `nextCraftingStage` and `finishStage1`.
40. Craft option mediator: prototype/manufacture/experiment branch modes and response-gated transition to customization.
41. Craft summary mediator: production table built from manager/schematic data, test-table branch separated, response/return routing and cleanup.
42. Missing-floor police POB reachability: no direct client template or current server portal-layout reference; only interior row/cache mentions, so retain conditional diagnostic.
43. LSAT lightsaber assets: full 45-file effective corpus, version-0000 reader, one/two blade grammar, optional light and resolved dependencies.
44. LSAT runtime: composite base and per-blade appearances, shared visibility parent, independent blade transition lengths and hardpoint behavior.
45. LSAT dynamic bounds: blade-length-driven DPVS extent refresh; optional LGHT and ASND runtime consumers explicitly remain unproven.
46. LSAT optional light/sound: resolved blade-state setter, Light construction and authored-field copy, palette color, sound fetch and cleanup.
47. LSAT template reachability: 208 effective weapon templates directly name 42 of 45 live `.lsb` assets; all referenced paths resolve.
48. XFMS transform masks: nine effective live version-0000 files, counted transform-name lists, cache/loader path and masked-priority animation-template reference.
49. XFMS runtime routing: mask-name membership selects transform-in versus transform-out group per skeleton transform; locomotion group remains separate.
50. XFMS reachability: two effective human LAT graphs directly reference four live masks 1,014 times; per-transform group queries use mapped array while playback delegates.
51. Sound descriptor outliers: two live `taan_*` SD2D records parse as huge category IDs because of shifted scalar bytes; no direct asset references found, bounded mixer validation required.
52. Auction sell UI: submit mode, normal versus immediate message construction, reliable send and response-to-selected-item correlation.
53. Auction duration policy: current server parses but ignores the client's duration, using fixed expiry constants; protocol-compatible yet UX-visible.
54. SKTM/0002 disk grammar: exact eight-chunk field order, 739 effective skeletons (114 standalone plus 625 nested), array widths and zero-joint edge cases.
55. SKTM corpus invariants: 14,138 transforms, all parent-before-child, all three rotation arrays unit quaternions, and all JROR values zero.
56. SKTM bind-pose route: ordered quaternion composition, BPTR translation, rigid inverse and parent accumulation; final indirect multiply convention remains open.
57. SKTM multiply backend: traced dispatcher to scalar affine 3x4 product; final order is local inverse times parent/external transform.
58. CKAT/0001 counted channels: all 7,771 clips validate INFO/XFIN/SROT/STRN counts; 32 optional QCHN chunks decoded structurally.
59. CKAT QCHN routing: separated per-AROT keyed QCHN from 32 optional trailing QCHNs; XFIN animated-rotation indices route to AROT channel evaluators, while trailing-channel playback is open.
60. CKAT AROT corpus: all 7,771 clips validate INFO animated-rotation counts against 171,000 nested QCHN children with zero width/tag discrepancies.
61. CKAT ATRN corpus: 40,701 nested CHNL translation channels validate against INFO counts and six-byte-per-key payloads with zero discrepancies.
62. CKAT XFIN binding: all 298,315 transform records have in-range static/animated rotation and X/Y/Z translation indices; axis-mask bits select eight evaluator variants.
63. CKAT key invariants: 12.06m rotation and 1.98m translation keys have strictly increasing U16 times; rotation U32 key bits are not ordinary floats.
64. CKAT packed quaternion: dynamic-rotation vtable decodes 11/11/10 sign/magnitude components, reconstructs the fourth and interpolates between adjacent key quaternions.
65. CKAT translation playback: static STRN value or animated ATRN channel per axis, cached key interval, and linear interpolation confirmed.
66. CKAT static rotation: SROT U32+3-byte entries use the shared packed-quaternion decoder; disassembly corrected ambiguous decompiler receiver flow.
67. CKAT root motion: optional top-level QCHN and LOCT choose four relative-motion evaluators (identity, rotation, translation, both); their playback consumer is identified.
68. CKAT LOCT corpus: 1,354 clips, 98,901 finite monotonic position keys, and all four optional root-motion combinations validated.
69. CKAT MSGS corpus: 2,954 clips contain 6,952 named message groups and validated occurrence times; runtime window query found, individual subscribers remain open.
70. CKAT timed-message runtime: MESG occurrence windows feed a compressed-animation virtual method that converts playback times with loop handling; subscribers remain open.
71. CKAT event integration: 3,129 deployed message groups use six known GameAnimationMessageCallback prefixes; 3,823 non-prefix groups need separate consumers.
72. CKAT frame-rate/speed: INFO first F32 and LOCT leading F32 satisfy net displacement times fps divided by frame span in all 1,354 LOCT clips.
73. CKAT quaternion selectors: three channel bytes index shared quantizer base/scale records for 11/11/10 packed components; exact table initialization remains open.
74. CKAT quantizer install: SharedMath installation builds the selector base/scale table used by the packed-quaternion decoder; individual numeric entries remain to verify.
75. CKAT INFO timing: 30/24 fps corpus split and declared end-frame field matching maximum sampled-channel time in all 7,505 clips with such channels.
76. CKAT function registry: assigned stable provisional identifiers to 20 loader, evaluator, root-motion, quantizer and message functions for future sessions.
77. CKAT selector coverage: corrected disk-offset audit finds 127 selector values (128..254) in AROT and 75 of those in SROT; earlier all-256 claim was an audit error.
78. CKAT quantizer formula: seven SharedMath ranges initialize selectors 128..254 with exact base/scale rules; six sampled norms marginally exceed one and need a numerical fixture.
79. CKAT event endpoints: 51,837 occurrence frames stay within the declared end frame; 2,305 start and 1,517 end occurrences require exact loop-boundary policy.
80. CKAT event-window bounds: instruction-level binary searches select half-open integer frame interval [start,end); higher-level loop scheduling remains to verify.
81. CKAT advance/completion: runtime clamps current frame, returns time overrun to track ownership and samples previous-to-current root motion; end-frame event fate remains a targeted edge.
82. CKAT outer track: completion substep emits outgoing-animation messages before replacement, but its normal half-open query excludes exact end-frame events.
83. CKAT final-frame priority: 1,517 end-frame events span 504 clips, including 294 `event_footstep` occurrences; a differential playback fixture is warranted.
84. CKAT full quaternion audit: 248 reconstructed norm overshoots among 12.19 million deployed packed values, confined to 16 clips; original x87 behavior remains to fixture.

Each checkpoint is `research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_<topic>.md`; machine-produced decompilation and temporary corpus probes remain ignored under `local-private/session-2026-09-23-marathon`. Do not infer whole-client percentage progress by simply adding these local action counts. No client/server/build/Git state mutations or commits are part of this session.
