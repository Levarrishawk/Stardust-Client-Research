# PASS/0010 true-flag configured reachability

Date: 2026-09-25. Question R01. Scope: the only three effective shader assets containing the four true `PASS/0010` inserted-Boolean values.

## Result

The true cases remain present original assets but have no configured literal consumer:

- `shader/ui_bar_glow.sht` — two true passes;
- `shader/wter_lava_02.sht` — one true pass;
- `shader/wter_lava_02_still.sht` — one true pass.

A fresh full-decompression search of every member in every TRE under `C:\Stardust-DEV` found zero occurrences of each full path and each basename outside the shader records themselves/archive directory metadata. Raw searches of `Stardust.exe` and exact-name searches of current Stardust source likewise found zero consumers.

This is stronger than the earlier effective-reference-table absence because it examines all decompressed member payloads rather than only known reference extractors.

## Conclusion

The true flag's exact auxiliary render-target/channel behavior is **not an ordinary current-content blocker**. The three shaders are strongly inferred dormant/orphaned under the configured overlay and current server. This is not proof against generated names, CRC lookup, admin/future content, or external UI configuration.

The replacement asset model must still decode and preserve the Boolean. The safe initial renderer policy is:

1. preserve it as `auxiliaryPostProcessWrite` (provisional semantic name);
2. emit an explicit diagnostic if a true pass becomes reachable;
3. do not silently treat true as false in a compatibility claim;
4. defer its exact bloom/heat target mapping to a targeted visual fixture if reachability is later established.

The directly referenced `PASS/0010` shaders, including the Mustafar volcano family, use false in observed passes. Their field alignment is already closed and they can proceed through the supported `PASS/0009` suffix semantics while retaining the false extension field.

## Confidence

- **Confirmed:** zero full-path/basename occurrences in all decompressed configured TRE payloads, executable, and current server source; exact true-case assets and counts.
- **Strongly inferred:** dormant/orphaned in ordinary current content.
- **Unknown:** exact historical name, auxiliary target/channel, and future/generated reachability.

Reproduction: `tools/python/find_tre_members_containing.py C:\Stardust-DEV` with all three full paths and basenames.
