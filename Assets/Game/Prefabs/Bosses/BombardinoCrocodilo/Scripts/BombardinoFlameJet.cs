using UnityEngine;
namespace Mavis
{
    public sealed class BombardinoFlameJet : MonoBehaviour
    {
        BombardinoBoss boss;
        ParticleSystem jet;
        void Start()
        {
            boss=GetComponent<BombardinoBoss>();
            jet=BossCombatVfx.Flames(transform,"Crocodile flame jet",100,0,1.05f,.8f,30);
            var renderer=jet.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;
            renderer.lengthScale=1.8f;renderer.velocityScale=.025f;
        }
        void LateUpdate()
        {
            if(!jet||!boss||PauseSettingsMenu.IsOpen)return;
            bool active=boss.isActiveAndEnabled&&boss.FightActive&&!boss.IsDead&&boss.CurrentState==BombardinoBoss.State.Attack&&boss.CurrentAttack==BombardinoBoss.AttackKind.FlameRun;
            var emission=jet.emission;emission.rateOverTime=active?95:0;
            if(!active)return;
            Vector3 origin=boss.WeaponOrigin(true);Vector3 delta=boss.FlameContact-origin;
            jet.transform.position=origin;jet.transform.rotation=Quaternion.LookRotation(delta);
            var main=jet.main;main.startLifetime=delta.magnitude/30f;
            var shape=jet.shape;shape.angle=5;shape.radius=.2f;
        }
        void OnDisable(){if(jet)jet.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    }
}
