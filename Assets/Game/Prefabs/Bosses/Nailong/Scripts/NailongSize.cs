using UnityEngine;
namespace Mavis
{
    public static class NailongSize
    {
        public static float RangeFactor(Transform actor)=>Mathf.Max(.01f,Mathf.Abs(actor.lossyScale.x));
        public static Vector3 Feet(Transform actor)
        {
            var capsule=actor.GetComponent<CharacterController>();
            if(capsule!=null && capsule.enabled)return new Vector3(actor.position.x,capsule.bounds.min.y,actor.position.z);
            var animator=actor.GetComponentInChildren<Animator>();
            if(animator!=null && animator.isHuman)
            {
                var a=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var b=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if(a!=null && b!=null)return new Vector3(actor.position.x,Mathf.Min(a.position.y,b.position.y),actor.position.z);
            }
            return actor.position;
        }
    }
}
