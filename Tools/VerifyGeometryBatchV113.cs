using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class VerifyGeometryBatchV113
{
    public static object Check()
    {
        if(Application.isPlaying)throw new Exception("Stopped original Editor required");
        var scene=EditorSceneManager.NewPreviewScene();int samples=0,parts=0;float largest=0;int mismatches=0;
        try
        {
            var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
            var read=typeof(Story1WreckMeshCollision).GetMethod("ReadWorldGeometry",BindingFlags.NonPublic|BindingFlags.Instance);
            for(int source=0;source<5;source++)
            {
                var root=new GameObject("Original compound geometry batch verification");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
                root.transform.position=new Vector3(1000,100,1000);root.transform.localScale=new Vector3(20,1,12);
                var shape=root.AddComponent<Story1WreckMeshCollision>();shape.Initialize(asset.decks[source].mesh,asset.decks[source].splinters);
                for(int pose=0;pose<5;pose++)
                {
                    root.transform.rotation=Quaternion.Euler(pose*.1f,pose*.1f,pose*.05f);
                    root.transform.position+=new Vector3(.003f,.001f,0);
                    object[] args={null,null,null,null};read.Invoke(shape,args);
                    var bounds=(Bounds[])args[1];var positions=(Vector3[])args[2];var rotations=(Quaternion[])args[3];
                    for(int i=0;i<shape.Parts.Length;i++)
                    {
                        var part=shape.Parts[i];var local=part.sharedMesh.bounds;Bounds native=default;bool first=true;
                        for(int corner=0;corner<8;corner++)
                        {
                            var point=part.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                            if(first){native=new Bounds(point,Vector3.zero);first=false;}else native.Encapsulate(point);
                        }
                        float error=Mathf.Max(Vector3.Distance(bounds[i].min,native.min),Vector3.Distance(bounds[i].max,native.max));largest=Mathf.Max(largest,error);
                        if(error>.0005f || !positions[i].Equals(part.transform.position) || !rotations[i].Equals(part.transform.rotation))mismatches++;
                        parts++;
                    }
                    samples++;
                }
                UnityEngine.Object.DestroyImmediate(root);
            }
            var result=new{nativeOriginalCompounds=true,isolatedPreview=true,sameFrameSmallPoseChanges=true,samples,parts,largestBoundaryError=largest,mismatches,pass=mismatches==0};
            File.WriteAllText(".codex/opening-optimization-20261012/geometry-batch-v113.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
