using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;

/// <summary>The two finish rewards and Sahur's temporary, controllable skyboard.</summary>
[DefaultExecutionOrder(250), DisallowMultipleComponent]
public sealed class SkyboardReward : MonoBehaviour, ICooldownTarget
{
    public GameObject skateboardModel, scrollModel;
    public Material skateboardMaterial;
    public AnimationClip skateboardingClip;
    public const string BoardItemPath = "Equipment/Tools/Skyboard";
    public const string ScrollItemPath = "Equipment/Tools/SpeedScroll";
    public float flightSeconds = 60f, cooldownSeconds = 900f, scrollReduction = 1200f;
    public float horizontalSpeed = 16f, verticalSpeed = 9f;
    [Min(0)] public float heightAboveIsland = 10f;
    public bool Owned => inventory && boardItem && inventory.Count(boardItem) > 0;
    public int Scrolls => inventory && scrollItem ? inventory.Count(scrollItem) : 0;
    public bool Flying { get; private set; }
    public bool Landing { get; private set; }
    public bool RallyFlight { get; private set; }
    public bool RallySteeringLocked { get; set; }
    float rallyCeiling, savedRallyCooldown;
    public float FlightRemaining { get; private set; }
    public float CooldownRemaining { get; private set; }
    public bool UnlimitedTestFlight
    {
        get
        {
#if UNITY_EDITOR
            return SecondIslandTestStation.UnlimitedSkyboard;
#else
            return false;
#endif
        }
    }
#if UNITY_EDITOR
    public void ClearTestCooldown(){CooldownRemaining=0;if(boardReward && Owned)boardReward.SetActive(false);}
#endif
    public int CollectedScrolls { get; private set; }
    public void RestoreProgress(bool collectedScrolls, float cooldown)
    {
        scrollsCollected = collectedScrolls;
        CollectedScrolls = collectedScrolls ? 10 : 0;
        CooldownRemaining = float.IsNaN(cooldown) || float.IsInfinity(cooldown) ? 0 : Mathf.Clamp(cooldown, 0, cooldownSeconds);
        if (scrollReward) scrollReward.SetActive(!scrollsCollected);
        if (boardReward) boardReward.SetActive(!Owned);
    }
    public bool RestoreFlight(float remaining)
    {
        if(float.IsNaN(remaining) || float.IsInfinity(remaining) || remaining<=0 || !TryStartFlight())return false;
        FlightRemaining=Mathf.Clamp(remaining,.01f,flightSeconds);
        return true;
    }
    public float CeilingHeight => RallyFlight ? rallyCeiling : UnlimitedTestFlight ? float.PositiveInfinity : islandSurface ? islandSurface.bounds.max.y + heightAboveIsland : float.PositiveInfinity;
    public float DeckHeight { get; private set; }
    public string CooldownDisplayName => "Skyboard";
    public bool CooldownTargetOwned => Owned;
    public bool CanReduceCooldown => Owned && !Flying && !Landing && CooldownRemaining > 0;
    public bool ReduceCooldown(float seconds)
    {
        if (seconds <= 0 || !CanReduceCooldown) return false;
        CooldownRemaining = Mathf.Max(0, CooldownRemaining - seconds); return true;
    }

    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    EquipmentInventory inventory;
    EquipmentItem boardItem, scrollItem;
    BoxCollider boardPlatform, scrollPlatform;
    GameObject boardReward, scrollReward, riddenBoard;
    Material thrusterMaterial, candyBoardMaterial;
    Texture2D candyBoardTexture;
    Renderer[] riddenRenderers;
    Renderer islandSurface;
    Collider islandCollider;
    readonly List<Collider> courseColliders = new List<Collider>();
    Bounds courseRegion;
    GameObject boardProbeObject;
    BoxCollider boardProbe;
    Vector3 boardProbeCenter;
    bool scrollsCollected;
    Animator riderAnimator;
    PlayableGraph riderGraph;
    AnimationClipPlayable riderPose;
    float riderElapsed;
    float flightStartedAt;
    Quaternion riderBaseRotation;
    bool previousRootMotion;
    Transform leftFoot, rightFoot;
    Vector3 leftSole, rightSole;
    bool soleOffsetsReady;
    SahurWeaponLoadout weaponLoadout;
    SahurSwimmingWeapon stickStow;
    GameObject flightHud;
    Text flightHudTitle, flightHudControls;

