using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneCreatePlacementScenarios
{
    private static float[] V(Vector3 v) => new[] { v.X, v.Y, v.Z };
    private static float[][] Rows(AppearanceTransform t) => new[] { V(t.RowX), V(t.RowY), V(t.RowZ) };
    private static Vector3 S(Vector3 p) => new(p.X, p.Y, -p.Z);
    private static AppearanceTransform Reflect(AppearanceTransform t) => new(
        new(t.RowX.X, t.RowX.Y, -t.RowX.Z), new(t.RowY.X, t.RowY.Y, -t.RowY.Z),
        new(-t.RowZ.X, -t.RowZ.Y, t.RowZ.Z), S(t.Translation));
    private static void Near(Vector3 actual, Vector3 expected)
    {
        var a = V(actual); var b = V(expected);
        for (int i = 0; i < 3; i++)
            if (!float.IsFinite(a[i]) || Math.Abs((double)a[i] - b[i]) > 3e-6 * Math.Max(1, Math.Abs((double)b[i])))
                throw new Exception("Placement point/basis agreement failed.");
    }
    private static uint[] Bits(SceneCreateObjectByCrcMessage m) => new[] { m.QuaternionX, m.QuaternionY, m.QuaternionZ, m.QuaternionW, m.PositionX, m.PositionZ, m.PositionY }.Select(BitConverter.SingleToUInt32Bits).ToArray();
    internal static void Run()
    {
        float h = MathF.Sqrt(.5f), e = float.Epsilon, n = BitConverter.UInt32BitsToSingle(0x00800000);
        var fixtures = new (string Key, Quaternion Q)[] {
            ("identity", new(0,0,0,1)), ("x90", new(h,0,0,h)), ("y90", new(0,h,0,h)), ("z90", new(0,0,h,h)),
            ("x180", new(1,0,0,0)), ("y180", new(0,1,0,0)), ("z180", new(0,0,1,0)),
            ("x270", new(h,0,0,-h)), ("y270", new(0,h,0,-h)), ("z270", new(0,0,h,-h)),
            ("asymmetric", new(.2f,-.35f,.6f,.73f)), ("opposite", new(-.2f,.35f,-.6f,-.73f)),
            ("nonunit", new(2,3,-4,5)), ("scalar2", new(0,0,0,2)),
            ("tiny_x", new(e,0,0,0)), ("tiny_w", new(0,0,0,e)), ("tiny_mixed", new(e,-2*e,3*e,-4*e)),
            ("minimum_normal", new(n,-2*n,3*n,-4*n)), ("maximum", new(float.MaxValue,-float.MaxValue,float.MaxValue,float.MaxValue)),
            ("unbalanced", new(float.MaxValue,e,-1,2)), ("small_scaled", new(2e-30f,-3e-30f,4e-30f,5e-30f)),
            ("large_scaled", new(2e30f,-3e30f,4e30f,5e30f)), ("signed_zero", new(-0f,0,-0f,1)),
            ("zero_identity", new(0,0,0,0)), ("signed_zero_identity", new(-0f,0,-0f,-0f)),
            ("extreme_position", new(.2f,-.35f,.6f,.73f)), ("subnormal_position", new(0,0,0,1)) };
        var values = new Dictionary<string, SceneCreatePlacement>(StringComparer.Ordinal);
        int probes = 0, compositions = 0, rejections = 0;
        void Reject<T>(Action action) where T : Exception
        { try { action(); } catch (T) { rejections++; return; } throw new Exception("Expected " + typeof(T).Name); }
        foreach (var fixture in fixtures)
        {
            var q = fixture.Q;
            Vector3 p = fixture.Key == "extreme_position" ? new(float.MaxValue, -float.MaxValue, float.MaxValue)
                : fixture.Key == "subnormal_position" ? new(e, -e, -0f) : fixture.Key == "signed_zero" ? new(-0f, 0, -0f) : new(3.25f, -7.5f, 2.125f);
            var source = new SceneCreateObjectByCrcMessage(0xFEDCBA9876543210, q.X, q.Y, q.Z, q.W, p.X, p.Z, p.Y, 2937962913, 255);
            byte[] wire = SceneObjectLifecycleCodec.EncodeCreate(source);
            var raw = SceneObjectLifecycleCodec.DecodeCreate(wire); uint[] bits = Bits(raw);
            var policy = fixture.Key.EndsWith("identity", StringComparison.Ordinal) ? SceneCreateZeroQuaternionPolicy.Identity : SceneCreateZeroQuaternionPolicy.Reject;
            var placement = SceneCreatePlacementBuilder.Create(raw, policy); Array.Clear(wire);
            if (ReferenceEquals(placement.RawCreate, raw) || !Bits(placement.RawCreate).SequenceEqual(bits) || placement.RawCreate != source)
                throw new Exception("Raw create ownership/bits changed.");
            var t = placement.Transform; values.Add(fixture.Key, placement);
            foreach (var row in new[] { t.RowX, t.RowY, t.RowZ })
            {
                if (Math.Abs(row.LengthSquared() - 1) > 2e-6f) throw new Exception("Derived rotation row is not unit.");
            }
            if (Math.Abs(Vector3.Dot(t.RowX, t.RowY)) > 2e-6 || Math.Abs(Vector3.Dot(t.RowX, t.RowZ)) > 2e-6 || Math.Abs(Vector3.Dot(t.RowY, t.RowZ)) > 2e-6 || Math.Abs(Vector3.Dot(t.RowX, Vector3.Cross(t.RowY, t.RowZ)) - 1) > 2e-6)
                throw new Exception("Derived rotation is not orthogonal/right handed.");
            Near(t.Translation, p);
            Console.WriteLine("placement=" + JsonSerializer.Serialize(new { key = fixture.Key, raw = placement.RawCreate, rawBits = bits,
                rows = Rows(t), translation = V(t.Translation), unit = new[] { placement.UnitQuaternion.X, placement.UnitQuaternion.Y, placement.UnitQuaternion.Z, placement.UnitQuaternion.W },
                placement.InputQuaternionNorm, placement.AppliedNormalization, placement.UsedZeroIdentity, policy = (int)policy }));
            var engine = Reflect(t);
            var points = fixture.Key == "extreme_position" ? new[] { Vector3.Zero } : new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, new Vector3(1.25f, -2.5f, 3.75f) };
            foreach (var point in points)
            {
                var direction = t.TransformDirection(point); var world = t.TransformPoint(point);
                var enginePoint = engine.TransformPoint(S(point)); Near(enginePoint, S(world));
                Console.WriteLine("placement_point=" + JsonSerializer.Serialize(new { key = fixture.Key, point = V(point), direction = V(direction), world = V(world), enginePoint = V(enginePoint), engineRows = Rows(engine), engineTranslation = V(engine.Translation) })); probes++;
            }
            if (fixture.Key != "extreme_position")
            {
                AppearanceTransform parent = new(new(0,-2,.25f),new(1.5f,.125f,0),new(.2f,0,1.25f),new(10,-20,30));
                Vector3 point = new(-1.75f, 2.5f, .625f); var composed = parent.Compose(t); var world = composed.TransformPoint(point);
                Near(world, parent.TransformPoint(t.TransformPoint(point)));
                var engineComposed = Reflect(parent).Compose(engine); Near(engineComposed.TransformPoint(S(point)), S(world));
                Console.WriteLine("placement_composed=" + JsonSerializer.Serialize(new { key = fixture.Key, parentRows = Rows(parent), parentTranslation = V(parent.Translation),
                    point = V(point), rows = Rows(composed), translation = V(composed.Translation), world = V(world), engineRows = Rows(engineComposed), engineTranslation = V(engineComposed.Translation), enginePoint = V(engineComposed.TransformPoint(S(point))) })); compositions++;
            }
        }
        Near(values["x90"].Transform.TransformDirection(Vector3.UnitY), Vector3.UnitZ);
        Near(values["y90"].Transform.TransformDirection(Vector3.UnitZ), Vector3.UnitX);
        Near(values["z90"].Transform.TransformDirection(Vector3.UnitX), Vector3.UnitY);
        Near(values["asymmetric"].Transform.RowX, values["opposite"].Transform.RowX);
        Near(values["asymmetric"].Transform.RowY, values["opposite"].Transform.RowY);
        Near(values["asymmetric"].Transform.RowZ, values["opposite"].Transform.RowZ);
        var retained = values["asymmetric"];
        var valid = retained.RawCreate;
        for (int field = 0; field < 7; field++)
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                var bad = field switch { 0 => valid with { QuaternionX = invalid }, 1 => valid with { QuaternionY = invalid }, 2 => valid with { QuaternionZ = invalid },
                    3 => valid with { QuaternionW = invalid }, 4 => valid with { PositionX = invalid }, 5 => valid with { PositionZ = invalid }, _ => valid with { PositionY = invalid } };
                Reject<InvalidDataException>(() => SceneCreatePlacementBuilder.Create(bad, SceneCreateZeroQuaternionPolicy.Identity));
            }
        Reject<ArgumentNullException>(() => SceneCreatePlacementBuilder.Create(null!, SceneCreateZeroQuaternionPolicy.Reject));
        Reject<ArgumentOutOfRangeException>(() => SceneCreatePlacementBuilder.Create(valid, (SceneCreateZeroQuaternionPolicy)2));
        foreach (var zero in new[] { new Quaternion(0,0,0,0), new Quaternion(-0f,0,-0f,-0f) })
            Reject<InvalidDataException>(() => SceneCreatePlacementBuilder.Create(valid with { QuaternionX = zero.X, QuaternionY = zero.Y, QuaternionZ = zero.Z, QuaternionW = zero.W }, SceneCreateZeroQuaternionPolicy.Reject));
        if (SceneCreatePlacementBuilder.Create(valid, SceneCreateZeroQuaternionPolicy.Reject) != retained) throw new Exception("Failed calls changed independent result.");
        if (values["tiny_x"].InputQuaternionNorm != e || values["maximum"].InputQuaternionNorm <= float.MaxValue || values["zero_identity"].InputQuaternionNorm != 0 || !values["zero_identity"].UsedZeroIdentity || values["zero_identity"].AppliedNormalization)
            throw new Exception("Extreme/zero normalization metadata failed.");
        Console.WriteLine("placement_validation=" + JsonSerializer.Serialize(new { cases = values.Count, probes, compositions, rejections, ownership = "wire buffers cleared; raw scalar snapshot retained", source = "synthetic decoded creates; modern quaternion placement", gpu = false }));
        Console.WriteLine("PASS R6-ASSETS-01: modern robust decoded-create placement, column vectors, explicit zero policy and raw identity; no engine/root-motion/container inference.");
    }
}
