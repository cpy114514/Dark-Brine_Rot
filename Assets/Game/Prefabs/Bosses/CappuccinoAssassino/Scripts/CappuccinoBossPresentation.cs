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
        CappuccinoEnemy enemy;
        CappuccinoUltimate ultimate;
        Health health;
        AudioSource audioSource;
        AudioClip[] clips;
        BossHudPanel hud;
        float victoryAt = -100f;

        void Awake() => Initialize();
        void Initialize()
        {
            if (ai != null) return;
            ai = GetComponent<CappuccinoAI>(); enemy = GetComponent<CappuccinoEnemy>();
            ultimate = GetComponent<CappuccinoUltimate>(); health = GetComponent<Health>();
            if (!Application.isPlaying) return;
            BuildHUD(); CreateAudio();
        }

        public void Cue(CueKind kind)
        {
            Initialize();
            if (kind == CueKind.Defeated) victoryAt = Time.time;
            if (audioSource != null && !PauseSettingsMenu.IsOpen)
            {
                int index = kind == CueKind.Swing ? 1 : kind == CueKind.Hit || kind == CueKind.Stagger ? 2 : 0;
                audioSource.pitch = kind == CueKind.Surge ? .7f : kind == CueKind.Stagger ? .75f : 1f;
                audioSource.PlayOneShot(clips[index], kind == CueKind.Hit ? .65f : .45f);
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
            string advice;
            if(health.IsDead)advice="The island is quiet again.";
            else if(ultimate && ultimate.CurrentPhase==CappuccinoUltimate.Phase.Immobilized)advice="EXHAUSTED  /  Cannot move or attack. Counterattack now.";
            else if(ultimate && ultimate.CurrentPhase==CappuccinoUltimate.Phase.Charging)advice="CAFFEINE SURGE  /  Create space before the blades power up";
            else if(ai.CurrentState==CappuccinoAI.State.Windup)advice=ai.AttackName+"  /  "+
                (ai.CurrentAttack==CappuccinoAI.AttackKind.Whirlwind?"Step back":ai.CurrentAttack==CappuccinoAI.AttackKind.DashThrust?"Dodge sideways":"Watch the follow-up");
            else if(ai.CurrentState==CappuccinoAI.State.Attack)advice=ai.AttackName+"  /  CUT "+(ai.SwingIndex+1);
            else if(ai.CurrentState==CappuccinoAI.State.Staggered)advice="POSTURE BROKEN  /  Strike now";
            else if(ai.CurrentState==CappuccinoAI.State.Recover)advice="OPENING  /  Counterattack";
            else advice=ai.FightPhase==2?"Three cuts. Wait for the final swing.":"Two blades. Keep stamina for a dodge.";
            hud.Refresh(visible,camera?(transform.position-camera.transform.position).sqrMagnitude:float.PositiveInfinity,
                health.Ratio,health.currentHealth,health.maxHealth,health.IsDead?"BOSS DEFEATED":"CAPPUCCINO ASSASSINO",
                ai.FightPhase==2?"DUAL BLADE  /  PHASE II":"DUAL BLADE  /  PHASE I",advice,Time.deltaTime,ai.PoiseRatio);
        }

        void CreateAudio()
        {
            var obj=new GameObject("Capri combat audio");obj.transform.SetParent(transform,false);
            audioSource=obj.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=1f;
            audioSource.minDistance=4f;audioSource.maxDistance=38f;audioSource.volume=.5f;
            clips=new AudioClip[3];
            for(int k=0;k<3;k++)
            {
                float duration=k==0?.3f:k==1?.18f:.13f;
                var pcm=new float[Mathf.CeilToInt(duration*22050)];var random=new System.Random(350+k);
                for(int i=0;i<pcm.Length;i++)
                {float t=i/22050f;float envelope=Mathf.Sin(Mathf.PI*t/duration)*Mathf.Exp(-t*(k==2?20f:4f));float noise=(float)random.NextDouble()*2-1;float frequency=k==0?380f+900f*t:k==1?700f-1200f*t:180f;pcm[i]=(Mathf.Sin(t*frequency*Mathf.PI*2)*.22f+noise*(k==0?.05f:.5f))*envelope;}
                clips[k]=AudioClip.Create(k==0?"Capri warning":k==1?"Blade sweep":"Steel impact",pcm.Length,1,22050,false);clips[k].SetData(pcm,0);
            }
        }

        void OnDisable()
        {hud?.Hide();}
        void OnDestroy()
        {hud?.Dispose();if(clips!=null)foreach(var clip in clips)Release(clip);}
        static void Release(Object value) {if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
