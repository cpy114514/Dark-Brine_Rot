using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(32800)]
public sealed class CapsuleBoundsObserverV094:MonoBehaviour
{
    public string output;
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly float[] cues={.9f,7f,9f,14.5f,18f};
    readonly List<object> rows=new List<object>();
    int next,pairs,rejected,hits,mismatches;
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null?battle.Finale:null;
        if(film==null || next>=cues.Length || film.CinematicTime<cues[next])return;
        var contact=battle.WreckContact;var type=typeof(Story1SharkWreckContact);
        type.GetMethod("SnapshotPlanningShapes",flags).Invoke(contact,null);
        var volumes=(Collider[])type.GetField("volumes",flags).GetValue(contact);
        var parts=(IList)type.GetField("planningParts",flags).GetValue(contact);
        var queryType=type.GetNestedType("CapsuleBoundsQuery",BindingFlags.NonPublic);
        var constructor=queryType.GetConstructor(new[]{typeof(Collider),typeof(Vector3),typeof(Quaternion)});
        var test=queryType.GetMethod("MayTouch");
        int startPairs=pairs,startRejected=rejected,startHits=hits;
        foreach(var volume in volumes)
        foreach(var offset in new[]{Vector3.zero,Vector3.down*.5f,Vector3.up*.5f,Vector3.left*2,Vector3.right*2,Vector3.forward*5,Vector3.back*5})
        foreach(var angle in new[]{Quaternion.identity,Quaternion.Euler(30,45,0),Quaternion.Euler(-25,-70,0)})
        {
            Vector3 position=volume.transform.position+offset;Quaternion heading=angle*volume.transform.rotation;
            object query=constructor.Invoke(new object[]{volume,position,heading});
            // An enclosing sphere stays valid for every test rotation. It does
            // not share the proposed segment/AABB rejection implementation.
            float reach=volume.bounds.extents.magnitude+(volume.bounds.center-volume.transform.position).magnitude+.01f;
            foreach(object part in parts)
            {
                var partType=part.GetType();Bounds bounds=(Bounds)partType.GetField("bounds").GetValue(part);
                if(bounds.SqrDistance(position)>reach*reach)continue;
                var mesh=(MeshCollider)partType.GetField("collider").GetValue(part);
                var at=(Vector3)partType.GetField("position").GetValue(part);
                var rotation=(Quaternion)partType.GetField("rotation").GetValue(part);
                bool allowed=(bool)test.Invoke(query,new object[]{bounds});
                bool actual=Physics.ComputePenetration(volume,position,heading,mesh,at,rotation,out _,out float depth);
                pairs++;if(!allowed)rejected++;if(actual && depth>0)hits++;
                if(!allowed && actual && depth>0)
                {
                    mismatches++;rows.Add(new{clock=film.CinematicTime,volume=volume.name,mesh=mesh.name,depth,offset=offset.ToString(),heading=heading.eulerAngles.ToString()});
                }
            }
        }
        rows.Add(new{clock=film.CinematicTime,pairs=pairs-startPairs,rejected=rejected-startRejected,hits=hits-startHits});next++;
        if(next>=cues.Length)Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{nativeGeometry=true,readOnly=true,performanceEvidence=false,
            poses=next,pairs,rejected,hits,mismatches,passed=next==cues.Length && mismatches==0 && rejected>0 && hits>0,rows},Newtonsoft.Json.Formatting.Indented));
    }
}
public static class VerifyCapsuleBoundsV094
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Native capsule bounds equivalence v094");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<CapsuleBoundsObserverV094>().output=output;return new{started=true,readOnly=true};
    }
}
