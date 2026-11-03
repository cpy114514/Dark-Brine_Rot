using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Mavis
{
    // Authored air-hit motion plus cinematic-only contact solving.
    public sealed class SahurAbductionPose : IDisposable
    {
        readonly Animator animator;readonly Transform root,hips,head;
        readonly Transform leftUpper,leftLower,leftHand,rightUpper,rightLower,rightHand;
        readonly PlayableGraph graph;readonly AnimationClipPlayable clip;readonly AnimationMixerPlayable mixer;
        readonly float speed,length;readonly bool rootMotion;readonly AnimatorCullingMode culling;
        public Vector3 Head=>head?head.position:root.position+Vector3.up*2;
        public Vector3 Hips=>hips?hips.position:root.position;
        public Vector3 Shoulders=>leftUpper&&rightUpper?(leftUpper.position+rightUpper.position)*.5f:Hips;
        public float ArmReach=>leftUpper&&leftLower&&leftHand?(Vector3.Distance(leftUpper.position,leftLower.position)+Vector3.Distance(leftLower.position,leftHand.position)):.7f;
        public float GripError {get;private set;}
        public SahurAbductionPose(ThirdPersonPlayerController player)
        {
            root=player.transform;animator=player.CharacterAnimator;
            if(!animator||!animator.isHuman)throw new InvalidOperationException("Abduction requires Sahur's humanoid Avatar");
            var source=Resources.LoadAll<AnimationClip>("Encounter/v214/CH1_SahurAirHit_v214/CH1_SahurAirHit_v214");
            AnimationClip motion=null;foreach(var c in source)if(!c.name.StartsWith("__preview__")){motion=c;break;}
            if(!motion)throw new InvalidOperationException("Missing authored Sahur air-hit clip");length=motion.length;
            speed=animator.speed;rootMotion=animator.applyRootMotion;culling=animator.cullingMode;
            animator.speed=1;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            graph=PlayableGraph.Create("Sahur abduction authored motion");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            clip=AnimationClipPlayable.Create(graph,motion);clip.SetApplyFootIK(false);clip.SetApplyPlayableIK(false);clip.SetSpeed(0);
            mixer=AnimationMixerPlayable.Create(graph,2);
            var idle=AnimatorControllerPlayable.Create(graph,animator.runtimeAnimatorController);idle.SetSpeed(0);
            graph.Connect(idle,0,mixer,0);graph.Connect(clip,0,mixer,1);
            mixer.SetInputWeight(0,1);mixer.SetInputWeight(1,0);
            var output=AnimationPlayableOutput.Create(graph,"Cinematic pose",animator);output.SetSourcePlayable(mixer);graph.Play();
            hips=animator.GetBoneTransform(HumanBodyBones.Hips);head=animator.GetBoneTransform(HumanBodyBones.Head);
            leftUpper=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);leftLower=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightUpper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);rightLower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
        public void Sample(float phase,float weight=1)
        {
            if(!graph.IsValid())return;Vector3 p=root.position;Quaternion q=root.rotation;
            mixer.SetInputWeight(0,1-Mathf.Clamp01(weight));mixer.SetInputWeight(1,Mathf.Clamp01(weight));
            clip.SetTime(Mathf.Clamp01(phase)*Mathf.Max(0,length-.001f));graph.Evaluate(0);root.SetPositionAndRotation(p,q);
        }
        public void Hold(Vector3 centre,Vector3 right,float span,float weight)
        {
            Vector3 a=centre-right*span,b=centre+right*span;
            Solve(leftUpper,leftLower,leftHand,a,-right,weight);Solve(rightUpper,rightLower,rightHand,b,right,weight);
            GripError=Mathf.Max(leftHand?Vector3.Distance(leftHand.position,a):0,rightHand?Vector3.Distance(rightHand.position,b):0);
        }
        static void Solve(Transform upper,Transform lower,Transform hand,Vector3 target,Vector3 pole,float weight)
        {
            if(!upper||!lower||!hand)return;target=Vector3.Lerp(hand.position,target,Mathf.Clamp01(weight));
            Vector3 shoulder=upper.position,delta=target-shoulder;float a=Vector3.Distance(shoulder,lower.position),b=Vector3.Distance(lower.position,hand.position);
            float distance=Mathf.Clamp(delta.magnitude,.001f,Mathf.Max(.002f,a+b-.001f));Vector3 axis=delta.normalized;
            var bend=Vector3.ProjectOnPlane(pole+Vector3.back*.15f,axis).normalized;
            if(bend.sqrMagnitude<.01f)bend=Vector3.Cross(axis,Vector3.forward).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Vector3 elbow=shoulder+axis*along+bend*height;
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
        }
        public void Dispose()
        {
            if(graph.IsValid())graph.Destroy();
            if(animator){animator.speed=speed;animator.applyRootMotion=rootMotion;animator.cullingMode=culling;}
        }
    }
}
