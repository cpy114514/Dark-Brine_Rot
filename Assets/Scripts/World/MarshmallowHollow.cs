using System;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Mark = MarshmallowHollowArt.Mark;

[DisallowMultipleComponent]
public sealed class MarshmallowHollow : MonoBehaviour
{
    [SerializeField] Mesh marshmallowMesh;
    [SerializeField] GameObject guidePrefab, scoutPrefab, hopperPrefab, huskPrefab;
    static readonly Vector3 Origin = new Vector3(4200, 2640, -2500);
    static readonly Vector3 EntranceExit = new Vector3(0,0,2);
    static readonly Vector3[] Checkpoints =
    {
        new Vector3(0,0,4), new Vector3(0,0,33), new Vector3(0,8,72),
        new Vector3(0,8,95), new Vector3(0,8,137)
    };
    static readonly int[] PrismAnswer = { 1, 3, 2 };
    static readonly string[] Compass = { "N", "E", "S", "W" };
    static readonly float[] DoorPositions = { 29f, 68f, 99f, 130f };
    static readonly Mark[] ArchiveAnswer = { Mark.Plane, Mark.Falling, Mark.Slope };
    static readonly string[] Objectives =
    {
        "Press the kiln switch", "Align the anchor prisms", "Reconstruct the flight",
        "Defeat the Kiln Guardian", "Copy the stranger's mark"
    };
    static readonly string[] WallNotes =
    {
        "01   THE FIRST KILNS\nThe founders fired sugar crystals to lift their home above the storm sea.",
        "02   THE ANCHOR\nThe crystal lattice can pull a falling object toward a chosen landing point.",
        "03   THE CHANGED ROUTE\nSomeone re-cut the anchor for the slope where you woke. They stamped it with a picture of your club.",
        "04   THE STRANGER'S MARK\nThe final panel has two empty fields. The mark in the margin belongs to another place."
    };
    static readonly string[] Hints =
    {
        "PIP  ·  The old switch reads weight. Stand on it and set your wristband to normal gravity.",
        "PIP  ·  Use low gravity to reach the shelf. Match each prism to the direction engraved above it, then press the switch.",
        "PIP  ·  Three tiles record a journey. Start with what brought you here and finish where you woke.",
        "PIP  ·  Jump over the floor wave. When the floor circles glow, step onto one in normal gravity.",
        "PIP  ·  The mark in the margin belongs to somewhere else. Take a rubbing before we leave."
    };

    public bool Bound => entrance && actor;
    public int Chapter => entrance ? entrance.Chapter : 0;
    public bool InArena => Bound && LocalFeet.z > 101f && LocalFeet.z < 130f;
    public bool Assisted => entrance && entrance.Assisted;
    public int PuzzleErrors { get; private set; }

    MarshmallowHollowEntrance entrance;
    ThirdPersonPlayerController actor;
    PlayerHealth health;
    PlayerDeathRespawn death;
    CharacterController body;
    GravityWristbandAbility gravity;
    MarshmallowHollowArt art;
    MarshmallowHollowGuardian guardian;
    Transform guide, guideVisual, hopper, shortcut, upperShortcut, reward;
    ChickenBroAI hopperAI;
    CharacterController hopperBody;
    Transform[] gates = new Transform[4], prisms = new Transform[3], floorSwitches = new Transform[4];
    readonly Transform[] pressureFaces = new Transform[4];
    readonly Renderer[] pressureRenderers = new Renderer[4];
    Text[] prismText = new Text[3];
    Transform[] archiveTiles = new Transform[3];
    readonly int[] prism = new int[3];
    readonly Mark[] archiveKeys = { Mark.Slope, Mark.Plane, Mark.Falling };
    readonly IslandRespawnPoint[] checkpoints = new IslandRespawnPoint[5];
    Text archiveStatus, rewardInscription;
    readonly GameObject[] captions = new GameObject[3];
    GameObject journalCard, hudCard, exitCard;
    Text hud, journal, exitPrompt;
    int sequence, checkpoint, pressedSwitch = -1;
    int renderedChapter = -1;
    float messageUntil, hopperTime = -1f;
    string message = "";
    bool journalOpen, built;

    Vector3 LocalFeet => transform.InverseTransformPoint(body.bounds.center - Vector3.up * body.bounds.extents.y);

    public void Bind(MarshmallowHollowEntrance owner, ThirdPersonPlayerController player)
    {
        entrance = owner;
        actor = player;
        body = actor.GetComponent<CharacterController>();
        health = actor.GetComponent<PlayerHealth>();
        death = actor.GetComponent<PlayerDeathRespawn>();
        gravity = actor.GetComponent<GravityWristbandAbility>();
        if (!built) Build();
        health.OnDeath.AddListener(OnPlayerDeath);
        death.Respawned += OnRespawn;
        owner.PlaceAt(transform.TransformPoint(Checkpoints[0]), transform.rotation);
        death.SetCheckpoint(checkpoints[0]);
        RefreshChapter();
    }

