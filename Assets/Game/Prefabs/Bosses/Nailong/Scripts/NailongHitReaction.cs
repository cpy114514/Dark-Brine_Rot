using UnityEngine;

namespace Mavis
{
    [DefaultExecutionOrder(100)]
    public sealed class NailongHitReaction : MonoBehaviour
    {
        Transform spine, head;
        NailongHealth health;
        [Range(0,20)] public float lightRecoilDegrees=9;
        [Range(0,35)] public float heavyRecoilDegrees=22;
        float elapsed, duration, strength;
        Vector3 impactDirection=Vector3.back;
        float baseStrength;
        bool pendingTuning;
        Quaternion spineBase,headBase,spineApplied,headApplied;
        bool offsetApplied;
        public bool IsReacting => health != null && !health.IsDead && elapsed < duration;
        void Awake()
        {
            health = GetComponent<NailongHealth>();
            var skin = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null) return;
            foreach (var bone in skin.bones)
            {
                if (bone == null) continue;
                if (bone.name == "Spine2") spine = bone;
                if (bone.name == "Head") head = bone;
            }
        }
        public void Hit(Vector3 point, CombatHitKind kind)
        {
            pendingTuning=false;
            bool heavy = kind == CombatHitKind.ChargedHeavy || kind == CombatHitKind.JumpSlash;
            float nextStrength = heavy ? heavyRecoilDegrees : kind == CombatHitKind.ComboFinisher ? lightRecoilDegrees*2 : kind == CombatHitKind.ComboTwo ? lightRecoilDegrees*1.25f : lightRecoilDegrees;
            if (IsReacting && strength > nextStrength) return;
            strength = nextStrength;
            baseStrength=nextStrength;pendingTuning=true;
            duration = heavy ? 0.48f : kind == CombatHitKind.ComboFinisher ? 0.36f : 0.28f;
            elapsed=0;impactDirection=Vector3.ProjectOnPlane(transform.position-point,Vector3.up).normalized;
            if(impactDirection.sqrMagnitude<.001f)impactDirection=-transform.forward;
        }
        public void SetImpactDirection(Vector3 direction,float multiplier)
        {
            if(!pendingTuning)return;
            pendingTuning=false;
            strength=Mathf.Min(32,baseStrength*Mathf.Clamp(multiplier,0,2));
            var planar=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
            impactDirection=planar.sqrMagnitude>.001f?planar:-transform.forward;
        }
        void RemoveOffset()
        {
            if(!offsetApplied)return;
            if(spine && Quaternion.Angle(spine.localRotation,spineApplied)<.01f)spine.localRotation=spineBase;
            if(head && Quaternion.Angle(head.localRotation,headApplied)<.01f)head.localRotation=headBase;
            offsetApplied=false;
        }
        void Update(){if(Time.timeScale>0)RemoveOffset();}
        void LateUpdate()
        {
            if(Time.timeScale<=0)
            {
                if(offsetApplied && IsReacting)
                {
                    if(spine)spine.localRotation=spineApplied;
                    if(head)head.localRotation=headApplied;
                }
                return;
            }
            RemoveOffset();
            if (!IsReacting) return;
            // The confirmed hit's visual stop finishes before recoil spends its clock.
            if(CombatLocalHitStop.Active(gameObject))return;
            elapsed=Mathf.Min(duration,elapsed+Time.deltaTime);
            float t=elapsed/Mathf.Max(.001f,duration);
            float envelope=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/.18f)*.5f)*Mathf.Pow(1-t,1.5f);
            // Model bone axes differ from actor axes. Lean in the measured
            // world hit direction, then remember the exact local additive pose.
            var axis=Vector3.Cross(Vector3.up,impactDirection);
            if(spine){spineBase=spine.localRotation;spine.rotation=Quaternion.AngleAxis(strength*envelope,axis)*spine.rotation;spineApplied=spine.localRotation;}
            if(head){headBase=head.localRotation;head.rotation=Quaternion.AngleAxis(strength*.3f*envelope,axis)*head.rotation;headApplied=head.localRotation;}
            offsetApplied=true;
        }
        void OnDisable(){RemoveOffset();elapsed=duration;}
    }
}
