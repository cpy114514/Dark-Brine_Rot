using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor.SceneManagement;

public static class BuildSecondIslandMotions
{
    const string Folder="Assets/Resources/SecondIslandMotion";
    static readonly Dictionary<HumanBodyBones,string> Map=new Dictionary<HumanBodyBones,string>{
        {HumanBodyBones.Hips,"pelvis"},{HumanBodyBones.Spine,"spine_01"},{HumanBodyBones.Chest,"spine_02"},{HumanBodyBones.UpperChest,"spine_03"},{HumanBodyBones.Neck,"neck_01"},{HumanBodyBones.Head,"head"},
        {HumanBodyBones.LeftShoulder,"clavicle_l"},{HumanBodyBones.RightShoulder,"clavicle_r"},
        {HumanBodyBones.LeftUpperArm,"upperarm_l"},{HumanBodyBones.LeftLowerArm,"lowerarm_l"},{HumanBodyBones.LeftHand,"hand_l"},
        {HumanBodyBones.RightUpperArm,"upperarm_r"},{HumanBodyBones.RightLowerArm,"lowerarm_r"},{HumanBodyBones.RightHand,"hand_r"},
        {HumanBodyBones.LeftUpperLeg,"thigh_l"},{HumanBodyBones.LeftLowerLeg,"calf_l"},{HumanBodyBones.LeftFoot,"foot_l"},{HumanBodyBones.LeftToes,"ball_l"},
        {HumanBodyBones.RightUpperLeg,"thigh_r"},{HumanBodyBones.RightLowerLeg,"calf_r"},{HumanBodyBones.RightFoot,"foot_r"},{HumanBodyBones.RightToes,"ball_r"}
    };
    static Avatar HumanAvatar(GameObject model)
    {
        var bones=model.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
        var mapping=new Dictionary<HumanBodyBones,string>(Map);
        foreach(var side in new[]{"Left","Right"})foreach(var finger in new[]{"Thumb","Index","Middle","Ring","Little"})for(int joint=1;joint<=3;joint++)
        {
            string suffix=joint==1?"Proximal":joint==2?"Intermediate":"Distal";
            string name=(finger=="Little"?"pinky":finger.ToLowerInvariant())+"_0"+joint+"_"+(side=="Left"?"l":"r");
            if(Enum.TryParse(side+finger+suffix,out HumanBodyBones key)&&bones.ContainsKey(name))mapping[key]=name;
        }
        var human=mapping.Where(p=>bones.ContainsKey(p.Value)).Select(p=>new HumanBone{humanName=HumanTrait.BoneName[(int)p.Key],boneName=p.Value,limit=new HumanLimit{useDefaultValues=true}}).ToArray();
        var used=new HashSet<Transform>();
        foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>!s.name.ToLowerInvariant().Contains("weapon")&&!s.name.ToLowerInvariant().Contains("dingpa")))
            foreach(var bone in skin.bones)for(var current=bone;current&&current.IsChildOf(model.transform);current=current.parent)used.Add(current);
        used.Add(model.transform);
        var skeleton=model.GetComponentsInChildren<Transform>(true).Where(used.Contains).GroupBy(t=>t.name).Select(g=>g.First()).Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
        var description=new HumanDescription{human=human,skeleton=skeleton,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.02f,legStretch=.02f,feetSpacing=0,hasTranslationDoF=false};
        var avatar=AvatarBuilder.BuildHumanAvatar(model,description);
        if(!avatar||!avatar.isValid||!avatar.isHuman)throw new Exception("Humanoid mapping failed: "+model.name);
        return avatar;
    }
    public static object TestAvatars()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var reports=new List<object>();
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(bool huang in new[]{true,false})
            {
                string path=huang?"Assets/Resources/SecondIslandHuangMei/Huang Mei Boss.prefab":"Assets/Resources/SecondIslandZhuBaJie/ZhuBaJie Boss.prefab";
                var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
                try
                {
                    GameObject model=huang?actor.GetComponent<HuangMeiRig>().skin.transform.parent.gameObject:actor.GetComponent<ZhuBaJieBoss>().humanForm.transform.Find("Human Model").gameObject;
                    if(huang)model=actor.transform.Find("Huang Mei Weighted Model").gameObject;
                    var avatar=HumanAvatar(model);avatar.name=huang?"HuangMeiRetargetAvatar":"ZhuBaJieRetargetAvatar";
                    string assetPath=Folder+"/"+avatar.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
                    if(existing){EditorUtility.CopySerialized(avatar,existing);UnityEngine.Object.DestroyImmediate(avatar);avatar=existing;}else AssetDatabase.CreateAsset(avatar,assetPath);
                    reports.Add(new{model=model.name,avatar.isHuman,avatar.isValid,avatar.humanDescription.human.Length});
                }
                finally{UnityEngine.Object.DestroyImmediate(actor);}
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        AssetDatabase.SaveAssets();return reports;
    }

    struct Beat
    {
        public float start,end,from,to;public AnimationClip clip;
        public Beat(float start,float end,AnimationClip clip,float from=0,float to=1){this.start=start;this.end=end;this.clip=clip;this.from=from;this.to=to;}
    }
    static AnimationClip[] Sources()=>new[]{"Assets/ThirdParty/SecondIslandMotion/UAL2_Standard.fbx","Assets/ThirdParty/CapriPolishKayKit/Rig_Medium_CombatMelee.fbx","Assets/ThirdParty/CapriPolishKayKit/Rig_Medium_General.fbx"}
        .SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();

    public static object Build()=>BuildActor(null);
    public static object BuildHuang()=>BuildActor(true);
    public static object BuildHuangIdle()=>BuildActor(true,"Idle");
    public static object BuildBajie()=>BuildActor(false);
    static object BuildActor(bool? selected,string onlyKey=null)
    {
        var sources=Sources();AnimationClip Find(string name)=>sources.First(c=>c.name==name||c.name=="Armature|"+name);
        var idle=Find("Melee_2H_Idle");var a=Find("Sword_Regular_A");var b=Find("Sword_Regular_B");var rec=Find("Sword_Regular_B_Rec");
        var chop=Find("Melee_2H_Attack_Chop");var cast=Find("OverhandThrow");var raise=Find("Idle_Rail_Call");
        var scene=EditorSceneManager.NewPreviewScene();var report=new List<object>();
        try
        {
            foreach(bool huang in new[]{true,false})
            {
                if(selected.HasValue&&selected.Value!=huang)continue;
                string prefix=huang?"Huang":"Bajie";
                var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(huang?"Assets/Resources/SecondIslandHuangMei/Huang Mei Boss.prefab":"Assets/Resources/SecondIslandZhuBaJie/ZhuBaJie Boss.prefab"),scene);
                try
                {
                    var model=huang?actor.transform.Find("Huang Mei Weighted Model").gameObject:actor.GetComponent<ZhuBaJieBoss>().humanForm.transform.Find("Human Model").gameObject;
                    model.SetActive(true);if(model.transform.parent!=actor.transform)model.transform.parent.gameObject.SetActive(true);
                    var animator=model.GetComponent<Animator>();if(!animator)animator=model.AddComponent<Animator>();
                    animator.avatar=AssetDatabase.LoadAssetAtPath<Avatar>(Folder+"/"+(huang?"HuangMei":"ZhuBaJie")+"RetargetAvatar.asset");
                    animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    void Make(string key,float length,Beat[] beats,bool loop=false)
                    {
                        if(onlyKey!=null&&key!=onlyKey)return;
                        var clip=Bake(model,animator,prefix+key,length,beats,idle,loop);
                        string path=Folder+"/"+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                        if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,path);
                        report.Add(new{name=prefix+key,length,loop,source=beats.Select(v=>v.clip.name).Distinct().ToArray()});
                    }
                    Make("Idle",idle.length,new[]{new Beat(0,idle.length,idle)},true);
                    var walk=Find("Walk_Carry_Loop");Make("Walk",walk.length,new[]{new Beat(0,walk.length,walk)},true);
                    var die=Find("Death_A");Make("Death",die.length,new[]{new Beat(0,die.length,die)});
                    if(!huang)
                    {
                        Make("Rake",2.93f,new[]{new Beat(0,.78f,a,0,.22f),new Beat(.78f,1.23f,a,.22f,1),new Beat(1.23f,1.49f,b,0,.20f),new Beat(1.49f,1.97f,b,.20f,1),new Beat(1.97f,2.93f,rec)});
                        var dodge=Find("Shield_Dash");Make("Dodge",.48f,new[]{new Beat(0,.48f,dodge,.14f,.73f)});
                        Make("Rise",.8f,new[]{new Beat(0,.8f,Find("Spawn_Ground"),.22f,1)});
                    }
                    else
                    {
                        Make("Sweep",3.17f,new[]{new Beat(0,.83f,a,0,.18f),new Beat(.83f,1.45f,a,.18f,1),new Beat(1.45f,1.75f,b,0,.17f),new Beat(1.75f,2.25f,b,.17f,1),new Beat(2.25f,3.17f,rec)});
                        Make("Slam",3.33f,new[]{new Beat(0,1.22f,chop,0,.22f),new Beat(1.22f,1.85f,chop,.22f,.82f),new Beat(1.85f,3.33f,chop,.82f,1)});
                        Make("Palm",3.03f,new[]{new Beat(0,.92f,cast,0,.24f),new Beat(.92f,1.60f,cast,.24f,.82f),new Beat(1.60f,3.03f,cast,.82f,1)});
                        Make("Invocation",.86f,new[]{new Beat(0,.86f,raise,.08f,.47f)});
                        Make("Mandala",7.55f,new[]{new Beat(0,1.72f,raise,.10f,.43f),new Beat(1.72f,2.69f,chop,.08f,.72f),new Beat(2.69f,3.97f,chop,.08f,.72f),new Beat(3.97f,5.38f,chop,.08f,.72f),new Beat(5.38f,7.55f,chop,.72f,1)});
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(actor);}
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        AssetDatabase.SaveAssets();File.WriteAllText(".codex/island2-upgrade/motions-"+(selected.HasValue?(selected.Value?"huang":"bajie"):"all")+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }

    static AnimationClip Bake(GameObject model,Animator animator,string name,float length,Beat[] beats,AnimationClip idle,bool loop)
    {
        var root=model.transform;
        var hierarchy=new HashSet<Transform>();
        foreach(var bone in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(s=>s.bones))
            for(var t=bone;t&&t!=root&&t.IsChildOf(root);t=t.parent)hierarchy.Add(t);
        var transforms=hierarchy.ToArray();
        var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();
        var scales=transforms.Select(t=>t.localScale).ToArray();
        var rigBones=model.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
        var actor=root.parent;
        var sleeveBones=transforms.Where(t=>t.name.StartsWith("Xiukou_")||t.name.StartsWith("Yixiu_")||t.name.StartsWith("Shunzi_")).ToArray();
        var sleeveAnchors=sleeveBones.Select(t=>{var p=t.parent;while(p&&p!=root&&!Map.Values.Contains(p.name))p=p.parent;return p;}).ToArray();
        var sleeveOffsets=sleeveBones.Select((t,i)=>actor.InverseTransformVector(t.position-sleeveAnchors[i].position)).ToArray();
        var sleeveRotations=sleeveBones.Select(t=>Quaternion.Inverse(actor.rotation)*t.rotation).ToArray();
        var humanoidNames=animator.isHuman?new HashSet<string>(animator.avatar.humanDescription.human.Select(h=>h.boneName)):new HashSet<string>(transforms.Select(t=>t.name));
        var modelPosition=root.localPosition;var modelRotation=root.localRotation;var modelScale=root.localScale;
        var skeletonRoot=rigBones["root"];var skeletonPosition=skeletonRoot.localPosition;var skeletonRotation=skeletonRoot.localRotation;
        float restFoot=Mathf.Min(rigBones["foot_l"].position.y,rigBones["foot_r"].position.y);
        var curves=new List<Keyframe>[transforms.Length,10];for(int i=0;i<transforms.Length;i++)for(int j=0;j<10;j++)curves[i,j]=new List<Keyframe>();
        var graph=PlayableGraph.Create("Bake "+name);graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var mixer=AnimationMixerPlayable.Create(graph,3);var output=AnimationPlayableOutput.Create(graph,"Humanoid retarget",animator);output.SetSourcePlayable(mixer);
        var library=beats.Select(v=>v.clip).Append(idle).Distinct().ToArray();
        var cleaned=library.Select(CleanSource).ToArray();
        var players=new AnimationClipPlayable[cleaned.Length,3];
        for(int i=0;i<cleaned.Length;i++)for(int slot=0;slot<3;slot++){players[i,slot]=AnimationClipPlayable.Create(graph,cleaned[i]);players[i,slot].SetApplyFootIK(false);}
        AnimationClipPlayable Player(AnimationClip c,int slot)=>players[Array.IndexOf(library,c),slot];
        var ready=Player(idle,2);graph.Connect(ready,0,mixer,2);graph.Play();
        try
        {
            for(int frame=0;frame<=Mathf.RoundToInt(length*60);frame++)
            {
                float time=Mathf.Min(length,frame/60f);int index=0;while(index<beats.Length-1&&time>=beats[index].end)index++;
                var beat=beats[index];var current=Player(beat.clip,0);var previous=Player(index>0?beats[index-1].clip:idle,1);
                for(int input=0;input<2;input++)if(mixer.GetInput(input).IsValid())graph.Disconnect(mixer,input);
                graph.Connect(current,0,mixer,0);graph.Connect(previous,0,mixer,1);
                current.SetTime(Mathf.Lerp(beat.from,beat.to,Mathf.InverseLerp(beat.start,beat.end,time))*beat.clip.length);
                previous.SetTime(index>0?beats[index-1].to*beats[index-1].clip.length:0);
                ready.SetTime(0);
                float correction=index>0?1-Mathf.SmoothStep(0,1,(time-beat.start)/.14f):0;
                float guard=loop||name.EndsWith("Death")?1:Mathf.Min(Mathf.SmoothStep(0,1,time/.16f),Mathf.SmoothStep(0,1,(length-time)/.25f));
                mixer.SetInputWeight(0,(1-correction)*guard);mixer.SetInputWeight(1,correction*guard);mixer.SetInputWeight(2,1-guard);
                graph.Evaluate(0);
                root.SetLocalPositionAndRotation(modelPosition,modelRotation);root.localScale=modelScale;
                // These rigs carry cloth and auxiliary twist chains. Their local
                // bind transforms must follow the animated limbs, rather than
                // inheriting the source avatar's incompatible twist corrections.
                for(int i=0;i<transforms.Length;i++)if(!humanoidNames.Contains(transforms[i].name))
                {transforms[i].SetLocalPositionAndRotation(positions[i],rotations[i]);transforms[i].localScale=scales[i];}
                skeletonRoot.SetLocalPositionAndRotation(skeletonPosition,skeletonRotation);
                if(!name.EndsWith("Death"))
                {
                    float feet=Mathf.Min(rigBones["foot_l"].position.y,rigBones["foot_r"].position.y);
                    rigBones["pelvis"].position+=Vector3.up*(restFoot-feet);
                }
                if(name.StartsWith("Huang"))
                {
                    for(int i=0;i<transforms.Length;i++)if(transforms[i].name=="upperarm_l"||transforms[i].name=="upperarm_r"||transforms[i].name=="lowerarm_l"||transforms[i].name=="lowerarm_r")transforms[i].localRotation=rotations[i];
                    ConstrainStaff(rigBones,actor,name.Substring(5),time,length,positions,rotations,transforms);
                    // Sleeve helper chains carry the hanging fabric. Keep their
                    // gravity direction independent of the raised forearm, while
                    // following its attachment point through the authored action.
                    for(int i=0;i<sleeveBones.Length;i++)
                    {
                        var lag=Quaternion.AngleAxis(Mathf.Sin((loop?time/length*Mathf.PI*2:time*3.2f)+i*.4f)*2.5f,actor.forward);
                        sleeveBones[i].SetPositionAndRotation(sleeveAnchors[i].position+lag*actor.TransformVector(sleeveOffsets[i]),lag*actor.rotation*sleeveRotations[i]);
                    }
                }
                if(name=="BoarWalk")PoseBoarWalk(rigBones,model.GetComponentInParent<ZhuBaJieBoss>().transform,time,length,positions,rotations,transforms);
                if(name=="BoarCharge"&&time>.9f&&time<1.9f)
                {
                    float weight=Mathf.Min(Mathf.SmoothStep(0,1,(time-.9f)/.12f),Mathf.SmoothStep(0,1,(1.9f-time)/.15f));
                    PoseBoarWalk(rigBones,model.GetComponentInParent<ZhuBaJieBoss>().transform,(time-.9f)*2.4f,1.2f,positions,rotations,transforms,weight,2.4f);
                }
                if(name=="BoarIdle")rigBones["spine_02"].localRotation*=Quaternion.Euler(0,0,Mathf.Sin(time/length*Mathf.PI*2)*.65f);
                for(int i=0;i<transforms.Length;i++)
                {
                    var q=transforms[i].localRotation;
                    if(!float.IsFinite(q.x)||!float.IsFinite(q.w))throw new Exception("Invalid retarget pose "+name);
                    var p=transforms[i].localPosition;
                    // Keep the controller in charge of planar travel.
                    curves[i,0].Add(new Keyframe(time,q.x));curves[i,1].Add(new Keyframe(time,q.y));curves[i,2].Add(new Keyframe(time,q.z));curves[i,3].Add(new Keyframe(time,q.w));
                    curves[i,4].Add(new Keyframe(time,p.x));curves[i,5].Add(new Keyframe(time,p.y));curves[i,6].Add(new Keyframe(time,p.z));
                    var scale=transforms[i].localScale;for(int axis=0;axis<3;axis++)curves[i,7+axis].Add(new Keyframe(time,scale[axis]));
                }
            }
        }
        finally
        {
            graph.Destroy();foreach(var source in cleaned)UnityEngine.Object.DestroyImmediate(source);
            for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
        }
        var clip=new AnimationClip{name=name,frameRate=60,legacy=true,wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever};
        for(int i=0;i<transforms.Length;i++)for(int j=0;j<10;j++)
        {
            var values=curves[i,j];bool constant=values.All(k=>Mathf.Abs(k.value-values[0].value)<.00001f);
            var keys=constant?new[]{values[0],values[values.Count-1]}:values.ToArray();
            if(!constant)for(int k=0;k<keys.Length;k++)
            {
                float left=k>0?(keys[k].value-keys[k-1].value)/Mathf.Max(.00001f,keys[k].time-keys[k-1].time):0;
                float right=k<keys.Length-1?(keys[k+1].value-keys[k].value)/Mathf.Max(.00001f,keys[k+1].time-keys[k].time):0;
                float slope=left*right<=0?0:Mathf.Sign(left)*Mathf.Min(Mathf.Abs((left+right)*.5f),3*Mathf.Min(Mathf.Abs(left),Mathf.Abs(right)));
                keys[k].inTangent=keys[k].outTangent=slope;
            }
            var curve=new AnimationCurve(keys);
            string property=j<4?"m_LocalRotation."+"xyzw"[j]:j<7?"m_LocalPosition."+"xyz"[j-4]:"m_LocalScale."+"xyz"[j-7];
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(transforms[i],root),typeof(Transform),property),curve);
        }
        clip.EnsureQuaternionContinuity();return clip;
    }
    static AnimationClip CleanSource(AnimationClip original)
    {
        var clip=UnityEngine.Object.Instantiate(original);
        foreach(var b in AnimationUtility.GetCurveBindings(clip))
        {
            if(b.type!=typeof(Animator))continue;
            float? constant=b.propertyName=="RootT.x"||b.propertyName=="RootT.z"||b.propertyName=="RootQ.x"||b.propertyName=="RootQ.y"||b.propertyName=="RootQ.z"?0:b.propertyName=="RootQ.w"?1:(float?)null;
            if(constant.HasValue)AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,clip.length,constant.Value));
        }
        AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());return clip;
    }

    struct HandPose
    {
        public float time;public Vector3 right,left,shaft;
        public HandPose(float time,Vector3 right,Vector3 left,Vector3 shaft){this.time=time;this.right=right;this.left=left;this.shaft=shaft;}
    }
    static readonly Vector3 ReadyRight=new Vector3(1.86f,4.48f,.79f),ReadyLeft=new Vector3(-1.55f,4.58f,.55f);
    static void ConstrainStaff(Dictionary<string,Transform> bones,Transform actor,string key,float time,float length,Vector3[] restPosition,Quaternion[] restRotation,Transform[] recorded)
    {
        var ready=new HandPose(0,ReadyRight,ReadyLeft,Vector3.up);
        HandPose[] keys;
        if(key=="Sweep")keys=new[]{ready,new HandPose(.83f,new Vector3(2.68f,5.3f,-.46f),new Vector3(-1.42f,5.42f,1.25f),new Vector3(.93f,.17f,-.32f)),new HandPose(1.3184f,new Vector3(.45f,4.75f,2.3f),new Vector3(-1.35f,4.9f,1.1f),new Vector3(-.94f,.15f,.22f)),new HandPose(1.76f,new Vector3(.68f,5.46f,2.04f),new Vector3(-1.56f,5.34f,.72f),new Vector3(-.93f,.3f,.05f)),new HandPose(2.0732f,new Vector3(2.81f,4.73f,1.44f),new Vector3(-1.28f,4.79f,1.13f),new Vector3(.86f,.14f,.48f)),new HandPose(length,ReadyRight,ReadyLeft,Vector3.up)};
        else if(key=="Slam")keys=new[]{ready,new HandPose(1.22f,new Vector3(1.55f,7.15f,.8f),new Vector3(-1.05f,6.4f,1.5f),new Vector3(.08f,.97f,.14f)),new HandPose(1.6825f,new Vector3(1.53f,4.14f,1.58f),new Vector3(-.35f,4.6f,2.0f),Vector3.up),new HandPose(2.1f,new Vector3(1.53f,4.14f,1.58f),new Vector3(-1.1f,4.9f,1.6f),Vector3.up),new HandPose(length,ReadyRight,ReadyLeft,Vector3.up)};
        else if(key=="Palm")keys=new[]{ready,new HandPose(.92f,ReadyRight,new Vector3(-1.1f,5.8f,.4f),Vector3.up),new HandPose(1.37f,ReadyRight,new Vector3(-.5f,4.9f,3.1f),Vector3.up),new HandPose(1.75f,ReadyRight,new Vector3(-.5f,4.9f,3.1f),Vector3.up),new HandPose(length,ReadyRight,ReadyLeft,Vector3.up)};
        else if(key=="Invocation")keys=new[]{ready,new HandPose(length*.7f,new Vector3(1.9f,7.5f,.67f),new Vector3(-1.46f,6.92f,1.81f),Vector3.up),new HandPose(length,new Vector3(1.9f,7.5f,.67f),new Vector3(-1.46f,6.92f,1.81f),Vector3.up)};
        else if(key=="Mandala")
        {
            var list=new List<HandPose>{ready,new HandPose(1.72f,new Vector3(1.9f,7.5f,.67f),new Vector3(-1.46f,6.92f,1.81f),Vector3.up)};
            foreach(float hit in new[]{2.69f,3.97f,5.38f})
            {
                list.Add(new HandPose(hit-.25f,new Vector3(1.9f,6.6f,.67f),new Vector3(-1.46f,6.1f,1.81f),Vector3.up));
                list.Add(new HandPose(hit,new Vector3(1.53f,4.14f,1.58f),new Vector3(-1.31f,5.38f,2.2f),Vector3.up));
                list.Add(new HandPose(hit+.25f,new Vector3(1.7f,4.9f,1.3f),new Vector3(-1.35f,5.5f,2),Vector3.up));
            }
            list.Add(new HandPose(length,ReadyRight,ReadyLeft,Vector3.up));keys=list.OrderBy(k=>k.time).ToArray();
        }
        else if(key=="Death")return;
        else
        {
            float step=key=="Walk"?Mathf.Sin(time*Mathf.PI)*.14f:Mathf.Sin(time*Mathf.PI*2/length)*.035f;
            keys=new[]{new HandPose(0,ReadyRight+new Vector3(0,step*.35f,-step),ReadyLeft+new Vector3(0,step*.35f,step),Vector3.up)};
        }
        int index=0;while(index<keys.Length-2&&time>keys[index+1].time)index++;
        var pose=keys[index];
        if(keys.Length>1)
        {
            var next=keys[index+1];float f=Mathf.SmoothStep(0,1,Mathf.InverseLerp(pose.time,next.time,time));
            pose.right=Vector3.Lerp(pose.right,next.right,f);pose.left=Vector3.Lerp(pose.left,next.left,f);pose.shaft=Vector3.Slerp(pose.shaft,next.shaft,f);
        }
        SolveArm(bones["upperarm_r"],bones["lowerarm_r"],bones["hand_r"],actor.TransformPoint(pose.right),actor.TransformPoint(new Vector3(3.8f,4.8f,.05f)));
        SolveArm(bones["upperarm_l"],bones["lowerarm_l"],bones["hand_l"],actor.TransformPoint(pose.left),actor.TransformPoint(new Vector3(-3.7f,4.7f,.25f)));
        var hand=bones["hand_r"];var shaft=actor.TransformDirection(pose.shaft).normalized;
        var wrist=Vector3.ProjectOnPlane(bones["lowerarm_r"].position-hand.position,shaft).normalized;
        if(wrist.sqrMagnitude<.01f)wrist=actor.right;
        hand.rotation=Quaternion.LookRotation(shaft,Vector3.Cross(shaft,wrist));
        if(key=="Palm"||key=="Invocation"||key=="Mandala")
        {
            var palm=bones["hand_l"];var back=bones["lowerarm_l"].position-palm.position;
            var axis=Vector3.ProjectOnPlane(back,actor.forward).normalized;
            if(axis.sqrMagnitude>.01f)palm.rotation=Quaternion.LookRotation(Vector3.Cross(axis,actor.forward),actor.forward);
            for(int i=0;i<recorded.Length;i++)if(recorded[i].name.EndsWith("_l")&&new[]{"index_","middle_","ring_","pinky_","thumb_"}.Any(recorded[i].name.StartsWith))recorded[i].localRotation=restRotation[i];
        }
    }
    static void SolveArm(Transform upper,Transform lower,Transform hand,Vector3 target,Vector3 pole)
    {
        Vector3 start=upper.position,a=lower.position-start,b=hand.position-lower.position,delta=target-start;
        float lenA=a.magnitude,lenB=b.magnitude;if(delta.sqrMagnitude<.00001f)return;
        var direction=delta.normalized;var bend=Vector3.ProjectOnPlane(pole-start,direction).normalized;
        if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(a,direction).normalized;
        float reach=Mathf.Clamp(delta.magnitude,Mathf.Abs(lenA-lenB)+.005f,lenA+lenB-.005f);
        float along=(lenA*lenA-lenB*lenB+reach*reach)/(2*reach);
        Vector3 elbow=start+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,lenA*lenA-along*along));
        upper.rotation=Quaternion.FromToRotation(a,elbow-start)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,start+direction*reach-lower.position)*lower.rotation;
    }

    public static object BuildBoar()
    {
        var scene=EditorSceneManager.NewPreviewScene();var results=new List<string>();
        var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/SecondIslandZhuBaJie/ZhuBaJie Boss.prefab"),scene);
        try
        {
            var animator=actor.GetComponent<ZhuBaJieBoss>().boarForm.GetComponentInChildren<Animator>();
            var controller=animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            var idle=controller.layers[0].stateMachine.states.First(s=>s.state.name=="Idle").state.motion as AnimationClip;
            var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/ZhuBaJiePigForm/Models/ZhuBaJie_PigForm.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            var charge=source.First(c=>c.name.EndsWith("_atk_01"));var tail=source.First(c=>c.name.EndsWith("_atk_02"));var death=source.First(c=>c.name.EndsWith("_dead"));
            if(!idle)idle=charge;
            void Make(string name,float length,Beat[] beats,bool loop=false)
            {
                var clip=Bake(animator.gameObject,animator,name,length,beats,idle,loop);var path=Folder+"/"+name+".anim";
                var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,path);results.Add(name);
            }
            Make("BoarIdle",1.8f,new[]{new Beat(0,1.8f,idle,0,0)},true);
            Make("BoarWalk",1.2f,new[]{new Beat(0,1.2f,idle,0,0)},true);
            Make("BoarCharge",3.15f,new[]{new Beat(0,.9f,charge,0,.17f),new Beat(.9f,2.15f,charge,.17f,.75f),new Beat(2.15f,3.15f,charge,.75f,1)});
            Make("BoarTail",2.5f,new[]{new Beat(0,.85f,tail,0,.16f),new Beat(.85f,1.65f,tail,.16f,.66f),new Beat(1.65f,2.5f,tail,.66f,1)});
            Make("BoarDeath",2.6f,new[]{new Beat(0,2.6f,death,0,.35f)});
            AssetDatabase.SaveAssets();return results;
        }
        finally{UnityEngine.Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void PoseBoarWalk(Dictionary<string,Transform> bones,Transform actor,float time,float length,Vector3[] restPosition,Quaternion[] restRotation,Transform[] recorded,float weight=1,float stride=1.8f)
    {
        var legNames=new[]{new[]{"upperarm_l","lowerarm_l","hand_l"},new[]{"upperarm_r","lowerarm_r","hand_r"},new[]{"thigh_l","calf_l","foot_l"},new[]{"thigh_r","calf_r","foot_r"}};
        for(int i=0;i<4;i++)
        {
            if(!bones.TryGetValue(legNames[i][2],out var foot))continue;
            var initial=foot.position;var facing=foot.rotation;
            float phase=Mathf.Repeat(time/length+(i==0||i==3?0:.5f),1);
            float travel=(phase<.62f?Mathf.Lerp(stride,-stride,phase/.62f):Mathf.Lerp(-stride,stride,Mathf.InverseLerp(.62f,1,phase)))*weight;
            float lift=phase<.62f?0:Mathf.Sin(Mathf.InverseLerp(.62f,1,phase)*Mathf.PI)*.65f;
            var target=initial+actor.forward*travel+actor.up*lift*weight;
            var pole=initial+actor.forward*(i<2?-2:2)+actor.up*2;
            SolveArm(bones[legNames[i][0]],bones[legNames[i][1]],foot,target,pole);foot.rotation=facing;
        }
    }
}
