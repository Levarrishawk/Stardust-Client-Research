# Caller-authored object template catalog profile

R9-ASSETS-01, October 2, 2026. [ObjectTemplateCatalogProfile](../poc/Stardust.Core/Assets/ObjectTemplateCatalogProfile.cs) adds a strict bounded JSON boundary around the accepted immutable [ObjectTemplateCatalog](../poc/Stardust.Core/Assets/ObjectTemplateCatalog.cs). It reads caller configuration and returns that existing catalog type. Existing creature/static readers and catalog semantics are unchanged. This schema is independently authored modern configuration, not a recovered original global template table.

## Version 1 schema

The root has exactly two case-sensitive properties, `version` and `entries`. Version must be decimal integer `1`. Entries must be an array; an empty array is valid. Every entry has exactly these five properties:

| Property | Required value |
| --- | --- |
| `templateCrc` | Decimal JSON uint32, equal to the normalized object path's legacy checksum |
| `objectPath` | Bounded printable ASCII logical `object/*.iff` reference with a nonempty basename |
| `objectSha256` | Exactly 64 ASCII hexadecimal characters, explicit object-source pin |
| `appearanceSha256` | Exactly 64 ASCII hexadecimal characters, explicit appearance-source pin |
| `recipe` | Exact symbolic string `creatureDetail0` or `staticAppearance` |

There are no defaults, extra properties, alternate numeric recipe IDs, guessed pins, appearance-path fields or fallback entries. Root and entry property order is immaterial on input. Unknown, repeated or missing properties reject. Property names are compared after JSON string decoding: an escaped spelling of an existing property is accepted once, and a duplicate such as `objectPath` plus `object\u0050ath` rejects. Case aliases reject.

Numbers must have only ASCII decimal digits in their JSON token and fit uint32. Negative values including `-0`, fractions, exponent syntax, quoted numbers, booleans, nulls and other types reject. JSON syntax additionally rejects leading-zero integer tokens, comments, trailing commas, trailing documents and truncation. `templateCrc` value zero is structurally a uint32 but still must match its object's checksum. Version values other than 1 reject.

The source is UTF-8 bytes. Strict decoding rejects malformed, overlong and truncated byte sequences. BOM-prefixed input is not stripped and rejects at the JSON boundary. Paths/pins/recipes and recognized property names must meet their respective ASCII/string constraints after decoding; malformed lone-surrogate strings reject. Ordinary JSON escapes representing valid ASCII names are supported.

Before legacy normalization, raw object names reject literal `.` or `..` path segments, including backslash-separated or JSON-escaped spellings. Other accepted legacy normalization remains: strip leading slashes, fold ASCII case, replace backslashes and collapse repeated separators. Trailing separators stay present and invalidate the `.iff` suffix. This is archive logical-path processing, not operating-system resolution. It intentionally tightens the older catalog's raw leading-dot-prefix behavior without changing that catalog.

The accepted catalog constructor verifies normalized `object/` prefix, `.iff` suffix, nonempty filename, exact CRC, valid pins and supported recipe; pins normalize to uppercase. Duplicate normalized paths, repeated/conflicting CRCs and distinct-path checksum collisions reject the whole profile. Literal dots and every rejection/limit here are **modern acceptance policy**, not recovered original malformed-input behavior.

## APIs, ownership and budgets

`Decode(ReadOnlySpan<byte> source, ObjectTemplateCatalogProfileLimits? limits = null)` validates limits and byte size, checks UTF-8, parses bounded JSON, validates all entries, then constructs the accepted catalog atomically. No catalog is returned on failure, including a valid earlier entry followed by an invalid later entry. It stores no JsonDocument, source buffer, resolver, token or file stream. JsonDocument is disposed on success and failure; the catalog owns immutable record/string snapshots and a read-only dictionary. Mutating source bytes after return cannot change it.

`Encode(ObjectTemplateCatalog catalog, ObjectTemplateCatalogProfileLimits? limits = null)` reuses the catalog entry/path gate, checks required output depth, writes compact UTF-8 and enforces final output bytes before returning an independently owned byte array. Output has no BOM or trailing newline. Root property order is `version`, `entries`; entries sort by ascending unsigned CRC; each entry's property order follows the table above. Only necessary JSON escaping is used for these ASCII values; printable HTML-significant characters remain ordinary JSON string content. The resulting JSON artifact is not HTML. Canonical output is independent of insertion order, input property order, aliases, pin case and whitespace. Re-encoding decoded output is byte-identical. Mutating one returned output buffer cannot alter the catalog or later encodings.

| Limit | Default | Allowed caller range |
| --- | --- | --- |
| UTF-8 source / encoded-output bytes | 1,048,576 | 1..16,777,216 |
| JSON container depth | 4 | 1..16 |
| Entries | 256 | 1..4,096 |
| Sum of normalized object-path characters | 262,144 | 1..4,194,304 |

