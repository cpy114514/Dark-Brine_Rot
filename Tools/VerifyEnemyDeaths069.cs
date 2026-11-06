using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Mavis;
public static class VerifyEnemyDeaths069
{
    public static Task<object> Run()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        var completion=new TaskCompletionSource<object>();var holder=new GameObject("Death regression 069");
        var run=holder.AddComponent<DeathRegression069>();run.completion=completion;run.Begin();return completion.Task;
    }
}
public sealed class DeathRegression069:MonoBehaviour
{
    public TaskCompletionSource<object> completion;
    readonly List<object> checks=new List<object>();readonly List<string> errors=new List<string>();
    GameObject root;MeshCollider ground;float oldTime;
    void Check(bool pass,string name){checks.Add(new{pass,name});if(!pass)throw new Exception(name);}
    void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message);}
    public void Begin(){oldTime=Time.timeScale;Application.logMessageReceived+=Log;StartCoroutine(Wrapped());}
    IEnumerator Wrapped()
    {
        var work=Core();
        while(true)
        {
            bool more=false;Exception failure=null;object current=null;
            try{more=work.MoveNext();if(more)current=work.Current;}catch(Exception e){failure=e;}
            if(failure!=null){Finish(failure);yield break;}
            if(!more){Finish(null);yield break;}yield return current;
        }
    }
    GameObject Spawn(string path,Vector3 at)
    {
        var holder=new GameObject("Inactive spawn staging");holder.transform.SetParent(root.transform,false);holder.SetActive(false);
        var go=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),holder.transform);go.AddComponent<GameSaveExcluded>();
        ground.Raycast(new Ray(at+Vector3.up*150,Vector3.down),out var hit,400);go.transform.position=hit.point;holder.SetActive(true);return go;
    }
    IEnumerator Core()
    {
        var scene=SceneManager.CreateScene("Death regression 069");root=new GameObject("Excluded death regression actors");SceneManager.MoveGameObjectToScene(root,scene);root.AddComponent<GameSaveExcluded>();
        ground=FindFirstObjectByType<ProceduralIsland>().GetComponent<MeshCollider>();
        var actor=Spawn("Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab",new Vector3(288,0,346));
        var attacker=new GameObject("Death069 attack origin");attacker.transform.SetParent(root.transform);attacker.transform.position=actor.transform.position-Vector3.forward*2;
        var extra=new GameObject("Reusable hit volume");extra.transform.SetParent(actor.transform,false);var extraCollider=extra.AddComponent<BoxCollider>();extraCollider.isTrigger=true;
        yield return new WaitForSeconds(.25f);
        var brain=actor.GetComponent<ChickenBroAI>();var hp=actor.GetComponent<Health>();var animator=actor.GetComponentInChildren<Animator>();var visual=animator.transform;
        bool originalAnimator=animator.enabled;var scale=visual.localScale;int deaths=0;hp.OnDeath.AddListener(()=>deaths++);
        brain.ResolveCombatHit(attacker,hp.maxHealth+1,actor.transform.position+Vector3.up,CombatHitKind.ChargedHeavy);
        var death=actor.GetComponent<EnemyDeathPresentation>();
        Check(hp.IsDead&&death&&death.IsPlaying&&brain.Mode==ChickenBroAI.Mood.Dead,"Real lethal combat hit starts death presentation");
        Check(actor.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"Dead enemies and their hit volumes stop blocking the player");
        hp.ApplyDamage(1,actor.transform.position);Check(deaths==1,"Death event fires once for repeated damage");
        yield return new WaitForSeconds(.3f);
        Time.timeScale=0;var paused=visual.position;var pausedRotation=visual.rotation;
        for(int i=0;i<5;i++)yield return null;
        Check(Vector3.Distance(paused,visual.position)<.0001f&&Quaternion.Angle(pausedRotation,visual.rotation)<.01f,"Death movement pauses with gameplay");Time.timeScale=oldTime;
        yield return new WaitForSeconds(1.5f);
        Check(Vector3.Distance(scale,visual.localScale)<.0001f,"Corpse keeps the character's size");
        var held=visual.position;var heldRotation=visual.rotation;yield return new WaitForSeconds(.2f);
        Check(Vector3.Distance(held,visual.position)<.0001f&&Quaternion.Angle(heldRotation,visual.rotation)<.01f,"Settled corpse stops moving");
        actor.SetActive(false);
        Check(!death.IsPlaying&&animator.enabled==originalAnimator&&extraCollider.enabled,"Pool return restores animation and child collider state");
        ground.Raycast(new Ray(new Vector3(286,150,349),Vector3.down),out var respawn,400);
        var motor=actor.GetComponent<CharacterController>();float sole=(motor.center.y-motor.height*.5f)*actor.transform.lossyScale.y;
        brain.Activate(respawn.point-Vector3.up*sole+Vector3.up*.04f,Quaternion.identity,null);
        yield return new WaitForSeconds(.18f);
        Check(brain.IsAlive&&motor.enabled&&!death.IsPlaying&&extraCollider.enabled,"Reused enemy returns to a functional living state");
        brain.ResolveCombatHit(attacker,1,actor.transform.position,CombatHitKind.ChargedHeavy);yield return new WaitForSeconds(.16f);
        var before=visual.position;
        brain.ResolveCombatHit(attacker,hp.maxHealth+1,actor.transform.position,CombatHitKind.ComboOne);yield return null;
        Check(deaths==2&&death.IsPlaying,"Reused enemy can die again exactly once");
        Check(Vector3.Distance(before,visual.position)<1f,"An airborne fatal hit does not snap the body to the ground");
        var cup=Spawn("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab",new Vector3(235,0,500));yield return null;
        var cupHp=cup.GetComponent<Health>();cupHp.ApplyDamage(cupHp.maxHealth+1,cup.transform.position+Vector3.forward);
        var loot=cup.GetComponent<CappuccinoWeaponLoot>();Check(loot.HasDropped&&loot.Drop()==null,"Cappuccino still drops its reward exactly once");
        var nailong=Spawn("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab",new Vector3(18,0,868));yield return new WaitForSeconds(.1f);
        var phase=nailong.GetComponent<NailongPhaseAnimation>();Check(phase&&phase.Play("BossCombat/Animations/v228/NailongRoar",2),"Nailong action mixer is active before lethal interruption");
        yield return new WaitForSeconds(.15f);var nh=nailong.GetComponent<NailongHealth>();nh.ApplyDamage(nh.maxHealth+1,nailong.transform.position+Vector3.forward);
        Check(!phase.IsPlaying&&nailong.GetComponent<EnemyDeathPresentation>().IsPlaying,"Nailong death releases the active skill animation mixer");
        // Test the existing story branch separately from the real fatal-damage
        // tests. Triggering its event avoids editing the player's quest progress.
        var storyRoot=new GameObject("Excluded abduction regression");SceneManager.MoveGameObjectToScene(storyRoot,SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity"));storyRoot.SetActive(false);storyRoot.AddComponent<GameSaveExcluded>();
        var player=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),storyRoot.transform);player.AddComponent<GameSaveExcluded>();player.transform.position=new Vector3(288,23,346);
        var plane=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab"),storyRoot.transform);plane.AddComponent<GameSaveExcluded>();plane.transform.position=new Vector3(288,42,330);storyRoot.SetActive(true);yield return new WaitForSeconds(.12f);
        var boss=plane.GetComponent<BombardinoBoss>();boss.target=player.transform;
        typeof(BombardinoBoss).GetField("targetHealth",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(boss,player.GetComponent<PlayerHealth>());
        var planeHp=plane.GetComponent<Health>();planeHp.currentHealth=0;planeHp.OnDeath.Invoke();yield return new WaitForSeconds(.25f);
        var flight=plane.GetComponent<BombardinoAbduction>();
        Check(boss.AbductionActive&&flight&&BombardinoAbduction.Playing&&!plane.GetComponent<EnemyDeathPresentation>(),"Aircraft defeat still enters the grab story instead of corpse animation");
        flight.enabled=false;for(int i=0;i<6;i++)yield return null;
        Check(!BombardinoAbduction.Playing&&player.GetComponent<ThirdPersonPlayerController>().enabled,"Interrupting the grab restores the player's control");
        Destroy(storyRoot);Check(errors.Count==0,"No runtime errors during death, reuse, loot or story checks");
    }
    void Finish(Exception error)
    {
        Time.timeScale=oldTime;Application.logMessageReceived-=Log;
        var result=new{passed=error==null&&errors.Count==0,checks,errors,error=error?.Message};
        File.WriteAllText(".codex/enemy-death069/regression.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        completion.TrySetResult(result);if(root)Destroy(root);Destroy(gameObject);
    }
}
