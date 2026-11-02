using System.Collections.Generic;
using UnityEngine;
namespace Mavis
{
    [DefaultExecutionOrder(18000)]
    public sealed class BossWeaponTrails : MonoBehaviour
    {
        sealed class Ribbon{public Transform hand;public Vector3 tip;public BrineWeaponTrail trail;}
        readonly List<Ribbon> ribbons=new List<Ribbon>();
        CappuccinoAI capri;NailongAI nailong;Health health;NailongHealth nailongHealth;
        void OnEnable(){if(Application.isPlaying&&ribbons.Count==0)Start();}
        void Start()
        {
            if(ribbons.Count>0)return;
            capri=GetComponent<CappuccinoAI>();nailong=GetComponent<NailongAI>();health=GetComponent<Health>();nailongHealth=GetComponent<NailongHealth>();
            var world=BrineVfxWorld.Get();if(!world)return;
            if(capri)
            {
                var rig=GetComponentInChildren<CappuccinoMotionRig>();
                if(rig&&rig.bladeGroundPoints!=null)
                {
                    var tips=new Dictionary<Transform,Vector3>();
                    foreach(var p in rig.bladeGroundPoints)if(p.bone&&(!tips.TryGetValue(p.bone,out var tip)||p.localPoint.sqrMagnitude>tip.sqrMagnitude))tips[p.bone]=p.localPoint;
                    foreach(var pair in tips)Add(pair.Key,pair.Value,world.Style);
                }
            }
            else if(nailong){var motion=GetComponent<NailongAttackMotion>();if(motion){Add(motion.LeftHand,Vector3.zero,world.Style);Add(motion.RightHand,Vector3.zero,world.Style);}}
        }
        void Add(Transform hand,Vector3 tip,BrineVfxStyle style)
        {
            if(hand)ribbons.Add(new Ribbon{hand=hand,tip=tip,trail=new BrineWeaponTrail(transform,"Boss fitted weapon loft",style)});
        }
        void LateUpdate()
        {
            if(PauseSettingsMenu.IsOpen||Time.deltaTime<=0)return;
            bool dead=(health&&health.IsDead)||(nailongHealth&&nailongHealth.IsDead);
            for(int i=0;i<ribbons.Count;i++)
            {
                var r=ribbons[i];bool active=!dead&&(capri?capri.DamageWindowActive:nailong&&nailong.ClawActive(i));
                var camera=Camera.main;if(camera&&(camera.transform.position-transform.position).sqrMagnitude>7200)active=false;
                Vector3 end=r.hand.TransformPoint(r.tip),start=Vector3.Lerp(r.hand.position,end,.3f);
                if(!capri){end=r.hand.position+transform.up*.2f;start=end-transform.up*.4f;}
                r.trail.Configure(CombatHitKind.ComboFinisher,.72f);r.trail.Sample(start,end,active,capri?.18f:.14f);
            }
        }
        void OnDisable(){foreach(var r in ribbons)r.trail.Clear();}
        public void ReleaseBeforeReload(){foreach(var r in ribbons)r.trail.Dispose(true);ribbons.Clear();}
        void OnDestroy(){foreach(var r in ribbons)r.trail.Dispose();}
    }
}