Individual raw paths retain the accepted 1,024-character printable ASCII bound. The usual nonempty schema has three container levels: root object, entries array, entry object. Empty entries need two. A caller may choose smaller depth and receive rejection. Entry and path ceilings match the existing catalog ceilings. Byte limits count all UTF-8 input, including whitespace, and actual encoded output. These are acceptance/retained-output limits, not a peak-allocation promise; JSON input is copied within its byte gate, and encoded length is checked after bounded serialization.

Invalid limit options throw `ArgumentOutOfRangeException` before parsing/writing. Null encoder catalog throws `ArgumentNullException`. Malformed input, schema/entry mismatches and profile budget failures throw `InvalidDataException`; inner parser/catalog errors are retained. Empty catalogs have no lookup fallback. Decode/encode perform no filesystem, asset, archive, network or engine access; callers control file reading and resource-reader lifetime.

## Authored public profile and evidence

[scene-object-catalog.json](../poc/preview-profiles/scene-object-catalog.json) contains exactly the two already pinned recipes. It contains metadata only, no original asset content.

| Recipe | Object path / CRC | Object SHA-256 | Appearance SHA-256 |
| --- | --- | --- | --- |
| `creatureDetail0` | `object/creature/player/shared_human_male.iff` / `2937962913` | `E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243` | `951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C` |
| `staticAppearance` | `object/tangible/terminal/shared_terminal_mission.iff` / `2907007746` | `E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281` | `74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021` |

**Observation / confidence:** These exact mappings/pins were already established by the [creature resource contract](SCENE_CREATURE_RESOURCE_CONTRACT.md) and [static resource contract](SCENE_STATIC_RESOURCE_CONTRACT.md), **Confirmed** bounded source/implementation evidence. The profile's association and symbolic recipe selection are deliberate caller configuration. **Implementation consequence:** consumers can reuse this file to construct the same accepted catalog without embedding an implicit global lookup table.

The authored file SHA-256 is `B945D2D4695FF37D54343F11A7DF3539A5AB36A2E5D13246969B04F92657492D`. Its canonical encoding is 598 bytes, SHA-256 `714A624F8BCE5FE9834E202C675C16314F5C329E4B98D6E99669B60073A0AACB`, with 96 normalized path characters. Canonical entry order is terminal, then human.

[Native scenarios](../poc/Stardust.Core.SmokeTests/ObjectTemplateCatalogProfileScenarios.cs) pass 12 acceptance cases, 126 malformed/schema/entry/budget rejections, 16 invalid-option checks and four encoder-budget failures. Coverage includes decoded-name duplicates, aliases/conflicts, a fixed independently authored CRC-collision pair, UTF-8 and escaped surrogate errors, unsupported primitive/container/numeric forms, truncation, raw dot segments, trailing separators, path/pin limits, exact byte/entry/path/depth boundaries and printable ASCII JSON escaping. Input and output mutation, read-only dictionary mutation rejection, unknown lookup and reversed insertion order establish ownership/determinism. Fresh original reader composition succeeds after all failures.

The decoded profile is passed directly to each unchanged accepted reader once: the human reader retains 37 read-once sources / 4,683,848 bytes; the static reader with explicit [SWTS frame 0 selection](SWITCH_TEXTURE_STATIC_RESOURCE_CONTRACT.md) retains 27 read-once sources / 1,401,794 bytes and four complete terminal meshes. Original overlay configuration SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Their closed geometry, mip, byte and animation audits are reused rather than duplicated or claimed as new research.

[Independent Python oracle](../tools/python/verify_object_template_catalog_profile.py) checks UTF-8, duplicate decoded properties, exact types and lexical numbers, container depth, limits, raw path policy/normalization, pins and CRC using the existing independent polynomial-long-division checksum. It agrees on all 138 decoder cases and canonical bytes, checks the fixed collision independently and matches both reader summaries. It reads only the profile and private native/canonical outputs; it does not invoke C#, re-audit original archives or export assets. **Confidence:** Confirmed finite-fixture implementation agreement; no exhaustive JSON-parser or original global-table claim.

## Reproduction and next action

From the isolated repository root using cached .NET 8 packages and the configured Python executable:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-assets-round9-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
New-Item -ItemType Directory -Force local-private/round9 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round9/NuGet.Config
dotnet restore poc/round9-tests/Assets/Assets.csproj --configfile local-private/round9/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round9-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round9-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round9/canonical.json > local-private/round9/native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/verify_object_template_catalog_profile.py poc/preview-profiles/scene-object-catalog.json local-private/round9/native.txt local-private/round9/canonical.json > local-private/round9/python-audit.json
python tools/python/validate_organized_workspace.py .
```

The own [runner](../poc/round9-tests/Assets/Assets.csproj) writes canonical evidence only under the calling checkout's private directory. It disposes overlay/configuration/animation-bank handles and catches failures without publishing a catalog/resource result. No original executable, GPU, socket, DNS, downloaded package or shared-runner edit is required.

Lead acceptance/integration is next. Future consumer wiring must keep its choice to load this caller profile explicit; no automatic process-start file discovery is introduced. Original global template discovery, other recipes/families, appearance alternatives and trust/pin distribution remain separate questions. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R9_CATALOG_PROFILE.md) records scope and validation. Stop after this milestone pending the next lead dispatch.
