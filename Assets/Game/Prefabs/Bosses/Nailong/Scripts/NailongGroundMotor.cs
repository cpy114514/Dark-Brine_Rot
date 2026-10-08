using UnityEngine;

namespace Mavis
{
    /// <summary>Collision and shore-safe movement when the island has no baked NavMesh.</summary>
    [DisallowMultipleComponent]
    public sealed class NailongGroundMotor : MonoBehaviour
    {
        CharacterController motor;
        CapsuleCollider original;
        OceanWorld ocean;
        readonly RaycastHit[] hits = new RaycastHit[32];
        static readonly float[] angles = { 0, 35, 70, -35, -70 };
        float side = 1;
        float Sole => (motor.center.y-motor.height*.5f)*Mathf.Abs(transform.lossyScale.y);

        public void Initialize()
        {
            original = GetComponent<CapsuleCollider>();
            motor = GetComponent<CharacterController>();
            if (!motor) motor = gameObject.AddComponent<CharacterController>();
            motor.enabled = false;
            motor.center = original ? original.center : Vector3.up*.54f;
            motor.height = original ? original.height : 1.72f;
            // The old narrow capsule covered only the spine, leaving the belly outside it.
            motor.radius = Mathf.Min(motor.height*.48f, original ? Mathf.Max(original.radius,.48f) : .48f);
            motor.stepOffset = .12f; motor.skinWidth = .025f; motor.slopeLimit = 55;
            if (original) original.enabled = false;
            ocean = FindFirstObjectByType<OceanWorld>();
            if (Ground(transform.position, out var ground))
                transform.position = new Vector3(transform.position.x, ground.point.y + .04f - Sole, transform.position.z);
            motor.enabled = true;
        }

        bool Ground(Vector3 at, out RaycastHit ground)
        {
            ground=default;float nearest=float.PositiveInfinity;
            float bodyHeight = motor.height * Mathf.Abs(transform.lossyScale.y);
            int count=Physics.RaycastNonAlloc(at+Vector3.up*Mathf.Max(6f,bodyHeight*.75f),Vector3.down,hits,Mathf.Max(20f,bodyHeight*2f),~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];
                if(hit.transform.IsChildOf(transform)||hit.collider.GetComponentInParent<IDamageable>()!=null)continue;
                if(Vector3.Angle(hit.normal,Vector3.up)>motor.slopeLimit||hit.distance>=nearest)continue;
                nearest=hit.distance;ground=hit;
            }
            return nearest<float.PositiveInfinity;
        }

        public bool Move(Vector3 delta, bool avoid=true)
        {
            if(!motor||!motor.enabled||delta.sqrMagnitude<.000001f)return false;
            Vector3 before=transform.position;
            float scale=Mathf.Abs(transform.lossyScale.y),radius=motor.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z));
            foreach(float angle in angles)
            {
                if(!avoid&&angle!=0)continue;
                Vector3 move=Quaternion.AngleAxis(angle*side,Vector3.up)*delta;
                Vector3 lookahead=transform.position+move.normalized*Mathf.Max(move.magnitude,radius*.55f);
                if(!Ground(lookahead,out var ground))continue;
                float sole=transform.position.y+Sole;
                float terrainTolerance = NailongSize.RangeFactor(transform);
                if(ground.point.y<sole-1.1f*terrainTolerance||ground.point.y>sole+1.4f*terrainTolerance)continue;
                if(ocean&&ground.point.y<ocean.oceanHeight+.15f)continue;
                Vector3 center=transform.TransformPoint(motor.center);
                float half=Mathf.Max(0,motor.height*scale*.5f-radius);
                int count=Physics.CapsuleCastNonAlloc(center-Vector3.up*Mathf.Max(0,half-.5f),center+Vector3.up*half,
                    radius*.92f,move.normalized,hits,Mathf.Max(move.magnitude,.4f),~0,QueryTriggerInteraction.Ignore);
                bool blocked=false;
                for(int i=0;i<count;i++)
                    if(!hits[i].transform.IsChildOf(transform)&&Vector3.Angle(hits[i].normal,Vector3.up)>motor.slopeLimit){blocked=true;break;}
                if(blocked)continue;
                motor.Move(move+Vector3.up*(ground.point.y+.04f-Sole-transform.position.y));
                if(angle<0)side=-side;
                break;
            }
            if(Ground(transform.position,out var actual))motor.Move(Vector3.up*(actual.point.y+.04f-Sole-transform.position.y));
            return Vector3.ProjectOnPlane(transform.position-before,Vector3.up).sqrMagnitude>.00001f;
        }
    }
}
