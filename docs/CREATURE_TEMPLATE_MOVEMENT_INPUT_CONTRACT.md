# One creature template chain: serialized movement inputs

For the pinned effective `object/creature/player/shared_human_male.iff` chain, all five selected movement slots have **inherited deterministic literal encodings**, authored in `object/creature/player/base/shared_base_player.iff`. The root's entries are mode 0; the nearest parent entries are mode 1 with space relative bytes. These are serialized input facts, not final runtime constructor values, active simulation defaults or proof that later writers are absent.

The [research verifier](../../SWG-Godot-Client/tools/python/verify_creature_template_movement.py) resolves only this root and its necessary parents, retaining selected raw encodings, exact F32 bits/decoded numbers, payload offsets and per-source provenance in an ignored private receipt. Public artifacts contain independently authored schema, source identities and compact classifications, not extracted payloads. See the [Round45 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-04_R45_CREATURE_TEMPLATE_MOVEMENT.md). [Round44 initialization](CREATURE_VIEW4_INITIALIZATION_CONTRACT.md) supplies the getter and constructor prerequisite; [the collision-profile contract](OBJECT_TEMPLATE_COLLISION_PROFILE_CONTRACT.md) supplies the accepted one-chain resolver boundary.

## Inputs and overlay policy

Executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Configuration SHA-256: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Configuration has 48 distinct descending numeric TRE priorities. The verifier checks executable/configuration identity before optional analysis imports or address interpretation, pins the static loaders, then resolves archive payloads. Every chain source is identity-checked before its structure is interpreted.

It reuses `cmpa_corpus_inventory.archives`, `verify_textured_mesh_bundle.Index`, `audit_tre_filename_crc.normalize`, `verify_static_mesh_slice.nodes` and `verify_creature_template_profile.parse`. No new archive resolver or production parser is introduced. Index lookup validates raw filename CRC, chooses the first present entry in descending priority and raises on a zero-size tombstone, without lower-priority fallback. Missing parents fail. Normalization lowercases, converts separators and applies the accepted legacy logical path rules. Repeated normalized parents fail as cycles. Full archive indexes are opened as resolver metadata; only the five chain payloads are read, and other template payloads are not surveyed.

| Source, in derivation order | Winning archive / priority | Bytes | SHA-256 |
|---|---|---:|---|
| `object/creature/player/shared_human_male.iff` | `mtg_patch_010_object_01.tre` / 39 | 1974 | `E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243` |
| `object/creature/player/base/shared_base_player.iff` | `mtg_patch_010_object_01.tre` / 39 | 2238 | `3063381CCAE043B41A279D888D4450B16D96D4F75187ED6EE1621067B8ED7ADB` |
| `object/creature/base/shared_base_creature.iff` | `mtg_patch_022.tre` / 51 | 2027 | `05F44EF9C42F0DF9C22091E57CCAEEB462BF5CB671D70E3A3803154A1583E3F5` |
| `object/tangible/base/shared_tangible_base.iff` | `stardust_3_01.tre` / 53 | 1320 | `2C15F794282685FBF47274E379222E6FC22887741851693B2BB9BCC495B6927E` |
| `object/object/base/shared_base_object.iff` | `stardust_3_01.tre` / 53 | 731 | `D4E7366A0ACBA837C4E5B9F7B03DB447166B8EF380FB2D09C9C4270346A83501` |

Five sources total 8,290 bytes. Observed layer versions are root SCOT/0012, STOT/0007, SHOT/0007; base player SCOT/0013, STOT/0009, SHOT/0009; base creature SCOT/0013, STOT/0009, SHOT/0010; tangible STOT/0009, SHOT/0009; object SHOT/0010. These are observed archive tags, not a claim about the original executable's complete version acceptance or diagnostic policy. No whole-archive hash is claimed; configuration, logical entry CRC and exact effective source payload identities bound this result.

## Direct loader evidence

