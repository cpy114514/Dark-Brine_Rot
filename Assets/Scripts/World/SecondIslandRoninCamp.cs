using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>The walk-up checkpoint and supply stop on the approach to Zhu Bajie.</summary>
[DisallowMultipleComponent]
public sealed class SecondIslandRoninCamp : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/Second Island/Second Island.unity";
    public const string SaveId = "second-island.ronin-rest-camp";
    const float DeckRadius = 4f;
    const int Sides = 32;
    static readonly Vector2 Site = new Vector2(-79f, 1715f);

    PlayerDeathRespawn respawn;
    IslandRespawnPoint point;
    IslandSupplyCache supply;
    Material deckMaterial, postMaterial;
    Mesh deck;
    bool wasSelected, wasRestocking;

    public bool IsCheckpoint => respawn && respawn.Checkpoint == point;

    internal static void Build(ZhuBaJieBoss boss)
    {
        var scene = boss.gameObject.scene;
        if (scene.path != ScenePath || FindFirstObjectByType<SecondIslandRoninCamp>()) return;
        var player = FindFirstObjectByType<ThirdPersonPlayerController>();
        var experience = player ? player.GetComponent<MarshmallowIslandExperience>() : null;
        var ground = experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if (!ground || !player.TryGetComponent<PlayerDeathRespawn>(out var respawn) || !Ground(ground, Site, out float height)) return;

        for (int i = 0; i < Sides; i++)
            if (Ground(ground, Site + Around(i) * DeckRadius, out float edgeHeight))
                height = Mathf.Max(height, edgeHeight);

        var root = new GameObject("Ridge Rest Camp");
        SceneManager.MoveGameObjectToScene(root, scene);
        Vector3 center = new Vector3(Site.x, height + .10f, Site.y);
        var toward = Vector3.ProjectOnPlane(boss.transform.position - center, Vector3.up);
        root.transform.SetPositionAndRotation(center, Quaternion.LookRotation(toward));
        var camp = root.AddComponent<SecondIslandRoninCamp>();
        camp.respawn = respawn;
        camp.point = root.AddComponent<IslandRespawnPoint>();
        camp.point.pointName = root.name;
        camp.point.isDefault = false;
        camp.point.useAsFallback = false;
        camp.point.requireGrounded = true;
        camp.point.activationRadius = 3.15f;
        camp.supply = root.AddComponent<IslandSupplyCache>();
        camp.supply.supportsMelee = true;
        camp.supply.interactionRadius = 3.15f;
        root.AddComponent<WorldSaveIdentity>().id = SaveId;
        camp.BuildDeck(ground);
        camp.BuildFixtures();
    }

    static Vector2 Around(int i)
    {
        float angle = i * (2f * Mathf.PI / Sides);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    static bool Ground(Collider ground, Vector2 at, out float height)
    {
        var bounds = ground.bounds;
        bool found = ground.Raycast(new Ray(new Vector3(at.x, bounds.max.y + 2f, at.y), Vector3.down),
            out var hit, bounds.size.y + 4f);
        height = hit.point.y;
        return found;
    }

    void BuildDeck(Collider ground)
    {
        const int rings = 4;
        const float rampWidth = 6f;
        var vertices = new Vector3[1 + rings * Sides];
        var floor = new List<int>();
        var ramps = new List<int>();
        for (int ring = 0; ring < rings; ring++)
        {
            float progress = ring / (float)(rings - 1);
            float radius = DeckRadius + progress * rampWidth;
            for (int i = 0; i < Sides; i++)
            {
                Vector2 at = Site + Around(i) * radius;
                float y = transform.position.y;
                if (ring > 0 && Ground(ground, at, out float islandY) && Ground(ground, Site + Around(i) * (DeckRadius + rampWidth), out float outerY))
                    y = Mathf.Max(islandY + .035f, Mathf.Lerp(y, outerY + .035f, progress));
                int v = 1 + ring * Sides + i;
                int next = 1 + ring * Sides + (i + 1) % Sides;
                vertices[v] = transform.InverseTransformPoint(new Vector3(at.x, y, at.y));
                if (ring == 0) { floor.Add(0); floor.Add(next); floor.Add(v); }
                else
                {
                    ramps.Add(v - Sides); ramps.Add(next); ramps.Add(v);
                    ramps.Add(v - Sides); ramps.Add(next - Sides); ramps.Add(next);
                }
            }
        }
        var uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++) uv[i] = new Vector2(vertices[i].x, vertices[i].z) / 3f;
        deck = new Mesh { name = "Ridge camp deck and approach", vertices = vertices, uv = uv, subMeshCount = 2 };
        deck.SetTriangles(floor, 0);
        deck.SetTriangles(ramps, 1);
        deck.RecalculateNormals();
        deck.RecalculateTangents();
        deckMaterial = MakeMaterial(new Color(.92f,.82f,.73f));
        deckMaterial.mainTexture = Resources.Load<Texture2D>("SecondIslandProps/RoninCamp/Deck Albedo");
        var normal = Resources.Load<Texture2D>("SecondIslandProps/RoninCamp/Deck Normal");
        if (normal)
        {
            deckMaterial.SetTexture("_BumpMap", normal);
            deckMaterial.SetFloat("_BumpScale", .55f);
            deckMaterial.EnableKeyword("_NORMALMAP");
        }
        var islandRenderer = ground.GetComponent<Renderer>();
        var approachMaterial = islandRenderer && islandRenderer.sharedMaterial ? islandRenderer.sharedMaterial : deckMaterial;
        gameObject.AddComponent<MeshFilter>().sharedMesh = deck;
        gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { deckMaterial, approachMaterial };
        gameObject.AddComponent<MeshCollider>().sharedMesh = deck;
    }

    void BuildFixtures()
    {
        PlaceFixture("Quaternius/Bench", "Rest bench", new Vector3(2.5f, 0, .7f), new Vector3(2.85f, 1.32f, 1.1f), -90);
        PlaceFixture("Quaternius/Crate", "Supply crate", new Vector3(1.65f, 0, 2.52f), new Vector3(1.4f, 1.1f, 1.3f), 12);
        PlaceFixture("Holiday/lantern", "Camp lantern", new Vector3(1.9f, 0, -2.75f), new Vector3(1.15f, 1.85f, 1.15f), 0);

        var sign = new GameObject("Ridge Rest Camp blackboard").transform;
        sign.SetParent(transform, false);
        sign.localPosition = new Vector3(-2.65f, 0, .55f);
        var fromTrail = Vector3.ProjectOnPlane(sign.position - new Vector3(-94, sign.position.y, 1676), Vector3.up);
        sign.rotation = Quaternion.LookRotation(fromTrail);
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "Blackboard post";
        post.transform.SetParent(sign, false);
        post.transform.localPosition = new Vector3(0, .97f, .26f);
        post.transform.localScale = new Vector3(.28f, 1.94f, .29f);
        postMaterial = MakeMaterial(new Color(.30f,.19f,.11f));
        post.GetComponent<Renderer>().sharedMaterial = postMaterial;
        SkyboardReward.Label(sign, "RIDGE REST CAMP\nWalk up to save your checkpoint\n" + GameControls.Prompt(GameControls.Command.Interact) +
            "  Supplies   ·   " + GameControls.Prompt(GameControls.Command.Gravity) + "  Switch gravity");
        var label = (RectTransform)sign.Find("Pickup label");
        label.localPosition = Vector3.up * 2.27f;
        label.localScale = Vector3.one * .009f;
        label.sizeDelta = new Vector2(460, 142);
        label.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 25;

    }

    void PlaceFixture(string path, string label, Vector3 site, Vector3 size, float yaw)
    {
        var asset = SecondIslandProp.Load(path);
        if (!asset) return;
        var root = new GameObject(label, typeof(BoxCollider)).transform;
        root.SetParent(transform, false);
        root.localPosition = site;
        root.localRotation = Quaternion.Euler(0, yaw, 0);
        var visual = SecondIslandProp.Mount(asset, root, Vector3.up * (size.y * .5f), size, Quaternion.identity);
        var renderers = visual.GetComponentsInChildren<Renderer>();
        float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
        foreach (var renderer in renderers)
        {
            bottom = Mathf.Min(bottom, renderer.bounds.min.y);
            top = Mathf.Max(top, renderer.bounds.max.y);
        }
        if (renderers.Length > 0) visual.position += Vector3.up * (root.position.y - bottom);
        size.y = renderers.Length > 0 ? top - bottom : size.y;
        var hit = root.GetComponent<BoxCollider>();
        hit.center = Vector3.up * (size.y * .5f);
        hit.size = size;
    }

    static Material MakeMaterial(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = color };
        material.SetFloat("_Smoothness", .12f);
        return material;
    }

    void Update()
    {
        bool selected = IsCheckpoint;
        bool restocking = supply.Remaining > 0f;
        if (!GameSaveManager.IsContinuePending && (selected && !wasSelected || restocking && !wasRestocking))
        {
            if (selected && !wasSelected)
            {
                FindFirstObjectByType<IslandActivityGuide>()?.ShowRidgeCampSaved();
                if (!GameSaveManager.SaveCurrentGame()) GameSaveManager.RequestAutosave();
            }
            else GameSaveManager.RequestAutosave();
        }
        wasSelected = selected;
        wasRestocking = restocking;
    }

    void OnDestroy()
    {
        if (deck) Destroy(deck);
        if (deckMaterial) Destroy(deckMaterial);
        if (postMaterial) Destroy(postMaterial);
    }

    internal static bool ReservesInteractionFor(ThirdPersonPlayerController player)
    {
        var camp = FindFirstObjectByType<SecondIslandRoninCamp>();
        if (!camp || !player.TryGetComponent<CharacterController>(out var body) || !body.enabled) return false;
        Vector3 feet = body.bounds.center - Vector3.up * body.bounds.extents.y;
        return Vector3.Distance(feet, camp.transform.position) < camp.supply.interactionRadius;
    }
}
