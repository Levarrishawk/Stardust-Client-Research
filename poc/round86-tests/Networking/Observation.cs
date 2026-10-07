using System;
using System.Linq;
using Stardust.Core.Session;
public static class Networking86Observation
{
    public static object Project(SceneWorldObjectMotionSnapshot motion, SceneWorldObjectMotionStore store)
    {
        var t=motion.Transform;
        return new {ObjectId=motion.Incarnation.ObjectId,Position=new[]{t.Translation.X,t.Translation.Y,t.Translation.Z},
            Rows=new[]{t.RowX.X,t.RowX.Y,t.RowX.Z,t.RowY.X,t.RowY.Y,t.RowY.Z,t.RowZ.X,t.RowZ.Y,t.RowZ.Z},
            motion.Speed,motion.Revision,motion.MovementCounter,motion.Authoritative,Current=store.IsCurrent(motion)};
    }
}
