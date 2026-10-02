using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;

public partial class Main
{
    private SkeletalCustomizationRevisions? customizationRevisions;
    private Action? customizationPoll;
    private int customizationPublications;
    private CreatureAppearanceDescriptorBundle? customizationDisplayed;
    private void DisposeCustomization()
    {customizationPoll=null;customizationRevisions?.Dispose();customizationRevisions=null;customizationDisplayed=null;}

    private (Dictionary<ShaderCustomizationKey,SpinBox> Controls,Button Reset) InstallCustomizationControls(
        VBoxContainer panel,CreatureAppearanceDescriptorBundle source,SkeletalMeshPreview preview,bool verifyPublication,Func<object?>? gameplayState=null)
    {
        DisposeCustomization();var queue=new SkeletalCustomizationRevisions(source);customizationRevisions=queue;customizationDisplayed=source;customizationPublications=0;
        var values=source.CustomizationValues!.ToDictionary(p=>p.Key,p=>p.Value);var controls=new Dictionary<ShaderCustomizationKey,SpinBox>();
        panel.AddChild(new HSeparator());panel.AddChild(new Label{Text="Character colors and face"});
        var status=new Label{Text="Ready"};
        string Label(ShaderCustomizationKey key)=>key.VariableName switch
        {
            "index_color_skin"=>key.Private?"Face highlight":"Skin color",
            "index_color_2"=>"Eye color","index_color_3"=>"Secondary color","index_color_facial_hair"=>"Facial hair color",
            "index_style_eyebrow"=>"Eyebrows","index_style_beard"=>"Beard","index_style_freckles"=>"Freckles",
            _=>"Color selection"
        };
        foreach(var control in SkeletalAppearanceCustomization.DescribeControls(source))
        {
            if(control.MaximumValue==0)continue;
            var row=new HBoxContainer();panel.AddChild(row);row.AddChild(new Label{Text=Label(control.Key),CustomMinimumSize=new(160,0)});
            var input=new SpinBox{MinValue=0,MaxValue=control.MaximumValue,Step=1,AllowGreater=true,AllowLesser=true,Value=values[control.Key],CustomMinimumSize=new(110,0)};row.AddChild(input);controls.Add(control.Key,input);
            input.ValueChanged+=value=>
            {
                if(!double.IsFinite(value)||value<int.MinValue||value>int.MaxValue||value!=Math.Truncate(value)){status.Text="Choose a whole-number value.";return;}
                values[control.Key]=(int)value;queue.Submit(values);status.Text="Updating appearance…";
            };
        }
        var reset=new Button{Text="Reset colors and face"};panel.AddChild(reset);panel.AddChild(status);
        reset.Pressed+=()=>{foreach(var pair in source.CustomizationValues!){values[pair.Key]=pair.Value;if(controls.TryGetValue(pair.Key,out var input))input.SetValueNoSignal(pair.Value);}queue.Submit(values);status.Text="Updating appearance…";};
        customizationPoll=()=>
        {
            if(!queue.TryTake(out var result)||result!.Number!=queue.Latest)return;
            if(result.Failure is not null){status.Text="Could not update appearance.";GD.Print("Customization update failed: "+result.Failure.GetType().Name);return;}
            var beforeState=gameplayState?.Invoke();var beforeTransform=preview.Root.Transform;var beforeBounds=preview.Bounds;
            string? geometry=verifyPublication?preview.GeometryFingerprint():null;int textureCount=preview.TextureCount;long bytes=preview.TextureBytes;
            preview.ApplyCustomization(result.Appearance!);
            if(!Equals(beforeState,gameplayState?.Invoke())||beforeTransform!=preview.Root.Transform||beforeBounds!=preview.Bounds)throw new AssetPreviewException("Customization altered actor/placement/pose bounds.");
            if(verifyPublication&&(geometry!=preview.GeometryFingerprint()||textureCount!=preview.TextureCount||bytes!=preview.TextureBytes))throw new AssetPreviewException("Customization replaced geometry/material identity or accumulated GPU textures.");
            if(verifyPublication)preview.VerifyBoundary(DisplayServer.GetName()!="headless");
            customizationDisplayed=result.Appearance;customizationPublications++;status.Text="Ready";
        };
        return(controls,reset);
    }

