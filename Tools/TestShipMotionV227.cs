using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public static class TestShipMotionV227
{
    delegate void Tilt(ref float angle, ref float velocity, float target, float smoothTime, float damping, float limit, float dt);
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    public static object Run()
    {
        if (Application.isPlaying) throw new Exception("Run the isolated contracts in Edit Mode.");
        var results = new List<object>();
        int passed = 0;
        Action<bool, string> check = (condition, name) => {
            if (!condition) throw new Exception(name);
            results.Add(new { name, passed = true }); passed++;
        };
        var step = (Tilt)Delegate.CreateDelegate(typeof(Tilt), typeof(ShipSailingMotion).GetMethod("StepTilt", BindingFlags.NonPublic | BindingFlags.Static));
        float reference = 0, referenceVelocity = 0;
        for (int i = 0; i < 120; i++) step(ref reference, ref referenceVelocity, 3, .9375f, .78f, 5, 1f / 120);
        foreach (int hz in new[] {10, 30, 60, 120})
        {
            float angle = 0, velocity = 0;
            for (int i = 0; i < hz; i++) step(ref angle, ref velocity, 3, .9375f, .78f, 5, 1f / hz);
            check(Mathf.Abs(angle - reference) < .0001f && Mathf.Abs(velocity - referenceVelocity) < .0002f, "Spring constant-load equivalence at " + hz + " Hz");
            for (int i = 0; i < hz * 10; i++) step(ref angle, ref velocity, i < hz ? 100 : 0, .9375f, .78f, 5, 1f / hz);
            check(!float.IsNaN(angle) && Mathf.Abs(angle) < .001f && Mathf.Abs(velocity) < .001f, "Overload bounded and settles at " + hz + " Hz");
        }
        float still = 2, stillVelocity = 1;
        step(ref still, ref stillVelocity, -3, .9375f, .78f, 5, 0);
        check(Mathf.Abs(still - 2) < 1e-6f && Mathf.Abs(stillVelocity - 1) < 1e-6f, "Zero time step preserves tilt and velocity");

        // Check actual imported upside-down root as well as an upright asset.
        foreach (var root in new[] { Quaternion.identity, Quaternion.Euler(180, 0, 0), Quaternion.Euler(180, 73, 0) })
        {
            Vector3 heading = Vector3.ProjectOnPlane(root * Vector3.right, Vector3.up).normalized;
            Vector3 beam = Vector3.ProjectOnPlane(root * Vector3.forward, Vector3.up).normalized;
            Vector3 axis = Vector3.Cross(beam, Vector3.up);
            check((Quaternion.AngleAxis(3, axis) * beam).y > 0, "Positive wave lift raises sampled beam: " + root.eulerAngles);
            check((Quaternion.AngleAxis(3, Vector3.Cross(heading, Vector3.up)) * heading).y > 0, "Positive bow lift raises bow: " + root.eulerAngles);
            Vector3 mast = Vector3.up;
            check(Vector3.Dot(Quaternion.AngleAxis(-3, axis) * mast, beam) > 0, "Wind heels mast downwind: " + root.eulerAngles);
        }

        var source = UnityEngine.Object.FindFirstObjectByType<ShipSailingMotion>();
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        check(source != null && ocean != null, "Native story ship and ocean present");
        var go = new GameObject("Temporary isolated sailing test") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var ship = go.AddComponent<ShipSailingMotion>();
            go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            Action<string, object> set = (name, value) => typeof(ShipSailingMotion).GetField(name, Private).SetValue(ship, value);
            set("ocean", ocean); set("baseRotation", source.transform.rotation);
            set("waterlineOffset", source.transform.position.y - ocean.oceanHeight); set("currentSpeed", 8f);
            var simulate = (Action<float, float, bool, bool>)Delegate.CreateDelegate(typeof(Action<float, float, bool, bool>), ship,
                typeof(ShipSailingMotion).GetMethod("Simulate", Private));
            Vector3 from = go.transform.position;
            Vector3 forward = Vector3.ProjectOnPlane(source.transform.right, Vector3.up).normalized;
            for (int i = 0; i < 600; i++) simulate(1f / 60, 0, false, false);
            Vector3 travelled = Vector3.ProjectOnPlane(go.transform.position - from, Vector3.up);
            check(Mathf.Abs(Vector3.Dot(travelled, forward) - 80) < .1f, "Automatic sail speed retains 80m in ten seconds");
            check(Vector3.ProjectOnPlane(travelled, forward).magnitude < .05f, "Wave and wind introduce no lateral position drift");
            var pausedPosition = go.transform.position; var pausedRotation = go.transform.rotation;
            simulate(0, 1, true, false);
            check(go.transform.position == pausedPosition && go.transform.rotation == pausedRotation, "Paused simulation leaves complete hull pose unchanged");
            float angle = (float)typeof(ShipSailingMotion).GetField("filteredRoll", Private).GetValue(ship);
            check(Mathf.Abs(angle) <= ship.maxRollDegrees, "Actual native-wave simulation observes heel limit");
            long before = GC.GetAllocatedBytesForCurrentThread();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // Warm the full native path before counting allocations.
            for (int i = 0; i < 100; i++) simulate(1f / 60, 0, false, false);
            before = GC.GetAllocatedBytesForCurrentThread(); watch.Restart();
            for (int i = 0; i < 5000; i++) simulate(1f / 60, 0, false, false);
            watch.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            double msPerStep = watch.Elapsed.TotalMilliseconds / 5000;
            check(allocated == 0, "5000 native five-probe sailing steps allocate zero managed bytes");
            results.Add(new { nativeSimulation = true, iterations = 5000, allocated, msPerStep });
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        var result = new { passed, results };
        File.WriteAllText(".codex/ship-motion-20261020/contracts.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
