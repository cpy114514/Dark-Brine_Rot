using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    public sealed partial class RuntimeStabilityCheck
    {
        bool firstIsland;
        Key[] islandKeys=Array.Empty<Key>();
        Component reviewedBoss;
        readonly Dictionary<string,HashSet<string>> seenBossStates=new Dictionary<string,HashSet<string>>();
        readonly Dictionary<string,int> attackingFrames=new Dictionary<string,int>();
        readonly List<object> phaseReports=new List<object>();
        float nextIslandInput;
        PlayerHealth receivingHealth;
        int receivedHits;
        void ReceivedTestHit(float amount){if(amount>0)receivedHits++;}
        void MaintainTestHealth(){if(firstIsland&&receivingHealth&&receivingHealth.currentHealth>0)receivingHealth.currentHealth=receivingHealth.maxHealth;}
        void ObserveBoss()
        {
            if(!firstIsland||!reviewedBoss)return;
            string state="";bool attacking=false;
            if(reviewedBoss is CappuccinoAI capri){state=capri.CurrentState+"/"+capri.CurrentAttack;attacking=capri.CurrentState==CappuccinoAI.State.Attack;}
            if(reviewedBoss is NailongAI nailong){state=nailong.CurrentState+"/"+nailong.AttackName;attacking=nailong.CurrentState==NailongAI.State.Attack;}
            if(reviewedBoss is BombardinoBoss aircraft){state=aircraft.CurrentState+"/"+aircraft.CurrentAttack;attacking=aircraft.CurrentState==BombardinoBoss.State.Attack;}
            if(!seenBossStates.TryGetValue(phase,out var states))seenBossStates.Add(phase,states=new HashSet<string>());
            states.Add(state);if(attacking){attackingFrames.TryGetValue(phase,out int frames);attackingFrames[phase]=frames+1;}
        }
        void PlaceNear(Vector3 point,Vector3 facing)
        {
            var hits=Physics.RaycastAll(point+Vector3.up*100,Vector3.down,220,~0,QueryTriggerInteraction.Ignore);
            float best=float.NegativeInfinity;
            foreach(var hit in hits)if(hit.normal.y>.6f&&!CombatBodyCollision.IsActor(hit.collider)&&hit.collider.GetComponentInParent<ProceduralIsland>())best=Mathf.Max(best,hit.point.y);
            if(best>float.NegativeInfinity)point.y=best;
            player.RestoreSavedPose(point+Vector3.up*(-player.LowestFootWorldOffset+.08f),Quaternion.LookRotation(Vector3.ProjectOnPlane(facing,Vector3.up)));
            Cursor.lockState=CursorLockMode.Locked;
        }
        void IslandInput(float elapsed)
        {
            if(Time.realtimeSinceStartup<nextIslandInput)return;nextIslandInput=Time.realtimeSinceStartup+.12f;
            var keys=new List<Key>(2);
            if(elapsed%4<.6f)keys.Add(GameInputSettings.Get(GameInputSettings.Action.Forward));
            if(elapsed%5<.15f)keys.Add(GameInputSettings.Get(GameInputSettings.Action.Dodge));
            if(elapsed%11<.15f)keys.Add(GameInputSettings.Get(GameInputSettings.Action.Jump));
            islandKeys=keys.ToArray();held=(Mathf.FloorToInt(elapsed*4)&1)==0;
        }
        IEnumerator BossWindow(Component boss,string name,int healthPhase,float seconds)
        {
            reviewedBoss=boss;phase=name+" phase "+healthPhase;nextMove=0;nextIslandInput=0;previous=0;
            SaveIslandProgress();
            float started=Time.realtimeSinceStartup;double largest=0;int frames=0;int initialAttacks=attackFrames;
            Vector3 center=boss is BombardinoBoss aircraft?aircraft.Ground(aircraft.transform.position):boss.transform.position;
            while(Time.realtimeSinceStartup-started<seconds)
            {
                float elapsed=Time.realtimeSinceStartup-started;
                if(boss is NailongAI nl)nl.health.currentHealth=nl.health.maxHealth*(healthPhase==1?1:healthPhase==2?.5f:.2f);
                else if(boss.TryGetComponent<Health>(out var hp))hp.currentHealth=hp.maxHealth*(healthPhase==1?1:.35f);
                if(Time.realtimeSinceStartup>=nextMove)
                {
                    nextMove=Time.realtimeSinceStartup+8;
                    float radius=boss is CappuccinoAI c?c.attackRange*.85f:boss is NailongAI n?((elapsed%24<12)?n.EffectiveAttackRange*.85f:n.EffectiveSpitRange*.65f):14;
                    float angle=elapsed*.2f;Vector3 direction=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                    var at=(boss is BombardinoBoss?center:boss.transform.position)+direction*radius;
                    PlaceNear(at,boss.transform.position-at);
                }
                IslandInput(elapsed);frames++;largest=Math.Max(largest,Time.unscaledDeltaTime*1000);
                yield return null;
            }
            islandKeys=Array.Empty<Key>();held=false;
            attackingFrames.TryGetValue(phase,out int activeFrames);
            Check(activeFrames>0,name+" phase "+healthPhase+" actually attacks in normal Player window");
            phaseReports.Add(new{phase,frames,activeFrames,playerAttackFrames=attackFrames-initialAttacks,largestEngineFrameMs=largest,states=seenBossStates[phase].ToArray()});
            SaveIslandProgress();
        }
        void SaveIslandProgress()
        {
            string progressPath=Path.ChangeExtension(path,"progress.json");
            File.WriteAllText(progressPath,Newtonsoft.Json.JsonConvert.SerializeObject(new{phase,phaseReports,errors,frame=Time.frameCount},Newtonsoft.Json.Formatting.Indented));
        }
        IEnumerator RunFirstIsland()
        {
            yield return Load("Assets/Scenes/First Island/Main.unity");GameWorldProgress.Reset();
            receivingHealth=player.GetComponent<PlayerHealth>();
            typeof(PlayerHealth).GetField("protectedUntil",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(receivingHealth,-1f);
            receivingHealth.Damaged+=ReceivedTestHit;
            phase="island exploration";float start=Time.realtimeSinceStartup;nextMove=0;nextIslandInput=0;
            SaveIslandProgress();
            var points=new[]{new Vector3(288,30,346),new Vector3(300,30,430),new Vector3(240,45,498),new Vector3(120,50,525)};
            while(Time.realtimeSinceStartup-start<30)
            {
                float elapsed=Time.realtimeSinceStartup-start;
                if(Time.realtimeSinceStartup>=nextMove){nextMove=Time.realtimeSinceStartup+8;PlaceNear(points[Mathf.FloorToInt(elapsed/8)%points.Length],Vector3.forward);}
                IslandInput(elapsed);yield return null;
            }
            var capri=FindFirstObjectByType<CappuccinoAI>(FindObjectsInactive.Include);var nailong=FindFirstObjectByType<NailongAI>(FindObjectsInactive.Include);var aircraft=FindFirstObjectByType<BombardinoBoss>(FindObjectsInactive.Include);
            Check(capri&&nailong&&aircraft,"All three first-island Boss components included");
            yield return BossWindow(capri,"Cappuccino",1,40);yield return BossWindow(capri,"Cappuccino",2,40);
            // QA advances prerequisite IDs only; the human save is excluded.
            GameWorldProgress.Record(capri,0);capri.gameObject.SetActive(false);GameWorldProgress.Record(ChickenBroEncounter.Active,0);
            yield return BossWindow(nailong,"Nailong",1,45);yield return BossWindow(nailong,"Nailong",2,45);yield return BossWindow(nailong,"Nailong",3,45);
            GameWorldProgress.Record(nailong,0);nailong.gameObject.SetActive(false);
            yield return BossWindow(aircraft,"Bombardino",1,55);yield return BossWindow(aircraft,"Bombardino",2,55);
            Check(attackFrames>0,"Player attacks through normal virtual-device input");
            Check(receivedHits>0,"Real hostile hits exercise hurt audio, hit-stop and impact feedback");
            Check(errors.Count==0,"No errors during first-island exploration and all Boss phases");
            reviewedBoss=null;phase="complete";SaveIslandProgress();
        }
    }
}
