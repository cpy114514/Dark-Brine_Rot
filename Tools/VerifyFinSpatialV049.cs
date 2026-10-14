using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyFinSpatialV049
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static Task pending;
    public static object Start(){pending=Run();return new{started=true};}
    static async Task Run()
    {
        const string folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";
        float oldScale=Time.timeScale;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Story1");while(!load.isDone)await Task.Delay(20);
            await Task.Delay(300);var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(120);sequence.BeginImpact();
            float deadline=Time.realtimeSinceStartup+25;
            while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting && Time.realtimeSinceStartup<deadline)await Task.Delay(20);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("No released wreck");
            Time.timeScale=0;var shark=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();var contact=battle.WreckContact;
            var skin=(SkinnedMeshRenderer)shark.VisualRenderer;int tip=Array.FindIndex(skin.bones,b=>b.name=="Tail_Tip");
            skin.forceMatrixRecalculationPerRender=true;skin.updateWhenOffscreen=true;
            var weights=skin.sharedMesh.boneWeights;var selected=Enumerable.Range(0,weights.Length).Where(i=>{var w=weights[i];return (w.boneIndex0==tip?w.weight0:0)+(w.boneIndex1==tip?w.weight1:0)+(w.boneIndex2==tip?w.weight2:0)+(w.boneIndex3==tip?w.weight3:0)>.9999f;}).ToArray();
            var fin=(MeshCollider)contact.GetType().GetField("finVolume",Private).GetValue(contact);
            var planning=(CapsuleCollider[])contact.GetType().GetField("planningVolumes",Private).GetValue(contact);
            var planningFin=(MeshCollider)contact.GetType().GetField("planningFin",Private).GetValue(contact);
            float maxSkinError=0,maxCostDifference=0,maxScaledBakeError=0,maxDisabledDifference=0;int queries=0,nonzeroQueries=0;var baked=new Mesh();var scaledBake=new Mesh();var rows=new System.Collections.Generic.List<object>();
            foreach(string action in new[]{"Tail_Strike","Ship_Smash","Bite_Lunge"})foreach(float seconds in new[]{0f,.3f,.65f,1f})
            {
                shark.SampleCinematicAction(action,seconds);await Task.Delay(25);shark.SampleCinematicAction(action,seconds);skin.BakeMesh(baked,false);skin.BakeMesh(scaledBake,true);var vertices=baked.vertices;var scaledVertices=scaledBake.vertices;
                var finPoints=fin.sharedMesh.vertices;
                var worldWithoutScale=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                for(int i=0;i<selected.Length;i++)
                {
                    var expected=skin.bones[tip].TransformPoint(finPoints[i]);
                    // Unity 6 useScale=true compensates the renderer's scale.
                    // Apply that scale exactly once when converting to world.
                    maxSkinError=Mathf.Max(maxSkinError,Vector3.Distance(worldWithoutScale.MultiplyPoint3x4(vertices[selected[i]]),expected));
                    maxScaledBakeError=Mathf.Max(maxScaledBakeError,Vector3.Distance(skin.transform.TransformPoint(scaledVertices[selected[i]]),expected));
                    if(i==0)rows.Add(new{action,seconds,expected=expected.ToString("F6"),unscaled=worldWithoutScale.MultiplyPoint3x4(vertices[selected[i]]).ToString("F6"),scaled=skin.transform.TransformPoint(scaledVertices[selected[i]]).ToString("F6")});
                }
                var pose=contact.CaptureAnatomy();
                foreach(bool tail in new[]{false,true})foreach(float yaw in new[]{-75f,0f,75f})
                {
                    Vector3 marker=(tail?shark.TailWorldPoint:shark.NoseWorldPoint)+new Vector3(yaw/15,0,0);
                    Quaternion rotation=Quaternion.Euler(12,yaw,0);float accelerated=contact.PoseObstruction(pose,marker,rotation,tail),brute=0;
                    foreach(var volume in planning)volume.enabled=false;planningFin.enabled=false;
                    float disabledCost=contact.PoseObstruction(pose,marker,rotation,tail);
                    foreach(var volume in planning)volume.enabled=true;planningFin.enabled=true;
                    maxDisabledDifference=Mathf.Max(maxDisabledDifference,Mathf.Abs(disabledCost-accelerated));if(accelerated>0)nonzeroQueries++;
                    Vector3 scale=shark.transform.lossyScale,root=marker-rotation*Vector3.Scale(tail?pose.tailMarker:pose.nose,scale);
                    Vector3 World(Vector3 p)=>root+rotation*Vector3.Scale(p,scale);
                    Vector3 nose=World(pose.nose),head=World(pose.head),body=World(pose.body),basePoint=World(pose.tailBase),tipPoint=World(pose.tailTip);
                    float r=Mathf.Clamp(shark.RootMeshBounds.extents.x*Mathf.Abs(scale.x)*.48f,1.2f,2),headRadius=r*.45f;
                    Vector3[] a={nose+(head-nose).normalized*headRadius,head,body,basePoint},b={head,body,basePoint,tipPoint};
                    void Grade(Collider volume,Vector3 at,Quaternion facing)
                    {
                        foreach(var shape in battle.Boards.Where(p=>!p.IsBroken && Time.time>=p.SharkContactAfter).Select(p=>p.Collision).Concat(battle.WreckDetails.Select(p=>p.Collision)))
                        {
                            if(shape==null || !shape.Active)continue;float depth=0;
                            for(int i=0;i<shape.Parts.Length;i++)if(Physics.ComputePenetration(volume,at,facing,shape.Parts[i],shape.PartPosition(i),shape.PartRotation(i),out _,out float d))depth=Mathf.Max(depth,d);
                            brute+=depth*depth;
                        }
                    }
                    for(int i=0;i<4;i++){Vector3 axis=b[i]-a[i];Grade(planning[i],(a[i]+b[i])*.5f,axis.sqrMagnitude>.00001f ? Quaternion.LookRotation(axis):Quaternion.identity);}
                    Grade(planningFin,World(pose.finPosition),rotation*pose.finRotation);
                    maxCostDifference=Mathf.Max(maxCostDifference,Mathf.Abs(accelerated-brute));queries++;
                }
                await Task.Delay(1);
            }
            UnityEngine.Object.Destroy(baked);UnityEngine.Object.Destroy(scaledBake);
            var result=new{selectedVertices=selected.Length,maxSkinError,maxScaledBakeError,maxCostDifference,disabledPlanningExperiment=new{maxDisabledDifference,rejected=maxDisabledDifference>=.0001f},nonzeroQueries,queries,fin.convex,fin.isTrigger,rows,passed=maxSkinError<.001f && maxCostDifference<.0001f && nonzeroQueries>0};
            File.WriteAllText(folder+"/fin-spatial-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(!result.passed)throw new Exception("Actual fin or spatial query equivalence failed");
        }
        catch(Exception ex){File.WriteAllText(folder+"/fin-spatial-validation-error.txt",ex.ToString());}
        finally{Time.timeScale=oldScale;}
    }
}