    public void Unbind()
    {
        if (health) health.OnDeath.RemoveListener(OnPlayerDeath);
        if (death) death.Respawned -= OnRespawn;
        if (hudCard) hudCard.SetActive(false);
        if (journalCard) journalCard.SetActive(false);
        if (exitCard) exitCard.SetActive(false);
        entrance = null;
        actor = null;
    }

    void Build()
    {
        built = true;
        transform.position = Origin;
        art = new MarshmallowHollowArt(marshmallowMesh);
        BuildShell();
        BuildLobby();
        BuildMine();
        BuildArchive();
        BuildArena();
        BuildGallery();
        BuildUi();
        for (int i=0;i<floorSwitches.Length;i++)
        {
            pressureFaces[i]=floorSwitches[i].GetChild(1);
            pressureRenderers[i]=pressureFaces[i].GetComponentInChildren<Renderer>();
        }
        for (int i = 0; i < checkpoints.Length; i++)
        {
            var point = MarshmallowHollowArt.Node(transform, "Hollow checkpoint " + (i + 1), Checkpoints[i]);
            var checkpointMarker = point.gameObject.AddComponent<IslandRespawnPoint>();
            checkpointMarker.pointName = i == 3 ? "Kiln antechamber" : "Marshmallow Hollow";
            checkpointMarker.isDefault = checkpointMarker.useAsFallback = false;
            checkpointMarker.activationRadius = 0f; // Activated by crossing the room threshold.
            checkpoints[i] = checkpointMarker;
            art.Rounded(point, "Checkpoint inlay", new Vector3(-2.9f,.09f,0), new Vector3(1,.18f,1), art.Mint);
        }
        if (guidePrefab)
        {
            guide = Instantiate(guidePrefab, transform.TransformPoint(new Vector3(-4,0,8)), transform.rotation, transform).transform;
            guide.name = "Pip - hollow guide";
            guideVisual = guide.Find("Bouncy visual");
        }
    }

    void BuildShell()
    {
        Room("Sugarstone vestibule", 0, -2, 30, 24, 15);
        Room("Elastic mine", -2, 30, 56, 24, 21);
        Room("Crystal overlook", 8, 56, 69, 24, 13);
        Room("Echo archive", 8, 69, 100, 24, 14);
        Room("Kiln chamber", 8, 100, 130, 34, 15);
        Room("Last stone wall", 8, 130, 150, 24, 13);
        art.Box(transform, "Back wall", new Vector3(0,6.5f,-2.2f), new Vector3(24,15,1.2f), art.Shadow);
        art.Box(transform, "End wall", new Vector3(0,14.5f,150.5f), new Vector3(24,13,1.2f), art.Stone);
        for (int i = 0; i < 4; i++)
        {
            float z = DoorPositions[i];
            float y = i == 0 ? 0 : 8;
            float width = i == 0 ? 7.6f : 9.5f;
            float outer = i >= 2 ? 18.3f : 13.4f;
            art.Arch(transform, new Vector3(0,y,z-.95f), width+2.2f, 8.4f, art.Stone);
            for (int side = -1; side <= 1; side += 2)
            {
                art.Box(transform, "Bulkhead wing", new Vector3(side * (width*.5f+outer)*.5f,y+6.9f,z),
                    new Vector3(outer-width*.5f+.06f,14.2f,1.55f), art.Stone);
                art.Lantern(transform,new Vector3(side*(width*.5f+1.85f),y+3.28f,z-1.3f),
                    art.Amber,new Color(1,.58f,.34f),9);
            }
            art.Box(transform, "Sealed roof seam", new Vector3(0,y+13.9f,z), new Vector3(outer*2,8.5f,1.66f), art.Shadow);
            gates[i] = art.Box(transform, "Sealed sugarstone door " + (i+1), new Vector3(0,y+4.8f,z),
                new Vector3(width+.10f,9.6f,1.10f), art.Copper);
        }
        // The top of the mine is a shelf, with a smooth lip at the last jump.
        art.Box(transform, "High shelf face", new Vector3(0,2.5f,56.45f), new Vector3(24,10.75f,.85f), art.Stone);
    }

