using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mavis;

/// <summary>Slowly opens Sahur's eyes at the start of the second island.</summary>
[DefaultExecutionOrder(500)]
public sealed class SecondIslandAwakening : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/Second Island.unity";
    static SecondIslandAwakening active;
    static readonly int OpeningId = Shader.PropertyToID("_Opening");
    static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    Material material;
    Image image;
    Canvas overlay;
    bool playing;
    ThirdPersonPlayerController player;
    CharacterController capsule;
    Animator animator;
    Camera view;
    GroundCalibration calibration;
    Collider groundCollider;
    Quaternion slopeRotation;
    Vector3 cameraVelocity;
    Vector3 cinematicCameraPosition;
    Quaternion cinematicCameraRotation;
    bool cameraPoseInitialized;
    float animatorSpeed, groundY, originalRootY;
    bool ownsPose, capsuleWasEnabled;
    bool dialogueShot;
    Transform fairy;
    Vector3 originalPosition;
    Quaternion originalRotation;
    [System.Serializable]
    sealed class GroundCalibration
    {
        public float sourceScale;
        public ContactPose[] poses;
    }
    [System.Serializable]
    sealed class ContactPose { public Vector3[] points; public bool[] valid; }
    public float Opening { get; private set; }
    public float Opacity { get; private set; }
    public bool Playing => playing;
    public bool GettingUp { get; private set; }
    public float GetUpPhase { get; private set; }
    public SecondIslandDialogue Dialogue { get; private set; }
    public static bool BlocksInputFor(ThirdPersonPlayerController player) =>
        active && active.playing && player && player.gameObject.scene == active.gameObject.scene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        active = null;
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitialScene()
    {
        for (int i=0;i<SceneManager.sceneCount;i++) CreateFor(SceneManager.GetSceneAt(i));
    }
    static void SceneLoaded(Scene scene, LoadSceneMode mode) => CreateFor(scene);
    static void CreateFor(Scene scene)
    {
        if (!scene.isLoaded || scene.path != ScenePath || active) return;
        var shader = Resources.Load<Shader>("SecondIslandAwakening/Eyelids");
        if (!shader || !shader.isSupported)
        {
            Debug.LogWarning("[Second Island] Eyelid shader unavailable; skipping opening effect.");
            return;
        }
        var root = new GameObject("Second Island awakening", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(root, scene);
        root.AddComponent<SecondIslandAwakening>();
    }

    void Awake()
    {
        active = this; playing = true;
        overlay = GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 32759; // Loading screen stays above the unopened eyes.
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.matchWidthOrHeight = .5f;
        var cover = new GameObject("Eyelids", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cover.transform.SetParent(transform, false);
        var rect = cover.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image = cover.GetComponent<Image>(); image.raycastTarget = false;
        material = new Material(Resources.Load<Shader>("SecondIslandAwakening/Eyelids"));
        image.material = material;
        SetEyes(0,1);
    }
    void Update()
    {
        // Pausing freezes the sequence, but the pause menu must remain visible.
        if (overlay) overlay.enabled = !PauseSettingsMenu.IsOpen;
    }
    IEnumerator Start()
    {
        // Keep the eyelids closed underneath the existing loading screen.
        yield return null;
        while (SceneLoadingScreen.IsLoading || AdditiveSceneBootstrap.IsLoading || GameSaveManager.IsContinuePending)
            yield return null;
        PrepareGetUp();
        yield return Animate(0,0,1,1,.55f);      // A moment of darkness.
        yield return Animate(0,.36f,1,1,1.05f); // First sleepy glimpse.
        yield return Animate(.36f,.24f,1,1,.22f);
        yield return Animate(.24f,1.38f,1,1,1.55f);
        yield return Animate(1.38f,1.65f,1,0,.45f);
        if (ownsPose)
        {
            // Let the fully opened eyes take in the resting pose before moving.
            yield return WaitWhileUnpaused(.4f);
            GettingUp = true;
            float elapsed=0;
            const float duration=2.6f;
            while (elapsed < duration)
            {
                if (Time.timeScale > 0 && !PauseSettingsMenu.IsOpen)
                {
                    GetUpPhase=Mathf.Clamp01(elapsed/duration);
                    SetGetUpPose(GetUpPhase);
                    elapsed += Time.deltaTime;
                }
                yield return null;
            }
            GetUpPhase=1; SetGetUpPose(1);
            GettingUp=false;
            animator.speed=animatorSpeed;
            animator.CrossFadeInFixedTime("Base Layer.Locomotion",.18f,0,0);
            float startY=player.transform.position.y;
            float targetY=groundY - (capsule.center.y-capsule.height*.5f)*Mathf.Abs(player.transform.lossyScale.y) + .04f;
            elapsed=0;
            while(elapsed<.22f)
            {
                if(Time.timeScale>0 && !PauseSettingsMenu.IsOpen)
                {
                    var p=player.transform.position; p.y=Mathf.Lerp(startY,targetY,Mathf.SmoothStep(0,1,elapsed/.22f));
                    player.transform.position=p; elapsed+=Time.deltaTime;
                }
                yield return null;
            }
            var standing=player.transform.position; standing.y=targetY;
            player.RestoreSavedPose(standing,originalRotation);
            FinishPose();
        }
        foreach(var root in gameObject.scene.GetRootGameObjects())
        {
            if(root.name=="Neal Fairy") { fairy=root.transform; break; }
            var found=root.transform.Find("Neal Fairy");
            if(found){fairy=found;break;}
        }
        dialogueShot=player && view;
        Dialogue=gameObject.AddComponent<SecondIslandDialogue>();
        yield return Dialogue.Play(transform);
        dialogueShot=false;
        if(player && view)player.ContinueCameraFromCurrentView(view,cameraVelocity);
        // Keep the input lock through the frame that dismissed the last line.
        yield return null;
        playing = false;
        Destroy(gameObject);
    }
    void PrepareGetUp()
    {
        foreach(var candidate in FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))
            if(candidate.gameObject.scene==gameObject.scene) { player=candidate; break; }
        var data=Resources.Load<TextAsset>("SecondIslandAwakening/GetUpContacts");
        if (!player || !data) return;
        calibration=JsonUtility.FromJson<GroundCalibration>(data.text);
        player.PrepareRuntime();
        animator=player.CharacterAnimator; capsule=player.GetComponent<CharacterController>(); view=Camera.main;
        if (!animator || !capsule || !view || calibration==null || calibration.poses==null ||
            calibration.poses.Length<2 || !animator.HasState(0,Animator.StringToHash("Base Layer.Shore Get Up")) ||
            player.Motion==null || player.Motion.Owner!=null) return;
        var origin=player.transform.position+Vector3.up*25;
        var hits=Physics.RaycastAll(origin,Vector3.down,80,~0,QueryTriggerInteraction.Ignore);
        float nearest=float.PositiveInfinity;
        foreach(var hit in hits)
        {
            if(hit.collider.transform.IsChildOf(player.transform) || hit.normal.y<.45f || hit.distance>=nearest) continue;
            nearest=hit.distance; groundY=hit.point.y; groundCollider=hit.collider;
            slopeRotation=Quaternion.FromToRotation(Vector3.up,hit.normal)*player.transform.rotation;
        }
        if(float.IsPositiveInfinity(nearest)) return;
        originalPosition=player.transform.position; originalRootY=originalPosition.y; originalRotation=player.transform.rotation;
        animatorSpeed=animator.speed; capsuleWasEnabled=capsule.enabled;
        if(!player.Motion.Claim(this,SahurControlState.Cinematic)) return;
        ownsPose=true; capsule.enabled=false;
        animator.speed=0;
        animator.SetFloat("ArrivalGetUpPhase",0);
        animator.Play("Base Layer.Shore Get Up",0,0);
        SetGetUpPose(0);
    }
    float RootHeightOnGround(float phase, Quaternion rotation)
    {
        float index=Mathf.Clamp01(phase)*(calibration.poses.Length-1);
        int a=Mathf.FloorToInt(index), b=Mathf.Min(a+1,calibration.poses.Length-1);
        var first=calibration.poses[a];var next=calibration.poses[b];
        float scale=Mathf.Abs(player.transform.lossyScale.y)/Mathf.Max(.01f,calibration.sourceScale);
        float h0=HeightForContactPose(first,rotation,scale);
        float h1=a==b ? h0 : HeightForContactPose(next,rotation,scale);
        if(float.IsNegativeInfinity(h0))return float.IsNegativeInfinity(h1)?groundY+3.33f*scale:h1;
        if(float.IsNegativeInfinity(h1))return h0;
        // Interpolate whole contact heights: cells can enter or leave the
        // footprint between samples, but their appearance must not pop Sahur.
        return Mathf.Lerp(h0,h1,index-a);
    }
    float HeightForContactPose(ContactPose pose, Quaternion rotation, float scale)
    {
        float height=float.NegativeInfinity;
        for(int i=0;i<pose.points.Length;i++)
        {
            if(!pose.valid[i])continue;
            Vector3 offset=rotation*pose.points[i]*scale;
            var origin=new Vector3(originalPosition.x+offset.x,originalPosition.y+25,originalPosition.z+offset.z);
            if(groundCollider && groundCollider.Raycast(new Ray(origin,Vector3.down),out var hit,80))
                height=Mathf.Max(height,hit.point.y-offset.y);
        }
        return height;
    }
    void SetGetUpPose(float phase)
    {
        if(!ownsPose || !player || !animator)return;
        animator.SetFloat("ArrivalGetUpPhase",phase);
        animator.Update(0);
        float upright=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,.8f,phase));
        Quaternion rotation=Quaternion.Slerp(slopeRotation,originalRotation,upright);
        var p=originalPosition; p.y=RootHeightOnGround(phase,rotation)+.04f;
        player.transform.SetPositionAndRotation(p,rotation);
    }
    void LateUpdate()
    {
        if ((!ownsPose && !dialogueShot) || !player || !animator || !view || !animator.isHuman) return;
        var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
        var head=animator.GetBoneTransform(HumanBodyBones.Head);
        if (!hips || !head) return;
        Vector3 focus=(hips.position+head.position)*.5f;
        if(dialogueShot && fairy && fairy.gameObject.activeInHierarchy)
            focus=Vector3.Lerp(focus,fairy.position+Vector3.up*Mathf.Abs(fairy.lossyScale.y)*.55f,.4f);
        Vector3 direction=(-player.transform.forward*4f+player.transform.right*9f+Vector3.up*4.5f).normalized;
        Vector3 at=focus+direction*11f;
        Quaternion look=Quaternion.LookRotation(focus-at,Vector3.up);
        if(!cameraPoseInitialized)
        {
            cameraPoseInitialized=true;
            cinematicCameraPosition=at;
            cinematicCameraRotation=look;
        }
        else if(Time.deltaTime>0 && !PauseSettingsMenu.IsOpen)
        {
            cinematicCameraPosition=Vector3.SmoothDamp(cinematicCameraPosition,at,ref cameraVelocity,.14f,Mathf.Infinity,Time.deltaTime);
            cinematicCameraRotation=Quaternion.Slerp(cinematicCameraRotation,look,1-Mathf.Exp(-10*Time.deltaTime));
        }
        view.transform.SetPositionAndRotation(cinematicCameraPosition,cinematicCameraRotation);
    }
    IEnumerator WaitWhileUnpaused(float duration)
    {
        float elapsed=0;
        while(elapsed<duration)
        {
            if(Time.timeScale>0 && !PauseSettingsMenu.IsOpen)elapsed+=Time.deltaTime;
            yield return null;
        }
    }
    void FinishPose()
    {
        if(!ownsPose)return;
        ownsPose=false;
        if(animator)animator.speed=animatorSpeed;
        if(capsule)capsule.enabled=capsuleWasEnabled;
        if(player)
        {
            player.Motion?.Release(this);
            if(view)player.ContinueCameraFromCurrentView(view);
        }
    }
    IEnumerator Animate(float from, float to, float opacityFrom, float opacityTo, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (Time.timeScale <= 0 || PauseSettingsMenu.IsOpen)
            {
                yield return null;
                continue;
            }
            float t = Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed / duration));
            SetEyes(Mathf.Lerp(from,to,t), Mathf.Lerp(opacityFrom,opacityTo,t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetEyes(to,opacityTo);
    }
    void SetEyes(float opening, float opacity)
    {
        Opening = opening; Opacity = opacity;
        if (!material) return;
        material.SetFloat(OpeningId,opening); material.SetFloat(OpacityId,opacity);
    }
    void OnDestroy()
    {
        if(ownsPose && player)
        {
            // Interrupted scene unload/disable must not leave the capsule suspended.
            var p=player.transform.position; p.y=originalRootY;
            player.transform.SetPositionAndRotation(p,originalRotation);
        }
        FinishPose();
        playing = false;
        if (active == this) active = null;
        if (material) Destroy(material);
    }
}
