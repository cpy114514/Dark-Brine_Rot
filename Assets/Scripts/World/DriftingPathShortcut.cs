using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>A hillside passage available after clearing the ridge encounter.</summary>
[DisallowMultipleComponent]
public sealed class DriftingPathShortcut : MonoBehaviour
{
    const float Radius = 3.65f;
    static readonly Vector2 Site = new Vector2(-122f, 1790f);
    static DriftingPathShortcut active;
    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth playerHealth;
    SecondIslandObjectives objectives;
    ZhuBaJieBoss boss;
    GameObject marker;
    Vector3 destination;
    Quaternion arrivalRotation;
    int usedFrame = -1;
    GUIStyle style;

    public bool Unlocked => boss && boss.Health.IsDead || objectives && objectives.RidgeCleared;
    public bool Guides(ThirdPersonPlayerController actor) => Unlocked && actor == player &&
        Vector3.ProjectOnPlane(actor.transform.position - transform.position, Vector3.up).sqrMagnitude < 160f * 160f;

    internal static void Build(ZhuBaJieBoss boss)
    {
        var scene = boss.gameObject.scene;
        if (scene.path != SecondIslandRoninCamp.ScenePath || active) return;
        var player = FindFirstObjectByType<ThirdPersonPlayerController>();
        var experience = player ? player.GetComponent<MarshmallowIslandExperience>() : null;
        var ground = experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        var guide = FindFirstObjectByType<MarshmallowChallengeGuide>();
        if (!ground || !guide || !player.TryGetComponent<CharacterController>(out var body)) return;

        var approach = guide.transform.position - Vector3.forward * (guide.triggerRadius + 3f);
        if (!FindGround(ground, new Vector3(Site.x, 0, Site.y), out var at) || !FindGround(ground, approach, out var destination)) return;
        var root = new GameObject("Drifting Path shortcut");
        SceneManager.MoveGameObjectToScene(root, scene);
        var facing = Vector3.ProjectOnPlane(at - boss.transform.position, Vector3.up);
        root.transform.SetPositionAndRotation(at, Quaternion.LookRotation(facing));
        var passage = root.AddComponent<DriftingPathShortcut>();
        active = passage;
        passage.player = player;
        passage.body = body;
        passage.playerHealth = player.GetComponent<PlayerHealth>();
        passage.boss = boss;
        passage.objectives = player.GetComponent<SecondIslandObjectives>();
        passage.destination = destination;
        passage.arrivalRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(guide.transform.position - destination, Vector3.up));
        passage.BuildMarker(ground);
    }

    static bool FindGround(Collider ground, Vector3 site, out Vector3 point)
    {
        var bounds = ground.bounds;
        bool found = ground.Raycast(new Ray(new Vector3(site.x, bounds.max.y + 3f, site.z), Vector3.down), out var hit, bounds.size.y + 6f);
        point = hit.point;
        return found && hit.normal.y > .70f;
    }

    void BuildMarker(Collider ground)
    {
        marker = new GameObject("Lantern waypost");
        marker.transform.SetParent(transform, false);
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "Waypost support";
        post.transform.SetParent(marker.transform, false);
        post.transform.localPosition = new Vector3(0, 1f, .26f);
        post.transform.localScale = new Vector3(.27f, 2.35f, .28f);
        var wood = Resources.Load<Material>("SecondIslandBlackboard/Blackboard Wood");
        if (wood) post.GetComponent<Renderer>().sharedMaterial = wood;
        SkyboardReward.Label(marker.transform, "DRIFTING PATH\nShortcut to the course entrance");
        var board = (RectTransform)marker.transform.Find("Pickup label");
        board.localPosition = Vector3.up * 2.28f;
        board.localScale = Vector3.one * .0095f;
        board.sizeDelta = new Vector2(465, 112);

        var lantern = SecondIslandProp.Load("Holiday/lantern");
        if (lantern)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var site = transform.TransformPoint(new Vector3(side * 2.35f, 0, .35f));
                if (!FindGround(ground, site, out var at)) continue;
                var fixture = new GameObject("Passage lantern", typeof(BoxCollider)).transform;
                fixture.SetParent(marker.transform, false);
                fixture.position = at;
                SecondIslandProp.Mount(lantern, fixture, Vector3.up * .65f, new Vector3(1.2f, 1.3f, 1.2f), Quaternion.identity);
                var hit = fixture.GetComponent<BoxCollider>();
                hit.center = Vector3.up * .65f;
                hit.size = new Vector3(.82f, 1.3f, .82f);
            }
        }
        marker.SetActive(false);
    }

    bool CanTravel()
    {
        if (!Unlocked || !player || !player.enabled || !body || !body.enabled || !body.isGrounded || player.Swimming ||
            player.ExternalControlLock || player.ExternalMovementLock || !playerHealth || playerHealth.currentHealth <= 0f ||
            Time.timeScale <= 0 || GameUITheme.ModalOpen || PlayerDeathRespawn.IsOpen) return false;
        Vector3 feet = body.bounds.center - Vector3.up * body.bounds.extents.y;
        return Vector3.Distance(feet, transform.position) < Radius;
    }

    internal static bool ReservesInteractionFor(ThirdPersonPlayerController actor) => active && active.player == actor &&
        (active.usedFrame == Time.frameCount || active.CanTravel());

    void Update()
    {
        if (marker.activeSelf != Unlocked) marker.SetActive(Unlocked);
        if (!CanTravel() || Cursor.lockState != CursorLockMode.Locked || !GameControls.Down(GameControls.Command.Interact)) return;
        usedFrame = Time.frameCount;
        float sole = (body.center.y - body.height * .5f) * Mathf.Abs(player.transform.lossyScale.y);
        player.RestoreSavedPose(destination + Vector3.up * (.10f - sole), arrivalRotation);
        GameplayAudio.Play(GameplayAudio.Cue.Pickup, destination, true);
        GameSaveManager.RequestAutosave();
    }

    void OnGUI()
    {
        if (!CanTravel()) return;
        style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 20, font = GameLocalization.Font };
        GUI.Box(new Rect(Screen.width * .5f - 210, Screen.height - 156, 420, 52),
            GameControls.Prompt(GameControls.Command.Interact) + "  ·  TAKE THE DRIFTING PATH SHORTCUT", style);
    }

    void OnDestroy() { if (active == this) active = null; }
}
