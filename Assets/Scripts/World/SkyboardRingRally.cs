using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>The island entrance and the ordered, replayable Skyboard time trial.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(450)]
public sealed partial class SkyboardRingRally : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/Second Island/Second Island.unity";
    const float RingRadius = 6.5f, RelocateAfter = 15f, RelocateSeconds = .8f, RaceLimit = 90f;
    static SkyboardRingRally entrance, activeRace;

    sealed class Ring
    {
        public Transform root;
        public Transform label;
        public Vector3 anchor;
        public bool boost;
    }

    readonly List<Ring> rings = new List<Ring>();
    readonly List<Material> materials = new List<Material>();
    Mesh ringMesh;
    ThirdPersonPlayerController player;
    CharacterController body;
    SkyboardReward board;
    EquipmentInventory inventory;
    EquipmentItem scroll;
    Collider island;
    Transform portal;
    GameObject sign, hudCard, countdownCard;
    Text status, detail, countdown;
    Camera raceCamera;
    CameraClearFlags savedClearFlags;
    Color savedBackground;
    float savedFarClip;
    Vector3 returnPosition, previous, lastCleared, moveFrom, moveTo;
    Quaternion returnRotation;
    bool hasPrevious, boostActive;
    float nextLookup, startedAt, boostUntil, baseSpeed, messageUntil, lastTime, targetDeadline, moveStarted, nextHudRefresh, penalty, exitReadyAt, recoveryUntil;
    string resultText = "";

    public bool InRace => activeRace == this;
    public bool Running { get; private set; }
    public bool RewardClaimed { get; private set; }
    public float BestTime { get; private set; }
    public int NextRing { get; private set; }
    public int TotalRings => rings.Count;
    public Transform StartMarker => sign ? sign.transform : null;
    public Transform EntranceRing => portal;
    public Transform RingTransform(int index) => index >= 0 && index < rings.Count ? rings[index].root : null;
    public float Elapsed => InRace ? Mathf.Max(0, Time.time - startedAt) + penalty : lastTime;
    public bool Relocating { get; private set; }
    public float TimeUntilMove => Mathf.Max(0, targetDeadline - Time.time);

    public static bool IsRacing(ThirdPersonPlayerController rider) => activeRace && activeRace.player == rider;
    public static bool UsesCircuitCamera(Camera camera) => activeRace && activeRace.raceCamera == camera;
    public static bool ReservesInteractionFor(ThirdPersonPlayerController rider) => entrance && entrance.player == rider &&
        !entrance.InRace && entrance.CanInteract() && entrance.NearSign(9f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        entrance = activeRace = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitialScene() { for (int i = 0; i < SceneManager.sceneCount; i++) Ensure(SceneManager.GetSceneAt(i)); }
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure(scene);
    static void Ensure(Scene scene)
    {
        if (!scene.isLoaded || scene.path != ScenePath) return;
        foreach (var root in scene.GetRootGameObjects()) if (root.GetComponentInChildren<SkyboardRingRally>(true)) return;
        var obj = new GameObject("Skyboard Ring Rally");
        SceneManager.MoveGameObjectToScene(obj, scene);
        obj.AddComponent<SkyboardRingRally>();
    }

    void Awake()
    {
        var experience = FindFirstObjectByType<MarshmallowIslandExperience>();
        island = experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if (!island) { enabled = false; return; }
        entrance = this;
        var crown = Resources.Load<GameObject>("SkyboardRally/Angel Crown");
        var crownFilter = crown ? crown.GetComponentInChildren<MeshFilter>() : null;
        var crownMesh = crownFilter ? crownFilter.sharedMesh : null;
        ringMesh = BuildRingMesh(crownMesh ? .55f : .36f);
        if (!crownMesh) crownMesh = ringMesh;
        var haloShader = Resources.Load<Shader>("SkyboardRally/PolishedHalo");
        var gold = CreateHaloMaterial(haloShader, "Polished gold checkpoint", new Color(1f, .77f, .35f));
        var violet = CreateHaloMaterial(haloShader, "Polished violet boost checkpoint", new Color(.75f, .49f, 1f));
        portal = CreateRing("Midnight Circuit Portal", transform, Ground(20, 1965) + Vector3.up * 12,
            Quaternion.LookRotation(new Vector3(-1, 0, 1)), crownMesh, gold, "CIRCUIT PORTAL").root;
        BuildSign();
        BuildCircuit();
        for (int i = 0; i < Course.Length; i++)
        {
            Vector3 from = i == 0 ? Grid : Course[i - 1];
            Vector3 direction = Vector3.ProjectOnPlane(Course[i] - from, Vector3.up);
            bool boost = i == 2 || i == 5 || i == 8;
            string label = i == Course.Length - 1 ? "FINISH" : boost ? "BOOST" : "CHECKPOINT";
            var ring = CreateRing("Checkpoint " + (i + 1).ToString("00"), circuit.transform, CircuitOrigin + Course[i],
                Quaternion.LookRotation(direction), crownMesh, boost ? violet : gold, label + "  " + (i + 1) + "/" + Course.Length);
            ring.boost = boost;
            rings.Add(ring);
        }
        BuildHud();
        RefreshRings();
    }

    Material CreateHaloMaterial(Shader shader, string name, Color tint)
    {
        if (!shader) return CircuitMaterial(name, tint);
        var material = new Material(shader) { name = name, color = tint };
        materials.Add(material);
        return material;
    }

    Ring CreateRing(string name, Transform parent, Vector3 center, Quaternion rotation, Mesh visualMesh, Material material, string caption)
    {
        var obj = new GameObject(name, typeof(MeshCollider), typeof(BoxCollider));
        obj.transform.SetParent(parent, false);
        obj.transform.SetPositionAndRotation(center, rotation);
        obj.GetComponent<MeshCollider>().sharedMesh = ringMesh;
        var trigger = obj.GetComponent<BoxCollider>(); trigger.isTrigger = true;
        trigger.size = new Vector3(RingRadius * 2, RingRadius * 2, 1.5f);
        var visual = new GameObject("Angel Crown", typeof(MeshFilter), typeof(MeshRenderer));
        visual.transform.SetParent(obj.transform, false);
        // The imported mesh is already in the local XY plane. Size it to the collision rim.
        visual.transform.localScale = Vector3.one * (ringMesh.bounds.extents.x / visualMesh.bounds.extents.x);
        visual.GetComponent<MeshFilter>().sharedMesh = visualMesh;
        var renderer = visual.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SkyboardReward.Label(obj.transform, caption);
        var label = obj.transform.Find("Pickup label");
        label.localPosition = new Vector3(0, RingRadius + 1.9f, 0);
        label.localScale = Vector3.one * .023f;
        var text = label.GetComponentInChildren<Text>();
        text.fontSize = 30; text.color = new Color(1f, .94f, .68f);
        return new Ring { root = obj.transform, label = label, anchor = center };
    }

    static Mesh BuildRingMesh(float thickness)
    {
        const int segments = 48, sides = 10;
        var vertices = new Vector3[(segments + 1) * (sides + 1)];
        var triangles = new int[segments * sides * 6];
        for (int i = 0; i <= segments; i++)
            for (int j = 0; j <= sides; j++)
            {
                float a = i * Mathf.PI * 2 / segments, b = j * Mathf.PI * 2 / sides;
                float r = RingRadius + Mathf.Cos(b) * thickness;
                vertices[i * (sides + 1) + j] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, Mathf.Sin(b) * thickness);
            }
        int k = 0;
        for (int i = 0; i < segments; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = i * (sides + 1) + j, b = a + sides + 1;
                triangles[k++] = a; triangles[k++] = b; triangles[k++] = a + 1;
                triangles[k++] = a + 1; triangles[k++] = b; triangles[k++] = b + 1;
            }
        var mesh = new Mesh { name = "Skyboard checkpoint hoop", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    Vector3 Ground(float x, float z)
    {
        var bounds = island.bounds;
        if (island.Raycast(new Ray(new Vector3(x, bounds.max.y + 10, z), Vector3.down), out var hit, bounds.size.y + 20)) return hit.point;
        throw new System.InvalidOperationException($"Skyboard entrance has no ground at {x}, {z}.");
    }

    static GameObject Solid(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name; obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position; obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    void BuildSign()
    {
        sign = new GameObject("Skyboard rally start sign");
        sign.transform.SetParent(transform, false); sign.transform.position = Ground(31, 1957);
        var frame = CircuitMaterial("Rally sign frame", new Color(.41f, .22f, .33f));
        var slate = CircuitMaterial("Rally sign slate", new Color(.045f, .06f, .10f));
        var trim = CircuitMaterial("Rally sign neon trim", new Color(.24f, .85f, 1f));
        Solid("Left sign post", PrimitiveType.Cylinder, sign.transform, new Vector3(-3, 2, 0), new Vector3(.35f, 2, .35f), frame);
        Solid("Right sign post", PrimitiveType.Cylinder, sign.transform, new Vector3(3, 2, 0), new Vector3(.35f, 2, .35f), frame);
        Solid("Rally sign board", PrimitiveType.Cube, sign.transform, new Vector3(0, 4.1f, 0), new Vector3(7.8f, 3, .4f), slate);
        Solid("Rally sign trim", PrimitiveType.Cube, sign.transform, new Vector3(0, 5.7f, 0), new Vector3(8, .25f, .5f), trim);
        SkyboardReward.Label(sign.transform, "MIDNIGHT SUGAR CIRCUIT\nSkyboard time trial\nInteract here or fly through the portal\nFirst finish earns a Speed Scroll");
        var label = sign.transform.Find("Pickup label");
        label.name = "Rally sign text"; label.localPosition = new Vector3(0, 4.15f, -.28f); label.localScale = Vector3.one * .013f;
        ((RectTransform)label).sizeDelta = new Vector2(550, 180);
        label.GetComponentInChildren<Text>().fontSize = 25;
    }

    void BuildHud()
    {
        var hud = new GameObject("Skyboard rally HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        hud.transform.SetParent(transform, false);
        var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
        GameUITheme.Scale(canvas);
        var card = GameUITheme.Rect(hud.transform, "Race timing", new Vector2(0, 1), new Vector2(545, 96));
        card.pivot = new Vector2(0, 1); card.anchoredPosition = new Vector2(28, -25);
        GameUITheme.Image(card, new Color(.035f, .045f, .105f, .95f));
        GameUITheme.Rule(card, new Vector2(0, -2), new Vector2(545, 3), new Color(.25f, .88f, 1f), new Vector2(.5f, 1));
        status = GameUITheme.Label(card, "Race status", 23, new Vector2(-254, -26), new Vector2(510, 34), TextAnchor.MiddleLeft);
        detail = GameUITheme.Label(card, "Race detail", 18, new Vector2(-254, -65), new Vector2(510, 50), TextAnchor.MiddleLeft);
        hudCard = card.gameObject; hudCard.SetActive(false);
        var center = GameUITheme.Rect(hud.transform, "Countdown", new Vector2(.5f, .5f), new Vector2(140, 130));
        center.anchoredPosition = new Vector2(0, 110);
        GameUITheme.Image(center, new Color(.035f, .045f, .105f, .84f));
        countdown = GameUITheme.Label(center, "Countdown number", 72, Vector2.zero, new Vector2(140, 125), TextAnchor.MiddleCenter);
        countdown.color = new Color(.29f, .94f, 1f);
        countdownCard = center.gameObject; countdownCard.SetActive(false);
    }

    public void RestoreProgress(bool claimed, float best)
    {
        RewardClaimed = claimed;
        BestTime = float.IsNaN(best) || float.IsInfinity(best) || best <= 0 ? 0 : Mathf.Min(best, 3600);
    }

    void Update()
    {
        if ((!player || !board) && Time.unscaledTime >= nextLookup)
        {
            nextLookup = Time.unscaledTime + 1;
            player = FindFirstObjectByType<ThirdPersonPlayerController>();
            if (player) { body = player.GetComponent<CharacterController>(); inventory = player.GetComponent<EquipmentInventory>(); }
            board = FindFirstObjectByType<SkyboardReward>();
            scroll = Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
        }
        if (!player || !body || !board) return;
        var cam = Camera.main;
        if (InRace) UpdateRace(); else UpdateEntrance();
        if (InRace && cam && NextRing < rings.Count) rings[NextRing].label.rotation = cam.transform.rotation;
        UpdateHud(cam);
    }

    bool CanInteract() => player && body && body.enabled && board && !GameUITheme.ModalOpen &&
        !SecondIslandAwakening.BlocksInputFor(player) && !player.ExternalControlLock && Time.timeScale > 0 && Cursor.lockState == CursorLockMode.Locked;

    bool NearSign(float distance) => sign && Mathf.Abs(body.bounds.min.y - sign.transform.position.y) < 9 &&
        Vector3.ProjectOnPlane(player.transform.position - sign.transform.position, Vector3.up).sqrMagnitude < distance * distance;

    void UpdateEntrance()
    {
        if (!CanInteract()) { hasPrevious = false; return; }
        if (!board.Flying)
        {
            hasPrevious = false;
            if (!player.ExternalMovementLock && board.Owned && NearSign(9) && GameControls.Down(GameControls.Command.Interact)) EnterRace();
            return;
        }
        Vector3 current = body.bounds.center;
        if (hasPrevious && Crosses(portal, previous, current)) { EnterRace(); return; }
        previous = current; hasPrevious = true;
    }

    void EnterRace()
    {
        var exitGround = Ground(36, 1952);
        if (activeRace || !board.TryStartRallyFlight(CircuitOrigin.y + 58)) return;
        activeRace = this;
        returnRotation = player.transform.rotation;
        returnPosition = exitGround + Vector3.up * (player.transform.position.y - body.bounds.min.y + .12f);
        raceCamera = Camera.main;
        if (raceCamera)
        {
            savedClearFlags = raceCamera.clearFlags; savedBackground = raceCamera.backgroundColor; savedFarClip = raceCamera.farClipPlane;
            raceCamera.clearFlags = CameraClearFlags.SolidColor;
            raceCamera.backgroundColor = new Color(.015f, .020f, .036f);
            raceCamera.farClipPlane = 600;
        }
        portal.gameObject.SetActive(false); circuit.SetActive(true);
        RestartRace();
    }

    void RestartRace()
    {
        StopBoost();
        Running = false; NextRing = 0; penalty = lastTime = 0; messageUntil = recoveryUntil = 0;
        resultText = ""; lastCleared = CircuitOrigin + Grid; Relocating = false; targetDeadline = 0;
        startedAt = Time.time + 3; exitReadyAt = Time.unscaledTime + .35f;
        board.RallySteeringLocked = true;
        ShowRing(0);
        PlaceRider(CircuitOrigin + Grid, rings[0].root.rotation);
    }

    void PlaceRider(Vector3 center, Quaternion rotation)
    {
        Vector3 offset = body.bounds.center - player.transform.position;
        player.RestoreSavedPose(center - offset, rotation);
        board.RefreshRallyPosition();
        hasPrevious = false;
    }

    void UpdateRace()
    {
        if (!board.RallyFlight || !board.Flying || !body.enabled) { ReturnToIsland("RACE ENDED"); return; }
        if (!CanInteract()) { hasPrevious = false; return; }
        if (Time.unscaledTime >= exitReadyAt && GameControls.Down(GameControls.Command.Interact)) { ReturnToIsland("RETURNED TO THE ISLAND"); return; }
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) { RestartRace(); return; }
        if (!Running)
        {
            if (Time.time < startedAt) return;
            Running = true; board.RallySteeringLocked = false;
            targetDeadline = Time.time + RelocateAfter;
            GameplayAudio.Play(GameplayAudio.Cue.Pickup, player.transform.position, true);
        }
        if (Elapsed >= RaceLimit) { ReturnToIsland("TIME UP  ·  TRY THE CIRCUIT AGAIN"); return; }
        if (boostActive && Time.time >= boostUntil) StopBoost();
        Vector3 current = body.bounds.center;
        Vector3 local = current - CircuitOrigin;
        if (local.y < -15 || local.x < -215 || local.x > 64 || local.z < -86 || local.z > 184)
        {
            var next = rings[NextRing].root;
            Vector3 resume = NextRing == 0 ? CircuitOrigin + Grid : lastCleared + next.forward * 5;
            PlaceRider(resume, next.rotation);
            penalty += 3; recoveryUntil = Time.time + 2;
            return;
        }
        if (Relocating)
        {
            float phase = Mathf.Clamp01((Time.time - moveStarted) / RelocateSeconds);
            rings[NextRing].root.position = Vector3.Lerp(moveFrom, moveTo, Mathf.SmoothStep(0, 1, phase));
            if (phase >= 1) { Relocating = false; targetDeadline = Time.time + RelocateAfter; }
            hasPrevious = false;
            return;
        }
        if (Time.time >= targetDeadline) { BeginRelocation(); hasPrevious = false; return; }
        if (hasPrevious && Crosses(rings[NextRing].root, previous, current))
        {
            PassRing();
            if (!InRace) return;
        }
        previous = current; hasPrevious = true;
    }

    void ShowRing(int index)
    {
        var ring = rings[index]; ring.root.position = ring.anchor;
        FaceRing(ring.root, ring.anchor);
        Relocating = false; targetDeadline = Time.time + RelocateAfter;
        RefreshRings();
    }

    void FaceRing(Transform ring, Vector3 point)
    {
        Vector3 direction = Vector3.ProjectOnPlane(point - lastCleared, Vector3.up);
        if (direction.sqrMagnitude > .01f) ring.rotation = Quaternion.LookRotation(direction);
    }

    void BeginRelocation()
    {
        var ring = rings[NextRing];
        Vector3 routeDirection = Vector3.ProjectOnPlane(ring.anchor - lastCleared, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, routeDirection);
        moveFrom = ring.root.position;
        float side = Vector3.Dot(moveFrom - ring.anchor, right) > .1f ? -1f : 1f;
        // Move sideways over the same lane, with a small climb to keep the rim clear of the deck.
        moveTo = ring.anchor + right * (side * Random.Range(4f, 6f)) + Vector3.up * Random.Range(1f, 3f);
        FaceRing(ring.root, moveTo);
        Relocating = true; moveStarted = Time.time;
    }

    static bool Crosses(Transform ring, Vector3 from, Vector3 to)
    {
        Vector3 segment = to - from;
        if (segment.sqrMagnitude < .000001f || segment.sqrMagnitude > 16f * 16f) return false;
        float a = Vector3.Dot(from - ring.position, ring.forward), b = Vector3.Dot(to - ring.position, ring.forward);
        if (a >= 0 || b < 0 || b - a < .00001f) return false;
        Vector3 local = ring.InverseTransformPoint(Vector3.Lerp(from, to, -a / (b - a)));
        return local.x * local.x + local.y * local.y <= (RingRadius - .8f) * (RingRadius - .8f);
    }

    void PassRing()
    {
        var ring = rings[NextRing]; lastCleared = ring.root.position;
        GameplayAudio.Play(GameplayAudio.Cue.Pickup, player.transform.position, true);
        if (ring.boost)
        {
            if (!boostActive) baseSpeed = board.horizontalSpeed;
            boostActive = true; boostUntil = Time.time + 1.5f;
            board.horizontalSpeed = baseSpeed * 1.26f;
        }
        NextRing++;
        if (NextRing == rings.Count) Finish(); else ShowRing(NextRing);
    }

    void StopBoost()
    {
        if (boostActive && board) board.horizontalSpeed = baseSpeed;
        boostActive = false;
    }

    void Finish()
    {
        if (!InRace || !Running) return;
        float time = Elapsed;
        bool personal = BestTime <= 0 || time < BestTime;
        if (personal) BestTime = time;
        bool reward = !RewardClaimed && inventory && scroll;
        if (reward) { inventory.RestoreStack(scroll, inventory.Count(scroll) + 1); RewardClaimed = true; }
        string medal = time < 32 ? "GOLD" : time < 42 ? "SILVER" : "BRONZE";
        ReturnToIsland(medal + " FINISH  ·  " + time.ToString("F1") + "s" + (personal ? "  ·  NEW BEST" : "") + (reward ? "  ·  +1 SPEED SCROLL" : ""));
        if (!GameSaveManager.SaveCurrentGame()) GameSaveManager.RequestAutosave();
    }

    void ReturnToIsland(string message)
    {
        if (!InRace) return;
        lastTime = Elapsed; StopBoost();
        if (board) board.StopRallyFlight();
        if (player && player.gameObject.scene.isLoaded) player.RestoreSavedPose(returnPosition, returnRotation);
        if (raceCamera)
        {
            raceCamera.clearFlags = savedClearFlags; raceCamera.backgroundColor = savedBackground; raceCamera.farClipPlane = savedFarClip;
        }
        raceCamera = null;
        activeRace = null; Running = Relocating = hasPrevious = false; NextRing = 0;
        circuit.SetActive(false); portal.gameObject.SetActive(true);
        resultText = message; messageUntil = Time.unscaledTime + 8;
        RefreshRings();
    }

    void RefreshRings()
    {
        nextHudRefresh = 0;
        for (int i = 0; i < rings.Count; i++) rings[i].root.gameObject.SetActive(InRace && i == NextRing);
    }

    void UpdateHud(Camera cam)
    {
        if (!hudCard) return;
        bool modal = GameUITheme.ModalOpen || SecondIslandAwakening.BlocksInputFor(player) || player.ExternalControlLock;
        bool result = Time.unscaledTime < messageUntil;
        bool show = !modal && (InRace || result || NearSign(24));
        hudCard.SetActive(show);
        countdownCard.SetActive(show && InRace && Time.time < startedAt + .65f);
        if (!show || Time.unscaledTime < nextHudRefresh) return;
        nextHudRefresh = Time.unscaledTime + .08f;
        if (InRace)
        {
            countdown.text = Running ? "GO!" : Mathf.Max(1, Mathf.CeilToInt(startedAt - Time.time)).ToString();
            status.text = $"MIDNIGHT CIRCUIT   ·   {NextRing}/{rings.Count}   ·   {Elapsed:F1}s";
            var gate = rings[NextRing].root;
            Vector3 delta = gate.position - body.bounds.center;
            Vector3 forward = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : player.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float f = Vector3.Dot(forward, delta), s = Vector3.Dot(right, delta);
            string arrow = Mathf.Abs(s) > Mathf.Abs(f) * .45f ? (s > 0 ? "→" : "←") : (f >= 0 ? "↑" : "↓");
            string guidance = !Running ? "HOLD AT THE START" : Time.time < recoveryUntil ? "BACK ON TRACK  ·  +3s" :
                $"{arrow}  NEXT RING  {Mathf.RoundToInt(delta.magnitude)} m  ·  {(Relocating ? "RING MOVING" : "MOVES IN " + Mathf.CeilToInt(TimeUntilMove) + "s")}";
            detail.text = guidance + (boostActive ? "  ·  BOOST" : "");
        }
        else if (result)
        {
            status.text = resultText;
            detail.text = "Best: " + (BestTime > 0 ? BestTime.ToString("F1") + "s" : "--") + "   ·   Return to the sign to race again.";
        }
        else
        {
            status.text = "MIDNIGHT SUGAR CIRCUIT";
            detail.text = board.Owned ? GameControls.Prompt(GameControls.Command.Interact) + "  ·  ENTER TIME TRIAL\n10 rings  ·  Purple rings boost your board" :
                "Find the Skyboard at the end of the marshmallow course.";
        }
    }

    void OnDisable()
    {
        if (InRace) ReturnToIsland("");
        if (entrance == this) entrance = null;
    }

    void OnDestroy()
    {
        foreach (var material in materials) if (material) Destroy(material);
        foreach (var mesh in sceneryMeshes) if (mesh) Destroy(mesh);
        if (ringMesh) Destroy(ringMesh);
    }
}
