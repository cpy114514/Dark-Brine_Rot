using UnityEngine;
namespace Mavis
{
    public sealed partial class NailongSpitProjectile:IPerfectDodgeThreatSource
    {
        public bool ForecastDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat)
        {
            threat=default;if(resolved||!owner)return false;
            return PerfectDodgeThreats.Projectile(this,owner,player,horizon,velocity,gravity,collisionRadius,0,PerfectDodgeThreat.Stamp(this,Time.time-age,0),out threat);
        }
    }
    public sealed partial class BombardinoOrdnance:IPerfectDodgeThreatSource
    {
        public bool ForecastDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat)
        {
            threat=default;if(!owner||resolved)return false;
            if(Payload==Kind.Fire)
                return PerfectDodgeThreat.Sector(this,owner.transform,player,Mathf.Max(0,fireTick-age),horizon,transform.position,Vector3.forward,radius,360,3,
                    PerfectDodgeThreat.Stamp(this,Time.time-age,Mathf.RoundToInt(fireTick*100)),out threat,false);
            return PerfectDodgeThreats.Projectile(this,owner.transform,player,horizon,velocity,Payload==Kind.Bomb?25:0,.25f,radius,
                PerfectDodgeThreat.Stamp(this,Time.time-age,0),out threat,owner.groundMask);
        }
    }
    public sealed partial class NailongGroundSlam:IPerfectDodgeThreatSource
    {
        public bool ForecastDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat)
        {
            threat=default;if(visualOnly||!owner||!ownerHealth||ownerHealth.IsDead||pulses==null)return false;
            var body=player.GetComponent<CharacterController>();Vector3 centre=body?body.bounds.center:player.transform.position+Vector3.up;
            float thickness=width*.5f+(body?body.radius:.3f);float earliest=float.PositiveInfinity;
            for(int i=0;i<pulses.Length;i++)
            {
                var pulse=pulses[i];if(pulse.crossed.Contains(player)||emissionStopped&&!pulse.launched)continue;
                Vector3 origin=pulse.launched?pulse.origin:fromMouth&&mouthMotion?mouthMotion.MouthPosition:transform.position;
                Vector3 delta=(fromMouth?centre:player.transform.position)-origin;
                float distance=fromMouth?Vector3.Dot(delta,pulse.launched?pulse.forward:owner.transform.forward):Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;
                if(distance<0||distance>radius+thickness)continue;
                if(fromMouth&&Vector3.ProjectOnPlane(delta,pulse.launched?pulse.forward:owner.transform.forward).magnitude>voiceStartRadius+distance*VoiceSpread+(body?body.bounds.extents.magnitude:.5f))continue;
                float seconds=(distance-thickness)/Mathf.Max(.01f,radius)*travelSeconds+i*interval-age;
                if(seconds<-.03f||seconds>horizon||seconds>=earliest)continue;
                earliest=seconds;threat=new PerfectDodgeThreat{source=this,attacker=owner.transform,seconds=Mathf.Max(0,seconds),stamp=PerfectDodgeThreat.Stamp(this,Time.time-age,i)};
            }
            return threat.source;
        }
    }
}
