using UnityEngine;
namespace Mavis
{
    /// <summary>Gate existing authored trails; never emit outside the damaging swing.</summary>
    [DisallowMultipleComponent]
    public sealed class BossWeaponTrails : MonoBehaviour
    {
        TrailRenderer[] trails;
        CappuccinoAI capri;
        NailongAI nailong;
        void Awake(){trails=GetComponentsInChildren<TrailRenderer>(true);capri=GetComponent<CappuccinoAI>();nailong=GetComponent<NailongAI>();}
        void LateUpdate()
        {
            bool swing=capri!=null && capri.DamageWindowActive;
            if(nailong!=null)swing=nailong.CurrentState==NailongAI.State.Attack;
            foreach(var trail in trails)if(trail!=null)trail.emitting=swing && !PauseSettingsMenu.IsOpen;
        }
        void OnDisable(){if(trails!=null)foreach(var trail in trails)if(trail!=null)trail.emitting=false;}
    }
}
