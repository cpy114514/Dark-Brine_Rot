using System.Collections;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MarshmallowHollowEntrance : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/Second Island/Marshmallow Hollow.unity";
    const string IslandPath = "Assets/Scenes/Second Island/Second Island.unity";
    static MarshmallowHollowEntrance active;

    public int Chapter { get; private set; }
    public int GuardianDefeats { get; private set; }
    public bool WaymarkCopied { get; private set; }
    public int PacksTaken { get; private set; }
    public bool RewardClaimed => PacksTaken >= 2;
    public bool Assisted { get; private set; }
    public bool Inside => active == this && cave && cave.Bound;
    public static bool Contains(ThirdPersonPlayerController player) => active && active.Inside && active.player == player;

    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    PlayerDeathRespawn death;
    GravityWristbandAbility gravity;
    MarshmallowIslandExperience weather;
    SkyboardReward board;
    Camera view;
    IslandRespawnPoint surfaceCheckpoint;
    MarshmallowHollow cave;
    MarshmallowHollowArt art;
    Transform guide, guideArm;
    Quaternion guideArmRest;
    Vector3 guideHome, guideDestination, returnPosition;
    Quaternion returnRotation;
    bool surfaceGravity, restoreWeather, moving, previousControl, guideSeen;
    float oldFarClip, guideNoticeUntil;
    Color oldBackground;
    CameraClearFlags oldClearFlags;
    CanvasGroup fade;
    Text prompt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        active = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initial()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) Install(SceneManager.GetSceneAt(i));
    }
    static void OnSceneLoaded(Scene scene, LoadSceneMode _) => Install(scene);
    static void Install(Scene scene)
    {
        if (!scene.isLoaded || scene.path != IslandPath) return;
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponent<MarshmallowHollowEntrance>()) return;
        var site = new GameObject("Marshmallow Hollow entrance");
        SceneManager.MoveGameObjectToScene(site, scene);
        site.AddComponent<MarshmallowHollowEntrance>();
    }

    IEnumerator Start()
    {
        // The cottage and gravity reward are installed during scene startup.
        yield return null;
        player = FindFirstObjectByType<ThirdPersonPlayerController>();
        var cottage = FindFirstObjectByType<CandyHouseMarker>();
        if (!player || !cottage || !player.TryGetComponent(out body)) yield break;
        health = player.GetComponent<PlayerHealth>();
        death = player.GetComponent<PlayerDeathRespawn>();
        gravity = player.GetComponent<GravityWristbandAbility>();
        weather = player.GetComponent<MarshmallowIslandExperience>();
        board = FindFirstObjectByType<SkyboardReward>();
        var surface = weather && weather.surface ? weather.surface.GetComponent<Collider>() : null;
        if (!surface) yield break;

        Vector3 center = cottage.transform.position;
        if (!Ground(surface, center + new Vector3(16, 0, 28), out var door) ||
            !Ground(surface, center + new Vector3(9, 0, 14), out guideHome)) yield break;
        transform.SetPositionAndRotation(door, Quaternion.LookRotation(Vector3.ProjectOnPlane(door - guideHome, Vector3.up)));
        if (!Ground(surface, door - transform.right*4.8f - transform.forward*2.2f, out guideDestination)) guideDestination=guideHome;
        art = new MarshmallowHollowArt();
        var arch = art.Arch(transform, Vector3.zero, 7.5f, 6.8f, art.Stone);
        art.Rounded(transform,"Hillside sugarstone cap",new Vector3(0,2.3f,4.4f),new Vector3(11.8f,7.3f,8.2f),art.Sugar,true);
        for (int side=-1;side<=1;side+=2)
        {
            var shoulder=art.Rounded(transform,"Buried sugarstone shoulder",new Vector3(side*4.4f,1.32f,2.05f),
                new Vector3(4.7f,5.75f,6.7f),art.Stone,true);
            shoulder.localRotation=Quaternion.Euler(13,side*22,side*15);
            art.Rounded(transform,"Sugarstone scree",new Vector3(side*5.3f,.20f,-.33f),new Vector3(2.9f,1.55f,3.4f),art.Sugar,true);
        }
        art.Panel(arch, "MARSHMALLOW HOLLOW", new Vector3(0, 5.75f, -1.10f), 5.6f, .8f);
        art.Box(arch, "Cave loading threshold", new Vector3(0, 2.78f, .36f), new Vector3(5.9f, 5.56f, .65f), art.Shadow);
        art.Crystal(arch, new Vector3(-3.35f, .62f, -1.05f), 1.15f, art.Amber);
        art.Crystal(arch, new Vector3(3.25f, .7f, -1.15f), 1.35f, art.Mint);
        var model = Resources.Load<GameObject>("SecondIslandResidents/Mint Friendly Marshmallow");
        if (model)
        {
            guide = Instantiate(model, guideHome, transform.rotation, transform).transform;
            guide.name = "Pip - cave guide";
            foreach (var bone in guide.GetComponentsInChildren<Transform>())
                if (bone.name=="Left arm pivot") { guideArm=bone; guideArmRest=bone.localRotation; break; }
        }
        BuildUi();
    }

    static bool Ground(Collider surface, Vector3 point, out Vector3 result)
    {
        var bounds = surface.bounds;
        bool found = surface.Raycast(new Ray(new Vector3(point.x, bounds.max.y + 3f, point.z), Vector3.down),
            out var hit, bounds.size.y + 6f);
        result = hit.point;
        return found;
    }

    void BuildUi()
    {
        var root = new GameObject("Hollow entrance UI", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 96;
        GameUITheme.Scale(canvas);
        var card = GameUITheme.Rect(root.transform, "Guide", new Vector2(.5f, 0), new Vector2(690, 82));
        card.anchoredPosition = new Vector2(0, 110);
        GameUITheme.Image(card, GameUITheme.Surface);
        prompt = GameUITheme.Label(card, "Guide prompt", 22, new Vector2(0, -41), new Vector2(655, 76), TextAnchor.MiddleCenter);
        prompt.gameObject.SetActive(true);
        card.gameObject.SetActive(false);

        var cover = new GameObject("Hollow loading fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        cover.transform.SetParent(transform, false);
        var coverCanvas = cover.GetComponent<Canvas>();
        coverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        coverCanvas.sortingOrder = 32000;
        var sheet = new GameObject("Black", typeof(RectTransform), typeof(Image));
        sheet.transform.SetParent(cover.transform, false);
        var rect = (RectTransform)sheet.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        sheet.GetComponent<Image>().color = new Color(.025f, .016f, .030f);
        fade = cover.GetComponent<CanvasGroup>();
        fade.alpha = 0f; fade.blocksRaycasts = false;
    }

    void Update()
    {
        if (!prompt) return;
        if (moving || Inside || !CanAct())
        {
            prompt.transform.parent.gameObject.SetActive(false);
            return;
        }
        if (!gravity) gravity = player.GetComponent<GravityWristbandAbility>();
        Vector3 feet = body.bounds.center - Vector3.up * body.bounds.extents.y;
        bool nearGuide = Vector3.Distance(feet, guideHome) < 9f;
        bool nearDoor = Vector3.Distance(feet, transform.position - transform.forward * 1.3f) < 5f;
        if (nearGuide && !guideSeen) { guideSeen = true; guideNoticeUntil = Time.unscaledTime + 8f; }
        if (guide)
        {
            Vector3 target = gravity && gravity.Owned && guideSeen ? guideDestination : guideHome;
            guide.position = Vector3.MoveTowards(guide.position,target,Time.deltaTime*4.7f);
            var forward = Vector3.ProjectOnPlane(player.transform.position - guide.position, Vector3.up);
            if (forward.sqrMagnitude > .01f) guide.rotation = Quaternion.LookRotation(forward);
            if (guideArm)
            {
                float wave=Time.unscaledTime<guideNoticeUntil ? 52f+Mathf.Sin(Time.time*11f)*19f : 0f;
                guideArm.localRotation=guideArmRest*Quaternion.Euler(-wave,0,wave*.45f);
            }
        }
        bool show = nearDoor || Time.unscaledTime < guideNoticeUntil;
        prompt.transform.parent.gameObject.SetActive(show);
        if (!show) return;
        if (!gravity || !gravity.Owned)
            prompt.text = "PIP\nBring the Gravity Wristband. Some doors down there open under your weight.";
        else if (nearDoor)
        {
            prompt.text = GameControls.Prompt(GameControls.Command.Interact) + "   ENTER MARSHMALLOW HOLLOW\nThe old kilns are still warm.";
            if (body.isGrounded && GameControls.Down(GameControls.Command.Interact)) StartCoroutine(Enter());
        }
        else prompt.text = WaymarkCopied ? "PIP\nThat mark comes from somewhere beyond this island. Keep the rubbing close." :
            "PIP\nFollow me behind the cottage. There's something down there you should see.";
    }

    public bool CanAct() => player && player.enabled && body && body.enabled && health && health.currentHealth > 0 &&
        !PlayerDeathRespawn.IsOpen && !player.ExternalControlLock && !player.ExternalMovementLock && !player.Swimming &&
        !GameUITheme.ModalOpen && !PauseSettingsMenu.IsOpen && Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked &&
        (!board || !board.Flying);

    IEnumerator Enter()
    {
        if (moving || active || !gravity || !gravity.Owned || !Application.CanStreamedLevelBeLoaded(ScenePath)) yield break;
        moving = true;
        active = this;
        surfaceCheckpoint = death ? death.Checkpoint : null;
        surfaceGravity = gravity.NormalGravity;
        Vector3 feet = body.bounds.center - Vector3.up * body.bounds.extents.y;
        returnPosition = feet;
        returnRotation = Quaternion.LookRotation(-transform.forward);
        Lock();
        yield return Fade(1);
        var load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
        if (load != null) yield return load;
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (scene.isLoaded)
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent(out cave)) break;
        if (cave)
        {
            view = Camera.main;
            if (view)
            {
                oldFarClip = view.farClipPlane; oldBackground = view.backgroundColor; oldClearFlags = view.clearFlags;
                view.farClipPlane = 172f; view.clearFlags = CameraClearFlags.SolidColor;
                view.backgroundColor = new Color(.033f, .026f, .048f);
            }
            restoreWeather = weather && weather.enabled;
            if (restoreWeather) weather.enabled = false;
            if (board) board.GetComponent<MarshmallowHopRecovery>()?.ClearTracking();
            cave.Bind(this, player);
            gravity.SetNormalGravity(false);
            GameSaveManager.RequestAutosave();
        }
        else
        {
            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            active = null;
        }
        yield return Fade(0);
        Unlock();
    }

    public void Leave()
    {
        if (Inside && !moving) StartCoroutine(Exit());
    }

    IEnumerator Exit()
    {
        moving = true;
        Lock();
        yield return Fade(1);
        var scene = cave.gameObject.scene;
        cave.Unbind();
        PlaceAt(returnPosition, returnRotation);
        if (death && surfaceCheckpoint) death.SetCheckpoint(surfaceCheckpoint);
        if (gravity) gravity.SetNormalGravity(surfaceGravity);
        RestoreView();
        cave = null;
        active = null;
        yield return SceneManager.UnloadSceneAsync(scene);
        yield return Fade(0);
        Unlock();
        GameSaveManager.RequestAutosave();
    }

    void RestoreView()
    {
        if (view)
        {
            view.farClipPlane = oldFarClip; view.backgroundColor = oldBackground; view.clearFlags = oldClearFlags;
            view = null;
        }
        if (restoreWeather && weather) weather.enabled = true;
        restoreWeather = false;
    }

    void Lock()
    {
        previousControl = player.ExternalControlLock;
        player.ExternalControlLock = true;
        fade.blocksRaycasts = true;
    }
    void Unlock()
    {
        if (player) player.ExternalControlLock = previousControl;
        if (fade) fade.blocksRaycasts = false;
        moving = false;
    }
    IEnumerator Fade(float target)
    {
        float start = fade.alpha;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / .28f)
        {
            fade.alpha = Mathf.SmoothStep(start, target, t);
            yield return null;
        }
        fade.alpha = target;
    }
    public void PlaceAt(Vector3 ground, Quaternion rotation)
    {
        float sole = (body.center.y - body.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
        player.RestoreSavedPose(ground + Vector3.up * (.10f - sole), rotation);
    }

    public void Advance(int chapter)
    {
        if (chapter <= Chapter || chapter > 4) return;
        Chapter = chapter;
        GameSaveManager.RequestAutosave();
    }
    public void RecordGuardianDefeat()
    {
        GuardianDefeats = Mathf.Min(999, GuardianDefeats + 1);
        GameSaveManager.RequestAutosave();
    }
    public void EnableAssistance()
    {
        Assisted = true;
        GameSaveManager.RequestAutosave();
    }
    public int ClaimReward()
    {
        if (Chapter < 4) return 0;
        int count = HealingPackSupply.Add(2-PacksTaken);
        if (count > 0 || !WaymarkCopied)
        {
            WaymarkCopied = true;
            PacksTaken += count;
            GameSaveManager.RequestAutosave();
        }
        return count;
    }
    public void RestoreProgress(int chapter, int defeats, bool assisted, bool copied, int packsTaken)
    {
        Chapter = Mathf.Clamp(chapter, 0, 4);
        GuardianDefeats = Mathf.Clamp(defeats, 0, 999);
        Assisted = assisted && GuardianDefeats >= 3;
        WaymarkCopied = copied && Chapter == 4;
        PacksTaken = Chapter == 4 ? Mathf.Clamp(packsTaken,0,2) : 0;
    }

    public static bool SurfaceSavePose(ThirdPersonPlayerController actor, out Vector3 position, out Quaternion rotation)
    {
        if (!Contains(actor)) { position = default; rotation = default; return false; }
        float sole = (active.body.center.y - active.body.height * .5f) * Mathf.Abs(actor.transform.lossyScale.y);
        position = active.returnPosition + Vector3.up * (.10f - sole);
        rotation = active.returnRotation;
        return true;
    }
    public static bool SaveGravity(ThirdPersonPlayerController actor, bool current) => Contains(actor) ? active.surfaceGravity : current;
    public static IslandRespawnPoint SaveCheckpoint(ThirdPersonPlayerController actor, IslandRespawnPoint current) =>
        Contains(actor) ? active.surfaceCheckpoint : current;

    void OnDestroy()
    {
        art?.Dispose();
        if (active != this) return;
        RestoreView();
        if (moving && player) player.ExternalControlLock = previousControl;
        active = null;
    }
}

