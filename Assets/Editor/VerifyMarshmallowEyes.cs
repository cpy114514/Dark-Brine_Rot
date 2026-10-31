using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyMarshmallowEyes
{
    const string Key = "MarshmallowEyes.Review";
    const string Folder = "Temp/MarshmallowEyesReview";
    static VerifyMarshmallowEyes() => EditorApplication.playModeStateChanged += OnMode;
    [MenuItem("Tools/Second Island Review/Test Marshmallow Red Eyes")]
    public static void Start()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop play before testing eyes.");
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/status.txt", "starting");
        SessionState.SetBool(Key, true); VerifySecondIslandAwakening.Arm(); EditorApplication.isPlaying = true;
    }
    static void OnMode(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, false); _ = Run();
    }
    static async Task WaitFor(Func<bool> condition, string name)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!condition())
        {
            if (!Application.isPlaying) throw new Exception("Play mode ended.");
            if (Time.realtimeSinceStartup > deadline) throw new Exception("Timed out: " + name);
            await Task.Delay(50);
        }
    }
    static async Task Run()
    {
        GameObject near = null, far = null;
        try
        {
            File.WriteAllText(Folder + "/status.txt", "opening");
            await WaitForOpening();
            var group = GameObject.Find("Marshmallow Friends - Second Island");
            var actors = group ? group.GetComponentsInChildren<MarshmallowEnemyVisual>() : Array.Empty<MarshmallowEnemyVisual>();
            if (actors.Length != 70) throw new Exception("Expected 70 marshmallow enemies, found " + actors.Length);
            foreach (var actor in actors) if (actor.redEyeRadius <= 0) throw new Exception("An enemy has no detection radius: " + actor.name);
            group.SetActive(false);
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var results = new List<string>();
            foreach (string name in new[] { "Strawberry", "Mint", "Sky", "Lemon", "Lavender" })
            {
                File.WriteAllText(Folder + "/status.txt", name);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Marshmallow/" + name + " Marshmallow.prefab");
                if (!prefab) throw new Exception("Missing prefab: " + name);
                // Disable the test controllers so terrain does not move the probes.
                Vector3 basePosition = player.transform.position;
                near = UnityEngine.Object.Instantiate(prefab, basePosition + Vector3.right * 24f, Quaternion.identity);
                far = UnityEngine.Object.Instantiate(prefab, basePosition + Vector3.right * 27f, Quaternion.identity);
                near.GetComponent<ChickenBroAI>().enabled = false; far.GetComponent<ChickenBroAI>().enabled = false;
                near.GetComponent<CharacterController>().enabled = false; far.GetComponent<CharacterController>().enabled = false;
                var nearVisual = near.GetComponent<MarshmallowEnemyVisual>(); var farVisual = far.GetComponent<MarshmallowEnemyVisual>();
                if (!Mathf.Approximately(nearVisual.redEyeRadius, 25f)) throw new Exception(name + " has incorrect radius: " + nearVisual.redEyeRadius);
                if (!Mathf.Approximately(nearVisual.chaseStopRadius, 40f)) throw new Exception(name + " has incorrect chase radius: " + nearVisual.chaseStopRadius);
                await WaitFor(() => nearVisual.EyeAlertLevel > .99f, name + " red eyes inside radius");
                if (farVisual.EyeAlertLevel > .01f) throw new Exception(name + " triggered outside its radius");
                if (!nearVisual.IsAlert || !Mathf.Approximately(near.GetComponent<ChickenBroAI>().MovementMultiplier, 1.5f))
                    throw new Exception(name + " did not enter the 50% speed increase.");
                if (name == "Strawberry")
                {
                    // A death screen pauses game time; alert must still clear.
                    var playerHealth = player.GetComponent<PlayerHealth>();
                    var deathScreen = player.GetComponent<PlayerDeathRespawn>();
                    float savedHealth = playerHealth.currentHealth, savedTime = Time.timeScale;
                    bool deathScreenEnabled = deathScreen && deathScreen.enabled;
                    try
                    {
                        if (deathScreenEnabled) deathScreen.enabled = false;
                        playerHealth.currentHealth = 0; Time.timeScale = 0;
                        await WaitFor(() => !nearVisual.IsAlert, "player death while game time is paused");
                    }
                    finally
                    {
                        playerHealth.currentHealth = savedHealth; Time.timeScale = savedTime;
                        if (deathScreenEnabled && deathScreen) deathScreen.enabled = true;
                    }
                    await WaitFor(() => nearVisual.IsAlert, "reacquire player after respawn");
                }
                foreach (string eyeName in new[] { "Left eye", "Right eye" })
                {
                    var eye = nearVisual.visual.Find(eyeName).GetComponent<Renderer>(); var block = new MaterialPropertyBlock();
                    eye.GetPropertyBlock(block, 0);
                    var color = block.GetColor("_BaseColor"); var emission = block.GetColor("_EmissionColor");
                    if (color.r < .9f || color.g > .1f || emission.r < 1f || !eye.sharedMaterial.IsKeywordEnabled("_EMISSION"))
                        throw new Exception(name + " " + eyeName + " did not render red emission.");
                }
                near.transform.position = basePosition + Vector3.right * 39f;
                await Task.Delay(400);
                if (!nearVisual.IsAlert || nearVisual.EyeAlertLevel < .99f || !Mathf.Approximately(near.GetComponent<ChickenBroAI>().MovementMultiplier, 1.5f))
                    throw new Exception(name + " did not maintain the chase outside 25 units.");
                near.transform.position = basePosition + Vector3.right * 41f;
                await WaitFor(() => !nearVisual.IsAlert && nearVisual.EyeAlertLevel < .01f, name + " disengages beyond 40 units");
                if (!Mathf.Approximately(near.GetComponent<ChickenBroAI>().MovementMultiplier, 1f)) throw new Exception(name + " retained boosted speed after disengaging.");
                var hp = near.GetComponent<MarshmallowEnemyHealth>();
                var camera = Camera.main;
                if (!camera) throw new Exception("No main camera for health bar test.");
                near.transform.position = camera.transform.position + camera.transform.forward * 2f - Vector3.up * hp.healthBarHeight;
                await WaitFor(() => MarshmallowHealthBars.TryGetBar(hp, out bool visible, out _) && visible, name + " visible health bar");
                hp.ApplyDamage(7f, near.transform.position);
                await WaitFor(() => MarshmallowHealthBars.TryGetBar(hp, out _, out float fill) && Mathf.Abs(fill - .8f) < .01f, name + " health bar decreases");
                hp.Kill();
                await WaitFor(() => MarshmallowHealthBars.TryGetBar(hp, out bool visible, out _) && !visible, name + " defeated health bar hides");
                await WaitFor(() => !nearVisual.IsAlert, name + " stops chasing on death");
                results.Add(name + ": activates at 25, chases with 1.5x speed through 39, disengages past 40 and on death; health bar passes");
                UnityEngine.Object.DestroyImmediate(near); UnityEngine.Object.DestroyImmediate(far); near = far = null;
            }
            File.WriteAllLines(Folder + "/result.txt", results);
            File.WriteAllText(Folder + "/status.txt", "completed");
        }
        catch (Exception e) { File.WriteAllText(Folder + "/status.txt", "failed: " + e); }
        finally { if (near) UnityEngine.Object.Destroy(near); if (far) UnityEngine.Object.Destroy(far); }
    }
    static async Task WaitForOpening()
    {
        float deadline = Time.realtimeSinceStartup + 45f;
        while (VerifySecondIslandAwakening.Status != "completed")
        {
            if (!Application.isPlaying || Time.realtimeSinceStartup > deadline || VerifySecondIslandAwakening.Status.StartsWith("failed"))
                throw new Exception("Opening failed: " + VerifySecondIslandAwakening.Status);
            await Task.Delay(50);
        }
    }
}
