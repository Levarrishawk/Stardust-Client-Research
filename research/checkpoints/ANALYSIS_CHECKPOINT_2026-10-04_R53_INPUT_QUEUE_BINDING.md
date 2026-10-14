# Round 53: Ground input map selection and normalized deactivation

Date: 2026-10-04. Question: does one selected ground InputMap connect its queue to player controller `+4`, and which normalized caller ends one binding? Stopping condition: close one selected path within 20 candidate xrefs, or preserve the exact partial edge and Unknown queue identity.

## Inputs and method

Executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static PE decoding, instructions, RTTI, vtable pointers, and direct call/data references were used. The executable was not executed. No live contact, asset extraction, native production edit, or semantic registry rename was performed.

Frozen round-53 input: 3,659 files; manifest SHA-256 `0373dac76092ab2cb10fc6311436885f9ecb5cfd62285f37042d57dd912693b4`; ZIP SHA-256 `434824c9edfc15d6390d339551207f7cf5701e934fa337444f09181a2dbc3ca0`; source HEAD `beee58c2fe14fa8c90ae0e2ae2f3f67e91c69e0b`. Worker HEAD remained `65de8c88141071744dc75594be126b3bab3696a5`. The prior worker's actual round-52 final receipt and its four file hashes were checked before overlaying frozen inputs. The overlay preflight and postflight checked all frozen members and destination boundaries.

## Result

The [durable contract](../../docs/PLAYER_CONTROLLER_INPUT_QUEUE_BINDING_CONTRACT.md) contains the canonical evidence and implementation consequences. GroundScene RTTI, its event virtual slot, the `0051AF36 -> 0051AA40 -> 0051AAC0 -> 00AAE2E0` route, and normalized type-8 device-matched family-1 end-binding at `00AAE332 -> 00AAE150` are **Confirmed**. Selector default uses scene `+4C`; selectors 3, 5, and 6 use other map slots. Null maps, mismatched devices, and out-of-range normalized types skip this selected edge.

GroundScene construction clears `+4C`. Teardown clears selected map `+0C` before release and clears scene `+4C`. The global profile template is constructed with a null queue. These observations do not prove which later writer supplies scene `+4C`, nor establish equality of that map's queue and controller `+4`. **Queue instance identity and active assignment remain Unknown.** Physical binding, profile selection/overlay, and runtime corpus occurrence remain Unknown.

The 20-candidate cap was exhausted: eight initial direct calls to constructor/end-binding, nine profile-global operand references, and three direct normalized-handler calls. Following known class bodies established the selected virtual event caller without opening another xref search. Exploratory windows and unproved adjacent-function interpretations were not promoted. Prior producer/consumer semantic correspondence remains separate from instance identity.

## Reproduction and verification

Use [the verifier](../../../SWG-Godot-Client/tools/python/verify_player_controller_input_queue_binding.py) and [independent runner](../../../SWG-Godot-Client/poc/round53-tests/Networking/research_checks.py) with explicit paths and fresh ignored outputs. SHA validation precedes VA use. The positive fixture checks 44 new anchors and bounded body/prefix signatures; it does not repeat the frozen 44-anchor producer audit. Independent coverage is ten byte witnesses, five calls, two RTTI names, three pointers, and 14 rejection cases, including wrong/missing executable, invalid evidence, existing/public output, reparse/link ancestry, and budget overflow. The positive check explicitly requires active assignment false and queue identity Unknown.

Final handoff validation checks repository links/workspace organization, Python syntax and UTF-8/LF, offline locked restore/build of existing Core/SmokeTests, whitespace, all 3,659 frozen hashes, unchanged HEAD and empty index, and only the four assigned additions. No unrelated smoke runtime replay is claimed. Detailed results and exact owned hashes belong in the worker's ignored final receipt; any failed final check must be reported rather than treated as execution verification.

## Resume

Find the non-null writer to GroundScene `+4C`, trace that exact map's queue `+0C` assignment against getter `0062A9B0`, and reconcile acquisition with the proved teardown. `006E1E70` profile lifecycle and `0051BD10` GroundScene receive-message entry are next leads. Use a new explicit candidate budget. Lead integration owns shared status, ledgers, navigation, and registries; no compatibility completion percentage is changed here.