**Observation, Confirmed.** SCOT loader `011B13F0` compares tag `53434F54` at `011B1423`; non-SCOT falls to base loader `012286D0`. DERV is recognized at `011B145C`, its parent path is read and passed to `00B286E0`, and the fetched parent is assigned to template `+18` at `011B14F9`. Parameter count is four raw bytes read at `011B1572`; `011B1584` uses a signed nonpositive-loop exit. Names route to the speed array and slope/water scalar offsets confirmed in Round44.

**Observation, Confirmed.** Float parameter vtable `01609AB8` slot 0 is loader `013FA200`. At `013FA228/013FA232` it reads one mode byte, sign-extends it at `013FA237`, then reads one relative byte at `013FA23B/013FA243` and stores it at parameter `+14`. The unsigned greater-than check at `013FA251` limits the serialized dispatch table to mode byte values 0 through 3. Raw table `013FA468` contains targets `013FA3E0`, `013FA25E`, `013FA296`, `013FA386` in that order.

`00A94F00` copies the requested byte count using `rep movsd`/`rep movsb` and advances the current chunk cursor at `00A94F3F`. It performs no byte swap in this body. The x86 scalar/count interpretations establish little-endian payload fields; IFF envelope lengths remain big-endian through the accepted node walker. The current IFF slice uses contiguous nodes without padding, as in the accepted original template parser.

| Serialized mode | Payload after the two header bytes | Original loader state |
|---|---|---|
| 0 | none | Clears previous storage by old mode; sets mode zero and loaded false at `013FA449/013FA450` |
| 1 | F32LE, four bytes | Loads/stores the scalar, sets runtime mode 1 and loaded true at `013FA279/013FA280` |
| 2 | S32LE count, then each S32LE weight followed by a recursively loaded float parameter | Sets runtime mode 2 and loaded true before reading the count; `013FA304` skips entries for nonpositive counts; child creation/load calls are `013FA327/013FA332` |
| 3 | Two F32LE endpoints, first then second | Reads four bytes twice at `013FA38E/013FA39B`; stores runtime mode 3 and loaded true |

**Interpretation, Confirmed for widths/order and ordinary loaded state.** Mode 2 and 3 getters sample RNG or fallback globals as documented in Round44; no distribution, normalization or runtime sampled value is inferred here. Serialized mode 4 is outside this loader's dispatch even though the runtime getter has a mode-4 branch. For unsupported mode bytes this loader has already stored the relative byte before it returns, without the demonstrated mode-0 clearing path. Do not infer that original unsupported-mode handling equals the research inspector's rejection policy.

**Observation, Confirmed.** Speed starts with a four-byte count; `011B1703` compares it using a signed branch. At most two parameters are stored, with 24-byte destination stride. Extra positive-count parameters are loaded into a temporary at `011B1743` and destroyed at `011B174E`; they do not become speed slot 2 or greater. Nonpositive counts skip parameter loading. The research inspector rejects negative counts and applies explicit budgets; those are inspection guards, not recovered original fatal-error rules.

## The five serialized provenance paths

Each XXXX parameter contains its terminated name followed by its encoded payload. In the root, speed payload is eight bytes: count two plus two absent two-byte parameters. Each root scalar payload is two bytes. In base player and base creature, speed payload is sixteen bytes: count two plus two six-byte literal parameters; each scalar payload is six bytes.

| Selected slot | Root entry | Nearest authored entry | Classification |
|---|---|---|---|
| `speed[0]` to CREO4 field 7 | mode 0, relative `0x20` | base player, mode 1, relative `0x20`, F32LE | inherited deterministic literal |
| `speed[1]` to CREO4 field 11 | mode 0, relative `0x20` | base player, mode 1, relative `0x20`, F32LE | inherited deterministic literal |
| `slopeModAngle` to CREO4 field 8 | mode 0, relative `0x20` | base player, mode 1, relative `0x20`, F32LE | inherited deterministic literal |
| `slopeModPercent` to CREO4 field 9 | mode 0, relative `0x20` | base player, mode 1, relative `0x20`, F32LE | inherited deterministic literal |
| `waterModPercent` to CREO4 field 12 | mode 0, relative `0x20` | base player, mode 1, relative `0x20`, F32LE | inherited deterministic literal |

