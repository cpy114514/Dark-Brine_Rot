using System.Linq;
using Mavis;
using UnityEngine;

/// <summary>Authored, real-time exchanges on the player's current wreck deck, then defeat.</summary>
[DefaultExecutionOrder(900), DisallowMultipleComponent]
public sealed class Story1WreckFinale : MonoBehaviour
{
    public float Elapsed { get; private set; }
    public string CurrentBeat { get; private set; }
    public int HeroCounterCount { get; private set; }
    public int SharkCounterCount { get; private set; }
    public bool HasLostStick { get; private set; }
    public bool IsComplete { get; private set; }
    public const float Duration = 14.4f;
    public float LeadInSeconds { get; private set; } = 1.1f;
    Story1WreckBattle battle;
    ThirdPersonPlayerController hero;
    TralaleroSwimAnimator shark;
    Camera shotCamera;
    Story1WreckPlank board;
    OceanWorld ocean;
    Animator animator;
    Transform stick, projectile;
    Renderer[] held;
    bool[] heldEnabled;
    Mesh stickMesh;
    Vector3 startFeet, initialShark, cameraStart, throwStart, lostStart;
    Quaternion cameraRotation, projectileRotation;
    float cameraFov;
    float swimSeconds;
    bool fromWater;
    int beat = -1, fired;
    string activeAnimation;
    bool threatPlayed;
    Vector3 sharkBeatStart;
    Quaternion sharkBeatRotation;

