# Forward playback scripts — current reachability closure

Date: 2026-09-25  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The four effective playback scripts containing `PSKA/0003`, `GRLB/0003`,
`FIPL` and/or `PPRL` are **dormant under the current configured ordinary
client/server routes**. This closes A05 for engine/build decisions; it does not
delete the assets or assign unknown action semantics.

Prior corpus work established that the four paths have no literal full-path or
basename reference in any other effective non-PST asset, this executable, or
the current Stardust server source. They are absent from the effective combat
manager's 24 referenced scripts.

This pass exported every static code reference to cached playback-template
fetch `0x00402FA0`. There are five callers/sites, comprising four route types:

| Route | Path source |
|---|---|
| `0x005F4D60` install | two fixed literals: posture-align and restore-scale scripts |
| `0x00469E80` preload | CRC-string paths read from `combat/combat_manager.iff` |
| `0x00467370` runtime | retained path from the selected combat-manager entry |
| `0x004030E0` helper | caller-supplied CrcString, constructs an instance after cached fetch |

The first three ordinary sources cannot select any of the four forward scripts
under the current overlay. No current server packet handler supplies a playback
template path to the generic helper. The helper means debug code, a future
module, or a new caller could still request an arbitrary path; therefore the
conclusion is configuration/current-program scoped rather than universal.

## Compatibility policy

The replacement asset system must still:

- parse and retain the outer PBSC/thread/action envelopes losslessly;
- retain unknown action tags and forward action versions as typed opaque
  records with source provenance;
- report unsupported execution only if such a script is actually instantiated;
- avoid truncating `PSKA/0003` or `GRLB/0003` to older layouts; and
- allow later semantic adapters without reimporting or corrupting the source
  asset.

Decoding the extra PSKA tag or inventing FIPL/PPRL execution is **not a
pre-build blocker** for the current Stardust server/configuration. A future
content change that names one of these paths automatically reopens the runtime
semantics question.

## Confidence

| Finding | Confidence | Evidence |
|---|---|---|
| absent from current configured asset/server literal reference graph | Confirmed negative corpus result | effective overlay and server-source audits |
| all static playback fetch routes enumerated | Confirmed | complete xrefs to `0x00402FA0` |
| absent from normal fixed/combat route sources | Confirmed | two fixed literals plus combat-manager path set |
| impossible for any future/debug/dynamic caller | Not claimed | generic helper accepts caller-supplied CrcString |