**Observation, Confirmed.** All five raw paths, mode bytes, relative bytes, exact literal bit patterns and offsets are retained in the private verifier receipt. Base creature also authors five literals, but the nearer base-player entries take precedence. Tangible/object sources lack SCOT movement parameters. **Interpretation, Confirmed:** serialized inheritance resolution has five determinate winning literal records; the absent root records do not supply numeric zero. **Consequence:** a future compatibility implementation can preserve these authored inputs and provenance without importing asset payloads into curated Git data.

Round44 getters use `+`, `-`, `=`, `_` as add, parent-minus-current, percentage increase and percentage decrease. Space takes the local sampled/literal operand. An absent parameter's relative byte is not applied as an override of the inherited loaded parent's relative byte. All selected authored records in this chain use space, so no relative expression or sampled branch is needed to classify their serialized provenance.

## Research guards, validation and limits

The inspector is research-only: one pinned root, at most 16 chain members, 1 MiB per template, 64 KiB per selected parameter stream, parameter nesting below 16 and nonnegative counts at most 64. It rejects truncation, trailing selected bytes, ambiguous payload locations, unsupported serialized modes and budget violations. Existing hierarchy parsing validates PCNT, parameter uniqueness and agreement among layer derivation paths. These guards do not prove original malformed-input safety. Nonfinite literal bits are retained as raw structure without claiming an executable float result.

The static audit passes 50 new loader anchors, one serialized-mode table and one float-loader vtable, plus the accepted Round44 prerequisite. The [synthetic runner](../../SWG-Godot-Client/poc/round45-tests/Networking/research_checks.py) passes 40 checks covering literal order/provenance/override, absent versus zero, relative and sampled classification, nested weighted layout, extra/short speed arrays, truncation, duplicate/count failures, missing/masked/cyclic parents and fresh-process executable/configuration/private-receipt gates. Synthetic tests use independently authored bytes; they do not execute the original or exercise an added production parser.

Reproduce with `verify_creature_template_movement.py --exe EXE --packages STATIC_PACKAGES --server-root SERVER --root ASSET_ROOT --configuration CONFIG --receipt PRIVATE_RECEIPT`. The inspected logical template path is fixed to human male; the CLI asset-root argument selects the filesystem directory containing the configured archives, and no template enumeration option exists. A second run must agree on complete chain metadata, selected raw records and five provenance results.

Final runtime values remain unclaimed: actual template installation/use, active RNG/global state, x87 angle conversion bit equivalence, version error behavior and later writes remain independent questions. The Round44 customization/event transitive paths remain open. Collections/listen-to initialization and full observer admission are unchanged. Next bounded probe is still the ranged-int customization virtual `+20`, child callback propagation `007D6C10`, or event dispatch `005F1CD0`; this serialized-input question no longer needs a broader template sweep.

## Round46 accepted compatibility increment

The [owned profile](SHARED_CREATURE_MOVEMENT_INPUT_PROFILE_CONTRACT.md) now preserves five selected slot modes, raw encodings, nearest authored provenance and necessary source identities.352 synthetic fixtures/573 independent checks and the pinned original five-source chain agree. Only finite mode1 space literals are operational; sampled/relative/absent inputs remain tagged. No runtime default, angle conversion or replicated seed is inferred.

## Round49 compatibility increment

[Original-resource sourced preview](SCENE_WORLD_SOURCED_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md) passes authored/received headless and authored GPU joins through the actual original recipe closure with zero extra movement reads. [Current raw inputs](SCENE_WORLD_CREATURE_MOVEMENT_CURRENT_INPUT_CONTRACT.md) retain explicit constructor provenance and exact received overrides without store seeding; [explicit stop and sourced input](SCENE_WORLD_SOURCED_INPUT_HOST_CONTRACT.md) own the modern direction/clock lifecycle. Original full arbitration, modifier/vehicle semantics and units remain separate.
