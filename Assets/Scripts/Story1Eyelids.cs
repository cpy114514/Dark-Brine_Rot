using UnityEngine;
using UnityEngine.UI;

/// <summary>Curved upper and lower lids reveal the scene through an eye-shaped aperture.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class Story1Eyelids : MaskableGraphic
{
    float openness;
    public float Openness
    {
        get => openness;
        set { openness = Mathf.Clamp01(value); SetVerticesDirty(); }
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        const int segments = 64;
        float Aperture(float x)
        {
            float normalized = ((x - r.center.x) / (r.width * .5f)) / (.12f + openness * 1.6f);
            return Mathf.Min(r.height * .5f, r.height * .75f * openness *
                Mathf.Sqrt(Mathf.Max(0f, 1f - normalized * normalized)));
        }
        void Quad(float x0, float x1, float y0, float y1, float edge)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x0, edge), color, Vector2.zero);
            mesh.AddVert(new Vector3(x1, edge), color, Vector2.zero);
            mesh.AddVert(new Vector3(x1, y1), color, Vector2.zero);
            mesh.AddVert(new Vector3(x0, y0), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
        for (int i = 0; i < segments; i++)
        {
            float x0 = Mathf.Lerp(r.xMin, r.xMax, i / (float)segments);
            float x1 = Mathf.Lerp(r.xMin, r.xMax, (i + 1f) / segments);
            Quad(x0, x1, r.center.y + Aperture(x0), r.center.y + Aperture(x1), r.yMax);
            Quad(x0, x1, r.center.y - Aperture(x0), r.center.y - Aperture(x1), r.yMin);
        }
    }
}
