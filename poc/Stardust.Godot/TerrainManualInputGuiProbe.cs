using Godot;

/// <summary>Explicit diagnostic focused GUI consumer for the bounded offline scene.</summary>
public partial class TerrainManualInputGuiProbe : Control
{
    public int Events {get;private set;}
    public override void _GuiInput(InputEvent input)
    {
        if(input is InputEventKey){Events++;AcceptEvent();}
    }
}
