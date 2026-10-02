using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// CMU trial 125_06, BVH conversion by Bruce Hahne. Re-run through Unity Pipeline.
public static class ImportSahurFreestyle
{
    const string ClipPath = "Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurFastFreestyle.anim";
    const string PrefabPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    const string ControllerPath = "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller";
    class Joint { public Transform transform; public Vector3 offset; public string[] channels; public int first; }
    class Bvh
    {
        public List<Joint> joints = new List<Joint>();
        public float[][] frames;
        string[] tokens; int cursor, channel;
        float Float() => float.Parse(tokens[cursor++], CultureInfo.InvariantCulture);
        public Bvh(string path, Transform root)
        {
            string[] sections = File.ReadAllText(path).Split(new[]{"MOTION"}, StringSplitOptions.None);
            tokens = Regex.Matches(sections[0], @"\S+").Cast<Match>().Select(m=>m.Value).ToArray();
            cursor=3; ReadJoint(root, tokens[2]);
            frames=sections[1].Split('\n').Skip(3).Where(s=>!string.IsNullOrWhiteSpace(s))
                .Select(s=>s.Split((char[])null,StringSplitOptions.RemoveEmptyEntries)
                .Select(v=>float.Parse(v,CultureInfo.InvariantCulture)).ToArray()).ToArray();
        }
        void ReadJoint(Transform parent, string name)
        {
            cursor++; // {
            var t = new GameObject(name).transform; t.SetParent(parent,false);
            cursor++; // OFFSET
            var offset=new Vector3(-Float(),Float(),Float())*.056444f;
            t.localPosition=offset;
            var j=new Joint{transform=t,offset=offset,first=channel}; joints.Add(j);
            if(tokens[cursor]=="CHANNELS")
            {
                cursor++; int n=int.Parse(tokens[cursor++]);
                j.channels=tokens.Skip(cursor).Take(n).ToArray();cursor+=n;channel+=n;
            }
            else j.channels=Array.Empty<string>();
            while(tokens[cursor]!="}")
            {
                string kind=tokens[cursor++]; string child=tokens[cursor++];
                ReadJoint(t,kind=="End"?name+"End":child);
            }
            cursor++;
        }
        public void Sample(int frame)
        {
            float[] values=frames[frame];
            foreach(var j in joints)
            {
                var p=j.offset; Quaternion q=Quaternion.identity;
                for(int c=0;c<j.channels.Length;c++)
                {
                    string ch=j.channels[c]; float v=values[j.first+c];
                    if(ch.EndsWith("rotation"))
                    {
                        Vector3 axis=ch[0]=='X'?Vector3.right:ch[0]=='Y'?Vector3.up:Vector3.forward;
                        q=q*Quaternion.AngleAxis(v,axis);
                    }
                    else if(ch[0]=='X') p.x=-v*.056444f;
                    else if(ch[0]=='Y') p.y=v*.056444f;
                    else p.z=v*.056444f;
                }
                j.transform.localRotation=new Quaternion(q.x,-q.y,-q.z,q.w);
                // Keep the capture's root at the origin; swimming translation is controlled by gameplay.
                if(j.first==0) p=new Vector3(0,frames[0][1]*.056444f,0);
                j.transform.localPosition=p;
            }
        }
    }
    public static object Author()
    {
        var source=new GameObject("CMU freestyle retarget source"); Avatar avatar=null; HumanPoseHandler handler=null;
        try
        {
            var bvh=new Bvh("Tools/AnimationSources/CMU/125_06.bvh",source.transform);
            bvh.Sample(0); // The conversion contains a calibrated T pose in its first frame.
            string[,] map={
                {"Hips","Hips"},{"Spine","Spine"},{"Chest","Spine1"},{"Neck","Neck1"},{"Head","Head"},
                {"LeftUpperLeg","LeftUpLeg"},{"LeftLowerLeg","LeftLeg"},{"LeftFoot","LeftFoot"},{"LeftToes","LeftToeBase"},
                {"RightUpperLeg","RightUpLeg"},{"RightLowerLeg","RightLeg"},{"RightFoot","RightFoot"},{"RightToes","RightToeBase"},
                {"LeftShoulder","LeftShoulder"},{"LeftUpperArm","LeftArm"},{"LeftLowerArm","LeftForeArm"},{"LeftHand","LeftHand"},
                {"RightShoulder","RightShoulder"},{"RightUpperArm","RightArm"},{"RightLowerArm","RightForeArm"},{"RightHand","RightHand"}
            };
            var human=new List<HumanBone>();
            for(int i=0;i<map.GetLength(0);i++) human.Add(new HumanBone{humanName=map[i,0],boneName=map[i,1],limit=new HumanLimit{useDefaultValues=true}});
            var desc=new HumanDescription{
                human=human.ToArray(),skeleton=source.GetComponentsInChildren<Transform>().Select(t=>new SkeletonBone{
                    name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
                upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0
            };
            avatar=AvatarBuilder.BuildHumanAvatar(source,desc);
            if(!avatar.isValid || !avatar.isHuman) throw new Exception("CMU source avatar did not validate.");
            handler=new HumanPoseHandler(avatar,source.transform);
            // A steady complete stroke at 27.9–29.6167 seconds. Close the tiny capture seam.
            const int first=3348, period=206, count=43;
            const float duration=1.38f; // brisk freestyle cadence for sprint swimming
            var poses=new HumanPose[count];
            for(int k=0;k<count;k++)
            {
                int f=first+Mathf.RoundToInt(period*k/(float)(count-1)); bvh.Sample(f);
                handler.GetHumanPose(ref poses[k]);
            }
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if(!clip) {clip=new AnimationClip();AssetDatabase.CreateAsset(clip,ClipPath);}
            clip.ClearCurves();clip.name="SahurFastFreestyle";clip.frameRate=60;
            Action<string,float[]> write=(name,values)=>{
                var curve=new AnimationCurve();
                float delta=values[count-1]-values[0];
                for(int k=0;k<count;k++) curve.AddKey(new Keyframe(duration*k/(count-1),values[k]-delta*Mathf.SmoothStep(0,1,k/(float)(count-1))));
                for(int k=0;k<curve.length;k++){
                    AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
                }
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),name),curve);
            };
            for(int m=0;m<HumanTrait.MuscleCount;m++)
            {
                string name=HumanTrait.MuscleName[m];float[] values=poses.Select(p=>p.muscles[m]).ToArray();
                // The capture's knees rest on a support. Replace that support pose with
                // a small alternating flutter kick while retaining the captured upper body.
                if(name.Contains("Upper Leg") || name.Contains("Lower Leg") || name.Contains("Foot") || name.Contains("Toes"))
                    for(int k=0;k<count;k++){
                        float phase=k/(float)(count-1)*Mathf.PI*6+(name.StartsWith("Right")?Mathf.PI:0);
                        values[k]=name.EndsWith("Upper Leg Front-Back")?.17f*Mathf.Sin(phase):
                            name.EndsWith("Lower Leg Stretch")?.88f-.16f*(.5f+.5f*Mathf.Sin(phase)):
                            name.EndsWith("Foot Up-Down")?1f:0;
                    }
                write(name,values);
            }
            // Capture was performed horizontally on a support. Preserve its prone body pitch,
            // remove heading drift, and let the CharacterController supply all displacement.
            Quaternion reference=poses[0].bodyRotation;
            float heading=Quaternion.LookRotation(Vector3.ProjectOnPlane(reference*Vector3.up,Vector3.up),Vector3.up).eulerAngles.y;
            var orientations=poses.Select(p=>Quaternion.Euler(0,-heading,0)*p.bodyRotation).ToArray();
            for(int k=1;k<count;k++) if(Quaternion.Dot(orientations[k-1],orientations[k])<0){var q=orientations[k];orientations[k]=new Quaternion(-q.x,-q.y,-q.z,-q.w);}
            write("RootQ.x",orientations.Select(q=>q.x).ToArray());write("RootQ.y",orientations.Select(q=>q.y).ToArray());
            write("RootQ.z",orientations.Select(q=>q.z).ToArray());write("RootQ.w",orientations.Select(q=>q.w).ToArray());
            write("RootT.x",Enumerable.Repeat(0f,count).ToArray());write("RootT.y",Enumerable.Repeat(.92f,count).ToArray());write("RootT.z",Enumerable.Repeat(0f,count).ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime=true;settings.loopBlend=true;settings.startTime=0;settings.stopTime=duration;
            settings.keepOriginalOrientation=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;
            settings.loopBlendOrientation=true;settings.loopBlendPositionXZ=true;settings.loopBlendPositionY=true;
            AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
            return new {clip=ClipPath,clip.length,clip.humanMotion,sourceFrames=bvh.frames.Length,reference=reference.ToString(),heading};
        }
        finally {handler?.Dispose();if(avatar) UnityEngine.Object.DestroyImmediate(avatar);UnityEngine.Object.DestroyImmediate(source);}
    }
    public static object Install()
    {
        if(Application.isPlaying) throw new Exception("Install outside Play Mode.");
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if(!clip || !clip.humanMotion) throw new Exception("Author the retargeted freestyle first.");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var sm=controller.layers[0].stateMachine;
        var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Swim Fast");
        if(!state) state=sm.AddState("Swim Fast",new Vector3(650,320,0));
        state.motion=clip;state.speed=1;state.writeDefaultValues=true;
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try{
            var p=root.GetComponent<ThirdPersonPlayerController>();p.swimSpeed=7.5f;p.swimAcceleration=20f;p.fastSwimMultiplier=1.75f;
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        // The combat demo contains a standalone player rather than a prefab instance.
        const string demoPath="Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoCombatDemo.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(demoPath);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(demoPath,OpenSceneMode.Additive);
        try{
            foreach(var p in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ThirdPersonPlayerController>(true))){
                Undo.RecordObject(p,"Increase swimming speed");p.swimSpeed=7.5f;p.swimAcceleration=20f;p.fastSwimMultiplier=1.75f;
                EditorUtility.SetDirty(p);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        return new {normal=7.5f,fast=13.125f,acceleration=20f,state=state.name};
    }
}
