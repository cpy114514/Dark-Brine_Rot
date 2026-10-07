using System.Collections;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Drifts the unconscious survivor ashore, then asks him to recover his stick.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(500)]
public sealed class FirstIslandArrival : MonoBehaviour
{
    public ThirdPersonPlayerController player;
    public Transform driftStart, shore, lostStickPoint;
    [Min(.1f)] public float driftSeconds = 7f;
    [Min(0f)] public float unconsciousSeconds = 1.6f;
    [Min(.1f)] public float fadeSeconds = 1f;
    public AnimationClip getUpClip;
    [Min(.5f)] public float getUpSeconds = 2.3f;
    [HideInInspector] public AnimationCurve getUpBottomOffset;
    [HideInInspector] public float standingBottomOffset;
    [Min(.5f)] public float pickupRadius = 3f;
    public bool HasLanded { get; private set; }
    public bool IsAwake { get; private set; }
    public bool IsGettingUp { get; private set; }
    public bool HasStick { get; private set; } = true;
    public bool CanPickUp => IsAwake && !HasStick && !PauseSettingsMenu.IsOpen && !SahurLoadoutUI.BlocksInput &&
        player != null && lostStickPoint != null &&
        Vector3.ProjectOnPlane(player.transform.position - lostStickPoint.position, Vector3.up).sqrMagnitude <= pickupRadius * pickupRadius &&
        Mathf.Abs(player.transform.position.y - lostStickPoint.position.y) < 7f;

    SahurAttack attack;
    SahurBoomerang boomerang;
    SahurSwimmingWeapon swimmingWeapon;
    CharacterController capsule;
    Animator animator;
    Camera arrivalCamera;
    OceanWorld ocean;
    MeshCollider islandGround;
    GameObject looseStick, ui;
    Image blackout;
    Text hint;
    float recoveredAt = -1f, previousAnimatorSpeed;
    bool continuing, previousMovementEnabled, previousCapsuleEnabled;
    Behaviour[] auxiliary;
    bool[] auxiliaryEnabled;

    void Awake()
    {
        if (player == null) player = GetComponent<ThirdPersonPlayerController>();
        attack = player.GetComponent<SahurAttack>();
        boomerang = player.GetComponent<SahurBoomerang>();
        swimmingWeapon = player.GetComponent<SahurSwimmingWeapon>();
        capsule = player.GetComponent<CharacterController>();
        continuing = GameSaveManager.IsContinuePending;
        if (continuing) { HasLanded = IsAwake = true; return; }
        SetStick(false);
        previousMovementEnabled = player.enabled;
        previousCapsuleEnabled = capsule != null && capsule.enabled;
        player.ExternalControlLock = true;
        player.enabled = false;
        if (capsule != null) capsule.enabled = false;
        auxiliary = new Behaviour[] { player.GetComponent<CombatKnockback>(), player.GetComponent<EnemyLockOn>(),
            player.GetComponent<SahurLoadoutUI>(), player.GetComponent<IslandMapUI>() };
        auxiliaryEnabled = new bool[auxiliary.Length];
        for (int i = 0; i < auxiliary.Length; i++)
            if (auxiliary[i] != null) { auxiliaryEnabled[i] = auxiliary[i].enabled; auxiliary[i].enabled = false; }
    }

