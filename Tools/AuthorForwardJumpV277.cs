using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Mavis;

// Bake a forward/down cut onto the retained smoothed jump, never runtime IK.
public static class AuthorForwardJumpV277
{
    const string Folder=".codex/jump-forward-v277";
    public static object Install()
    {
        Directory.CreateDirectory(Folder+"/controllers-before");
        var paths=new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
        foreach(var path in paths)
        {
            string backup=Folder+"/controllers-before/"+Path.GetFileName(path);if(!File.Exists(backup))File.Copy(path,backup);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            foreach(var pair in new[]{("Jump Slash","Club"),("Capri Jump Slash","Blade")})
            {
                var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name==pair.Item1).state;
                var clip=Resources.Load<AnimationClip>("Combat/Combinations/"+pair.Item2+"JumpChopV277");if(!clip)throw new Exception("Missing forward chop");
                state.motion=clip;EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        return new{installed=true,paths};
    }
    static float Ease(float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(x));
    static float Value(float t,float[] times,float[] values)
    {for(int i=1;i<times.Length;i++)if(t<=times[i])return Mathf.Lerp(values[i-1],values[i],Ease(Mathf.InverseLerp(times[i-1],times[i],t)));return values.Last();}
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    static bool Right(string n)=>new[]{"Right Shoulder ","Right Arm ","Right Forearm ","Right Hand "}.Any(n.StartsWith);
    static void Arm(Transform upper,Transform lower,Transform hand,Vector3 target,Vector3 hint)
    {
        var origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,hand.position);
        var axis=(target-origin).normalized;float d=Mathf.Clamp(Vector3.Distance(origin,target),Mathf.Abs(a-b)+.001f,a+b-.005f);
        var bend=Vector3.ProjectOnPlane(hint-origin,axis).normalized;
        float x=(a*a-b*b+d*d)/(2*d),y=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));var elbow=origin+axis*x+bend*y;
        upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,origin+axis*d-lower.position)*lower.rotation;
    }
    static Quaternion Basis(Vector3 local,Vector3 world,Vector3 localAcross,Vector3 worldAcross)
    {
        local.Normalize();world.Normalize();localAcross=Vector3.ProjectOnPlane(localAcross,local).normalized;worldAcross=Vector3.ProjectOnPlane(worldAcross,world).normalized;
        var localBasis=Quaternion.LookRotation(local,Vector3.Cross(local,localAcross));
        var worldBasis=Quaternion.LookRotation(world,Vector3.Cross(world,worldAcross));
        return worldBasis*Quaternion.Inverse(localBasis);
    }
    static void Segment(CapsuleCollider c,out Vector3 a,out Vector3 b)
    {Vector3 axis=c.direction==0?Vector3.right:c.direction==1?Vector3.up:Vector3.forward;float half=Mathf.Max(0,c.height*.5f-c.radius);a=c.transform.TransformPoint(c.center-axis*half);b=c.transform.TransformPoint(c.center+axis*half);}
    public static object Run()
    {
        if(!Application.isPlaying)throw new Exception("Use the existing Play session for the initialized native avatar");
        Directory.CreateDirectory(Folder);var report=new List<object>();var cursor=Cursor.lockState;bool visible=Cursor.visible;
        foreach(bool blades in new[]{false,true})
        {
            GameObject container=null;AnimatorController controller=null;HumanPoseHandler handler=null;
            try
            {
                container=new GameObject("Temporary forward chop baking rig");container.SetActive(false);container.hideFlags=HideFlags.DontSave;container.transform.position=new Vector3(12000,500,12000);
                var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),container.transform);actor.AddComponent<GameSaveExcluded>();
                foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
                container.SetActive(true);foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
                var attack=actor.GetComponent<SahurAttack>();var motor=actor.GetComponent<ThirdPersonPlayerController>();var weapons=actor.GetComponent<SahurWeaponLoadout>();motor.enabled=weapons.enabled=attack.enabled=true;
                motor.ExternalControlLock=motor.ExternalMovementLock=false;
                if(blades)weapons.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);
                var animator=attack.animator;var source=Resources.Load<AnimationClip>("Combat/Combinations/"+(blades?"Blade":"Club")+"JumpChopV276");
                controller=new AnimatorController();controller.AddLayer("Base Layer");var state=controller.layers[0].stateMachine.AddState("Source");state.motion=source;
                animator.runtimeAnimatorController=controller;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
                foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
                handler=new HumanPoseHandler(animator.avatar,animator.transform);
                var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var lower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
                var chest=animator.GetBoneTransform(HumanBodyBones.Chest);var cap=(CapsuleCollider)attack.stickHitbox;
                animator.Play("Source",0,0);animator.Update(0);Segment(cap,out var ca,out var cb);
                var shaft=(Vector3.Distance(ca,hand.position)>Vector3.Distance(cb,hand.position)?ca-cb:cb-ca).normalized;
                Vector3 localShaft=Quaternion.Inverse(hand.rotation)*shaft;
                int[] indices=Enumerable.Range(0,HumanTrait.MuscleCount).Where(i=>Right(HumanTrait.MuscleName[i])).ToArray();
                var curves=indices.ToDictionary(i=>i,i=>new AnimationCurve());var sourceData=new List<object>();
                Quaternion previousHand=hand.rotation;bool havePrevious=false;
                for(int frame=0;frame<=51;frame++)
                {
                    float phase=frame/51f,time=frame/60f;animator.Play("Source",0,phase);animator.Update(0);
                    var originalHand=hand.rotation;var originalPosition=hand.position;
                    float weight=Ease((phase-.02f)/.20f)*(1-Ease((phase-.70f)/.28f));
                    float reach=Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position);
                    var shoulder=actor.transform.InverseTransformPoint(upper.position);var original=actor.transform.InverseTransformPoint(originalPosition);
                    float forward=Value(phase,new[]{0f,.22f,.34f,.48f,.64f,.82f,1f},new[]{.38f,.42f,.65f,.87f,.82f,.65f,.45f});
                    float side=Value(phase,new[]{0f,.24f,.40f,.58f,.75f,1f},new[]{.22f,.20f,.10f,-.04f,-.10f,.10f});
                    if(blades)side=Value(phase,new[]{0f,.24f,.34f,.43f,.58f,.75f,1f},new[]{.64f,.64f,.40f,.06f,-.04f,-.10f,.10f});
                    var targetLocal=new Vector3(side,original.y,shoulder.z+reach/actor.transform.lossyScale.x*forward);
                    var target=Vector3.Lerp(originalPosition,actor.transform.TransformPoint(targetLocal),weight);
                    var hint=chest.position+actor.transform.right*reach*.7f-actor.transform.up*reach*.22f-actor.transform.forward*reach*.12f;
                    if(weight>.0001f)Arm(upper,lower,hand,target,Vector3.Lerp(lower.position,hint,weight));
                    float elevation=Value(phase,new[]{0f,.22f,.34f,.44f,.54f,.66f,.82f,1f},new[]{55f,65f,40f,-5f,-38f,-60f,-48f,-30f})*Mathf.Deg2Rad;
                    if(blades)elevation=Value(phase,new[]{0f,.22f,.34f,.44f,.54f,.66f,.82f,1f},new[]{45f,50f,25f,-5f,-38f,-60f,-48f,-30f})*Mathf.Deg2Rad;
                    float outward=blades?Value(phase,new[]{0f,.24f,.34f,.43f,1f},new[]{.28f,.28f,.12f,.035f,.035f}):.035f;
                    var direction=actor.transform.TransformDirection(new Vector3(outward,Mathf.Sin(elevation),Mathf.Cos(elevation))).normalized;
                    var basis=Basis(localShaft,direction,Vector3.up,actor.transform.right);
                    HumanPose bestPose=default;float best=float.PositiveInfinity;Quaternion bestHand=originalHand;
                    for(int roll=-180;roll<=180;roll+=15)
                    {
                        hand.rotation=Quaternion.Slerp(originalHand,Quaternion.AngleAxis(roll,direction)*basis,weight);
                        var pose=new HumanPose();handler.GetHumanPose(ref pose);float cost=0;
                        foreach(int i in indices){float value=pose.muscles[i];float over=Mathf.Max(0,Mathf.Abs(value)-.95f);cost+=over*over*30+value*value*.15f;}
                        if(havePrevious)cost+=Mathf.Pow(Quaternion.Angle(previousHand,hand.rotation)/35,2)*.7f;
                        if(cost<best){best=cost;bestPose=pose;bestHand=hand.rotation;}
                    }
                    hand.rotation=bestHand;previousHand=bestHand;havePrevious=true;
                    foreach(int i in indices)curves[i].AddKey(time,bestPose.muscles[i]);
                    sourceData.Add(new{phase,cost=best,weight,direction=V(actor.transform.InverseTransformDirection(direction)),target=V(targetLocal)});
                }
                var clip=UnityEngine.Object.Instantiate(source);clip.name=(blades?"Blade":"Club")+"JumpChopV277";
                foreach(int i in indices)
                {
                    var raw=curves[i];var c=new AnimationCurve();string muscle=HumanTrait.MuscleName[i];
                    float limit=muscle.Contains("Twist")?.9f:muscle.Contains("Hand ")?.85f:1f;float[] weights={1,4,6,4,1};
                    for(int frame=0;frame<=51;frame++){float sum=0;for(int j=-2;j<=2;j++)sum+=Mathf.Clamp(raw.Evaluate(Mathf.Clamp(frame+j,0,51)/60f),-limit,limit)*weights[j+2];c.AddKey(frame/60f,sum/16);}
                    for(int k=0;k<c.length;k++){AnimationUtility.SetKeyLeftTangentMode(c,k,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(c,k,AnimationUtility.TangentMode.ClampedAuto);}
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),HumanTrait.MuscleName[i]),c);
                }
                string path="Assets/Resources/Combat/Combinations/"+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;}else AssetDatabase.CreateAsset(clip,path);
                EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
                state.motion=clip;animator.Rebind();var samples=new List<object>();
                foreach(float phase in new[]{.28f,.34f,.40f,.46f,.52f,.60f,.67f})
                {
                    animator.Play("Source",0,phase);animator.Update(0);Segment(cap,out ca,out cb);
                    var tip=Vector3.Distance(ca,hand.position)>Vector3.Distance(cb,hand.position)?ca:cb;
                    var dir=actor.transform.InverseTransformDirection((tip-hand.position).normalized);
                    samples.Add(new{phase,tip=V(actor.transform.InverseTransformPoint(tip)),hand=V(actor.transform.InverseTransformPoint(hand.position)),direction=V(dir)});
                }
                report.Add(new{blades,path,samples,sourceData});
            }
            finally{handler?.Dispose();if(container)UnityEngine.Object.DestroyImmediate(container);if(controller)UnityEngine.Object.DestroyImmediate(controller);Cursor.lockState=cursor;Cursor.visible=visible;}
        }
        File.WriteAllText(Folder+"/authored.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return new{authored=report.Count};
    }
}
