using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;
public static class TestCombatFeelIntegrationV233
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> passed=new List<string>();
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);passed.Add(message);}
    static void Set(SahurAttack attack,string name,object value)=>typeof(SahurAttack).GetField(name,Private).SetValue(attack,value);
    static object V(Vector3 x)=>new{x=x.x,y=x.y,z=x.z};
    public static async Task<object> Run()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        GameObject player=null,target=null,clock=null,capri=null;float originalScale=Time.timeScale,volume=AudioListener.volume;
        passed.Clear();
        try
        {
            // Isolated from story/menu/tutorial and the native-input preview.
            CombatFeelPreview.Spawn();
            player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(5000,500,5000),Quaternion.identity);
            player.AddComponent<GameSaveExcluded>();player.GetComponent<ThirdPersonPlayerController>().enabled=false;
            player.GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled=false;
            var attack=player.GetComponent<SahurAttack>();attack.enabled=false;
            var feedback=player.GetComponent<CombatHitFeedback>();
            Vector3 origin=player.GetComponent<CharacterController>().bounds.center;
            target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="Thin swept contact fixture";target.tag="Enemy";
            target.transform.position=origin+Vector3.forward;target.transform.localScale=new Vector3(.08f,.08f,.08f);
            target.AddComponent<GameSaveExcluded>();var health=target.AddComponent<Health>();health.maxHealth=health.currentHealth=100000;
            var child=new GameObject("Second hurtbox for the same health");child.transform.SetParent(target.transform,false);child.AddComponent<BoxCollider>();
            Set(attack,"attacking",true);Set(attack,"comboStep",0);Set(attack,"swingDamage",25f);attack.stickHitbox.enabled=true;
            var sweep=typeof(SahurAttack).GetMethod("ScanWeaponSweep",Private);
            var args=new object[]{origin+Vector3.left*3,origin+Vector3.right*3,origin+Vector3.left*3+Vector3.forward*2,origin+Vector3.right*3+Vector3.forward*2,.055f};
            Physics.SyncTransforms();float before=health.currentHealth;int count=feedback.HitCount;
            sweep.Invoke(attack,args);sweep.Invoke(attack,args);
            Check(before-health.currentHealth==25 && feedback.HitCount==count+1,"Thin target crossed by middle of club is hit exactly once despite repeated sweeps and multiple hurtboxes");
            var hits=(HashSet<IDamageable>)typeof(SahurAttack).GetField("hitThisSwing",Private).GetValue(attack);hits.Clear();
            sweep.Invoke(attack,args);Check(before-health.currentHealth==50,"A new attack can hit the same enemy again");
            // External Animator speed changes belong to their owner, not the expiring hit stop.
            var stop=player.GetComponent<CombatLocalHitStop>();stop.Release();attack.animator.speed=.8f;
            stop.Hold(attack.animator,.08f);attack.animator.speed=.6f;stop.Release();
            Check(Mathf.Abs(attack.animator.speed-.6f)<.0001f,"Local stop does not overwrite a new playback owner");
            attack.animator.speed=1;
            // Native unscaled clock still expires if another system pauses the game.
            stop.Hold(attack.animator,.06f);Time.timeScale=0;await Task.Delay(180);
            Check(!stop.IsStopped && Mathf.Abs(attack.animator.speed-1)<.0001f,"Local stop safely releases while global pause is active");Time.timeScale=1;
            // A real Cappuccino presentation is manually driven; final rendered bones still hold.
            capri=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab"),origin+Vector3.forward*20,Quaternion.identity);
            capri.AddComponent<GameSaveExcluded>();capri.GetComponent<CappuccinoAI>().enabled=false;
            var presentation=capri.GetComponent<CappuccinoEnemy>();var capriAnimator=presentation.animator;
            CombatLocalHitStop.Freeze(capri,capriAnimator,.13f);var capriStop=capri.GetComponent<CombatLocalHitStop>();
            var late=typeof(CombatLocalHitStop).GetMethod("LateUpdate",Private);late.Invoke(capriStop,null);
            var bones=capri.GetComponentInChildren<SkinnedMeshRenderer>().bones;
            var rotations=new Quaternion[bones.Length];for(int i=0;i<bones.Length;i++)if(bones[i])rotations[i]=bones[i].localRotation;
            typeof(CappuccinoEnemy).GetMethod("SetBossPose",Private).Invoke(presentation,new object[]{.6f,false});capriAnimator.Update(0);
            late.Invoke(capriStop,null);float maxError=0;
            for(int i=0;i<bones.Length;i++)if(bones[i] && bones[i]!=capriAnimator.transform)maxError=Mathf.Max(maxError,Quaternion.Angle(rotations[i],bones[i].localRotation));
            Check(maxError<.001f,"Local rendered pose holds even when Cappuccino manually samples another attack frame");
            UnityEngine.Object.DestroyImmediate(capri);capri=null;
            var nailong=CombatFeelPreview.Target.GetComponent<NailongHealth>();
            var recoil=nailong.GetComponent<NailongHitReaction>();
            CombatHitFeedback.Apply(player,nailong,1,nailong.transform.position,CombatHitKind.ChargedHeavy);
            float firstStrength=(float)typeof(NailongHitReaction).GetField("strength",Private).GetValue(recoil);
            for(int i=0;i<8;i++)CombatHitFeedback.Apply(player,nailong,1,nailong.transform.position,CombatHitKind.ChargedHeavy);
            float repeatedStrength=(float)typeof(NailongHitReaction).GetField("strength",Private).GetValue(recoil);
            Check(repeatedStrength<=firstStrength+.001f,"Repeated boss impacts cannot multiply an existing recoil indefinitely");
            // Sound respects an already muted listener.
            AudioListener.volume=0;CombatHitFeedback.Apply(player,health,1,target.transform.position,CombatHitKind.ChargedHeavy);
            Check(AudioListener.volume==0,"Confirmed impact respects the existing listener mute");AudioListener.volume=volume;
            // Warm-up, then measure the complete routed impact; frame capture and reflection are excluded.
            for(int i=0;i<12;i++)CombatHitFeedback.Apply(player,health,1,target.transform.position,CombatHitKind.ComboOne,Vector3.forward);
            var timer=new Stopwatch();timer.Start();timer.Stop();
            using(var impactRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Combat.ConfirmedImpact",32))
            {
            long allocated=GC.GetAllocatedBytesForCurrentThread();timer.Restart();
            for(int i=0;i<200;i++)CombatHitFeedback.Apply(player,health,1,target.transform.position,CombatHitKind.ComboOne,Vector3.forward);
            timer.Stop();allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
            await Task.Delay(100);
            var nativeSamples=new List<ProfilerRecorderSample>();impactRecorder.CopyTo(nativeSamples);
            long markerNanoseconds=0,markerCalls=0;foreach(var sample in nativeSamples){markerNanoseconds+=sample.Value;markerCalls+=sample.Count;}
            int audioSources=player.GetComponentsInChildren<AudioSource>(true).Length;
            int particles=player.GetComponentsInChildren<ParticleSystem>(true).Length;
            Check(audioSources<=5 && particles<=8,"Two hundred impacts keep the same bounded audio and particle objects");
            var output=new{passed,impactBatchCount=200,impactBatchMs=timer.Elapsed.TotalMilliseconds,managedBytes=allocated,audioSources,particles,
                profiler=new{valid=impactRecorder.Valid,markerNanoseconds,markerCalls,samples=nativeSamples.Count},
                measurement="Warmed same-frame Editor batch; not a Player FPS or audible mix benchmark",weaponCollider=attack.stickHitbox.GetType().Name,originalScale,point=V(target.transform.position)};
            File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/uvcs-repair-20261020/combat-contracts.json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));
            return output;
            }
        }
        finally
        {
            if(player)UnityEngine.Object.DestroyImmediate(player);if(target)UnityEngine.Object.DestroyImmediate(target);if(clock)UnityEngine.Object.DestroyImmediate(clock);if(capri)UnityEngine.Object.DestroyImmediate(capri);
            CombatFeelPreview.Clear();Time.timeScale=originalScale;AudioListener.volume=volume;
        }
    }
}
