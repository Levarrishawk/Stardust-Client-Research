# Client Icon Manager Resolution Checkpoint (2026-09-22)

## Outcome

`CuiIconManager` is a UI drag/action icon resolver, not a renderer texture loader or network subsystem. Its main resolver `cui_icon_manager_resolve_action_icon_VA_009D52D0` converts retained UI drag/action payloads into typed icon descriptors used by toolbar and command UI.

## Confirmed resolution policy

The resolver switches on the UI payload type at offset `+0x38`:

- type 1 returns an empty/default descriptor;
- type 2 resolves a textual action/command payload;
- type 4 first attempts to resolve an object/template-backed icon and otherwise falls back to empty;
- unsupported types follow a generic empty/fallback path.

For textual type 2 payloads it strips an optional leading slash, tokenizes the command, and resolves in this order of observed specialized handling:

1. social command -> numeric social ID -> canonical social name/icon descriptor;
2. mood command -> numeric mood ID -> canonical mood name/icon descriptor;
3. `ui action ...` -> action-name descriptor, with diagnostic `CuiIconManager bad ui action [%s]` on malformed input;
4. generic command/action classification;
5. droid-specialized fallback when the active object/template classification matches.

Missing social and mood IDs generate explicit diagnostics rather than fabricated icons. The `CuiIconManagerCallback` RTTI and several concrete callback users show that icon updates are delivered asynchronously to UI owners where necessary.

## Replacement-client consequences

- Preserve command normalization (including optional `/`) and the distinction among social, mood, UI action, generic command and object/template icon sources.
- Reuse the already mapped social/mood catalogs and original UI/icon assets; do not duplicate those registries in the icon manager.
- Return an explicit missing/default icon descriptor for malformed or unknown actions and log the original token.
- Keep icon identity independent from GPU texture handles. Asset/resource resolution should map the semantic descriptor to the renderer/UI backend.
- This subsystem is not needed by the headless network proof of concept.

## Confidence

- Class/callback identity and principal resolver: **Confirmed by RTTI and diagnostic string xrefs**.
- Social, mood and `ui action` branches: **Confirmed by executable code and known catalog helpers**.
- Friendly historical names for all numeric returned descriptor categories: **Unknown; preserve categories and specialize only when evidenced**.

