using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Boss encounter HUD and animation-led combat cues.</summary>
    [DisallowMultipleComponent]
    public sealed class CappuccinoBossPresentation : MonoBehaviour
    {
        public enum CueKind { Intro, Windup, Swing, Hit, Stagger, Surge, Defeated }
        public Material effectsTemplate;
        CappuccinoAI ai;
        Health health;
        AudioSource audioSource;
        AudioClip[] clips;
        BossHudPanel hud;
        float victoryAt = -100f;

        void Awake() => Initialize();
        void Initialize()
        {
            if (ai != null) return;
            ai = GetComponent<CappuccinoAI>(); health = GetComponent<Health>();
            if (!Application.isPlaying) return;
            if(GetComponent<BossWeaponTrails>()==null)gameObject.AddComponent<BossWeaponTrails>();
            BuildHUD(); CreateAudio();
        }

        public void Cue(CueKind kind)
        {
            Initialize();
            if (kind == CueKind.Defeated) victoryAt = Time.time;
            if(Application.isPlaying&&!PauseSettingsMenu.IsOpen)
            {
                if(kind==CueKind.Hit)
                {
                    Vector3 point=ai.target?ai.target.position+Vector3.up:transform.position+transform.forward*1.6f+Vector3.up;
                    var body=ai.target?ai.target.GetComponent<Collider>():null;
                    if(body)point=body.ClosestPoint(transform.position+Vector3.up);
                    BossCombatVfx.Burst(point,transform.forward,new Color(.72f,.71f,.65f,.7f),8,1.5f);
                }
                else if(kind==CueKind.Stagger||kind==CueKind.Surge)BossCombatVfx.Burst(transform.position,Vector3.up,new Color(.55f,.48f,.36f,.55f),16,3);
                else if(kind==CueKind.Swing&&ai.CurrentAttack==CappuccinoAI.AttackKind.DashThrust)BossCombatVfx.Burst(transform.position-transform.forward*.5f,Vector3.up,new Color(.42f,.37f,.29f,.5f),10,2);
            }
            if (audioSource != null && !PauseSettingsMenu.IsOpen)
            {
                int index = kind == CueKind.Swing ? 1 : kind == CueKind.Hit || kind == CueKind.Stagger ? 2 : 0;
                audioSource.pitch = kind == CueKind.Surge ? .7f : kind == CueKind.Stagger ? .75f : 1f;
                float gain = kind == CueKind.Windup ? .9f : kind == CueKind.Hit ? .65f : .45f;
                if (clips[index]) audioSource.PlayOneShot(clips[index], gain);
            }
        }

        void LateUpdate()
        {
            Initialize();
            if (ai == null || health == null || !Application.isPlaying) return;
            UpdateHUD();
        }

        void BuildHUD() => hud = new BossHudPanel(transform,"Capri Boss HUD");

        void UpdateHUD()
        {
            if(hud==null)return;
            var camera=Camera.main;
            bool victory=health.IsDead && Time.time-victoryAt<3f;
            bool visible=camera && (ai.FightActive || victory);
            hud.Refresh(visible,camera?(transform.position-camera.transform.position).sqrMagnitude:float.PositiveInfinity,
                health.Ratio,health.currentHealth,health.maxHealth,ai.FightPhase==2?"CAPPUCCINO ASSASSINO · RISTRETTO":"CAPPUCCINO ASSASSINO",Time.deltaTime);
        }

        void CreateAudio()
        {
            if (!GameAudioPolicy.SoundEnabled) return;
            var obj=new GameObject("Capri combat audio");obj.transform.SetParent(transform,false);
            audioSource=obj.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=1f;
            audioSource.minDistance=4f;audioSource.maxDistance=38f;audioSource.volume=.5f;
            clips = new[]
            {
                GameplayAudio.LoadClip("CapriWarning"),
                GameplayAudio.LoadClip("CapriSweep"),
                GameplayAudio.LoadClip("CapriImpact")
            };
        }

        void OnDisable()
        {hud?.Hide();}
        void OnDestroy() => hud?.Dispose();
    }
}
