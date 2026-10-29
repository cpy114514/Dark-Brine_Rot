using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Retarget KayKit's jump and shape a single-foot lunar bound in humanoid muscle space.
public static class AuthorMarshmallowSpacewalk
{
    const string Folder="Assets/Game/Prefabs/Characters/Sahur/Animations/Marshmallow";
    static int Muscle(string name)=>Array.IndexOf(HumanTrait.MuscleName,name);
    static void Limit(ref HumanPose pose,string name,float min,float max)
    {int i=Muscle(name);if(i>=0)pose.muscles[i]=Mathf.Clamp(pose.muscles[i],min,max);}
    static float Curve(float t,float[] values)
    {
        float[] times={0,.18f,.42f,.65f,.86f,1};
        for(int i=1;i<times.Length;i++)if(t<=times[i])
            return Mathf.Lerp(values[i-1],values[i],Mathf.SmoothStep(0,1,Mathf.InverseLerp(times[i-1],times[i],t)));
        return values[values.Length-1];
    }
    public static object Bake()
    {
        if(Application.isPlaying)throw new Exception("Bake outside Play Mode");
        System.IO.Directory.CreateDirectory(Folder);
        var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var walk=AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/KayKitLunarBound/Rig_Medium_MovementBasic.fbx")
            .OfType<AnimationClip>().First(c=>c.name=="Jump_Full_Long");
        var arms=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Source/Walking.fbx")
            .OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
        GameObject rig=null;HumanPoseHandler handler=null;AnimatorController controller=null;
        var clips=new List<AnimationClip>();float maxAnkle=0,maxFootTwist=0,maxToe=0;
        try
        {
            rig=UnityEngine.Object.Instantiate(player.transform.Find("Pbr Sahur Visual").gameObject);
            rig.name="Temporary constrained spacewalk rig";rig.hideFlags=HideFlags.HideAndDontSave;
            rig.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);rig.transform.localScale=Vector3.one;
            foreach(var script in rig.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
            var animator=rig.GetComponent<Animator>();animator.avatar=player.GetComponent<Animator>().avatar;
            controller=new AnimatorController();controller.AddLayer("Base Layer");
            var state=controller.layers[0].stateMachine.AddState("Lunar Jump");state.motion=walk;
            var armState=controller.layers[0].stateMachine.AddState("Arm Swing");armState.motion=arms;
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.enabled=true;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.fireEvents=false;
            animator.Rebind();animator.Play("Lunar Jump",0,0);animator.Update(0);
            handler=new HumanPoseHandler(animator.avatar,animator.transform);
            var bones=MarshmallowSpacewalk.PoseBones.Select(animator.GetBoneTransform).ToArray();
            var initial=new HumanPose();handler.GetHumanPose(ref initial);float baseBodyY=initial.bodyPosition.y;
            foreach(bool left in new[]{true,false})
            {
                var clip=new AnimationClip{name=left?"MoonStrideLeft":"MoonStrideRight",frameRate=60,legacy=true};
                var curves=new Dictionary<string,AnimationCurve[]>();
                foreach(var bone in bones.Where(b=>b))curves[AnimationUtility.CalculateTransformPath(bone,animator.transform)]=
                    Enumerable.Range(0,4).Select(_=>new AnimationCurve()).ToArray();
                var hipPosition=Enumerable.Range(0,3).Select(_=>new AnimationCurve()).ToArray();
                for(int frame=0;frame<=60;frame++)
                {
                    float t=frame/60f;
                    animator.Play("Arm Swing",0,Mathf.Repeat((left?0:.5f)+t*.5f,1));animator.Update(0);
                    var armPose=new HumanPose();handler.GetHumanPose(ref armPose);
                    animator.Play("Lunar Jump",0,Mathf.Min(.9999f,t));animator.Update(0);
                    var pose=new HumanPose();handler.GetHumanPose(ref pose);
                    // Keep the jump's torso/legs but use Sahur's natural counter-swing arms.
                    for(int arm=37;arm<=54;arm++)pose.muscles[arm]=armPose.muscles[arm];
                    foreach(string side in new[]{"Left","Right"})
                    {
                        int hip=Muscle(side+" Upper Leg Front-Back");
                        bool leading=(side=="Left")==left;
                        pose.muscles[hip]=Curve(t,leading?new[]{.88f,-.18f,-.38f,-.30f,-.06f,.15f}:
                            new[]{.15f,.55f,.92f,.95f,.92f,.88f});
                        Limit(ref pose,side+" Upper Leg In-Out",-.12f,.12f);
                        Limit(ref pose,side+" Upper Leg Twist In-Out",-.15f,.15f);
                        int knee=Muscle(side+" Lower Leg Stretch");
                        // The advancing leg folds while it swings through, then
                        // extends for landing. The supporting leg straightens to
                        // push off before folding back in flight. Clip endpoints
                        // match the opposite leg at the next bound.
                        pose.muscles[knee]=Curve(t,leading?new[]{.55f,.32f,.48f,.70f,.77f,.70f}:
                            new[]{.70f,.88f,.72f,.52f,.50f,.55f});
                        Limit(ref pose,side+" Lower Leg Twist In-Out",-.1f,.1f);
                        Limit(ref pose,side+" Foot Up-Down",-.32f,.32f);
                        Limit(ref pose,side+" Foot Twist In-Out",-.12f,.12f);
                        Limit(ref pose,side+" Toes Up-Down",-.12f,.12f);
                        maxAnkle=Mathf.Max(maxAnkle,Mathf.Abs(pose.muscles[Muscle(side+" Foot Up-Down")]));
                        maxFootTwist=Mathf.Max(maxFootTwist,Mathf.Abs(pose.muscles[Muscle(side+" Foot Twist In-Out")]));
                        maxToe=Mathf.Max(maxToe,Mathf.Abs(pose.muscles[Muscle(side+" Toes Up-Down")]));
                    }
                    foreach(string part in new[]{"Spine","Chest","UpperChest"})
                    {
                        Limit(ref pose,part+" Front-Back",-.08f,.08f);
                        Limit(ref pose,part+" Left-Right",-.06f,.06f);
                        Limit(ref pose,part+" Twist Left-Right",-.08f,.08f);
                    }
                    Limit(ref pose,"Neck Nod Down-Up",-.06f,.06f);
                    Limit(ref pose,"Head Nod Down-Up",-.08f,.08f);
                    Limit(ref pose,"Neck Tilt Left-Right",-.04f,.04f);
                    Limit(ref pose,"Head Tilt Left-Right",-.04f,.04f);
                    var rotation=pose.bodyRotation.eulerAngles;
                    pose.bodyRotation=Quaternion.Euler(Mathf.Clamp(Mathf.DeltaAngle(0,rotation.x),-4,6),
                        Mathf.Clamp(Mathf.DeltaAngle(0,rotation.y),-5,5),Mathf.Clamp(Mathf.DeltaAngle(0,rotation.z),-3,3));
                    // Source arm swing and torso are retained. Body translation stays local.
                    pose.bodyPosition=new Vector3(0,baseBodyY+Mathf.Clamp(pose.bodyPosition.y-baseBodyY,-.05f,.05f),0);
                    handler.SetHumanPose(ref pose);
                    if(bones[0].localPosition.sqrMagnitude>25)throw new Exception("Hip tracks must stay local");
                    foreach(var bone in bones.Where(b=>b))
                    {
                        var curve=curves[AnimationUtility.CalculateTransformPath(bone,animator.transform)];var q=bone.localRotation;
                        curve[0].AddKey(t,q.x);curve[1].AddKey(t,q.y);curve[2].AddKey(t,q.z);curve[3].AddKey(t,q.w);
                    }
                    var hp=bones[0].localPosition;hipPosition[0].AddKey(t,hp.x);hipPosition[1].AddKey(t,hp.y);hipPosition[2].AddKey(t,hp.z);
                }
                foreach(var pair in curves)for(int axis=0;axis<4;axis++)
                    clip.SetCurve(pair.Key,typeof(Transform),"localRotation."+"xyzw"[axis],pair.Value[axis]);
                string hips=AnimationUtility.CalculateTransformPath(bones[0],animator.transform);
                for(int axis=0;axis<3;axis++)clip.SetCurve(hips,typeof(Transform),"localPosition."+"xyz"[axis],hipPosition[axis]);
                clip.EnsureQuaternionContinuity();
                string thighPath=AnimationUtility.CalculateTransformPath(bones[3],animator.transform);
                var track=curves[thighPath];
                Quaternion At(float time)=>new Quaternion(track[0].Evaluate(time),track[1].Evaluate(time),track[2].Evaluate(time),track[3].Evaluate(time));
                float variation=Mathf.Max(Quaternion.Angle(At(0),At(.5f)),Quaternion.Angle(At(0),At(1)));
                if(variation<10)throw new Exception("Walk bake did not sample a moving leg: "+variation);
                string path=Folder+"/"+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;}
                else AssetDatabase.CreateAsset(clip,path);
                EditorUtility.SetDirty(clip);clips.Add(clip);
            }
            var gait=player.GetComponent<MarshmallowSpacewalk>();if(!gait)gait=Undo.AddComponent<MarshmallowSpacewalk>(player.gameObject);
            Undo.RecordObject(gait,"Assign constrained astronaut stride clips");gait.leftStride=clips[0];gait.rightStride=clips[1];EditorUtility.SetDirty(gait);
            AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            return new{baked=true,source=walk.name,maxAnkle,maxFootTwist,maxToe,clips=clips.Select(c=>AssetDatabase.GetAssetPath(c)).ToArray()};
        }
        finally{handler?.Dispose();if(rig)UnityEngine.Object.DestroyImmediate(rig);if(controller)UnityEngine.Object.DestroyImmediate(controller);}
    }
}

