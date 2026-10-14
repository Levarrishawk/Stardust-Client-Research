# Explicit modern five-family vertex blend

TerrainModernFiveFamilyBlend (external historical reference)
binds exactly nine supplied family labels to the exact retained
`TerrainPatchGeometry`. Its policy name is `ModernFiveFamilyVertexLinearBlend`.
This is a modern rendering choice, not recovered original ALP selection,
feathering, shader execution or blending. The existing
[partial surface labels](TERRAIN_SURFACE_PREVIEW_CONTRACT.md) establish only
the selected families; they do not establish this interpolation policy.

```csharp
TerrainModernFiveFamilyBlend.Bind(geometry, vertexFamilyKeys, cancellation);
```

The optional cancellation token defaults to none. `Geometry` retains the same
object. `VertexFamilyKeys` and `Weights` expose detached read-only arrays;
`CopyFamilyKeys()` and `CopyWeights()` return independent copies. Binding
accepts only keys3,5,6,8,9 and rejects null geometry, other label counts,
unknown keys and cancellation. Work and owned attributes are bounded by nine
vertices. Binding never exports, reconstructs or modifies geometry positions,
normals, indices, packed COLOR, base UVs, auxiliary UVs or selectors.

| Family | Vector4 attribute | Interpolated contribution |
|---|---|---|
|3|(1,0,0,0)|X|
|5|(0,1,0,0)|Y|
|6|(0,0,1,0)|Z|
|8|(0,0,0,1)|W|
|9|(0,0,0,0)|1-X-Y-Z-W|

An adapter linearly interpolates these attributes on each retained geometry
triangle, using the same barycentric interpolation as other vertex attributes.
For barycentric weights `b0,b1,b2`, the contribution of family `f` is the sum
of `bj` over vertices labeled `f`. The fifth channel follows from
`b0+b1+b2=1`. Pure faces remain pure, endpoint labels remain pure, and shared
edges agree if their endpoint positions/labels and edge subdivision agree,
regardless of triangle orientation or differing center labels. An endpoint3
to endpoint9 edge queried one quarter along has exact contributions3/4 and1/4.
The helper does not interpolate world-space categorical labels, select textures,
or promise matching weights when adjacent geometry/labels differ. GPU float
interpolation and material sampling are separately verified adapter behavior;
roundoff may leave a small residual in the derived fifth contribution.

## Focused validation

Native tests (external historical reference) use an independent
normalized `BigInteger` Fraction oracle. It converts the actual binary32 X/Z
positions exactly, finds triangles using signed rational areas, and sums
barycentric contributions by the supplied categorical keys. A separate double
attribute interpolation consumes the helper's four channels and derives the
fifth. This checks the encoder against an independent family-sum construction,
rather than reusing its switch.

The synthetic fixtures contain two adjacent ten-metre patches at negative X,
oppositely directed shared edges, differing center/outer labels, family3/9
endpoints and exact quarter positions. They check reversed oracle triangle
orientation, all five vertex encodings, constant pure faces across all nine
topology patterns, mixed-face interior samples, unchanged geometry fields,
detached input/exports, read-only attributes, repeat binding, invalid count/key,
null and pre-cancellation. No original assets or executable are needed.

Run the source-free test project with an existing offline NuGet cache:

```powershell
dotnet restore poc/terrain-five-family-blend-tests/TerrainFiveFamilyBlendTests.csproj --configfile poc/terrain-five-family-blend-tests/NuGet.Config
dotnet run --no-restore --project poc/terrain-five-family-blend-tests/TerrainFiveFamilyBlendTests.csproj
```

The bounded native gate passes3,394 assertions and165 independent Fraction
samples. `OriginalBlendParity` is false. This contract closes the modern CPU
attribute definition; actual GPU upload/interpolation, appearance and seam
capture remain the adapter's acceptance gate. No original-function claim or
compatibility coverage denominator changes.

The focused suite is required as `terrain-five-family-blend` in the
offline regression runner (external historical reference). It takes no private
input arguments. Registration and runner orchestration tests are distinct from
a full thirty-suite replay and from visual adapter acceptance.
