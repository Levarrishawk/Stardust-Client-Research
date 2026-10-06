# Round80: persistent offline outcome and bounded restart composition

Date:2026-10-06. Question: can actual configured movement and synthetic root owners complete, preserve immutable outcomes, retire, and start fresh generations through a persistent Godot outcome/restart display, including fault recovery and factory failures? Stop four independently sealed components plus one bounded joined headless/GPU proof. This is a modern compatibility prototype; no live connection or legacy restart behavior is inferred.

## Input identity and tools

Frozen accepted79:4608 public files; manifest d51ed19b0af61626ead8049942d9f12690304e308bd2bceb10126d851cf2b51b; archive 3f7adfc1647ce05dcae6bc9967de1eb345d8178e8d572a7d0643be9799579484. Main HEAD ece6e3943ac84fb10d84bc9d44314fa7ba032f92 was captured after the user's intervening Git update and is preserved. Four existing isolated specialists remained Low. Exact incoming/prior/own-final sync gates and final baseline-relative review preserve dirty/private/HEAD/index state;29 final worker file hashes match integration.

Lead reused cached Python/.NET8/Godot4.7.2 packages and fresh ignored replay outputs. Synthetic runtime fixture SHA c69010c21428da37435ccef1f8f371449c01481687332482774616aaf257dd21 and terrain fixture SHA 85226fe4fb7f1961c3ebb1e3df520d3d5ec0e9ac1601b0c2338ee08a9d7698cd are gated by the joined oracle before any native build. Static original executable SHA E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150 is independently gated before getter VAs. No original executable was run.

## Four accepted component boundaries

| Component | Observation and validation | Interpretation/confidence and consequence |
|---|---|---|
| [Key getter](../../docs/COMPRESSED_ROOT_KEY_GETTER_CONTRACT.md) |13 bytes/five instructions at00AA41C0, SHA d16c026950a9c3e8eded66bc5f342573d7e6c3311ca277299abc93678b15a04a; dword this+C or fixed01908090, no calls/writes;19 admission guards/eight word-choice fixtures | Confirmed local pointer selection/nonmutation. Dynamic type is conditional; fallback byte is virtual-only and runtime contents/lifetime remain Unknown. Setter remains unopened. |
| [Movement restart owner](../../docs/TERRAIN_OFFLINE_SESSION_RESTART_OWNER_CONTRACT.md) |129 checks/37 negatives; actual three runs/seven successful updates/four completion reports, real completed1/attempted2 terrain fault and subsequent healthy recovery | Confirmed modern serialized ownership. Capture before controller/point retirement, immutable history, successful-generation cap1..32, explicit factory retry; failed retirement blocks future factories. No rollback of caller placement/publication. |
| [Restart HUD](../../docs/TERRAIN_OFFLINE_RESTART_HUD_CONTRACT.md) |94 headless/95 GPU checks; two display owners/six actual GUI rows/22 failure prefixes/five teardown cases | Confirmed owned persistent outcome/button, rejected retry and duplicate suppression, subtree/policy guards and native-safe retirement. Caller retains and replaces the display; external callback effects are not rolled back. |
| [Root restart owner](../../docs/OFFLINE_SOE_ROOT_MOTION_RESTART_OWNER_CONTRACT.md) |92 checks/nine actual updates/three handshakes/two protected root frames/four completion reports; actual wrong-ACK fault and foreign-thread cleanup failure | Confirmed synthetic serialized generations, clean healthy completion versus honest faulted prefix, closed old transport before new factory. Borrowed FIFO remains caller-owned; clean buffers do not establish server receipt or global movement completion. |

Rare same-thread child-retirement/allocation failures without injection hooks remain source-inspected. Both owners execute a safe intentionally foreign-thread returned-prefix failure and close that prefix on its creator thread; this demonstrates rejection/terminal retention, not cross-thread cleanup support.

## Joined proof and durable lifecycle

[Native composition](../../poc/Stardust.Godot/TerrainOfflineRestartCompositionVerification.cs) joins the three independently authored owners over the same caller-owned synthetic world/store/surface/FIFO. It does not load resource actors. [Independent oracle](../../poc/round80-tests/Integration/restart_peer.py) computes binary32 positions/completion reports and constructs handshakes,62-byte roots, seeded transforms and ACKs before native execution. The saved expectations must precede native output and equal recomputation; native output never seeds expectations.

