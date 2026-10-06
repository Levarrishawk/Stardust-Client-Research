using System;
using System.Text.Json;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

// Offline encoding boundary only. No socket or world application is performed.
public partial class ScenePumpedWorldPreview
{
    private void InspectFullTransform(SceneWorldLocalMotionDriverState state,int index)
    {
        if(!nominalSlotRequested)return;
        var t=state.Transform;
        var m=new System.Numerics.Matrix4x4(t.RowX.X,t.RowX.Y,t.RowX.Z,0,t.RowY.X,t.RowY.Y,t.RowY.Z,0,
            t.RowZ.X,t.RowZ.Y,t.RowZ.Z,0,0,0,0,1);
        var q=System.Numerics.Quaternion.CreateFromRotationMatrix(m);
        foreach(var direction in new[]{ObjectControllerTransformDirection.ClientToServer,ObjectControllerTransformDirection.ServerToClient})
        foreach(var kind in new[]{ObjectControllerTransformKind.Root,ObjectControllerTransformKind.WithParent})
        {
            uint word=direction==ObjectControllerTransformDirection.ClientToServer
                ?kind==ObjectControllerTransformKind.WithParent?0x7FC00000u:checked((uint)(state.TimestampMicroseconds/1000)):0;
            var message=new ObjectControllerTransformMessage(direction,kind,direction==ObjectControllerTransformDirection.ClientToServer?0x23u:0x1Bu,
                humanId,word,checked((uint)(index+1)),kind==ObjectControllerTransformKind.WithParent?0UL:null,
                q.X,q.Y,q.Z,q.W,t.Translation.X,t.Translation.Y,t.Translation.Z,state.Speed);
            var bytes=ObjectControllerTransformCodec.Encode(message);
            var decoded=ObjectControllerTransformCodec.Decode(bytes,direction);
            Check(decoded==message&&ObjectControllerTransformCodec.Encode(decoded).AsSpan().SequenceEqual(bytes),"Actual directional full transform roundtrip");
            GD.Print("terrain_full_transform="+JsonSerializer.Serialize(new{GlobalStep=index,Direction=direction.ToString(),Kind=kind.ToString(),
                Hex=Convert.ToHexString(bytes),message.OuterWord,message.MovementCounter,QuaternionBits=new[]{BitConverter.SingleToUInt32Bits(q.X),BitConverter.SingleToUInt32Bits(q.Y),BitConverter.SingleToUInt32Bits(q.Z),BitConverter.SingleToUInt32Bits(q.W)},
                ActualDriverPosition=true,SyntheticParentEnvelope=kind==ObjectControllerTransformKind.WithParent,
                TimestampPolicy=direction==ObjectControllerTransformDirection.ClientToServer?(kind==ObjectControllerTransformKind.WithParent?"SuppliedNaNPatternIntegerWord":"SuppliedMicrosecondsDividedBy1000"):"ServerZeroScalar",
                Sent=false,WorldApplied=false,OriginalSenderTimingParity=false}));
        }
    }
}
