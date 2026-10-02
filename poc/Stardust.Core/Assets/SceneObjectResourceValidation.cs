using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Stardust.Core.Protocol;

namespace Stardust.Core.Assets;

/// <summary>Modern no-I/O completion consistency gate; it cannot attest origin, ownership or encoded bytes from metadata alone.</summary>
public static class SceneObjectResourceValidation
{
    public static void Validate(SceneObjectResources result, SceneCreateObjectByCrcMessage expectedCreate,
        ObjectTemplateCatalog catalog, SceneObjectResourceOptions options)
    {
        ArgumentNullException.ThrowIfNull(result); ArgumentNullException.ThrowIfNull(expectedCreate);
        ArgumentNullException.ThrowIfNull(catalog); ArgumentNullException.ThrowIfNull(options);
        try
        {
            Need(new[] { expectedCreate.QuaternionX, expectedCreate.QuaternionY, expectedCreate.QuaternionZ, expectedCreate.QuaternionW,
                expectedCreate.PositionX, expectedCreate.PositionZ, expectedCreate.PositionY }.All(float.IsFinite), "Finite expected placement required.");
            var recipe = catalog.Require(expectedCreate.TemplateCrc);
            Need(result.Recipe == recipe && result.Kind == options.Recipe && result.Kind == recipe.Recipe && Same(result.Create, expectedCreate), "Completion create/catalog/options mismatch.");
            Need((result.Creature is null) != (result.Static is null), "Exactly one typed payload required.");
            int maxCount = options is SceneCreatureResourceOptions c ? c.MaximumResources : ((SceneStaticResourceOptions)options).Limits.MaximumResources;
            long maxBytes = options is SceneCreatureResourceOptions cc ? cc.MaximumSourceBytes : ((SceneStaticResourceOptions)options).Limits.MaximumSourceBytes;
            var sources = result.Sources; Need(sources is { Count: > 0 } && sources.Count <= maxCount && result.ExternalReads == sources.Count, "Source count/read budget mismatch.");
            long bytes = 0;
            foreach (var p in sources)
            {
                Need(p.Value is not null && p.Key == p.Value.Path && LegacyAssetPath.Normalize(p.Key) == p.Key && p.Value.Bytes > 0 &&
                    p.Value.Sha256 is { Length: 64 } && p.Value.Sha256.All(Uri.IsHexDigit), "Invalid source identity metadata.");
                bytes = checked(bytes + p.Value.Bytes);
            }
            Need(bytes == result.SourceBytes && bytes <= maxBytes, "Source byte budget mismatch.");
            if (options is SceneCreatureResourceOptions creatureOptions)
            {
                Need(result.Creature is not null && result.Static is null, "Creature discriminator mismatch.");
                Need(Same(result.Creature.Create, result.Create) && result.Creature.Recipe == recipe && result.Creature.ExternalReads == result.ExternalReads && result.Creature.SourceBytes == bytes, "Creature common metadata mismatch.");
                ValidateCreature(result.Creature, creatureOptions, recipe);
                Subset(result.Creature.Creature.Profile.Sources, sources); Subset(result.Creature.Creature.Sources, sources, exact: true);
                var materials = result.Creature.Creature.MaterialBundle!;
                Need((materials.AlbedoFactors is not null) == creatureOptions.LoadAlbedoFactors, "Albedo option/result mismatch.");
                Need(result.Creature.Creature.FaceBlueprints is null && result.Creature.Creature.FaceResources is null && result.Creature.Creature.GeneratedFaces is null &&
                    result.Creature.Creature.BlendShapes is null && result.Creature.Creature.ShapeSelection is null && result.Creature.Creature.CustomizationValues is null &&
                    materials.TextureSelection is null && materials.CompressedNormals is null && materials.VertexLitSpecular is null && materials.TangentLighting is null, "Unrequested creature extensions.");
                Need(materials.PaletteDefaults is { Count: <= 64 }, "Palette map validation budget.");
                foreach (var p in materials.PaletteDefaults!)
                {
                    Need(p.Value is not null && p.Value.Shader.PaletteFactors.Count <= 4096 && p.Value.Shader.TextureSelections.Count <= 4096 && p.Value.Shader.TextureChoices.Count <= 4096 && p.Value.Palettes.Count <= 4096, "Palette descriptor validation budget.");
                    Need(materials.Materials.ContainsKey(p.Key) && p.Value is not null && sources.ContainsKey(p.Key) && materials.Materials[p.Key].Customization is { } authored && p.Value.Shader.Version == authored.Version &&
                        p.Value.Shader.Metadata.Payload.Span.SequenceEqual(authored.Metadata.Payload.Span) && p.Value.Shader.PaletteFactors.SequenceEqual(authored.PaletteFactors) &&
                        p.Value.Shader.TextureChoices.SequenceEqual(authored.TextureChoices) && p.Value.Shader.TextureSelections.SequenceEqual(authored.TextureSelections), "Palette shader source mismatch.");
                    Subset(p.Value.Sources, sources);
                    Need(p.Value.Shader.Metadata.Payload.Length <= sources[p.Key].Bytes && p.Value.AuthoredFactors.Count <= 4096, "Palette metadata validation budget.");
                    PaletteDescription? Palette(string path) => p.Value.MissingPalettePaths.Contains(path) ? null : p.Value.Palettes.TryGetValue(path, out var owned) ? owned : null;
                    Need(p.Value.AuthoredFactors.SequenceEqual(CustomizableShaderPaletteQuery.Select(p.Value.Shader, Palette)), "Authored palette selection mismatch.");
                }
                if (materials.AlbedoFactors is { } albedo)
                {
                    _ = SceneCreaturePaletteCustomization.DescribeControls(result.Creature);
                    Need(albedo.Programs is { Count: > 0 and <= 64 } && albedo.Materials.Keys.ToHashSet().SetEquals(materials.Materials.Keys), "Incomplete albedo closure.");
                    foreach (var p in albedo.Materials)
                    {
                        Need(p.Value is not null && sources.ContainsKey(p.Value.PixelProgram) && albedo.Programs.TryGetValue(p.Value.PixelProgram, out var program) && program is not null &&
                            program.UsesAlphaHueMask == p.Value.UsesAlphaHueMask, "Albedo program/source mismatch.");
                        materials.PaletteDefaults.TryGetValue(p.Key, out var palette);
                        Need(SkeletalAlbedoFactorReader.WithPalette(materials.Materials[p.Key], palette, p.Value) == p.Value, "Albedo color factors mismatch.");
                        var effect = materials.Effects[materials.Materials[p.Key].References.EffectPath]; Need(effect.Implementations.Count <= 64 && effect.Implementations.All(i => i.Passes.Count <= 64), "Albedo pass validation budget.");
                        var passes = effect.Implementations.Where(i => i.Passes.Count == 1).Select(i => i.Passes[0]).Where(pass => pass.PixelProgram?.EndsWith("_ps11.psh", StringComparison.Ordinal) == true).ToArray();
                        Need(passes.Length == 1 && passes[0].PixelProgram == p.Value.PixelProgram && passes[0].Fields[0x40] == p.Value.FirstTag && passes[0].Fields[0x44] == p.Value.SecondTag, "Albedo effect/program/factor tags mismatch.");
                    }
                    Need(albedo.Programs.Keys.ToHashSet().SetEquals(albedo.Materials.Values.Select(v => v.PixelProgram)), "Extra albedo program.");
                }
                long decoded = 0; foreach (var p in materials.Textures) decoded = checked(decoded + Image(p.Value));
                Need(decoded <= 33554432, "Creature decoded image budget.");
            }
            else ValidateStatic(result.Static ?? throw new InvalidDataException("Static discriminator mismatch."), (SceneStaticResourceOptions)options, recipe, result);
        }
        catch (Exception error) when (error is ArgumentException or NullReferenceException or KeyNotFoundException or OverflowException or IndexOutOfRangeException)
        { throw new InvalidDataException("Malformed resource completion structure.", error); }
    }
    private static bool Same(SceneCreateObjectByCrcMessage a, SceneCreateObjectByCrcMessage b) => a is not null && b is not null && SceneObjectLifecycleCodec.EncodeCreate(a).SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(b));
    private static void Subset(IReadOnlyDictionary<string, AssetSourceIdentity> map, IReadOnlyDictionary<string, AssetSourceIdentity> sources, bool exact = false)
    {
        Need(map is not null && map.Count <= sources.Count && (!exact || map.Count == sources.Count), "Source closure cardinality mismatch.");
        foreach (var p in map) Need(sources.TryGetValue(p.Key, out var value) && p.Value == value, "Nested source identity/hash mismatch.");
    }
    private static long Image(DdsImage image)
    {
        Need(image is { Mips.Count: > 0 and <= 32 } && Enum.IsDefined(image.Format) && Enum.IsDefined(image.ColorSpace) && Enum.IsDefined(image.AlphaMode), "Invalid owned DDS description.");
        int w = image.Width, h = image.Height; Need(w is > 0 and <= 8192 && h is > 0 and <= 8192, "Image dimensions."); long bytes = 0;
        foreach (var mip in image.Mips)
        { Need(mip is not null && mip.Width == w && mip.Height == h && mip.Rgba.Length == (long)w * h * 4, "Image mip extent/bytes mismatch."); bytes = checked(bytes + mip.Rgba.Length); w = Math.Max(1, w / 2); h = Math.Max(1, h / 2); }
        Need(image.Mips.Count <= 1 + (int)Math.Floor(Math.Log2(Math.Max(image.Width, image.Height))), "Excess image mips."); return bytes;
    }
    private static void ValidateStatic(SceneStaticResources value, SceneStaticResourceOptions options, ObjectTemplateCatalogEntry recipe, SceneObjectResources common)
    {
        var sources = common.Sources;
        bool Source(string path, string? pin = null) => sources.TryGetValue(path, out var s) && (pin is null || s.Sha256.Equals(pin, StringComparison.OrdinalIgnoreCase));
        Need(Same(value.Create, common.Create) && value.Recipe == recipe && value.ExternalReads == common.ExternalReads && value.SourceBytes == common.SourceBytes && value.MaterialPolicy == options.MaterialPolicy, "Static common metadata/policy mismatch.");
        var profile = value.Profile; var graph = value.Appearance;
        Need(profile is not null && graph is not null && graph.Geometry is not null && graph.Meshes is not null && graph.Meshes.Count <= options.Limits.MaximumResources &&
            profile.RootPath == recipe.ObjectPath && Source(profile.RootPath, recipe.ObjectSha256) && Source(profile.AppearancePath, recipe.AppearanceSha256), "Static root/appearance structure mismatch.");
        Subset(value.Sources, sources, true); Subset(profile.Sources, sources); Subset(graph.Sources, sources); Subset(graph.Geometry.Sources, sources);
        Need(profile.Sources.Keys.Concat(graph.Sources.Keys).ToHashSet().SetEquals(sources.Keys) && profile.Inheritance is { Count: > 0 and <= 16 } &&
            profile.Inheritance[0].SourcePath == recipe.ObjectPath && profile.Inheritance.Select(p => p.SourcePath).Distinct().Count() == profile.Inheritance.Count &&
            profile.Inheritance.All(p => profile.Sources.ContainsKey(p.SourcePath)) && profile.Sources.ContainsKey(profile.AppearanceField.SourcePath), "Static inheritance/source closure mismatch.");
        Need(graph.Geometry.Root.Path == profile.AppearancePath, "Static graph root mismatch.");
        var nodes = new Dictionary<string, AppearanceGeometryTemplate>(); var active = new HashSet<string>(); int edges = 0; long work = 0;
        void MeshShape(StaticMeshDescription mesh)
        {
            Need(mesh is not null && mesh.Primitives is { Count: > 0 and <= 1024 }, "Mesh primitive budget.");
                foreach (var primitive in mesh.Primitives)
                {
                    Need(primitive.Vertices is { Count: > 0 and <= 65536 } && primitive.Indices is { Count: > 0 } && primitive.Indices.Count % 3 == 0, "Static mesh buffer shape.");
                    work = checked(work + primitive.Vertices.Count + primitive.Indices.Count); Need(work <= 1048576, "Validation geometry work budget.");
                    Need(Source(primitive.ShaderPath) && primitive.Vertices.All(v => Finite(v.Position) && Finite(v.Normal) && float.IsFinite(v.TextureCoordinate.X) && float.IsFinite(v.TextureCoordinate.Y)) && primitive.Indices.All(i => i < primitive.Vertices.Count), "Static vertex/index/reference mismatch.");
                }
        }
        void Visit(AppearanceGeometryTemplate node, int depth)
        {
            Need(node is not null && depth <= options.Limits.MaximumDepth && Source(node.Path) && node.Diagnostics.Count == 0 && node.Children.Count <= options.Limits.MaximumEdges, "Static geometry/depth mismatch.");
            Need((node.Mesh is null ? 0 : 1) + (node.Detail is null ? 0 : 1) + (node.Component is null ? 0 : 1) == 1 && node.Children.Select(c => c.AuthoredIndex).Distinct().Count() == node.Children.Count, "Contradictory geometry family/child index.");
            Need(active.Add(node.Path), "Static graph cycle.");
            if (nodes.TryGetValue(node.Path, out var prior)) { Need(ReferenceEquals(prior, node), "Contradictory shared geometry."); active.Remove(node.Path); return; }
            nodes.Add(node.Path, node); Need(nodes.Count <= options.Limits.MaximumResources, "Static geometry count budget.");
            foreach (var child in node.Children)
            { Need(++edges <= options.Limits.MaximumEdges && child.AuthoredIndex >= 0 && Finite(child.LocalTransform.RowX) && Finite(child.LocalTransform.RowY) && Finite(child.LocalTransform.RowZ) && Finite(child.LocalTransform.Translation), "Static edge/placement mismatch."); Visit(child.Template, depth + 1); }
            if (node.Mesh is { } mesh)
            {
                Need(node.MeshSourcePath is not null && Source(node.MeshSourcePath) && mesh.Primitives is { Count: > 0 and <= 1024 }, "Static mesh source/structure mismatch.");
                MeshShape(mesh);
            }
            active.Remove(node.Path);
        }
        Visit(graph.Geometry.Root, 1);
        var meshNodes = nodes.Values.Where(n => n.Mesh is not null).ToDictionary(n => n.Path);
        Need(meshNodes.Count > 0 && graph.Meshes.Keys.All(meshNodes.ContainsKey) &&
            (options.MaterialPolicy != SceneStaticMaterialPolicy.RequireComplete || graph.Diagnostics.Count == 0 && graph.Meshes.Keys.ToHashSet().SetEquals(meshNodes.Keys)), "Static complete material policy mismatch.");
        var images = new Dictionary<string, DdsImage>(); long decoded = 0; bool selected = false;
        foreach (var pair in graph.Meshes)
        {
            var mesh = pair.Value; Need(mesh is not null && mesh.Materials.Count is > 0 and <= 256 && mesh.Textures.Count <= sources.Count && mesh.Effects.Count <= sources.Count && mesh.HdrTextures.Count <= sources.Count, "Static material map budgets.");
            MeshShape(mesh.Mesh); Need(Stardust.Core.Proof.StaticMeshVerification.ComputeGeometrySha256(mesh.Mesh) == Stardust.Core.Proof.StaticMeshVerification.ComputeGeometrySha256(meshNodes[pair.Key].Mesh!) && mesh.Materials.Count is > 0 and <= 256, "Static material/geometry mismatch.");
            Subset(mesh.Sources, sources); Need(mesh.Materials.Keys.ToHashSet().SetEquals(mesh.Mesh.Primitives.Select(p => p.ShaderPath)), "Static material keys mismatch.");
            foreach (var material in mesh.Materials)
            {
                Need(Source(material.Key) && material.Value is { References: not null, Parameters: not null } && mesh.Effects.TryGetValue(material.Value.References.EffectPath, out var effect) && effect is not null && material.Value.References.Textures.Count <= 256 && Source(material.Value.References.EffectPath), "Static shader/effect closure mismatch.");
                work = checked(work + material.Value.References.Textures.Count + 1); Need(work <= 1048576, "Validation material work budget.");
                foreach (var texture in material.Value.References.Textures) Need(texture.AssetPath is not null && Source(texture.AssetPath) && (mesh.Textures.ContainsKey(texture.AssetPath) || mesh.HdrTextures.ContainsKey(texture.AssetPath)), "Static texture reference mismatch.");
                if (material.Value.SwitchTexture is { } sw)
                {
                    var frame = options.SwitchTextureFrame;
                    Need(frame is not null && material.Key == frame.ShaderPath && sw.Selection == frame && Source(material.Key, frame.ShaderSha256) &&
                        sw.BaseShaderSource == sources[sw.Description.BaseShaderPath] && sw.Description.Frames.Count is > 0 and <= 256 && frame.FrameIndex < sw.Description.Frames.Count, "Static selected-frame identity mismatch.");
                    var chosen = sw.Description.Frames[frame.FrameIndex];
                    Need(material.Value.References.Textures.Any(t => t.Tag == chosen.TextureTag && t.AssetPath == chosen.TexturePath) && Source(chosen.TexturePath), "Static selected-frame texture mismatch."); selected = true;
                }
            }
            foreach (var texture in mesh.Textures)
            {
                Need(Source(texture.Key), "Static decoded image source absent.");
                if (images.TryGetValue(texture.Key, out var prior)) Need(ReferenceEquals(prior, texture.Value), "Contradictory shared image.");
                else { images.Add(texture.Key, texture.Value); decoded = checked(decoded + Image(texture.Value)); }
            }
            Need(mesh.HdrTextures.Count == 0, "HDR static validation requires a separately specified contract.");
        }
        Need(decoded <= options.MaximumDecodedBytes && (options.SwitchTextureFrame is null ? !selected : selected), "Static decoded/frame option mismatch.");
    }
    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
    private static void Need([DoesNotReturnIf(false)] bool value, string message) { if (!value) throw new InvalidDataException(message); }
    private static void ValidateCreature(SceneCreatureResources result, SceneCreatureResourceOptions options, ObjectTemplateCatalogEntry recipe)
    {
        var creature = result.Creature;
        Need(creature is not null && creature.Profile is not null && creature.Appearance is not null && creature.Sources is not null, "Incomplete creature result.");
        var sources = creature!.Sources;
        Need(sources.Count > 0 && sources.Count <= options.MaximumResources && result.ExternalReads == sources.Count &&
            sources.All(p => p.Value is not null && p.Key == p.Value.Path && p.Value.Bytes > 0) &&
            result.SourceBytes == sources.Values.Sum(p => (long)p.Bytes) && result.SourceBytes <= options.MaximumSourceBytes, "Creature source accounting mismatch.");
        bool Source(string path, string? pin = null) => sources.TryGetValue(path, out var identity) && (pin is null || identity.Sha256 == pin);
        Need(creature.Profile.RootPath == recipe.ObjectPath && Source(recipe.ObjectPath, recipe.ObjectSha256) &&
            Source(creature.Profile.AppearancePath, recipe.AppearanceSha256), "Creature root/appearance identity mismatch.");
        Need(creature.Skeletons is { Count: > 0 and <= 64 } && creature.Geometries is { Count: > 0 and <= 64 } &&
            creature.SkinSources is { Count: > 0 and <= 64 } && creature.LodMeshes is { Count: > 0 and <= 64 } && creature.LogicalTables is { Count: > 0 and <= 64 } &&
            creature.MaterialBundle is not null && creature.SelectedAnimation is not null && creature.PreparedAnimation is not null &&
            creature.AdditionalAnimations is { Count: 2 }, "Missing prepared creature closure.");
        Need(creature.Appearance.Skeletons.Count is > 0 and <= 64 && creature.Appearance.MeshGenerators.Count is > 0 and <= 64 &&
            creature.Appearance.Skeletons.All(p => Source(p.Path) && creature.Skeletons!.TryGetValue(p.Path, out var value) && value is not null && value.ExposedLevels().FirstOrDefault() is { Joints.Count: > 0 }) &&
            creature.Appearance.MeshGenerators.All(p => Source(p) && creature.LodMeshes!.TryGetValue(p, out var value) && value is not null && value.SerializedPaths.Count > 0 && creature.Geometries!.ContainsKey(value.SerializedPaths[0])), "Missing skeleton/detail0 geometry references.");
        var materials = creature.MaterialBundle!;
        Need(materials.Materials is { Count: > 0 and <= 64 } && materials.Textures is { Count: > 0 and <= 64 } && materials.Effects is { Count: > 0 and <= 64 } && materials.PaletteDefaults is not null,
            "Missing authored material closure.");
        Need(creature.Geometries!.All(p => Source(p.Key) && p.Value is not null && p.Value.Skin is not null && creature.SkinSources!.TryGetValue(p.Key, out var skin) && skin is not null &&
            p.Value.Shaders.Count > 0 && p.Value.Shaders.All(shader => materials.Materials.ContainsKey(shader.ShaderPath))) &&
            materials.Materials.All(p => Source(p.Key) && p.Value?.References is { } references && p.Value.Parameters is not null &&
                Source(references.EffectPath) && materials.Effects.TryGetValue(references.EffectPath, out var effect) && effect is not null &&
                references.Textures.All(t => t.AssetPath is string path && Source(path) && materials.Textures.ContainsKey(path))) &&
            materials.Textures.All(p => Source(p.Key) && p.Value is { Mips.Count: > 0 }), "Incomplete geometry/material dependencies.");
        void Clip(PreparedSkeletalAnimation clip, LogicalAnimationClipSelection selection)
        {
            Need(clip is not null && clip.ClipSha256 == selection.ClipSha256 && Source(clip.ClipPath, selection.ClipSha256) &&
                creature.Skeletons!.ContainsKey(clip.SkeletonPath) && creature.LogicalTables!.TryGetValue(selection.TablePath, out var table) && table is not null && Source(selection.TablePath) && clip.EndFrame > 0 && float.IsFinite(clip.FrameRate) && clip.FrameRate > 0,
                "Prepared clip identity/dependency mismatch.");
            var resolvedTable = creature.LogicalTables![selection.TablePath]; Need(resolvedTable.LookupEntries.Count <= 65536, "Logical table validation budget.");
            var template = resolvedTable.Find(selection.LogicalName).Template ?? throw new InvalidDataException("Selected logical proxy absent.");
            var proxy = LogicalAnimationProxyDecoder.Decode(template, selection.ExplicitChildRoute);
            Need(proxy.ClipPath == clip.ClipPath && proxy.EffectiveTimeScale == clip.ProxyTimeScale, "Prepared logical route/scale mismatch.");
        }
        var selected = creature.SelectedAnimation!;
        Need(selected.Selection is not null && selected.Proxy is not null && selected.Clip is not null && selected.Selection.TablePath == options.Idle.TablePath &&
            selected.Selection.LogicalName == options.Idle.LogicalName && selected.Selection.ClipSha256 == options.Idle.ClipSha256 &&
            (selected.Selection.ExplicitChildRoute ?? Array.Empty<int>()).SequenceEqual(options.Idle.ExplicitChildRoute ?? Array.Empty<int>()) &&
            selected.Proxy.ClipPath == creature.PreparedAnimation!.ClipPath, "Selected idle identity mismatch.");
        Clip(creature.PreparedAnimation!, options.Idle);
        foreach (string key in new[] { "walk", "run" })
        {
            Need(creature.AdditionalAnimations!.TryGetValue(key, out var clip) && clip is not null && clip.Locomotion is not null && clip.Locomotion.AuthoredSpeed > 0, "Missing prepared movement clip.");
            Clip(clip!, options.Movement[key]);
        }
    }
}