    void Start()
    {
        player = FindFirstObjectByType<ThirdPersonPlayerController>();
        if (!player || !skateboardModel || !scrollModel) { enabled = false; return; }
        body = player.GetComponent<CharacterController>();
        health = player.GetComponent<PlayerHealth>();
        inventory = player.GetComponent<EquipmentInventory>();
        boardItem = Resources.Load<EquipmentItem>(BoardItemPath);
        scrollItem = Resources.Load<EquipmentItem>(ScrollItemPath);
        if (!inventory || !boardItem || !scrollItem) { enabled = false; return; }
        islandSurface = player.GetComponent<MarshmallowIslandExperience>()?.surface;
        islandCollider = islandSurface ? islandSurface.GetComponent<Collider>() : null;
        Transform last = null;
        foreach (Transform child in transform) if (child.name.StartsWith("Row ")) last = child;
        if (!body || !last || last.childCount < 2) { enabled = false; return; }
        boardPlatform = last.GetChild(0).GetComponentInChildren<BoxCollider>();
        scrollPlatform = last.GetChild(1).GetComponentInChildren<BoxCollider>();
        if (!boardPlatform || !scrollPlatform) { enabled = false; return; }
        foreach (Transform row in transform)
            if (row.name.StartsWith("Row "))
                foreach (Transform platform in row)
                    foreach (var collider in platform.GetComponentsInChildren<Collider>(true))
                        if (collider is BoxCollider || collider is CapsuleCollider) courseColliders.Add(collider);
        if(courseColliders.Count>0)
        {
            courseRegion=courseColliders[0].bounds;
            foreach(var collider in courseColliders)courseRegion.Encapsulate(collider.bounds);
            courseRegion.Expand(24f);
        }
        boardReward = new GameObject("Skyboard pickup");
        var board = MakeVisual(skateboardModel, boardReward.transform, 2.6f, skateboardMaterial);
        CenterVisual(board, Vector3.zero);
        var boardPickupCollider=boardReward.AddComponent<BoxCollider>();
        boardPickupCollider.isTrigger=true;boardPickupCollider.size=new Vector3(2.8f,1.1f,2.8f);
        Label(boardReward.transform, "Skyboard\nWalk up to collect");
        scrollReward = new GameObject("Ten speed scrolls");
        for (int i = 0; i < 10; i++)
        {
            var visual = MakeVisual(scrollModel, scrollReward.transform, .55f, null);
            CenterVisual(visual, new Vector3((i % 5 - 2) * .48f, (i % 2) * .08f, (i / 5 - .5f) * .65f));
        }
        var scrollPickupCollider=scrollReward.AddComponent<BoxCollider>();
        scrollPickupCollider.isTrigger=true;scrollPickupCollider.size=new Vector3(3.3f,1.1f,1.8f);
        Label(scrollReward.transform, "10 Speed Scrolls\nEach removes 20 minutes of cooldown");
        if (Owned) boardReward.SetActive(false);
    }

    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
    internal static GameObject MakeVisual(GameObject model, Transform parent, float maxSize, Material material)
    {
        var go = Instantiate(model, parent); go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        if (material) foreach (var renderer in go.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
        var b = BoundsOf(go); float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
        go.transform.localScale *= maxSize / Mathf.Max(.001f, size);
        return go;
    }
    internal static void CenterVisual(GameObject go, Vector3 desiredLocalCenter)
    {
        var b = BoundsOf(go);
        go.transform.position += go.transform.parent.TransformVector(desiredLocalCenter) - (b.center - go.transform.parent.position);
    }
    internal static void Label(Transform parent, string value)
    {
        var go = new GameObject("Pickup label", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.up * 2.3f;
        go.transform.localScale = Vector3.one * .012f;
        var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(550, 120);
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var t = new GameObject("Text", typeof(RectTransform), typeof(Text)); t.transform.SetParent(go.transform, false);
        var tr = (RectTransform)t.transform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
        var label = t.GetComponent<Text>(); label.font = GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = value; label.fontSize = 26; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
        var outline = t.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(2, -2);
        SecondIslandBlackboards.Attach(go);
    }
    bool InputAllowed => player && body && body.enabled && player.enabled && !player.ExternalControlLock &&
        !(health && health.currentHealth <= 0) && !PlayerDeathRespawn.IsOpen && !GameUITheme.ModalOpen &&
        !PauseSettingsMenu.IsOpen && Time.timeScale > 0 && Cursor.lockState == CursorLockMode.Locked;
    void Update()
    {
        if (!player || !body || !boardPlatform || !scrollPlatform) return;
        PositionReward(boardReward, boardPlatform); PositionReward(scrollReward, scrollPlatform);
        if ((Flying || Landing) && (health && health.currentHealth <= 0 || PlayerDeathRespawn.IsOpen || !body.enabled)) EndFlight();
        RefreshFlightHud();
        if (Time.timeScale <= 0 || PauseSettingsMenu.IsOpen) return;
        if (!Flying && !Landing) CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);
        if (InputAllowed && !Flying && !Landing && !player.ExternalMovementLock)
        {
            if (!Owned && Near(boardPlatform) && inventory.Collect(boardItem)) { boardReward.SetActive(false); GameSaveManager.RequestAutosave(); }
            if (!scrollsCollected && Near(scrollPlatform))
            {
                scrollsCollected = true; CollectedScrolls = 10;
                inventory.RestoreStack(scrollItem, Scrolls + 10); scrollReward.SetActive(false);
                GameSaveManager.RequestAutosave();
                GameplayAudio.Play(GameplayAudio.Cue.Pickup, player.transform.position, true);
            }
            if (!CandyHuntQuest.InteractionConsumedThisFrame && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                var residents=FindFirstObjectByType<FriendlyMarshmallowVillage>();
                var melody=FindFirstObjectByType<CandyMelodyGarden>();
                var bounce=FindFirstObjectByType<CandyBounceGarden>();
                var friends=FindFirstObjectByType<MarshmallowFriendsQuest>();
                bool reserved=(!residents || !residents.ReservesInteraction) && (!melody || !melody.ReservesInteraction) && (!bounce || !bounce.ReservesInteraction) && (!friends || !friends.ReservesInteraction);
                reserved=reserved && !SkyboardRingRally.ReservesInteractionFor(player) &&
                    !SecondIslandRoninCamp.ReservesInteractionFor(player) && !DriftingPathShortcut.ReservesInteractionFor(player);
#if UNITY_EDITOR
                reserved=reserved && !SecondIslandTestStation.ReservesInteraction;
#endif
                if(reserved)TryStartFlight();
            }
        }
        if (Flying && InputAllowed && !RallyFlight)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && (UnlimitedTestFlight ? Time.time-flightStartedAt>.25f : FlightRemaining < flightSeconds - .25f)) EndFlight();
            else if(!UnlimitedTestFlight)
            {
                FlightRemaining = Mathf.Max(0, FlightRemaining - Time.deltaTime);
                if (FlightRemaining <= 0) EndFlight();
            }
        }
        if (Flying && InputAllowed && !RallySteeringLocked)
        {
            var move = player.RequestedMoveDirection * horizontalSpeed;
            float up = (GameInputSettings.Pressed(GameInputSettings.Action.Jump) ? 1 : 0) - (GameInputSettings.Pressed(GameInputSettings.Action.Dodge) ? 1 : 0);
            move.y = up * verticalSpeed;
            if (move.y > 0 && islandSurface)
                move.y = Mathf.Min(move.y, Mathf.Max(0, CeilingHeight - DeckHeight) / Mathf.Max(.0001f, Time.deltaTime));
            body.Move(move * Time.deltaTime);
            Vector3 horizontal = Vector3.ProjectOnPlane(move, Vector3.up);
            if (horizontal.sqrMagnitude > .01f) player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, Quaternion.LookRotation(horizontal), 540f * Time.deltaTime);
        }
        RefreshFlightHud();
    }
    void BuildFlightHud()
    {
        if (flightHud) return;
        flightHud = new GameObject("Skyboard flight controls", typeof(RectTransform), typeof(Canvas));
        var canvas = flightHud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 26;
        GameUITheme.Scale(canvas);
        var card = GameUITheme.Rect(flightHud.transform, "Controls card", new Vector2(1, 0), new Vector2(430, 172));
        card.pivot = new Vector2(1, 0); card.anchoredPosition = new Vector2(-28, 135);
        GameUITheme.Image(card, new Color(.035f, .035f, .035f, .88f));
        flightHudTitle = GameUITheme.Label(card, "Flight time", 24, new Vector2(-195, -23), new Vector2(390, 32), TextAnchor.MiddleLeft);
        flightHudControls = GameUITheme.Label(card, "Controls", 20, new Vector2(-195, -100), new Vector2(390, 122), TextAnchor.MiddleLeft);
        flightHudControls.lineSpacing = 1.13f;
    }
    void RefreshFlightHud()
    {
        if (!flightHud) return;
        bool visible = Flying && InputAllowed;
        if (flightHud.activeSelf != visible) flightHud.SetActive(visible);
        if (!visible) return;
        flightHudTitle.text = RallyFlight ? "SKYBOARD  ·  RACE MODE" : UnlimitedTestFlight ? "SKYBOARD  ·  TEST: UNLIMITED" : "SKYBOARD  ·  " + Mathf.CeilToInt(FlightRemaining) + "s";
        flightHudControls.text = $"{GameInputSettings.Get(GameInputSettings.Action.Forward)} / {GameInputSettings.Get(GameInputSettings.Action.Left)} / {GameInputSettings.Get(GameInputSettings.Action.Back)} / {GameInputSettings.Get(GameInputSettings.Action.Right)}   Move\n" +
            $"{GameInputSettings.Get(GameInputSettings.Action.Jump)}   Ascend\n{GameInputSettings.Get(GameInputSettings.Action.Dodge)}   Descend\n"+(RallyFlight ? "R   Restart    F   Exit race" : "F   Put away");
    }
    void PositionReward(GameObject reward, BoxCollider platform)
    {
        if (!reward || !reward.activeSelf) return;
        Bounds b = platform.bounds; reward.transform.position = new Vector3(b.center.x, b.max.y + .65f + Mathf.Sin(Time.time * 1.8f) * .12f, b.center.z);
        var c = Camera.main; var canvas = reward.GetComponentInChildren<Canvas>(); if (c && canvas) canvas.transform.rotation = c.transform.rotation;
    }
    bool Near(BoxCollider platform)
    {
        var b = platform.bounds; var p = body.bounds.center; float feet = body.bounds.min.y;
        return Mathf.Abs(feet - b.max.y) < 2.2f && Vector2.Distance(new Vector2(p.x, p.z), new Vector2(b.center.x, b.center.z)) < 2.2f;
    }
    public bool TryStartFlight() => TryStartFlight(false);

    public bool TryStartRallyFlight(float ceiling)
    {
        if (!Owned || RallyFlight || Landing || !player || !body || !body.enabled) return false;
        bool arrivedFlying = Flying;
        savedRallyCooldown = arrivedFlying && !UnlimitedTestFlight ? cooldownSeconds : CooldownRemaining;
        rallyCeiling = ceiling;
        RallyFlight = true;
        if (arrivedFlying || TryStartFlight(true)) return true;
        RallyFlight = false;
        return false;
    }

    public void StopRallyFlight()
    {
        if (RallyFlight) EndFlight();
    }

    // Align the altitude clamp immediately after an in-race teleport.
    public void RefreshRallyPosition()
    {
        if (RallyFlight && body) DeckHeight = body.bounds.min.y;
    }

    bool TryStartFlight(bool forRally)
    {
        if (MarshmallowHollowEntrance.Contains(player)) return false;
        var village=FindFirstObjectByType<FriendlyMarshmallowVillage>();
        if(village && village.InGrove && !UnlimitedTestFlight)return false;
        if (!Owned || Flying || Landing || !forRally && CooldownRemaining > 0 && !UnlimitedTestFlight || !player || !body || !body.enabled || !islandSurface || player.ExternalMovementLock) return false;
        Flying = true; FlightRemaining = flightSeconds; flightStartedAt=Time.time; player.ExternalMovementLock = true;
        weaponLoadout = player.GetComponent<SahurWeaponLoadout>();
        weaponLoadout?.SetSkyboardStowed(true);
        stickStow = player.GetComponent<SahurSwimmingWeapon>();
        if (!stickStow) stickStow = player.gameObject.AddComponent<SahurSwimmingWeapon>();
        stickStow.SkyboardStow = !weaponLoadout || (weaponLoadout.CurrentWeapon == SahurWeaponLoadout.Weapon.Stick && weaponLoadout.HasStick);
        BuildFlightHud();
        RefreshFlightHud();
        player.GetComponent<MarshmallowTraversal>()?.CancelSpacewalk();
        GetComponent<MarshmallowHopRecovery>()?.ClearTracking();
        riddenBoard = new GameObject("Sahur skyboard");
        var visual = MakeVisual(skateboardModel, riddenBoard.transform, 3.4f, skateboardMaterial);
        var candyHunt=FindFirstObjectByType<CandyHuntQuest>();
        if(candyHunt && candyHunt.RewardClaimed && candyHunt.SelectedStyle<CandySkyboardStyles.Names.Length-1 && skateboardMaterial)
        {
            candyBoardMaterial=new Material(skateboardMaterial) { name="Candy-colour Skyboard" };
            candyBoardTexture=CandySkyboardStyles.CreateTexture(candyHunt.SelectedStyle);
            if(candyBoardMaterial.HasProperty("_BaseMap"))candyBoardMaterial.SetTexture("_BaseMap",candyBoardTexture);
            if(candyBoardMaterial.HasProperty("_MainTex"))candyBoardMaterial.SetTexture("_MainTex",candyBoardTexture);
            if(candyBoardMaterial.HasProperty("_BaseColor"))candyBoardMaterial.SetColor("_BaseColor",Color.white);
            else candyBoardMaterial.color=Color.white;
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=candyBoardMaterial;
        }
        // Give both soles enough room across the imported narrow deck.
        visual.transform.localScale = Vector3.Scale(visual.transform.localScale, new Vector3(1, 1, 1.6f));
        CenterVisual(visual, Vector3.zero);
        BuildThrusters();
        riddenRenderers = riddenBoard.GetComponentsInChildren<Renderer>();
        BuildBoardProbe();
        DeckHeight = body.bounds.min.y;
        StartRiderAnimation();
        if (!forRally) GameSaveManager.RequestAutosave();
        return true;
    }
    void StartRiderAnimation()
    {
        riderAnimator = player.CharacterAnimator;
        if (!riderAnimator) return;
        riderBaseRotation = riderAnimator.transform.localRotation;
        previousRootMotion = riderAnimator.applyRootMotion;
        riderAnimator.applyRootMotion = false;
        soleOffsetsReady = false;
        leftFoot = riderAnimator.isHuman ? riderAnimator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
        rightFoot = riderAnimator.isHuman ? riderAnimator.GetBoneTransform(HumanBodyBones.RightFoot) : null;
        if (!skateboardingClip || !riderAnimator.isHuman || !skateboardingClip.isHumanMotion) return;
        riderGraph = PlayableGraph.Create("Sahur skateboarding");
        // Evaluate after the normal Animator update so locomotion cannot overwrite
        // the mounted pose before the board is fitted to the character's feet.
        riderGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        riderPose = AnimationClipPlayable.Create(riderGraph, skateboardingClip);
        riderPose.SetSpeed(0); riderPose.SetApplyFootIK(false); riderPose.SetApplyPlayableIK(false);
        AnimationPlayableOutput.Create(riderGraph, "Skateboarding", riderAnimator).SetSourcePlayable(riderPose);
        riderElapsed = 0;
        riderGraph.Play();
    }
    void StopRiderAnimation()
    {
        if (riderGraph.IsValid()) riderGraph.Destroy();
        if (riderAnimator) { riderAnimator.applyRootMotion = previousRootMotion; riderAnimator.transform.localRotation = riderBaseRotation; }
        riderAnimator = null;
        leftFoot = rightFoot = null; soleOffsetsReady = false;
    }
    void BuildThrusters()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) return;
        thrusterMaterial = new Material(shader); thrusterMaterial.SetColor("_BaseColor", new Color(.25f, .9f, 1f));
        var b = BoundsOf(riddenBoard);
        for (int n = -1; n <= 1; n += 2)
        {
            var obj = new GameObject("Hover glow", typeof(LineRenderer)); obj.transform.SetParent(riddenBoard.transform, false);
            obj.transform.localPosition = new Vector3(n * b.extents.x * .62f, b.min.y - riddenBoard.transform.position.y - .06f, 0);
            var line = obj.GetComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true; line.positionCount = 24;
            line.widthMultiplier = .045f; line.sharedMaterial = thrusterMaterial; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < line.positionCount; i++) { float a = i * Mathf.PI * 2 / line.positionCount; line.SetPosition(i, new Vector3(Mathf.Cos(a) * .22f, 0, Mathf.Sin(a) * .22f)); }
        }
    }
    public bool TryUseScrollOn(ICooldownTarget target)
    {
        return SpeedScroll.TryUse(inventory, target, scrollReduction);
    }
    public bool CollectSpawnTestKit()
    {
        if (!inventory || !boardItem || !scrollItem) return false;
        if (!Owned && !inventory.Collect(boardItem)) return false;
        if (boardReward) boardReward.SetActive(false);
        inventory.RestoreStack(scrollItem, Scrolls + 10);
        GameplayAudio.Play(GameplayAudio.Cue.Pickup, player.transform.position, true);
        return true;
    }
    void EndFlight()
    {
        if (RallyFlight) CooldownRemaining = savedRallyCooldown;
        else if (Flying) CooldownRemaining = UnlimitedTestFlight ? 0 : cooldownSeconds;
        RallyFlight = RallySteeringLocked = false;
        Flying = Landing = false; FlightRemaining = 0;
        if (player) player.ExternalMovementLock = false;
        StopRiderAnimation();
        RestoreWeapons();
        GameSaveManager.RequestAutosave();
        RefreshFlightHud();
        if (riddenBoard) Destroy(riddenBoard);
        if (thrusterMaterial) Destroy(thrusterMaterial);
        if (candyBoardMaterial) Destroy(candyBoardMaterial); if(candyBoardTexture) Destroy(candyBoardTexture);
        if (boardProbeObject) Destroy(boardProbeObject);
        boardProbe = null;
        riddenRenderers = null;
    }
    void LateUpdate()
    {
        if (!riddenBoard || !body || !player || riddenRenderers == null || riddenRenderers.Length == 0) return;
        if (riderGraph.IsValid() && skateboardingClip && skateboardingClip.length > 0)
        {
            riderElapsed += Time.deltaTime;
            riderPose.SetTime(Mathf.Repeat(riderElapsed, skateboardingClip.length));
            riderGraph.Evaluate(0);
        }
        // The board travels lengthwise; the rider stands sideways across it.
        riddenBoard.transform.rotation = player.transform.rotation * Quaternion.Euler(0, 90, 0);
        if (riderAnimator) riderAnimator.transform.localRotation = riderBaseRotation * Quaternion.Euler(0, 90, 0);
        Vector3 center = body.bounds.center;
        DeckHeight = body.bounds.min.y - .03f;
        if (riderAnimator && leftFoot && rightFoot)
        {
            if (!soleOffsetsReady)
            {
                SahurSupportPose.Prepare(riderAnimator);
                SahurSupportPose.MeasureSoles(riderAnimator, ref leftSole, ref rightSole);
                soleOffsetsReady = true;
            }
            Vector3 left = leftFoot.TransformPoint(leftSole), right = rightFoot.TransformPoint(rightSole);
            center = (left + right) * .5f;
            DeckHeight = Mathf.Min(left.y, right.y) - .025f;
            if (Flying && islandSurface && DeckHeight > CeilingHeight)
            {
                float before = player.transform.position.y;
                body.Move(Vector3.down * (DeckHeight - CeilingHeight));
                float moved = player.transform.position.y - before;
                DeckHeight += moved; center.y += moved; left.y += moved; right.y += moved;
            }
        }
        var b = riddenRenderers[0].bounds;
        for (int i = 1; i < riddenRenderers.Length; i++) b.Encapsulate(riddenRenderers[i].bounds);
        riddenBoard.transform.position += new Vector3(center.x - b.center.x, DeckHeight - b.max.y, center.z - b.center.z);
        if (!RallyFlight)
        {
            KeepBoardAbovePlatforms();
            ResolveBoardPlatformIntersections();
            KeepBoardAboveIsland();
        }
        if (stickStow && stickStow.SkyboardStow) stickStow.RefreshPose();
    }
    void KeepBoardAbovePlatforms()
    {
        if(!Flying || !body || !body.enabled || !riddenBoard || riddenRenderers==null || riddenRenderers.Length==0)return;
        var boardBounds=BoardBounds();
        if(courseColliders.Count==0 || !courseRegion.Intersects(boardBounds))return;
        float lift=0f;
        foreach(var other in courseColliders)
        {
            if(!(other is BoxCollider) || !other.enabled || !other.gameObject.activeInHierarchy)continue;
            var top=other.bounds;
            if(boardBounds.max.x<top.min.x || boardBounds.min.x>top.max.x ||
               boardBounds.max.z<top.min.z || boardBounds.min.z>top.max.z)continue;
            // If the rider is approaching from above, keep the board on top.
            // A rider below the marshmallow is instead pushed out at its side.
            if(body.bounds.min.y<top.max.y-.5f || boardBounds.min.y>=top.max.y+.06f)continue;
            var marshmallow=other.GetComponentInParent<CapsuleCollider>();
            if(marshmallow && boardBounds.max.y<marshmallow.bounds.min.y)continue;
            lift=Mathf.Max(lift,top.max.y+.06f-boardBounds.min.y);
        }
        if(lift<=.001f)return;
        float before=player.transform.position.y;
        body.Move(Vector3.up*lift);
        float moved=player.transform.position.y-before;
        if(moved>0){riddenBoard.transform.position+=Vector3.up*moved;DeckHeight+=moved;}
    }
    void BuildBoardProbe()
    {
        // Keep the enabled query shape away from the level. ComputePenetration
        // accepts an explicit pose, so the probe cannot trigger game objects.
        var bounds=BoardBounds();
        boardProbeCenter=riddenBoard.transform.InverseTransformPoint(bounds.center);
        boardProbeObject=new GameObject("Skyboard collision query");
        boardProbeObject.transform.position=new Vector3(50000,-50000,50000);
        boardProbeObject.layer=2;
        boardProbe=boardProbeObject.AddComponent<BoxCollider>();
        boardProbe.isTrigger=true;
        boardProbe.size=Vector3.Max(bounds.size-Vector3.one*.03f,Vector3.one*.05f);
    }
    void ResolveBoardPlatformIntersections()
    {
        if(!Flying || !boardProbe || !body || !body.enabled || !riddenBoard || riddenRenderers==null || riddenRenderers.Length==0)return;
        if(courseColliders.Count==0 || !courseRegion.Intersects(BoardBounds()))return;
        for(int pass=0;pass<3;pass++)
        {
            bool corrected=false;
            var bounds=BoardBounds();bounds.Expand(.1f);
            foreach(var other in courseColliders)
            {
                if(!other || !other.enabled || !other.gameObject.activeInHierarchy || !bounds.Intersects(other.bounds))continue;
                Vector3 probePosition=riddenBoard.transform.TransformPoint(boardProbeCenter);
                if(!Physics.ComputePenetration(boardProbe,probePosition,riddenBoard.transform.rotation,
                    other,other.transform.position,other.transform.rotation,out var direction,out var distance) || distance<.001f)continue;
                Vector3 before=player.transform.position;
                body.Move(direction*(distance+.025f));
                Vector3 moved=player.transform.position-before;
                if(moved.sqrMagnitude<.000001f)continue;
                riddenBoard.transform.position+=moved;DeckHeight+=moved.y;
                bounds=BoardBounds();bounds.Expand(.1f);
                corrected=true;
            }
            if(!corrected)break;
        }
    }
    Bounds BoardBounds()
    {
        var bounds=riddenRenderers[0].bounds;
        for(int i=1;i<riddenRenderers.Length;i++)if(riddenRenderers[i])bounds.Encapsulate(riddenRenderers[i].bounds);
        return bounds;
    }
    void KeepBoardAboveIsland()
    {
        if (!Flying || !islandCollider || !islandCollider.enabled || !body.enabled) return;
        var bounds = riddenRenderers[0].bounds;
        for (int i = 1; i < riddenRenderers.Length; i++) bounds.Encapsulate(riddenRenderers[i].bounds);
        float lowest = bounds.min.y;
        float requiredLift = 0;
        // The capsule does not include the board. Probe its full footprint,
        // including the ends, so the deck cannot cut into uneven slopes.
        for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                Vector3 point = riddenBoard.transform.position + riddenBoard.transform.right * (x * 1.4f) + riddenBoard.transform.forward * (z * .5f);
                point.y = DeckHeight + 4f;
                if (!islandCollider.Raycast(new Ray(point, Vector3.down), out var hit, 7f) || hit.normal.y < .4f || hit.point.y > DeckHeight + 2.5f) continue;
                requiredLift = Mathf.Max(requiredLift, hit.point.y + .07f - lowest);
            }
        if (requiredLift <= .001f) return;
        float before = player.transform.position.y;
        body.Move(Vector3.up * requiredLift);
        float moved = player.transform.position.y - before;
        if (moved > 0) { riddenBoard.transform.position += Vector3.up * moved; DeckHeight += moved; }
    }
    void OnDestroy()
    {
        if ((Flying || Landing) && player) player.ExternalMovementLock = false;
        StopRiderAnimation();
        RestoreWeapons();
        if (flightHud) Destroy(flightHud);
        if (riddenBoard) Destroy(riddenBoard); if (thrusterMaterial) Destroy(thrusterMaterial); if (candyBoardMaterial) Destroy(candyBoardMaterial); if(candyBoardTexture) Destroy(candyBoardTexture); if (boardProbeObject) Destroy(boardProbeObject); if (boardReward) Destroy(boardReward); if (scrollReward) Destroy(scrollReward);
    }
    void RestoreWeapons()
    {
        if (stickStow) { stickStow.SkyboardStow = false; stickStow.RefreshPose(); }
        if (weaponLoadout) weaponLoadout.SetSkyboardStowed(false);
    }
}
