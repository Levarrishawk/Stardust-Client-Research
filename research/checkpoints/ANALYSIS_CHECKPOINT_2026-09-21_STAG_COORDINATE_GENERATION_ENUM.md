# STAG trailing coordinate-generation enum

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe` and deployed `s207_r.dll`

## Result

The final STAG byte at runtime `+0x5C` is closed to the level needed for the installed asset corpus: it is a legacy texture-coordinate generation/profile selector, not a color/alpha combiner, sampler override, or arbitrary flag. Exact historical enum spelling is unavailable, so the profile names below are behavioral labels.

## Evidence

- The constructor initializes the field to zero; both STAG versions deserialize it after all combiner, tag, address and filter fields.
- Across all 951 effective STAG records its values are only 0, 3, 4 and 5.
- Value 3 occurs exclusively in environment/reflection families (`*_envmask*`, `*_irid*`) whose first texture tag is `ENVM` or `IRID`. This is the camera/reflection-vector profile.
- Value 4 occurs in the first scrolling coordinate profile used by `*_scroll`, `cloudlayer`, `water`, lava and whitewater assets.
- Value 5 occurs only in the second STAG of two-stage scrolling assets (`a_splitalpha_scroll`, crystal-fall and whitewater), while their preceding scrolling stage uses value 4. This is a second independent scroll-coordinate profile.
- Value 0 covers ordinary mesh UV/pass-through stages.
- The installed `s207_r.dll` active programmable route obtains texture-coordinate-set bindings through the second STAG tag and its generated vertex-shader cache. The orphaned D3DTSS applicator has no reachable consumer. Therefore this byte belongs to the legacy coordinate-generation contract and is not an additional live D3D texture-stage state in the recovered backend route.

## Effective mapping

| Wire value | Replacement behavior | Confidence |
|---:|---|---|
| 0 | ordinary/pass-through coordinate profile selected by the stage coordinate-set tag | Confirmed for corpus behavior |
| 3 | environment/reflection-vector coordinate profile | Strongly inferred; exclusive asset correlation |
| 4 | first scroll-generated coordinate profile | Strongly inferred; exclusive asset correlation and scroll parameter system |
| 5 | second independent scroll-generated coordinate profile | Strongly inferred; occurs only after a value-4 stage in two-scroll effects |
| 1, 2 | preserve as unsupported legacy enum values; no effective installed asset uses them | Confirmed absence, semantics unknown |

The modern importer should decode the byte, preserve its numeric value, and compile 0/3/4/5 to the renderer-neutral coordinate profiles above. Values 1/2 must round-trip and produce a diagnostic rather than being guessed.

## Residual fixed-function uncertainty

The friendly historical names and complete enum table for values 1/2 remain unavailable, but they are absent from the configured asset overlay. Exact numeric combiner translation for a few unused argument/modifier combinations remains a dormant legacy breadth item. Neither prevents correct handling of the original assets actually present in this installation.
