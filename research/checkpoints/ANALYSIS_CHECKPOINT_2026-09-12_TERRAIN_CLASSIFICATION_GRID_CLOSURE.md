# Terrain classification-grid closure

Date: 2026-09-12  
Subject: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

The audit item describing a ClientChunk `+0xD?` “classification/group grid” was
stale. Direct wrapper and constructor evidence already assigns both 16-bit grids:

| ClientChunk field | Wrapper | Constructor callback/domain | Meaning |
|---:|---:|---|---|
| `+0xCC` | `0x00862880` | `0x0084AD70`, `FloraDynamicNear` | dynamic-near flora packed family/selection grid |
| `+0xD0` | `0x008628B0` | `0x0084ADB0`, `FloraDynamicFar` | dynamic-far flora packed family/selection grid |

Both wrappers call common sampler `0x00862720`; near passes its final mode byte as
one, far as zero. The helper maps world position to chunk-grid coordinates, reads
one U16 from the supplied grid, resolves it through the terrain flora-family group,
copies the associated three-byte color from ClientChunk `+0xC4`, and returns the
selected child pointer. The first packed byte identifies a family; the second is
normalized for weighted child selection.

This is not an unidentified terrain classification channel and requires no new
replacement subsystem. It is the shared dynamic-flora query implementation.

## Stable names

- `0x00862720`: `client_chunk_sample_dynamic_flora_VA_00862720`
- `0x00862880`: `client_chunk_sample_dynamic_near_flora_VA_00862880`
- `0x008628B0`: `client_chunk_sample_dynamic_far_flora_VA_008628B0`

## Residual detail

The near/far mode byte's exact internal branch label is unavailable. Its only
observed values and callers are domain-specific and behavior is preserved by
keeping the near/far query paths distinct. It is not an architectural blocker.
