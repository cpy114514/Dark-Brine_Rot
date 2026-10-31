using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[DefaultExecutionOrder(3000)]
public sealed class MarshmallowStrideReview : MonoBehaviour
{
    public readonly List<object> frames=new List<object>();
    public readonly List<bool> leads=new List<bool>();
    public int captured, lastStride;
    int firstCapturedStride;
    public float maxSplit, maxLandingFootPitch;
    public float maxAnkleFlex,maxAnkleTwist,maxToeFlex;
    public float maxTorsoLean;
    public int reachFrames,correctReachFrames;
    public float leadEarly,leadLate,trailEarly,trailLate,maxLandingPhaseJump;
    public int earlySamples,lateSamples,landingTransitions;
    public bool exclusive=true;
    public int footSamples,footPenetrations,footCorrections;
    public float worstFootPenetration,worstBeforeCorrection;
    Vector3 leftSole,rightSole;
    Vector3 fastLeft,fastRight;
    long denseTicks,fastTicks;
    int timingSamples;
    long legacyTicks;
    int legacySamples;
    public float maxCacheError;
    public double DenseSoleMilliseconds => timingSamples==0 ? 0 : denseTicks*1000.0/System.Diagnostics.Stopwatch.Frequency/timingSamples;
    public double FastSoleMilliseconds => timingSamples==0 ? 0 : fastTicks*1000.0/System.Diagnostics.Stopwatch.Frequency/timingSamples;
    public double LegacySoleMilliseconds => legacySamples==0 ? 0 : legacyTicks*1000.0/System.Diagnostics.Stopwatch.Frequency/legacySamples;
    ThirdPersonPlayerController motor;MarshmallowSpacewalk gait;Camera camera;
    readonly HashSet<string> shots=new HashSet<string>();
    const string Folder="Temp/MarshmallowSpacewalk/FootRepair/Review";
    HumanPoseHandler poseReader;
    HumanPose measuredPose;
    bool previousGrounded,previousStriding;
    float previousPhase;
    int previousStride;
    void Start()
    {
        Directory.CreateDirectory(Folder);motor=GetComponent<ThirdPersonPlayerController>();gait=GetComponent<MarshmallowSpacewalk>();
        var go=new GameObject("Temporary spacewalk side camera");camera=go.AddComponent<Camera>();camera.enabled=false;
        camera.CopyFrom(Camera.main);camera.enabled=false;camera.fieldOfView=45;camera.nearClipPlane=.1f;
        poseReader=new HumanPoseHandler(motor.CharacterAnimator.avatar,motor.CharacterAnimator.transform);
    }
    void LateUpdate()
    {
        if(!gait || !motor.CharacterAnimator || Time.deltaTime<=0)return;
        if(gait.Strides!=lastStride){lastStride=gait.Strides;leads.Add(gait.LeftLeading);}
        var a=motor.CharacterAnimator;
        var left=a.GetBoneTransform(HumanBodyBones.LeftFoot);var right=a.GetBoneTransform(HumanBodyBones.RightFoot);
        var traversal=GetComponent<MarshmallowTraversal>();
        var experience=GetComponent<MarshmallowIslandExperience>();
        string phase=PlaySecondIslandMarshmallow.Status;
        if((phase.StartsWith("walk-") || phase.StartsWith("steer-") || phase.StartsWith("sprint-") ||
            phase.StartsWith("release-") || phase.StartsWith("manual-jump-") || phase.StartsWith("stop-")) &&
            !motor.IsDodging && !(GetComponent<Mavis.SahurAttack>()?.IsCombatMotionActive ?? false))
        {
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            SahurSupportPose.MeasureSoles(a,ref leftSole,ref rightSole);
            denseTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
            started=System.Diagnostics.Stopwatch.GetTimestamp();
            SahurSupportPose.MeasureSolesFast(a,ref fastLeft,ref fastRight);
            fastTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;timingSamples++;
            if(timingSamples%15==0)
            {
                Vector3 nativeLeft=leftSole,nativeRight=rightSole;
                started=System.Diagnostics.Stopwatch.GetTimestamp();
                SahurSupportPose.MeasureSolesLegacy(a,ref nativeLeft,ref nativeRight);
                legacyTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;legacySamples++;
                maxCacheError=Mathf.Max(maxCacheError,Mathf.Abs(left.TransformPoint(nativeLeft).y-left.TransformPoint(leftSole).y),
                    Mathf.Abs(right.TransformPoint(nativeRight).y-right.TransformPoint(rightSole).y));
            }
            foreach(var point in new[]{left.TransformPoint(leftSole),right.TransformPoint(rightSole)})
                if(experience.TryVisualSurface(point,out float ground))
                {footSamples++;float penetration=ground-point.y;worstFootPenetration=Mathf.Max(worstFootPenetration,penetration);if(penetration>.08f)footPenetrations++;}
            footCorrections+=traversal.CorrectedFeet;
            worstBeforeCorrection=Mathf.Max(worstBeforeCorrection,traversal.FootPenetrationBefore);
        }
        float split=Mathf.Abs(Vector3.Dot(left.position-right.position,transform.forward));
        float signedSplit=Vector3.Dot(left.position-right.position,transform.forward);
        var leadFoot=gait.LeftLeading?left:right;
        var leadToe=a.GetBoneTransform(gait.LeftLeading?HumanBodyBones.LeftToes:HumanBodyBones.RightToes);
        var soleDirection=leadToe?leadToe.position-leadFoot.position:transform.forward;
        float toePitch=Mathf.Atan2(Vector3.Dot(soleDirection,Vector3.up),Vector3.Dot(soleDirection,transform.forward))*Mathf.Rad2Deg;
        if(gait.Striding && gait.Weight>.99f)maxSplit=Mathf.Max(maxSplit,split);
        if(gait.Striding && gait.Weight>.99f && gait.Phase>.8f)maxLandingFootPitch=Mathf.Max(maxLandingFootPitch,Mathf.Abs(toePitch));
        if(gait.Weight>.99f)
        {
            var leftThigh=a.GetBoneTransform(HumanBodyBones.LeftUpperLeg);var leftKnee=a.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            var rightThigh=a.GetBoneTransform(HumanBodyBones.RightUpperLeg);var rightKnee=a.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            float leftBend=Vector3.Angle(leftKnee.position-leftThigh.position,left.position-leftKnee.position);
            float rightBend=Vector3.Angle(rightKnee.position-rightThigh.position,right.position-rightKnee.position);
            float leadBend=gait.LeftLeading?leftBend:rightBend,trailBend=gait.LeftLeading?rightBend:leftBend;
            if(gait.Striding && !GetComponent<CharacterController>().isGrounded && gait.Phase>=.14f && gait.Phase<.28f)
            {leadEarly+=leadBend;trailEarly+=trailBend;earlySamples++;}
            if(gait.Striding && !GetComponent<CharacterController>().isGrounded && gait.Phase>=.70f && gait.Phase<.82f)
            {leadLate+=leadBend;trailLate+=trailBend;lateSamples++;}
            var head=a.GetBoneTransform(HumanBodyBones.Head);var hips=a.GetBoneTransform(HumanBodyBones.Hips);
            maxTorsoLean=Mathf.Max(maxTorsoLean,Vector3.Angle(head.position-hips.position,Vector3.up));
            if(gait.Phase>.8f && gait.Striding)
            {
                reachFrames++;
                if((gait.LeftLeading?signedSplit:-signedSplit)>.2f)correctReachFrames++;
            }
            poseReader.GetHumanPose(ref measuredPose);
            foreach(string side in new[]{"Left","Right"})
            {
                maxAnkleFlex=Mathf.Max(maxAnkleFlex,Mathf.Abs(measuredPose.muscles[Array.IndexOf(HumanTrait.MuscleName,side+" Foot Up-Down")]));
                maxAnkleTwist=Mathf.Max(maxAnkleTwist,Mathf.Abs(measuredPose.muscles[Array.IndexOf(HumanTrait.MuscleName,side+" Foot Twist In-Out")]));
                maxToeFlex=Mathf.Max(maxToeFlex,Mathf.Abs(measuredPose.muscles[Array.IndexOf(HumanTrait.MuscleName,side+" Toes Up-Down")]));
            }
        }
        string state=PlaySecondIslandMarshmallow.Status;
        bool grounded=GetComponent<CharacterController>().isGrounded;
        if(!previousGrounded && grounded && previousStriding && gait.Striding && previousStride==gait.Strides)
        {maxLandingPhaseJump=Mathf.Max(maxLandingPhaseJump,Mathf.Abs(gait.Phase-previousPhase));landingTransitions++;}
        previousGrounded=grounded;previousStriding=gait.Striding;previousPhase=gait.Phase;previousStride=gait.Strides;
        if((state.StartsWith("manual-jump") || state.StartsWith("air-flip")) && gait.Weight>.001f)exclusive=false;
        frames.Add(new {state,stride=gait.Strides,left=gait.LeftLeading,gait.Phase,gait.Weight,split,toePitch,
            hips=a.GetBoneTransform(HumanBodyBones.Hips).localPosition.ToString(),vertical=motor.VerticalSpeed});
        if(!gait.Striding || gait.Weight<.99f)return;
        if(firstCapturedStride==0)firstCapturedStride=gait.Strides;
        if(gait.Strides>firstCapturedStride+1)return;
        int bin=gait.Phase<.32f?0:gait.Phase<.58f?1:gait.Phase<.8f?2:3;
        string key=gait.Strides+"-"+bin;
        if(!shots.Add(key))return;
        var focus=transform.position+Vector3.up*(motor.LowestFootWorldOffset+2.5f);
        camera.transform.position=focus+transform.right*11+Vector3.up*3.5f-transform.forward*2.8f;
        camera.transform.LookAt(focus);
        var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,720,24);
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes(Folder+"/stride-"+key+".png",image.EncodeToPNG());captured++;
            camera.transform.position=focus+transform.forward*10+Vector3.up*2.5f;camera.transform.LookAt(focus);
            camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes(Folder+"/front-"+key+".png",image.EncodeToPNG());captured++;
        }
        finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Destroy(image);}
    }
    void OnDestroy(){poseReader?.Dispose();if(camera)Destroy(camera.gameObject);}
}
public static class VerifyMarshmallowSpacewalk
{
    public static string Status="idle";
    public static object Summary;
    static Task pending;
    public static object Start(){if(pending!=null && !pending.IsCompleted)return "Already running";pending=Run();return "Spacewalk native playthrough started";}
    static async Task Run()
    {
        MarshmallowStrideReview probe=null;
        try
        {
            Status="playing";Summary=null;
            var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();probe=p.gameObject.AddComponent<MarshmallowStrideReview>();
            PlaySecondIslandMarshmallow.Start();
            while(PlaySecondIslandMarshmallow.Status!="completed" && !PlaySecondIslandMarshmallow.Status.StartsWith("failed"))await Task.Delay(30);
            if(PlaySecondIslandMarshmallow.Status!="completed")throw new Exception(PlaySecondIslandMarshmallow.Status);
            bool alternating=probe.leads.Count>=6 && probe.leads.Skip(1).Where((lead,i)=>lead==probe.leads[i]).Count()==0;
            File.WriteAllText("Temp/MarshmallowSpacewalk/FootRepair/Review/diagnostics.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {
                alternating,probe.exclusive,probe.maxSplit,probe.maxLandingFootPitch,probe.maxAnkleFlex,probe.maxAnkleTwist,
                probe.maxToeFlex,probe.maxTorsoLean,probe.reachFrames,probe.correctReachFrames,probe.captured,
                probe.earlySamples,probe.lateSamples,probe.leadEarly,probe.leadLate,probe.trailEarly,probe.trailLate,probe.maxLandingPhaseJump,probe.landingTransitions,
                probe.footSamples,probe.footPenetrations,probe.footCorrections,probe.worstFootPenetration,probe.worstBeforeCorrection,
                probe.DenseSoleMilliseconds,probe.FastSoleMilliseconds,probe.LegacySoleMilliseconds,probe.maxCacheError},Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText("Temp/MarshmallowSpacewalk/FootRepair/Review/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(probe.frames,Newtonsoft.Json.Formatting.Indented));
            if(!alternating || !probe.exclusive || probe.maxSplit<1f || probe.captured<12 ||
                probe.maxAnkleFlex>.4f || probe.maxAnkleTwist>.2f || probe.maxToeFlex>.2f ||
                probe.maxLandingFootPitch>45 || probe.maxTorsoLean>20 || probe.reachFrames==0 || probe.correctReachFrames<probe.reachFrames*.85f ||
                probe.earlySamples==0 || probe.lateSamples==0 || probe.landingTransitions==0 || probe.maxLandingPhaseJump>.12f ||
                probe.leadEarly/probe.earlySamples < probe.leadLate/probe.lateSamples+5 ||
                probe.trailLate/probe.lateSamples < probe.trailEarly/probe.earlySamples+5)
                throw new Exception("Stride review failed: alternating="+alternating+",exclusive="+probe.exclusive+",split="+probe.maxSplit+",captures="+probe.captured);
            Summary=new{passed=true,alternating,manualJumpAndFlipPreserved=probe.exclusive,strides=probe.leads.Count,
                maxForeAftFootSeparation=probe.maxSplit,landingFootPitch=probe.maxLandingFootPitch,
                probe.maxAnkleFlex,probe.maxAnkleTwist,probe.maxToeFlex,probe.maxTorsoLean,
                probe.reachFrames,probe.correctReachFrames,screenshots=probe.captured,
                leadEarlyDegrees=probe.leadEarly/probe.earlySamples,leadLateDegrees=probe.leadLate/probe.lateSamples,
                trailEarlyDegrees=probe.trailEarly/probe.earlySamples,trailLateDegrees=probe.trailLate/probe.lateSamples,
                probe.maxLandingPhaseJump,probe.landingTransitions};
            File.WriteAllText("Temp/MarshmallowSpacewalk/FootRepair/Review/summary.json",Newtonsoft.Json.JsonConvert.SerializeObject(Summary,Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText("Temp/MarshmallowSpacewalk/FootRepair/Review/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(probe.frames,Newtonsoft.Json.Formatting.Indented));
            Status="completed";
        }
        catch(Exception e){Status="failed: "+e;File.WriteAllText("Temp/MarshmallowSpacewalk/FootRepair/Review/error.txt",e.ToString());}
        finally{if(probe)UnityEngine.Object.Destroy(probe);}
    }
}