    void Room(string name, float y, float start, float end, float width, float height)
    {
        var room = MarshmallowHollowArt.Node(transform, name, Vector3.zero);
        float span = end - start, middle = (start + end) * .5f;
        art.Box(room, "Solid sugarstone floor", new Vector3(0,y-.66f,middle), new Vector3(width,1.32f,span+.12f), art.Stone);
        art.Box(room, "Cave roof", new Vector3(0,y+height,middle), new Vector3(width+2.4f,2.5f,span+.35f), art.Shadow);
        for (int side = -1; side <= 1; side += 2)
        {
            art.Box(room, "Solid cave wall", new Vector3(side * width*.5f,y+height*.5f,middle),
                new Vector3(1.9f,height,span+.2f), art.Shadow);
            for (int i = 0; i < 3; i++)
            {
                float z = Mathf.Lerp(start+3,end-3,(i+.5f)/3);
                float x = side*(width*.5f-1.45f);
                var rib = art.Rounded(room, "Scalloped sugarstone", new Vector3(x,y+height*.46f,z),
                    new Vector3(3.9f,height*.98f,span*.28f), i % 2 == 0 ? art.Stone : art.Sugar, true);
                rib.localRotation = Quaternion.Euler(side*6, i*32, side*8);
                var cluster = new Vector3(side*(width*.5f-4.2f),y+.63f,z+1);
                art.Crystal(room,cluster,1.05f+i*.17f,i==1?art.Rose:art.Mint);
                art.Crystal(room,cluster+new Vector3(side*.67f,-.27f,-.52f),.74f,art.Amber).Rotate(0,52,26);
                art.Crystal(room,cluster+new Vector3(-side*.32f,-.31f,.58f),.63f,art.Mint).Rotate(14,60,-34);
                var seam = art.Rounded(room,"Mineral crust",cluster+new Vector3(side*1.2f,-.56f,-.4f),
                    new Vector3(2.7f,.22f,3.5f),art.Sugar);
                seam.localRotation=Quaternion.Euler(0,side*19+i*28,0);
            }
            art.Lantern(room,new Vector3(side*(width*.5f-4.1f),y+3.7f,middle-3),
                side<0?art.Mint:art.Amber,side<0?new Color(.47f,.85f,1):new Color(1,.67f,.43f), 16);
        }
    }

    void BuildLobby()
    {
        art.Arch(transform, new Vector3(0,0,1), 7.8f, 7.2f, art.Stone);
        var exitSign = MarshmallowHollowArt.Node(transform, "Interior exit sign", new Vector3(0,4.55f,1.95f));
        exitSign.localRotation = Quaternion.Euler(0,180,0);
        art.Panel(exitSign, "SURFACE EXIT", Vector3.zero, 4.55f,.72f);
        art.Panel(transform, "I   THE FIRST KILNS", new Vector3(0,6.6f,26.1f), 12,.88f);
        var wall = CarvingWall(new Vector3(-7.65f,3.7f,18.5f), 0);
        Relief(wall,Mark.Wave, new Vector3(-5.5f,0,0), art.Mint);
        Relief(wall,Mark.Kiln, Vector3.zero, art.Amber);
        Relief(wall,Mark.Island, new Vector3(5.5f,0,0), art.Rose);
        floorSwitches[0] = FloorSwitch(new Vector3(0,0,21), art.Mint);
        if (Chapter < 1)
        {
            Spawn(scoutPrefab, "Kiln scout", new Vector3(-6,0,13), 54, 8, 12);
            Spawn(scoutPrefab, "Kiln scout", new Vector3(6,0,18), 54, 8, 12);
        }
    }

