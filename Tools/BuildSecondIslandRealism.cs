using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildSecondIslandRealism
{
    const string Target="Assets/Resources/SecondIslandRealism";
    const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    static readonly string[] Assets={"wooden_crate_01","painted_wooden_bench","rock_moss_set_01","tree_stump_01","food_apple_01","wooden_lantern_01"};
    public static object Prefabs()
    {
        Directory.CreateDirectory(Target);AssetDatabase.Refresh();
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var id in Assets)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/SecondIslandRealism/"+id+"/"+id+".gltf");
                if(!source)throw new Exception("Model import missing: "+id);
                var clone=(GameObject)PrefabUtility.InstantiatePrefab(source,preview);
                try
                {
                    clone.name=id;
                    foreach(var collider in clone.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                    foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                        foreach(var material in renderer.sharedMaterials)if(material&&!material.shader.isSupported)throw new Exception("Unsupported material: "+id);
                    }
                    if(!PrefabUtility.SaveAsPrefabAsset(clone,Target+"/"+id+".prefab"))throw new Exception("Prefab save failed "+id);
                }
                finally{UnityEngine.Object.DestroyImmediate(clone);}
            }
            var candy=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/CandyDressing/Strawberry Swirl Lollipop.prefab");
            var lollipop=(GameObject)PrefabUtility.InstantiatePrefab(candy,preview);
            try
            {
                foreach(var c in lollipop.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                PrefabUtility.SaveAsPrefabAsset(lollipop,Target+"/Lollipop.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(lollipop);}
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        AssetDatabase.SaveAssets();return new{prefabs=Assets.Length+1};
    }
    public static object Scene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Edit Mode");
        var existing=SceneManager.GetSceneByPath(ScenePath);if(existing.IsValid()&&existing.isDirty)throw new Exception("Preserve unsaved Second Island scene");
        string backup=".codex/island2-upgrade/before/Second Island.unity";
        if(!File.Exists(backup))File.Copy(ScenePath,backup);
        bool already=existing.IsValid()&&existing.isLoaded;
        var scene=already?existing:EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(scene);
        try
        {
            var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Second Island - Natural details");if(old)UnityEngine.Object.DestroyImmediate(old);
            var root=new GameObject("Second Island - Natural details");
            var experience=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MarshmallowIslandExperience>(true)).First();
            var ground=experience.surface.GetComponent<Collider>();if(!ground)throw new Exception("Missing original island ground");
            var sites=new[]{new Vector2(-79,1715),new Vector2(70,1940),new Vector2(124,2059),new Vector2(-143,1637)};
            int placed=0;
            foreach(var center in sites)
            {
                for(int i=0;i<4;i++)
                {
                    float angle=(i*.5f+.18f)*Mathf.PI;
                    var site=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(i%2==0?19:25);
                    var bounds=ground.bounds;
                    if(!ground.Raycast(new Ray(new Vector3(site.x,bounds.max.y+10,site.y),Vector3.down),out var hit,bounds.size.y+20)||hit.normal.y<.72f)continue;
                    string id=i%2==0?"rock_moss_set_01":"tree_stump_01";
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Target+"/"+id+".prefab"),scene);
                    model.transform.SetParent(root.transform,false);model.transform.SetPositionAndRotation(hit.point,Quaternion.Euler(0,placed*137.508f,0));
                    float height=id=="tree_stump_01"?2.5f:3.3f;
                    SecondIslandProp.Fit(model.transform,Vector3.zero,new Vector3(8,height,8));
                    // Fit uses parent-local coordinates; keep the authored ground anchor.
                    var renderers=model.GetComponentsInChildren<Renderer>();
                    var box=renderers[0].bounds;foreach(var r in renderers)box.Encapsulate(r.bounds);
                    model.transform.position+=hit.point-new Vector3(box.center.x,box.min.y,box.center.z);
                    // A rock collection contains several separate meshes. Fit each
                    // mesh to the sloping ground rather than floating the whole
                    // collection on one flat bounding-box plane.
                    foreach(var renderer in renderers)
                    {
                        var contact=renderer.bounds;
                        if(ground.Raycast(new Ray(new Vector3(contact.center.x,bounds.max.y+10,contact.center.z),Vector3.down),out var localFloor,bounds.size.y+20))
                            renderer.transform.position+=Vector3.up*(localFloor.point.y-contact.min.y-.12f);
                    }
                    foreach(var mesh in model.GetComponentsInChildren<MeshFilter>())
                        if(mesh.sharedMesh){var collider=mesh.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh.sharedMesh;}
                    var lod=model.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=true;
                    lod.SetLODs(new[]{new LOD(.025f,renderers)});lod.RecalculateBounds();placed++;
                }
            }
            if(placed<8)throw new Exception("Insufficient safe ground placements: "+placed);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return new{placed,existingGameplayGroundPreserved=true};
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            if(!already)EditorSceneManager.CloseScene(scene,true);
        }
    }
}
