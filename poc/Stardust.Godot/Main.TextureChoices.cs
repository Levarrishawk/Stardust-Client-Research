using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;

public partial class Main
{
    private (Dictionary<ShaderCustomizationKey,SpinBox> Controls,Button Reset) InstallTextureChoiceControls(VBoxContainer panel,
        CreatureAppearanceDescriptorBundle source,SkeletalMeshPreview preview,bool verifyPublication,Func<object?>? gameplayState=null)
    {
        var values=source.MaterialBundle!.TextureSelection!.Values.ToDictionary(p=>p.Key,p=>p.Value);
        var inputs=new Dictionary<ShaderCustomizationKey,SpinBox>();
        panel.AddChild(new Label{Text="Age detail"});var status=new Label{Text="Ready"};
        void Publish()
        {
            string geometry=preview.GeometryFingerprint();var transform=preview.Root.Transform;var state=gameplayState?.Invoke();
            int textures=preview.TextureCount;long bytes=preview.TextureBytes;
            var material=SkeletalTextureChoices.Apply(source.MaterialBundle,values);
            preview.ApplyTextureChoices(source with{MaterialBundle=material});
            if(geometry!=preview.GeometryFingerprint()||transform!=preview.Root.Transform||!Equals(state,gameplayState?.Invoke())||textures!=preview.TextureCount||bytes!=preview.TextureBytes)
                throw new AssetPreviewException("Detail edit changed retained geometry/actor/resources.");
            if(verifyPublication)preview.VerifyBoundary(DisplayServer.GetName()!="headless");
            status.Text="Ready";
        }
        foreach(var pair in values)
        {
            if(pair.Key!=new ShaderCustomizationKey("index_age",true))throw new AssetPreviewException("Unsupported detail control.");
            var input=new SpinBox{MinValue=0,MaxValue=3,Step=1,Value=pair.Value,CustomMinimumSize=new(100,0)};
            panel.AddChild(input);inputs.Add(pair.Key,input);
            input.ValueChanged+=v=>{if(!double.IsFinite(v)||v<0||v>3||v!=Math.Truncate(v))return;values[pair.Key]=(int)v;Publish();};
        }
        var reset=new Button{Text="Reset detail"};panel.AddChild(reset);panel.AddChild(status);
        reset.Pressed+=()=>{foreach(var pair in source.MaterialBundle.TextureSelection.Values){values[pair.Key]=pair.Value;inputs[pair.Key].SetValueNoSignal(pair.Value);}Publish();};
        return(inputs,reset);
    }
    private async Task VerifyTextureChoiceControls(SkeletalMeshPreview preview,CreatureAppearanceDescriptorBundle source,
        (Dictionary<ShaderCustomizationKey,SpinBox> Controls,Button Reset) ui)
    {
        string geometry=preview.GeometryFingerprint(false);int textures=preview.TextureCount;long bytes=preview.TextureBytes;
        foreach(var input in ui.Controls.Values){for(int i=0;i<4;i++)input.Value=i;input.Value=0;input.Value=3;}
        var age=new ShaderCustomizationKey("index_age",true);
        if(customizationRevisions is not null)
        {
            var colors=source.CustomizationValues!.ToDictionary(p=>p.Key,p=>p.Value);
            var skin=new ShaderCustomizationKey("index_color_skin",false);colors[skin]=(colors[skin]+1)%256;
            customizationRevisions.Submit(colors);await customizationRevisions.WorkerCompletion.WaitAsync(TimeSpan.FromSeconds(15));customizationPoll!();
            if(preview.SelectedTextureValue(age)!=3)throw new AssetPreviewException("Color edit reset selected age detail.");
            customizationRevisions.Submit(source.CustomizationValues!);await customizationRevisions.WorkerCompletion.WaitAsync(TimeSpan.FromSeconds(15));customizationPoll!();
        }
        var invalid=source.MaterialBundle!.TextureSelection! with{References=new Dictionary<string,StaticShaderReferenceDescription>()};
        try{preview.ApplyTextureChoices(source with{MaterialBundle=source.MaterialBundle with{TextureSelection=invalid}});throw new InvalidOperationException("Invalid detail bindings accepted.");}catch(AssetPreviewException){}
        preview.VerifyBoundary(DisplayServer.GetName()!="headless");
        ui.Reset.EmitSignal(Button.SignalName.Pressed);
        if(geometry!=preview.GeometryFingerprint(false)||textures!=preview.TextureCount||bytes!=preview.TextureBytes)throw new AssetPreviewException("Detail reset changed retained resources.");
        GD.Print("PASS: live age detail choices=4 reset=true color_edit_preserves=true invalid_bindings_preserve=true retained_pose_material=true no_additional_io=true");
    }
}
