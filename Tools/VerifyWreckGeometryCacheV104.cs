using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class VerifyWreckGeometryCacheV104
{
    public static object Check()
    {
        if(Application.isPlaying)throw new Exception("Stopped original Editor required");
        var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;
        try
        {
            var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
            root=new GameObject("Isolated original wreck cache regression");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            root.transform.position=new Vector3(1000,100,1000);root.transform.localScale=new Vector3(20,1,12);
            var shape=root.AddComponent<Story1WreckMeshCollision>();shape.Initialize(asset.decks[0].mesh);
            Bounds first=shape.WorldBounds;
            root.transform.rotation=Quaternion.Euler(0,.1f,0);
            Bounds cached=shape.WorldBounds,expected=default;bool initialized=false;int staleRotations=0;
            for(int i=0;i<shape.Parts.Length;i++)
            {
                var part=shape.Parts[i];Bounds local=part.sharedMesh.bounds;
                if(!shape.PartRotation(i).Equals(part.transform.rotation))staleRotations++;
                for(int corner=0;corner<8;corner++)
                {
                    Vector3 point=part.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                    if(!initialized){expected=new Bounds(point,Vector3.zero);initialized=true;}else expected.Encapsulate(point);
                }
            }
            float error=Mathf.Max(Vector3.Distance(cached.min,expected.min),Vector3.Distance(cached.max,expected.max));
            var result=new{native=true,isolatedPreview=true,sameRenderedFrame=true,parts=shape.Parts.Length,
                engineApproximateRotationsEqual=Quaternion.identity==root.transform.rotation,exactRotationsEqual=Quaternion.identity.Equals(root.transform.rotation),
                staleRotations,errorMetres=error,geometryActuallyMoved=Vector3.Distance(first.size,expected.size),pass=staleRotations==0 && error<.0005f};
            string label=staleRotations>0?"before":"after";
            File.WriteAllText(".codex/opening-optimization-20261012/geometry-cache-v104-"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
