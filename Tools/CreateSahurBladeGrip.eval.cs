if(Application.isPlaying)throw new System.Exception("Stop Play Mode before authoring the grip mesh");
var player=UnityEngine.Object.FindFirstObjectByType<Mavis.PlayerHealth>();
var renderer=player.GetComponentInChildren<SkinnedMeshRenderer>();
var source=renderer.sharedMesh;
var vertices=source.vertices;
var closed=(Vector3[])vertices.Clone();
var weights=source.boneWeights;
var touched=new System.Collections.Generic.List<int>();
foreach(var name in new[]{"RightHand","LeftHand"})
{
    int bone=System.Array.FindIndex(renderer.bones,t=>t.name==name);
    if(bone<0)throw new System.Exception("Missing hand bone "+name);
    float direction=name=="RightHand"?-1f:1f;
    var toHand=source.bindposes[bone];var fromHand=toHand.inverse;
    for(int i=0;i<vertices.Length;i++)
    {
        var w=weights[i];
        float influence=(w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)+
            (w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
        if(influence<.5f)continue;
        var point=toHand.MultiplyPoint3x4(vertices[i]);var curled=point;
        // Four fingers bend from the knuckles toward the palm; the wrist stays unchanged.
        if(point.z>-.057f && point.y>.108f)
        {
            const float radius=.031f;
            float angle=Mathf.Min(3.5f,(point.y-.108f)/radius);
            curled.y=.108f+Mathf.Sin(angle)*radius;
            curled.x+=direction*(1-Mathf.Cos(angle))*radius;
        }
        // The thumb folds over the handle from the side of the palm.
        else if(point.z<-.057f && point.y>.07f)
        {
            const float radius=.024f;
            float angle=Mathf.Min(2.25f,(-point.z-.057f)/radius);
            curled.z=-.057f-Mathf.Sin(angle)*radius;
            curled.x+=direction*(1-Mathf.Cos(angle))*radius;
        }
        if((curled-point).sqrMagnitude<.00000001f)continue;
        closed[i]=Vector3.Lerp(vertices[i],fromHand.MultiplyPoint3x4(curled),influence);
        touched.Add(i);
    }
}
var deformed=UnityEngine.Object.Instantiate(source);deformed.vertices=closed;
deformed.RecalculateNormals();deformed.RecalculateTangents();
var normals=source.normals;var newNormals=deformed.normals;
var tangents=source.tangents;var newTangents=deformed.tangents;
var delta=new Vector3[vertices.Length];var dn=new Vector3[vertices.Length];var dt=new Vector3[vertices.Length];
foreach(int i in touched){delta[i]=closed[i]-vertices[i];dn[i]=newNormals[i]-normals[i];dt[i]=(Vector3)(newTangents[i]-tangents[i]);}
var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Sahur Twin Blade Hand Grip";
mesh.AddBlendShapeFrame("Capri handle grip",100,delta,dn,dt);
UnityEngine.Object.DestroyImmediate(deformed);
var path="Assets/Game/Prefabs/Weapons/CapriTwinBlades/SahurTwinBladeHandGrip.asset";
var existing=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
if(existing){UnityEditor.EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;UnityEditor.EditorUtility.SetDirty(mesh);}
else UnityEditor.AssetDatabase.CreateAsset(mesh,path);
var prefabPath="Assets/Game/Prefabs/Weapons/CapriTwinBlades/CapriTwinBlades.prefab";
var root=UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try{root.GetComponent<Mavis.CapriTwinBladeEquipment>().handGripMesh=mesh;UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}
finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
UnityEditor.AssetDatabase.SaveAssets();
return "Created hand-only grip blend shape; altered "+touched.Count+" vertices, body and wrists unchanged.";
