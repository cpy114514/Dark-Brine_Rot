using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;
using Mavis;

public static class VerifyBirdRush
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(SahurAttack a,string field,object value)=>typeof(SahurAttack).GetField(field,Flags).SetValue(a,value);
    static object Call(SahurAttack a,string method,params object[] args)=>typeof(SahurAttack).GetMethod(method,Flags).Invoke(a,args);
    public static object State()=>new{playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,productionRushLoaded=typeof(SahurAttack).GetMethod("ScanTwinBladeRushTravel",Flags)!=null,lockHeld=SessionState.GetBool("BirdCombatFix.ReloadLock",false)};
    static void Reset(SahurAttack a,Animator animator,float phase)
    {
        a.GetComponent<CombatLocalHitStop>()?.Release();
        Set(a,"attacking",true);Set(a,"activeAttackHash",Animator.StringToHash("Base Layer.Capri Heavy"));
        Set(a,"swingDamage",10f);Set(a,"observedAttackPhase",-1f);Set(a,"hasWeaponSample",false);Set(a,"swishPlayed",true);
        ((HashSet<IDamageable>)typeof(SahurAttack).GetField("hitThisSwing",Flags).GetValue(a)).Clear();
        animator.Play("Base Layer.Capri Heavy",0,phase);animator.Update(0);
    }
    static void Volume(SahurAttack a,out Vector3 center)
    {
        object[] args={null,null,null};Call(a,"GetWeaponVolume",args);center=((Vector3)args[0]+(Vector3)args[1])*.5f;
    }
    public static object Coverage()
    {
        var report=new List<object>();
        var root=new GameObject("Isolated bird anatomy coverage");root.SetActive(false);root.AddComponent<GameSaveExcluded>();root.transform.position=new Vector3(15000,2200,15000);
        try
        {
            foreach(var path in new[]{"ChickenBro/ChickenBro.prefab","LongLeggedDuck/LongLeggedDuck.prefab","BullyGoose/BullyGoose.prefab"})
            {
                var bird=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/"+path),root.transform,false);
                bird.AddComponent<GameSaveExcluded>();foreach(var mono in bird.GetComponentsInChildren<MonoBehaviour>(true))mono.enabled=false;
                root.SetActive(true);var hurt=BirdHurtboxes.Ensure(bird);
                if(!Application.isPlaying){typeof(BirdHurtboxes).GetMethod("Awake",Flags).Invoke(hurt,null);typeof(BirdHurtboxes).GetMethod("OnEnable",Flags).Invoke(hurt,null);}
                var skin=bird.GetComponentInChildren<SkinnedMeshRenderer>();var animator=bird.GetComponentInChildren<Animator>();
                var clips=animator.runtimeAnimatorController.animationClips.Where(c=>c).Distinct().Take(8).ToArray();
                var parts=bird.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled).ToArray();var motor=bird.GetComponent<CharacterController>();
                var samples=new List<object>();
                var vertices=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;var binds=skin.sharedMesh.bindposes;
                    foreach(var clip in clips)foreach(float time in new[]{.15f,.5f,.85f})
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*time);Physics.SyncTransforms();
                        var matrices=skin.bones.Select((bone,index)=>bone.localToWorldMatrix*binds[index]).ToArray();
                        int total=0,beforeMiss=0,afterMiss=0;
                        for(int i=0;i<vertices.Length;i+=Mathf.Max(1,vertices.Length/500))
                        {
                            var w=weights[i];var v=vertices[i];
                            var point=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;total++;
                            if(Vector3.Distance(motor.ClosestPoint(point),point)>.12f)beforeMiss++;
                            if(parts.All(c=>Vector3.Distance(c.ClosestPoint(point),point)>.12f))afterMiss++;
                        }
                        samples.Add(new{clip=clip.name,time,total,beforeMiss,afterMiss});
                    }
                report.Add(new{prefab=path,samples});UnityEngine.Object.DestroyImmediate(bird);root.SetActive(false);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        File.WriteAllText("Documentation/BirdAnatomyVerification-20261108.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
    public static object Run()
    {
        var root=new GameObject("Bird rush isolated verification");root.SetActive(false);root.AddComponent<GameSaveExcluded>();
        root.transform.position=new Vector3(15000,2200,15000);
        var report=new List<object>();
        try
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
            var actor=new GameObject("Isolated twin blade user");actor.transform.SetParent(root.transform,false);actor.transform.localScale=source.transform.localScale;
            actor.AddComponent<GameSaveExcluded>();
            var originalBody=source.GetComponent<CharacterController>();var body=actor.AddComponent<CharacterController>();
            body.center=originalBody.center;body.height=originalBody.height;body.radius=originalBody.radius;
            var visual=UnityEngine.Object.Instantiate(source.transform.Find("Pbr Sahur Visual").gameObject,actor.transform,false);
            foreach(var mono in visual.GetComponentsInChildren<MonoBehaviour>(true))mono.enabled=false;
            var animator=visual.GetComponent<Animator>();
            animator.runtimeAnimatorController=Resources.Load<RuntimeAnimatorController>("Encounter/v006/SahurGameplay");
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var attack=actor.AddComponent<SahurAttack>();attack.animator=animator;attack.enabled=false;
            root.SetActive(true);animator.enabled=true;animator.Rebind();animator.Update(0);
            if(!Application.isPlaying)Call(attack,"Awake");
            var loadout=actor.AddComponent<SahurWeaponLoadout>();actor.AddComponent<EquipmentInventory>();loadout.Initialize();loadout.enabled=false;
            bool made=(bool)typeof(SahurWeaponLoadout).GetMethod("CreateBlades",Flags).Invoke(loadout,null);
            if(!made)throw new Exception("Could not create the real twin-blade meshes.");
            var right=(Collider)typeof(SahurWeaponLoadout).GetField("rightHitbox",Flags).GetValue(loadout);
            var left=(Collider)typeof(SahurWeaponLoadout).GetField("leftHitbox",Flags).GetValue(loadout);
            right.gameObject.SetActive(true);left.gameObject.SetActive(true);attack.SetTwinBladeMode(true,right,left);
            var paths=new[]{"ChickenBro/ChickenBro.prefab","LongLeggedDuck/LongLeggedDuck.prefab","BullyGoose/BullyGoose.prefab"};
            foreach(var path in paths)
            {
                body.enabled=false;actor.transform.localPosition=Vector3.zero;body.enabled=true;
                var targetRoot=new GameObject("Isolated target");targetRoot.transform.SetParent(root.transform,false);targetRoot.SetActive(false);
                var bird=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/"+path),targetRoot.transform,false);
                bird.AddComponent<GameSaveExcluded>();foreach(var mono in bird.GetComponentsInChildren<MonoBehaviour>(true))mono.enabled=false;
                targetRoot.SetActive(true);var hurtboxes=BirdHurtboxes.Ensure(bird);
                if(!Application.isPlaying)
                {
                    typeof(BirdHurtboxes).GetMethod("Awake",Flags).Invoke(hurtboxes,null);
                    typeof(BirdHurtboxes).GetMethod("OnEnable",Flags).Invoke(hurtboxes,null);
                }
                var hp=bird.GetComponent<Health>();hp.maxHealth=hp.currentHealth=1000;
                // Finish the move on .64 after an active sample at .48. Place a
                // real bird's wing/body around the blade's final travel segment.
                Reset(attack,animator,.48f);Call(attack,"UpdateHitbox");
                Vector3 beforeMove=actor.transform.position;
                body.Move(Vector3.forward*2f);
                Vector3 travel=actor.transform.position-beforeMove;
                animator.Play("Base Layer.Capri Heavy",0,.64f);animator.Update(0);
                Volume(attack,out var center);
                var skin=bird.GetComponentInChildren<SkinnedMeshRenderer>();
                bird.transform.position+=center-skin.bounds.center;
                Physics.SyncTransforms();
                float before=hp.currentHealth;Call(attack,"UpdateHitbox");float oldLoss=before-hp.currentHealth;
                Call(attack,"ScanTwinBladeRushTravel",travel);float newLoss=before-hp.currentHealth;
                Call(attack,"ScanTwinBladeRushTravel",travel);float repeatLoss=before-hp.currentHealth;
                if(newLoss!=10||repeatLoss!=10)throw new Exception(path+" expected exactly 10 damage, got "+newLoss+" / "+repeatLoss);
                // Receive-only anatomy must disable at death and re-enable when
                // a pooled actor activates, without creating duplicate shapes.
                int parts=bird.GetComponentsInChildren<Collider>(true).Count(CombatBodyCollision.IsAnatomy);
                hp.currentHealth=0;hp.OnDeath?.Invoke();bool deadDisabled=bird.GetComponentsInChildren<Collider>(true).Where(CombatBodyCollision.IsAnatomy).All(c=>!c.enabled);
                hp.currentHealth=1000;BirdHurtboxes.Ensure(bird);bool restored=bird.GetComponentsInChildren<Collider>(true).Where(CombatBodyCollision.IsAnatomy).All(c=>c.enabled);
                if(!deadDisabled||!restored||parts!=bird.GetComponentsInChildren<Collider>(true).Count(CombatBodyCollision.IsAnatomy))throw new Exception("Pool lifecycle failure: "+path);
                report.Add(new{prefab=path,controllerTravel=travel.magnitude,oldTailDamage=oldLoss,fixedTailDamage=newLoss,repeatedSweepDamage=repeatLoss,parts,deadDisabled,restored});
                UnityEngine.Object.DestroyImmediate(targetRoot);
            }
            // Scenery must still reject contacts; a non-contact must stay a miss.
            var dummy=GameObject.CreatePrimitive(PrimitiveType.Cube);dummy.name="Isolated wall-test victim";dummy.transform.SetParent(root.transform,false);dummy.tag="Enemy";
            var hpDummy=dummy.AddComponent<Health>();hpDummy.maxHealth=hpDummy.currentHealth=1000;
            Reset(attack,animator,.64f);Volume(attack,out var point);dummy.transform.position=point;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Isolated scenery blocker";wall.transform.SetParent(root.transform,false);
            wall.transform.position=(body.bounds.center+point)*.5f;wall.transform.rotation=Quaternion.LookRotation(point-body.bounds.center);wall.transform.localScale=new Vector3(50,50,.1f);
            Physics.SyncTransforms();Call(attack,"ScanTwinBladeRushTravel",Vector3.forward);float wallDamage=1000-hpDummy.currentHealth;
            UnityEngine.Object.DestroyImmediate(wall);dummy.transform.position+=Vector3.right*30;hpDummy.currentHealth=1000;
            Reset(attack,animator,.64f);Physics.SyncTransforms();Call(attack,"ScanTwinBladeRushTravel",Vector3.forward);float outsideDamage=1000-hpDummy.currentHealth;
            if(wallDamage!=0||outsideDamage!=0)throw new Exception("Wall/non-contact regression: "+wallDamage+" / "+outsideDamage);
            report.Add(new{wallDamage,outsideDamage});
        }
        finally{UnityEngine.Object.DestroyImmediate(root);Physics.SyncTransforms();}
        Directory.CreateDirectory("Documentation");
        File.WriteAllText("Documentation/BirdRushVerification-20261108.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
