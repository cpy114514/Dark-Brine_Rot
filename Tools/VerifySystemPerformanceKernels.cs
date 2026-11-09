using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;

public static class VerifySystemPerformanceKernels
{
    static int Count(Bounds[] bounds, int[] counts, Vector3 position, Plane[] planes, bool grouped)
    {
        int accepted = 0;
        for (int i = 0; i < bounds.Length; i++)
        {
            int loops = grouped ? 1 : counts[i];
            for (int j = 0; j < loops; j++)
            {
                float distance = bounds[i].SqrDistance(position);
                if (distance > 240f * 240f || !GeometryUtility.TestPlanesAABB(planes, bounds[i])) continue;
                if (!grouped) distance = bounds[i].SqrDistance(position);
                // The same distance drives LOD classification in both cases.
                accepted += (grouped ? counts[i] : 1) * (1 + Mathf.FloorToInt(Mathf.Sqrt(distance)));
            }
        }
        return accepted;
    }

    public static object Run()
    {
        var scatter = UnityEngine.Object.FindFirstObjectByType<NaturalGrassScatter>();
        if (!scatter || !scatter.data) throw new Exception("Load the production first island meadow.");
        var data = scatter.data;
        var bounds = new List<Bounds>();
        var counts = new List<int>();
        int instances = 0;
        foreach (var cell in data.cells)
        {
            if (cell.positions == null || cell.normals == null || cell.positions.Length != cell.normals.Length) continue;
            int batches = 0;
            var variants = new int[data.meshes.Length];
            for (int i = 0; i < cell.positions.Length; i++)
            {
                int variant = cell.variants?.Length == cell.positions.Length ? cell.variants[i] : i % variants.Length;
                if (variant >= variants.Length) continue;
                variants[variant]++;
                instances++;
            }
            foreach (int count in variants) batches += (count + 255) / 256;
            if (batches == 0) continue;
            bounds.Add(cell.bounds);
            counts.Add(batches);
        }
        var cellBounds = bounds.ToArray();
        var cellCounts = counts.ToArray();
        var views = new List<(Vector3 position, Plane[] planes)>();
        foreach (var center in new[] { new Vector3(91, 53, 371), new Vector3(235, 44, 500), new Vector3(288, 25, 346) })
        {
            for (int angle = 0; angle < 360; angle += 15)
            {
                var orientation = Quaternion.Euler(18, angle, 0);
                var matrix = Matrix4x4.Perspective(55, 16f / 9, .1f, 1200) *
                    Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(center, orientation, Vector3.one).inverse;
                views.Add((center, GeometryUtility.CalculateFrustumPlanes(matrix)));
            }
        }
        foreach (var view in views)
            if (Count(cellBounds, cellCounts, view.position, view.planes, false) != Count(cellBounds, cellCounts, view.position, view.planes, true))
                throw new Exception("Grass distance/frustum changed.");
        double Time(bool grouped)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            int checksum = 0;
            for (int i = 0; i < 20; i++)
                foreach (var view in views) checksum += Count(cellBounds, cellCounts, view.position, view.planes, grouped);
            if (checksum == 0) throw new Exception("Grass fixture has no visible cells.");
            return timer.Elapsed.TotalMilliseconds / (20 * views.Count);
        }
        var before = new double[5];
        var after = new double[5];
        for (int i = 0; i < before.Length; i++)
        {
            if (i % 2 == 0) { before[i] = Time(false); after[i] = Time(true); }
            else { after[i] = Time(true); before[i] = Time(false); }
        }
        Array.Sort(before);
        Array.Sort(after);
        if (scatter.IsReady && scatter.InstanceCount != instances) throw new Exception("Production meadow count changed.");
        var result = new { passed = true, instances, cells = cellBounds.Length, batches = cellCounts.Sum(),
            views = views.Count, beforeMsPerView = before[2], afterMsPerView = after[2], asset = AssetDatabase.GetAssetPath(data),
            ocean = VerifyOcean() };
        File.WriteAllText(".codex/system-performance-20261107/grass-kernel.json",
            Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    static object VerifyOcean()
    {
        var world = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var camera = Camera.main;
        if (!world || !camera) throw new Exception("Production ocean/camera unavailable.");
        var filter = world.GetComponent<MeshFilter>();
        var renderer = world.GetComponent<MeshRenderer>();
        var originalMesh = filter.sharedMesh;
        var originalMaterial = renderer.sharedMaterial;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var select = (Action<Camera>)Delegate.CreateDelegate(typeof(Action<Camera>), world,
            typeof(OceanWorld).GetMethod("SelectCoverage", flags));
        var test = new GameObject("Temporary ocean coverage camera", typeof(Camera));
        var control = test.GetComponent<Camera>();
        control.enabled = false;
        control.CopyFrom(camera);
        control.enabled = false;
        try
        {
            control.transform.rotation = Quaternion.Euler(0, 0, 0);
            select(control);
            var forward = filter.sharedMesh;
            var forwardBounds = forward.bounds;
            control.transform.rotation = Quaternion.Euler(75, 0, 0);
            select(control);
            var surround = filter.sharedMesh;
            var surroundBounds = surround.bounds;
            if (forward == surround) throw new Exception("Ocean coverage failed to switch.");
            float sample = world.SampleSurfaceHeight(camera.transform.position, 4);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 120; i++)
            {
                control.transform.rotation = Quaternion.Euler(i % 2 == 0 ? 0 : 75, 0, 0);
                select(control);
                var expected = i % 2 == 0 ? forward : surround;
                var bounds = i % 2 == 0 ? forwardBounds : surroundBounds;
                if (filter.sharedMesh != expected || !expected.bounds.Equals(bounds) || renderer.sharedMaterial != originalMaterial)
                    throw new Exception("Ocean mesh, material or culling bounds changed.");
            }
            if (world.SampleSurfaceHeight(camera.transform.position, 4) != sample)
                throw new Exception("Water physics changed during camera coverage switches.");
            return new { switches = 120, meanMs = timer.Elapsed.TotalMilliseconds / 120, stable = true };
        }
        finally
        {
            select(camera);
            UnityEngine.Object.DestroyImmediate(test);
        }
    }
}
