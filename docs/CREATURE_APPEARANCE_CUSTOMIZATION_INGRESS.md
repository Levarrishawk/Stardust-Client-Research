# Creature appearance customization ingress

Round19 Networking, October2,2026. The exact replication envelope and field4 byte storage are already established in the [schema map](REPLICATION_SCHEMA_MAP.md), [tangible checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_TANGIBLE_VIEW3_REPLICATION.md) and [CREO baseline codec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView3BaselineCodec.cs). This investigation connects those boundaries to the original packed-data reader and identifies the remaining typed-value bridge. It adds no production decoder or original player values.

## Outer wire and current storage

CREO:3 inherits TANO:3 field4. The baseline reads it after complexity, StringId, Unicode custom name and U32 volume; a delta carries U16 index4 followed by the same byte-string representation. Outer string length is U16 LE; FFFF selects an additional U32 LE byte length. It counts bytes only, without a trailing NUL or Unicode decoding. Empty, embedded zero, high bytes and FF escape pairs belong to the payload. The outer replication envelope separately owns payload size, count, tag and view.

Direct executable observation: baseline00641030 reads two bytes at00641085..94, branches onFFFF at00641099..B5 to read four more, builds/assigns the length-bounded string at006410BA..D9 and advances the input by that length at006410EA..F1. It compares old/new lengths and byte contents at00641101..18; changed data calls0063F880 at0064111D. Delta00641180 repeats this boundary and comparison, calling0063F880 at0064126D. The decoder's string member is relative +0C within this auto-delta wrapper, not a proposed x64 object layout. Confidence: Confirmed static instructions on the pinned executable.

Current CreatureView3BaselineCodec stores an owned ReplicationByteString with exact bytes and UsesExtendedLength, including noncanonical extended-small/extended-empty forms. CopyBytes returns a clone; encoding reconstructs the same prefix form. Configured string/payload/decoded aggregate budgets and truncation/tail checks apply to the whole baseline. No packed customization validation occurs. A structurally valid outer baseline with invalid inner escape bytes is therefore retained, not interpreted as a successful appearance decode.

Generic ReplicationEnvelopeCodec preserves complete opaque field bytes for TANO baselines and CREO deltas. It does not parse TANO field4. As of the original Round19 ingress probe, scalar/mixed CREO parsers covered11..16/11..17 and components5 while rejecting field4; typed world state then exposed only baseline customization. Rounds21/22 now add ordered applied raw journals and projected appearance sources, with a bounded4/11..16 atomic state union. Mixed4+5/17 and inherited TANO remain open. Opaque bytes alone do not establish successful packed projection.

## Original callback and packed reader

0063F880 returns immediately for an empty supplied string (0063F887..8F), so an empty outer change is not evidence of clearing every live customization variable. For nonempty bytes it obtains the object's associated property via00B39870/00B23EE0, obtains its data via00B399A0, calls00B33020 with the string at0063F8B2..B5, then calls00B33780 and owner vtable+3C at0063F8BA..C5. Property/data semantic names are Strongly inferred from the customization consumers; the exact call sequence and empty guard are Confirmed. Property absence also skips application.

00B33020 performs an unescape pass into owned temporary bytes, then00B33E30 dispatches on the first decoded byte. Version1 calls00B33F70; an empty decoded buffer and unsupported version have explicit diagnostic/failure paths. Successful record parsing calls00B33730. These calls establish the reaction boundary; they do not by themselves establish every notification listener.

| Original observation | Meaning and confidence |
| --- | --- |
| 00B330F0..F5 compares each raw byte withFF | Raw non-FF bytes, including00, are appended. Confirmed. |
| 00B33136..46 selects escape1/2/3 | FF01 appends00 at00B331E4..F8; FF02 appendsFF at00B331C0..DA. FF03 contributes no decoded byte. Confirmed. |
| 00B3312E..30 / 00B3314C..4F | Lone trailingFF and nonterminal EOS have diagnostics. Unknown escape goes to diagnostic branch00B333E1. Do not infer transactional rejection from a warning. |
| 00B33EC7..CD | Decoded version must be1 for this record path. Confirmed. |
| 00B34000..10 | Count is decoded U8; initial length preflight is at least2+2*count. Confirmed; not a proof of complete bounds validation for every wider value. |
| 00B340D5..F2 | ID is byte&7F; high bit chooses value byte width1 or2. Confirmed. |
| 00B340F5 / 00B3410C | ID resolves through00B54D50; full resolved name is looked up through00B32890. Confirmed. |
| 00B3417C..8C | Found variable receives decoded buffer, offset and width via vtable+10; setter result checked. Confirmed. |
| 00B34177..7A / 00B34197..A8 | Missing live variable is diagnosed/skipped, still advances by its value width; loop proceeds in supplied order. Confirmed. |
| 00B341AE..D8 | After count records, temporary name is released and success returned; no final decoded-tail equality test on this path. Confirmed. |

