using System;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Island-local sound palette, pooled accents and a continuous river sound field.</summary>
[DisallowMultipleComponent]
public sealed class SecondIslandAudio : MonoBehaviour
{
    public const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    public Vector3[] riverPoints=Array.Empty<Vector3>();
    public float[] riverHalfWidths=Array.Empty<float>();
    public Vector3 chocolateLakeCenter;
    public Vector2 chocolateLakeRadii;
    public Vector3 chocolateFallLip;
    public SecondIslandChocolateWaterway waterway;
    public Vector3[] confectionPoints=Array.Empty<Vector3>();
    public AudioClip cloudWind,chocolateFlow,chocolateBubbles;
    static readonly Dictionary<string,AudioClip> cache=new Dictionary<string,AudioClip>();
    static readonly int[] variations=new int[12];
    static SecondIslandAudio active;
    static CombatFeelProfile soundProfile;
    readonly AudioSource[] voices=new AudioSource[6];
    readonly System.Random random=new System.Random();
    AudioSource wind,flow,bubbles,waterfall;
    ThirdPersonPlayerController player;
    HuangMeiBoss huang;
    ZhuBaJieBoss bajie;
    float nextLookup,nextAccent,nextRiverProbe;
    bool paused;
    Vector3 riverContact;
    public float DistanceFromRiver {get;private set;}=float.PositiveInfinity;
    public bool BattleMixActive {get;private set;}
    public int WorldCuesPlayed {get;private set;}
    public int MovementCuesResolved {get;private set;}
    public string LastMovementClip {get;private set;}
    public float RiverGain=>flow?flow.volume:0;
    public bool BedsPaused=>paused;
    public static bool IsIsland=>SceneManager.GetActiveScene().path==ScenePath;
    public static SecondIslandAudio Current=>active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){active=null;cache.Clear();Array.Clear(variations,0,variations.Length);soundProfile=null;}

    public static AudioClip Clip(string name)
    {
        if(!IsIsland)return null;
        if(!cache.TryGetValue(name,out var clip)){clip=Resources.Load<AudioClip>("SecondIslandAudio/"+name);cache.Add(name,clip);}
        return clip;
    }
    public static AudioClip Cue(GameplayAudio.Cue cue)
    {
        if(!IsIsland)return null;
        int count=cue==GameplayAudio.Cue.Swing||cue==GameplayAudio.Cue.Pickup?3:cue==GameplayAudio.Cue.Hurt||cue==GameplayAudio.Cue.Dodge||cue==GameplayAudio.Cue.Click?2:1;
        int n=variations[(int)cue]++%count;
        return Clip(cue+(n==0?"":"_"+n));
    }
    public static CombatFeelProfile SoundProfile(CombatFeelProfile fallback)
    {
        if(!IsIsland)return fallback;
        if(!soundProfile)soundProfile=Resources.Load<CombatFeelProfile>("SecondIslandAudio/CombatSounds");
        return soundProfile?soundProfile:fallback;
    }
    public static AudioClip Movement(int kind,Vector3 at,AudioClip fallback)
    {
        if(!IsIsland)return fallback;
        bool wet=active && active.InChocolate(at);
        string name=kind==0?(wet?"RiverStep":"CottonPress"):kind==1?"CottonLift":wet?"RiverLand":"CottonLand";
        int n=variations[8+kind]++%(kind==0?4:3);
        var clip=Clip(name+(n==0?"":"_"+n));
        if(active && clip){active.MovementCuesResolved++;active.LastMovementClip=clip.name;}
        return clip?clip:fallback;
    }
    public bool InChocolate(Vector3 at)
    {
        if (waterway)
            return waterway.TrySample(at, out var surface, out _) &&
                surface.y - at.y is > .025f and < 2.5f;
        if(chocolateLakeRadii.x>0&&chocolateLakeRadii.y>0){var q=new Vector2((at.x-chocolateLakeCenter.x)/chocolateLakeRadii.x,(at.z-chocolateLakeCenter.z)/chocolateLakeRadii.y);
            float shape=1+.06f*Mathf.Sin(Mathf.Atan2(q.y,q.x)*3+.5f)+.035f*Mathf.Cos(Mathf.Atan2(q.y,q.x)*7);
            if(q.magnitude<shape&&Mathf.Abs(at.y-chocolateLakeCenter.y)<3.5f)return true;}
        Closest(at,out var point,out float width);
        return new Vector2(at.x-point.x,at.z-point.z).sqrMagnitude<width*width && Mathf.Abs(at.y-point.y)<3.5f;
    }
    public void Closest(Vector3 at,out Vector3 point,out float width)
    {
        if (waterway)
        {
            point = waterway.NearestSurface(at);
            width = 0f;
            return;
        }
        point=at+Vector3.up*10000;width=0;float best=float.PositiveInfinity;
        for(int i=1;i<riverPoints.Length;i++){
            var a=riverPoints[i-1];var delta=riverPoints[i]-a;float t=Mathf.Clamp01(Vector3.Dot(at-a,delta)/Mathf.Max(.001f,delta.sqrMagnitude));var p=a+delta*t;float d=(at-p).sqrMagnitude;
            if(d>=best)continue;best=d;point=p;width=riverHalfWidths.Length==riverPoints.Length?Mathf.Lerp(riverHalfWidths[i-1],riverHalfWidths[i],t):9;
        }
        if(chocolateLakeRadii.x>0&&chocolateLakeRadii.y>0){var q=new Vector2((at.x-chocolateLakeCenter.x)/chocolateLakeRadii.x,(at.z-chocolateLakeCenter.z)/chocolateLakeRadii.y);
            float angle=Mathf.Atan2(q.y,q.x),shape=1+.06f*Mathf.Sin(angle*3+.5f)+.035f*Mathf.Cos(angle*7);q/=Mathf.Max(1,q.magnitude/shape);
            var p=new Vector3(chocolateLakeCenter.x+q.x*chocolateLakeRadii.x,chocolateLakeCenter.y,chocolateLakeCenter.z+q.y*chocolateLakeRadii.y);
            if((at-p).magnitude<Mathf.Max(0,Mathf.Sqrt(best)-width)){point=p;width=0;}}
    }
    AudioSource Source(string label,bool spatial,int priority)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=spatial?1:0;
        s.priority=priority;s.dopplerLevel=0;s.rolloffMode=AudioRolloffMode.Linear;s.minDistance=5;s.maxDistance=65;s.volume=0;return s;
    }
    void Awake()
    {
        if(!Application.isPlaying||gameObject.scene.path!=ScenePath){enabled=false;return;}
        if(active && active!=this){enabled=false;return;}active=this;
        wind=Source("Cloud air - stereo bed",false,200);flow=Source("Chocolate current - nearest river reach",true,180);bubbles=Source("Thick cocoa bubbles - nearest river reach",true,190);
        waterfall=Source("Chocolate cloudfall - distant heavy pour",true,185);waterfall.transform.position=waterway ? waterway.FallLip : chocolateFallLip;waterfall.minDistance=18;waterfall.maxDistance=280;waterfall.pitch=.82f;
        var sources=new[]{wind,flow,bubbles,waterfall};var beds=new[]{cloudWind,chocolateFlow,chocolateBubbles,chocolateFlow};
        for(int i=0;i<sources.Length;i++){sources[i].clip=beds[i];sources[i].loop=true;if(GameAudioPolicy.SoundEnabled&&beds[i])sources[i].Play();}
        for(int i=0;i<voices.Length;i++)voices[i]=Source("Confection accent "+i,true,130);
        nextAccent=Time.time+9;Lookup();
    }
    void Lookup()
    {
        if(!player)player=FindFirstObjectByType<ThirdPersonPlayerController>();
        if(!huang)huang=FindFirstObjectByType<HuangMeiBoss>();if(!bajie)bajie=FindFirstObjectByType<ZhuBaJieBoss>();
        if(huang&&!huang.GetComponent<SecondIslandBossAudio>())huang.gameObject.AddComponent<SecondIslandBossAudio>();
        if(bajie&&!bajie.GetComponent<SecondIslandBossAudio>())bajie.gameObject.AddComponent<SecondIslandBossAudio>();
    }
    void OnEnable()
    {
        if(!Application.isPlaying||gameObject.scene.path!=ScenePath||!wind)return;
        active=this;paused=false;nextAccent=Time.time+9;
        if(GameAudioPolicy.SoundEnabled){if(wind.clip&&!wind.isPlaying)wind.Play();if(flow.clip&&!flow.isPlaying)flow.Play();if(bubbles.clip&&!bubbles.isPlaying)bubbles.Play();if(waterfall.clip&&!waterfall.isPlaying)waterfall.Play();}
    }
    void SetPause(bool value)
    {
        if(paused==value)return;paused=value;
        foreach(var s in voices)if(s){if(value)s.Pause();else s.UnPause();}
        foreach(var s in new[]{wind,flow,bubbles,waterfall})if(s){if(value)s.Pause();else s.UnPause();}
    }
    public void PlayWorld(string cue,Vector3 at,float gain=.45f)
    {
        if(!GameAudioPolicy.SoundEnabled||paused)return;var clip=Clip(cue);if(!clip)return;
        AudioSource selected=null;foreach(var voice in voices)if(!voice.isPlaying){selected=voice;break;}
        if(!selected)return; // Ambient accents never steal an active warning.
        selected.transform.position=at;selected.clip=clip;selected.volume=Mathf.Clamp01(gain);selected.pitch=.97f+(float)random.NextDouble()*.06f;selected.Play();WorldCuesPlayed++;
    }
    void Update()
    {
        if(!Application.isPlaying||active!=this)return;
        if(!IsIsland){SetPause(true);return;}
        SetPause(Time.timeScale<=0||PauseSettingsMenu.IsOpen);
        if(paused)return;
        if(Time.unscaledTime>=nextLookup){nextLookup=Time.unscaledTime+.75f;Lookup();}
        BattleMixActive=huang&&huang.FightActive||bajie&&bajie.FightActive;
        if(!player)return;
        var feet=player.transform.position+Vector3.up*player.LowestFootWorldOffset;
        if(Time.unscaledTime>=nextRiverProbe){nextRiverProbe=Time.unscaledTime+.1f;Closest(feet,out riverContact,out float width);
            float horizontal=new Vector2(feet.x-riverContact.x,feet.z-riverContact.z).magnitude;
            DistanceFromRiver=Mathf.Sqrt(Mathf.Pow(Mathf.Max(0,horizontal-width),2)+Mathf.Pow(Mathf.Max(0,Mathf.Abs(feet.y-riverContact.y)-3),2));
            // Move one continuous spatial field to the nearest reach, never stack dozens of loops.
            flow.transform.position=riverContact;bubbles.transform.position=riverContact+Vector3.up*.1f;}
        float battle=BattleMixActive?.42f:1;float near=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(DistanceFromRiver/43));float k=1-Mathf.Exp(-Time.unscaledDeltaTime*3);
        wind.volume=Mathf.Lerp(wind.volume,.22f*battle,k);flow.volume=Mathf.Lerp(flow.volume,.62f*near*battle,k);bubbles.volume=Mathf.Lerp(bubbles.volume,.29f*near*near*battle,k);
        waterfall.volume=Mathf.Lerp(waterfall.volume,waterway || chocolateFallLip!=Vector3.zero?.5f*battle:0,k);
        if(Time.time<nextAccent||BattleMixActive)return;nextAccent=Time.time+9+(float)random.NextDouble()*10;
        if(DistanceFromRiver<12){PlayWorld("BubblePop",riverContact,.22f);return;}
        float best=24*24;Vector3 anchor=feet;bool found=false;foreach(var p in confectionPoints){float d=(p-feet).sqrMagnitude;if(d<best){best=d;anchor=p;found=true;}}
        if(found)PlayWorld("CandyCreak",anchor,.17f);
    }
    void OnDisable()
    {
        if(active!=this)return;
        if(wind)wind.Stop();if(flow)flow.Stop();if(bubbles)bubbles.Stop();if(waterfall)waterfall.Stop();foreach(var s in voices)if(s)s.Stop();active=null;
    }
}