    IEnumerator Start()
    {
        BuildUI();
        if (continuing) yield break;
        blackout.color = Color.black;
        // The bootstrap loads Environment, Gameplay, then Lighting.
        for (int i = 0; i < 180; i++)
        {
            foreach (var candidate in FindObjectsByType<OceanWorld>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene.name == "Environment") { ocean = candidate; break; }
            foreach (var candidate in FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.gameObject.scene.name == "Environment") { islandGround = candidate.GetComponent<MeshCollider>(); break; }
            foreach (var candidate in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene == gameObject.scene && candidate.CompareTag("MainCamera")) { arrivalCamera = candidate; break; }
            if (ocean != null && arrivalCamera != null && UnityEngine.SceneManagement.SceneManager.GetSceneByName("Lighting").isLoaded) break;
            yield return null;
        }
        if (driftStart == null || shore == null || lostStickPoint == null || arrivalCamera == null || getUpClip == null ||
            getUpBottomOffset == null || getUpBottomOffset.length < 2)
        {
            Debug.LogError("First Island arrival is missing its beach, stick or camera references.", this);
            RestoreProgress(true, true); ReleaseCinematic(); blackout.color = Color.clear;
            yield break;
        }
        SpawnLooseStick();
        animator = player.CharacterAnimator;
        if (animator != null)
        {
            previousAnimatorSpeed = animator.speed; animator.speed = 0;
            animator.SetFloat("ArrivalGetUpPhase", 0); animator.Play("Shore Get Up", 0, 0); animator.Update(0);
        }
        player.GetComponent<PlayerHealth>()?.GrantProtection(driftSeconds + unconsciousSeconds + getUpSeconds + fadeSeconds * 4 + 10);
        hint.text = "你失去了意识。";
        yield return new WaitForSeconds(1f);
        hint.text = "";
        Vector3 heading = Vector3.ProjectOnPlane(shore.position - driftStart.position, Vector3.up).normalized;
        // LayToIdle lies with its head behind the actor's forward axis.
        Quaternion upright = Quaternion.LookRotation(-heading, Vector3.up);
        Vector3 landing = shore.position;
        float landingBottom = SurfaceHeight(landing) + .08f;
        float arrivalBottom = landingBottom;
        Quaternion arrivalRotation = upright;
        for (float elapsed = 0; elapsed < driftSeconds; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / driftSeconds);
            // Incoming swells push him forward; he never paddles or walks.
            float advance = Mathf.Clamp01(progress - Mathf.Sin(progress * Mathf.PI * 6f) * .012f);
            Vector3 at = Vector3.Lerp(driftStart.position, shore.position, Mathf.SmoothStep(0, 1, advance));
            float water = ocean != null ? ocean.SampleSurfaceHeight(at, Time.time) : player.seaLevel;
            float ground = SurfaceHeight(at);
            float bottom = Mathf.Max(water - .35f, ground + .08f);
            float roll = Mathf.Sin(elapsed * 1.9f) * 4f;
            player.transform.SetPositionAndRotation(at, Quaternion.AngleAxis(roll, heading) * upright);
            PlaceAnimationOnSurface(bottom, 0); FocusCamera(heading);
            blackout.color = new Color(0, 0, 0, 1 - Mathf.Clamp01(elapsed / fadeSeconds));
            // Ground contact ends the drift immediately, independently of its timer.
            if (ground >= (ocean != null ? ocean.oceanHeight : player.seaLevel) + .75f)
            {
                landing = at; landing.y = ground;
                landingBottom = ground + .08f; arrivalBottom = bottom;
                arrivalRotation = player.transform.rotation;
                break;
            }
            yield return null;
        }
        HasLanded = true;
        // Settle vertically as the wave recedes, without sliding along the sand.
        for (float elapsed = 0; elapsed < .25f; elapsed += Time.deltaTime)
        {
            float settle = Mathf.SmoothStep(0, 1, elapsed / .25f);
            player.transform.SetPositionAndRotation(landing, Quaternion.Slerp(arrivalRotation, upright, settle));
            PlaceAnimationOnSurface(Mathf.Lerp(arrivalBottom, landingBottom, settle), 0);
            FocusCamera(heading);
            yield return null;
        }
        player.transform.SetPositionAndRotation(landing, upright);
        PlaceAnimationOnSurface(landingBottom, 0); FocusCamera(heading);
        yield return new WaitForSeconds(unconsciousSeconds);
        blackout.color = Color.clear; hint.text = "海浪把你推上了岸。";
        IsGettingUp = true;
        for (float elapsed = 0; elapsed < getUpSeconds; elapsed += Time.deltaTime)
        {
            float phase = Mathf.Clamp01(elapsed / getUpSeconds);
            animator.SetFloat("ArrivalGetUpPhase", phase); animator.Update(0);
            PlaceAnimationOnSurface(landingBottom, phase);
            FocusCamera(heading);
            yield return null;
        }
        animator.SetFloat("ArrivalGetUpPhase", 1); animator.Update(0);
        PlaceAnimationOnSurface(landingBottom, 1);
        // Blend the authored standing pose into the normal idle before enabling input.
        animator.speed = previousAnimatorSpeed;
        animator.CrossFadeInFixedTime("Locomotion", .18f, 0, 0);
        for (float elapsed = 0; elapsed < .22f; elapsed += Time.deltaTime)
        {
            Vector3 position = player.transform.position;
            float blend = Mathf.Clamp01(elapsed / .18f);
            position.y = landingBottom - Mathf.Lerp(getUpBottomOffset.Evaluate(1), standingBottomOffset, blend);
            player.transform.position = position;
            FocusCamera(heading);
            yield return null;
        }
        float sole = capsule != null ? (capsule.center.y - capsule.height * .5f) * Mathf.Abs(player.transform.lossyScale.y) : 0;
        Vector3 standing = landing; standing.y = landingBottom - sole;
        // The lying pose faces out to sea. Hand control back facing inland so
        // the third-person camera shows the island and the lost-stick objective.
        Vector3 inland = islandGround != null
            ? Vector3.ProjectOnPlane(islandGround.bounds.center - standing, Vector3.up)
            : heading;
        if (inland.sqrMagnitude < .001f) inland = heading;
        Quaternion standingRotation = Quaternion.LookRotation(inland, Vector3.up);
        for (float elapsed = 0; elapsed < .55f; elapsed += Time.deltaTime)
        {
            player.transform.rotation = Quaternion.Slerp(upright, standingRotation, Mathf.SmoothStep(0, 1, elapsed / .55f));
            FocusCamera(heading);
            yield return null;
        }
        player.RestoreSavedPose(standing, standingRotation);
        IsGettingUp = false; HasLanded = IsAwake = true; ReleaseCinematic();
        hint.text = "棍子被冲走了。沿着沙滩找到棍子。";
        blackout.color = Color.clear;
    }

    void PlaceAnimationOnSurface(float bottom, float phase)
    {
        Vector3 position = player.transform.position;
        position.y = bottom - getUpBottomOffset.Evaluate(phase);
        player.transform.position = position;
    }

    float SurfaceHeight(Vector3 at)
    {
        if (islandGround == null)
            foreach (var island in FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None))
                if (island.isActiveAndEnabled && island.gameObject.scene.name == "Environment") { islandGround = island.GetComponent<MeshCollider>(); break; }
        if (islandGround != null && islandGround.Raycast(new Ray(new Vector3(at.x, islandGround.bounds.max.y + 30, at.z), Vector3.down),
            out var hit, islandGround.bounds.size.y + 60)) return hit.point.y;
        return ocean != null ? ocean.oceanHeight - 10 : shore.position.y;
    }

    void FocusCamera(Vector3 heading)
    {
        Vector3 focus = (animator.GetBoneTransform(HumanBodyBones.Head).position + animator.GetBoneTransform(HumanBodyBones.Hips).position) * .5f;
        Vector3 side = Vector3.Cross(Vector3.up, heading);
        arrivalCamera.transform.SetPositionAndRotation(focus - heading * 9f + side * 10f + Vector3.up * 8f,
            Quaternion.LookRotation(heading * 9f - side * 10f - Vector3.up * 8f));
    }

    void ReleaseCinematic()
    {
        if (animator != null) animator.speed = previousAnimatorSpeed;
        if (capsule != null) capsule.enabled = previousCapsuleEnabled;
        player.ExternalControlLock = false; player.enabled = previousMovementEnabled;
        if (auxiliary != null)
            for (int i = 0; i < auxiliary.Length; i++) if (auxiliary[i] != null) auxiliary[i].enabled = auxiliaryEnabled[i];
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }

    void Update()
    {
        if (!IsAwake || hint == null) return;
        hint.text = HasStick ? (recoveredAt >= 0 && Time.time - recoveredAt < 3 ? "找回了棍子。" : "") :
            CanPickUp ? "按 F 拾起棍子" : "棍子被冲走了。沿着沙滩找到棍子。";
        if (CanPickUp && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked) TryPickUp();
    }

    public bool TryPickUp()
    {
        if (!CanPickUp) return false;
        var health = player.GetComponent<PlayerHealth>();
        if (health != null && health.currentHealth <= 0) return false;
        SetStick(true); recoveredAt = Time.time;
        if (looseStick != null) { looseStick.SetActive(false); Destroy(looseStick); looseStick = null; }
        return true;
    }

    public void RestoreProgress(bool landed, bool hasStick)
    {
        HasLanded = IsAwake = landed; SetStick(hasStick);
        if (hasStick && looseStick != null) { Destroy(looseStick); looseStick = null; }
        else if (!hasStick) SpawnLooseStick();
    }

    void SetStick(bool available)
    {
        HasStick = available;
        if (attack != null) { attack.SuspendForSwimming(); attack.enabled = available; }
        if (boomerang != null) boomerang.enabled = available;
        if (swimmingWeapon != null && swimmingWeapon.stick != null) swimmingWeapon.stick.gameObject.SetActive(available);
    }

    void SpawnLooseStick()
    {
        if (looseStick != null || HasStick || lostStickPoint == null || swimmingWeapon == null || swimmingWeapon.stick == null) return;
        Transform source = swimmingWeapon.stick;
        var sourceMesh = source.GetComponent<MeshFilter>(); var sourceRenderer = source.GetComponent<MeshRenderer>();
        if (sourceMesh == null || sourceRenderer == null) return;
        looseStick = new GameObject("Lost Sahur Stick", typeof(MeshFilter), typeof(MeshRenderer));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(looseStick, gameObject.scene);
        looseStick.GetComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;
        looseStick.GetComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;
        looseStick.transform.localScale = source.lossyScale;
        Vector3 axis = source.up;
        if (attack != null && attack.stickHitbox is CapsuleCollider hitbox)
            axis = hitbox.transform.TransformDirection(hitbox.direction == 0 ? Vector3.right : hitbox.direction == 1 ? Vector3.up : Vector3.forward);
        looseStick.transform.rotation = Quaternion.FromToRotation(axis, lostStickPoint.right) * source.rotation;
        looseStick.transform.position = lostStickPoint.position - looseStick.transform.TransformVector(sourceMesh.sharedMesh.bounds.center);
        var renderer = looseStick.GetComponent<Renderer>();
        looseStick.transform.position += Vector3.up * (lostStickPoint.position.y + .07f - renderer.bounds.min.y);
    }

    void BuildUI()
    {
        ui = new GameObject("First Island Arrival Text", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ui, gameObject.scene);
        ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; ui.GetComponent<Canvas>().sortingOrder = 900;
        var scaler = ui.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var screen = new GameObject("Blackout", typeof(RectTransform), typeof(Image)); screen.transform.SetParent(ui.transform, false);
        var rect = (RectTransform)screen.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        blackout = screen.GetComponent<Image>(); blackout.color = Color.clear; blackout.raycastTarget = false;
        var label = new GameObject("Find the stick", typeof(RectTransform), typeof(Text), typeof(Outline)); label.transform.SetParent(ui.transform, false);
        rect = (RectTransform)label.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .12f); rect.sizeDelta = new Vector2(1100, 80);
        hint = label.GetComponent<Text>(); hint.font = GameLocalization.Font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 28; hint.color = Color.white; hint.alignment = TextAnchor.MiddleCenter; hint.raycastTarget = false;
        label.GetComponent<Outline>().effectColor = Color.black; label.GetComponent<Outline>().effectDistance = new Vector2(1.5f, -1.5f);
        LocalizedGameText.Bind(hint);
    }

    void OnDestroy() { if (looseStick != null) Destroy(looseStick); if (ui != null) Destroy(ui); }
}
