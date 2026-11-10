using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReviewHuangCraftsmanship
{
    const string Evidence = ".codex/model-refinement/huang";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Use isolated Play in Second Island");
        var boss = UnityEngine.Object.FindFirstObjectByType<HuangMeiBoss>();
        var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var opening = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
        if (opening) UnityEngine.Object.Destroy(opening.gameObject);
        await Task.Delay(350);
        var hp = player.GetComponent<PlayerHealth>();
        hp.maxHealth = hp.currentHealth = 10000;
        var protector = player.GetComponent<SecondIslandStartProtection>();
        if (protector) typeof(SecondIslandStartProtection).GetProperty("WaitingForMovement").SetValue(protector, false);
        var skin = boss.GetComponent<HuangMeiRig>().skin;
        var refined = skin.sharedMaterials;
        var originals = refined.Select(material =>
        {
            var original = new Material(material);
            if (original.GetTexture("_MetallicGlossMap")?.name.StartsWith("Huang") == true)
            {
                original.SetTexture("_MetallicGlossMap", null);
                original.SetFloat("_Smoothness", .24f);
                original.DisableKeyword("_METALLICSPECGLOSSMAP");
            }
            return original;
        }).ToArray();
        int newMaps = refined.Count(material => material.GetTexture("_MetallicGlossMap")?.name.StartsWith("Huang") == true);
        if (newMaps != 7 || skin.bones.Length != 340) throw new InvalidOperationException("Huang production material or skeleton mismatch");
        var probe = new GameObject("Huang actual action comparison", typeof(Camera));
        var camera = probe.GetComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.fieldOfView = 48;
        var target = new RenderTexture(1280, 900, 24);
        var screenshot = new Texture2D(1280, 900, TextureFormat.RGB24, false);
        var oldTarget = RenderTexture.active;
        var style = Mavis.VisualStyles.VisualStyleManager.Instance;
        var records = new List<object>();
        style.Select("realistic", false);
        try
        {
            foreach (HuangMeiBoss.Attack attack in Enum.GetValues(typeof(HuangMeiBoss.Attack)))
            {
                var surface = player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
                var at = boss.transform.position + boss.transform.forward * (attack == HuangMeiBoss.Attack.StaffSweep ? 8 : 11);
                var bounds = surface.bounds;
                if (!surface.Raycast(new Ray(new Vector3(at.x, bounds.max.y + 10, at.z), Vector3.down), out var floor, bounds.size.y + 20))
                    throw new InvalidOperationException("Missing Huang review footing");
                var body = player.GetComponent<CharacterController>();
                float sole = (body.center.y - body.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
                player.RestoreSavedPose(floor.point + Vector3.up * (.1f - sole), Quaternion.LookRotation(-boss.transform.forward));
                player.ExternalControlLock = player.ExternalMovementLock = false;
                typeof(PlayerHealth).GetField("protectedUntil", Private).SetValue(hp, 0f);
                typeof(PlayerHealth).GetField("recoveryUntil", Private).SetValue(hp, 0f);
                typeof(HuangMeiBoss).GetMethod("Engage", Private).Invoke(boss, null);
                if (attack == HuangMeiBoss.Attack.GoldenMandala)
                {
                    boss.Health.ApplyDamage(boss.Health.currentHealth - boss.Health.maxHealth * .49f, boss.transform.position);
                    await Until(() => boss.SecondPhase && boss.CurrentState == HuangMeiBoss.State.Invocation, 2, "Huang half-health invocation");
                }
                else typeof(HuangMeiBoss).GetMethod("BeginAttack", Private).Invoke(boss, new object[] { attack });
                await Until(() => boss.CurrentState == HuangMeiBoss.State.Strike, 6, "Huang windup " + attack);
                await Task.Delay(attack == HuangMeiBoss.Attack.GoldenMandala ? 850 : 400);
                float time = Time.timeScale;
                var pose = skin.bones.Select(bone => bone.localToWorldMatrix).ToArray();
                Time.timeScale = 0;
                try
                {
                    var world = skin.bounds;
                    camera.transform.position = world.center + (boss.transform.forward + boss.transform.right * .65f + Vector3.up * .18f).normalized * world.size.magnitude * 1.15f;
                    camera.transform.LookAt(world.center);
                    foreach (bool after in new[] { false, true })
                    {
                        skin.sharedMaterials = after ? refined : originals;
                        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                        RenderTexture.active = target;
                        screenshot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        screenshot.Apply();
                        File.WriteAllBytes(Evidence + "/runtime-" + attack + (after ? "-after" : "-before") + ".png", screenshot.EncodeToPNG());
                    }
                    if (!pose.SequenceEqual(skin.bones.Select(bone => bone.localToWorldMatrix)))
                        throw new InvalidOperationException("Material comparison changed Huang bones");
                }
                finally { skin.sharedMaterials = refined; Time.timeScale = time; }
                await Until(() => boss.CurrentState == HuangMeiBoss.State.Recover, 7, "Huang recovery " + attack);
                records.Add(new { attack = attack.ToString(), recovery = true, boss.HitsLanded, playerHealth = hp.currentHealth });
            }
            boss.Health.Kill();
            await Task.Delay(200);
            if (boss.CurrentState != HuangMeiBoss.State.Dead) throw new InvalidOperationException("Huang defeat did not complete");
            var result = new { records, bones = skin.bones.Length,
                triangles = Enumerable.Range(0, skin.sharedMesh.subMeshCount).Sum(sub => (int)skin.sharedMesh.GetIndexCount(sub) / 3),
                surfaceSlots = newMaps, phaseTwo = boss.SecondPhase, defeat = true };
            File.WriteAllText(Evidence + "/runtime-regression.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            skin.sharedMaterials = refined;
            RenderTexture.active = oldTarget;
            target.Release();
            foreach (var obj in originals.Cast<UnityEngine.Object>().Concat(new UnityEngine.Object[] { target, screenshot, probe }))
                UnityEngine.Object.DestroyImmediate(obj);
            style.RestorePreference();
        }
    }

    static async Task Until(Func<bool> condition, float seconds, string label)
    {
        var limit = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition())
        {
            if (!Application.isPlaying || DateTime.UtcNow >= limit) throw new InvalidOperationException("Timed out " + label);
            await Task.Delay(20);
        }
    }
}
