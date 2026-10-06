# Round75 composed root session — 2026-10-06

Question: can one bounded owner compose cadence sampling with at most one offline drain while preserving measured queue frontier and honest failures? Stop after120 actual point-driver updates and targeted lifecycle/prefix negatives.

Input accepted74 frozen4436 public files; manifest6956cc00d990625861fb705822d2be3e98f5f94c6ab827b6964f8a489f6dcc57, archive4b0ca114aaaebe129d4d433424dd9b1d76f30e4a36875715abb88c025e93290d. No EXE input/VA claims. Tools cached Python independent packing/binary32/frozen peer framing; offline .NET8; workspace validator/AST/XML/diff/hash checks.

Observation: frozen outbox owns cadence/sample frontier; frozen drain separately owns bridge/single frame and terminal admission checks. New session owns these children only and records real FIFO count/bytes immediately after cadence, before drain. Lead explicitly requested those receipt fields rather than status reconstruction. [Durable contract](../../docs/SCENE_WORLD_OFFLINE_ROOT_MOTION_SESSION_CONTRACT.md) gives final API/policy.

Findings Confirmed by282 native checks:120 actual frozen point-driver updates with movement/pause/resume/idle,120 independent complete cadence/frontier receipts, seven exact roots/protected frames/ACKs. Bad ACK at sample10 keeps newly queued prefix, terminal session fault and previous complete receipt; actual enqueue failure keeps historical queue/pending prefix with consumed sample clocks. Closed transport declines drain while cadence queues; BP and NotDue each drain only one historical frame. Measured prefixes1/62 and2/124 pass. Constructor second-child failure and disposal preserve borrowers/history. No earlier production change.

Interpretation/compatibility consequence: composed sample can complete before drain fails; owners must not infer atomic rollback from missing combined receipt. Dedicated channel/config/queue association remain caller contracts; new owner verifies no hidden binding. Caller closes world/FIFO/transport separately. Private partial-constructor disposal, zeroing and rare concurrency failures are source-inspected, not dynamically instrumented.

Validation final replay/build zero warnings/errors; expectations before native; links zero broken/private; frozen/public scope/HEAD/index/private-ignore checks. No failed final replay; initial282-count refinement explicitly added failure clock checks after initial280 pass. Source changes finalized after requested queue-frontier receipt amendment. Original assets/EXE/socket/send/live legality unrun; no research/function/protocol denominator change. Shared living docs remain lead-owned.

Next: lead uses composed session with actual1000-step owner trace and independent ACKs, records actual pre-drain FIFO metrics and explicitly disposes all borrowed owners. Seal and idle.
