using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
public static class VerifyBlenderSharkRig
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static object VerifyAssets()
    {
        Require(!Application.isPlaying,"Requires Edit Mode.");
        var set=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations");
        var go=UnityEngine.Object.Instantiate(set.rigPrefab);var animator=go.GetComponentInChildren<Animator>();animator.enabled=false;
        var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
        try
        {
            var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/SharkAnimation/TralaleroAnimated.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            Require(clips.Length==7 && skin.bones.Length==16,"Imported rig or actions missing.");
            Require(animator.avatar!=null&&animator.avatar.isValid,"Generic shark avatar invalid.");
            float seam=0,tailTravel=0,jawAngle=0;
            var tail=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Marker");
            var jaw=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="Jaw");
            foreach(var clip in clips)
            {
                clip.SampleAnimation(animator.gameObject,0);skin.BakeMesh(mesh);var start=mesh.vertices;
                Vector3 tailStart=tail.position;Quaternion jawStart=jaw.localRotation;
                for(int i=0;i<=30;i++)
                {
                    clip.SampleAnimation(animator.gameObject,clip.length*i/30);skin.BakeMesh(mesh);
                    foreach(var p in mesh.vertices)Require(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z),"Invalid skinned vertex.");
                    if(clip.name=="Tail_Strike")tailTravel=Mathf.Max(tailTravel,(tail.position-tailStart).magnitude);
                    if(clip.name=="Bite_Lunge")jawAngle=Mathf.Max(jawAngle,Quaternion.Angle(jawStart,jaw.localRotation));
                }
                if(clip.isLooping)
                {
                    var end=mesh.vertices;
                    for(int i=0;i<start.Length;i++)seam=Mathf.Max(seam,(start[i]-end[i]).magnitude);
                }
            }
            Require(seam<.001f,"Swim loop has a visible pose seam.");
            Require(tailTravel>.05f&&jawAngle>20,"Tail strike or biting pose did not animate.");
            foreach(var pair in new[]{("Tail_Strike",set.tailStrikeContact),("Ship_Smash",set.shipSmashContact)})
            {
                clips.Single(c=>c.name==pair.Item1).SampleAnimation(animator.gameObject,.65f);
                Require((go.transform.InverseTransformPoint(tail.position)-pair.Item2).magnitude<.00001f,"Tail contact sample does not match baked animation.");
            }
            return new{boneCount=16,authoredActions=7,swimLoopSeam=seam,tailTravelMeshUnits=tailTravel,maximumJawAngle=jawAngle,contactMatchesAuthoredFrame=true};
        }
        finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(go);}
    }
    public static async Task<object> VerifyRuntime()
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
        while(!loading.isDone)await Task.Delay(40);await Task.Delay(300);
        var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
        Require(shark.UsesBlenderRig && shark.VisualRenderer is SkinnedMeshRenderer,"Story is not using the Blender rig.");
        Require(!sequence.swimmer.GetComponentInChildren<MeshFilter>().GetComponent<Renderer>().enabled,"The static and animated sharks render together.");
        var bone=shark.BoneAnimator.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Base");
        Quaternion before=bone.localRotation;await Task.Delay(350);
        Require(Quaternion.Angle(before,bone.localRotation)>1,"Swimming bones are stationary.");
        shark.PlayBite();await Task.Delay(300);Require(shark.BoneAnimator.GetCurrentAnimatorStateInfo(0).IsName("Bite_Lunge"),"Bite did not play.");
        shark.PlayRecoil();await Task.Delay(220);Require(shark.BoneAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hit_Recoil"),"Recoil did not play.");
        await Task.Delay(1000);Require(shark.BoneAnimator.GetCurrentAnimatorStateInfo(0).IsName("Swim"),"Shark did not recover swimming after a reaction.");
        sequence.BeginImpact();await Task.Delay(1600);Require(shark.BoneAnimator.GetCurrentAnimatorStateInfo(0).IsName("Ship_Smash"),"Ship impact did not use the authored smash.");
        return new{storyUsesBlenderRig=true,noDoubleMesh=true,swimmingBonesMove=true,bitePlays=true,hitReactionPlays=true,returnsToSwimming=true,shipSmashPlays=true};
    }
}
