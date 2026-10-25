using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class TestCombatRecoveryV245
{
    static readonly List<string> checks=new List<string>();
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add(message);}
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    public static async Task<object> Run()
    {
        if(!Application.isPlaying)throw new Exception("Existing Editor Play required");
        var extra=new List<GameObject>();checks.Clear();var measures=new Dictionary<string,object>();
        try
        {
            CombatFeelPreview.Spawn();await Task.Delay(700);
            var player=CombatFeelPreview.Player;var movement=player.GetComponent<ThirdPersonPlayerController>();
            var health=player.GetComponent<PlayerHealth>();if(!health)health=player.AddComponent<PlayerHealth>();
            movement.ExternalControlLock=false;movement.enabled=false;
            var animator=movement.CharacterAnimator;var feedback=player.GetComponent<CombatHitFeedback>();
            health.maxHealth=health.currentHealth=100;health.NarrativeHealthFloor=0;
            var originalDeath=health.OnDeath;health.OnDeath=new UnityEngine.Events.UnityEvent();int deaths=0;health.OnDeath.AddListener(()=>deaths++);
            Vector3 origin=player.transform.position+Vector3.forward*3,point=player.GetComponent<CharacterController>().bounds.center;
            int beforeHits=feedback.HitCount;float oldScale=Time.timeScale;
            Check(health.ApplyAttackDamage(30,point,origin),"A real hostile hit is accepted");
            Check(Mathf.Abs(health.currentHealth-82)<.001f,"30 raw hostile damage becomes 18 after multiplier and per-hit ceiling");
            Check(feedback.HitCount==beforeHits+1 && CombatLocalHitStop.Active(player),"Accepted player damage emits exactly one feedback and local stop");
            Check(player.GetComponent<CombatReceivedReaction>().IsReacting && player.GetComponent<CombatDamageFlash>().IsFlashing,"Player receives authored reaction and material response");
            Check(!health.ApplyAttackDamage(30,point,origin+Vector3.right*4) && health.currentHealth==82 && feedback.HitCount==beforeHits+1,"A following simultaneous source cannot chain-damage or duplicate feedback");
            await Task.Delay(160);
            var received=player.GetComponent<CombatReceivedReaction>();
            measures["receivedWeightAt160ms"]=received.Weight;
            Check(received.Weight>.05f,"Authored received-hit layer actually blends into the running native Animator graph");
            Check(Time.timeScale==oldScale,"Received feedback never changes global time");
            await Task.Delay(530);
            Check(!health.IsProtected && received.Weight<.001f,"Hit protection and authored overlay recover without a permanent lock");
            Check(health.ApplyAttackDamage(6,point,origin) && Mathf.Abs(health.currentHealth-77.8f)<.001f,"Small hostile attacks scale by 0.7 rather than always using the damage ceiling");
            await Task.Delay(650);
            movement.enabled=true;
            Set(movement,"rollTimer",movement.rollDuration*.9f);
            Check(movement.DodgeInvulnerable && !health.ApplyAttackDamage(30,point,origin),"Early active dodge avoids real hostile damage");
            Set(movement,"rollTimer",movement.rollDuration*.32f);
            Check(movement.DodgeInvulnerable,"Late active dodge stays protected through 68 percent");
            Set(movement,"rollTimer",movement.rollDuration*.1f);
            Check(!movement.DodgeInvulnerable,"Dodge recovery remains vulnerable");
            Set(movement,"rollTimer",0f);movement.enabled=false;
            beforeHits=feedback.HitCount;health.currentHealth=100;health.NarrativeHealthFloor=1;
            health.ApplyDamage(30,point);Check(health.currentHealth==70 && feedback.HitCount==beforeHits,"Raw narrative damage retains the authored 30HP loss and no gameplay hurt overlay");
            health.NarrativeHealthFloor=0;health.currentHealth=100;
            for(int i=0;i<5;i++){Check(health.ApplyAttackDamage(10000,point,origin),"Heavy hit "+(i+1)+" accepted once");await Task.Delay(640);}
            Check(health.currentHealth==10 && deaths==0,"Five separate oversized attacks still leave a full-health player alive");
            health.ApplyAttackDamage(10000,point,origin);health.ApplyAttackDamage(10000,point,origin);
            Check(health.currentHealth==0 && deaths==1,"Combat can still kill; death fires exactly once");
            health.currentHealth=100;health.OnDeath=originalDeath;Set(health,"recoveryUntil",0f);
            var boss=CombatFeelPreview.Target;var nl=boss.GetComponent<NailongHealth>();var recoil=boss.GetComponent<NailongHitReaction>();
            float hp=nl.currentHealth;int count=feedback.HitCount;
            Check(CombatHitFeedback.Apply(player,nl,25,boss.transform.position,CombatHitKind.ComboOne,Vector3.forward),"Actual boss light damage resolves");
            Check(nl.currentHealth==hp-25 && feedback.HitCount==count+1 && recoil.IsReacting && boss.GetComponent<CombatDamageFlash>().IsFlashing,"Boss damage, recoil, flash and feedback remain synchronized");
            Check(CombatLocalHitStop.Active(boss),"Armored boss light contact has a visible local stop");
            var stop=boss.GetComponent<CombatLocalHitStop>();float remaining=(float)typeof(CombatLocalHitStop).GetField("until",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(stop)-Time.unscaledTime;
            measures["bossLightStopSeconds"]=remaining;Check(remaining>.035f,"Boss light contact is not reduced to the old approximately 17ms stop");
            await Task.Delay(120);
            Check(recoil.IsReacting,"Boss recoil survives beyond local hit stop");
            // A routed player hit carries actual accepted damage, rather than reporting the requested amount.
            Set(health,"recoveryUntil",0f);health.currentHealth=100;
            float accepted=-1;int events=0;void Capture(CombatHitEvent e){if(e.target==health){accepted=e.damage;events++;}}
            CombatHitFeedback.HitConfirmed+=Capture;
            try{CombatHitFeedback.Apply(boss,health,30,point,CombatHitKind.ComboOne,-Vector3.forward);}
            finally{CombatHitFeedback.HitConfirmed-=Capture;}
            Check(events==1 && Mathf.Abs(accepted-18)<.001f && health.currentHealth==82,"Resolver path emits one actual accepted-damage event");
            var data=new{passed=true,checks,measures};File.WriteAllText(".codex/combat-recovery-20261023/recovery-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;
        }
        catch(Exception e){File.WriteAllText(".codex/combat-recovery-20261023/recovery-failure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks,measures,error=e.ToString()},Newtonsoft.Json.Formatting.Indented));throw;}
        finally{foreach(var go in extra)if(go)UnityEngine.Object.DestroyImmediate(go);CombatFeelPreview.Clear();}
    }
}
