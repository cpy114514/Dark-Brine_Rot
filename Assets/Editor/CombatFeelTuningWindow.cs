using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Mavis;

public sealed class CombatFeelTuningWindow : EditorWindow
{
    Editor profileEditor;
    Vector2 scroll;
    [MenuItem("Tools/Combat/Sahur Combat Feel")]
    public static void Open()=>GetWindow<CombatFeelTuningWindow>("Sahur Combat Feel");
    void OnGUI()
    {
        EditorGUILayout.HelpBox("Tune the existing club attacks. In Play Mode, spawn a temporary arena. Left Mouse: combo. Hold Right Mouse: charge. WASD: move. Ctrl: dodge. No scene or save data is authored.",MessageType.Info);
        using(new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if(GUILayout.Button("Spawn / Reset Preview Arena")){CombatFeelPreview.Clear();CombatFeelPreview.Spawn();}
            if(GUILayout.Button("Remove Preview Arena"))CombatFeelPreview.Clear();
            if(GUILayout.Button("Toggle Armored Target") && CombatFeelPreview.Target)
            {var response=CombatFeelPreview.Target.GetComponent<CombatImpactResponse>();response.unflinching=!response.unflinching;}
            if(GUILayout.Button("Toggle Blocking Target") && CombatFeelPreview.Target)
            {var response=CombatFeelPreview.Target.GetComponent<CombatImpactResponse>();response.blocksMelee=!response.blocksMelee;}
        }
        var profile=CombatFeelProfile.Default;
        scroll=EditorGUILayout.BeginScrollView(scroll);
        Editor.CreateCachedEditor(profile,null,ref profileEditor);profileEditor.OnInspectorGUI();
        if(CombatFeelPreview.Player)
        {
            var feedback=CombatFeelPreview.Player.GetComponent<CombatHitFeedback>();
            EditorGUILayout.LabelField("Confirmed hits",feedback.HitCount.ToString());
            EditorGUILayout.LabelField("Last result",feedback.LastResult.ToString());
        }
        EditorGUILayout.EndScrollView();
    }
    void OnDisable(){if(profileEditor)DestroyImmediate(profileEditor);}
}

[InitializeOnLoad]
public static class CombatFeelPreview
{
    public static GameObject Player { get; private set; }
    public static GameObject Target { get; private set; }
    public static Camera Camera { get; private set; }
    static GameObject arena;
    static Material floorMaterial;
    static (Behaviour behaviour,bool enabled)[] originals;
    static (GameObject camera,string tag)[] cameras;
    static CursorLockMode cursor;
    static bool cursorVisible;
    static float timeScale;
    static CombatFeelPreview()=>EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingPlayMode)Clear();};
    public static void Spawn()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Preview arena requires Play Mode.");
        if(arena)return;
        cursor=Cursor.lockState;cursorVisible=Cursor.visible;
        timeScale=Time.timeScale;
        originals=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(x=>x.GetType().Assembly==typeof(SahurAttack).Assembly || x is UnityEngine.InputSystem.PlayerInput)
            .Select(x=>((Behaviour)x,x.enabled)).ToArray();
        foreach(var x in originals)x.behaviour.enabled=false;
        cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(x=>(x.gameObject,x.tag)).ToArray();
        foreach(var c in cameras)if(c.tag=="MainCamera")c.camera.tag="Untagged";
        arena=new GameObject("Temporary Combat Feel Preview (not saved)");arena.transform.position=new Vector3(2000,500,2000);
        var cameraObject=new GameObject("Preview gameplay camera");cameraObject.transform.SetParent(arena.transform,false);cameraObject.tag="MainCamera";
        Camera=cameraObject.AddComponent<Camera>();Camera.nearClipPlane=.1f;Camera.farClipPlane=200;Camera.fieldOfView=60;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Preview collision floor";floor.transform.SetParent(arena.transform,false);floor.transform.localPosition=Vector3.down*.5f;floor.transform.localScale=new Vector3(100,1,100);
        floorMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Temporary neutral combat floor",color=new Color(.12f,.13f,.14f)};
        floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
        var light=new GameObject("Preview key light");light.transform.SetParent(arena.transform,false);light.transform.rotation=Quaternion.Euler(40,-30,0);var sun=light.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.8f;
        Player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),arena.transform);
        Player.AddComponent<GameSaveExcluded>();Player.name="Preview Sahur";
        Player.GetComponent<ThirdPersonPlayerController>().seaLevel=0;
        var motor=Player.GetComponent<ThirdPersonPlayerController>();var capsule=Player.GetComponent<CharacterController>();
        float sole=(capsule.center.y-capsule.height*.5f)*Player.transform.lossyScale.y;
        motor.RestoreSavedPose(arena.transform.position+Vector3.up*(.03f-sole),Quaternion.identity);
        var stamina=Player.GetComponent<PlayerStamina>();stamina.enabled=false;stamina.currentStamina=100;
        Target=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab"),arena.transform.position+Vector3.forward*3.8f,Quaternion.Euler(0,180,0),arena.transform);
        Target.AddComponent<GameSaveExcluded>();Target.name="Preview Nailong";
        Target.GetComponent<NailongAI>().enabled=false;
        var agent=Target.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
        var health=Target.GetComponent<NailongHealth>();health.maxHealth=health.currentHealth=100000;
        Target.AddComponent<CombatImpactResponse>();
        Time.timeScale=1;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Physics.SyncTransforms();
    }
    public static void Clear()
    {
        if(arena)UnityEngine.Object.DestroyImmediate(arena);
        if(floorMaterial)UnityEngine.Object.DestroyImmediate(floorMaterial);floorMaterial=null;
        if(originals!=null)foreach(var x in originals)if(x.behaviour)x.behaviour.enabled=x.enabled;
        if(cameras!=null)foreach(var c in cameras)if(c.camera)c.camera.tag=c.tag;
        if(originals!=null){Time.timeScale=timeScale;Cursor.lockState=cursor;Cursor.visible=cursorVisible;}
        originals=null;cameras=null;arena=Player=Target=null;Camera=null;
    }
}
