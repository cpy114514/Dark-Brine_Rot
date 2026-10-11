using System;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class AuditSurfboardsV018
{
    public static object Inspect()
    {
        var preview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/First story/Story1.unity");
        try
        {
            var sequence=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Story1SharkCollisionSequence>(true)).Single();
            Vector3 scale=sequence.ship.transform.lossyScale*.01f;
            var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
            var original=asset.decks.Concat(asset.hulls).Concat(asset.details).Select(f=>new{name=f.name,size=Vector3.Scale(f.sourceSize,scale)})
                .Where(f=>Story1WreckPlank.SurfboardShape(f.size)).Select(f=>new{f.name,size=f.size.ToString()}).ToArray();
            var shards=asset.decks.Concat(asset.hulls).Concat(asset.details).SelectMany(f=>f.splinters.Select((s,i)=>new{f.name,index=i,size=Vector3.Scale(Vector3.Scale(f.sourceSize,scale),s.localSize)}))
                .Where(f=>Story1WreckPlank.SurfboardShape(f.size)).Select(f=>new{f.name,f.index,size=f.size.ToString()}).ToArray();
            var result=new{scale=scale.ToString(),originalSurfboardShapes=original,realFractureSurfboardShapes=shards};
            Directory.CreateDirectory(".codex/encounter-v018");File.WriteAllText(".codex/encounter-v018/surfboard-shape-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
    }
}
