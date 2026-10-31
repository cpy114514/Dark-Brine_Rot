using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Mavis;

public static class GoosePolish034
{
    const string Folder="Assets/Game/Prefabs/Enemies/BullyGoose";
    const string Prefab=Folder+"/BullyGoose.prefab";
    const string Work=".codex/goose034";
    const string Revision=Folder+"/Animations/Polish034";
    static object V(Vector3 v)=>new{x=v.x,y=v.y,z=v.z};
    public static object Apply()
    {
        if(File.Exists(Work+"/applied.json"))throw new Exception("Already applied; inspect existing checkpoint");
        Directory.CreateDirectory(Work+"/before-assets");
        foreach(var path in Directory.GetFiles(Folder,"*",SearchOption.AllDirectories))
        {
            string to=Work+"/before-assets/"+path.Substring(Folder.Length+1);
            Directory.CreateDirectory(Path.GetDirectoryName(to));File.Copy(path,to,false);
        }
        Directory.CreateDirectory(Revision);AssetDatabase.Refresh();
        string controllerPath=Revision+"/BullyGoosePolish034.controller";
        if(!AssetDatabase.CopyAsset(Folder+"/BullyGoose.controller",controllerPath))throw new Exception("Controller copy failed");
        var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var a=root.GetComponentInChildren<Animator>();a.enabled=false;
            var ai=root.GetComponent<ChickenBroAI>();var cc=root.GetComponent<CharacterController>();
            const float factor=1.3f;
            a.transform.localScale*=factor;a.transform.localPosition*=factor;
            cc.height*=factor;cc.radius*=factor;cc.center*=factor;cc.stepOffset*=factor;cc.skinWidth*=factor;
            ai.meleeRange*=factor;ai.lungeMin*=factor;ai.lungeMax*=factor;
            ai.surroundDistance*=factor;ai.separationRadius*=factor;ai.peckReach*=factor;
            ai.attackRadius*=factor;ai.wingRange*=factor;ai.wingRadius*=factor;
            ai.matchLocomotionToMovement=true;
            var initialOverrides=PrefabUtility.GetPropertyModifications(a.gameObject);
            var originalPose=a.GetComponentsInChildren<Transform>(true).Select(t=>new{t,p=t.localPosition,q=t.localRotation,s=t.localScale}).ToArray();
            var sm=c.layers[0].stateMachine;
            var idle=sm.states.First(s=>s.state.name=="Idle").state;
            var walk=sm.states.First(s=>s.state.name=="Walk").state;
            var run=sm.states.First(s=>s.state.name=="Run").state;
            var clips=new[]{idle,walk,run}.Select(s=>(AnimationClip)s.motion).ToArray();
            var revised=new List<AnimationClip>();
            foreach(var original in clips)
            {
                var clip=UnityEngine.Object.Instantiate(original);clip.name=original.name.Replace("Armature|","")+"Polish034";
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=curve.keys;
                    if(keys.Length<3)continue;
                    // Periodic velocity at the cycle boundary; preserve every interior pose.
                    var first=keys[0];var last=keys[keys.Length-1];
                    float tangent=(first.outTangent+last.inTangent)*.5f;
                    if(float.IsNaN(tangent)||float.IsInfinity(tangent))continue;
                    last.value=first.value;first.inTangent=first.outTangent=tangent;last.inTangent=last.outTangent=tangent;
                    first.weightedMode=last.weightedMode=WeightedMode.None;
                    keys[0]=first;keys[keys.Length-1]=last;curve.keys=keys;
                    AnimationUtility.SetKeyLeftTangentMode(curve,0,AnimationUtility.TangentMode.Free);
                    AnimationUtility.SetKeyRightTangentMode(curve,0,AnimationUtility.TangentMode.Free);
                    AnimationUtility.SetKeyLeftTangentMode(curve,keys.Length-1,AnimationUtility.TangentMode.Free);
                    AnimationUtility.SetKeyRightTangentMode(curve,keys.Length-1,AnimationUtility.TangentMode.Free);
                    AnimationUtility.SetEditorCurve(clip,binding,curve);
                }
                clip.EnsureQuaternionContinuity();
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;
                AnimationUtility.SetAnimationClipSettings(clip,settings);
                AssetDatabase.CreateAsset(clip,Revision+"/"+clip.name+".anim");revised.Add(clip);
            }
            float StrideRate(AnimationClip clip,float speed)
            {
                var left=a.GetComponentsInChildren<Transform>().First(t=>t.name=="FootL");
                var right=a.GetComponentsInChildren<Transform>().First(t=>t.name=="FootR");
                float minL=999,maxL=-999,minR=999,maxR=-999;
                for(int i=0;i<64;i++){clip.SampleAnimation(a.gameObject,clip.length*i/64f);minL=Mathf.Min(minL,left.position.z);maxL=Mathf.Max(maxL,left.position.z);minR=Mathf.Min(minR,right.position.z);maxR=Mathf.Max(maxR,right.position.z);}
                // One complete left-right cycle advances twice one foot's stance excursion.
                float stride=2f*Mathf.Max(maxL-minL,maxR-minR);
                return Mathf.Clamp(speed*clip.length/Mathf.Max(.1f,stride),.6f,2f);
            }
            float walkRate=StrideRate(revised[1],ai.walkSpeed),runRate=StrideRate(revised[2],ai.runSpeed);
            foreach(var pose in originalPose){pose.t.localPosition=pose.p;pose.t.localRotation=pose.q;pose.t.localScale=pose.s;}
            var tree=new BlendTree{name="Goose continuous locomotion034",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(tree,c);
            tree.children=new[]{new ChildMotion{motion=revised[0],threshold=0,timeScale=1},new ChildMotion{motion=revised[1],threshold=ai.walkSpeed/ai.runSpeed,timeScale=walkRate},new ChildMotion{motion=revised[2],threshold=1,timeScale=runRate}};
            foreach(var t in sm.anyStateTransitions.Where(t=>t.destinationState==idle||t.destinationState==walk||t.destinationState==run).ToArray())sm.RemoveAnyStateTransition(t);
            sm.RemoveState(walk);sm.RemoveState(run);idle.name="Locomotion";idle.motion=tree;idle.speed=1;
            foreach(int motion in new[]{0,1,2}){var t=sm.AddAnyStateTransition(idle);t.AddCondition(AnimatorConditionMode.Equals,motion,"Motion");t.duration=.16f;t.hasFixedDuration=true;t.hasExitTime=false;t.canTransitionToSelf=false;}
            sm.defaultState=idle;
            a.runtimeAnimatorController=c;a.enabled=true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(a);
            PrefabUtility.RecordPrefabInstancePropertyModifications(a.transform);
            var sourceRoot=PrefabUtility.GetCorrespondingObjectFromSource(a.transform);
            bool BonePose(PropertyModification m)=>m.target is Transform&&m.target!=sourceRoot&&(m.propertyPath.StartsWith("m_LocalPosition")||m.propertyPath.StartsWith("m_LocalRotation")||m.propertyPath.StartsWith("m_LocalScale"));
            PrefabUtility.SetPropertyModifications(a.gameObject,PrefabUtility.GetPropertyModifications(a.gameObject).Where(m=>!BonePose(m)).Concat(initialOverrides.Where(BonePose)).ToArray());
            EditorUtility.SetDirty(c);PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
            var report=new{factor,height=cc.height,walkRate,runRate,walkCyclesPerSecond=walkRate/revised[1].length,runCyclesPerSecond=runRate/revised[2].length,ai.matchLocomotionToMovement,controller=controllerPath,originalModelUnchanged=true};
            File.WriteAllText(Work+"/applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static object RestoreSerializedPose()
    {
        const string temporary=Revision+"/OriginalPose034.prefab";
        if(File.Exists(temporary))throw new Exception("Temporary asset already exists");
        File.Copy(Work+"/before-assets/BullyGoose.prefab",temporary,false);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
        GameObject source=null,target=null;
        try
        {
            source=PrefabUtility.LoadPrefabContents(temporary);target=PrefabUtility.LoadPrefabContents(Prefab);
            var a=source.GetComponentInChildren<Animator>().transform;var b=target.GetComponentInChildren<Animator>().transform;
            int count=0;
            foreach(var t in a.GetComponentsInChildren<Transform>(true))
            {
                if(t==a)continue;
                var match=b.Find(AnimationUtility.CalculateTransformPath(t,a));if(!match)throw new Exception("Missing bone "+t.name);
                match.localPosition=t.localPosition;match.localRotation=t.localRotation;match.localScale=t.localScale;count++;
            }
            var sourceRoot=PrefabUtility.GetCorrespondingObjectFromSource(b);
            bool BonePose(PropertyModification m)=>m.target is Transform&&m.target!=sourceRoot&&(m.propertyPath.StartsWith("m_LocalPosition")||m.propertyPath.StartsWith("m_LocalRotation")||m.propertyPath.StartsWith("m_LocalScale"));
            var original=PrefabUtility.GetPropertyModifications(a.gameObject);
            var current=PrefabUtility.GetPropertyModifications(b.gameObject);
            PrefabUtility.SetPropertyModifications(b.gameObject,current.Where(m=>!BonePose(m)).Concat(original.Where(BonePose)).ToArray());
            PrefabUtility.SaveAsPrefabAsset(target,Prefab);AssetDatabase.SaveAssets();return new{restored=count};
        }
        finally{if(source)PrefabUtility.UnloadPrefabContents(source);if(target)PrefabUtility.UnloadPrefabContents(target);AssetDatabase.DeleteAsset(temporary);}
    }
    public static object Validate()
    {
        var tests=new List<object>();int passed=0,failed=0;
        void Test(bool ok,string name,object detail=null){tests.Add(new{ok,name,detail});if(ok)passed++;else failed++;}
        string Hash(string p){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p)));}
        foreach(var path in Directory.GetFiles(Work+"/before-assets","*",SearchOption.AllDirectories))
        {
            string rel=path.Substring((Work+"/before-assets/").Length).Replace('\\','/');
            if(rel=="BullyGoose.prefab")continue;
            Test(File.Exists(Folder+"/"+rel)&&Hash(path)==Hash(Folder+"/"+rel),"Original unchanged: "+rel);
        }
        var p=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);var ai=p.GetComponent<ChickenBroAI>();var cc=p.GetComponent<CharacterController>();
        Test(Mathf.Abs(cc.height-5.98f)<.001,"Goose enlarged by 30%",new{cc.height,cc.radius});
        Test(Mathf.Abs(cc.center.y-cc.height*.5f)<.0001f,"Capsule sole remains at root");
        Test(ai.matchLocomotionToMovement,"Locomotion uses actual movement");
        Test(ai.walkSpeed==2&&ai.runSpeed==5,"Movement speed preserved");
        Test(ai.strikeDamage==4.8f&&ai.lungeDamage==8&&ai.wingDamage==6,"All attack damage preserved");
        Test(Mathf.Abs(ai.strikeDuration-.5666667f)<.0001f&&ai.wingDuration==.5f,"Attack clocks preserved");
        Test(ai.strikeWindowStart==.38f&&ai.strikeWindowEnd==.58f&&ai.wingWindowStart==.42f&&ai.wingWindowEnd==.66f,"Hit windows preserved");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var a=root.GetComponentInChildren<Animator>();var c=(AnimatorController)a.runtimeAnimatorController;
            var sm=c.layers[0].stateMachine;var tree=(BlendTree)sm.defaultState.motion;
            Test(tree.children.Length==3&&tree.blendParameter=="Speed","One continuous idle/walk/run tree");
            Test(tree.children[2].timeScale<1.6f&&tree.children[1].timeScale<1.5f,"Stride rates remain below sewing-machine baseline",tree.children.Select(t=>new{t.timeScale,t.threshold}).ToArray());
            Test(!a.applyRootMotion,"AI retains movement ownership");
            foreach(string state in new[]{"Strike","Lunge","Flinch","Knockdown","Death","Alert","Wing"})
            {
                var s=sm.states.First(x=>x.state.name==state).state;
                Test(AssetDatabase.GetAssetPath(s.motion)==Folder+"/BullyGoose.fbx","Original action preserved: "+state);
            }
            foreach(float dt in new[]{1f/30,1f/60})
            {
                a.enabled=true;a.Rebind();a.SetInteger("Motion",0);a.SetFloat("Speed",0);a.Play("Locomotion",0,0);a.Update(0);
                float previous=0;bool continuous=true;float maximumJump=0;
                for(int i=0;i<240;i++)
                {
                    float speed=i<30?0:i<90?.4f:i<180?1:0;
                    a.SetInteger("Motion",speed==0?0:speed>.7f?2:1);a.SetFloat("Speed",speed);a.Update(dt);
                    var info=a.GetCurrentAnimatorStateInfo(0);
                    continuous&=info.IsName("Locomotion")&&!a.IsInTransition(0)&&info.normalizedTime>=previous-.0001f;
                    maximumJump=Mathf.Max(maximumJump,info.normalizedTime-previous);previous=info.normalizedTime;
                }
                Test(continuous,"Walk/run/idle phase continuity at "+Mathf.RoundToInt(1/dt)+"Hz",new{maximumJump,normalizedTime=previous});
                foreach(var item in new[]{(3,"Strike"),(4,"Lunge"),(5,"Flinch"),(6,"Knockdown"),(7,"Death"),(8,"Alert"),(9,"Wing")})
                {
                    a.SetInteger("Motion",item.Item1);for(int i=0;i<12;i++)a.Update(dt);
                    Test(a.GetCurrentAnimatorStateInfo(0).IsName(item.Item2),"Native "+item.Item2+" enters at "+Mathf.RoundToInt(1/dt)+"Hz");
                    a.SetInteger("Motion",0);a.SetFloat("Speed",0);for(int i=0;i<12;i++)a.Update(dt);
                    Test(a.GetCurrentAnimatorStateInfo(0).IsName("Locomotion")&&!a.IsInTransition(0),"Native recovery from "+item.Item2+" at "+Mathf.RoundToInt(1/dt)+"Hz");
                }
                Test(a.GetCurrentAnimatorClipInfo(0).Any(x=>x.clip.name=="IdlePolish034"&&x.weight>.999f),"Stopped animation has full idle weight at "+Mathf.RoundToInt(1/dt)+"Hz");
            }
            var instance=root.GetComponent<ChickenBroAI>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(ChickenBroAI).GetField("animator",flags).SetValue(instance,a);
            typeof(ChickenBroAI).GetField("animatorReady",flags).SetValue(instance,true);
            typeof(ChickenBroAI).GetField("locomotionSpeed",flags).SetValue(instance,0f);
            var mode=typeof(ChickenBroAI).GetField("<Mode>k__BackingField",flags);var present=typeof(ChickenBroAI).GetMethod("Present",flags);
            foreach(var mood in new[]{ChickenBroAI.Mood.Patrol,ChickenBroAI.Mood.Chase})
            {
                mode.SetValue(instance,mood);a.SetFloat("Speed",1);present.Invoke(instance,null);
                Test(a.GetInteger("Motion")==0&&a.GetFloat("Speed")==0,"Actual AI stops feet when blocked in "+mood);
            }
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var report=new{passed,failed,tests,playing=Application.isPlaying,timeScale=Time.timeScale,fullLiveCombatTested=false};
        File.WriteAllText(Work+"/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return new{passed,failed,report=Work+"/validation.json",playing=Application.isPlaying,timeScale=Time.timeScale};
    }
    public static object VerifyPoseAndReferences()
    {
        const string temporary=Revision+"/OriginalVerify034.prefab";
        if(File.Exists(temporary))throw new Exception("Preserve existing temporary asset");
        File.Copy(Work+"/before-assets/BullyGoose.prefab",temporary,false);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
        GameObject source=null,target=null;
        try
        {
            source=PrefabUtility.LoadPrefabContents(temporary);target=PrefabUtility.LoadPrefabContents(Prefab);
            var a=source.GetComponentInChildren<Animator>().transform;var b=target.GetComponentInChildren<Animator>().transform;
            var rows=new List<object>();bool all=true;
            foreach(var t in a.GetComponentsInChildren<Transform>(true))
            {
                if(t==a)continue;var match=b.Find(AnimationUtility.CalculateTransformPath(t,a));
                bool ok=match&&(t.localPosition-match.localPosition).sqrMagnitude<1e-16f&&Quaternion.Angle(t.localRotation,match.localRotation)<.001f&&(t.localScale-match.localScale).sqrMagnitude<1e-12f;
                all&=ok;rows.Add(new{t.name,ok});
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var arena=UnityEngine.Object.FindFirstObjectByType<CombatTestArena>();
            bool assigned=arena&&arena.goose==prefab;
            var report=new{passed=all&&assigned,originalBoneTransforms=all,bones=rows,arenaAssignedToUpdatedPrefab=assigned,playing=Application.isPlaying,timeScale=Time.timeScale,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
            File.WriteAllText(Work+"/final-state.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
        }
        finally{if(source)PrefabUtility.UnloadPrefabContents(source);if(target)PrefabUtility.UnloadPrefabContents(target);AssetDatabase.DeleteAsset(temporary);}
    }
    public static object Film()
    {
        const string temporary=Revision+"/OriginalFilm034.prefab";
        string dir=Work+"/film-qualified";if(Directory.Exists(dir)||File.Exists(temporary))throw new Exception("Preserve take");Directory.CreateDirectory(dir);
        File.Copy(Work+"/before-assets/BullyGoose.prefab",temporary,false);AssetDatabase.ImportAsset(temporary,ImportAssetOptions.ForceSynchronousImport);
        var scene=EditorSceneManager.NewPreviewScene();GameObject old=null,current=null,cameraGo=null,lightGo=null,floor=null;Material material=null;
        var rt=new RenderTexture(1280,720,24);rt.Create();var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        var graph=PlayableGraph.Create("Owned goose034 native manual graph");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var proxies=new List<GameObject>();var meshes=new List<Mesh>();
        try
        {
            old=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(temporary),scene);
            current=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);
            old.transform.position=new Vector3(3.7f,0,0);current.transform.position=new Vector3(-3.7f,0,0);
            var a=old.GetComponentInChildren<Animator>();var b=current.GetComponentInChildren<Animator>();a.cullingMode=b.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var skin in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<SkinnedMeshRenderer>()))skin.updateWhenOffscreen=true;
            a.Rebind();b.Rebind();
            var originalController=AnimatorControllerPlayable.Create(graph,a.runtimeAnimatorController);
            var revisedController=AnimatorControllerPlayable.Create(graph,b.runtimeAnimatorController);
            AnimationPlayableOutput.Create(graph,"original",a).SetSourcePlayable(originalController);
            AnimationPlayableOutput.Create(graph,"revised",b).SetSourcePlayable(revisedController);graph.Play();graph.Evaluate(0);
            var skins=new[]{old.GetComponentInChildren<SkinnedMeshRenderer>(),current.GetComponentInChildren<SkinnedMeshRenderer>()};
            foreach(var skin in skins)
            {
                var proxy=new GameObject("Owned actual baked skin",typeof(MeshFilter),typeof(MeshRenderer));SceneManager.MoveGameObjectToScene(proxy,scene);
                proxy.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);proxy.transform.localScale=skin.transform.lossyScale;
                var mesh=new Mesh();proxy.GetComponent<MeshFilter>().sharedMesh=mesh;proxy.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                skin.enabled=false;proxies.Add(proxy);meshes.Add(mesh);
            }
            cameraGo=new GameObject("Owned goose034 film camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);var cam=cameraGo.AddComponent<Camera>();cam.enabled=false;cam.scene=scene;
            cam.orthographic=true;cam.orthographicSize=4.2f;cam.nearClipPlane=.1f;cam.farClipPlane=100;
            cam.transform.position=new Vector3(0,4.2f,20);cam.transform.LookAt(new Vector3(0,2.9f,0));cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.16f,.20f);cam.targetTexture=rt;
            lightGo=new GameObject("Owned goose034 film light");SceneManager.MoveGameObjectToScene(lightGo,scene);var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;lightGo.transform.rotation=Quaternion.Euler(35,-30,0);
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(floor,scene);floor.transform.position=new Vector3(0,-.1f,0);floor.transform.localScale=new Vector3(25,.2f,12);
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.22f,.27f,.29f));floor.GetComponent<Renderer>().sharedMaterial=material;
            var rows=new List<object>();
            var footA=a.GetComponentsInChildren<Transform>().First(t=>t.name=="FootL");var footB=b.GetComponentsInChildren<Transform>().First(t=>t.name=="FootL");
            Vector3 firstA=footA.position,firstB=footB.position;float maxA=0,maxB=0;
            for(int i=0;i<180;i++)
            {
                int mode=i<20?0:i<70?1:i<130?2:i<160?1:0;float speed=mode==0?0:mode==1?.4f:1;
                originalController.SetInteger("Motion",mode);originalController.SetFloat("Speed",speed);revisedController.SetInteger("Motion",mode);revisedController.SetFloat("Speed",speed);
                graph.Evaluate(1f/30);
                for(int s=0;s<skins.Length;s++)skins[s].BakeMesh(meshes[s],true);
                maxA=Mathf.Max(maxA,(footA.position-firstA).magnitude);maxB=Mathf.Max(maxB,(footB.position-firstB).magnitude);
                rows.Add(new{frame=i,mode,oldFoot=V(footA.position),newFoot=V(footB.position),newPhase=revisedController.GetCurrentAnimatorStateInfo(0).normalizedTime});
                cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(dir+"/frame-"+i.ToString("D3")+".png",tex.EncodeToPNG());
            }
            var report=new{frames=180,rate=30,seconds=6,left="original",right="polish034",nativeUnity=true,previewOnly=true,maxA,maxB,qualified=maxA>.1f&&maxB>.1f,rows};
            File.WriteAllText(dir+"/proof.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return new{frames=180,maxA,maxB,qualified=maxA>.1f&&maxB>.1f};
        }
        finally
        {
            graph.Destroy();RenderTexture.active=previous;if(cameraGo)cameraGo.GetComponent<Camera>().targetTexture=null;
            foreach(var proxy in proxies)UnityEngine.Object.DestroyImmediate(proxy);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
            foreach(var go in new[]{old,current,cameraGo,lightGo,floor})if(go)UnityEngine.Object.DestroyImmediate(go);
            if(material)UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);AssetDatabase.DeleteAsset(temporary);
        }
    }
    public static object LoopAudit(string label)
    {
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var a=root.GetComponentInChildren<Animator>();a.enabled=false;
            var b=root.GetComponentsInChildren<Transform>().Where(t=>new[]{"FootL","FootR","Head","WingL2"}.Contains(t.name)).ToArray();
            var rows=new List<object>();
            foreach(var cl in a.runtimeAnimatorController.animationClips.Where(c=>c.isLooping).Distinct())
            {
                float e=cl.length/200f;
                Vector3[] At(float t){cl.SampleAnimation(a.gameObject,t);return b.Select(x=>x.position).ToArray();}
                var p0=At(0);var p1=At(e);var p2=At(cl.length-e);var p3=At(cl.length);
                rows.Add(new{cl.name,cl.length,bones=b.Select((t,i)=>new{t.name,seam=(p3[i]-p0[i]).magnitude,velocityJump=((p1[i]-p0[i])-(p3[i]-p2[i])).magnitude/e}).ToArray()});
            }
            File.WriteAllText(Work+"/"+label+"-loops.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static object Inspect()
    {
        var p=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        var a=p.GetComponentInChildren<Animator>(true);
        var ai=p.GetComponent<ChickenBroAI>();
        var cc=p.GetComponent<CharacterController>();
        var c=(AnimatorController)a.runtimeAnimatorController;
        var report=new {
            playing=Application.isPlaying, timeScale=Time.timeScale,
            scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray(),
            scale=V(p.transform.localScale),visualScale=V(a.transform.localScale),
            capsule=new{cc.height,cc.radius,center=V(cc.center),cc.stepOffset},
            ai=new{ai.matchLocomotionToMovement,ai.walkSpeed,ai.runSpeed,ai.strikeDuration,ai.wingDuration,ai.alertSeconds,ai.flinchSeconds,ai.knockdownSeconds},
            states=c.layers[0].stateMachine.states.Select(s=>new{name=s.state.name,speed=s.state.speed,speedParameter=s.state.speedParameter,motion=s.state.motion?s.state.motion.name:"",duration=s.state.motion is AnimationClip clip?clip.length:0}).ToArray(),
            transitions=c.layers[0].stateMachine.anyStateTransitions.Select(t=>new{destination=t.destinationState.name,t.duration,t.hasFixedDuration,t.hasExitTime,t.canTransitionToSelf}).ToArray(),
            clips=c.animationClips.Distinct().Select(cl=>new{cl.name,cl.length,cl.frameRate,cl.isLooping,curves=AnimationUtility.GetCurveBindings(cl).Length,events=cl.events.Length}).ToArray(),
            live=UnityEngine.Object.FindObjectsByType<ChickenBroAI>(FindObjectsSortMode.None).Where(x=>x.name.Contains("Goose")).Select(x=>new{x.name,x.enabled,mode=x.Mode.ToString(),x.ActionPhase,position=V(x.transform.position),height=x.GetComponent<CharacterController>().height}).ToArray()
        };
        Directory.CreateDirectory(Work);
        File.WriteAllText(Work+"/inspection-latest.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