    void BuildMine()
    {
        JumpPad(new Vector3(0,0,33), new Vector3(9.4f,.80f,6.4f));
        JumpPad(new Vector3(-2.9f,1.95f,39), new Vector3(6.6f,.9f,5.6f));
        JumpPad(new Vector3(2.2f,4.1f,45), new Vector3(6.4f,.9f,5.6f));
        JumpPad(new Vector3(-2.0f,6.2f,50.7f), new Vector3(6.8f,.9f,5.5f));
        JumpPad(new Vector3(0,7.25f,55.1f), new Vector3(7.5f,.9f,4.8f));
        JumpPad(new Vector3(7.5f,-.6f,39), new Vector3(4.4f,.9f,4.6f));
        shortcut = MarshmallowHollowArt.Node(transform, "Restored mine lift", new Vector3(6.7f,0,33));
        art.Rounded(shortcut, "Lift stone", new Vector3(0,-.25f,0), new Vector3(3.4f,.52f,3.6f), art.Mint, true);
        art.Panel(shortcut, "RETURN LIFT", new Vector3(0,2f,1.7f), 3.55f,.67f);
        upperShortcut = MarshmallowHollowArt.Node(transform,"Upper return lift",new Vector3(6,8,60));
        art.Rounded(upperShortcut,"Lift disc",new Vector3(0,-.11f,0),new Vector3(3.25f,.26f,3.4f),art.Mint);
        art.Panel(transform, "II   THE ANCHOR\nMatch the engraved directions", new Vector3(0,5f,34), 10.5f,1.3f);
        for (int i = 0; i < prisms.Length; i++)
        {
            Vector3 p = new Vector3((i-1)*5.6f,8,63.4f);
            var pedestal = art.Rounded(transform, "Anchor prism pedestal", p + Vector3.up*.63f, new Vector3(2.2f,1.26f,2.1f), art.Stone, true);
            prisms[i] = MarshmallowHollowArt.Node(pedestal, "Rotating prism " + (i+1), Vector3.up*1.42f);
            art.Crystal(prisms[i], Vector3.zero, 1.12f, i == 1 ? art.Rose : art.Mint);
            art.Box(prisms[i], "Directional notch", new Vector3(0,0,-.82f), new Vector3(.25f,.32f,1.0f), art.Amber, false);
            art.Panel(transform, "ENGRAVING   " + Compass[PrismAnswer[i]], p + new Vector3(0,3.65f,.78f), 3.9f,.70f);
            prismText[i] = art.Panel(transform, "PRISM   N", p + new Vector3(0,1.08f,-1.25f), 2.4f,.57f);
        }
        floorSwitches[1] = FloorSwitch(new Vector3(0,8,59.4f), art.Amber);
        var wall = CarvingWall(new Vector3(-7.65f,11.9f,63), 1, 10);
        Relief(wall,Mark.Island,Vector3.zero,art.Mint);
        if (Chapter < 2)
        {
            hopper = Spawn(hopperPrefab, "Springtail", new Vector3(7.8f,-2,35.9f), 66, 9, 13);
            if (hopper) { hopperAI = hopper.GetComponent<ChickenBroAI>(); hopperBody = hopper.GetComponent<CharacterController>(); }
            var husk = Spawn(huskPrefab, "Sugar husk", new Vector3(-7.6f,-2,46), 105, 12, 14);
            if (husk) art.Rounded(husk, "Crystallized shell", new Vector3(0,1.3f,.58f), new Vector3(1.22f,1.05f,.74f), art.Copper);
        }
    }

    void BuildArchive()
    {
        art.Panel(transform, "III   THE CHANGED ROUTE", new Vector3(0,14.4f,88.9f), 13,.86f);
        var wall = CarvingWall(new Vector3(-7.65f,12.45f,87), 2);
        Relief(wall,Mark.Plane, new Vector3(-5.5f,0,0), art.Amber);
        Relief(wall,Mark.Falling, Vector3.zero, art.Mint);
        Relief(wall,Mark.Slope, new Vector3(5.5f,0,0), art.Rose);
        art.Glyph(captions[2].transform,Mark.Club,new Vector3(0,4.03f,-.39f),.7f,art.Amber);
        for (int i = 0; i < archiveTiles.Length; i++)
        {
            Vector3 p = new Vector3((i-1)*5.1f,8,83);
            archiveTiles[i] = art.Rounded(transform, archiveKeys[i]+" tile", p+Vector3.up*.72f, new Vector3(2.45f,1.44f,1.42f), art.Copper, true);
            art.Glyph(archiveTiles[i],archiveKeys[i], new Vector3(0,.16f,-.76f), .92f, art.Amber);
        }
        archiveStatus = art.Panel(transform,"CHRONICLE   0 / 3",new Vector3(0,9.4f,89.0f), 7.8f,.8f);
        if (Chapter < 3) Spawn(scoutPrefab, "Archive sentry", new Vector3(-7,8,73.5f), 65, 9, 7);
        art.Panel(transform, "KILN CHECKPOINT", new Vector3(0,11.7f,97.3f), 6.4f,.70f);
    }

    void BuildArena()
    {
        art.Rounded(transform,"Scorched kiln foundation",new Vector3(0,7.97f,117),new Vector3(14.8f,.10f,14.8f),art.Shadow);
        for(int i=0;i<12;i++)
        {
            float angle=i*Mathf.PI/6f;
            var at=new Vector3(Mathf.Sin(angle)*7.8f,8.035f,117+Mathf.Cos(angle)*7.8f);
            art.Box(transform,"Copper kiln inlay",at,new Vector3(.52f,.075f,1.17f),art.Copper,false).localRotation=Quaternion.Euler(0,i*30,0);
        }
        floorSwitches[2] = FloorSwitch(new Vector3(-9,8,113.5f), art.Amber);
        floorSwitches[3] = FloorSwitch(new Vector3(9,8,117), art.Amber);
        if (Chapter >= 4) return;
        var boss = MarshmallowHollowArt.Node(transform, "The Kiln Guardian", new Vector3(0,8,117));
        boss.gameObject.tag = "Enemy";
        guardian = boss.gameObject.AddComponent<MarshmallowHollowGuardian>();
        guardian.Build(this, art);
    }