Record reader invokes variable setters inside the loop. A later unknown-ID/setter failure can follow earlier setter calls; rollback is not established. The inherited architecture document's transactional rejection recommendation must be read as a replacement constraint, not a proven original failure policy. Similarly, FF03 is a raw escape/EOS pair removed by the unescaper, not two decoded record bytes. A strict modern parser can require a final EOS and reject trailing/unknown escapes, but that behavior would be explicitly modern policy.

The high ID bit directly proves **two-byte value width**, independent of namespace. [Round20 concrete setter evidence](PACKED_CUSTOMIZATION_CONTRACT.md#concrete-original-setter-evidence) now confirms U8/LE S16 conversion for BasicRangedIntCustomizationVariable and PaletteColorCustomizationVariable, whose local setters retain invalid attempts rather than clamp. Other types/live mount and linked-variable context remain separate.

## ID-to-consumer key bridge

00B54D50 subtracts1 from ID and bounds-checks against the installed vector at00B54D56..73, then obtains the corresponding name and copies it to its output. ID0 is not a valid installed row. [CIDM/ACST research](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_CUSTOMIZATION_MANAGER_TABLES.md) already proves CIDM0001 DATA as repeated U16 LE ID plus NUL ASCII name,121 unique consecutive installed IDs1..121, with exact case/spelling preserved. It also proves asset-dependent ranged/palette variable creation and direct/inherited expansion. No new asset extraction/count is claimed here.

Existing [shared customization evidence](SKELETAL_CUSTOMIZATION_SELECTION_CONTRACT.md) proves shader/blueprint consumers concatenate `/private/` when factory flag is true and `/shared_owner/` when false, followed by the exact template variable name. Therefore the confirmed identity bridge is:

`packed ID -> installed CIDM full name -> existing live typed variable -> full-name match to consumer prefix + exact VariableName -> (VariableName, factoryFlag) integer snapshot`.

The ID is not the palette index and its high bit is not factoryFlag. Never derive the consumer Boolean from a wire sign/width bit. For names rooted in those two namespaces, remove exactly the matched prefix to form the explicit consumer key, preserving case and suffix bytes. Other namespaces require separate consumers; do not silently classify them. CIDM's known ID118 spelling `private/alternate_shader_blade` lacks a leading slash and must not be normalized automatically.

Round20 closes typed conversion/getters/local range checks for those two classes. Variable construction/mount context and declared per-variable ranges still gate translation into a live integer snapshot. Current BlueprintCustomizationKey/ShaderCustomizationKey APIs and face/full appearance policies consume explicitly supplied inspection values. Actual original player appearance remains Unknown.

## Server correlation and limits

Local Stardust-3 HEAD9a8529ef8291efc5ca1b41d9927f0e88d64b79bb:

- `MMOCoreORB/src/server/zone/packets/creature/CreatureObjectMessage3.h:13` invokes TangibleObjectMessage3 with CREO tag/count18.
- `MMOCoreORB/src/server/zone/packets/tangible/TangibleObjectMessage3.h:43..45` obtains customization and insertAscii(app); delta sibling:23..26 emits index4 via addAsciiUpdate.
- `MMOCoreORB/src/server/zone/objects/tangible/TangibleObject.idl:612..613` delegates getCustomizationString to customizationVariables.getData.
- `MMOCoreORB/src/server/zone/objects/scene/variables/CustomizationVariables.cpp:81..122` emits version/count, keyed values, zero/FF escapes and finalFF03. Empty map emits empty string. Negative reference values use flagged ID, low byte andFF02 high byte; positive values are cast to one byte. Do not generalize this encoder to arbitrary int16 values.
- Same file:124..225 parses escapes/records and checks final raw EOS. CountFF returns early; version mismatch logs; catch clears the server collection. These are server-specific policies, not proof of original client rollback/count handling.
- `MMOCoreORB/src/templates/customization/CustomizationIdManager.cpp:66..87` loads ID/name maps from CIDM.
- `MMOCoreORB/utils/engine3/MMOEngine/src/system/net/Packet.h:116..133` distinguishes strlen(char*) from length-preserving String/explicit-length insertAscii. `DeltaMessage.h:78..80` calls insertAscii(val.toCharArray()), so embedded NUL would truncate that server delta path. Client acceptance and server canonical emission are separate. Canonical escaped customization normally avoids zero value bytes; raw embedded-NUL fixtures test preservation only.

Source hashes and baseline/tool identities are recorded in the [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-02_R19_CUSTOMIZATION_INGRESS.md). Third-party/server correlation does not replace client instructions.

## Verification and next probe

[Owned synthetic runner](../../SWG-Godot-Client/poc/round19-tests/Networking/Networking.csproj) and [independent producer/oracle](../../SWG-Godot-Client/poc/round19-tests/Networking/peer.py) exercise the existing production outer codecs, with no duplicated inner parser. Six cases cover empty, escaped inspection bytes, embedded zero/high/invalid-escape bytes, all256 octets, extended-small and extended-empty. Exact CREO baseline round trips, copied input/output clearing, opaque delta/TANO round trips and947 truncation/tail/string-budget/unsupported-field probes pass. These tests prove outer preservation, not original inner execution or a typed TANO parser.

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r19'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round19 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round19/NuGet.Config
& $python poc/round19-tests/Networking/peer.py --generate local-private/round19/peer.json
dotnet restore poc/round19-tests/Networking/Networking.csproj --configfile local-private/round19/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round19-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round19/peer.json local-private/round19/export.json
& $python poc/round19-tests/Networking/peer.py --peer local-private/round19/peer.json --verify local-private/round19/export.json
```

Next implementation probe: current-incarnation rawfield4 delta/state, then CIDM plus explicit variable/range context to appearance snapshots. Preserve empty bypass, ordered duplicates and local reject-with-old-value behavior; strict atomic malformed rejection is modern policy. [Round20 joined acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R20_CUSTOMIZATION_INTEGRATION.md) keeps supplied rendering separate. Research89%/readiness94%,2602 stable names and345 packet rows unchanged.

## Round21 integrated boundary

Round21 implements bounded CREO:3 field4-only ingress over current finalized incarnation, ordered raw journal and declared-range projection. Raw values and baseline history remain distinct from projected display values. Fullzero seed/ranges are supplied inspection context; mixed fields and ACST/mount/default recovery remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R21_WIRE_CUSTOMIZATION_INTEGRATION.md).


## Round22 applied union and directory

ACST owned descriptors and original targeted queries pass. Current raw/scalar/world journal candidates commit atomically before downstream packed projection. Authored palette/default descriptors are distinct from live values and from current supplied consumer bounds. [Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_CUSTOMIZATION_SCALAR_INTEGRATION.md).

## Round23 integrated boundary

The supported CREO:3 union4/5/11–17 now stages raw customization, scalars, components, wounds and pending world state together. Original selected ACST/CIDM context supplies exact ranges and authored defaults through five already owned palettes. Main independent native, headless/GPU, motion and held-container gates pass. Broader fields/views, live mounts/player seeds and graphical attachment/wound behavior remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R23_FULL_CREATURE_INTEGRATION.md).

## Round24 integrated boundary

Supported CREO3 fixed common fields0/3/6–10 join the existing union. Current common state survives older ingress; visibility uses that overlay while hidden appearance work commits. Actual delta headless/GPU/motion and separate held-baseline visibility proofs pass. Names1/2, TANO, gameplay meanings and live interoperability remain open. STF variant1 exact owned lookup passes two original tables; locale/cache/fallback is deferred. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_EXTENDED_CREATURE_INTEGRATION.md).

## Round25 named creature increment

The supported ordered CREO3 union now covers indices0–17, including raw StringId/custom names. Current name overlays and variable charge survive older ingress. Bounded original STF1 resolution drives an optional caption; actual delta headless/GPU and separate held-baseline name proofs pass. Explicit modern name precedence/directory/display policies do not establish original UI/locale behavior. Other families/views and live interoperability remain open. [Evidence and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R25_NAMED_CREATURE_INTEGRATION.md).
