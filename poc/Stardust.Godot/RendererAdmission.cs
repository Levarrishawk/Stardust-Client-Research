using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using Godot;

/// <summary>Observe the active device, rather than inferring it from project settings.</summary>
public static class RendererAdmission
{
    public static void Verify(bool requireD3d12)
    {
        bool headless = DisplayServer.GetName() == "headless";
        string driver = RenderingServer.GetCurrentRenderingDriverName();
        string method = RenderingServer.GetCurrentRenderingMethod();
        bool accepted = System.Environment.Is64BitProcess &&
            RuntimeInformation.ProcessArchitecture == Architecture.X64 && !headless &&
            driver == "d3d12" && method == "forward_plus" && RenderingServer.GetRenderingDevice() is not null;
        GD.Print("renderer_admission=" + JsonSerializer.Serialize(new
        {
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            PointerBytes = IntPtr.Size, Driver = driver, Method = method, Headless = headless,
            Adapter = headless ? "" : RenderingServer.GetVideoAdapterName(),
            AdapterApi = headless ? "" : RenderingServer.GetVideoAdapterApiVersion(),
            Engine = Engine.GetVersionInfo()["string"].AsString(), D3d12Accepted = accepted
        }));
        if (requireD3d12 && !accepted)
            throw new InvalidOperationException("This launch requires an actual Windows x64 Forward+ D3D12 device; fallback refused.");
    }
}
