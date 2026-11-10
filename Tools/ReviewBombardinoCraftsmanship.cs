using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReviewBombardinoCraftsmanship
{
    const string Evidence = ".codex/model-refinement/bombardino";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<object> Run()
    {
        if (!Application.isPlaying || AdditiveSceneBootstrap.IsLoading) throw new InvalidOperationException("Load First Island in isolated Play");
        var existing = UnityEngine.Object.FindFirstObjectByType<BombardinoBoss>();
        var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var arrival = UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
        if (arrival) { arrival.enabled = false; arrival.RestoreProgress(true, true); }
        var tempScene = SceneManager.CreateScene("Isolated production Bombardino material verification");
        var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab"), existing.transform.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(actor, tempScene);
        actor.AddComponent<GameSaveExcluded>();
        var boss = actor.GetComponent<BombardinoBoss>();
        bool previous = existing.enabled;
        existing.enabled = false;
        var renderer = actor.GetComponentInChildren<MeshRenderer>();
        var materials = renderer.sharedMaterials;
        var before = materials.Select(mat =>
        {
            var copy = new Material(mat);
            if (copy.GetTexture("_MetallicGlossMap")?.name.StartsWith("Bombardino") == true)
            {
                copy.SetTexture("_MetallicGlossMap", null);
                copy.SetFloat("_Smoothness", .3f);
                copy.DisableKeyword("_METALLICSPECGLOSSMAP");
            }
            return copy;
        }).ToArray();
        var probe = new GameObject("Bombardino live comparison camera", typeof(Camera));
        var camera = probe.GetComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.fieldOfView = 43;
        var target = new RenderTexture(1280, 800, 24);
        var shot = new Texture2D(1280, 800, TextureFormat.RGB24, false);
        var savedTarget = RenderTexture.active;
        var style = Mavis.VisualStyles.VisualStyleManager.Instance;
        var records = new List<object>();
        style.Select("realistic", false);
        try
        {
            var ground = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<Collider>();
            var home = actor.transform.position;
            if (!ground.Raycast(new Ray(home + Vector3.up * 300, Vector3.down), out var hit, 1000))
                throw new InvalidOperationException("Bombardino ground missing");
            player.RestoreSavedPose(hit.point + Vector3.up * (.08f - player.LowestFootWorldOffset), Quaternion.identity);
            player.ExternalControlLock = player.ExternalMovementLock = false;
            player.GetComponent<PlayerHealth>().GrantProtection(100);
            boss.target = player.transform;
            await Task.Delay(350);
            var health = boss.GetComponent<Health>();
            foreach (BombardinoBoss.AttackKind attack in Enum.GetValues(typeof(BombardinoBoss.AttackKind)))
            {
                if (attack == BombardinoBoss.AttackKind.MissileSalvo)
                {
                    health.currentHealth = health.maxHealth * .49f;
                    if (boss.FightPhase != 2) throw new InvalidOperationException("Bombardino second phase missing");
                }
                typeof(BombardinoBoss).GetProperty("FightActive").SetValue(boss, true);
                typeof(BombardinoBoss).GetField("nextAttackAt", Private).SetValue(boss, 0f);
                typeof(BombardinoBoss).GetMethod("StartAttack", Private).Invoke(boss, new object[] { attack });
                await Until(() => boss.CurrentState == BombardinoBoss.State.Attack, 8, "Bombardino windup " + attack);
                var from = actor.transform.position;
                await Task.Delay(450);
                float traveled = Vector3.Distance(from, actor.transform.position);
                if (traveled < .1f) throw new InvalidOperationException("Bombardino attack did not fly");
                float time = Time.timeScale;
                Time.timeScale = 0;
                try
                {
                    var bounds = renderer.bounds;
                    camera.transform.position = bounds.center + (actor.transform.forward + actor.transform.right * .9f + Vector3.up * .20f).normalized * bounds.size.magnitude * 1.2f;
                    camera.transform.LookAt(bounds.center);
                    foreach (bool refined in new[] { false, true })
                    {
                        renderer.sharedMaterials = refined ? materials : before;
                        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                        RenderTexture.active = target;
                        shot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        shot.Apply();
                        File.WriteAllBytes(Evidence + "/runtime-" + attack + (refined ? "-after" : "-before") + ".png", shot.EncodeToPNG());
                    }
                }
                finally { renderer.sharedMaterials = materials; Time.timeScale = time; }
                await Until(() => boss.CurrentState == BombardinoBoss.State.Exposed, 6, "Bombardino counter window " + attack);
                records.Add(new { attack = attack.ToString(), traveled, counterWindow = true, boss.BombsReleased, boss.MissilesLaunched, boss.FlameZonesCreated });
            }
            if (boss.BombsReleased == 0 || boss.MissilesLaunched == 0 || boss.FlameZonesCreated == 0)
                throw new InvalidOperationException("Bombardino payloads missing");
            health.Kill();
            await Until(() => boss.CurrentState == BombardinoBoss.State.Dead, 2, "Bombardino defeat");
            var result = new { fixture = "Production prefab in a temporary runtime scene above the actual First Island ground",
                records, defeat = true, surfaceSlots = materials.Count(mat => mat.GetTexture("_MetallicGlossMap")?.name.StartsWith("Bombardino") == true),
                mesh = renderer.GetComponent<MeshFilter>().sharedMesh.name };
            File.WriteAllText(Evidence + "/runtime-regression.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            existing.enabled = previous;
            RenderTexture.active = savedTarget;
            target.Release();
            foreach (var obj in before.Cast<UnityEngine.Object>().Concat(new UnityEngine.Object[] { target, shot, probe }))
                UnityEngine.Object.DestroyImmediate(obj);
            var unload = SceneManager.UnloadSceneAsync(tempScene);
            while (unload != null && !unload.isDone && Application.isPlaying) await Task.Delay(10);
            style.RestorePreference();
        }
    }

    static async Task Until(Func<bool> condition, float seconds, string label)
    {
        var limit = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition())
        {
            if (!Application.isPlaying || DateTime.UtcNow > limit) throw new InvalidOperationException("Timed out " + label);
            await Task.Delay(20);
        }
    }
}
