using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class InspectSahurCombo
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static object Inspect()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var root = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 500, 0), Quaternion.identity);
        var cameraObject = new GameObject("Combo inspection camera");
        var lightObject = new GameObject("Combo inspection light");
        var sheet = new Texture2D(1600, 960, TextureFormat.RGB24, false);
        var frame = new Texture2D(320, 320, TextureFormat.RGB24, false);
        var target = new RenderTexture(320, 320, 24);
        var previous = RenderTexture.active;
        try
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            var attack = root.GetComponent<Mavis.SahurAttack>();
            var movement = root.GetComponent<ThirdPersonPlayerController>();
            movement.enabled = false;
            var animator = attack.animator;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var relay = animator.GetComponent<SahurRootMotionRelay>();
            if (relay != null) UnityEngine.Object.DestroyImmediate(relay);
            var camera = cameraObject.AddComponent<Camera>();
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.22f, .26f, .30f);
            camera.orthographic = true;
            camera.orthographicSize = 3.3f;
            camera.targetTexture = target;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(35, -35, 0);
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var report = new System.Collections.Generic.List<object>();
            for (int stage = 0; stage < 3; stage++)
            {
                attack.SuspendForSwimming();
                root.GetComponent<Mavis.PlayerStamina>().currentStamina = 100f;
                animator.Play("Locomotion", 0, 0f);
                animator.Update(.02f);
                typeof(Mavis.SahurAttack).GetMethod("StartComboStage", Private).Invoke(attack, new object[] { stage });
                animator.Update(.1f);
                float maxStep = 0, minHead = float.MaxValue;
                Vector3 lastHand = hand.position;
                for (int sample = 0; sample <= 120; sample++)
                {
                    float phase = sample / 120f;
                    animator.Play("Combo " + (stage + 1), 0, phase);
                    animator.Update(0f);
                    if (sample > 0) maxStep = Mathf.Max(maxStep, Vector3.Distance(lastHand, hand.position));
                    lastHand = hand.position;
                    var capsule = (CapsuleCollider)attack.stickHitbox;
                    Vector3 center = capsule.transform.TransformPoint(capsule.center);
                    Vector3 extent = capsule.transform.TransformVector(Vector3.up * (capsule.height * .5f - capsule.radius));
                    Vector3 a = center - extent, b = center + extent;
                    Vector3 segment = b - a;
                    float t = Mathf.Clamp01(Vector3.Dot(head.position - a, segment) / segment.sqrMagnitude);
                    minHead = Mathf.Min(minHead, Vector3.Distance(head.position, a + t * segment));
                    if (sample % 30 == 0)
                    {
                        Vector3 focus = root.transform.position;
                        camera.transform.position = focus + new Vector3(5, 1.5f, 7);
                        camera.transform.LookAt(focus);
                        var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>();
                        var bakedObjects = new System.Collections.Generic.List<GameObject>();
                        var bakedMeshes = new System.Collections.Generic.List<Mesh>();
                        foreach (var skin in skins)
                        {
                            var mesh = new Mesh();
                            skin.BakeMesh(mesh, true);
                            var obj = new GameObject("Baked inspection pose");
                            obj.layer = 31;
                            obj.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                            obj.transform.localScale = skin.transform.lossyScale;
                            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                            obj.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                            skin.enabled = false;
                            bakedObjects.Add(obj);
                            bakedMeshes.Add(mesh);
                        }
                        camera.Render();
                        RenderTexture.active = target;
                        frame.ReadPixels(new Rect(0, 0, 320, 320), 0, 0);
                        frame.Apply();
                        sheet.SetPixels32(sample / 30 * 320, (2 - stage) * 320, 320, 320, frame.GetPixels32());
                        foreach (var skin in skins) skin.enabled = true;
                        foreach (var obj in bakedObjects) UnityEngine.Object.DestroyImmediate(obj);
                        foreach (var mesh in bakedMeshes) UnityEngine.Object.DestroyImmediate(mesh);
                    }
                }
                report.Add(new { stage = stage + 1, maxHandStep = maxStep, minHeadBoneDistance = minHead });
            }
            sheet.Apply();
            var chains = new System.Collections.Generic.List<object>();
            foreach (float dt in new[] { 1f / 20f, 1f / 30f, 1f / 60f })
            {
                attack.SuspendForSwimming();
                root.GetComponent<Mavis.PlayerStamina>().currentStamina = 100f;
                animator.Play("Locomotion", 0, 0f);
                animator.Update(.1f);
                typeof(Mavis.SahurAttack).GetMethod("StartComboStage", Private).Invoke(attack, new object[] { 0 });
                int highest = 0;
                bool locomotionGap = false;
                for (int tick = 0; tick < 400 && attack.IsGroundComboActive; tick++)
                {
                    typeof(Mavis.SahurAttack).GetField("attackStartedAt", Private).SetValue(attack, Time.time - 1f);
                    if (attack.CurrentComboStage < 2)
                        typeof(Mavis.SahurAttack).GetField("comboContinueQueued", Private).SetValue(attack, true);
                    animator.Update(dt);
                    if (highest < 2 && tick > 4 && animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))
                        locomotionGap = true;
                    typeof(Mavis.SahurAttack).GetMethod("UpdateAttackProgress", Private).Invoke(attack, null);
                    highest = Mathf.Max(highest, attack.CurrentComboStage);
                }
                if (highest != 2 || locomotionGap || attack.IsCombatMotionActive)
                    throw new Exception("Combo chain or recovery failed at " + (1f / dt) + " FPS.");
                chains.Add(new { fps = Mathf.RoundToInt(1f / dt), allThreeStages = true, locomotionGap, finishes = true });
            }
            string path = System.IO.Path.GetFullPath("Tools/SahurComboInspection.png");
            System.IO.File.WriteAllBytes(path, sheet.EncodeToPNG());
            return new { path, rows = "Combo 1, 2, 3; columns 0%,25%,50%,75%,100%", report, chains };
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(sheet);
            UnityEngine.Object.DestroyImmediate(frame);
        }
    }
}