Four successful admissions comprise two consecutive maximum3 budget completions, a strict terrain fault (one completed/two attempted), and a healthy maximum3 recovery. Three accepted restart callbacks create fresh movement and Established transport owners with zero update/sample/sequence history and four distinct connection IDs/seeds. Three final-stop protected root frames and exact ACKs compare byte-for-byte. The terrain-fault generation has one observed root sample and zero frames; its buffers are empty while movement remains Faulted and Stopped=false.

Complete captures both primitive reports before child teardown. The old outcome remains visible over two scene frames with no active children. A deliberate thrown first factory returns a rejected restart request without generation/history changes; the outcome remains visible and the next real GUI button gesture admits both fresh owners. Accepted duplicate requests cannot invoke callbacks again. Only after the new pair is admitted does the caller dispose the old display; late old controller advancement rejects and leaves the new row/FIFO intact. Four displays retire once with no remaining owned native children.

A separate joined second-factory failure admits a new movement owner, rejects transport construction, then closes both restart owners. No completion is invented, no active child survives, and the caller's explicit initial placement is not rolled back. Cross-owner admission is sequential, not an atomic transaction. A later failure or display guard failure can retain partial effects and must terminate/retire the caller's run.

Headless and GPU joins each pass60 checks and compare four exact report/position/handshake records, three root/ACK records, three accepted restarts and one rejected factory request. Borrowed scene, motion store and surface survive; FIFO is empty. GPU captures of ordinary and actual fault outcomes were inspected: all five lines and the240x40 restart button fit with an8px gap. This tiny scene proves real report provenance and GUI-driven owner replacement; it does not show terrain/actor rendering or persistent full-playground use.

GUI gestures use the component's explicit standalone SubViewport and NotifyMouseEntered plus motion/left down/up through actual PushInput routing. No positive activation relies on EmitSignal, native notification hooks or OS pointer movement. Physical native-window clicking remains unexecuted.

## Failures retained and repaired

- Lead review found that transport cleanup exceptions could lose the owned prefix and permit another factory. Final code retains the prefix, records terminal CleanupFaultType, preserves completion before retirement and rejects future factory/complete operations. Revised exact expectations and92-check replay pass.
- Lead review found that raw outcome Label/Panel deletion was not detected by checking only facade retirement. Final pre/post-callback guards validate the frozen passive subtree through Refresh and exact button policy.22 failure prefixes pass without weakening invocation/history expectations.
- Native-window synthetic gestures reached the button without hover/activation. Retained failed replays led to the documented SubViewport fixture; production button release behavior and activation assertions remain unchanged. Official engine source is correlation evidence, not proof of the installed binary.
- The lead's concatenated archive label was ambiguous and contributed to copied hash-transcription failures. Trusted sync gates always contained the correct64-digit digest. Workers retained failures, corrected literals to the frozen artifact and replayed exact admission.
- Joined build/native/independent comparisons passed on their first execution. Final verifier strengthening also confirms saved expectations predate native output; no native replay was needed for that read-only check.

Focused builds have zero warnings/errors. Successful headless/GPU logs retain the known Windows root-certificate-store diagnostic; no socket was requested. No clean certificate configuration is claimed. No other error/leak diagnostic occurred in these joined logs.

## Reproduction

Run each linked component producer/verifier with its explicit inputs, fresh ignored output and cached dependencies. The joined runner takes explicit Python, Godot, cached-package directory, runtime/terrain fixtures and a fresh ignored output root:

```powershell
& ./poc/round80-tests/Integration/Invoke-Restart.ps1 -Python <cached-python> -CachedPackages <cached-packages> -Godot <godot-console> -RuntimeFixture <runtime-cases.json> -TerrainFixture <surface-fixtures.json> -RunRoot <fresh-local-private-output>
```

It generates independent fixtures before native build, builds the private scene offline, imports, runs headless then GPU, and verifies both outputs. It creates no socket or original execution. Private manifests/receipts/logs remain ignored; curated files contain independently authored contracts and compact hashes only.

## Limits and next action

The three restart components and bounded composition are reusable; the existing Version13 playground launcher still performs one diagnostic session and exits. Wiring these components into its full persistent terrain/actor/input/camera lifecycle is the next separately authorized milestone, with reset clocks/input/latches, fresh root generation and scene-resource teardown tests. Original camera/input/terrain collision, persistent world streaming and live multiplayer remain unverified. Research89/readiness94/function uncalibrated/2626 names/345 packet rows are unchanged and separate.

Next asset probe is the28-byte00AA4350 setter, bounded to destination writes/reference/copy forwarding and any immediate failure child. No allocator/erase/lifetime graph is authorized by this result. All four specialists are idle after Round80; no Round81 or resumed heartbeat is authorized.
