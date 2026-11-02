using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Exercises the finish pickups and flight using the real player motor and input.</summary>
[InitializeOnLoad]
public static class VerifySkyboardReward
{
    const string Request = "Temp/VerifySkyboard.request";
    static Keyboard keyboard;
    static Key[] keys = Array.Empty<Key>();
    static VerifySkyboardReward()
    {
        if (!File.Exists(Request)) return;
        EditorApplication.playModeStateChanged += StateChanged;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (Application.isBatchMode && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Second Island/Second Island.unity")
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Second Island/Second Island.unity");
            EditorApplication.isPlaying = true;
        };
    }
    static void StateChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= StateChanged;
        if (File.Exists(Request)) File.Delete(Request);
        Run();
    }
    static void Inject()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard, new KeyboardState(keys));
    }
    static async Task Wait(float seconds)
    {
        float end = Time.time + seconds;
        var deadline = DateTime.UtcNow.AddSeconds(Math.Max(20, seconds * 3));
        while (Time.time < end)
        {
            if (!EditorApplication.isPlaying || DateTime.UtcNow > deadline) throw new Exception("Play mode stopped or stalled.");
            await Task.Delay(25);
        }
    }
    static async Task Press(Key key, float seconds = .15f)
    {
        keys = new[] { key }; await Wait(seconds); keys = Array.Empty<Key>(); await Wait(.15f);
    }
    static void Progress(string value) { File.WriteAllText("Temp/VerifySkyboard.progress", value); Debug.Log("Skyboard review: " + value); }
    static async void Run()
    {
        string result = "";
        try
        {
            await Wait(.5f);
            var opening = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>(); if (opening) UnityEngine.Object.DestroyImmediate(opening.gameObject);
            var enemies = GameObject.Find("Marshmallow Friends - Second Island"); if (enemies) enemies.SetActive(false);
            var boss = UnityEngine.Object.FindFirstObjectByType<PigRoninBoss>(); if (boss) boss.gameObject.SetActive(false);
            var root = GameObject.Find("Marshmallow Hop Challenge - 33");
            var reward = root ? root.GetComponent<SkyboardReward>() : null;
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if (!root || !reward || !player || !reward.enabled) throw new Exception("Reward, player or course missing.");
            var body = player.GetComponent<CharacterController>();
            player.GetComponent<Mavis.PlayerHealth>().GrantProtection(180);
            keyboard = Keyboard.current;
            if (keyboard == null && Application.isBatchMode) keyboard = InputSystem.AddDevice<Keyboard>();
            if (Mouse.current == null && Application.isBatchMode) InputSystem.AddDevice<Mouse>();
            if (keyboard == null) throw new Exception("No keyboard.");
            InputSystem.onBeforeUpdate += Inject;
            if (reward.flightSeconds != 60 || reward.cooldownSeconds != 900 || reward.scrollReduction != 1200) throw new Exception("Incorrect configured durations.");
            Transform last = null; foreach (Transform row in root.transform) if (row.name.StartsWith("Row ")) last = row;
            async Task Place(int index)
            {
                var box = last.GetChild(index).GetComponentInChildren<BoxCollider>();
                float offset = -(body.center.y - body.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
                var pos = box.bounds.center; pos.y = box.bounds.max.y + offset + .06f;
                player.RestoreSavedPose(pos, Quaternion.identity); await Wait(.75f);
            }
            await Place(0); if (!reward.Owned) throw new Exception("Skyboard pickup failed.");
            await Place(1); if (reward.Scrolls != 10 || reward.CollectedScrolls != 10) throw new Exception("Ten scrolls not collected.");
            var recovery = root.GetComponent<MarshmallowHopRecovery>();
            int previousRecoveries = recovery ? recovery.RespawnCount : 0;
            Progress("Both moving-platform pickups collected; testing flight controls.");
            await Press(Key.F); if (!reward.Flying || !player.ExternalMovementLock) throw new Exception("F did not start flight.");
            if (reward.FlightRemaining < 58 || reward.FlightRemaining > 60) throw new Exception("Flight did not begin at 60 seconds.");
            Vector3 from = player.transform.position;
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Forward), GameInputSettings.Get(GameInputSettings.Action.Jump) }; await Wait(1.2f);
            keys = Array.Empty<Key>(); await Wait(.3f);
            Vector3 moved = player.transform.position - from;
            if (new Vector2(moved.x, moved.z).magnitude < 5 || moved.y < 3) throw new Exception("Forward / ascend movement failed: " + moved);
            var board = GameObject.Find("Sahur skyboard"); if (!board) throw new Exception("Ridden board missing.");
            var renderers = board.GetComponentsInChildren<Renderer>(); if (renderers.Length == 0) throw new Exception("Ridden board has no visible mesh.");
            var bounds = renderers[0].bounds; for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var anim = player.CharacterAnimator;
            if (!anim || !anim.isHuman) throw new Exception("Humanoid rider animator missing.");
            using (var poseHandler = new HumanPoseHandler(anim.avatar, anim.transform))
            {
                var pose = new HumanPose(); poseHandler.GetHumanPose(ref pose);
                int leftKnee = Array.IndexOf(HumanTrait.MuscleName, "Left Lower Leg Stretch");
                int rightKnee = Array.IndexOf(HumanTrait.MuscleName, "Right Lower Leg Stretch");
                if (leftKnee < 0 || rightKnee < 0 || pose.muscles[leftKnee] < .2f || pose.muscles[rightKnee] < .2f)
                    throw new Exception("Riding pose was overwritten or knees are folded too far.");
            }
            Vector3 leftOffset = default, rightOffset = default;
            SahurSupportPose.MeasureSoles(anim, ref leftOffset, ref rightOffset);
            Vector3 leftSole = anim.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint(leftOffset);
            Vector3 rightSole = anim.GetBoneTransform(HumanBodyBones.RightFoot).TransformPoint(rightOffset);
            if (Mathf.Abs(bounds.max.y - reward.DeckHeight) > .08f ||
                leftSole.y < bounds.max.y - .07f || rightSole.y < bounds.max.y - .07f ||
                leftSole.y - bounds.max.y > .22f || rightSole.y - bounds.max.y > .22f)
                throw new Exception($"Soles do not rest on deck: board={bounds.max.y:F2}, left={leftSole.y:F2}, right={rightSole.y:F2}.");
            if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("Temp/SkyboardFlight.png");
            float y = player.transform.position.y; keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Dodge) }; await Wait(.6f); keys = Array.Empty<Key>(); await Wait(.2f);
            if (player.transform.position.y > y - 2) throw new Exception("Descend input did not work.");
            float footRelativeToRoot = reward.DeckHeight - player.transform.position.y;
            var nearCeiling = player.transform.position; nearCeiling.y = reward.CeilingHeight - footRelativeToRoot - .6f;
            player.RestoreSavedPose(nearCeiling, player.transform.rotation); await Wait(.3f);
            keys = new[] { GameInputSettings.Get(GameInputSettings.Action.Jump) }; await Wait(1.1f); keys = Array.Empty<Key>(); await Wait(.2f);
            if (reward.DeckHeight > reward.CeilingHeight + .06f || reward.DeckHeight < reward.CeilingHeight - .3f)
                throw new Exception($"Altitude limit failed: deck={reward.DeckHeight:F2}, limit={reward.CeilingHeight:F2}.");
            Progress("Flying and steering work; checking the full 60-second duration.");
            await Wait(Mathf.Max(0, reward.FlightRemaining - .5f));
            if (!reward.Flying) throw new Exception("Flight expired too early.");
            await Wait(1f);
            if (reward.Flying || reward.Landing || player.ExternalMovementLock || reward.CooldownRemaining < 898) throw new Exception("Expiry did not release the player into free fall and start cooldown.");
            float fallStart = player.transform.position.y; await Wait(.5f);
            if (player.transform.position.y > fallStart - .2f) throw new Exception("Player did not fall after the timer expired.");
            if (recovery && recovery.RespawnCount != previousRecoveries) throw new Exception("Flying away incorrectly triggered the course fall recovery.");
            result += "60-second flight, both feet aligned, altitude limit and free fall; ";
            await Place(1);
            var ui = player.GetComponent<Mavis.SahurLoadoutUI>();
            if (!ui || ui.EquipmentSlotCount != 6) throw new Exception("Tools category is missing.");
            ui.SetOpen(true); ui.SelectEquipment(Mavis.SahurLoadoutUI.EquipmentSlot.Tools);
            Button FindButton(string label)
            {
                foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var caption = button.GetComponentInChildren<Text>();
                    if (button.gameObject.activeInHierarchy && caption && caption.text.StartsWith(label, StringComparison.Ordinal)) return button;
                }
                return null;
            }
            var scrollCard = FindButton("Speed Scroll"); var boardCard = FindButton("Skyboard");
            if (!ui.PanelVisible || !scrollCard || !boardCard) throw new Exception("Collected tools not visible in the equipment menu.");
            scrollCard.onClick.Invoke(); ui.EquipSelection();
            var boardTarget = FindButton("Skyboard");
            if (!boardTarget) throw new Exception("Scroll target selection did not show the skyboard.");
            boardTarget.onClick.Invoke(); ui.EquipSelection();
            if (reward.Scrolls != 9 || reward.CooldownRemaining != 0) throw new Exception("Equipment menu did not apply the scroll to the chosen item.");
            if (reward.TryUseScrollOn(reward) || reward.Scrolls != 9) throw new Exception("Scroll consumed when cooldown already zero.");
            ui.SetOpen(false); await Wait(.2f);
            await Press(Key.F); if (!reward.Flying) throw new Exception("Could not fly after scroll.");
            await Wait(.35f); await Press(Key.F);
            if (reward.Flying || reward.CooldownRemaining < 898) throw new Exception("Manual landing failed.");
            result += "15-minute cooldown, scroll clamped to zero, no waste, reuse, manual dismount; passed";
            Progress(result);
        }
        catch (Exception e) { result += "FAILED: " + e; Progress(result); }
        finally
        {
            keys = Array.Empty<Key>(); InputSystem.onBeforeUpdate -= Inject;
            if (keyboard != null) InputState.Change(keyboard, new KeyboardState());
            File.WriteAllText("Temp/VerifySkyboard.result", result);
            if (Application.isBatchMode) EditorApplication.Exit(result.Contains("FAILED") ? 1 : 0);
        }
    }
}