    void BuildGallery()
    {
        art.Panel(transform, "IV   THE LAST PANEL", new Vector3(0,14.8f,148.9f), 14.3f,.9f);
        art.Panel(transform, "", new Vector3(-5.8f,11.65f,148.8f), 7.9f,3.55f);
        art.Panel(transform, "", new Vector3(5.8f,11.65f,148.8f), 7.9f,3.55f);
        art.Glyph(transform, Mark.Waymark, new Vector3(0,12.05f,148.59f), 1.37f, art.Mint);
        reward = art.Rounded(transform, "Waymark rubbing and cave supplies", new Vector3(0,8.7f,144.5f),
            new Vector3(3.05f,1.4f,1.65f), art.Copper, true);
        art.Glyph(reward, Mark.Waymark, new Vector3(0,.18f,-.85f), .72f, art.Amber);
        rewardInscription = art.Panel(transform, "THE STRANGER'S MARK", new Vector3(0,10.1f,145.3f), 6.6f,.74f);
        art.Panel(transform, "SURFACE LIFT", new Vector3(7.2f,11.0f,141.65f), 4.6f,.76f);
        art.Rounded(transform,"Surface lift disc",new Vector3(7.2f,7.88f,139.8f),new Vector3(3.5f,.24f,3.5f),art.Mint,true);
    }

    Transform CarvingWall(Vector3 position, int chapter, float width=16.3f)
    {
        var wall=MarshmallowHollowArt.Node(transform,"History wall " + (chapter+1),position);
        wall.localRotation=Quaternion.Euler(0,-90,0);
        art.Box(wall,"Stone slab",new Vector3(0,-.38f,.55f),new Vector3(width,4.95f,.90f),art.Shadow,true);
        captions[chapter]=art.Panel(wall,WallNotes[chapter],new Vector3(0,-2.15f,-.1f),width-.55f,1.5f).transform.parent.parent.gameObject;
        return wall;
    }
    void Relief(Transform wall, Mark mark, Vector3 p, Material tint)
    {
        art.Rounded(wall,"Carved stone panel",p,new Vector3(4.55f,3.45f,.48f),art.Ink,true);
        art.Glyph(wall,mark,p-Vector3.forward*.29f,1.62f,tint);
    }
    void JumpPad(Vector3 top, Vector3 size)
    {
        art.Rounded(transform,"Soft marshmallow ledge",top-Vector3.up*(size.y*.5f),size,art.Sugar,true);
        art.Rounded(transform,"Bright marshmallow rim",top-Vector3.up*.06f,new Vector3(size.x*.87f,.16f,size.z*.86f),art.Mint);
    }
    Transform FloorSwitch(Vector3 p, Material tint)
    {
        var root = MarshmallowHollowArt.Node(transform,"Weight switch",p);
        art.Rounded(root,"Copper switch seat",new Vector3(0,-.13f,0),new Vector3(4.3f,.27f,3.95f),art.Copper);
        art.Rounded(root,"Soft pressure surface",new Vector3(0,-.025f,0),new Vector3(3.85f,.13f,3.55f),tint);
        return root;
    }

    Transform Spawn(GameObject prefab, string enemyName, Vector3 p, float hp, float damage, float sight)
    {
        if (!prefab) return null;
        var unit = Instantiate(prefab,transform.TransformPoint(p),transform.rotation,transform);
        unit.name = enemyName;
        var enemyHealth = unit.GetComponent<Health>();
        if (enemyHealth) enemyHealth.currentHealth = enemyHealth.maxHealth = hp;
        var ai = unit.GetComponent<ChickenBroAI>();
        if (ai)
        {
            ai.sightRange = sight; ai.giveUpRange = sight + 8; ai.strikeDamage = damage; ai.lungeDamage = damage+3;
            ai.attackCooldown = 1.7f;
        }
        var visual = unit.GetComponent<MarshmallowEnemyVisual>();
        if (visual) { visual.redEyeRadius = sight; visual.chaseStopRadius = sight+8; }
        return unit.transform;
    }

