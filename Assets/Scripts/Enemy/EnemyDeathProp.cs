using UnityEngine;

namespace Mavis
{
    // A visual-only dropped weapon, without adding a new pickup or damage volume.
    public sealed class EnemyDeathProp : MonoBehaviour
    {
        Vector3 velocity;
        Quaternion from,to;
        Renderer[] parts;
        Collider floor;
        float age;
        bool landed;
        public void Drop(Transform owner)
        {
            transform.SetParent(owner,true);parts=GetComponentsInChildren<Renderer>();from=transform.rotation;
            var direction=Vector3.ProjectOnPlane(owner.forward,Vector3.up).normalized;
            to=Quaternion.LookRotation(direction.sqrMagnitude>.01f?direction:Vector3.forward,Vector3.up);
            velocity=owner.right*.55f+Vector3.up*.15f;
            var hits=Physics.RaycastAll(transform.position+Vector3.up*3,Vector3.down,100,~0,QueryTriggerInteraction.Ignore);
            float closest=float.PositiveInfinity;
            foreach(var hit in hits)if(!hit.transform.IsChildOf(owner)&&hit.collider.GetComponentInParent<IDamageable>()==null&&hit.normal.y>.4f&&hit.distance<closest){floor=hit.collider;closest=hit.distance;to=Quaternion.LookRotation(Vector3.ProjectOnPlane(direction,hit.normal).normalized,hit.normal);}
        }
        void LateUpdate()
        {
            float dt=Time.deltaTime;if(Time.timeScale<=0||dt<=0||PauseSettingsMenu.IsOpen||parts==null)return;age+=dt;
            if(!landed){velocity+=Vector3.down*(18*dt);transform.position+=velocity*dt;transform.rotation=Quaternion.Slerp(from,to,Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.55f)));}
            float lowest=float.PositiveInfinity;
            foreach(var part in parts)
            {
                if(!part||!part.enabled)continue;var b=part.bounds;
                if(floor&&floor.Raycast(new Ray(new Vector3(b.center.x,floor.bounds.max.y+2,b.center.z),Vector3.down),out var hit,floor.bounds.size.y+20))lowest=Mathf.Min(lowest,b.min.y-hit.point.y);
            }
            if(lowest<.015f||landed)
            {
                if(!float.IsInfinity(lowest))transform.position+=Vector3.up*(.018f-lowest);
                if(!landed){landed=true;from=transform.rotation;age=0;}
                transform.rotation=Quaternion.Slerp(from,to,Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.3f)));
                if(age>.4f)enabled=false;
            }
        }
    }
}
