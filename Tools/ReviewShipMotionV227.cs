using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class ReviewShipMotionV227
{
    public static object Start(string label, int fps = 30)
    {
        if (!Application.isPlaying) throw new Exception("Use the existing Editor Play Mode.");
        string folder = ".codex/ship-motion-20261020/" + label;
        if (Directory.Exists(folder)) throw new Exception("Preserve previous evidence.");
        Directory.CreateDirectory(folder);
        var probe = new GameObject("Temporary ship motion review v227").AddComponent<ShipMotionReviewV227>();
        probe.folder = folder;
        probe.fps = fps;
        return new { started = true, folder };
    }

    public static object FinishExisting()
    {
        var probe = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Single(p => p.GetType().Name == "ShipMotionReviewV227");
        var type = probe.GetType();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        string folder = (string)type.GetField("folder").GetValue(probe);
        File.WriteAllText(folder + "/handoff.json", Newtonsoft.Json.JsonConvert.SerializeObject(
            type.GetField("impactRows", flags).GetValue(probe), Newtonsoft.Json.Formatting.Indented));
        type.GetMethod("Finish", flags).Invoke(probe, null);
        return new { stoppedAtTutorial = true, folder };
    }
}

[DefaultExecutionOrder(31900)]
public sealed class ShipMotionReviewV227 : MonoBehaviour
{
    public string folder;
    public int fps = 30;
    ShipSailingMotion ship;
    Story1SharkCollisionSequence sequence;
    Story1SahurPassenger passenger;
    Animator animator;
    Camera cameraView;
    RenderTexture target;
    Texture2D pixels;
    readonly List<object> rows = new List<object>(1600);
    readonly BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
    int frames, captureIndex, oldCapture;
    float start, oldScale;
    Vector3 origin, initialHeading, initialSoleLocal;
    Quaternion initialRotation;
    Vector3 leftSole, rightSole;
    Collider[] colliders;
    bool initialized;
    bool handoffStarted;
    readonly List<object> impactRows = new List<object>();

    void Start()
    {
        sequence = FindFirstObjectByType<Story1SharkCollisionSequence>();
        ship = sequence.ship;
        sequence.enabled = false; // Hold intact voyage only in Play Mode.
        passenger = sequence.sahur.GetComponent<Story1SahurPassenger>();
        animator = sequence.sahur.CharacterAnimator;
        colliders = ship.GetComponentsInChildren<Collider>();
        oldCapture = Time.captureFramerate; oldScale = Time.timeScale;
        Time.captureFramerate = fps; Time.timeScale = 1;
        start = Time.time; origin = ship.transform.position; initialRotation = ship.transform.rotation;
        initialHeading = Vector3.ProjectOnPlane(ship.transform.right, Vector3.up).normalized;
        cameraView = new GameObject("Temporary ship review camera").AddComponent<Camera>();
        cameraView.CopyFrom(sequence.storyCamera);
        cameraView.gameObject.AddComponent<UniversalAdditionalCameraData>();
        cameraView.fieldOfView = 48;
        target = new RenderTexture(960, 540, 24); target.Create(); cameraView.targetTexture = target;
        pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
        EditorApplication.isPaused = false;
    }