    void BuildUi()
    {
        var root = new GameObject("Hollow guidance",typeof(RectTransform),typeof(Canvas));
        root.transform.SetParent(transform,false);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 87;
        GameUITheme.Scale(canvas);
        var status = GameUITheme.Rect(root.transform,"Exploration objective",new Vector2(0,1),new Vector2(510,150));
        status.pivot=new Vector2(0,1); status.anchoredPosition=new Vector2(28,-161);
        GameUITheme.Image(status,GameUITheme.Surface);
        hud = GameUITheme.Label(status,"Pip",19,new Vector2(0,-75),new Vector2(476,143),TextAnchor.MiddleCenter);
        hudCard = status.gameObject;
        var exit = GameUITheme.Rect(root.transform,"Hollow exit prompt",new Vector2(.5f,0),new Vector2(510,52));
        exit.anchoredPosition = new Vector2(0,107);
        GameUITheme.Image(exit,GameUITheme.Surface);
        exitPrompt = GameUITheme.Label(exit,"Return to surface",21,new Vector2(0,-26),new Vector2(485,48),TextAnchor.MiddleCenter);
        exitCard = exit.gameObject;
        exitCard.SetActive(false);
        var notes = GameUITheme.Rect(root.transform,"Wall journal",new Vector2(1,.5f),new Vector2(515,476));
        notes.pivot=new Vector2(1,.5f); notes.anchoredPosition=new Vector2(-28,0);
        GameUITheme.Image(notes,new Color(.055f,.040f,.074f,.96f));
        journal=GameUITheme.Label(notes,"Recorded murals",20,new Vector2(0,-238),new Vector2(465,433),TextAnchor.UpperLeft);
        journalCard=notes.gameObject; journalCard.SetActive(false);
    }

    void Update()
    {
        if (!Bound) return;
        bool show = actor.isActiveAndEnabled && !actor.ExternalControlLock && health.currentHealth>0 && !GameUITheme.ModalOpen;
        hudCard.SetActive(show);
        journalCard.SetActive(show && journalOpen);
        if (!show)
        {
            exitCard.SetActive(false);
            return;
        }
        Vector3 feet = LocalFeet;
        bool atExit = Near(feet,EntranceExit,5.5f) || Chapter==4 && Near(feet,new Vector3(7.2f,8,139.8f),3.6f);
        exitCard.SetActive(atExit);
        if (atExit) exitPrompt.text = GameControls.Prompt(GameControls.Command.Interact) + "   RETURN TO MARSHMALLOW ISLAND";
        if (!entrance.CanAct())
        {
            ShowObjective("");
            return;
        }
        if (atExit && GameControls.Down(GameControls.Command.Interact))
        {
            entrance.Leave();
            return;
        }
        int step = 0;
        for (int i=1;i<Checkpoints.Length && Chapter>=i;i++)
            if (feet.z>=Checkpoints[i].z-1.4f) step=i;
        if (step>checkpoint)
        {
            checkpoint=step;
            death.SetCheckpoint(checkpoints[checkpoint]);
            if (step==3) Notify("KILN CHECKPOINT SET", 2.8f);
        }
        if (feet.y < -7f || Mathf.Abs(feet.x) > 25 || feet.z < -5 || feet.z > 154)
        {
            entrance.PlaceAt(checkpoints[checkpoint].GroundPosition, transform.rotation);
            return;
        }
        MoveGuide(feet);
        AnimateHopper(feet);
        RefreshChapter();
        TickSwitches(feet);
        string context = "";
        if (Chapter==3 && entrance.GuardianDefeats>=3 && !Assisted && Near(feet,Checkpoints[3],5.5f))
        {
            context = "ASK PIP FOR HELP";
            if (GameControls.Down(GameControls.Command.Interact))
            {
                entrance.EnableAssistance();
                Notify("PIP  ·  I loosened the braces. The guardian will stay open longer.");
            }
        }
        else if (Chapter>=2 && Near(feet,shortcut.localPosition,3.15f))
        {
            context = "USE THE RESTORED LIFT";
            if (GameControls.Down(GameControls.Command.Interact))
                entrance.PlaceAt(transform.TransformPoint(new Vector3(6,8,60)),transform.rotation);
        }
        else if (Chapter>=2 && Near(feet,upperShortcut.localPosition,1.85f))
        {
            context = "TAKE THE LIFT DOWN";
            if (GameControls.Down(GameControls.Command.Interact))
                entrance.PlaceAt(transform.TransformPoint(shortcut.localPosition),transform.rotation);
        }
        else if (Chapter==1)
        {
            if (!gravity.NormalGravity && body.isGrounded && Near(feet, Checkpoints[1], 3.7f))
            {
                context = "BOUNCE  ·  AIM TOWARD THE FIRST LEDGE";
                if (GameControls.Down(GameControls.Command.Interact) && actor.TryCandySpring(4.75f))
                    GameplayAudio.Play(GameplayAudio.Cue.Pickup, actor.transform.position, true);
            }
            else
            {
                int index = Closest(feet, prisms, 2.85f);
                if (index>=0)
                {
                    context = "TURN PRISM " + (index+1);
                    if (GameControls.Down(GameControls.Command.Interact)) RotatePrism(index);
                }
            }
        }
        else if (Chapter==2)
        {
            int index = Closest(feet, archiveTiles, 2.75f);
            if (index>=0)
            {
                context = "TOUCH " + archiveKeys[index].ToString().ToUpperInvariant() + " TILE";
                if (GameControls.Down(GameControls.Command.Interact)) SelectTile(archiveKeys[index]);
            }
        }
        else if (Chapter==4 && Near(feet,reward.localPosition,3.6f))
        {
            context = entrance.RewardClaimed ? "READ THE RUBBING" : entrance.WaymarkCopied ?
                "TAKE THE REMAINING SUPPLIES" : "TAKE THE RUBBING AND SUPPLIES";
            if (GameControls.Down(GameControls.Command.Interact))
            {
                int claimed = entrance.ClaimReward();
                Notify(claimed>0 ? "MARK COPIED TO THE JOURNAL  ·  +" + claimed + " HEALING " + (claimed==1 ? "PACK" : "PACKS") :
                    entrance.RewardClaimed ? WallNotes[3] : "MARK COPIED TO THE JOURNAL. THE CACHE WILL HOLD YOUR SUPPLIES.");
                RefreshCache();
            }
        }
        if (Keyboard.current!=null && Keyboard.current.jKey.wasPressedThisFrame) journalOpen=!journalOpen;
        journalCard.SetActive(journalOpen);
        if (journalOpen) journal.text="HOLLOW JOURNAL   ·   J CLOSE\n\n" +
            (Chapter==0?"Read the first wall to begin.":string.Join("\n\n", WallNotes, 0, Chapter)) +
            (entrance.WaymarkCopied ? "\n\nRubbing filed for the next voyage." : "");
        ShowObjective(context);
    }

