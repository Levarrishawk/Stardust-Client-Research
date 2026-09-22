# Custom UI Render Widgets

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`  
Scope: embedded 3D object viewers, object-list viewers, paperdoll and ground radar registration

## Conclusions

The legacy UI contains renderer-backed custom widget types, not merely image
controls. `0x009B4240` registers five named factories:

- `CuiWidget3dObjectViewer`
- `CuiWidget3dPaperdoll`
- `CuiWidgetGroundRadar`
- `CuiWidget3dObjectListViewer`
- `CuiGenericRenderer`

The effective UI corpus contains 183 object-list viewers, two object viewers,
two paperdolls and three ground-radar instances. These are therefore deployed
asset contracts rather than dormant engine features.

## Object ownership and display setup

`0x00A65E90` is the object-viewer's set-object path. It updates two retained
references, explicitly releases replaced references, obtains and retains the
object appearance, and warns/clears the retained display reference when the
object has no appearance. It copies the object's localized Unicode display name
and invalidates/recomputes the widget after the assignment.

The path also:

- distinguishes `ManufactureSchematicObject`;
- has a creature-specific appearance/extent path controlled by widget flags;
- captures the appearance transform/extent;
- derives framing geometry from the appearance extent and supplied viewport
  dimensions; and
- preserves separate current/target camera-framing values.

This is strong evidence that inventory, examine, crafting and character UI
previews are live scene-object presentations rather than pre-rendered icons.

## List-viewer bounds and camera fitting

`0x00A4EF20` computes a transformed aggregate box for an object and its attached
scene objects. It walks child/attachment transforms, uses both ordinary and
skeletal extent paths, transforms all eight box corners when necessary, and
accumulates the result into an aggregate extent.

Transient presentation appearances are deliberately excluded from camera-fit
bounds:

- `FadingTextAppearance`
- `RibbonAppearance`
- `ParticleEffectAppearance`
- `SwooshAppearance`
- `GlowAppearance`

This prevents trails, combat text and effects from changing preview-camera
distance. A fit-rectangle mode can invert the root transform used during the
bounds calculation. `0x00A4E090` registers debug controls for fit rectangle,
removal, camera and general object-list-viewer diagnostics; `0x00A4C8A0`
installs the viewer memory pool.

The UI corpus supplies the declarative side of this interface, including
automatic zoom, fit-bone selection, look-at, field of view, LOD bias,
yaw/pitch/roll, object yaw/pitch, fit rectangle and light parameters.

## Replacement-client requirement

Implement one renderer-facing `ScenePreviewService` with specialized adapters:

1. object viewer: one retained logical object/appearance and framing camera;
2. object-list viewer: multiple objects, aggregate transformed bounds and
   selectable entries;
3. paperdoll: creature/equipment presentation with skeletal attachment support;
4. ground radar: orthographic/top-down scene projection plus radar overlays;
5. generic renderer: controlled renderer insertion point for UI mediators.

The UI runtime should own only stable logical handles. Renderer resources and
scene instances remain renderer/resource-manager owned; no 32-bit pointer or
legacy intrusive-reference layout may cross the new subsystem boundary.

Original object templates, appearances, meshes, skeletons, hardpoints,
materials/shaders and UI property values are mandatory inputs. Original asset
compatibility is therefore required even for menus and inspection panels before
full world rendering is available.

## Confidence

| Finding | Confidence | Evidence |
|---|---|---|
| Custom widget factory registration | Confirmed | Literal type registrations at `0x009B4240` |
| Object viewer retains scene object and appearance | Confirmed | reference release/acquire and appearance lookup at `0x00A65E90` |
| List viewer computes transformed aggregate bounds | Confirmed | attachment traversal, transforms and box accumulation at `0x00A4EF20` |
| Transient effects are excluded from camera fitting | Confirmed | RTTI checks in `0x00A4EF20` |
| Paperdoll requires skeletal/equipment specialization | Strongly inferred | registered dedicated type plus shared skeletal extent/attachment facilities |
| Ground radar is a renderer-backed custom adapter | Strongly inferred | dedicated registered widget and deployed corpus instances |

## Remaining bounded work

Paperdoll equipment-slot behavior and ground-radar projection/overlay semantics
need dedicated entry-point closure. They no longer represent an architectural
unknown: both sit behind the renderer-backed custom-widget boundary described
here.
