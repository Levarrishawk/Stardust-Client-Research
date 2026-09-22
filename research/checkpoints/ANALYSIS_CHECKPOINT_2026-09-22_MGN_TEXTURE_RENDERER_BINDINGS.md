# MGN texture-renderer bindings

Date: 2026-09-22

## Result

The rare `FORM TRTS` suffix in sixteen effective skeletal mesh generators is
not an unknown geometry dialect. It is an explicit bridge from selected mesh
primitive/shader slots to named blueprint texture-renderer templates. This path
is required for the original dynamically composited humanoid face textures.

## Confirmed executable grammar

Stardust.exe has three directly traced skeletal-mesh loaders:

- `0x007FE6B0`: SKMG/0002 loader
- `0x007FF9D0`: SKMG/0003 loader
- `0x00800EF0`: SKMG/0004 loader

All optionally enter `FORM TRTS`, read `INFO` as two little-endian U32 counts,
reserve a pointer vector and an eight-byte-pair vector, then read the first
count of `TRT ` chunks. Each `TRT ` contains:

```text
cstring textureRendererTemplateName
U32 bindingCount
bindingCount * { U32 primitiveIndex, U32 textureTag }
```

The second INFO value is the total pair count used to reserve the flattened
binding vector. Each runtime template record retains the name, its binding
count and its starting pair index. This is confirmed from the loaders rather
than inferred from file names.

## Effective corpus

Exactly sixteen configured-overlay MGN files contain TRTS. They are the L0/L1
head meshes for human, Twi'lek, Zabrak and Sullustan females/males where
present, plus `mellichae_l0/l1.mgn`. Every file has `INFO=(1,1)`, one TRT name
and one binding. Eight face-template families are referenced.

The first pair value varies from 0 through 4 across meshes and LODs. The second
is uniformly `0x4D41494E`, the tag `MAIN`. This strongly identifies the pair as
the target primitive/shader index and texture parameter tag. The differing
index between some L0/L1 meshes proves it must not be globally hard-coded.

## Blueprint template boundary

`hum_m_face.trt` resolves through the live overlay to `stardust_3_03.tre`. It
is `BTRT/0001` and contains `DEST`, shader-template names (`SHTM`), texture
descriptions (`TXTS`), vertex/index buffers, camera data, render-command data
and prepare-command data. `0x0086E240` registers the `BTRT` factory under the
RTTI-backed `BlueprintTextureRendererTemplate` identity; `0x0086E860`
constructs the template and dispatches versions 0001/0002.

The generated texture is therefore an asset-authored render product injected
into `MAIN`, not a static DDS filename embedded in the head mesh.

## Replacement requirement

The asset IR must preserve TRTS records exactly and expose a renderer-neutral
dynamic-texture job. An initial importer may parse and retain BTRT command
graphs before executing them, but a visually compatible character milestone
must implement the face-template subset and bind its output to the indicated
primitive/tag. Substituting one static face texture is acceptable only as an
explicit diagnostic fallback, not compatibility behavior.

## Confidence

- TRTS byte grammar and executable version coverage: **Confirmed by code**.
- Sixteen-file corpus, referenced names and binding values: **Confirmed by configured assets**.
- Pair meaning `primitiveIndex, textureTag`: **Strongly inferred**.
- Full BTRT command semantics: **Partially reconstructed; still open**.
