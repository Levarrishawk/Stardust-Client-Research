# Blueprint texture renderer envelope

Date: 2026-09-22

## Scope and result

This block bounded the `.trt` subsystem reached from head-mesh TRTS bindings.
The configured overlay has exactly eight effective `.trt` resources. All are
`BTRT/0001`, all eight are referenced by the sixteen effective TRTS head meshes,
and all share one structural envelope. There is no unreferenced second family
in the installed client corpus.

## Executable factory and versions

- `blueprint_texture_renderer_template_install_VA_0086E240` registers FourCC
  `BTRT` and the RTTI-backed `BlueprintTextureRendererTemplate` identity.
- `blueprint_texture_renderer_template_ctor_dispatch_VA_0086E860` constructs
  the runtime template and accepts versions 0001 and 0002.
- `blueprint_texture_renderer_template_load_v0001_VA_0086EEB0` parses the live
  corpus.
- `blueprint_texture_renderer_template_load_v0002_VA_0086F9E0` preserves the
  later compatible path even though it is not selected by this overlay.

## Confirmed BTRT/0001 envelope

The live loader consumes these ordered members:

```text
FORM BTRT
  FORM 0001
    INFO                  # U32 template-level value
    DEST                  # U32 width, U32 height, U32 count, count*U32 format/attribute IDs
    FORM SHTM
      INFO                # U32 count
      count * NAME        # shader-template resources
    FORM TXTS
      INFO                # U32 count followed by count texture descriptions/names
    FORM VBS
      INFO                # U32 count
      count * VTXA        # static vertex arrays
    FORM IBS
      INFO                # U32 count
      count * IDAT        # U32 index count, then count U16 indices
    FORM CAM              # parsed camera setup
    FORM RCMS
      INFO                # U32 render-command count
      repeated command forms
    FORM PCMS
      INFO                # U32 prepare-command count
      repeated command forms
```

The `hum_m_face.trt` fixture uses a 256x256 destination, five shader templates,
five texture descriptions, five vertex arrays, zero index buffers, one camera,
five render commands and nine prepare commands. Command forms are polymorphic;
the fixture includes nested `SRSC` render commands and `TOPS` prepare-operation
sequences (`SSTC`, `STFP`, `SST1`). Exact opcode semantics remain the next
bounded analysis target.

Across all eight live templates, destination sizes are 512x512 for human and
Twi'lek female, 256x256 for ordinary male/Sullustan/Zabrak female templates,
and 128x256 for `zab_m_face_special_01`. Every DEST contains the same five file
IDs `[4,4,3,5,6]`; the executable lookup table translates these to internal IDs
`[4,4,1,3,0]`. The friendly pixel/channel names remain unknown, but the exact
conversion required by a decoder is preserved.

## Replacement implication

BTRT is best represented as a small offline-decoded render graph, not emulated
as an opaque D3D9 call stream. Preserve the destination definition, resource
tables, geometry, camera and ordered prepare/render commands in engine-neutral
IR. A modern render backend can then implement only the command families used
by the eight live face templates before broadening to dormant BTRT/0002.

## Confidence

- Factory, versions, member order, counts and index width: **Confirmed by code**.
- Effective resource count/version/signature: **Confirmed by configured assets**.
- Friendly meaning of every DEST attribute: **Unknown; exact IDs/remap retained**.
- Live command opcodes: **Closed in the subsequent command checkpoint**.
