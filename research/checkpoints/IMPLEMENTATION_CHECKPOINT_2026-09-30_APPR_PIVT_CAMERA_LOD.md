# APPR extent and PIVT camera LOD checkpoint — 2026-09-30

## Question and stopping condition

What local offset does DTLA PIVT=0 select, and can independent original extent bytes plus transformed fixtures establish camera-driven LOD? Stop after the virtual/extent chain, full current inventory agreement, positive/negative native fixtures and standalone/composed Godot switching gates.

## Inputs and evidence

Rehashed original executable `C:\Stardust-DEV\Stardust.exe`: SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Configuration SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 TREE0005 mounts. Existing DTLA inventory supplies 6,198 names; sorted newline member-list SHA-256 `8AFA871E8541F59794C9A96DCFA3F6D44E2CE4CEC1A911F954F1580EFE60FA0E`.

Tools: Capstone/pefile static disassembly and vtable reader, existing independent TRE/IFF Python readers, new `verify_detail_extents.py`, owned .NET smoke suite and Godot 4.7.2 .NET x64 headless/OpenGL. The original executable was not run. A mistaken end-address invocation exposed an unbounded negative slice; `disasm_range.py` now rejects non-increasing or over-1-MiB ranges before reading the executable.

The [durable contract](../../docs/APPEARANCE_EXTENT_DISTANCE_CONTRACT.md) records exact constructor/vtable/reader/initializer addresses. **Confirmed:** false PIVT uses the transformed primary extent sphere center; true uses world translation; absent pivot is initialized false; NULL falls back to local zero. BOX stores maximum then minimum. The valid box path preserves the authored sphere center rather than recalculating midpoint.

## Results and consequences

**Confirmed original agreement:** all 6,198 primary extents, pivots, priorities and source identities match the independent parser: 6,048 boxes, 150 NULL; PIVT distribution is v0007 false 5,919/true 63, v0006 false 66, v0005 absent 150. There are 6,001 nonzero centers. Of 3,146 exact midpoint differences, only one exceeds 1e-4 (about 0.46737); most are rounding differences. Midpoint substitution is consequently not a valid general implementation.

**Confirmed implementation:** owned valid EXSP/EXBX version-one structure, retained center/radius/bounds, malformed/truncated/nonfinite/negative/reversed rejection, and rotated/translated PIVT true/false/absent fixtures pass. Wider distance math and rejection policies are explicitly modern implementation choices. Original reversed-box recovery and negative-radius diagnostic policy remain separate uncertainty.

**Confirmed host boundary:** standalone original room and composed textured garage pass headless camera midpoint switches. Actual GPU garage switches three independent detail instances; geometry, texture readback and transform fixture gates pass. The camera-enabled capture was visually inspected. Manual slider remains available through the checkbox. The garage remains unlit MAIN inspection with 35 textured/ten neutral surfaces and seventeen shared textures.

## Validation and next probe

Core smoke suite passes; Godot build has zero warnings/errors; independent corpus checks and bounded headless/GPU runs pass. Existing offline Godot certificate-store warning remains nonfatal. Five newly labeled addresses bring the distinct provisional registry to 2,590, with no duplicate addresses. Research score and 345-row packet boundary are unchanged.

Next: add typed BC6H unsigned/signed HDR DDS only if a separate owned HDR output representation and independent decoder reference can be verified. Full materials, DPVS and lazy runtime loading remain deferred.
