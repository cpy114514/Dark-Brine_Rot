using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyVoyagePatrolSteering
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        if(sequence==null || sequence.HasImpacted)throw new Exception("Need unbroken sailing phase");
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        var type=typeof(Story1SharkCollisionSequence);
        foreach(string field in new[]{"voyageTime","voyageSwimClock","breachVisibilityAt"})type.GetField(field,Private).SetValue(sequence,0f);
        foreach(string field in new[]{"voyageBreaching","breachLaunched","voyageOwnsAnimation"})type.GetField(field,Private).SetValue(sequence,false);
        type.GetProperty("VoyageBreachCount").GetSetMethod(true).Invoke(sequence,new object[]{0});
        var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();shark.ReleaseCinematicAnimation();
        type.GetMethod("InitializeVoyagePatrol",Private).Invoke(sequence,null);shark.SampleCinematicAction("Swim",0);shark.ReleaseCinematicAnimation();
        var holder=new GameObject("Native10FPS helm and shark patrol regression");
        var input=holder.AddComponent<VoyageHelmInput>();input.sequence=sequence;
        var observer=holder.AddComponent<VoyageHelmObserver>();observer.sequence=sequence;
        Time.captureFramerate=10;return new{started=true,simulationFPS=10,diagnosticInitialStagingReset=true,realInput=true,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(-1000)]
public sealed class VoyageHelmInput:MonoBehaviour
{
    public Story1SharkCollisionSequence sequence;
    Keyboard oldKeyboard,keyboard;
    void Awake(){oldKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>("Temporary voyage helm regression");}
    void Update()
    {
        if(sequence==null)return;keyboard.MakeCurrent();float time=sequence.VoyageTime;
        var held=time>=12 && time<14 ? new[]{GameInputSettings.Get(GameInputSettings.Action.Right)} :
            time>=22 && time<24 ? new[]{GameInputSettings.Get(GameInputSettings.Action.Left)} : Array.Empty<Key>();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(held));InputSystem.Update();
    }
    void OnDestroy(){if(keyboard!=null)InputSystem.RemoveDevice(keyboard);oldKeyboard?.MakeCurrent();}
}
[DefaultExecutionOrder(32000)]
public sealed class VoyageHelmObserver:MonoBehaviour
{
    public Story1SharkCollisionSequence sequence;
    readonly List<object> rows=new List<object>();
    TralaleroSwimAnimator shark;Collider[] hull;SphereCollider probe;
    bool sampled,finished,approachObserved;Vector3 lastBody;float firstHeading,maxTurn,maxStepExcess,maxNativeDepth,minBounds=float.PositiveInfinity;
    int frames,approachFrames,frozenApproachFrames,breaches;
    double began;
    void Awake(){began=Time.realtimeSinceStartupAsDouble;}
    void LateUpdate()
    {
        if(sequence==null)return;
        if(shark==null)
        {
            shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();hull=sequence.ship.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
            firstHeading=sequence.ship.transform.eulerAngles.y;
            var go=new GameObject("Disabled hypothetical shark envelope");go.transform.SetParent(transform);go.transform.position=Vector3.down*10000;
            probe=go.AddComponent<SphereCollider>();probe.isTrigger=true;probe.enabled=false;
            probe.radius=Vector3.Scale(shark.RootMeshBounds.extents,sequence.swimmer.lossyScale).magnitude;
        }
        Vector3 body=sequence.swimmer.TransformPoint(shark.RootMeshBounds.center);
        var battle=sequence.GetComponent<Story1WreckBattle>();
        if(sequence.IsPatrolling)
        {
            frames++;float allowed=Mathf.Max(sequence.maximumSwimSpeed,sequence.patrolSwimSpeed+sequence.escapePursuitSpeed)*Time.deltaTime;
            if(sampled)maxStepExcess=Mathf.Max(maxStepExcess,Vector3.ProjectOnPlane(body-lastBody,Vector3.up).magnitude-allowed);
            foreach(var c in hull)
            {
                float clearance=Mathf.Sqrt(c.bounds.SqrDistance(body))-probe.radius;minBounds=Mathf.Min(minBounds,clearance);
                if(clearance<=.01f && Physics.ComputePenetration(probe,body,Quaternion.identity,c,c.transform.position,c.transform.rotation,out _,out float depth))maxNativeDepth=Mathf.Max(maxNativeDepth,depth);
            }
            maxTurn=Mathf.Max(maxTurn,Mathf.Abs(Mathf.DeltaAngle(firstHeading,sequence.ship.transform.eulerAngles.y)));
        }
        if(!sequence.IsPatrolling && !sequence.HasImpacted)
        {
            approachObserved=true;approachFrames++;if(shark.BoneAnimator.speed<=0)frozenApproachFrames++;
        }
        breaches=sequence.VoyageBreachCount;
        rows.Add(new{time=sequence.VoyageTime,sequence.IsPatrolling,sequence.IsVoyageBreaching,sequence.VoyageBreachCount,
            body=body.ToString("F4"),shipHeading=sequence.ship.transform.eulerAngles.y,animatorSpeed=shark.BoneAnimator.speed});
        sampled=true;lastBody=body;
        if(sequence.HasImpacted || Time.realtimeSinceStartupAsDouble-began>180)Finish(battle);
    }
    void Finish(Story1WreckBattle battle)
    {
        if(finished)return;finished=true;
        const string path=".codex/shark-voyage-patrol-20261013/steering-10fps.json";
        File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{
            native=true,simulationFPS=10,diagnosticInitialStagingReset=true,realHelmInput=true,performanceEvidence=false,frames,breaches,maxTurn,maxStepExcess,
            minBounds,maxNativeDepth,approachObserved,approachFrames,frozenApproachFrames,impact=sequence.HasImpacted,phase=battle.CurrentPhase.ToString(),
            passed=maxTurn>10 && maxStepExcess<.001f && maxNativeDepth<.001f && approachFrames>5 && frozenApproachFrames==0 && sequence.HasImpacted,rows},Newtonsoft.Json.Formatting.Indented));
        Time.captureFramerate=0;EditorApplication.isPlaying=false;
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