    public void Initialize(Story1WreckBattle encounter, ThirdPersonPlayerController player,
        TralaleroSwimAnimator enemy, Camera shotCamera, Story1WreckPlank stage, OceanWorld water, bool swimming)
    {
        battle=encounter;hero=player;shark=enemy;this.shotCamera=shotCamera;board=stage;ocean=water;fromWater=swimming;
        animator=hero.CharacterAnimator;
        // Clear charged-attack masks before authored full-body clips take over.
        for(int i=1;i<animator.layerCount;i++)animator.SetLayerWeight(i,0);
        animator.SetFloat("Speed",0);
        startFeet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        initialShark=battle.SharkBody;cameraStart=shotCamera.transform.position;cameraRotation=shotCamera.transform.rotation;cameraFov=shotCamera.fieldOfView;
        stick=hero.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Sahur Stick");
        if(stick!=null)
        {
            held=stick.GetComponentsInChildren<Renderer>(true);heldEnabled=held.Select(r=>r.enabled).ToArray();
            stickMesh=stick.GetComponent<MeshFilter>()?.sharedMesh;
        }
        if(fromWater)
        {
            swimSeconds=Mathf.Max(.2f,Vector3.ProjectOnPlane(startFeet-Deck(-4.3f),Vector3.up).magnitude/(hero.swimSpeed*hero.fastSwimMultiplier));
            LeadInSeconds=Mathf.Max(1.1f,swimSeconds+.95f);
        }
        SetBeat(0);
    }
    Vector3 Forward => Vector3.ProjectOnPlane(board.transform.forward,Vector3.up).normalized;
    Vector3 Side => Vector3.Cross(Vector3.up,Forward);
    Vector3 Deck(float z=0) => board.transform.TransformPoint(new Vector3(0,board.TopLocalY,z/board.Dimensions.z))+Vector3.up*.06f;
    float Water(Vector3 at) => ocean!=null ? ocean.SampleSurfaceHeight(at,Time.time) : hero.seaLevel;
    void Animate(string name, float fade=.08f)
    {
        if(activeAnimation==name)return;activeAnimation=name;
        animator.CrossFadeInFixedTime(name,fade,0,0);
    }
    void SetBeat(int value)
    {
        beat=value;fired=0;sharkBeatStart=battle.SharkBody;sharkBeatRotation=shark.transform.rotation;
        switch(value)
        {
            case 0:CurrentBeat="Regain footing";Animate(fromWater ? "Swim Fast" : "Finale Guard");break;
            case 1:CurrentBeat="Stick counter";Animate("Combo 1");break;
            case 2:CurrentBeat="Dodge tail sweep";Animate("Roll");shark.PlayTailSlap();SharkCounterCount++;break;
            case 3:CurrentBeat="Boomerang counter";Animate("Boomerang Throw");break;
            case 4:CurrentBeat="Jumping counter";Animate("Jump Start");break;
            case 5:CurrentBeat="Block and stagger";Animate("Finale Guard");shark.PlayTailSlap();SharkCounterCount++;break;
            case 6:CurrentBeat="Last stick counter";Animate("Combo 3");break;
            case 7:CurrentBeat="Overwhelming tail strike";Animate("Finale Guard");shark.PlayTailSlap(powerful:true);SharkCounterCount++;break;
        }
    }
    void HeroAt(Vector3 feet, bool backward=false)
    {
        hero.transform.SetPositionAndRotation(feet-Vector3.up*hero.LowestFootWorldOffset,Quaternion.LookRotation(backward ? -Forward : Forward));
    }
    void SharkAt(Vector3 at, float lift=0, float bank=0)
    {
        battle.PoseCinematicShark(at,Quaternion.LookRotation(-Forward)*Quaternion.Euler(0,0,bank),lift);
    }
    void TailAt(Vector3 contact, float travel)
    {
        Quaternion turn=Quaternion.LookRotation(Side);
        Vector3 target=battle.TailContactBody(contact+Vector3.up*2.4f,turn);
        Vector3 approach=sharkBeatStart;
        float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(travel/.65f));
        Vector3 at=Vector3.Lerp(approach,target,t)-Side*(Mathf.Sin(t*Mathf.PI)*2.2f);
        battle.PoseCinematicShark(at,Quaternion.Slerp(sharkBeatRotation,turn,t),absoluteHeight:true);
    }
    void Counter(Vector3 at, bool heavy=false)
    {
        HeroCounterCount++;battle.CinematicImpact(at,true,heavy);
    }
    void Update()
    {
        if(battle==null || IsComplete || PauseSettingsMenu.IsOpen || Time.deltaTime<=0)return;
        Elapsed+=Time.deltaTime;
        float t=Elapsed-LeadInSeconds+1.1f;
        int next=Elapsed<LeadInSeconds ? 0 : t<2.8f ? 1 : t<4.4f ? 2 : t<6.3f ? 3 : t<8.3f ? 4 : t<10 ? 5 : t<11.5f ? 6 : 7;
        if(next!=beat)SetBeat(next);
        Vector3 deck=Deck(), f=Forward;
        switch(beat)
        {
            case 0:
                if(fromWater)
                {
                    Vector3 edge=Deck(-4.3f);edge.y=Water(edge)-2;
                    if(Elapsed<swimSeconds)
                    {
                        HeroAt(Vector3.Lerp(startFeet,edge,Elapsed/swimSeconds));
                        Vector3 toward=Vector3.ProjectOnPlane(edge-startFeet,Vector3.up);
                        if(toward.sqrMagnitude>.01f)hero.transform.rotation=Quaternion.LookRotation(toward);
                    }
                    else
                    {
                        Animate("Wreck Climb");
                        HeroAt(Vector3.Lerp(edge,Deck(-1),Mathf.SmoothStep(0,1,(Elapsed-swimSeconds)/.95f)));
                    }
                }
                else HeroAt(Vector3.Lerp(startFeet,Deck(-1),Mathf.SmoothStep(0,1,Elapsed/LeadInSeconds)));
                SharkAt(Vector3.Lerp(initialShark,deck+f*12,Mathf.SmoothStep(0,1,Elapsed/LeadInSeconds)));
                break;
            case 1:
                float a=t-1.1f;
                HeroAt(Deck(Mathf.Lerp(-1,1.2f,Mathf.SmoothStep(0,1,a/.8f))));
                SharkAt(deck+f*(a<.58f ? 10 : Mathf.Lerp(10,14,Mathf.Clamp01((a-.58f)/.6f))),0,a>.58f ? -8*Mathf.Sin((a-.58f)*4) : 0);
                if(a>=.58f && fired==0){fired=1;Counter(shark.NoseWorldPoint);}
                if(a>.85f)Animate("Finale Guard");
                break;
            case 2:
                float b=t-2.8f;
                HeroAt(Deck(Mathf.Lerp(1.2f,-1.8f,Mathf.SmoothStep(0,1,b/.7f))),b<.85f);
                TailAt(Deck(1.2f),b);
                if(b>=.65f && fired==0){fired=1;battle.CinematicImpact(Deck(1.2f),false);}
                if(b>.85f)Animate("Finale Guard");
                break;
            case 3:
                float c=t-4.4f;HeroAt(Deck(-1.8f));
                SharkAt(deck+f*(c<.82f ? 12 : 12+Mathf.Min(1,(c-.82f)/.6f)*3));
                if(c>=.27f && fired==0){fired=1;CreateProjectile();throwStart=projectile!=null ? projectile.position : hero.transform.position;}
                if(projectile!=null)
                {
                    Vector3 target=shark.NoseWorldPoint;
                    Vector3 centre=c<.82f ? Vector3.Lerp(throwStart,target,Mathf.Clamp01((c-.27f)/.55f)) :
                        Vector3.Lerp(target,GripCentre,Mathf.Clamp01((c-.82f)/.55f));
                    SpinProjectile(centre,c);
                }
                if(c>=.82f && fired==1){fired=2;Counter(shark.NoseWorldPoint);}
                if(c>=1.37f && fired==2){fired=3;CatchStick();Animate("Finale Guard");}
                break;
            case 4:
                float d=t-6.3f;float jump=Mathf.Clamp01(d/1.35f);
                HeroAt(Deck(Mathf.Lerp(-1.8f,2.4f,Mathf.SmoothStep(0,1,jump)))+Vector3.up*(Mathf.Sin(jump*Mathf.PI)*2.7f));
                SharkAt(deck+f*(d<.9f ? 11 : 11+Mathf.Min(1,(d-.9f)/.8f)*4),Mathf.Sin(jump*Mathf.PI)*1.6f);
                if(d>.2f && d<1.35f)Animate("Jump Slash");
                if(d>=.9f && fired==0){fired=1;Counter(shark.NoseWorldPoint,true);}
                if(d>=1.35f)Animate(d<1.65f ? "Land" : "Finale Guard");
                break;
            case 5:
                float e=t-8.3f;TailAt(Deck(2.4f),e);
                HeroAt(Deck(e<.65f ? 2.4f : Mathf.Lerp(2.4f,.8f,Mathf.Clamp01((e-.65f)/.7f))));
                if(e>=.65f && fired==0){fired=1;Animate("Finale Stagger",.03f);battle.CinematicImpact(Deck(2.4f),false);}
                if(e>1.25f)Animate("Finale Guard");
                break;
            case 6:
                float g=t-10;
                HeroAt(Deck(Mathf.Lerp(.8f,2.2f,Mathf.SmoothStep(0,1,g/.7f))));
                SharkAt(deck+f*(g<.5f ? 11 : Mathf.Lerp(11,14,Mathf.Clamp01((g-.5f)/.5f))));
                if(g>=.5f && fired==0){fired=1;Counter(shark.NoseWorldPoint);}
                if(g>1.2f)Animate("Finale Guard");
                break;
            case 7:
                float h=t-11.5f;TailAt(Deck(2.2f),h);
                if(h>=1.65f && !threatPlayed){threatPlayed=true;shark.PlayThreat();}
                if(h>=.65f && fired==0)
                {
                    fired=1;Animate("Finale Knockdown",.02f);battle.CinematicImpact(Deck(2.2f),false,true);
                    CreateProjectile();lostStart=GripCentre;HasLostStick=true;
                }
                float fall=Mathf.Clamp01((h-.65f)/2.1f);
                // The 2.3m gap between neighbouring decks is open water.
                Vector3 feet=Deck(2.2f)-Side*(Mathf.SmoothStep(0,1,fall)*2.8f)-f*(fall*2);
                feet.y=Mathf.Lerp(Deck(2.2f).y,Water(feet)-1.7f,fall)+Mathf.Sin(fall*Mathf.PI)*1.8f;
                HeroAt(feet);
                if(projectile!=null)
                {
                    Vector3 lost=lostStart+Side*(fall*8)-f*(fall*3)+Vector3.up*(Mathf.Sin(fall*Mathf.PI)*3-fall*3);
                    SpinProjectile(lost,h);
                }
                if(fall>=.8f && fired==1){fired=2;battle.CinematicImpact(feet,false,true);}
                break;
        }
        if(t>=Duration){IsComplete=true;CurrentBeat="Defeated";battle.CompleteFinale();}
    }
    Vector3 GripCentre => stick!=null && stickMesh!=null ? stick.TransformPoint(stickMesh.bounds.center) : hero.transform.position+Vector3.up*3;
    void CreateProjectile()
    {
        CatchStick();if(stick==null || stickMesh==null)return;
        var visual=new GameObject("Cinematic Sahur stick",typeof(MeshFilter),typeof(MeshRenderer));
        visual.transform.SetPositionAndRotation(stick.position,stick.rotation);visual.transform.localScale=stick.lossyScale;
        visual.GetComponent<MeshFilter>().sharedMesh=stickMesh;
        visual.GetComponent<MeshRenderer>().sharedMaterials=stick.GetComponent<Renderer>().sharedMaterials;
        projectile=visual.transform;projectileRotation=stick.rotation;
        foreach(var renderer in held)renderer.enabled=false;
    }
    void SpinProjectile(Vector3 centre,float age)
    {
        projectile.rotation=Quaternion.AngleAxis(age*680,Side)*projectileRotation;
        projectile.position=centre-projectile.TransformVector(stickMesh.bounds.center);
    }
    void CatchStick()
    {
        if(projectile!=null)Destroy(projectile.gameObject);projectile=null;
        if(held!=null && !HasLostStick)for(int i=0;i<held.Length;i++)if(held[i]!=null)held[i].enabled=heldEnabled[i];
    }
    void LateUpdate()
    {
        if(battle==null || IsComplete || PauseSettingsMenu.IsOpen)return;
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        Vector3 focus=Vector3.Lerp(feet+Vector3.up*3,shark.NoseWorldPoint,.28f);
        Vector3 shot=focus+Side*(beat==7 ? 18 : 15)-Forward*10+Vector3.up*(beat==4 ? 9 : 6);
        shot.y=Mathf.Max(shot.y,Water(shot)+1);
        float blend=1-Mathf.Exp(-7*Time.deltaTime);
        float opening=Mathf.SmoothStep(0,1,Elapsed/1.1f);
        shot=Vector3.Lerp(cameraStart,shot,opening);
        shotCamera.transform.position=Vector3.Lerp(shotCamera.transform.position,shot,blend);
        Quaternion look=Quaternion.LookRotation(focus-shotCamera.transform.position);
        shotCamera.transform.rotation=Quaternion.Slerp(shotCamera.transform.rotation,Quaternion.Slerp(cameraRotation,look,opening),blend);
        shotCamera.fieldOfView=Mathf.Lerp(cameraFov,beat==7 ? 50 : 44,opening);
    }
    void OnDestroy(){CatchStick();}
}