    void ShowObjective(string context)
    {
        string instruction = context.Length>0 ? GameControls.Prompt(GameControls.Command.Interact)+"  "+context :
            Time.unscaledTime<messageUntil ? message : Hints[Chapter];
        string objective = entrance.WaymarkCopied ? "Return to Marshmallow Island" : Objectives[Chapter];
        if (entrance.WaymarkCopied && context.Length==0 && Time.unscaledTime>=messageUntil)
            instruction = "Use the surface lift or return to the entrance.";
        hud.text = "MARSHMALLOW HOLLOW   /   " + (Chapter<4 ? (Chapter+1)+" OF 4" : "THE STRANGER'S MARK") + "\n" + objective + "\n\n" +
            instruction + "\nJ  Journal   ·   " + GameControls.Prompt(GameControls.Command.Gravity) + "  Gravity";
    }

    static bool Near(Vector3 feet, Vector3 at, float radius)
    {
        var delta=feet-at;
        return Mathf.Abs(delta.y)<2f && delta.x*delta.x+delta.z*delta.z<radius*radius;
    }
    int Closest(Vector3 feet, Transform[] options, float radius)
    {
        int result=-1; float best=radius*radius;
        for (int i=0;i<options.Length;i++)
        {
            Vector3 point=transform.InverseTransformPoint(options[i].position);
            Vector3 delta=feet-point;
            float distance=delta.x*delta.x+delta.z*delta.z;
            if (Mathf.Abs(delta.y)<3.0f && distance<best) { best=distance; result=i; }
        }
        return result;
    }

    void TickSwitches(Vector3 feet)
    {
        int press=-1;
        for (int i=0;i<floorSwitches.Length;i++)
        {
            bool enabled = i < 2 ? Chapter == i : Chapter == 3;
            bool over=enabled && Near(feet,floorSwitches[i].localPosition,1.65f) && body.isGrounded;
            float depth=over ? (gravity.NormalGravity ? -.19f : -.063f) : 0;
            var surface = pressureFaces[i];
            if (i>=2)
            {
                var tint=guardian && guardian.CanOpen ? art.Mint : art.Copper;
                if (pressureRenderers[i].sharedMaterial!=tint) pressureRenderers[i].sharedMaterial=tint;
            }
            Vector3 p=surface.localPosition;
            p.y=Mathf.MoveTowards(p.y,-.025f+depth,Time.deltaTime*2.3f);
            surface.localPosition=p;
            if (over && gravity.NormalGravity) press=i;
        }
        if (press==pressedSwitch && (press<2 || !guardian || !guardian.CanOpen)) return;
        pressedSwitch=press;
        switch (press)
        {
            case 0:
                Advance(1,WallNotes[0]);
                break;
            case 1:
                if (prism[0]==PrismAnswer[0] && prism[1]==PrismAnswer[1] && prism[2]==PrismAnswer[2])
                    Advance(2,WallNotes[1]);
                else
                {
                    PuzzleErrors++;
                    Notify("PIP  ·  Check the letters over each prism. The old anchor won't lock until all three match.");
                }
                break;
            case 2: case 3:
                if (guardian && !guardian.TryOpen())
                    Notify("PIP  ·  Wait for the floor wave to pass, then press the glowing switch.",2.3f);
                break;
        }
    }

