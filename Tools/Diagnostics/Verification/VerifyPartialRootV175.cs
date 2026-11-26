using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public static class VerifyPartialRootV175
{
    public static object Start()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Own native benchmark required");
        var holder=new GameObject("Native fractional root queries v175");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<PartialRootObserverV175>().benchmark=benchmark;
        return new{started=true,woodMoved=false,posesTemporarilySampledAndRestored=true,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(32960)]
public sealed class PartialRootObserverV175:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<object> rows=new List<object>();
    int cases,oldAccepted,newAccepted,partial,mismatches;float worstResidual,worstExcess;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null || battle.WreckContact==null || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<1)return;
        var contact=battle.WreckContact;var type=contact.GetType();
        T Field<T>(string name)=>(T)type.GetField(name,Private).GetValue(contact);
        var shark=Field<TralaleroSwimAnimator>("shark");var bones=Field<Transform[]>("poseBones");
        var previous=Field<Vector3>("previousRoot");var previousRotation=Field<Quaternion>("previousRotation");
        var retainedPosition=Field<Vector3[]>("retainedPositions");var retainedRotation=Field<Quaternion[]>("retainedRotations");
        var positions=new Vector3[bones.Length];var rotations=new Quaternion[bones.Length];
        for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}
        Vector3 actual=shark.transform.position;Quaternion actualRotation=shark.transform.rotation;
        var update=(Action)Delegate.CreateDelegate(typeof(Action),contact,type.GetMethod("UpdateVolumes",Private));
        var clear=(Func<Vector3,float,bool>)Delegate.CreateDelegate(typeof(Func<Vector3,float,bool>),contact,type.GetMethod("ClearAtTolerance",Private));
        var current=(Func<Vector3,Quaternion,int,bool>)Delegate.CreateDelegate(typeof(Func<Vector3,Quaternion,int,bool>),contact,type.GetMethod("TryPoseAdvance",Private));
        void Restore()
        {
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
            shark.transform.SetPositionAndRotation(actual,actualRotation);update();
        }
        bool Original(Vector3 desired,Quaternion heading,int rootSteps)
        {
            float angularTravel=0;
            for(int i=0;i<bones.Length;i++)angularTravel+=Quaternion.Angle(retainedRotation[i],rotations[i]);
            int steps=Mathf.Clamp(Mathf.Max(rootSteps,Mathf.CeilToInt(angularTravel/7)),1,32);
            for(int attempt=0;attempt<4;attempt++)
            {
                float fraction=1-attempt*.25f;bool valid=true;
                for(int step=1;step<=steps;step++)
                {
                    float time=(float)step/steps,weight=time*fraction;
                    for(int i=0;i<bones.Length;i++){bones[i].localPosition=Vector3.Lerp(retainedPosition[i],positions[i],weight);bones[i].localRotation=Quaternion.Slerp(retainedRotation[i],rotations[i],weight);}
                    shark.transform.SetPositionAndRotation(Vector3.Lerp(previous,desired,time),Quaternion.Slerp(previousRotation,heading,time));update();
                    if(!clear(Vector3.zero,.00099f)){valid=false;break;}
                }
                if(valid)return true;
            }
            Restore();return false;
        }
        try
        {
            for(int n=0;n<4 && cases<256;n++,cases++)
            {
                float angle=(cases%16)*22.5f*Mathf.Deg2Rad;
                float distance=new[]{.45f,.8f,1.5f,3f,5f,8f,12f,18f}[(cases/16)%8];
                Vector3 delta=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*distance;
                if(cases>=128 && battle.Boards.Count>0)
                    delta=Vector3.ClampMagnitude(battle.Boards[cases%battle.Boards.Count].Collision.WorldBounds.center-previous,distance);
                Vector3 desired=previous+delta;Quaternion heading=previousRotation;
                int steps=Mathf.Clamp(Mathf.CeilToInt(delta.magnitude/.45f),1,32);
                bool old=Original(desired,heading,steps);Vector3 oldRoot=shark.transform.position;
                var oldPositions=new Vector3[bones.Length];var oldRotations=new Quaternion[bones.Length];
                if(old)for(int i=0;i<bones.Length;i++){oldPositions[i]=bones[i].localPosition;oldRotations[i]=bones[i].localRotation;}
                Restore();bool accepted=current(desired,heading,steps);
                if(old)oldAccepted++;if(accepted)newAccepted++;
                float residual=0,excess=0;
                if(old && !accepted)mismatches++;
                if(old && accepted)
                {
                    if(Vector3.Distance(oldRoot,shark.transform.position)>1e-6f)mismatches++;
                    for(int i=0;i<bones.Length;i++)if(Vector3.Distance(oldPositions[i],bones[i].localPosition)>1e-6f || Quaternion.Angle(oldRotations[i],bones[i].localRotation)>.05f){mismatches++;break;}
                }
                if(accepted)
                {
                    residual=contact.CurrentPenetration();excess=Vector3.Distance(previous,shark.transform.position)-delta.magnitude;
                    worstResidual=Mathf.Max(worstResidual,residual);worstExcess=Mathf.Max(worstExcess,excess);
                    if(residual>.0011f || excess>.0001f)mismatches++;
                    if(!old)partial++;
                }
                rows.Add(new{cases,old,accepted,residual,excess,requested=delta.magnitude,actual=Vector3.Distance(previous,shark.transform.position)});
                Restore();
            }
        }
        finally{Restore();}
        if(cases>=256)
        {
            File.WriteAllText(".codex/opening-optimization-20261012/partial-root-v175.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                native=true,originalReleasedMeshes=true,woodMoved=false,temporaryPoseSamplingRestored=true,performanceEvidence=false,
                cases,oldAccepted,newAccepted,partial,mismatches,worstResidual,worstExcess,
                passed=partial>0 && mismatches==0,rows},Newtonsoft.Json.Formatting.Indented));Destroy(gameObject);
        }
    }
}
