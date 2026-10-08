using UnityEngine;
namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class BombardinoFlameJet : MonoBehaviour
    {
        BombardinoBoss boss;ParticleSystem jet;
        void Start()
        {
            boss=GetComponent<BombardinoBoss>();
            jet=BossCombatVfx.Flames(transform,"Flame nozzle",64,35,.65f,.55f,10);
            jet.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void LateUpdate()
        {
            if(boss==null || jet==null)return;
            jet.transform.SetPositionAndRotation(boss.WeaponOrigin(true),Quaternion.LookRotation(boss.FlameContact-boss.WeaponOrigin(true)));
            bool active=boss.CurrentState==BombardinoBoss.State.Attack && boss.CurrentAttack==BombardinoBoss.AttackKind.FlameRun && !PauseSettingsMenu.IsOpen;
            if(active && !jet.isPlaying)jet.Play();else if(!active && jet.isPlaying)jet.Stop(true,ParticleSystemStopBehavior.StopEmitting);
        }
        void OnDisable(){if(jet!=null)jet.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    }
}
