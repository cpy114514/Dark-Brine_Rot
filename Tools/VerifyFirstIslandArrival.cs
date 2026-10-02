using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyFirstIslandArrival
{
    const string Main = "Assets/Scenes/First Island/Main.unity";
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static async Task<FirstIslandArrival> WaitArrival()
    {
        for (int k = 0; k < 400; k++)
        {
            var arrival = UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
            if (arrival != null) return arrival;
            await Task.Delay(50);
        }
        throw new Exception("Gameplay arrival did not load.");
    }
    static async Task Reload()
    {
        var load = SceneManager.LoadSceneAsync(Main, LoadSceneMode.Single);
        while (!load.isDone) await Task.Delay(50);
    }
    public static async Task<object> VerifyStoryBridge()
    {
        Require(Application.isPlaying, "Requires Play Mode.");
        var old = UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
        Require(old != null && old.IsAwake, "Run after the arrival test.");
        var sequenceObject = new GameObject("Story1 blackout bridge test");
        var sequence = sequenceObject.AddComponent<Story1OceanAwakening>();
        sequence.player = old.player; sequence.storyCamera = Camera.main;
        sequence.blackHoldSeconds = .1f;
        var screen = new GameObject("Bridge test blackout", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        screen.transform.SetParent(sequenceObject.transform);
        sequence.BeginBlackout(screen.GetComponent<UnityEngine.UI.Image>(), old.player.transform.position, Vector3.forward);
        Require(sequence.HasBegun && sequence.plank == null, "The new bridge still required a floating plank.");
        for (int k = 0; k < 200 && old != null; k++) await Task.Delay(50);
        Require(old == null, "Story1 blackout did not load the island.");
        var arrival = await WaitArrival();
        Require(!arrival.IsAwake && !arrival.HasStick, "Story1 skipped the unconscious beach arrival.");
        if (arrival.player.GetComponent<GameSaveExcluded>() == null) arrival.player.gameObject.AddComponent<GameSaveExcluded>();
        return new { story1BlackoutLoadsIsland = true, floatingPlankNotRequired = true, unconsciousAndUnarmed = true };
    }
    public static async Task<object> Verify()
    {
        Require(Application.isPlaying, "Requires Play Mode.");
        string save = Path.Combine(Application.persistentDataPath, "dark-brine-rot-save.json");
        byte[] originalSave = File.Exists(save) ? File.ReadAllBytes(save) : null;
        try
        {
            GameSaveManager.CancelPendingContinue(); await Reload();
            var arrival = await WaitArrival(); var player = arrival.player;
            Require(!arrival.IsAwake && !arrival.HasStick && !player.enabled, "New game did not start unconscious and unarmed.");
            Require(!arrival.TryPickUp(), "Unconscious player collected the stick.");
            Require(!GameSaveManager.SaveCurrentGame(), "Saved an unsafe drifting pose.");
            var attack = player.GetComponent<SahurAttack>(); var boomerang = player.GetComponent<SahurBoomerang>();
            attack.TriggerAttack(); Require(!attack.IsCombatMotionActive && !boomerang.TryThrow(), "Unconscious player attacked.");
            bool landedUnconscious = false, lyingOnWaves = false, visibleGetUp = false, headLeading = false;
            float maxTravel = 0, dryDriftDistance = 0, landedMovement = 0, lowestGetUpHead = float.MaxValue, highestGetUpHead = float.MinValue;
            Vector3 start = arrival.driftStart.position, heading = Vector3.ProjectOnPlane(arrival.shore.position - start, Vector3.up).normalized;
            Vector3? firstDry = null, landedPosition = null;
            var island = UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None);
            MeshCollider ground = null;
            foreach (var candidate in island)
                if (candidate.isActiveAndEnabled && candidate.gameObject.scene.name == "Environment") ground = candidate.GetComponent<MeshCollider>();
            for (int k = 0; k < 600 && !arrival.IsAwake; k++)
            {
                await Task.Delay(50);
                if (arrival.HasLanded) landedUnconscious = true;
                var screen = (UnityEngine.UI.Image)typeof(FirstIslandArrival).GetField("blackout",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(arrival);
                bool visibleDrift = !arrival.HasLanded && screen != null && screen.color.a < .999f;
                if (visibleDrift && Vector3.Dot(player.CharacterAnimator.bodyRotation * Vector3.forward, Vector3.up) > .85f) lyingOnWaves = true;
                if (lyingOnWaves && visibleDrift)
                {
                    var animator = player.CharacterAnimator;
                    Vector3 headDirection = Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.Head).position - animator.GetBoneTransform(HumanBodyBones.Hips).position, Vector3.up).normalized;
                    Require(Vector3.Dot(headDirection, heading) > .9f, "The survivor drifted feet-first toward shore.");
                    headLeading = true;
                    Vector3 driftAt = player.transform.position;
                    if (ground.Raycast(new Ray(new Vector3(driftAt.x, ground.bounds.max.y + 30, driftAt.z), Vector3.down), out var hit, ground.bounds.size.y + 60) && hit.point.y >= player.seaLevel + .5f)
                    {
                        if (!firstDry.HasValue) firstDry = driftAt;
                        dryDriftDistance = Mathf.Max(dryDriftDistance, Vector3.ProjectOnPlane(driftAt - firstDry.Value, Vector3.up).magnitude);
                    }
                }
                if (arrival.HasLanded)
                {
                    if (!landedPosition.HasValue) landedPosition = player.transform.position;
                    landedMovement = Mathf.Max(landedMovement, Vector3.ProjectOnPlane(player.transform.position - landedPosition.Value, Vector3.up).magnitude);
                }
                if(arrival.IsGettingUp){
                    visibleGetUp=true;
                    float head=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head).position.y;
                    lowestGetUpHead=Mathf.Min(lowestGetUpHead,head);highestGetUpHead=Mathf.Max(highestGetUpHead,head);
                    Require(!player.enabled&&!arrival.CanPickUp,"Controls enabled before getting up finished.");
                    var blackout=(UnityEngine.UI.Image)typeof(FirstIslandArrival).GetField("blackout",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(arrival);
                    Require(blackout.color.a<.001f,"Get-up animation was hidden behind black.");
                }
                if (visibleDrift || arrival.HasLanded) maxTravel = Mathf.Max(maxTravel, Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up).magnitude);
                Require(!arrival.HasStick, "Stick appeared before pickup.");
            }
            Require(arrival.IsAwake && arrival.HasLanded && landedUnconscious && lyingOnWaves && maxTravel > 50, "Wave-to-shore cinematic did not complete correctly.");
            Require(headLeading && dryDriftDistance < 2 && landedMovement < .02f, "Survivor kept floating along the beach after landing.");
            Require(arrival.shore.position.y - player.seaLevel < 1.1f && Vector3.ProjectOnPlane(player.transform.position - arrival.shore.position, Vector3.up).magnitude < 4,
                "Landing remained too far inland.");
            Require(visibleGetUp && highestGetUpHead-lowestGetUpHead > 2,"Did not visibly animate from lying to standing.");
            Require(player.enabled && player.GetComponent<CharacterController>().enabled && !player.ExternalControlLock, "Wake-up did not restore locomotion.");
            Require(!arrival.TryPickUp() && !attack.enabled && !boomerang.TryThrow(), "Weapon usable before reaching the stick.");
            Require(GameSaveManager.SaveCurrentGame(), "Could not save unarmed exploration.");
            Require(GameSaveManager.QueueContinue(), "Could not queue unarmed continue."); await Reload();
            arrival = await WaitArrival(); await Task.Delay(350); player = arrival.player;
            Require(arrival.IsAwake && arrival.HasLanded && !arrival.HasStick && player.enabled, "Continue replayed intro or restored a missing weapon.");
            Require(GameObject.Find("Lost Sahur Stick") != null, "Continue lost the beach pickup.");
            var lost = GameObject.Find("Lost Sahur Stick").GetComponent<Renderer>();
            Require(Vector3.ProjectOnPlane(lost.bounds.center - arrival.lostStickPoint.position, Vector3.up).magnitude < .01f, "Visible stick was offset from its pickup point.");
            Vector3 at = arrival.lostStickPoint.position;
            var capsule = player.GetComponent<CharacterController>();
            float sole = (capsule.center.y - capsule.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
            player.RestoreSavedPose(at + Vector3.up * (.08f - sole), arrival.shore.rotation); Physics.SyncTransforms();
            await Task.Delay(100);
            Require(arrival.CanPickUp && arrival.TryPickUp(), "Nearby living player could not pick up the stick.");
            Require(arrival.HasStick && player.GetComponent<SahurAttack>().enabled && player.GetComponent<SahurBoomerang>().enabled &&
                player.GetComponent<SahurSwimmingWeapon>().stick.gameObject.activeInHierarchy, "Pickup did not restore the held weapon and combat.");
            Require(!arrival.TryPickUp(), "Collected the stick twice.");
            foreach (var renderer in player.GetComponent<SahurSwimmingWeapon>().stick.GetComponentsInChildren<Renderer>())
                Require(renderer.enabled, "The recovered held stick remained invisible.");
            Require(GameSaveManager.SaveCurrentGame() && GameSaveManager.QueueContinue(), "Could not save recovered stick."); await Reload();
            arrival = await WaitArrival(); await Task.Delay(350);
            Require(arrival.IsAwake && arrival.HasStick && GameObject.Find("Lost Sahur Stick") == null, "Recovered weapon did not survive continue.");
            return new { faceUpWaveDrift = lyingOnWaves, headLeadsTowardShore = headLeading, dryDriftDistance, landedMovement, landingHeight = arrival.shore.position.y, visibleGetUp, getUpHeadRise=highestGetUpHead-lowestGetUpHead, driftDistance = maxTravel, unconsciousOnBeach = landedUnconscious,
                wakesMobileAndUnarmed = true, nearbyPickupRestoresCombat = true, noDuplicatePickup = true, unarmedAndArmedContinue = true };
        }
        finally
        {
            // Preserve the human's save and prevent Play Mode exit from overwriting it.
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if (player != null && player.GetComponent<GameSaveExcluded>() == null) player.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();
            if (originalSave != null) File.WriteAllBytes(save, originalSave);
            else if (File.Exists(save)) File.Delete(save);
        }
    }
}
