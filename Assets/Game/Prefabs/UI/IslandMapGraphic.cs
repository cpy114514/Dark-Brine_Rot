using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    // Actual island triangles projected to XZ. No extra render camera, shader or texture assets.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IslandMapGraphic : MaskableGraphic
    {
        readonly List<ProceduralIsland> islands = new List<ProceduralIsland>();
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        readonly List<int> submeshTriangles = new List<int>();
        public Vector2 WorldCenter { get; private set; }
        public float WorldSpan { get; private set; } = 100f;
        float waterHeight;
        public int IslandCount => islands.Count;
        public void Refresh(Vector3 player)
        {
            islands.Clear();
            Bounds bounds = new Bounds(player, Vector3.zero);
            bool found = false;
            foreach (var island in FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None))
            {
                if (!island.isActiveAndEnabled) continue;
                var renderer = island.GetComponent<MeshRenderer>();
                var mesh = island.GetComponent<MeshFilter>();
                if (renderer == null || mesh == null || mesh.sharedMesh == null) continue;
                islands.Add(island);
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            // Include an offshore player without changing the map's north-up orientation.
            bounds.Encapsulate(player);
            WorldCenter = new Vector2(bounds.center.x, bounds.center.z);
            WorldSpan = Mathf.Max(100f, Mathf.Max(bounds.size.x, bounds.size.z) * 1.15f);
            var ocean = FindFirstObjectByType<OceanWorld>();
            waterHeight = ocean != null ? ocean.oceanHeight : 0f;
            SetVerticesDirty();
        }
        public Vector2 WorldToMap(Vector3 world)
        {
            return new Vector2((world.x - WorldCenter.x) / WorldSpan * rectTransform.rect.width,
                (world.z - WorldCenter.y) / WorldSpan * rectTransform.rect.height);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float scaleX=rect.width/WorldSpan,scaleY=rect.height/WorldSpan;
            foreach (var island in islands)
            {
                if (island == null) continue;
                var mesh = island.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null) continue;
                mesh.GetVertices(vertices);
                triangles.Clear();
                if(mesh.subMeshCount==1)mesh.GetTriangles(triangles,0);
                else for(int submesh=0;submesh<mesh.subMeshCount;submesh++)
                {mesh.GetTriangles(submeshTriangles,submesh);triangles.AddRange(submeshTriangles);}
                int start = vh.currentVertCount;
                // Unity UI uses 16-bit indices; gracefully omit excessive geometry.
                if (start + vertices.Count >= 65000) continue;
                float peak = Mathf.Max(waterHeight + 1f, island.GetComponent<MeshRenderer>().bounds.max.y);
                Matrix4x4 matrix=island.transform.localToWorldMatrix;
                for (int i = 0; i < vertices.Count; i++)
                {
                    var world = matrix.MultiplyPoint3x4(vertices[i]);
                    float elevation = Mathf.Clamp01((world.y - waterHeight) / (peak - waterHeight));
                    Color terrain = GameUITheme.Gray(Mathf.Lerp(.68f,.38f,elevation));
                    if (world.y < waterHeight) terrain = GameUITheme.Gray(.12f);
                    else if (elevation < .16f) terrain = GameUITheme.Gray(.83f);
                    terrain.a = 1f;
                    vh.AddVert(new Vector2((world.x-WorldCenter.x)*scaleX,(world.z-WorldCenter.y)*scaleY), terrain, Vector2.zero);
                }
                for (int i = 0; i < triangles.Count; i += 3)
                    vh.AddTriangle(start + triangles[i], start + triangles[i + 1], start + triangles[i + 2]);
            }
        }
    }
}
