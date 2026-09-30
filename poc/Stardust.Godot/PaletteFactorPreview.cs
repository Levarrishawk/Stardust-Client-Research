using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

/// <summary>Canvas swatches for explicit customization inspection; does not alter scene materials.</summary>
public sealed class PaletteFactorPreview
{
    public Control Root {get;}=new(){Name="PaletteFactorInspector"};
    private readonly CustomizableShaderPaletteBundle bundle;
    private readonly Dictionary<ShaderCustomizationKey,int> values=new();
    private readonly List<(ColorRect Swatch,Label Detail,CheckButton Default,SpinBox Index)> rows=new();
    private bool refreshing;
    private IReadOnlyList<SelectedShaderPaletteFactor> selected=Array.Empty<SelectedShaderPaletteFactor>();
    public PaletteFactorPreview(CustomizableShaderPaletteBundle bundle,string member)
    {
        this.bundle=bundle;
        if(bundle.AuthoredFactors.Count>128)throw new InvalidOperationException("Palette inspector supports at most 128 factors.");
        Root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        Root.AddChild(new ColorRect{Color=new Color("#14202e"),MouseFilter=Control.MouseFilterEnum.Ignore,OffsetRight=1280,OffsetBottom=720});
        var content=new VBoxContainer{Position=new Vector2(28,20),Size=new Vector2(1224,666)};Root.AddChild(content);
        var title=new Label{Text="STARDUST — PALETTE FACTOR INSPECTOR"};title.AddThemeFontSizeOverride("font_size",24);content.AddChild(title);
        content.AddChild(new Label{Text=member});
        content.AddChild(new Label{Text=$"{bundle.Sources.Count} sources • {bundle.Palettes.Count} palettes • {bundle.MissingPalettePaths.Count} missing • {bundle.AuthoredFactors.Count} factors"});
        content.AddChild(new Label{Text="Authored selects serialized defaults. Uncheck to supply a value; repeated variable keys share that value."});
        var scroll=new ScrollContainer{SizeFlagsVertical=Control.SizeFlags.ExpandFill,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};content.AddChild(scroll);
        var list=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};list.AddThemeConstantOverride("separation",14);scroll.AddChild(list);
        for(int i=0;i<bundle.AuthoredFactors.Count;i++)
        {
            var factor=bundle.Shader.PaletteFactors[i];var key=new ShaderCustomizationKey(factor.VariableName,factor.RawPrivateFlag!=0);
            var row=new HBoxContainer{CustomMinimumSize=new Vector2(1140,100)};row.AddThemeConstantOverride("separation",18);list.AddChild(row);
            var swatch=new ColorRect{CustomMinimumSize=new Vector2(104,88),MouseFilter=Control.MouseFilterEnum.Ignore};row.AddChild(swatch);
            var detail=new Label{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};row.AddChild(detail);
            var inputs=new VBoxContainer{CustomMinimumSize=new Vector2(180,80)};row.AddChild(inputs);
            var useDefault=new CheckButton{Text="Authored",ButtonPressed=true};inputs.AddChild(useDefault);
            var index=new SpinBox{MinValue=int.MinValue,MaxValue=int.MaxValue,Step=1,Value=factor.AuthoredDefault,Editable=false};inputs.AddChild(index);
            rows.Add((swatch,detail,useDefault,index));
            useDefault.Toggled+=enabled=>{if(refreshing)return;if(enabled)values.Remove(key);else values[key]=checked((int)index.Value);Refresh();};
            index.ValueChanged+=value=>{if(refreshing || useDefault.ButtonPressed)return;values[key]=checked((int)value);Refresh();};
        }
        if(rows.Count==0)list.AddChild(new Label{Text="This shader has no palette factors."});
        content.AddChild(new Label{Text="Color swatches only. Original lighting, shader factor semantics and live object state remain untranslated."});
        Refresh();
    }
    private PaletteDescription? Resolve(string path)=>bundle.MissingPalettePaths.Contains(path,StringComparer.Ordinal)?null:bundle.Palettes[path];
    private void Refresh()
    {
        selected=CustomizableShaderPaletteQuery.Select(bundle.Shader,Resolve,values);refreshing=true;
        try
        {
            for(int i=0;i<rows.Count;i++)
            {
                var factor=selected[i];var row=rows[i];uint color=factor.Color?.OpaqueArgb??0;
                row.Swatch.Color=ToColor(color);row.Default.ButtonPressed=factor.UsedAuthoredDefault;row.Index.Editable=!factor.UsedAuthoredDefault;row.Index.Value=factor.RequestedIndex;
                string scope=factor.Private?"/private/":"/shared_owner/";
                string result=factor.Color is null?"empty: no color":$"entry {factor.Color.SelectedIndex} • #{factor.Color.OpaqueArgb:X8} • flags {factor.Color.RawFlags:X2}";
                row.Detail.Text=$"{Tag(factor.FactorTag)} • {scope}{factor.VariableName}\n{factor.PalettePath}\nRequested {factor.RequestedIndex} → {result}"+(factor.MissingPalette?"\nMissing palette: cyan fallback":"");
            }
        }
        finally{refreshing=false;}
    }
    private static Color ToColor(uint color)=>new(((color>>16)&255)/255f,((color>>8)&255)/255f,(color&255)/255f,((color>>24)&255)/255f);
    private static string Tag(uint tag)=>new string(new[]{(char)(tag>>24),(char)((tag>>16)&255),(char)((tag>>8)&255),(char)(tag&255)});
    public void VerifyControls()
    {
        foreach(int extreme in new[]{int.MinValue,int.MaxValue})
        {
            for(int i=0;i<rows.Count;i++)
            {
                rows[i].Default.ButtonPressed=false;rows[i].Index.Value=extreme;
                var key=new ShaderCustomizationKey(bundle.Shader.PaletteFactors[i].VariableName,bundle.Shader.PaletteFactors[i].RawPrivateFlag!=0);
                foreach(var factor in selected.Where(f=>new ShaderCustomizationKey(f.VariableName,f.Private)==key))
                    if(factor.RequestedIndex!=extreme || factor.UsedAuthoredDefault || (factor.Color is not null && factor.Color.SelectedIndex!=0))throw new InvalidOperationException($"Palette inspector shared override mismatch: row {i}, requested {extreme}, observed {factor.RequestedIndex}, authored {factor.UsedAuthoredDefault}.");
            }
        }
        for(int i=0;i<rows.Count;i++)rows[i].Default.ButtonPressed=true;
        if(selected.Any(f=>!f.UsedAuthoredDefault) || values.Count!=0)throw new InvalidOperationException("Palette inspector authored reset mismatch.");
        for(int i=0;i<rows.Count;i++)if(selected[i]!=bundle.AuthoredFactors[i])throw new InvalidOperationException("Palette inspector result projection mismatch.");
        GD.Print($"PASS: palette inspector controls factors={rows.Count} shared override extremes and authored reset");
    }
    public int VerifyPixels(Image image,Rect2 viewport)
    {
        int verified=0;
        for(int i=0;i<rows.Count;i++)
        {
            if(selected[i].Color is null)continue;
            Vector2 point=rows[i].Swatch.GlobalPosition+rows[i].Swatch.Size/2;
            if(!viewport.HasPoint(point) || point.Y>620)continue;
            Color observed=image.GetPixel((int)point.X,(int)point.Y),expected=rows[i].Swatch.Color;
            if(Math.Abs(observed.R-expected.R)>2/255f || Math.Abs(observed.G-expected.G)>2/255f || Math.Abs(observed.B-expected.B)>2/255f || Math.Abs(observed.A-1)>2/255f)throw new InvalidOperationException("Palette swatch GPU mismatch.");
            verified++;
        }
        if(selected.Any(f=>f.Color is not null) && verified==0)throw new InvalidOperationException("No visible palette swatches verified.");
        return verified;
    }
    public static void VerifySharedControls(CustomizableShaderPaletteBundle original,Node host)
    {
        if(original.Shader.PaletteFactors.Count==0)return;
        var factor=original.Shader.PaletteFactors[0];
        var shader=original.Shader with {PaletteFactors=Array.AsReadOnly(new[]{factor,factor with {AuthoredDefault=int.MaxValue}})};
        var derived=original with {Shader=shader,AuthoredFactors=CustomizableShaderPaletteQuery.Select(shader,path=>original.MissingPalettePaths.Contains(path,StringComparer.Ordinal)?null:original.Palettes[path])};
        var preview=new PaletteFactorPreview(derived,"Synthetic repeated-variable control fixture");
        preview.Root.Visible=false;host.AddChild(preview.Root);
        try{preview.VerifyControls();}
        catch(InvalidOperationException failure){GD.Print("Synthetic palette control gate: "+failure.Message);throw;}
        finally{preview.Root.Free();}
        GD.Print("PASS: synthetic repeated variable controls retain per-factor defaults and share supplied state");
    }
}
