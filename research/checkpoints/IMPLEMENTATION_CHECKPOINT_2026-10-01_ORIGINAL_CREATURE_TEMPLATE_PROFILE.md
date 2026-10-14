# October 1, 2026 — Original creature template profile

## Question and stopping condition

Can unchanged object IFF and inherited sources produce a reproducible deterministic actor collision profile and appearance path? Stop at owned engine-neutral decoding, negative cases and independent original-byte agreement. Do not claim appearance instantiation or original movement simulation.

## Inputs and exact evidence

Executable SHA-256 remains `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Read-only Ghidra confirms float constructor/loader `013FA000/013FA200`, vtable `01609AB8`; string constructor/loader `013FA9B0/013FADA0`, vtable `01609B48`. Field loader/getter and fallback geometry evidence is consolidated in the [durable profile contract](../../docs/OBJECT_TEMPLATE_COLLISION_PROFILE_CONTRACT.md) and [previous trace](ANALYSIS_CHECKPOINT_2026-10-01_CREATURE_COLLISION_FALLBACK.md). No original execution.

Original configured root is `object/creature/player/shared_human_male.iff`, priority39,1,974 bytes, SHA-256 `E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243`. Configuration SHA-256: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`;48 descending mounts. Five source identities and detailed raw trees remain ignored private evidence.

## Implementation and findings

`SharedObjectTemplateDecoder` owns bounded SCOT/STOT/SHOT layers and parameter payloads, validates count/duplicates/hierarchy/derivation and preserves opaque unrelated types. `SharedCreatureTemplateProfileReader` follows normalized inheritance with source/provenance accounting and explicit cycles, budgets, missing/masked sources and identity failure. Only inherited or literal floats with space modifier and inherited/literal strings are supported. Random/range/relative modes and unresolved required fields reject.

Five originals total8,290 bytes. Eight resolved fields agree independently: step height0.5, collision height1.8 F32, radius0.5, X/Z offsets0, length1.5, warp tolerance17 and `appearance/hum_m.sat`. Ordinary local fallback sphere at scale1 is center `(0,1,0)`, radius0.5, lowest point0.5. Step height provenance is base player; other collision dimensions are base creature; appearance is the root human file.

Observation: this is original byte/provenance agreement. Interpretation: it supplies an authoritative authored input for a bounded replacement actor shape. Confidence: Confirmed structure and profile; controller activation and global body/floor policy remain Unknown. The existing preview actor box is not changed by this milestone.

## Validation and reproduction

Full native smoke passes new override/provenance/ownership/scaled-sphere tests and negative pin/cycle/mask/depth/truncation/duplicate/nonfinite/mode/modifier gates. Original probe:

`dotnet run --no-restore --project poc/Stardust.Core.SmokeTests -- --creature-template-profile ROOT --configuration CONFIG --member object/creature/player/shared_human_male.iff --expected-sha256 ROOT_SHA`

Redirect detailed output privately. Independently verify with `verify_creature_template_profile.py ROOT CONFIG NATIVE_OUTPUT --configuration-sha256 CONFIG_SHA --expected-root-sha256 ROOT_SHA`. It checks all source priority/byte/SHA identities, parent links, eight field values/provenance and sphere geometry. Prototype host build passes with zero warnings/errors. Documentation validation follows promotion.

## Next milestone

Build a separately labeled modern sphere/contact query from this profile, including floor support, door bounds and transforms, before considering actor-host adoption. Original movement controller reachability remains a separate static question. No canonical names or research/protocol scores change.
