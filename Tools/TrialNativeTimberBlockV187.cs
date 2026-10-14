using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Diagnostic only: run against freely released original timber. No asset,
// actor, timber pose, layer matrix, damage threshold or gameplay code edits.
[DefaultExecutionOrder(33500)]
public sealed class NativeTimberBlockTrialV187:MonoBehaviour
{
    public OpeningBenchmark benchmark;
    Collider[] source,proxies;
    Rigidbody[] bodies;
    GameObject root;
    int fixedFrames,contacts,created;
    bool finished;
    float largestCentreLag;
    void LateUpdate()
    {
        var battle=benchmark!=null?benchmark.Battle:null;
        if(battle==null)return;
        if(source==null && battle.CurrentPhase==Story1WreckBattle.Phase.Fighting && battle.WreckContact!=null)
        {
            source=(Collider[])typeof(Story1SharkWreckContact).GetField("volumes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle.WreckContact);
            root=new GameObject("Diagnostic native shark wood response");
            bodies=new Rigidbody[source.Length];proxies=new Collider[source.Length];
            // Only original native timber receives this diagnostic's contacts.
            var excluded=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(c=>c.GetComponentInParent<Story1WreckMeshCollision>()==null).ToArray();
            for(int i=0;i<source.Length;i++)
            {
                var go=new GameObject("Native timber response "+i);go.transform.SetParent(root.transform,false);
                go.transform.SetPositionAndRotation(source[i].transform.position,source[i].transform.rotation);
                go.transform.localScale=source[i].transform.lossyScale;
                if(source[i] is CapsuleCollider capsule)
                {
                    var copy=go.AddComponent<CapsuleCollider>();copy.direction=capsule.direction;copy.radius=capsule.radius;copy.height=capsule.height;copy.center=capsule.center;proxies[i]=copy;
                }
                else if(source[i] is MeshCollider mesh)
                {
                    var copy=go.AddComponent<MeshCollider>();copy.sharedMesh=mesh.sharedMesh;copy.convex=true;proxies[i]=copy;
                }
                else throw new Exception("Unexpected original anatomy");
                proxies[i].contactOffset=.008f;
                var body=go.AddComponent<Rigidbody>();body.mass=1500;body.useGravity=false;body.isKinematic=true;
                body.interpolation=RigidbodyInterpolation.None;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                bodies[i]=body;go.AddComponent<NativeTimberContactCountV187>().owner=this;
                foreach(var collider in excluded)if(collider!=null)Physics.IgnoreCollision(proxies[i],collider,true);
                for(int earlier=0;earlier<i;earlier++)Physics.IgnoreCollision(proxies[i],proxies[earlier],true);
                created++;
            }
        }
        if(source!=null && battle.CurrentPhase==Story1WreckBattle.Phase.Defeat)Finish();
    }
    void FixedUpdate()
    {
        if(source==null)return;fixedFrames++;
        for(int i=0;i<source.Length;i++)
        {
            if(source[i]==null || bodies[i]==null)continue;
            largestCentreLag=Mathf.Max(largestCentreLag,Vector3.Distance(bodies[i].position,source[i].transform.position));
            if(source[i] is CapsuleCollider capsule && proxies[i] is CapsuleCollider copy)
            {copy.center=capsule.center;copy.radius=capsule.radius;copy.height=capsule.height;}
            bodies[i].MovePosition(source[i].transform.position);bodies[i].MoveRotation(source[i].transform.rotation);
        }
    }
    public void Contact(){contacts++;}
    void Finish()
    {
        if(finished)return;finished=true;
        File.WriteAllText(".codex/animation-continuation-20261014/native-block-trial-v187.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            diagnosticOnly=true,productionInstalled=false,originalAnatomy=true,woodPosesAssigned=false,
            created,fixedFrames,contacts,largestCentreLag,usesKinematicNativeContacts=true,performanceEvidence=false},Newtonsoft.Json.Formatting.Indented));
        Destroy(this);
    }
    void OnDestroy(){if(root!=null)Destroy(root);}
}
public sealed class NativeTimberContactCountV187:MonoBehaviour
{
    public NativeTimberBlockTrialV187 owner;
    void OnCollisionEnter(Collision contact){if(owner!=null)owner.Contact();}
    void OnCollisionStay(Collision contact){if(owner!=null)owner.Contact();}
}
public static class TrialNativeTimberBlockV187
{
    public static object Attach()
    {
        var benchmark=UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>();
        if(!Application.isPlaying || benchmark==null)throw new Exception("Existing native diagnostic required");
        benchmark.gameObject.AddComponent<NativeTimberBlockTrialV187>().benchmark=benchmark;
        return new{attached=true,diagnosticOnly=true,woodPosesAssigned=false};
    }
}
