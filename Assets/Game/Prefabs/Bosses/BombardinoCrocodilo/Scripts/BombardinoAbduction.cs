using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>
    /// After Bombardino is wrecked on the first island, it snatches Sahur,
    /// climbs away, then drops him out of the sky over the second island.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(800)]
    public sealed class BombardinoAbduction : MonoBehaviour
    {
        const string FlatScene = "Assets/Scenes/Second Island.unity";
        const string NestedScene = "Assets/Scenes/Second Island/Second Island.unity";
        public static bool Playing { get; private set; }
        BombardinoBoss boss;
        ThirdPersonPlayerController player;
        CharacterController capsule;
        Transform anchor;
        Camera view;
        Vector3 cameraVelocity, bellyLocal, playerHome;
        Quaternion visualRest, playerHomeRotation;
        bool carried, sceneReady, restored;
        Scene destination;
        Vector3 islandScale = Vector3.one;

        public static bool TryBegin(BombardinoBoss boss)
        {
            if (!Application.isPlaying || Playing || !boss) return false;
            if (!boss.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/")) return false;
            var body = boss.target ? boss.target.GetComponent<ThirdPersonPlayerController>() : null;
            if (!body || !body.isActiveAndEnabled) return false;
            var run = boss.gameObject.AddComponent<BombardinoAbduction>();
            run.boss = boss;
            run.player = body;
            return true;
        }

        IEnumerator Start()
        {
            Playing = true;
            capsule = player.GetComponent<CharacterController>();
            playerHome = player.transform.position;
            playerHomeRotation = player.transform.rotation;
            bellyLocal = boss.transform.InverseTransformPoint(Belly(boss));
            visualRest = boss.visual ? boss.visual.localRotation : Quaternion.identity;
            var motion = player.Motion ? player.Motion : player.gameObject.AddComponent<SahurMotionContext>();
            motion.Claim(this, SahurControlState.Cinematic);
            player.ExternalControlLock = true;
            player.ExternalMovementLock = true;
            if (capsule) capsule.enabled = false;
            player.enabled = false;
            CinematicLetterbox.Set(this, true);
            view = Camera.main;
            boss.Explosion(boss.transform.position, 3.5f);

            Vector3 forward = Flat(boss.transform.forward);
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Quaternion face = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 bellyTarget = player.transform.position + Vector3.up * (Hang() + .8f);
            yield return MoveBoss(boss.transform.position, RootForBelly(bellyTarget, face), forward, 1.7f, -18f);
            AttachUnderBelly();
            Vector3 away = Flat(boss.transform.forward);
            if (away.sqrMagnitude < .01f) away = forward;
            Vector3 climb = boss.transform.position + away * 96f + Vector3.up * 52f;
            yield return MoveBoss(boss.transform.position, climb, away, 3.7f, 16f);

            string scenePath = ScenePath();
            if (string.IsNullOrEmpty(scenePath)) { RestoreInPlace(); yield break; }
            DontDestroyOnLoad(boss.gameObject);
            SceneManager.sceneLoaded += SceneArrived;
            if (!SceneLoadingScreen.Load(scenePath))
            {
                SceneManager.sceneLoaded -= SceneArrived;
                RestoreInPlace();
                yield break;
            }
            float deadline = Time.realtimeSinceStartup + 45f;
            while (!sceneReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!sceneReady) { SceneManager.sceneLoaded -= SceneArrived; RestoreInPlace(); yield break; }
            while (SceneLoadingScreen.IsLoading) yield return null;
            yield return null;
            view = Camera.main;
            CinematicLetterbox.Set(this, true);
            yield return ThrowOntoIsland();
            Finish();
        }

        void SceneArrived(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Second Island") return;
            SceneManager.sceneLoaded -= SceneArrived;
            destination = scene;
            ThirdPersonPlayerController resident = null;
            foreach (var other in FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))
            {
                if (!other || other == player) continue;
                if (other.gameObject.scene == scene) resident = other;
            }
            SceneManager.MoveGameObjectToScene(boss.gameObject, scene);
            if (resident) AdoptIslandBody(resident);
            sceneReady = true;
        }

        void AdoptIslandBody(ThirdPersonPlayerController resident)
        {
            var travel = resident.GetComponent<MarshmallowTraversal>();
            var experience = resident.GetComponent<MarshmallowIslandExperience>();
            if (travel && !player.GetComponent<MarshmallowTraversal>())
            {
                var copy = player.gameObject.AddComponent<MarshmallowTraversal>();
                copy.gravity = travel.gravity;
                copy.jumpHeight = travel.jumpHeight;
                copy.walkingBoundHeight = travel.walkingBoundHeight;
                copy.sprintingBoundHeight = travel.sprintingBoundHeight;
                copy.groundCompressionTime = travel.groundCompressionTime;
                copy.airControl = travel.airControl;
                copy.landingSquash = travel.landingSquash;
            }
            if (experience && !player.GetComponent<MarshmallowIslandExperience>())
            {
                var copy = player.gameObject.AddComponent<MarshmallowIslandExperience>();
                copy.surface = experience.surface;
                copy.fluffMaterial = experience.fluffMaterial;
                copy.standingDepression = experience.standingDepression;
                copy.impressionLifetime = experience.impressionLifetime;
                copy.soundVolume = experience.soundVolume;
                copy.cameraSpring = experience.cameraSpring;
                copy.ambientFloss = experience.ambientFloss;
            }
            var strides = resident.GetComponent<MarshmallowSpacewalk>();
            if (strides && !player.GetComponent<MarshmallowSpacewalk>())
            {
                var copy = player.gameObject.AddComponent<MarshmallowSpacewalk>();
                copy.leftStride = strides.leftStride;
                copy.rightStride = strides.rightStride;
                copy.blendTime = strides.blendTime;
            }
            var residentBody = resident.GetComponent<CharacterController>();
            if (capsule && residentBody)
            {
                capsule.height = residentBody.height;
                capsule.radius = residentBody.radius;
                capsule.center = residentBody.center;
                capsule.stepOffset = residentBody.stepOffset;
                capsule.slopeLimit = residentBody.slopeLimit;
            }
            islandScale = resident.transform.lossyScale;
            ApplyIslandScale();
            player.BindMarshmallowTraversal();
            resident.gameObject.SetActive(false);
        }

        void ApplyIslandScale()
        {
            if (!player || islandScale.sqrMagnitude < .01f) return;
            Vector3 parentScale = player.transform.parent ? player.transform.parent.lossyScale : Vector3.one;
            player.transform.localScale = new Vector3(
                islandScale.x / Mathf.Max(.001f, parentScale.x),
                islandScale.y / Mathf.Max(.001f, parentScale.y),
                islandScale.z / Mathf.Max(.001f, parentScale.z));
        }

        IEnumerator ThrowOntoIsland()
        {
            Transform spawn = null;
            var transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i] && transforms[i].name == "Sahur Spawn Point" && transforms[i].gameObject.scene == destination)
                { spawn = transforms[i]; break; }
            Vector3 ground = spawn ? spawn.position : player.transform.position;
            Vector3 probe = ground + Vector3.up * 40f;
            var hits = Physics.RaycastAll(probe, Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (!hit.collider || hit.collider.transform.IsChildOf(boss.transform) || hit.collider.transform.IsChildOf(player.transform)) continue;
                if (hit.distance < nearest) { nearest = hit.distance; ground = hit.point; }
            }
            Vector3 facing = spawn ? Flat(spawn.forward) : Vector3.forward;
            if (facing.sqrMagnitude < .01f) facing = Vector3.forward;
            Quaternion face = Quaternion.LookRotation(facing, Vector3.up);
            Vector3 releaseBelly = ground + Vector3.up * 36f - facing * 16f;
            boss.transform.SetPositionAndRotation(RootForBelly(releaseBelly, face), face);
            if (anchor) anchor.rotation = boss.transform.rotation;
            view = Camera.main;
            Vector3 pass = boss.transform.position + facing * 18f;
            yield return MoveBoss(boss.transform.position, pass, facing, 1.2f, -10f);

            Vector3 thrownFrom = player.transform.position;
            Quaternion thrownRot = player.transform.rotation;
            player.transform.SetParent(null, true);
            carried = false;
            Vector3 landing = ground + facing * 11f;
            if (capsule)
            {
                float sole = (capsule.center.y - capsule.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
                landing.y = ground.y - sole + .25f;
            }
            else landing.y = ground.y + 1.7f;
            Quaternion upright = Quaternion.LookRotation(facing, Vector3.up);
            Vector3 planeStart = boss.transform.position;
            Vector3 planeEnd = planeStart + facing * 74f + Vector3.up * 30f;
            float duration = 1.5f, elapsed = 0f;
            while (elapsed < duration)
            {
                if (Time.timeScale > 0f && !PauseSettingsMenu.IsOpen)
                {
                    float u = Mathf.Clamp01(elapsed / duration);
                    float drop = u * u;
                    var point = Vector3.Lerp(thrownFrom, landing, drop);
                    player.transform.SetPositionAndRotation(point, Quaternion.Slerp(thrownRot, upright, Mathf.SmoothStep(0f, 1f, u)));
                    float fly = Mathf.SmoothStep(0f, 1f, u);
                    Vector3 course = planeEnd - planeStart;
                    boss.transform.SetPositionAndRotation(Vector3.Lerp(planeStart, planeEnd, fly), Quaternion.LookRotation(course.normalized, Vector3.up));
                    Bank(20f);
                    elapsed += Time.deltaTime;
                }
                FrameCamera(player.transform.position, facing);
                yield return null;
            }
            player.transform.SetPositionAndRotation(landing, upright);
        }

        Vector3 RootForBelly(Vector3 bellyWorld, Quaternion rotation)
        {
            Vector3 offset = Vector3.Scale(bellyLocal, boss.transform.lossyScale);
            return bellyWorld - rotation * offset;
        }

        void AttachUnderBelly()
        {
            if (!anchor)
            {
                anchor = new GameObject("Grab").transform;
                anchor.SetParent(boss.transform, false);
            }
            anchor.localPosition = bellyLocal + Vector3.down * (Hang() * .62f);
            anchor.localRotation = Quaternion.identity;
            player.transform.SetParent(anchor, true);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.Euler(74f, 0f, 8f);
            carried = true;
        }

        float Hang() => capsule ? Mathf.Max(1.8f, capsule.height * Mathf.Abs(player.transform.lossyScale.y) * .55f) : 2.4f;

        IEnumerator MoveBoss(Vector3 from, Vector3 to, Vector3 facing, float seconds, float pitch)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (Time.timeScale > 0f && !PauseSettingsMenu.IsOpen)
                {
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(.01f, seconds)));
                    Vector3 point = Vector3.Lerp(from, to, u);
                    Vector3 course = to - from;
                    if (course.sqrMagnitude < .01f) course = facing;
                    boss.transform.SetPositionAndRotation(point, Quaternion.LookRotation(course.normalized, Vector3.up));
                    Bank(pitch);
                    if (anchor && carried) anchor.localRotation = Quaternion.identity;
                    elapsed += Time.deltaTime;
                }
                Vector3 look = carried && player ? player.transform.position : boss.transform.position;
                FrameCamera(look, Flat(courseOr(facing)));
                yield return null;
            }
        }

        Vector3 courseOr(Vector3 facing) => facing.sqrMagnitude > .01f ? facing : boss.transform.forward;

        void Bank(float pitch)
        {
            if (!boss.visual) return;
            boss.visual.localRotation = visualRest * Quaternion.Euler(pitch, 0f, Mathf.Sin(Time.time * 1.7f) * 7f);
        }

        void FrameCamera(Vector3 focus, Vector3 forward)
        {
            if (!view) view = Camera.main;
            if (!view) return;
            forward = Flat(forward);
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 desired = focus - forward * 16f + Vector3.up * 7.5f + right * 5.5f;
            view.transform.position = Vector3.SmoothDamp(view.transform.position, desired, ref cameraVelocity, .16f);
            Vector3 at = focus + Vector3.up * 1.1f;
            if ((at - view.transform.position).sqrMagnitude > .01f)
                view.transform.rotation = Quaternion.Slerp(view.transform.rotation, Quaternion.LookRotation(at - view.transform.position, Vector3.up), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        void Finish()
        {
            if (restored) return;
            restored = true;
            Playing = false;
            if (player)
            {
                player.transform.SetParent(null, true);
                ApplyIslandScale();
                if (destination.IsValid()) SceneManager.MoveGameObjectToScene(player.gameObject, destination);
                if (capsule) capsule.enabled = true;
                player.ExternalControlLock = false;
                player.ExternalMovementLock = false;
                player.enabled = true;
                view = view ? view : Camera.main;
                if (view) player.ContinueCameraFromCurrentView(view, cameraVelocity);
                player.Motion?.Release(this);
            }
            CinematicLetterbox.Set(this, false);
            if (boss) Destroy(boss.gameObject);
        }

        void RestoreInPlace()
        {
            if (restored) return;
            restored = true;
            Playing = false;
            if (player)
            {
                player.transform.SetParent(null, true);
                player.transform.SetPositionAndRotation(playerHome, playerHomeRotation);
                if (capsule) capsule.enabled = true;
                player.ExternalControlLock = false;
                player.ExternalMovementLock = false;
                player.enabled = true;
                player.Motion?.Release(this);
            }
            CinematicLetterbox.Set(this, false);
            if (boss) boss.ReleaseAbduction();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneArrived;
            if (!restored && player && !player.enabled)
                RestoreInPlace();
            Playing = false;
        }

        static string ScenePath()
        {
            if (Application.CanStreamedLevelBeLoaded(FlatScene)) return FlatScene;
            if (Application.CanStreamedLevelBeLoaded(NestedScene)) return NestedScene;
            return null;
        }

        static Vector3 Belly(BombardinoBoss boss)
        {
            var renderers = boss.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return boss.transform.position;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                if (renderers[i]) bounds.Encapsulate(renderers[i].bounds);
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > .0001f ? value.normalized : Vector3.zero;
        }
    }
}