    void RotatePrism(int i)
    {
        prism[i]=(prism[i]+1)%4;
        prisms[i].localRotation=Quaternion.Euler(0,90*prism[i],0);
        prismText[i].text="PRISM   "+Compass[prism[i]];
        GameplayAudio.Play(GameplayAudio.Cue.Pickup,prisms[i].position,true);
    }

    void SelectTile(Mark mark)
    {
        if (mark!=ArchiveAnswer[sequence])
        {
            sequence=0; PuzzleErrors++;
            archiveStatus.text="CHRONICLE   0 / 3";
            Notify(PuzzleErrors>=2 ? "PIP  ·  The aircraft is first. Follow the falling figure to the slope." :
                "PIP  ·  Look at where the flight ends.");
            return;
        }
        sequence++;
        archiveStatus.text="CHRONICLE   "+sequence+" / 3";
        GameplayAudio.Play(GameplayAudio.Cue.Pickup,actor.transform.position,true);
        if (sequence==3) Advance(3,WallNotes[2]);
    }

    public void GuardianFell() => Advance(4,WallNotes[3]);
    public void Notify(string text, float duration=5.5f) { message=text; messageUntil=Time.unscaledTime+duration; }
    void Advance(int next, string note)
    {
        entrance.Advance(next);
        RefreshChapter();
        GameplayAudio.Play(GameplayAudio.Cue.Pickup,actor.transform.position,true);
        Notify(note,8);
    }
    void RefreshChapter()
    {
        if (renderedChapter==Chapter) return;
        renderedChapter=Chapter;
        for (int i=0;i<gates.Length;i++) gates[i].gameObject.SetActive(Chapter<=i);
        for (int i=0;i<captions.Length;i++) captions[i].SetActive(Chapter>i);
        shortcut.gameObject.SetActive(Chapter>=2);
        upperShortcut.gameObject.SetActive(Chapter>=2);
        if (Chapter>=3) archiveStatus.text="CHRONICLE   COMPLETE";
        RefreshCache();
    }
    void RefreshCache() => rewardInscription.text = entrance.RewardClaimed ? "RUBBING TAKEN" :
        entrance.WaymarkCopied ? (2-entrance.PacksTaken) + " HEALING SUPPLIES LEFT" : "THE STRANGER'S MARK";

    void MoveGuide(Vector3 feet)
    {
        if (!guide) return;
        Vector3 site;
        switch (Chapter)
        {
            case 0: site=new Vector3(-4,0,feet.z<12?9:20); break;
            case 1: site=feet.y>6.7f?new Vector3(-7,8,61):new Vector3(4,0,33); break;
            case 2: site=new Vector3(-7,8,82); break;
            case 3: site=new Vector3(-4.8f,8,95); break;
            default: site=new Vector3(-4.9f,8,143); break;
        }
        Vector3 at=transform.TransformPoint(site);
        if (Vector3.Distance(at,guide.position)>15)
            guide.position=at;
        else
            guide.position=Vector3.MoveTowards(guide.position,at,Time.deltaTime*5.1f);
        if (guideVisual) guideVisual.localPosition=Vector3.up * (Mathf.Abs(Mathf.Sin(Time.time*4f))*.13f);
        var face=Vector3.ProjectOnPlane(actor.transform.position-guide.position,Vector3.up);
        if (face.sqrMagnitude>.01f) guide.rotation=Quaternion.LookRotation(face);
    }

    void AnimateHopper(Vector3 feet)
    {
        if (!hopper || !hopperAI || !hopperBody || !hopperAI.IsAlive || hopperTime>=1f) return;
        if (hopperTime<0)
        {
            if (Chapter!=1 || feet.z<31 || feet.z>42) return;
            hopperTime=0; hopperAI.enabled=false;
        }
        hopperTime=Mathf.Min(1,hopperTime+Time.deltaTime*.92f);
        var at=Vector3.Lerp(new Vector3(7.8f,-2,35.9f),new Vector3(7.5f,-.57f,39),hopperTime);
        at.y+=Mathf.Sin(hopperTime*Mathf.PI)*2.55f;
        hopperBody.Move(transform.TransformPoint(at)-hopper.position);
        if (hopperTime>=1) hopperAI.enabled=true;
    }

    void OnPlayerDeath()
    {
        if (InArena && Chapter==3) entrance.RecordGuardianDefeat();
    }
    void OnRespawn()
    {
        pressedSwitch=-1;
        if (guardian && Chapter==3) guardian.ResetFight();
    }
    void OnDestroy()
    {
        Unbind();
        art?.Dispose();
    }
}

