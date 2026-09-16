using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The imported tree prefabs have an extra geometry_nodes renderer outside their
/// LODGroup. Swap only that renderer to the prefab's existing LOD1 mesh at distance.
/// The original mesh collider remains untouched.
/// </summary>
public sealed class TreeGeometryLodOptimizer : MonoBehaviour
{
    [Min(10f)] public float fullDetailDistance = 95f;
    [Min(0.05f)] public float updateInterval = 0.3f;

    sealed class Entry
    {
        public MeshFilter filter;
        public MeshRenderer renderer;
        public Mesh fullMesh;
        public Material[] fullMaterials;
        public Mesh distantMesh;
        public Material[] distantMaterials;
        public bool distant;
    }

    readonly List<Entry> entries = new List<Entry>();
    Camera mainCamera;
    float nextUpdate;

    void OnEnable()
    {
        CacheTrees();
        nextUpdate = 0f;
    }

    void LateUpdate()
    {
        if (Time.unscaledTime < nextUpdate)
            return;

        nextUpdate = Time.unscaledTime + updateInterval;
        if (mainCamera == null || !mainCamera.isActiveAndEnabled)
            mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        float fullDetailDistanceSquared = fullDetailDistance * fullDetailDistance;
        Vector3 cameraPosition = mainCamera.transform.position;
        foreach (Entry entry in entries)
        {
            if (entry.filter == null || entry.renderer == null)
                continue;

            bool useDistantMesh = (entry.renderer.bounds.center - cameraPosition).sqrMagnitude > fullDetailDistanceSquared;
            if (useDistantMesh == entry.distant)
                continue;

            entry.filter.sharedMesh = useDistantMesh ? entry.distantMesh : entry.fullMesh;
            entry.renderer.sharedMaterials = useDistantMesh ? entry.distantMaterials : entry.fullMaterials;
            entry.distant = useDistantMesh;
        }
    }

    void OnDisable()
    {
        foreach (Entry entry in entries)
        {
            if (entry.filter == null || entry.renderer == null || !entry.distant)
                continue;

            entry.filter.sharedMesh = entry.fullMesh;
            entry.renderer.sharedMaterials = entry.fullMaterials;
            entry.distant = false;
        }
        entries.Clear();
    }

    void CacheTrees()
    {
        entries.Clear();
        foreach (LODGroup group in FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
        {
            LOD[] levels = group.GetLODs();
            if (levels.Length < 2 || levels[1].renderers.Length == 0 || levels[1].renderers[0] == null)
                continue;

            Renderer distantRenderer = levels[1].renderers[0];
            MeshFilter distantFilter = distantRenderer.GetComponent<MeshFilter>();
            if (distantFilter == null || distantFilter.sharedMesh == null)
                continue;

            foreach (MeshFilter filter in group.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.name.Contains("geometry_nodes") || filter.sharedMesh == null)
                    continue;

                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null)
                    continue;

                entries.Add(new Entry
                {
                    filter = filter,
                    renderer = renderer,
                    fullMesh = filter.sharedMesh,
                    fullMaterials = renderer.sharedMaterials,
                    distantMesh = distantFilter.sharedMesh,
                    distantMaterials = distantRenderer.sharedMaterials
                });
            }
        }
    }
}
