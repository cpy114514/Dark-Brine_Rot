using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class AuditSahurVisualFacing
{
    public static Task<string> Run() => Capture(false);
    public static Task<string> Verify() => Capture(true);

    static async Task<string> Capture(bool verify)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode is required.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var holder = new GameObject("Temporary Sahur visual heading audit");
        holder.SetActive(false);
        holder.transform.position = new Vector3(2200f, 300f, 2200f);
        var actor = UnityEngine.Object.Instantiate(prefab, holder.transform);
        actor.transform.localPosition = Vector3.zero;
        var movement = actor.GetComponent<ThirdPersonPlayerController>();
        var attack = actor.GetComponent<SahurAttack>();
        movement.snapSpawnToIslandSurface = false;
        movement.waterSplashes = false;
        movement.cameraCollision = false;
        foreach (var input in actor.GetComponentsInChildren<PlayerInput>(true)) input.enabled = false;
        var cursor = Cursor.lockState;
        bool cursorVisible = Cursor.visible;
        GameObject cameraObject = null;
        RenderTexture render = null;
        Texture2D texture = null;
        var active = RenderTexture.active;
        var report = new StringBuilder();
        try
        {
            holder.SetActive(true);
            await Task.Delay(400);
            movement.enabled = false;
            attack.enabled = false;
            var animator = attack.animator;
            animator.speed = 0f;
            actor.transform.rotation = Quaternion.identity;
            animator.Play("Locomotion", 0, 0f);
            animator.Update(0f);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var visual = animator.transform;
            Quaternion originalVisualRotation = visual.localRotation;
            Vector3 headForward = head.InverseTransformDirection(actor.transform.forward);
            Vector3 referenceBody = Vector3.ProjectOnPlane(animator.bodyRotation * Vector3.forward, Vector3.up).normalized;
            cameraObject = new GameObject("Temporary Sahur heading camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.2f, 0.22f, 0.24f);
            camera.fieldOfView = 35f;
            foreach (Transform child in actor.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            render = new RenderTexture(960, 720, 24);
            texture = new Texture2D(960, 720, TextureFormat.RGB24, false);
            camera.targetTexture = render;
            var light = cameraObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
            light.cullingMask = 1 << 31;
            for (int stage = 1; stage <= 3; stage++)
            {
                foreach (float phase in new[] { 0f, 0.1f, 0.25f, 0.32f, 0.45f, 0.5f, 0.55f, 0.75f, 0.9f, 0.99f })
                {
                    visual.localRotation = originalVisualRotation;
                    animator.Play("Combo " + stage, 0, phase);
                    animator.Update(0f);
                    typeof(ThirdPersonPlayerController).GetMethod("AlignComboVisualFacing", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(movement, null);
                    await Task.Delay(30);
                    var body = Vector3.ProjectOnPlane(animator.bodyRotation * Vector3.forward, Vector3.up).normalized;
                    var face = Vector3.ProjectOnPlane(head.TransformDirection(headForward), Vector3.up).normalized;
                    var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
                    var right = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
                    var shoulders = Vector3.ProjectOnPlane(Vector3.Cross(right - left, Vector3.up), Vector3.up).normalized;
                    report.AppendLine($"Combo {stage} phase={phase:F2}: rootYaw={actor.transform.eulerAngles.y:F2}, bodyYaw={Vector3.SignedAngle(referenceBody, body, Vector3.up):F2}, headYaw={Vector3.SignedAngle(Vector3.forward, face, Vector3.up):F2}, shoulderYaw={Vector3.SignedAngle(Vector3.forward, shoulders, Vector3.up):F2}");
                    if (verify && stage == 3 && phase >= attack.comboThreeHitWindow.x &&
                        phase <= attack.comboThreeHitWindow.y && Vector3.Dot(face, Vector3.forward) <= 0f)
                        throw new InvalidOperationException("The third hit faces backwards during its hit window.");
                    if (stage == 3 && (phase == 0f || phase == 0.5f || phase == 0.99f))
                    {
                        var bounds = actor.GetComponentInChildren<SkinnedMeshRenderer>().bounds;
                        Vector3 target = bounds.center;
                        float distance = Mathf.Max(3.5f, bounds.extents.magnitude / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.3f);
                        Vector3 offset = new Vector3(2.5f, 0.7f, 4.3f).normalized * distance;
                        camera.transform.SetPositionAndRotation(target + offset, Quaternion.LookRotation(-offset));
                        camera.Render();
                        RenderTexture.active = render;
                        texture.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
                        texture.Apply();
                        string path = Path.Combine(Path.GetTempPath(), $"sahur-combo3-heading-{phase:F2}.png");
                        File.WriteAllBytes(path, texture.EncodeToPNG());
                        report.AppendLine("Preview: " + path);
                    }
                }
            }
            return report.ToString();
        }
        finally
        {
            RenderTexture.active = active;
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
            if (render != null) UnityEngine.Object.DestroyImmediate(render);
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(holder);
            Cursor.lockState = cursor;
            Cursor.visible = cursorVisible;
        }
    }
}
