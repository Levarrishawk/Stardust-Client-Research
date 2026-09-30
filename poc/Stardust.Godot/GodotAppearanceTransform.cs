using System;
using Godot;
using Stardust.Core.Assets;

internal static class GodotAppearanceTransform
{
    internal static Transform3D ToGodot(AppearanceTransform t)=>new(new Basis(
        new(t.RowX.X,t.RowY.X,-t.RowZ.X),new(t.RowX.Y,t.RowY.Y,-t.RowZ.Y),new(-t.RowX.Z,-t.RowY.Z,t.RowZ.Z)),
        new(t.Translation.X,t.Translation.Y,-t.Translation.Z));
    internal static AppearanceTransform ToCore(Transform3D t)=>new(
        new(t.Basis.X.X,t.Basis.Y.X,-t.Basis.Z.X),new(t.Basis.X.Y,t.Basis.Y.Y,-t.Basis.Z.Y),
        new(-t.Basis.X.Z,-t.Basis.Y.Z,t.Basis.Z.Z),new(t.Origin.X,t.Origin.Y,-t.Origin.Z));
    internal static float Distance(byte? pivot,AppearanceExtent? extent,Transform3D world,Vector3 camera)
    {
        double value=DetailAppearanceDistance.Measure(pivot,extent,ToCore(world),new(camera.X,camera.Y,-camera.Z));
        return (float)Math.Min(value,float.MaxValue);
    }
}
