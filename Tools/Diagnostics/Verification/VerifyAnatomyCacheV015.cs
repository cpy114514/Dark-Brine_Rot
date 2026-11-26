using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
public static class VerifyAnatomyCacheV015
{
    public static object Verify()
    {
        var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        var shark=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();var contact=battle.WreckContact;
        var flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic;
        var update=typeof(Story1SharkWreckContact).GetMethod("UpdateVolumes",flags);
        var overlap=typeof(Story1SharkWreckContact).GetMethod("OverlapShape",flags);
        var volumes=(CapsuleCollider[])typeof(Story1SharkWreckContact).GetField("volumes",flags).GetValue(contact);
        var shapes=battle.Boards.Where(b=>!b.IsBroken && Time.time>=b.SharkContactAfter).Select(b=>b.Collision).Concat(battle.WreckDetails.Select(d=>d.Collision)).ToArray();
        Vector3 position=shark.transform.position;Quaternion rotation=shark.transform.rotation;string action=shark.CinematicAction;float seconds=shark.CinematicActionSeconds;
        float maxError=0,maxGeometryError=0;bool costsEquivalent=true;int samples=0;var rows=new System.Collections.Generic.List<object>();var watch=System.Diagnostics.Stopwatch.StartNew();
        try
        {
            foreach(bool tail in new[]{false,true})foreach(float time in new[]{0f,.3f,.65f})
            {
                shark.SampleCinematicAction(tail ? "Tail_Strike" : "Bite_Lunge",time);var pose=contact.CaptureAnatomy();
                foreach(float pitch in new[]{-65f,-16f,25f,65f})
                {
                    Vector3 marker=battle.Boards[0].BoardingPoint(position)+Vector3.up*3;
                    Quaternion facing=rotation*Quaternion.Euler(pitch,40,0);
                    float cached=contact.PoseObstruction(pose,marker,facing,tail);
                    Vector3 local=shark.transform.InverseTransformPoint(tail ? shark.TailWorldPoint : shark.NoseWorldPoint);
                    shark.transform.SetPositionAndRotation(marker-facing*Vector3.Scale(local,shark.transform.lossyScale),facing);update.Invoke(contact,null);
                    Vector3 root=marker-facing*Vector3.Scale(tail ? pose.tailMarker : pose.nose,shark.transform.lossyScale);
                    Vector3 World(Vector3 point)=>root+facing*Vector3.Scale(point,shark.transform.lossyScale);
                    var fresh=contact.CaptureAnatomy();
                    float geometryError=Mathf.Max(Vector3.Distance(World(pose.head),shark.transform.TransformPoint(fresh.head)),Vector3.Distance(World(pose.tailTip),shark.transform.TransformPoint(fresh.tailTip)));
                    maxGeometryError=Mathf.Max(maxGeometryError,geometryError);
                    float native=0;
                    foreach(var volume in volumes)foreach(var shape in shapes)
                    {
                        object[] args={volume,shape,Vector3.zero,Vector3.zero,0f};
                        if((bool)overlap.Invoke(null,args)){float depth=(float)args[4];native+=depth*depth;}
                    }
                    maxError=Mathf.Max(maxError,Mathf.Abs(native-cached));samples++;
                    costsEquivalent&=Mathf.Abs(native-cached)<=.001f+.001f*Mathf.Max(native,cached);
                    rows.Add(new{tail,time,pitch,cached,native,geometryError});
                    shark.transform.SetPositionAndRotation(position,rotation);update.Invoke(contact,null);
                }
            }
            var result=new{samples,maximumNativeCostError=maxError,maxGeometryError,elapsedMs=watch.ElapsedMilliseconds,rows};
            System.IO.File.WriteAllText(".codex/encounter-v015/anatomy-cache-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(maxGeometryError>.001f || !costsEquivalent)throw new Exception("Cached pose differs from the native rig beyond world-space float precision.");
            return result;
        }
        finally{shark.transform.SetPositionAndRotation(position,rotation);shark.SampleCinematicAction(action,seconds);update.Invoke(contact,null);}
    }
}
