# Agent instructions for Stardust client research

These instructions apply to the entire repository. They guide agents performing reverse-engineering research, maintaining the evidence record, or preparing bounded compatibility prototypes. A more specific future `AGENTS.md` may add constraints but must not weaken the legal, evidence, or publication boundaries here.

## Mission

Build a reproducible, evidence-backed specification for a clean native 64-bit client that interoperates with the Stardust/Core3 server and consumes the expected legacy data formats. This repository's immediate product is knowledge: verified behavior, schemas, invariants, risks, and testable implementation requirements.

Keep these measures separate:

- **Research coverage:** how much of the compatibility surface is understood.
- **Implementation readiness:** whether a replacement can be built from that understanding.
- **Function coverage:** how much executable code has a stable semantic label.
- **Protocol coverage:** whether reachable server/client message boundaries are documented.

Never combine their denominators into one unsupported completion claim.

## Required orientation

Before substantive work:

1. Read [START_HERE.md](START_HERE.md) and the latest session report it names.
2. Read the relevant durable document in `docs/` and search `research/checkpoints/` for prior work.
3. Check [ANALYSIS_STATUS.md](ANALYSIS_STATUS.md), [COMPREHENSIVE_ANALYSIS_LEDGER.md](COMPREHENSIVE_ANALYSIS_LEDGER.md), and [UNKNOWN_FUNCTIONS.md](UNKNOWN_FUNCTIONS.md) for current terminology and open questions.
4. Inspect relevant tools and curated CSV rows before inventing another script, schema, identifier, or count.
5. Confirm the input executable matches the documented SHA-256 before relying on virtual addresses.

Use `rg` and `rg --files` for discovery. Search by concept, literal string, address, packet name/opcode, IFF tag, RTTI name, and known caller/callee; one vocabulary is rarely sufficient.

## Non-negotiable boundaries

- Default to static analysis. Dynamic execution, live-server contact, packet capture, fuzzing, or mutation of external systems requires explicit task authorization and a bounded safety plan.
- Do not bypass DRM, authentication, licensing, encryption used for access control, or other technical protections.
- Do not expose credentials, account data, private endpoints, or unrelated personal data.
- Never add `Stardust.exe`, original game assets, raw bulk decompiler output, Ghidra databases, downloaded toolchains, or generated proprietary corpora to Git.
- Keep sensitive and redistributability-uncertain material under ignored `local-private/` paths.
- Treat third-party source, emulator source, names, and documentation as correlation evidence, not proof of client behavior.
- Do not commit, push, create a remote, publish, or change repository visibility unless explicitly requested.

If requested work crosses a boundary, stop and explain the exact additional authorization or input required.

## Investigation method

### 1. Define a bounded question

Use a falsifiable question and an explicit stopping condition. Good forms include:

- What bytes does this handler consume, in what order, and under which discriminator?
- Which factory accepts this IFF tag/version, and which runtime object owns the result?
- What state transition causes this message to be sent or accepted?
- Which thread produces, transfers, and destroys this object?
- Is this path reachable in the baseline corpus or merely tolerated by the executable?

Avoid open-ended “analyze subsystem” work unless the task explicitly asks for a survey.

### 2. Build an evidence chain

Prefer evidence in roughly this order:

1. Direct executable instructions/data, RTTI, vtables, imports, and cross-references.
2. Exact serializers/deserializers, captures, or byte-level test vectors.
3. Original asset bytes and effective overlay/corpus results.
4. Client call-site, ownership, and lifecycle relationships.
5. Server source at the client/server boundary.
6. External documentation or analogous projects as leads only.

One source may establish a fact; two independent sources are preferred for field semantics or lifecycle claims. Preserve contradictory evidence instead of forcing a conclusion.

### 3. Separate fact from interpretation

For every material conclusion, record:

- **Observation:** the bytes, instruction, string, call, asset record, or source line.
- **Interpretation:** the most likely role.
- **Confidence:** Confirmed, Strongly inferred, Tentative, or Unknown.
- **Implementation consequence:** what must be preserved, may differ internally, or can be deferred.

Use “unknown” for semantics, not for raw structure already established. A known field width does not make its meaning known.

### 4. Close the loop

Before declaring a question closed, check callers, callees, error paths, version/discriminator handling, ownership/destruction, and at least one real corpus or protocol occurrence when available. Document unreachable, dormant, legacy, malformed-input, and fallback behavior explicitly.

## Subsystem strategies

### Executable and function analysis

- Treat addresses as VAs for the documented baseline unless labeled otherwise.
- Name provisional functions `subsystem_role_VA_XXXXXXXX`, using eight uppercase hex digits.
- Keep one canonical semantic name per address in `data/curated/provisional_names.csv`.
- Never silently repurpose a name. Record the former interpretation and rationale, then update references consistently.
- Combine callers, callees, RTTI/vtables, strings, field offsets, imports, and construction/destruction evidence. Do not name from one nearby string alone.
- Distinguish compiler thunks, library code, duplicate wrappers, and application behavior.
- Keep bulk decompiler output in `local-private/`; curated docs should contain independently written descriptions, addresses, and only small necessary excerpts.

### Network and protocol analysis

