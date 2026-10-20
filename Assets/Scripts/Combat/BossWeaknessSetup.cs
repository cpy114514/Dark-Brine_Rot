using UnityEngine;
namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class BossWeaknessSetup : MonoBehaviour
    {
        void Start()=>Initialize();
        public void Initialize()
        {
            // Also refit existing aircraft points after a runtime/editor refresh.
            if(GetComponent<BombardinoBoss>() && FitEngineWeakPoints())return;
            if(GetComponentInChildren<BossWeakPoint>())return;
            // Capri renders a fitted cup rig, while its humanoid Animator is only
            // a motion source. Anchor the weak point to the visible rig's chest.
            var cupRig=GetComponentInChildren<CappuccinoMotionRig>(true);
            if(GetComponent<CappuccinoAI>() && cupRig && cupRig.bindings!=null)
            {
                Transform chest=null;
                foreach(var binding in cupRig.bindings)
                    if(binding.sourceBone==HumanBodyBones.Chest && binding.target){chest=binding.target;break;}
                var body=System.Array.Find(GetComponentsInChildren<SkinnedMeshRenderer>(true),r=>r.name=="Capuchino");
                if(chest && body)
                {
                    var weak=BossWeakPoint.Create(chest,Vector3.zero,.45f);
                    weak.LocationLabel="Back weak point";
                    weak.transform.position=body.bounds.center+Vector3.up*body.bounds.size.y*.15f-transform.forward*body.bounds.extents.z*.7f;
                    return;
                }
            }
            var animator=GetComponentInChildren<Animator>();
            if(animator && animator.isHuman)
            {
                bool nailong=GetComponent<NailongHealth>()!=null;
                var anchor=animator.GetBoneTransform(nailong?HumanBodyBones.Head:HumanBodyBones.Chest);
                if(anchor)
                {
                    // The head is Nailong's weak point; Capri's upper back rewards flanking.
                    var weak=BossWeakPoint.Create(anchor,Vector3.zero,nailong?.5f:.45f);
                    weak.LocationLabel=nailong?"Head weak point":"Back weak point";
                    if(!nailong)weak.transform.position=anchor.position-transform.forward*.45f;
                    return;
                }
            }
            var meshes=GetComponentsInChildren<Renderer>();
            if(meshes.Length==0)return;Bounds bounds=meshes[0].bounds;
            foreach(var r in meshes)bounds.Encapsulate(r.bounds);
            if(GetComponent<NailongHealth>() || GetComponent<CappuccinoAI>())
            {
                bool nailong=GetComponent<NailongHealth>()!=null;
                Transform anchor=transform;
                foreach(var t in GetComponentsInChildren<Transform>())
                    if(t.name.ToLowerInvariant().Contains(nailong?"head":"spine2")){anchor=t;break;}
                var weak=BossWeakPoint.Create(anchor,Vector3.zero,nailong?.55f:.45f);
                weak.LocationLabel=nailong?"Head weak point":"Back weak point";
                weak.transform.position=nailong?new Vector3(bounds.center.x,bounds.max.y-bounds.size.y*.22f,bounds.center.z)+transform.forward*bounds.extents.z*.65f:
                    bounds.center+Vector3.up*bounds.size.y*.15f-transform.forward*bounds.extents.z*.85f;
                return;
            }
            if(FitEngineWeakPoints())return;
            // Two physical engine weak points on the aircraft's underside.
            foreach(float side in new[]{-1f,1f})
            {
                var weak=BossWeakPoint.Create(transform,Vector3.zero,Mathf.Max(.65f,bounds.size.y*.15f));
                weak.LocationLabel=side<0?"Left engine weak point":"Right engine weak point";
                weak.transform.position=bounds.center+transform.right*(bounds.extents.x*.45f*side)-Vector3.up*(bounds.extents.y*.6f);
            }
        }
        bool FitEngineWeakPoints()
        {
            int engines=0;
            foreach(var c in GetComponentsInChildren<BoxCollider>())
                if(c.name=="Left engine" || c.name=="Right engine")
                {
                    // Awake lifts the aircraft before Physics has synced its colliders.
                    // Fit in engine space, avoiding stale ground bounds and banked AABBs.
                    Vector3 underside=c.center-Vector3.up*(c.size.y*.325f);
                    float scale=Mathf.Max(.001f,Mathf.Abs(c.transform.lossyScale.y));
                    float radius=Mathf.Max(.65f,c.size.y*scale*.45f);
                    var weak=c.GetComponentInChildren<BossWeakPoint>(true);
                    if(!weak)weak=BossWeakPoint.Create(c.transform,underside,radius);
                    weak.transform.localPosition=underside;
                    weak.transform.localRotation=Quaternion.identity;
                    weak.transform.localScale=Vector3.one*(radius*2/scale);
                    weak.LocationLabel=c.name=="Left engine"?"Left engine weak point":"Right engine weak point";
                    engines++;
                }
            return engines>0;
        }
    }
}
