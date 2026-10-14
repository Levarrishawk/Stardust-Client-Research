# Original TRE reader implementation checkpoint — 2026-09-29

## Bounded question

Can the engine-neutral C# core consume TREE 0005 index/payload bytes, preserve
priority/tombstone semantics, and reproduce one original entry extracted by the
existing independent Python reader? Stop at byte extraction; no mesh, texture,
world, or proprietary content is promoted into curated files.

## Input identity and evidence

- Durable contract: [asset pipeline](../../docs/ASSET_PIPELINE.md), especially
  TREE 0005, path normalization/CRC, and SearchTree overlay semantics.
- Prior correction: [kind-0 size audit](ANALYSIS_CHECKPOINT_2026-09-22_TRE_SIZE_TOOL_AUDIT.md).
- Server HEAD: `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`.
- Server symbols under `MMOCoreORB/src/tre3`: `TreeFile::readHeader`,
  `readFileBlock`, `readNameBlock`, `readMD5Sums`; `TreeDataBlock::uncompress`;
  `TreeFileRecord::readFromBuffer`. They supply correlation for explicit LE32
  fields, index/name block ordering, stored/zlib data and the digest area.
- Source SHA-256: `TreeFile.cpp` =
  `73ca61f8c8a42599584ad04f9ee2eb7afcb6d5de98b0394373b3771406602e79`;
  `TreeDataBlock.h` =
  `0756bdfb8884afe689e247e6862694b83c431c255253346f72ea6df7e21ac0e1`;
  `TreeFileRecord.h` =
  `030e7a5642aa14e61c09a71579cfdbc005c5739571a0766a58daebd20dd633cf`.
- Original archive: `C:\Stardust-DEV\stardust_3_03.tre`, 51,095,126 bytes;
  SHA-256 `29d8dd18c15c8d2a13c21c6c377b2ede5dbba427afde016dfd46945c45fabd03`.

No executable VAs were newly analyzed or named. Original archive access was
read-only; no original payload file was extracted to the curated tree.

## Findings and consequences

| Observation | Confidence | Implementation consequence |
|---|---|---|
| Synthetic stored records with `usize>0,csize=0` extract their full data | Confirmed against independent fixture writer | Stored length is `usize`; compressed-size metadata is not a presence test |
| Higher-priority synthetic zero-`usize` entry masks a lower live entry | Confirmed against existing contract and deterministic test | Overlay lookup returns Masked, distinct from Missing, and stops fallback |
| Original index loads 1,477 entries with all normalized-name CRCs and payload bounds accepted | Confirmed for this archive | The new reader handles one real installed profile; full overlay is not claimed |
| Python and C# both extract 348 bytes from `terrain/environment/space_coruscant.iff` with the same SHA-256 | Confirmed byte equality through independent digests | Original compressed entry access is demonstrated without an intermediate asset format |
| .NET zlib accepted a synthetic payload whose advertised compressed size excluded the Adler trailer | Confirmed by an initially failing negative test | Shared `BoundedZlib` validates the zlib header and final Adler-32 in addition to output size |

Extracted member SHA-256:
`adcec78629b4bc0de5be324b60a58558b6421c9c85199244810b2135aaf2d3c1`.
This establishes byte extraction, not interpretation or rendering of that IFF.

The C# implementation uses a seekable stream, lazy payload reads, explicit
32-bit fields with 64-bit bounds arithmetic, deterministic printable ASCII
normalization, and filename CRC verification. Archive handles have explicit
ownership; overlays borrow them. New code lives under
`poc/Stardust.Core/Assets` and `poc/Stardust.Core/Compression`.

## Reproduction

The default core suite uses only independently authored synthetic archives and
fixed CRC/zlib vectors. It covers stored/compressed indices, stored/compressed
payloads, masks, overlay precedence, truncated files, bad counts/offsets/kinds,
duplicate paths, wrong output lengths, and missing zlib trailers.

Read-only optional original-entry verification:

```powershell
dotnet run --project .\poc\Stardust.Core.SmokeTests\Stardust.Core.SmokeTests.csproj -- `
  --tre C:\Stardust-DEV\stardust_3_03.tre `
  --member terrain/environment/space_coruscant.iff `
  --expected-sha256 adcec78629b4bc0de5be324b60a58558b6421c9c85199244810b2135aaf2d3c1
```

Independent Python reference from the repository root:

```python
import sys, hashlib, zlib
from pathlib import Path
sys.path.insert(0, "tools/python")
from scan_tre_sound_references import archive_records
archive = Path(r"C:\Stardust-DEV\stardust_3_03.tre")
for name, size, kind, stored in archive_records(archive):
    if name == "terrain/environment/space_coruscant.iff":
        payload = stored[:size] if kind == 0 else zlib.decompress(stored)
        assert len(payload) == size
        print(size, hashlib.sha256(payload).hexdigest())
        break
```

## Remaining uncertainty and exact next action

Only TREE 0005 and compression 0/2 are supported. Equal-priority mounts are
rejected pending a tie policy. TOC, loose-file nodes, locale/non-ASCII names,
MD5 verification and full configured-overlay validation remain open. No
proprietary data is embedded in tests. Bounded zlib checks do not establish
historical codec behavior or byte-exact compressed output.

Next: use this reader to load one original static mesh, resolve its shader and
texture dependencies, and validate recovered vertex/index packing and axis
conventions before displaying it through a prototype host adapter. The archive remains
the authoritative input. See the acceptance record (external historical reference).
