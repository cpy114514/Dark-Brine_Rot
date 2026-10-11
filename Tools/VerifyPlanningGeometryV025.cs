using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;

public static class VerifyPlanningGeometryV025
{
    static float LegacyOverlap(CapsuleCollider volume,Story1WreckMeshCollision shape)
    {
        if(shape==null || !shape.Active)return 0;
        Vector3 scale=volume.transform.lossyScale;
        float reach=volume.height*.5f*Mathf.Max(scale.x,Mathf.Max(scale.y,scale.z));
        Vector3 centre=volume.transform.TransformPoint(volume.center);float depth=0;
        if(shape.WorldBounds.SqrDistance(centre)>reach*reach)return 0;
        for(int i=0;i<shape.Parts.Length;i++)
        {
            if(shape.PartBounds(i).SqrDistance(centre)>reach*reach)continue;
            if(Physics.ComputePenetration(volume,volume.transform.position,volume.transform.rotation,
                shape.Parts[i],shape.PartPosition(i),shape.PartRotation(i),out _,out var penetration))depth=Mathf.Max(depth,penetration);
        }
        return depth*depth;
    }
    static void Segment(CapsuleCollider volume,Vector3 a,Vector3 b,float radius)
    {
        Vector3 along=b-a;volume.transform.SetPositionAndRotation((a+b)*.5f,along.sqrMagnitude>.00001f ? Quaternion.LookRotation(along) : Quaternion.identity);
        volume.radius=radius;volume.height=along.magnitude+2*radius;
    }
    static float Legacy(Story1WreckBattle battle,TralaleroSwimAnimator shark,Story1SharkWreckContact.AnatomyPose pose,
        Vector3 marker,Quaternion rotation,bool tail,CapsuleCollider[] volumes)
    {
        Vector3 scale=shark.transform.lossyScale;
        Vector3 at=marker-rotation*Vector3.Scale(tail ? pose.tailMarker : pose.nose,scale);
        Vector3 World(Vector3 local)=>at+rotation*Vector3.Scale(local,scale);
        float radius=Mathf.Clamp(shark.RootMeshBounds.extents.x*Mathf.Abs(scale.x)*.48f,1.2f,2),headRadius=radius*.45f;
        Vector3 nose=World(pose.nose),head=World(pose.head),body=World(pose.body),basePoint=World(pose.tailBase),tip=World(pose.tailTip);
        Segment(volumes[0],nose+(head-nose).normalized*headRadius,head,headRadius);
        Segment(volumes[1],head,body,radius);Segment(volumes[2],body,basePoint,radius*.7f);Segment(volumes[3],basePoint,tip,radius*.32f);
        float cost=0;
        foreach(var volume in volumes)
        {
            foreach(var board in battle.Boards)
                if(board!=null && !board.IsBroken && board.gameObject.activeInHierarchy && board.Deck!=null && board.Deck.enabled && Time.time>=board.SharkContactAfter)
                    cost+=LegacyOverlap(volume,board.Collision);
            foreach(var piece in battle.WreckDetails)cost+=LegacyOverlap(volume,piece.Collision);
        }
        return cost;
    }
    public static async Task<object> Verify()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        float oldTime=Time.timeScale;GameObject fixture=null;
        try
        {
            GameSaveManager.CancelPendingContinue();var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(20);await Task.Delay(250);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=sequence.GetComponent<Story1WreckBattle>();
            var hp=sequence.sahur.GetComponent<PlayerHealth>();hp.currentHealth=hp.maxHealth;hp.GrantProtection(200);
            battle.minimumFightSeconds=battle.maximumFightSeconds=120;sequence.BeginImpact();
            for(int i=0;i<500 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(20);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("No real released wreck");
            Time.timeScale=0;var contact=battle.WreckContact;var shark=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var live=(CapsuleCollider[])typeof(Story1SharkWreckContact).GetField("volumes",flags).GetValue(contact);
            var previous=live.Select(c=>(c.transform.position,c.transform.rotation,c.radius,c.height)).ToArray();
            fixture=new GameObject("Owned planning geometry reference");var volumes=new CapsuleCollider[4];
            for(int i=0;i<4;i++){var go=new GameObject("Legacy reference "+i);go.transform.SetParent(fixture.transform,false);volumes[i]=go.AddComponent<CapsuleCollider>();volumes[i].direction=2;volumes[i].isTrigger=true;}
            double oldMilliseconds=0,newMilliseconds=0;float maximumError=0,maximumAggregateDepthError=0;int samples=0,blocked=0,clear=0;
            var rows=new System.Collections.Generic.List<object>();
            var markers=battle.Boards.Where(b=>!b.IsBroken).Take(4).Select(b=>b.BoardingPoint(b.transform.position)+Vector3.up*1.8f)
                .Concat(new[]{sequence.sahur.transform.position+Vector3.up*80}).ToArray();
            foreach(var action in new[]{("Bite_Lunge",.52f,false),("Tail_Strike",.65f,true),("Swim",.2f,false)})
            {
                shark.SampleCinematicAction(action.Item1,action.Item2);var pose=contact.CaptureAnatomy();
                foreach(var marker in markers)foreach(float yaw in new[]{0f,60f,120f,180f,240f,300f})
                {
                    Quaternion heading=Quaternion.Euler(yaw%120==0 ? -25 : 25,yaw,0);
                    var watch=System.Diagnostics.Stopwatch.StartNew();float expected=Legacy(battle,shark,pose,marker,heading,action.Item3,volumes);watch.Stop();oldMilliseconds+=watch.Elapsed.TotalMilliseconds;
                    watch.Restart();float actual=contact.PoseObstruction(pose,marker,heading,action.Item3);watch.Stop();newMilliseconds+=watch.Elapsed.TotalMilliseconds;
                    float error=Mathf.Abs(expected-actual),depthError=Mathf.Abs(Mathf.Sqrt(expected)-Mathf.Sqrt(actual));
                    maximumError=Mathf.Max(maximumError,error);maximumAggregateDepthError=Mathf.Max(maximumAggregateDepthError,depthError);samples++;if(expected>0)blocked++;else clear++;
                    // Native collision arithmetic has world-space float roundoff.
                    // Compare physical depth in metres and require identical
                    // route-clear classification, rather than an arbitrary
                    // absolute tolerance on a sum of squared metre values.
                    if(depthError>.0005f || (expected<=.0004f)!=(actual<=.0004f))rows.Add(new{action=action.Item1,marker=marker.ToString(),yaw,expected,actual,error,depthError});
                }
            }
            bool liveUnchanged=true;
            for(int i=0;i<live.Length;i++)liveUnchanged&=live[i].transform.position==previous[i].position && live[i].transform.rotation==previous[i].rotation && live[i].radius==previous[i].radius && live[i].height==previous[i].height;
            var result=new{samples,blocked,clear,maximumSquaredPenetrationCostError=maximumError,maximumAggregateDepthError,routeClearClassificationUnchanged=rows.Count==0,legacyMilliseconds=oldMilliseconds,planningMilliseconds=newMilliseconds,
                liveAnatomyUntouched=liveUnchanged,failures=rows,passed=rows.Count==0 && blocked>0 && clear>0 && liveUnchanged};
            Directory.CreateDirectory(".codex/encounter-v025");File.WriteAllText(".codex/encounter-v025/planning-geometry-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(!result.passed)throw new Exception("Planning geometry changed actual collision costs: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
        }
        finally
        {
            if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);Time.timeScale=oldTime;
            var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(p!=null && p.GetComponent<GameSaveExcluded>()==null)p.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);
        }
    }
}
