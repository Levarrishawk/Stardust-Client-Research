using Godot;

/// <summary>Inspection controls for offline geometry; no gameplay/network input.</summary>
public partial class PreviewOrbitCamera : Camera3D
{
    private Vector3 target;
    private float radius, distance, yaw=0.654f, pitch=0.331f;
    private bool dragging;
    private float resetYaw=0.654f;
    public void Configure(Vector3 center,float boundingRadius,bool front=false)
    {target=center;radius=Mathf.Max(boundingRadius,1);resetYaw=front?Mathf.Pi+0.654f:0.654f;Reset();}
    private void Reset(){distance=radius*2.4f;yaw=resetYaw;pitch=0.331f;UpdatePose();}
    private void UpdatePose()
    {
        Position=target+new Vector3(Mathf.Sin(yaw)*Mathf.Cos(pitch),Mathf.Sin(pitch),Mathf.Cos(yaw)*Mathf.Cos(pitch))*distance;
        LookAt(target);Near=Mathf.Max(radius*0.001f,0.01f);Far=radius*100;
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if(input is InputEventMouseButton button)
        {
            if(button.ButtonIndex==MouseButton.Left)dragging=button.Pressed;
            if(button.Pressed && button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {distance=Mathf.Clamp(distance*(button.ButtonIndex==MouseButton.WheelUp?0.9f:1.1f),radius*0.3f,radius*20);UpdatePose();GetViewport().SetInputAsHandled();}
        }
        else if(input is InputEventMouseMotion motion && dragging)
        {yaw-=motion.Relative.X*0.007f;pitch=Mathf.Clamp(pitch+motion.Relative.Y*0.007f,-1.45f,1.45f);UpdatePose();GetViewport().SetInputAsHandled();}
        else if(input is InputEventKey key && key.Pressed && !key.Echo && key.Keycode==Key.R)
        {Reset();GetViewport().SetInputAsHandled();}
    }
}