    void LateUpdate()
    {
        float clock = Time.time - start;
        if (handoffStarted) { ReviewHandoff(clock - 24); return; }
        Vector3 heading = Vector3.ProjectOnPlane(ship.transform.right, Vector3.up).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, heading);
        Vector3 look = ship.transform.position + Vector3.up * 19;
        cameraView.transform.SetPositionAndRotation(look - heading * 120 + side * 115 + Vector3.up * 43,
            Quaternion.LookRotation(heading * 120 - side * 115 - Vector3.up * 43, Vector3.up));
        if (!initialized && clock > .3f)
        {
            leftSole = (Vector3)typeof(Story1SahurPassenger).GetField("leftSole", fields).GetValue(passenger);
            rightSole = (Vector3)typeof(Story1SahurPassenger).GetField("rightSole", fields).GetValue(passenger);
            initialSoleLocal = ship.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint(leftSole));
            initialized = true;
        }
        float leftGap = 0, rightGap = 0;
        bool leftFound = false, rightFound = false;
        Vector3 localSole = initialSoleLocal;
        if (initialized)
        {
            Vector3 left = animator.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint(leftSole);
            Vector3 right = animator.GetBoneTransform(HumanBodyBones.RightFoot).TransformPoint(rightSole);
            leftFound = Story1SahurPassenger.TryDeckSurface(colliders, left, out var lp);
            rightFound = Story1SahurPassenger.TryDeckSurface(colliders, right, out var rp);
            leftGap = left.y - lp.y; rightGap = right.y - rp.y;
            localSole = ship.transform.InverseTransformPoint(left);
        }
        var type = typeof(ShipSailingMotion);
        rows.Add(new {
            clock, y = ship.transform.position.y,
            pitch = (float)type.GetField("filteredPitch", fields).GetValue(ship),
            roll = (float)type.GetField("filteredRoll", fields).GetValue(ship),
            speed = ship.CurrentSpeed,
            courseError = Vector3.SignedAngle(initialHeading, heading, Vector3.up),
            leftFound, rightFound, leftGap, rightGap,
            soleLocal = new[] { localSole.x, localSole.y, localSole.z },
            passengerEnabled = passenger.enabled, shipEnabled = ship.enabled
        });
        // Read the completed normal render from the preceding frame.
        if (frames > 4 && clock >= 2 + captureIndex * 4 && captureIndex < 6)
        {
            var previous = RenderTexture.active;
            try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0, false);
                File.WriteAllBytes(folder + "/ship-" + captureIndex + ".png", pixels.EncodeToPNG()); }
            finally { RenderTexture.active = previous; }
            captureIndex++;
        }
        frames++;
        if (clock < 24) return;
        File.WriteAllText(folder + "/motion.json", Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
        if (folder.Contains("after"))
        {
            var position = ship.transform.position; var rotation = ship.transform.rotation;
            sequence.BeginImpact();
            handoffStarted = true; captureIndex = 0;
            File.WriteAllText(folder + "/handoff-start.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                positionJump = Vector3.Distance(position, ship.transform.position),
                rotationJump = Quaternion.Angle(rotation, ship.transform.rotation),
                shipDisabled = !ship.enabled, passengerDisabled = !passenger.enabled,
                impacted = sequence.HasImpacted
            }, Newtonsoft.Json.Formatting.Indented));
            return;
        }
        Finish();
    }

    void ReviewHandoff(float clock)
    {
        var battle = sequence.GetComponent<Story1WreckBattle>();
        cameraView.transform.SetPositionAndRotation(sequence.storyCamera.transform.position, sequence.storyCamera.transform.rotation);
        cameraView.fieldOfView = sequence.storyCamera.fieldOfView;
        impactRows.Add(new { clock, stage = battle.RedesignStage.ToString(), battle.RedesignClock,
            shipEnabled = ship.enabled, shipActive = ship.gameObject.activeSelf, passengerEnabled = passenger.enabled });
        if (clock > .5f + captureIndex * 2 && captureIndex < 6)
        {
            var previous = RenderTexture.active;
            try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0, false);
                File.WriteAllBytes(folder + "/handoff-" + captureIndex + ".png", pixels.EncodeToPNG()); }
            finally { RenderTexture.active = previous; }
            captureIndex++;
        }
        if (clock < 14 && battle.RedesignStage != Story1WreckBattle.OpeningStage.Tutorial) return;
        File.WriteAllText(folder + "/handoff.json", Newtonsoft.Json.JsonConvert.SerializeObject(impactRows, Newtonsoft.Json.Formatting.Indented));
        Finish();
    }

    void Finish()
    {
        Time.captureFramerate = oldCapture; Time.timeScale = oldScale;
        enabled = false;
        EditorApplication.isPlaying = false;
    }

    void OnDestroy()
    {
        Time.captureFramerate = oldCapture; Time.timeScale = oldScale;
        if (cameraView != null) Destroy(cameraView.gameObject);
        if (target != null) { target.Release(); Destroy(target); }
        if (pixels != null) Destroy(pixels);
    }
}
