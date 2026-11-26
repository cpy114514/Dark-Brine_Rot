using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public static class VerifyCurrentPlacementV183
{
    public static object Start()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Own benchmark required");
        var go=new GameObject("Native new-pose movement ball v183");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<CurrentPlacementObserverV183>().benchmark=benchmark;return new{started=true,woodMoved=false,temporaryPoseSamplingRestored=true};
    }
}
[DefaultExecutionOrder(32960)]
public sealed class CurrentPlacementObserverV183:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    delegate bool Search(float radius,out Vector3 result);
    int cases,accepted,oldMissNewClear,mismatches;float worstResidual,worstExcess;
    readonly List<object> rows=new List<object>();
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || battle.WreckContact==null || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<1)return;
        var solid=battle.WreckContact;var type=solid.GetType();
        var shark=(TralaleroSwimAnimator)type.GetField("shark",Private).GetValue(solid);
        var bones=(Transform[])type.GetField("poseBones",Private).GetValue(solid);
        var previousField=type.GetField("previousRoot",Private);Vector3 previous=(Vector3)previousField.GetValue(solid);
        Vector3 actual=shark.transform.position;Quaternion heading=shark.transform.rotation;
        string action=shark.CinematicAction;float age=shark.CinematicActionSeconds;
        var positions=new Vector3[bones.Length];var rotations=new Quaternion[bones.Length];
        for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}
        var update=(Action)Delegate.CreateDelegate(typeof(Action),solid,type.GetMethod("UpdateVolumes",Private));
        var oldSearch=(Search)Delegate.CreateDelegate(typeof(Search),solid,type.GetMethod("TryRetainedPlacement",Private));
        var newSearch=(Search)Delegate.CreateDelegate(typeof(Search),solid,type.GetMethod("TryCurrentPlacement",Private));
        try
        {
            for(int n=0;n<2 && cases<160;n++,cases++)
            {
                string future=cases%2==0 ? "Bite_Lunge" : "Tail_Strike";
                float futureTime=cases%2==0 ? .52f : .65f;
                shark.SampleCinematicAction(future,futureTime);
                var futurePositions=new Vector3[bones.Length];var futureRotations=new Quaternion[bones.Length];
                for(int i=0;i<bones.Length;i++){futurePositions[i]=bones[i].localPosition;futureRotations[i]=bones[i].localRotation;}
                float angle=(cases%16)*22.5f*Mathf.Deg2Rad;
                Vector3 centre=actual+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*((cases/16)%5+1)*1.5f;
                Vector3 raw=centre+new Vector3(Mathf.Cos(angle),0,-Mathf.Sin(angle))*3;
                previousField.SetValue(solid,centre);shark.transform.SetPositionAndRotation(raw,heading);update();
                const float radius=1.1f;
                bool old=oldSearch(radius,out var oldAt);
                for(int i=0;i<bones.Length;i++){bones[i].localPosition=futurePositions[i];bones[i].localRotation=futureRotations[i];}
                shark.transform.SetPositionAndRotation(raw,heading);update();
                bool current=newSearch(radius,out var at);float residual=0,excess=0;
                if(current)
                {
                    accepted++;if(!old)oldMissNewClear++;
                    residual=solid.CurrentPenetration();excess=Vector3.Distance(centre,at)-radius;
                    worstResidual=Mathf.Max(worstResidual,residual);worstExcess=Mathf.Max(worstExcess,excess);
                    if(residual>.00301f || excess>.0001f)mismatches++;
                    if(Quaternion.Angle(heading,shark.transform.rotation)>.05f)mismatches++;
                    for(int i=0;i<bones.Length;i++)if(Vector3.Distance(futurePositions[i],bones[i].localPosition)>1e-6f || Quaternion.Angle(futureRotations[i],bones[i].localRotation)>.05f){mismatches++;break;}
                }
                rows.Add(new{cases,future,old,current,residual,excess});
            }
        }
        finally
        {
            previousField.SetValue(solid,previous);shark.SampleCinematicAction(action,age);
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
            shark.transform.SetPositionAndRotation(actual,heading);update();
        }
        if(cases>=160)
        {
            File.WriteAllText(".codex/opening-optimization-20261012/current-placement-v183.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                native=true,originalReleasedMeshes=true,woodMoved=false,temporaryPoseSamplingRestored=true,hypotheticalOrigins=true,performanceEvidence=false,
                cases,accepted,oldMissNewClear,mismatches,worstResidual,worstExcess,passed=oldMissNewClear>0 && mismatches==0,rows},Newtonsoft.Json.Formatting.Indented));Destroy(gameObject);
        }
    }
}
