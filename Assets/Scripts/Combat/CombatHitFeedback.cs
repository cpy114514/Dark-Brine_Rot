using System;
using UnityEngine;
namespace Mavis
{
    public enum CombatHitKind { ComboOne, ComboTwo, ComboFinisher, ChargedHeavy, JumpSlash, Boomerang, Spin }
    public sealed class CombatHitFeedback : MonoBehaviour
    {
        public CombatFeelProfile profile;
        public CombatHitKind LastHitKind { get; private set; }
        public CombatHitResult LastResult { get; private set; }
        public int HitCount { get; private set; }
        public static event Action<CombatHitEvent> HitConfirmed;
        AudioSource contactSource, weightSource, responseSource;
        CombatImpactVisuals visuals;
        CombatCameraShake cameraShake;
        Camera viewer;
        int soundFrame=-1;
        static readonly Unity.Profiling.ProfilerMarker ImpactMarker=new Unity.Profiling.ProfilerMarker("Combat.ConfirmedImpact");
        public CombatImpactVisuals Visuals => visuals;
        public CombatFeelProfile Profile => profile ? profile : CombatFeelProfile.Default;
        public static bool Apply(GameObject attacker, IDamageable target, float damage, Vector3 point, CombatHitKind kind, Vector3 swingDirection=default)
        {
            var victim=target as Component;
            if (!attacker || !victim || target==null || damage<=0 || !victim.gameObject.activeInHierarchy) return false;
            if (target is Health hp && hp.IsDead || target is NailongHealth nl && nl.IsDead ||
                target is PlayerHealth ph && (ph.currentHealth<=0 || ph.IsProtected) ||
                target is Story1SharkHealth shark && shark.IsProtected) return false;
            var response=victim.GetComponent<CombatImpactResponse>();
            CombatHitResult result=response && response.blocksMelee ? CombatHitResult.Blocked :
                response && response.unflinching ? CombatHitResult.Unflinching :
                kind==CombatHitKind.ChargedHeavy || kind==CombatHitKind.ComboFinisher || kind==CombatHitKind.JumpSlash ? CombatHitResult.HeavyHit : CombatHitResult.Hit;
            if(BossWeakPoint.Contains(victim,point))damage*=BossWeakPoint.DamageMultiplier;
            if(target is ICombatHitResolver resolver)result=resolver.ResolveCombatHit(attacker,damage,point,kind);
            else if(result!=CombatHitResult.Blocked)
            {
                if(target is NailongHealth boss)boss.ApplyCombatDamage(damage,point,kind);
                else target.ApplyDamage(damage,point);
            }
            if(target is NailongHealth && result==CombatHitResult.Hit)result=CombatHitResult.Unflinching;
            if(victim.GetComponent<CappuccinoAI>() is CappuccinoAI capri && !capri.IsExposed && result==CombatHitResult.Hit)result=CombatHitResult.Unflinching;
            Vector3 direction=swingDirection.sqrMagnitude>.0001f ? swingDirection.normalized : (point-attacker.transform.position).normalized;
            if(direction.sqrMagnitude<.01f)direction=attacker.transform.forward;
            // Edit Mode contract checks resolve damage without creating runtime audio, particles or clocks.
            if(!Application.isPlaying)return true;
            var feedback=attacker.GetComponent<CombatHitFeedback>();
            if(!feedback)feedback=attacker.AddComponent<CombatHitFeedback>();
            var hit=new CombatHitEvent(attacker,victim,point,direction,kind,result,result==CombatHitResult.Blocked?0:damage);
            feedback.Emit(hit,response);
            HitConfirmed?.Invoke(hit);
            return true;
        }
        public static void Block(GameObject defender,Vector3 point,Vector3 direction)
        {
            var feedback=defender.GetComponent<CombatHitFeedback>();
            if(!feedback)feedback=defender.AddComponent<CombatHitFeedback>();
            var hit=new CombatHitEvent(defender,defender.GetComponent<PlayerHealth>(),point,direction,CombatHitKind.ComboOne,CombatHitResult.Blocked,0);
            feedback.Emit(hit,null);HitConfirmed?.Invoke(hit);
        }
        void Awake()
        {
            visuals=GetComponent<CombatImpactVisuals>();if(!visuals)visuals=gameObject.AddComponent<CombatImpactVisuals>();
            contactSource=Source("Melee contact transient");weightSource=Source("Melee weight");responseSource=Source("Melee material and reaction");
            if(!GetComponent<CombatLocalHitStop>())gameObject.AddComponent<CombatLocalHitStop>();
        }
        AudioSource Source(string label)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);
            var audio=go.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=.8f;audio.minDistance=4;audio.maxDistance=40;audio.dopplerLevel=0;return audio;
        }
        void Emit(CombatHitEvent hit, CombatImpactResponse response)
        {
            using(var sample=ImpactMarker.Auto())
            {
            LastHitKind=hit.kind;LastResult=hit.result;HitCount++;
            var config=Profile;var settings=config.For(hit.kind);
            bool blocked=hit.result==CombatHitResult.Blocked, unflinching=hit.result==CombatHitResult.Unflinching;
            if(blocked){settings.stop*=.55f;settings.cameraStrength*=.5f;settings.vfx*=.65f;}
            if(unflinching)settings.stop*=.7f;
            var melee=GetComponent<SahurAttack>();
            var attackerAnimator=melee ? melee.animator : GetComponentInChildren<Animator>();
            CombatLocalHitStop.Freeze(gameObject,attackerAnimator,settings.stop);
            // Death and story/cinematic graphs retain their own clocks and poses.
            bool alive=!(hit.target is Health health && health.IsDead) && !(hit.target is NailongHealth nh && nh.IsDead) && !(hit.target is PlayerHealth ph && ph.currentHealth<=0);
            if(alive && !(hit.target is Story1SharkHealth) && !blocked)
            {
                CombatLocalHitStop.Freeze(hit.target.gameObject,hit.target.GetComponentInChildren<Animator>(),settings.stop*(response?response.stopMultiplier:1)*(unflinching?.5f:1));
                Vector3 recoilDirection=Vector3.ProjectOnPlane(hit.direction,Vector3.up);
                if(recoilDirection.sqrMagnitude<.04f)recoilDirection=Vector3.ProjectOnPlane(hit.target.transform.position-hit.attacker.transform.position,Vector3.up);
                if(recoilDirection.sqrMagnitude<.001f)recoilDirection=hit.attacker.transform.forward;
                recoilDirection.Normalize();
                hit.target.GetComponent<CappuccinoEnemy>()?.ApplyCombatImpact(recoilDirection,settings.reaction*(response?response.reactionMultiplier:1));
                hit.target.GetComponent<NailongHitReaction>()?.SetImpactDirection(recoilDirection,settings.reaction*(response?response.reactionMultiplier:1));
                if(!unflinching && !hit.target.GetComponent<UnityEngine.AI.NavMeshAgent>())
                {
                    var push=hit.target.GetComponent<CombatKnockback>();
                    if(!push && hit.target.GetComponent<CharacterController>())push=hit.target.gameObject.AddComponent<CombatKnockback>();
                    push?.Push(Vector3.ProjectOnPlane(hit.direction,Vector3.up).normalized*settings.knockback*(response?response.knockbackMultiplier:1));
                }
            }
            CombatMaterial surface=blocked?CombatMaterial.Metal:response?response.material:CombatMaterial.Flesh;
            visuals.Impact(hit,settings,surface,config.impactBrightness);
            if(soundFrame!=Time.frameCount)
            {
                soundFrame=Time.frameCount;
                if(GameAudioPolicy.SoundEnabled)
                {
                    Play(contactSource,config.contact,hit.point,config.soundVolume*(blocked?.6f:.85f));
                    if(!blocked)Play(weightSource,config.lowImpact,hit.point,config.soundVolume*(hit.result==CombatHitResult.HeavyHit?.8f:.32f),.91f);
                    Play(responseSource,surface==CombatMaterial.Metal?config.metal:surface==CombatMaterial.Wood?config.wood:config.flesh,hit.point,config.soundVolume*.35f);
                    if(!blocked && config.enemyReaction!=null && config.enemyReaction.Length>0)Play(responseSource,config.enemyReaction,hit.point,config.soundVolume*.22f);
                }
                if(viewer!=Camera.main || !cameraShake)
                {viewer=Camera.main;if(viewer){cameraShake=viewer.GetComponent<CombatCameraShake>();if(!cameraShake)cameraShake=viewer.gameObject.AddComponent<CombatCameraShake>();}}
                if(cameraShake)cameraShake.PulseDirectional(hit.direction,settings.cameraStrength*config.cameraMotion,settings.cameraDuration);
            }
            }
        }
        static void Play(AudioSource source,AudioClip[] clips,Vector3 point,float gain,float pitch=1)
        {
            if(clips==null||clips.Length==0)return;
            var clip=clips[UnityEngine.Random.Range(0,clips.Length)];if(!clip)return;
            source.transform.position=point;source.pitch=pitch+UnityEngine.Random.Range(-.035f,.035f);source.PlayOneShot(clip,gain);
        }
        void OnDisable(){GetComponent<CombatLocalHitStop>()?.Release();if(visuals)visuals.ClearTrail();}
        void OnDestroy()
        {
            if(contactSource)Destroy(contactSource.gameObject);
            if(weightSource)Destroy(weightSource.gameObject);
            if(responseSource)Destroy(responseSource.gameObject);
        }
    }
}
