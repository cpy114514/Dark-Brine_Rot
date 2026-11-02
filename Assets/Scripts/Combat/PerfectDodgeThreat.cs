using System.Collections.Generic;
using UnityEngine;
namespace Mavis
{
    public interface IPerfectDodgeThreatSource
    {
        bool ForecastDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat);
    }
    public struct PerfectDodgeThreat
    {
        public MonoBehaviour source;
        public Transform attacker;
        public float seconds;
        public long stamp;
        public static long Stamp(MonoBehaviour source,float started,int strike)=>((long)source.GetInstanceID()<<32) ^ (uint)(Mathf.RoundToInt(started*1000)*17+strike);
        public static bool Sector(MonoBehaviour source,Transform attacker,PlayerHealth player,float seconds,float horizon,
            Vector3 origin,Vector3 direction,float reach,float arc,float height,long stamp,out PerfectDodgeThreat threat,bool cover=true,bool addRadius=true)
        {
            threat=default;
            if(!source||!source.isActiveAndEnabled||!attacker||!player||seconds<-.025f||seconds>horizon||reach<=0)return false;
            Vector3 delta=player.transform.position-origin;
            var motor=player.GetComponent<CharacterController>();float radius=addRadius?(motor?motor.radius*Mathf.Abs(player.transform.lossyScale.x):.35f):0;
            float feet=motor&&motor.enabled?motor.bounds.min.y:player.transform.position.y;
            if(Mathf.Abs(feet-origin.y)>height)return false;
            delta.y=0;
            if(delta.sqrMagnitude>(reach+radius)*(reach+radius))return false;
            if(arc<359&&delta.sqrMagnitude>.001f&&Vector3.Angle(Vector3.ProjectOnPlane(direction,Vector3.up),delta)>arc*.5f+.01f)return false;
            if(cover&&!PerfectDodgeThreats.ClearPath(player.transform,attacker,origin+Vector3.up,motor?motor.bounds.center:player.transform.position+Vector3.up))return false;
            threat=new PerfectDodgeThreat{source=source,attacker=attacker,seconds=Mathf.Max(0,seconds),stamp=stamp};return true;
        }
    }
    public static class PerfectDodgeThreats
    {
        static readonly List<MonoBehaviour> sources=new List<MonoBehaviour>();
        static RaycastHit[] hits=new RaycastHit[32];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>sources.Clear();
        public static void Register(MonoBehaviour source){if(source&&source is IPerfectDodgeThreatSource&&!sources.Contains(source))sources.Add(source);}
        public static bool Find(PlayerHealth player,float horizon,long[] consumed,out PerfectDodgeThreat best)
        {
            best=default;float earliest=float.PositiveInfinity;
            for(int i=sources.Count-1;i>=0;i--)
            {
                var source=sources[i];if(!source){sources.RemoveAt(i);continue;}
                if(!source.isActiveAndEnabled||!(source is IPerfectDodgeThreatSource forecast))continue;
                if(forecast.ForecastDodge(player,horizon,out var threat)&&Alive(threat.attacker)&&System.Array.IndexOf(consumed,threat.stamp)<0&&threat.seconds<earliest)
                {best=threat;earliest=threat.seconds;}
            }
            return best.source;
        }
        public static bool ClearPath(Transform player,Transform attacker,Vector3 from,Vector3 to)
        {
            Vector3 ray=to-from;if(ray.sqrMagnitude<.001f)return true;int count;
            while((count=Physics.RaycastNonAlloc(from,ray.normalized,hits,ray.magnitude,~0,QueryTriggerInteraction.Ignore))==hits.Length)
                System.Array.Resize(ref hits,hits.Length*2);
            for(int i=0;i<count;i++)
            {
                var t=hits[i].transform;
                if(t.IsChildOf(player)||t.IsChildOf(attacker)||player.IsChildOf(t)||attacker.IsChildOf(t))continue;
                return false;
            }
            return true;
        }
        public static bool Alive(Transform target)
        {
            if(!target||!target.gameObject.activeInHierarchy)return false;
            var health=target.GetComponentInParent<Health>();if(health)return !health.IsDead;
            var nailong=target.GetComponentInParent<NailongHealth>();return nailong&&!nailong.IsDead;
        }
        public static bool Projectile(MonoBehaviour source,Transform attacker,PlayerHealth player,float horizon,Vector3 velocity,float gravity,
            float bodyRadius,float blastRadius,long stamp,out PerfectDodgeThreat threat,int mask=~0)
        {
            threat=default;if(!attacker||!player)return false;
            Vector3 previous=source.transform.position;
            for(int sample=1;sample<=16;sample++)
            {
                float time=horizon*sample/16;
                Vector3 next=source.transform.position+velocity*time+Vector3.down*(.5f*gravity*time*time),segment=next-previous;
                if(segment.sqrMagnitude<.000001f)continue;
                int count;
                while((count=Physics.SphereCastNonAlloc(previous,bodyRadius,segment.normalized,hits,segment.magnitude,mask,QueryTriggerInteraction.Ignore))==hits.Length)
                    System.Array.Resize(ref hits,hits.Length*2);
                float distance=float.PositiveInfinity;Collider collider=null;Vector3 point=default;
                for(int i=0;i<count;i++)
                {
                    var hit=hits[i];if(hit.transform.IsChildOf(attacker)||hit.transform.IsChildOf(source.transform)||hit.collider.GetComponentInParent<BombardinoOrdnance>())continue;
                    if(hit.distance<distance){distance=hit.distance;collider=hit.collider;point=hit.point;}
                }
                if(collider)
                {
                    var body=player.GetComponent<CharacterController>();
                    if(collider.GetComponentInParent<PlayerHealth>()!=player&&(!body||blastRadius<=0||body.bounds.SqrDistance(point)>blastRadius*blastRadius))return false;
                    threat=new PerfectDodgeThreat{source=source,attacker=attacker,seconds=time,stamp=stamp};return true;
                }
                previous=next;
            }
            return false;
        }
    }
}
