using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>D3DDECLTYPE_D3DCOLOR COLOR0: ARGB word to normalized RGBA, without gamma conversion.</summary>
public static class PackedVertexColor
{
    public static Vector4 ToRgba(uint argb)=>new((argb>>16&255)/255f,(argb>>8&255)/255f,(argb&255)/255f,(argb>>24)/255f);
}
