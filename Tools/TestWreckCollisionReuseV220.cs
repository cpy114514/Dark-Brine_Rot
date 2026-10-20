using System;
using System.Linq;
using System.IO;
using UnityEngine;
public static class TestWreckCollisionReuseV220
{
    public static object Run()
    {
        var source=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        var fragment=source.details.Concat(source.decks).First(s=>s.splinters!=null&&s.splinters.Length>1);
        var go=new GameObject("Temporary compound reuse test",typeof(Rigidbody),typeof(Story1WreckMeshCollision));go.SetActive(false);
        var replacement=UnityEngine.Object.Instantiate(fragment.mesh);
        try
        {
            var collision=go.GetComponent<Story1WreckMeshCollision>();collision.Initialize(fragment.mesh,fragment.splinters);
            var original=collision.Parts;var meshes=original.Select(p=>p.sharedMesh).ToArray();
            var positions=original.Select(p=>p.transform.localPosition).ToArray();var scales=original.Select(p=>p.transform.localScale).ToArray();
            collision.SetEnabled(false);collision.Initialize(replacement,fragment.splinters);
            if(!ReferenceEquals(original,collision.Parts))throw new Exception("Equivalent compound rebuilt native shapes");
            for(int i=0;i<original.Length;i++)
                if(!original[i].enabled||original[i].sharedMesh!=meshes[i]||original[i].transform.localPosition!=positions[i]||original[i].transform.localScale!=scales[i])throw new Exception("Compound geometry changed");
            var changed=fragment.splinters.ToArray();collision.Initialize(replacement,changed);
            if(ReferenceEquals(original,collision.Parts))throw new Exception("New shard inputs were not invalidated");
            collision.Initialize(fragment.mesh);var single=collision.Parts;collision.Initialize(replacement);
            if(ReferenceEquals(single,collision.Parts))throw new Exception("Single render-mesh input was not invalidated");
            var result=new{passed=true,compoundParts=original.Length,checks="Native shape identity, meshes, local poses/scales, enable restoration; new shard-array and single-mesh invalidation"};
            File.WriteAllText(".codex/encounter-redesign-20261017/revision-v220/compound-reuse-test.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(replacement);}
    }
}