- Separate SOE transport framing/reliability/transforms from application-message codecs.
- State byte order, signedness, width, string encoding, length ownership, nesting, and malformed-input behavior.
- Distinguish login, connection-server, and zone-server phases and identify where a packet is legal.
- Trace both directions when possible: server emitter/parser, wire representation, client dispatch, handler, and resulting state change.
- Record object-controller envelopes separately from inner payloads.
- For baselines/deltas, identify object family, view, field order/index, container update rules, and failure policy.
- Update packet maps, coverage docs, replication maps, and `data/curated/server_packet_gap_matrix.csv` together when their shared boundary changes.
- Prefer deterministic byte fixtures and positive/negative tests over prose-only codec claims.

### Asset and corpus analysis

- Preserve IFF form/chunk tags, versions, field order, alignment, optional branches, and rejection/fallback behavior.
- Distinguish parser acceptance from formats present in the effective installed overlay.
- Respect archive priority, path normalization, CRC lookup, redirects, and tombstones.
- Report input roots, precedence, sample counts, version distributions, and parse failures for reproducibility.
- Do not promote extracted proprietary content into curated Git data; retain aggregates and independently authored schemas.
- Test coordinate systems, transforms, winding, tangent conventions, units, and packing conversions at engine boundaries.

### Runtime architecture and lifecycle

- Trace creation, registration, update order, callbacks/events, thread affinity, teardown, and failure recovery.
- Identify ownership: borrowed/retained references, cache residency, queue transfer, cancellation, and destruction order.
- Distinguish load-time models, runtime instances, and render-submission records.
- For asynchronous systems, document producer/consumer threads, synchronization, completion ownership, shutdown, and stale-work rejection.
- For UI, separate data/model managers, message queues, mediators/widgets, and presentation-only behavior.

### Server correlation

- Use Stardust/Core3 source to find emitted packets, expected requests, configuration gates, and authoritative gameplay state.
- Cite the exact repository path and line or symbol.
- Label server-only behavior. Do not infer client layouts, types, or control flow from analogous server classes.
- Reconcile server changes against the dated executable baseline; they may evolve independently.

### Prototype and replacement-client work

- Keep protocol, state, asset, and simulation logic engine-neutral. Engine adapters translate input, rendering, audio, and lifecycle concerns.
- Preserve compatibility at wire and asset boundaries; internal layouts need not reproduce legacy memory.
- Implement from durable specifications and byte fixtures, not copied decompiler pseudocode.
- Prefer the smallest vertical slice that confirms or disproves one risk.
- Clearly label synthetic fixtures. The PoC envelope is not recovered Stardust protocol.
- Add negative tests for truncation, invalid sizes/tags/versions, impossible transitions, and deterministic cleanup.

## Where work belongs

- `docs/`: durable consolidated specifications useful during implementation.
- `research/checkpoints/`: one dated bounded investigation with evidence, confidence, and open questions.
- `research/sessions/`: multi-checkpoint consolidation and a precise resume point.
- `COMPREHENSIVE_ANALYSIS_LEDGER.md`: chronological material progress; do not erase history.
- `ANALYSIS_STATUS.md`: current conclusions, metrics, readiness, and prioritized gaps.
- `UNKNOWN_FUNCTIONS.md`: address observations and stable-name reconciliation.
- `data/curated/`: canonical normalized machine-readable registries only.
- `local-private/generated-evidence/`: regenerable detailed inventories and proprietary-derived output.

Do not create a checkpoint for trivial editorial work. Do not bury durable facts only in a session report. Update every affected canonical artifact without duplicating large passages.

A substantive checkpoint should include the date and question, input identity and tools, exact evidence, findings versus interpretation, confidence, compatibility consequences, unresolved questions, and a concrete next probe.

## Change discipline and validation

- Preserve user changes and unrelated work; the tree may be intentionally dirty.
- Keep edits focused and avoid incidental reformatting of ledgers or generated CSVs.
- Use relative Markdown links within the repository.
- Preserve historical dates/counts in dated reports; update current counts only in living documents.
- Generate counts mechanically from canonical data where possible.
- Preserve CSV schema, normalization, ordering convention, and one-row-per-key invariants.
- Put reusable analysis code under the appropriate `tools/` language directory. Prefer explicit input paths and print enough input identity for auditability.
- Generated output is not automatically canonical. Promote only compact stable data with a documented consumer.

Run checks proportional to the change. The baseline validator is:

```powershell
python .\tools\python\validate_organized_workspace.py .
```

Also run the producer/verifier for each changed registry, fixture, parser, or PoC component. For protocol claims, retain deterministic positive and malformed-input fixtures. For documentation-only work, at minimum validate links and inspect `git diff --check` plus the focused diff.

If a check cannot run because a private input or local toolchain is absent, say so precisely. Do not claim execution-based verification from inspection alone.

## Handoff checklist

1. Confirm every new claim has evidence and confidence.
2. Reconcile durable docs, checkpoint, ledger/status, unknown-function entries, and curated data where applicable.
3. Run relevant validation and report failures honestly.
4. Inspect `git status --short`; do not alter or include unrelated files.
5. Ensure curated files neither link to nor embed content from `local-private/`.
6. State what changed, what was verified, remaining uncertainty, and the exact next action.

## Definition of done

A research task is complete when another agent can reproduce the result, distinguish evidence from inference, understand its compatibility impact, find the canonical conclusion, and resume from a precise open question without repeating the investigation.

An implementation task is complete when its bounded behavior lives outside inappropriate engine-specific layers, is validated against authoritative fixtures and negative cases, is documented at the compatibility boundary, and leaves no generated or private material staged for publication.
