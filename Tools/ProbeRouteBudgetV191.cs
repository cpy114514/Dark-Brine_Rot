using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Explicit hypothetical planning fixture; does not move wood or authorize hits.
[DefaultExecutionOrder(32990)]
public sealed class RouteBudgetProbeV191:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    readonly List<object> rows=new List<object>();int attempted,ready;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;var film=battle!=null?battle.Finale:null;
        if(film==null || film.CinematicTime<.5f || film.CinematicTime>4)return;
        var shark=battle.GetComponent<Story1SharkCollisionSequence>().swimmer.GetComponent<TralaleroSwimAnimator>();
        var type=typeof(Story1WreckFinale);
        string[] names={"attackAim","attackFacingKey","attackStartRoot","attackStartRotation","biteRouteStartAge",
            "tailRouteStartAge","hasIntendedTailRoot","hasIntendedBiteRoot","attackFacingSampleTime","attackFacing",
            "attackContactMarkerLocal","attackEndpointCost","attackRouteCost","attackRouteOffset","facingSearchCursor",
            "tailStrokeReplans","biteStrokeReplans","lastTailReplanAge","lastBiteReplanAge"};
        var saved=names.ToDictionary(n=>type.GetField(n,Flags),n=>type.GetField(n,Flags).GetValue(film));
        var bones=shark.BoneAnimator.GetComponentsInChildren<Transform>(true);
        var positions=bones.Select(b=>b.localPosition).ToArray();var rotations=bones.Select(b=>b.localRotation).ToArray();
        string action=shark.CinematicAction;float seconds=shark.CinematicActionSeconds;
        Vector3 actual=shark.transform.position;Quaternion heading=shark.transform.rotation;
        void Set(string n,object value)=>type.GetField(n,Flags).SetValue(film,value);
        const string key="Hypothetical route budget v191";
        try
        {
            var boards=battle.Boards.Where(p=>p.IsBoardable && p.Collision!=null).OrderByDescending(p=>p.Collision.Parts.Length).Take(8).ToArray();
            if(boards.Length==0)return;
            var board=boards[attempted%boards.Length];
            shark.SampleCinematicAction("Bite_Lunge",.52f);var pose=battle.WreckContact.CaptureAnatomy();
            Vector3 centre=board.Collision.WorldBounds.center;
            Vector3 axis=Quaternion.Euler(0,attempted*45,0)*Vector3.forward;
            Quaternion facing=Quaternion.LookRotation(axis)*Quaternion.Euler(-16,0,0);
            Vector3 marker=centre+axis*(20+attempted%3*6)+Vector3.up;
            float endpoint=battle.WreckContact.PoseObstruction(pose,marker,facing,false);
            if(endpoint>.0004f){attempted++;return;}
            ready++;
            Set("attackFacingKey",key);Set("attackFacing",facing);
            Set("attackStartRoot",centre-facing*Vector3.Scale(pose.body,shark.transform.lossyScale));
            Set("attackStartRotation",facing);Set("biteRouteStartAge",-1.5f);
            Set("hasIntendedBiteRoot",false);Set("hasIntendedTailRoot",false);
            Set("attackFacingSampleTime",film.CinematicTime-1);Set("attackRouteOffset",Vector3.zero);Set("facingSearchCursor",0);
            int before=film.FacingBudgetYields;var watch=System.Diagnostics.Stopwatch.StartNew();
            type.GetMethod("ChooseAttackFacing",Flags).Invoke(film,new object[]{key,marker,false,false,0f});watch.Stop();
            rows.Add(new{attempted,board=board.name,parts=board.Collision.Parts.Length,endpoint,
                elapsedMilliseconds=watch.Elapsed.TotalMilliseconds,yields=film.FacingBudgetYields-before,
                route=(float)type.GetField("attackRouteCost",Flags).GetValue(film),
                rootUnchanged=Vector3.Distance(shark.transform.position,actual)<.00001f});
            attempted++;
        }
        catch(Exception ex){File.WriteAllText(".codex/animation-continuation-20261014/route-budget-v191-error.txt",ex.ToString());attempted=32;}
        finally
        {
            foreach(var item in saved)item.Key.SetValue(film,item.Value);
            ((IDictionary)type.GetField("attackPlanningPoses",Flags).GetValue(film)).Remove(key);
            shark.SampleCinematicAction(action,seconds);shark.transform.SetPositionAndRotation(actual,heading);
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
            typeof(Story1SharkWreckContact).GetMethod("UpdateVolumes",Flags).Invoke(battle.WreckContact,null);
            File.WriteAllText(".codex/animation-continuation-20261014/route-budget-v191.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                native=true,hypotheticalOnly=true,woodMoved=false,actorRootMoved=false,restoredFieldsAndBones=true,
                attempted,ready,rows,performanceAcceptance=false},Newtonsoft.Json.Formatting.Indented));
            if(attempted>=24)Destroy(gameObject);
        }
    }
}
public static class ProbeRouteBudgetV191
{
    public static object Attach()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Separate numerical fixture required");
        var go=new GameObject("Hypothetical native budget probe v191");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<RouteBudgetProbeV191>().benchmark=benchmark;return new{attached=true,hypotheticalOnly=true};
    }
}
