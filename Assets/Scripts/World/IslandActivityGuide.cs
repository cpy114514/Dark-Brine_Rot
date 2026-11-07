using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Brief first-visit fairy tips for optional second-island activities.</summary>
[DisallowMultipleComponent]
public sealed class IslandActivityGuide : MonoBehaviour
{
    const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    public int DiscoveredMask { get; private set; }
    public string VisibleTip => card && card.activeSelf ? text.text : "";
    static readonly string[] Tips={
        "Those candy springs can send you up to the stars. Try the sign!",
        "The singing candies are a memory game. Watch their lights and repeat the tune.",
        "Lily looks worried. Maybe she could use a friend like you.",
        "That's the Skyboard rally! Fly through its rings when you have a board."
    };
    ThirdPersonPlayerController player;
    PlayerHealth health;
    CandyBounceGarden bounce;
    CandyMelodyGarden melody;
    MarshmallowFriendsQuest friends;
    SkyboardRingRally rally;
    PigRoninBoss boss;
    GameObject card;
    Text text;
    float nextLookup,nextScan,until;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initial(){for(int i=0;i<SceneManager.sceneCount;i++)Ensure(SceneManager.GetSceneAt(i));}
    static void OnSceneLoaded(Scene s,LoadSceneMode mode)=>Ensure(s);
    static void Ensure(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath)return;
        foreach(var root in scene.GetRootGameObjects())if(root.GetComponentInChildren<IslandActivityGuide>(true))return;
        var go=new GameObject("Fairy activity guide");SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<IslandActivityGuide>();
    }
    void Awake()
    {
        var obj=new GameObject("Fairy discovery tips",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));obj.transform.SetParent(transform,false);
        var canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=83;GameUITheme.Scale(canvas);
        var panel=GameUITheme.Rect(obj.transform,"Discovery tip",new Vector2(.5f,0),new Vector2(680,100));panel.pivot=new Vector2(.5f,0);panel.anchoredPosition=new Vector2(0,28);
        GameUITheme.Image(panel,new Color(.075f,.04f,.12f,.93f));
        text=GameUITheme.Label(panel,"Fairy says",21,new Vector2(-315,-50),new Vector2(630,90),TextAnchor.MiddleLeft);card=panel.gameObject;card.SetActive(false);
    }
    public void RestoreProgress(int mask){DiscoveredMask=mask & 15;until=0;if(card)card.SetActive(false);}
    void Update()
    {
        if(!player && Time.unscaledTime>=nextLookup)
        {
            nextLookup=Time.unscaledTime+1;player=FindFirstObjectByType<ThirdPersonPlayerController>();health=player?player.GetComponent<PlayerHealth>():null;
            bounce=FindFirstObjectByType<CandyBounceGarden>();melody=FindFirstObjectByType<CandyMelodyGarden>();friends=FindFirstObjectByType<MarshmallowFriendsQuest>();rally=FindFirstObjectByType<SkyboardRingRally>();boss=FindFirstObjectByType<PigRoninBoss>();
        }
        if(!player)return;
        bool allowed=Time.timeScale>0 && player.isActiveAndEnabled && (!health || health.currentHealth>0) && !player.ExternalControlLock && !player.ExternalMovementLock && !GameUITheme.ModalOpen && !SecondIslandAwakening.BlocksInputFor(player) && !MarshmallowChallengeGuide.AnyConversation && !(boss && boss.FightActive);
        if(allowed && Time.unscaledTime>=nextScan && Time.unscaledTime>=until)
        {
            nextScan=Time.unscaledTime+.5f;
            for(int i=0;i<Tips.Length;i++)
            {
                if(i==0 && bounce && bounce.RewardClaimed || i==1 && melody && melody.RewardClaimed || i==2 && friends && friends.Stage>0 || i==3 && rally && rally.RewardClaimed){DiscoveredMask|=1<<i;continue;}
                var target=i==0?bounce?bounce.transform:null:i==1?melody?melody.transform:null:i==2?friends?friends.Home:null:rally?rally.StartMarker:null;
                if((DiscoveredMask&(1<<i))!=0 || !target || Mathf.Abs(player.transform.position.y-target.position.y)>15 || Vector3.ProjectOnPlane(player.transform.position-target.position,Vector3.up).sqrMagnitude>22*22)continue;
                DiscoveredMask|=1<<i;text.text="FAIRY  ·  NEW PLACE\n"+Tips[i];until=Time.unscaledTime+6;GameSaveManager.RequestAutosave();break;
            }
        }
        bool show=allowed && Time.unscaledTime<until;if(card.activeSelf!=show)card.SetActive(show);
    }
    public void ShowBossVictory()
    {
        text.text="FAIRY\nHe was guarding the drifting path. The way ahead is open now.";
        until=Time.unscaledTime+9f;nextScan=until;
    }
}
