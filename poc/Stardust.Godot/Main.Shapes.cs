using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

public partial class Main
{
    private (Dictionary<string,SpinBox> Controls,Button Reset) InstallShapeControls(VBoxContainer panel,
        CreatureAppearanceDescriptorBundle source,SkeletalMeshPreview preview,bool verifyPublication,Func<object?>? gameplayState=null)
    {
        if(source.BlendShapes!.Values.Sum(s=>s.Targets.Sum(t=>(long)t.Positions.Count+t.Normals.Count+t.Tangents.Count))>65536)
            throw new AssetPreviewException("Interactive shape preview exceeds its bounded delta budget.");
        var values=source.ShapeSelection!.Values.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);var inputs=new Dictionary<string,SpinBox>();
        panel.AddChild(new Label{Text="Character shape"});var status=new Label{Text="Ready"};
        void Publish()
        {
            string identity=preview.GeometryFingerprint(false);var transform=preview.Root.Transform;var state=gameplayState?.Invoke();
            int textures=preview.TextureCount;long bytes=preview.TextureBytes;
            try{preview.ApplyShapes(SkeletalAppearanceShapes.Apply(source,values));status.Text="Ready";}
            catch(Exception error)when(error is InvalidDataException or AssetPreviewException){status.Text="Could not update shape.";GD.Print("Shape update failed: "+error.GetType().Name);return;}
            if(identity!=preview.GeometryFingerprint(false)||transform!=preview.Root.Transform||!Equals(state,gameplayState?.Invoke())||textures!=preview.TextureCount||bytes!=preview.TextureBytes)
                throw new AssetPreviewException("Shape edit changed retained topology/material/actor resources.");
            if(verifyPublication)preview.VerifyBoundary(DisplayServer.GetName()!="headless");
        }
        foreach(var pair in values.OrderBy(p=>p.Key,StringComparer.Ordinal))
        {
            string label=pair.Key.StartsWith("blend_",StringComparison.Ordinal)?pair.Key[6..]:pair.Key;
            label=label.Replace("chinsize","chin size").Replace("eyedirection","eye direction").Replace("eyeshape","eye shape").Replace("eyesize","eye size")
                .Replace("lipfullness","lip fullness").Replace("lipwidth","lip width").Replace("nosedepth","nose depth").Replace("noselength","nose length").Replace("nosewidth","nose width")
                .Replace("_0"," A").Replace("_1"," B").Replace('_',' ');
            var row=new HBoxContainer();panel.AddChild(row);row.AddChild(new Label{Text=char.ToUpperInvariant(label[0])+label[1..],CustomMinimumSize=new(175,0)});
            var input=new SpinBox{MinValue=0,MaxValue=255,Step=1,Value=pair.Value,CustomMinimumSize=new(100,0)};row.AddChild(input);inputs.Add(pair.Key,input);
            input.ValueChanged+=v=>{if(!double.IsFinite(v)||v<0||v>255||v!=Math.Truncate(v))return;values[pair.Key]=(int)v;Publish();};
        }
        var reset=new Button{Text="Reset shape"};panel.AddChild(reset);panel.AddChild(status);
        reset.Pressed+=()=>{foreach(var pair in source.ShapeSelection.Values){values[pair.Key]=pair.Value;inputs[pair.Key].SetValueNoSignal(pair.Value);}Publish();};
        return(inputs,reset);
    }
    private void VerifyShapeControls(SkeletalMeshPreview preview,CreatureAppearanceDescriptorBundle source,
        (Dictionary<string,SpinBox> Controls,Button Reset) ui)
    {
        string original=preview.GeometryFingerprint();string identity=preview.GeometryFingerprint(false);
        var invalidMeshes=source.ShapeSelection!.Geometries.ToDictionary(p=>p.Key,p=>p.Value);string first=invalidMeshes.Keys.First();var invalid=invalidMeshes[first];
        invalidMeshes[first]=invalid with{Shaders=invalid.Shaders.ToArray()};
        try{preview.ApplyShapes(source with{ShapeSelection=source.ShapeSelection with{Geometries=invalidMeshes}});throw new InvalidOperationException("Invalid shape topology accepted.");}catch(AssetPreviewException){}
        if(original!=preview.GeometryFingerprint())throw new AssetPreviewException("Rejected shape changed uploaded geometry.");
        var positions=invalid.Skin.Positions.ToArray();positions[0]=new(float.NaN,0,0);invalidMeshes[first]=invalid with{Skin=invalid.Skin with{Positions=positions}};
        try{preview.ApplyShapes(source with{ShapeSelection=source.ShapeSelection with{Geometries=invalidMeshes}});throw new InvalidOperationException("Invalid shape position accepted.");}catch(System.IO.InvalidDataException){}
        if(original!=preview.GeometryFingerprint())throw new AssetPreviewException("Failed shape preparation changed uploaded geometry.");
        var input=ui.Controls["blend_muscle"];input.Value=255;
        if(original==preview.GeometryFingerprint())throw new AssetPreviewException("Shape control did not change uploaded positions.");
        // Every edit recomputes from the original source rather than accumulating previous deltas.
        string once=preview.GeometryFingerprint();input.Value=127;input.Value=255;
        if(once!=preview.GeometryFingerprint())throw new AssetPreviewException("Shape changes accumulated deformation.");
        foreach(var item in ui.Controls.Values)item.Value=255;
        preview.VerifyBoundary(DisplayServer.GetName()!="headless");
        ui.Reset.EmitSignal(Button.SignalName.Pressed);
        if(original!=preview.GeometryFingerprint()||identity!=preview.GeometryFingerprint(false))throw new AssetPreviewException("Shape reset changed retained pose/topology/material identity.");
        GD.Print($"PASS: live shape controls keys={ui.Controls.Count} all_max=true nonaccumulating=true reset=true failed_edit_preserves=true retained_pose_topology_material=true");
    }
}
