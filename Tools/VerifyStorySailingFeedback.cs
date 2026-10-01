using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
public static class VerifyStorySailingFeedback
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying, "Requires Play mode.");
        var ship = UnityEngine.Object.FindFirstObjectByType<ShipSailingMotion>();
        var sequence = UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        bool sequenceEnabled = sequence != null && sequence.enabled;
        if (sequence != null) sequence.enabled = false;
        try
        {
            Vector3 start = ship.transform.position;
            float started = Time.time;
            await Task.Delay(5000);
            float duration = Time.time - started;
            float distance = Vector3.ProjectOnPlane(ship.transform.position - start, Vector3.up).magnitude;
            Check(duration > .1f && distance / duration > 7.5f, "Actual ship is not advancing at the tuned speed.");
            var wake = ship.GetComponent<ShipWakeEffects>();
            Check(wake != null, "Wake component was not created.");
            int count = (int)typeof(ShipWakeEffects).GetField("count", Private).GetValue(wake);
            var history = (Vector4[])typeof(ShipWakeEffects).GetField("trail", Private).GetValue(wake);
            Check(count >= 3 && Vector2.Distance(new Vector2(history[0].x,history[0].y),
                new Vector2(history[count-1].x,history[count-1].y)) > 10f, "World-space wake is not forming behind the boat.");
            var material = (Material)typeof(OceanWorld).GetField("generatedMaterial",Private).GetValue(ocean);
            Check(material.GetInt("_WakeCount") == count && material.GetVector("_WakeDirection").z > .5f,
                "Wake data did not reach the ocean shader.");
            foreach(var message in ShaderUtil.GetShaderMessages(material.shader))
                Check(message.severity.ToString() != "Error", message.message);
            Capture(Camera.main);
            ship.enabled = false;
            typeof(ShipWakeEffects).GetMethod("Simulate",Private).Invoke(wake,new object[]{.1f,Time.time+12f});
            Check(material.GetInt("_WakeCount") == 0 && material.GetVector("_WakeDirection").z == 0f,
                "Wake did not stop and expire when sailing stopped for the cinematic.");
            ship.enabled = true;
            return new { actualSpeed = distance / duration, distance, samples = count,
                oceanShaderValid = true, cinematicStopsWake = true, image = "Tools/StorySailingFeedback.png" };
        }
        finally { if (sequence != null) sequence.enabled = sequenceEnabled; ship.enabled = true; }
    }
    static void Capture(Camera camera)
    {
        var target = new RenderTexture(1280,720,24);
        var pixels = new Texture2D(1280,720,TextureFormat.RGB24,false);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
            File.WriteAllBytes(Path.GetFullPath("Tools/StorySailingFeedback.png"),pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