    private async Task VerifyCustomizationControls(SkeletalMeshPreview preview,CreatureAppearanceDescriptorBundle source,
        (Dictionary<ShaderCustomizationKey,SpinBox> Controls,Button Reset) ui)
    {
        var queue=customizationRevisions!;var key=new ShaderCustomizationKey("index_color_skin",false);var skin=ui.Controls[key];
        string beforeRejection=preview.GeometryFingerprint();int retainedTextures=preview.TextureCount;long retainedBytes=preview.TextureBytes;
        void RejectRevision(CreatureAppearanceDescriptorBundle invalid)
        {
            try{preview.ApplyCustomization(invalid);throw new InvalidOperationException("Invalid GPU customization revision was accepted.");}catch(AssetPreviewException){}
            if(beforeRejection!=preview.GeometryFingerprint()||retainedTextures!=preview.TextureCount||retainedBytes!=preview.TextureBytes)throw new AssetPreviewException("Rejected GPU revision changed retained resources.");
            preview.VerifyBoundary(true);
        }
        var factorBundle=source.MaterialBundle!.AlbedoFactors!;var invalidFactors=factorBundle.Materials.ToDictionary(p=>p.Key,p=>p.Value);string factorPath=invalidFactors.Keys.First();invalidFactors[factorPath]=invalidFactors[factorPath] with{FirstTag=0};
        RejectRevision(source with{MaterialBundle=source.MaterialBundle with{AlbedoFactors=factorBundle with{Materials=invalidFactors}}});
        var invalidFaces=source.GeneratedFaces!.Outputs.ToDictionary(p=>p.Key,p=>p.Value);string facePath=invalidFaces.Keys.First();var face=invalidFaces[facePath];
        invalidFaces[facePath]=face with{Image=face.Image with{Mips=new[]{face.Image.Mips[0] with{Rgba=new byte[1]}}}};
        RejectRevision(source with{GeneratedFaces=source.GeneratedFaces with{Outputs=invalidFaces}});
        int initial=source.CustomizationValues![key],before=customizationPublications;string geometry=preview.GeometryFingerprint(false);
        skin.Value=(initial+1)%((int)skin.MaxValue+1);skin.Value=(initial+2)%((int)skin.MaxValue+1);skin.Value=(initial+3)%((int)skin.MaxValue+1);
        await queue.WorkerCompletion.WaitAsync(TimeSpan.FromSeconds(15));customizationPoll!();
        if(customizationPublications!=before+1||customizationDisplayed!.CustomizationValues![key]!=(int)skin.Value)throw new AssetPreviewException("Rapid customization edits published an old revision.");
        for(int i=0;i<8;i++)
        {
            skin.Value=(initial+4+i)%((int)skin.MaxValue+1);await queue.WorkerCompletion.WaitAsync(TimeSpan.FromSeconds(15));customizationPoll!();
        }
        var retained=customizationDisplayed;before=customizationPublications;queue.Submit(new Dictionary<ShaderCustomizationKey,int>());
        await queue.WorkerCompletion.WaitAsync(TimeSpan.FromSeconds(15));customizationPoll!();
        if(customizationPublications!=before||!ReferenceEquals(retained,customizationDisplayed))throw new AssetPreviewException("Failed customization replaced current appearance.");
        ui.Reset.EmitSignal(Button.SignalName.Pressed);await queue.WorkerCompletion.WaitAsync(TimeSpan.FromSeconds(15));customizationPoll!();
        if(!source.CustomizationValues.OrderBy(p=>p.Key.VariableName).ThenBy(p=>p.Key.Private).SequenceEqual(customizationDisplayed!.CustomizationValues!.OrderBy(p=>p.Key.VariableName).ThenBy(p=>p.Key.Private))||geometry!=preview.GeometryFingerprint(false))throw new AssetPreviewException("Customization reset did not preserve original values/geometry identity.");
        GD.Print($"PASS: live customization controls rapid_latest=true repeated_updates=8 invalid_gpu_revision_preserves=true failure_preserves=true reset=true retained_mesh_material_pose=true gpu_textures={preview.TextureCount} gpu_bytes={preview.TextureBytes}");
    }
}
