# Original light-scaling selector — October1,2026

Question: what owns the byte+0x12 selecting raw/scaled light colors, when is it captured, and what is its default? Stop at configuration/constructor/copy/raster evidence and bounded adapter tests.

Inputs: documented SHA-verified executable/DLL; same original157-source interior. Tools: read-only PE/Capstone, prior curated terminology/decompiler correlation, verify_light_scaling_selector_static.py, native producer/independent byte audit. No original execution/server contact.

Observation: client_graphics_config_install_VA_00779A50 reads [ClientGraphics] enableLightScaling with default true at0x00779C93/95/9A/A4, storing global0x01923544 at0x00779CB5. Getter0x00779E50 returns that byte. Both StaticShader constructors0x00769AC0 and0x00769BD0 capture it at0x00769B02/00769C11 into engine shader+0x12. Copy constructor0x00769CA0 preserves it at0x00769CDD/E0. The raster wrapper0x62A046B0 obtains backend data through engine shader+0x0C, then reads the separate engine shader+0x12 at0x62A046C5. This corrects the previously imprecise graphics-data owner description; the selector is not part of the Direct3d9_StaticShaderData vector header.

Raster0x62A0A050 installs scaled color and scaled-score callbacks when true and raw equivalents when false. Configuration is a shader-construction snapshot; copies preserve it. Direct raw-call scan finds both constructor callers; absolute-global scan finds the configuration writer and getter. Absence of a key in inspected local text configs does not prove every effective runtime override.

Changes: FromLegacyCell has explicit scaleLightColors(default true); owned mode matches the recovered default. CELL construction initializes independent scales to1, so current original fixture colors/ranking remain unchanged in both modes. Twenty-three hash-gated anchors/literal checks, hand mode/default/ownership tests,576 independent original lighting cases and full source/route/headless checks pass. Build zero warnings/errors. One new canonical getter name brings current registry to2602;345 packets/research scores unchanged.

Canonical result: [legacy selection contract](../../docs/LEGACY_LIGHT_SELECTION_CONTRACT.md). Original live scale updates/global light creation/order and compiled high-power LIT remain open. Next complete owned selected vertex-source include dependencies so the original VSH include graph is explicit and shared/read-once.
