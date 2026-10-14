using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(32650)]
public sealed class VisibilityObserverV096:MonoBehaviour
{
    public string output;
    readonly List<object> samples=new List<object>();
    float next;
    void LateUpdate()
    {
        var battle=FindFirstObjectByType<Story1WreckBattle>();var film=battle!=null?battle.Finale:null;
        if(film==null || film.CinematicTime<0 || film.CinematicTime<next)return;
        next=film.CinematicTime+.1f;
        var sequence=battle.GetComponent<Story1SharkCollisionSequence>();var camera=sequence.storyCamera;
        var animator=sequence.sahur.CharacterAnimator;var ocean=FindFirstObjectByType<OceanWorld>();
        var surface=OceanSurfaceSampler.Capture(ocean,camera,Time.time);
        float Clearance(Vector3 point)
        {
            float minimum=float.PositiveInfinity;int count=Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(camera.transform.position,point)/2),6,18);
            for(int i=1;i<count;i++){var along=Vector3.Lerp(camera.transform.position,point,(float)i/count);minimum=Mathf.Min(minimum,along.y-surface.Height(along));}
            return minimum;
        }
        var head=animator.GetBoneTransform(HumanBodyBones.Head).position;var chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        samples.Add(new{clock=film.CinematicTime,headWaterClearance=Clearance(head),chestWaterClearance=Clearance(chest),
            headFreeboard=head.y-surface.Height(head),raised=typeof(Story1WreckFinale).GetField("cameraRaised",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(film),
            camera=camera.transform.position.ToString("F3"),fov=camera.fieldOfView});
        if(film.IsComplete || battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Destroy(gameObject);
    }
    void OnDestroy(){if(output!=null)File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,readOnly=true,observesWaterSightlineOnly=true,artAcceptance=false,samples},Newtonsoft.Json.Formatting.Indented));}
}
public static class ObserveVisibilityV096
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Original Play Mode required");
        var holder=new GameObject("Read-only camera water sightline v096");UnityEngine.Object.DontDestroyOnLoad(holder);
        holder.AddComponent<VisibilityObserverV096>().output=output;return new{started=true,readOnly=true};
    }
}
