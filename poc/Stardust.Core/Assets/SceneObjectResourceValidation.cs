using System.Diagnostics.CodeAnalysis;
using System.Buffers.Binary;
using System.Security.Cryptography;
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
                Need(result.Creature.Creature.BlendShapes is null && result.Creature.Creature.ShapeSelection is null && result.Creature.Creature.CustomizationValues is null &&
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
                if(creatureOptions.FaceInspection is null)
                    Need(result.Creature.Creature.FaceBlueprints is null && result.Creature.Creature.FaceResources is null && result.Creature.Creature.GeneratedFaces is null,"Unrequested face extensions.");
                else ValidateFaces(result,creatureOptions.FaceInspection);
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
        Need(profile.Sources.Keys.Concat(graph.Sources.Keys).Concat(value.SwitchFrames?.Sources.Keys??Enumerable.Empty<string>()).ToHashSet().SetEquals(sources.Keys) && profile.Inheritance is { Count: > 0 and <= 16 } &&
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
        var allPolicy=options.SwitchFrames; var bundle=value.SwitchFrames; var proof=value.SwitchFrameProof;
        Need((allPolicy is null)==(bundle is null) && (bundle is null)==(proof is null),"Unrequested/missing frame bundle.");
        if(allPolicy is not null)
        {
            allPolicy.Validate(options.MaterialPolicy,options.SwitchTextureFrame);
            var fl=allPolicy.Limits??new(); var d=fl.DdsLimits??new();
            Need(bundle is not null && proof is not null && ReferenceEquals(proof.Bundle,bundle) && proof.Policy==allPolicy && proof.Selected==options.SwitchTextureFrame!.FrameIndex,"Frame association/options mismatch.");
            Need(bundle.FrameCount==bundle.Frames.Count && bundle.FrameCount is >0 and <=256 && bundle.FrameCount<=fl.MaximumFrames && bundle.Images.Count<=fl.MaximumFrames && bundle.MipCount<=fl.MaximumMips && bundle.MipCount<=allPolicy.MaximumRetainedMips && bundle.DecodedBytes<=fl.MaximumDecodedBytes && bundle.DecodedBytes<=options.MaximumDecodedBytes && bundle.SourceBytes<=fl.MaximumSourceBytes && bundle.Sources.Count<=fl.MaximumSources,"Frame bounded validation preflight.");
            Need(bundle.ShaderPath==options.SwitchTextureFrame!.ShaderPath && Source(bundle.ShaderPath,options.SwitchTextureFrame.ShaderSha256) && float.IsFinite(bundle.Interval) && bundle.Interval>0,"Frame shader/interval mismatch.");
            Subset(bundle.Sources,sources);
            var needed=bundle.Frames.Select(f=>f.TexturePath).Append(bundle.ShaderPath).Append(bundle.BaseShaderPath).ToHashSet(StringComparer.Ordinal);
            Need(needed.SetEquals(bundle.Sources.Keys) && bundle.Frames.Select(f=>f.TexturePath).ToHashSet(StringComparer.Ordinal).SetEquals(bundle.Images.Keys),"Frame exact source/image union.");
            for(int i=0;i<bundle.Frames.Count;i++)Need(bundle.Frames[i].Index==i && bundle.Images.TryGetValue(bundle.Frames[i].TexturePath,out var image) && ReferenceEquals(image,bundle.Frames[i].Image),"Frame alias/order mismatch.");
            foreach(var image in bundle.Images.Values)
            {
                Need(image.Mips.Count is >0 and <=32 && image.Mips[0].Width<=d.MaximumDimension && image.Mips[0].Height<=d.MaximumDimension && image.Mips.Sum(m=>(long)m.DecodedBytes)<=d.MaximumDecodedBytes,"Frame per-image validation bounds.");
            }
            Need(bundle.Sources.All(p=>p.Value.Bytes<=(p.Key.StartsWith("shader/",StringComparison.Ordinal)?fl.MaximumShaderBytes:d.MaximumInputBytes)),"Frame per-source validation cap.");
        }
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
                    Need(material.Value.References.Textures.Any(t => t.Tag == chosen.TextureTag && t.AssetPath == chosen.TexturePath) && Source(chosen.TexturePath), "Static selected-frame texture mismatch.");
                    if(bundle is not null)
                    {
                        Need(sw.Description.BaseShaderPath==bundle.BaseShaderPath && sw.Description.RawDtstWord0==bundle.RawDtstWord0 && sw.Description.RawDtstWord1==bundle.RawDtstWord1 && sw.Description.Frames.Count==bundle.Frames.Count &&
                            sw.Description.Frames.Select((f,i)=>f.TextureTag==bundle.Frames[i].TextureTag && f.TexturePath==bundle.Frames[i].TexturePath).All(b=>b),"Frame authored description mismatch.");
                        Need(sw.Description.Metadata.Payload.Length<=sources[bundle.ShaderPath].Bytes && material.Value.References.Metadata.Payload.Length<=sources[bundle.BaseShaderPath].Bytes && material.Value.References.Textures.All(t=>t.BindingMetadata.Length<=4096) && proof!.MatchesMaterial(sw.Description,material.Value.References),"Selected material metadata contradicts bundle.");
                        Need(mesh.Textures.TryGetValue(chosen.TexturePath,out var selectedImage) && selectedImage is not null && selectedImage.Mips.Count is >0 and <=32 && selectedImage.Mips.Count<=bundle.MipCount && selectedImage.Mips.Sum(m=>(long)m.Rgba.Length)<=options.MaximumDecodedBytes && proof!.Matches(selectedImage,frame.FrameIndex),"Selected appearance pixels contradict immutable frames.");
                    }
                    selected = true;
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
        if(bundle is not null)
        {
            var receipt=SceneObjectResourceAccounting.Measure(common);
            Need(receipt.DecodedTextureBytes<=options.MaximumDecodedBytes && receipt.Mips<=allPolicy!.MaximumRetainedMips && receipt.PendingWeight<=allPolicy.MaximumPendingWeight,"Combined retained frame budget.");
        }
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

    private static void ValidateFaces(SceneObjectResources common,SceneCreatureFaceInspectionPolicy policy)
    {
        var creature=common.Creature!.Creature; var blueprints=creature.FaceBlueprints;
        var resources=creature.FaceResources; var generated=creature.GeneratedFaces;
        Need(blueprints is {Count:>0 and <=16} && resources is not null && generated is not null,"Complete face inspection closure required.");
        Need(resources.Shaders is {Count:>0 and <=64} && resources.EffectsByShader is {Count:>0 and <=64} &&
            resources.Textures is {Count:>0 and <=64} && resources.Palettes is {Count:<=64} && resources.PixelPrograms is {Count:>0 and <=64},"Face dependency budgets.");
        Need(generated.Values is {Count:<=4096} && generated.Outputs is {Count:>0 and <=16} && generated.Bindings is {Count:>0 and <=4096},"Generated face budgets.");
        bool Source(string path)=>common.Sources.ContainsKey(path);
        void Tree(IffNode root,string path)
        {
            Need(Source(path) && root is not null && root.Payload.Length<=common.Sources[path].Bytes,"Face metadata/source mismatch.");
            int nodes=0;
            void Visit(IffNode node,int depth)
            {
                Need(node is not null && depth<=16 && ++nodes<=16384 && node.Children.Count<=4096 && node.Payload.Length<=common.Sources[path].Bytes,"Face metadata work budget.");
                foreach(var child in node.Children)Visit(child,depth+1);
            }
            Visit(root,1);
        }
        var keys=new HashSet<BlueprintCustomizationKey>(); int operations=0;
        foreach(var pair in blueprints)
        {
            var b=pair.Value;
            Need(Source(pair.Key) && b is not null && b.Width is >0 and <=1024 && b.Height is >0 and <=1024 &&
                b.DestinationFormatIds is {Count:>0 and <=32} && b.ShaderPaths is {Count:>0 and <=256} && b.TexturePaths is {Count:<=4096} &&
                b.VertexArrays is {Count:>0 and <=256} && b.RenderSets is {Count:>0 and <=256} && b.PrepareLists is {Count:<=256},"Face blueprint budget/source mismatch.");
            foreach(var set in b.RenderSets)Need(set is not null && set.VertexArrayIndices is {Count:>0 and <=256},"Face render set budget.");
            foreach(var list in b.PrepareLists)
            {
                Need(list is {Count:<=4096},"Face prepare list budget.");
                foreach(var op in list)
                {
                    Need(op is not null && ++operations<=4096 && op.TargetIndex<b.ShaderPaths.Count && op.TextureTag==0x4D41494E && op.Kind is "SSTC" or "SST1" or "STFP","Face prepare operation budget/policy.");
                    if(op.Kind=="STFP")Need(op.PalettePath is not null && op.VariableName is not null && op.RawFactoryFlag is 0 or 1,"Face palette operation.");
                    else Need(op.SourceIndex is {} start && start<b.TexturePaths.Count && (op.Kind!="SST1" || op.VariableName is not null && op.SourceCount is >0 && (long)start+op.SourceCount<=b.TexturePaths.Count),"Face texture selection range.");
                    if(op.VariableName is {} name)
                    {
                        Need(name.Length is >0 and <=256 && name.All(c=>c is >=(char)33 and <=(char)126),"Face variable name.");
                        keys.Add(new(name,op.Kind=="SST1" || op.RawFactoryFlag==1));
                    }
                }
            }
            foreach(var array in b.VertexArrays)Tree(array,pair.Key);
        }
        Need(keys.SetEquals(policy.Values.Keys) && keys.SetEquals(generated.Values.Keys) && policy.Values.All(p=>generated.Values[p.Key]==p.Value),"Face option/value association mismatch.");
        var shaderKeys=blueprints.Values.SelectMany(b=>b.ShaderPaths).ToHashSet(StringComparer.Ordinal);
        Need(shaderKeys.SetEquals(resources.Shaders.Keys) && shaderKeys.SetEquals(resources.EffectsByShader.Keys),"Face shader/effect closure mismatch.");
        foreach(var pair in resources.Shaders)
        {
            Need(pair.Value is not null && pair.Value.Textures is {Count:<=256},"Face shader budget."); Tree(pair.Value.Metadata,pair.Key);
            var effect=resources.EffectsByShader[pair.Key];
            Need(effect is not null && effect.Implementations is {Count:>0 and <=64} && effect.Implementations.All(i=>i is not null && i.Passes is {Count:<=64} && i.Passes.All(p=>p is not null && p.Fields.Count<=256 && p.PixelTextures.Count<=64)),"Face effect pass budget.");
            Tree(effect.Metadata,pair.Value.InlineEffect is null?pair.Value.EffectPath:pair.Key);
        }
        var textures=blueprints.Values.SelectMany(b=>b.TexturePaths).Concat(resources.Shaders.Values.SelectMany(s=>s.Textures).Select(t=>t.AssetPath).OfType<string>()).ToHashSet(StringComparer.Ordinal);
        Need(textures.SetEquals(resources.Textures.Keys),"Face texture closure mismatch.");
        foreach(var pair in resources.Textures) { Need(Source(pair.Key),"Face input source absent."); _=Image(pair.Value); }
        var palettes=blueprints.Values.SelectMany(b=>b.PrepareLists).SelectMany(l=>l).Select(op=>op.PalettePath).OfType<string>().ToHashSet(StringComparer.Ordinal);
        Need(palettes.SetEquals(resources.Palettes.Keys),"Face palette closure mismatch.");
        foreach(var pair in resources.Palettes)Need(Source(pair.Key) && pair.Value is {Entries.Count:>0 and <=1024} && (pair.Value.Version>>8)==3 && 24L+pair.Value.Entries.Count*4<=common.Sources[pair.Key].Bytes,"Face palette metadata/source mismatch.");
        var programs=resources.EffectsByShader.Values.SelectMany(e=>e.Implementations).SelectMany(i=>i.Passes).Select(p=>p.PixelProgram).OfType<string>().ToHashSet(StringComparer.Ordinal);
        Need(programs.SetEquals(resources.PixelPrograms.Keys),"Face program closure mismatch.");
        foreach(var pair in resources.PixelPrograms)
        {
            var program=pair.Value; Need(program is not null && program.TextureStage==0 && program.FactorRegister==3,"Face pixel program registers."); Tree(program.Metadata,pair.Key);
            var root=program.Metadata; Need(root.IsForm("PSHP") && root.Children.Count==1 && root.Children[0].IsForm("0000"),"Face program envelope.");
            var parts=root.Children[0].Children; Need(parts.Count==2 && parts[0].Tag=="PSRC" && parts[1].Tag=="PEXE","Face program chunks.");
            var text=parts[0].Payload.Span; Need(text.Length is >0 and <=65536 && text[^1]==0 && text[..^1].IndexOf((byte)0)<0,"Face program text.");
            uint[] tokens={0xFFFF0101,0x00000042,0xB00F0000,0x00000005,0x80070000,0xB0E40000,0xA0E40003,0x40000001,0x80080000,0xB0FF0000,0x0000FFFF};
            var code=parts[1].Payload.Span; Need(code.Length==tokens.Length*4,"Face program token extent.");
            for(int i=0;i<tokens.Length;i++)Need(BinaryPrimitives.ReadUInt32LittleEndian(code[(i*4)..])==tokens[i],"Face program tokens.");
        }
        var expected=new Dictionary<BlueprintGeneratedTextureKey,BlueprintGeneratedTextureBinding>(); var referenced=new HashSet<string>(StringComparer.Ordinal); int rendererWork=0;
        foreach(var mesh in creature.Geometries!)
        {
            Need(mesh.Value.TextureRenderers is {Count:<=256} && mesh.Value.Shaders.Count<=256,"Face geometry reference budget.");
            foreach(var renderer in mesh.Value.TextureRenderers)
            {
                Need(renderer is not null && ++rendererWork<=4096 && blueprints.ContainsKey(renderer.Path) && renderer.Bindings is {Count:>0 and <=4096},"Face renderer closure/budget."); referenced.Add(renderer.Path);
                foreach(var target in renderer.Bindings)
                {
                    Need(++rendererWork<=4096 && target.TextureTag==0x4D41494E && target.ShaderIndex<mesh.Value.Shaders.Count,"Face MAIN slot binding.");
                    var key=new BlueprintGeneratedTextureKey(mesh.Key,target.ShaderIndex,target.TextureTag);
                    var binding=new BlueprintGeneratedTextureBinding(renderer.Path,mesh.Value.Shaders[(int)target.ShaderIndex].ShaderPath);
                    if(expected.TryGetValue(key,out var old))Need(old==binding,"Conflicting face destination."); else expected.Add(key,binding);
                }
            }
        }
        Need(expected.Count>0 && referenced.SetEquals(blueprints.Keys) && referenced.SetEquals(generated.Outputs.Keys) && expected.Keys.ToHashSet().SetEquals(generated.Bindings.Keys) && expected.All(p=>generated.Bindings[p.Key]==p.Value),"Face output/binding closure mismatch.");
        foreach(var pair in blueprints)
        {
            var output=generated.Outputs[pair.Key]; Need(output is not null && output.Image is not null,"Face output absent.");
            Need(output.Image.Format==DdsFormat.Rgba32 && output.Image.ColorSpace==DdsColorSpace.Unspecified && output.Image.AlphaMode==DdsAlphaMode.Straight && output.Image.Mips.Count==1 && output.Image.Width==pair.Value.Width && output.Image.Height==pair.Value.Height,"Face output extent/format policy.");
            _=Image(output.Image);
            var preparation=BlueprintFacePreparer.Prepare(pair.Value,resources,policy.Values);
            FaceLayerGate.Check(pair.Value,resources,preparation,output);
        }
        var receipt=SceneObjectResourceAccounting.Measure(common);
        Need(checked(receipt.DecodedTextureBytes+receipt.GeneratedFaceBytes)<=policy.MaximumDecodedBytes,"Face combined decoded budget.");
    }
    private static class FaceLayerGate
    {
        private const uint Main=0x4D41494E;
        private readonly record struct Vertex(double X,double Y,double U,double V);
        private sealed record Draw(uint Shader,uint Array,string Path,DdsImage Image,uint Factor,bool Blend,bool ClampU,bool ClampV,Vertex[] Vertices);

        public static void Check(BlueprintTextureRendererDescription blueprint,BlueprintFaceResources resources,
            BlueprintFacePreparation preparation,BlueprintComposedFace output)
        {
            ArgumentNullException.ThrowIfNull(blueprint);ArgumentNullException.ThrowIfNull(resources);ArgumentNullException.ThrowIfNull(preparation);
            var limits=new BlueprintFaceRasterLimits();
            if(limits.MaximumDimension is <1 or >4096||limits.MaximumTriangles is <1 or >65536||limits.MaximumPixelTests<1)throw new ArgumentOutOfRangeException(nameof(limits));
            Need(blueprint.Width>0&&blueprint.Height>0&&blueprint.Width<=limits.MaximumDimension&&blueprint.Height<=limits.MaximumDimension,"Face destination budget.");
            Need(float.IsFinite(blueprint.CameraScalar)&&blueprint.CameraScalar>0,"Face camera invalid.");
            Need(preparation.ShaderSlots.Count==blueprint.ShaderPaths.Count,"Prepared face slot count differs.");
            int width=(int)blueprint.Width,height=(int)blueprint.Height,triangleCount=0;long pixelTests=0;
            var draws=new List<Draw>();
            foreach(var set in blueprint.RenderSets)
            {

                Need(set.ShaderIndex<preparation.ShaderSlots.Count,"Face render shader out of range.");
                var slot=preparation.ShaderSlots[(int)set.ShaderIndex];
                Need(slot.Slot==set.ShaderIndex&&slot.ShaderPath==blueprint.ShaderPaths[(int)set.ShaderIndex],"Prepared shader identity differs.");
                Need(resources.EffectsByShader.TryGetValue(slot.ShaderPath,out var effect)&&resources.Shaders.TryGetValue(slot.ShaderPath,out _),"Face effect absent.");
                var passes=effect!.Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0]).Where(p=>p.PixelProgram is not null).ToArray();
                Need(passes.Length==1,"Face preview requires one programmable candidate.");var pass=passes[0];
                Need(pass.Version=="0007"&&pass.VertexProgram=="vertex_program/texren_copy_c1a1.vsh"&&pass.FixedStageCount==0,"Unsupported face vertex/pass policy.");
                uint Field(int offset){Need(pass.Fields.TryGetValue(offset,out uint value),"Face pass field absent.");return value;}
                Need(Field(0x19)==0&&Field(0x1A)==0&&Field(0x30)==0&&Field(0x4C)==0&&Field(0x3C)==15&&Field(0x24)==0&&Field(0x28)==4&&Field(0x2C)==5&&Field(0x40)==Main&&Field(0x44)==0&&Field(0x20)<=1,"Unsupported face blend/depth/stencil/factor policy.");
                Need(resources.PixelPrograms is not null&&resources.PixelPrograms.TryGetValue(pass.PixelProgram!,out var program)&&program.TextureStage==0&&program.FactorRegister==3,"Validated face pixel program required.");
                Need(pass.PixelTextures.Count==1&&pass.PixelTextures[0].Stage==0&&pass.PixelTextures[0].TextureTag==Main,"Unsupported face texture stage.");
                var pixel=pass.PixelTextures[0];var inherited=pixel.Metadata.Span;
                Need(pixel.Version=="0000"&&inherited.Length==16&&U32(inherited[5..])==Main&&inherited[9..].SequenceEqual(new byte[]{5,5,5,6,6,6,0}),"Unsupported face sampler inheritance.");
                var shader=resources.Shaders[slot.ShaderPath];var binding=shader.Textures.Where(t=>t.Tag==Main).ToArray();
                Need(binding.Length==1&&binding[0].Version=="0002"&&binding[0].BindingMetadata.Length==12,"Unsupported face texture binding.");
                var sampler=binding[0].BindingMetadata.Span;
                Need(sampler[5] is 0 or 2&&sampler[6] is 0 or 2&&sampler[7]==0&&sampler[8]==1&&sampler[9]==2&&sampler[10]==2&&sampler[11]==0,"Unsupported face sampler.");
                var coordinates=shader.Metadata.Children[0].Children.Where(n=>n.IsForm("TCSS")).ToArray();
                Need(coordinates.Length==1&&coordinates[0].Children.Count==1&&coordinates[0].Children[0].Tag=="0000","Face coordinates absent.");
                var map=coordinates[0].Children[0].Payload.Span;
                Need(map.Length==5&&U32(map)==Main&&map[4]==0,"Face preview requires MAIN/UV0.");
                Need(slot.TextureBindings.TryGetValue(Main,out string? path)&&resources.Textures.TryGetValue(path,out _),"Prepared MAIN texture absent.");
                Need(slot.TextureFactors.TryGetValue(Main,out uint factor),"Prepared MAIN factor absent.");var image=resources.Textures[path!];
                Need(image.Mips.Count is >0 and <=16&&image.ColorSpace!=DdsColorSpace.Srgb&&image.AlphaMode!=DdsAlphaMode.Premultiplied,"Unsupported face image policy.");
                for(int mip=0;mip<image.Mips.Count;mip++)
                {
                    var level=image.Mips[mip];Need(level.Width>0&&level.Height>0&&level.Width<=8192&&level.Height<=8192&&level.Rgba.Length==(long)level.Width*level.Height*4,"Invalid face pixels.");
                    Need(level.Width==Math.Max(1,image.Width>>mip)&&level.Height==Math.Max(1,image.Height>>mip),"Invalid face mip dimensions.");
                }
                foreach(uint index in set.VertexArrayIndices)
                {
                    Need(index<blueprint.VertexArrays.Count,"Face vertex array out of range.");var node=blueprint.VertexArrays[(int)index];
                    Need(node.IsForm("VTXA")&&node.Children.Count==1&&node.Children[0].IsForm("0003"),"Unsupported face vertices.");var chunks=node.Children[0].Children;
                    Need(chunks.Count==2&&chunks[0].Tag=="INFO"&&chunks[0].Payload.Length==8&&U32(chunks[0].Payload.Span)==0x1101&&chunks[1].Tag=="DATA","Face vertex declaration invalid.");
                    uint count=U32(chunks[0].Payload.Span[4..]);Need(count>=3&&count<=limits.MaximumTriangles+2&&chunks[1].Payload.Length==(long)count*20,"Face vertex/triangle budget.");
                    var vertices=new Vertex[count];var bytes=chunks[1].Payload.Span;float z=F32(bytes[8..]);
                    Need(float.IsFinite(z)&&z>=1&&z<=5000,"Face depth outside camera.");
                    for(int v=0;v<vertices.Length;v++)
                    {
                        var b=bytes[(v*20)..];float x=F32(b),y=F32(b[4..]),u=F32(b[12..]),vv=F32(b[16..]);
                        Need(float.IsFinite(x)&&float.IsFinite(y)&&float.IsFinite(u)&&float.IsFinite(vv)&&F32(b[8..])==z,"Face fan requires finite planar coordinates.");
                        double screenX=x/(double)blueprint.CameraScalar*width,screenY=y/(double)blueprint.CameraScalar*height;
                        Need(Math.Abs(screenX)<=1e6&&Math.Abs(screenY)<=1e6&&Math.Abs(u)<=1e4&&Math.Abs(vv)<=1e4,"Face coordinate budget.");vertices[v]=new(screenX,screenY,u,vv);
                    }
                    for(int t=1;t<vertices.Length-1;t++)
                    {
                        Need(++triangleCount<=limits.MaximumTriangles,"Face triangle budget.");double area=Edge(vertices[0],vertices[t],vertices[t+1].X,vertices[t+1].Y);
                        Need(area>=0,"Unsupported face fan winding.");if(area==0)continue;
                        var box=Bounds(vertices[0],vertices[t],vertices[t+1],width,height);
                        pixelTests+=(long)Math.Max(0,box.Right-box.Left)*Math.Max(0,box.Bottom-box.Top);Need(pixelTests<=limits.MaximumPixelTests,"Face raster work budget.");
                    }
                    draws.Add(new(set.ShaderIndex,index,path!,image,factor,Field(0x20)!=0,sampler[5]==2,sampler[6]==2,vertices));
                }
            }

            Need(output.Layers is not null && output.Layers.Count==draws.Count && draws.Count>0,"Face layer closure mismatch.");
            int layerIndex=0;
            foreach(var draw in draws)
            {
                var expectedMips=new List<int>(); long maximumWrites=0;
                for(int triangle=1;triangle<draw.Vertices.Length-1;triangle++)
                {
                    Vertex a=draw.Vertices[0],b=draw.Vertices[triangle],c=draw.Vertices[triangle+1]; double area=Edge(a,b,c.X,c.Y); if(area==0)continue;
                    double dudx=((b.U-a.U)*(c.Y-a.Y)-(c.U-a.U)*(b.Y-a.Y))/area;
                    double dudy=((b.X-a.X)*(c.U-a.U)-(c.X-a.X)*(b.U-a.U))/area;
                    double dvdx=((b.V-a.V)*(c.Y-a.Y)-(c.V-a.V)*(b.Y-a.Y))/area;
                    double dvdy=((b.X-a.X)*(c.V-a.V)-(c.X-a.X)*(b.V-a.V))/area;
                    double footprint=Math.Max(Math.Sqrt(Math.Pow(dudx*draw.Image.Width,2)+Math.Pow(dvdx*draw.Image.Height,2)),Math.Sqrt(Math.Pow(dudy*draw.Image.Width,2)+Math.Pow(dvdy*draw.Image.Height,2)));
                    expectedMips.Add((int)Math.Clamp(Math.Floor(Math.Log2(Math.Max(1,footprint))+.5),0,draw.Image.Mips.Count-1));
                    var box=Bounds(a,b,c,width,height); maximumWrites+=(long)Math.Max(0,box.Right-box.Left)*Math.Max(0,box.Bottom-box.Top);
                }
                var layer=output.Layers[layerIndex++];
                Need(layer is not null && layer.ShaderIndex==draw.Shader && layer.VertexArrayIndex==draw.Array && layer.TexturePath==draw.Path && layer.MipLevels is not null && layer.MipLevels.SequenceEqual(expectedMips) && layer.WrittenPixels>=0 && layer.WrittenPixels<=maximumWrites && layer.RgbaSha256 is {Length:64} && layer.RgbaSha256.All(Uri.IsHexDigit),"Face layer association/LOD/hash metadata mismatch.");
            }
            Need(output.Layers[^1].RgbaSha256.Equals(Convert.ToHexString(SHA256.HashData(output.Image.Mips[0].Rgba.Span)),StringComparison.OrdinalIgnoreCase),"Final face layer/pixel hash mismatch.");
        }
        private static (int Left,int Top,int Right,int Bottom) Bounds(Vertex a,Vertex b,Vertex c,int width,int height)
            =>(Math.Clamp((int)Math.Ceiling(Math.Min(a.X,Math.Min(b.X,c.X))-.5),0,width),Math.Clamp((int)Math.Ceiling(Math.Min(a.Y,Math.Min(b.Y,c.Y))-.5),0,height),
                Math.Clamp((int)Math.Floor(Math.Max(a.X,Math.Max(b.X,c.X))-.5)+1,0,width),Math.Clamp((int)Math.Floor(Math.Max(a.Y,Math.Max(b.Y,c.Y))-.5)+1,0,height));
        private static double Edge(Vertex a,Vertex b,double x,double y)=>(b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X);
        private static uint U32(ReadOnlySpan<byte> bytes)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        private static float F32(ReadOnlySpan<byte> bytes)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));
        private static void Need([DoesNotReturnIf(false)] bool value,string message){if(!value)throw new InvalidDataException(message);}
    }
}
