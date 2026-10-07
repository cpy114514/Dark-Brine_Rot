using UnityEngine;
using UnityEngine.UI;

/// <summary>Compatibility for old launch canvases; impact reads through the scene itself.</summary>
[DisallowMultipleComponent]
public sealed class Story1LaunchOverlay : MaskableGraphic
{
    public void SetFrame(float contactSeconds, float flightSeconds, Vector2 hit, Vector2 destination)
    { raycastTarget = false; enabled = false; }
    protected override void OnPopulateMesh(VertexHelper mesh) => mesh.Clear();
}
