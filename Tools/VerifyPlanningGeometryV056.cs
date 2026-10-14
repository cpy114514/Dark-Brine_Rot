using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyPlanningGeometryV056
{
    static Task pending;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    struct Query{public Story1SharkWreckContact.AnatomyPose pose;public Vector3 marker;public Quaternion rotation;public bool tail;}
    public static object Start(){pending=Run();return new{started=true};}
    static async Task Run()
    {
        const string folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012";
        float oldScale=Time.timeScale;GameObject legacyRoot=null;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Story1");while(!load.isDone)await Task.Delay(20);
            await Task.Delay(250);var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(120);sequence.BeginImpact();
            float deadline=Time.realtimeSinceStartup+30;while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting && Time.realtimeSinceStartup<deadline)await Task.Delay(20);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("No actual released wreck");
            Time.timeScale=0;var contact=battle.WreckContact;var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
            var type=contact.GetType();var fin=(MeshCollider)type.GetField("planningFin",Private).GetValue(contact);
            var cost=(Func<Collider,Vector3,Quaternion,Bounds,float>)Delegate.CreateDelegate(typeof(Func<Collider,Vector3,Quaternion,Bounds,float>),contact,type.GetMethod("PlanningCost",Private));
            var finBounds=(Func<Vector3,Quaternion,Vector3,Bounds>)Delegate.CreateDelegate(typeof(Func<Vector3,Quaternion,Vector3,Bounds>),contact,type.GetMethod("FinBounds",Private));
            legacyRoot=new GameObject("Isolated exact legacy planning reference");legacyRoot.transform.SetParent(contact.transform,false);
            var legacy=Enumerable.Range(0,4).Select(i=>{var go=new GameObject("reference "+i);go.transform.SetParent(legacyRoot.transform,false);go.transform.localPosition=Vector3.down*(15000+i*20);var c=go.AddComponent<CapsuleCollider>();c.direction=2;c.isTrigger=true;return c;}).ToArray();
            var omitted=new System.Collections.Generic.List<object>();
            float Reference(Query q,bool brute=false)
            {
                Vector3 scale=shark.transform.lossyScale,at=q.marker-q.rotation*Vector3.Scale(q.tail?q.pose.tailMarker:q.pose.nose,scale);
                Vector3 World(Vector3 p)=>at+q.rotation*Vector3.Scale(p,scale);
                Vector3 nose=World(q.pose.nose),head=World(q.pose.head),body=World(q.pose.body),tailBase=World(q.pose.tailBase),tailTip=World(q.pose.tailTip);
                float radius=Mathf.Clamp(shark.RootMeshBounds.extents.x*Mathf.Abs(scale.x)*.48f,1.2f,2),headRadius=radius*.45f,result=0;
                float Grade(Collider volume,Vector3 position,Quaternion rotation,Bounds bounds)
                {
                    if(!brute)return cost(volume,position,rotation,bounds);
                    float value=0;
                    foreach(var shape in battle.Boards.Where(p=>!p.IsBroken && Time.time>=p.SharkContactAfter).Select(p=>p.Collision).Concat(battle.WreckDetails.Select(p=>p.Collision)))
                    {
                        if(shape==null || !shape.Active)continue;float depth=0;
                        for(int j=0;j<shape.Parts.Length;j++)if(Physics.ComputePenetration(volume,position,rotation,shape.Parts[j],shape.PartPosition(j),shape.PartRotation(j),out _,out float d))
                        {
                            depth=Mathf.Max(depth,d);
                            if(omitted.Count<40 && !shape.PartBounds(j).Intersects(bounds))omitted.Add(new{shape=shape.name,part=j,volume=volume.name,d,query=bounds.ToString("F5"),partBounds=shape.PartBounds(j).ToString("F5"),nativeBounds=shape.Parts[j].bounds.ToString("F5"),position=position.ToString("F5"),rotation=rotation.eulerAngles.ToString("F3")});
                        }
                        value+=depth*depth;
                    }
                    return value;
                }
                void Segment(int i,Vector3 a,Vector3 b,float r,float length)
                {
                    Vector3 axis=b-a,position=(a+b)*.5f;Quaternion rotation=axis.sqrMagnitude>.00001f?Quaternion.LookRotation(axis):Quaternion.identity;
                    float height=length+2*r;if(legacy[i].radius!=r)legacy[i].radius=r;if(legacy[i].height!=height)legacy[i].height=height;
                    Vector3 half=axis*.5f;var bounds=new Bounds(position,2*(new Vector3(Mathf.Abs(half.x),Mathf.Abs(half.y),Mathf.Abs(half.z))+Vector3.one*r));
                    result+=Grade(legacy[i],position,rotation,bounds);
                }
                Segment(0,nose+(head-nose).normalized*headRadius,head,headRadius,Mathf.Abs(Vector3.Scale(q.pose.head-q.pose.nose,scale).magnitude-headRadius));
                Segment(1,head,body,radius,Vector3.Scale(q.pose.body-q.pose.head,scale).magnitude);
                Segment(2,body,tailBase,radius*.7f,Vector3.Scale(q.pose.tailBase-q.pose.body,scale).magnitude);
                Segment(3,tailBase,tailTip,radius*.32f,Vector3.Scale(q.pose.tailTip-q.pose.tailBase,scale).magnitude);
                Vector3 finAt=World(q.pose.finPosition);Quaternion finRotation=q.rotation*q.pose.finRotation;
                return result+Grade(fin,finAt,finRotation,finBounds(finAt,finRotation,fin.transform.lossyScale));
            }
            var queries=new System.Collections.Generic.List<Query>();
            foreach(string action in new[]{"Tail_Strike","Bite_Lunge","Ship_Smash"})foreach(float seconds in new[]{0f,.2f,.4f,.65f,1f})
            {
                shark.SampleCinematicAction(action,seconds);var pose=contact.CaptureAnatomy();
                foreach(bool tail in new[]{false,true})foreach(float yaw in new[]{-75f,-25f,25f,75f})
                    queries.Add(new Query{pose=pose,tail=tail,marker=(tail?shark.TailWorldPoint:shark.NoseWorldPoint)+Vector3.right*(yaw/15),rotation=Quaternion.Euler(12,yaw,0)});
            }
            float maxLegacyDifference=0,maxBruteDifference=0;int nonzero=0;
            foreach(var q in queries)
            {
                float cached=contact.PoseObstruction(q.pose,q.marker,q.rotation,q.tail),previous=Reference(q),brute=Reference(q,true);
                maxLegacyDifference=Mathf.Max(maxLegacyDifference,Mathf.Abs(cached-previous));maxBruteDifference=Mathf.Max(maxBruteDifference,Mathf.Abs(cached-brute));if(brute>0)nonzero++;
            }
            // Interleave five authored poses as the production route planner does.
            var working=Enumerable.Range(0,5).Select(i=>queries[i*8]).ToArray();
            double Measure(bool cached)
            {
                var clock=Stopwatch.StartNew();for(int i=0;i<1000;i++){var q=working[i%working.Length];if(cached)contact.PoseObstruction(q.pose,q.marker,q.rotation,q.tail);else Reference(q);}return clock.Elapsed.TotalMilliseconds;
            }
            Measure(true);Measure(false);var cachedTimes=new double[7];var previousTimes=new double[7];
            for(int i=0;i<7;i++){previousTimes[i]=Measure(false);cachedTimes[i]=Measure(true);}
            bool passed=maxLegacyDifference<.0001f && maxBruteDifference<.0001f && nonzero>0;
            var result=new{passed,queries=queries.Count,nonzero,maxLegacyDifference,maxBruteDifference,medianLegacyMs=previousTimes.OrderBy(t=>t).ElementAt(3),medianRuntimeMs=cachedTimes.OrderBy(t=>t).ElementAt(3),previousTimes,cachedTimes,omitted};
            File.WriteAllText(folder+"/planning-native-bounds-v056.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(!passed)throw new Exception("Cached native anatomy differs from actual native wreck queries");
        }
        catch(Exception ex){File.WriteAllText(folder+"/planning-cache-validation-v056-error.txt",ex.ToString());}
        finally{if(legacyRoot!=null)UnityEngine.Object.Destroy(legacyRoot);Time.timeScale=oldScale;}
    }
}
