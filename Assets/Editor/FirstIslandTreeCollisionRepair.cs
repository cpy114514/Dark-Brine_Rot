using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Repair missing trunk collision in the first-island environment without colliding with grass or leaves.</summary>
public static class FirstIslandTreeCollisionRepair
{
    const string Path="Assets/Scenes/First Island/Environment.unity";
    public static string Apply()
    {
        if(Application.isPlaying)throw new Exception("Stop play before repairing the environment.");
        if(SceneManager.GetSceneByPath(Path).isLoaded)throw new Exception("The first-island environment is already open; preserve the existing scene state.");
        var scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
        try
        {
            if(scene.isDirty)throw new Exception("The opened environment has unsaved changes; not modifying it.");
            var added=new List<Collider>();int simple=0,meshes=0;
            foreach(var root in scene.GetRootGameObjects())
            {
                if(!root.name.ToLowerInvariant().Contains("tree"))continue;
                foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(!mf.sharedMesh || mf.GetComponent<Collider>())continue;
                    var renderer=mf.GetComponent<Renderer>();if(!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
                    if(mf.name=="tree_small_02_LOD0" || mf.name=="island_tree_01_LOD0")
                    {
                        // The imported LOD0 meshes contain 1.3–1.8 million
                        // vertices each. A bounded trunk capsule avoids adding
                        // six expensive whole-canopy physics meshes.
                        var c=Undo.AddComponent<CapsuleCollider>(mf.gameObject);
                        c.center=new Vector3(0,.8f,0);c.radius=.2f;c.height=1.6f;c.isTrigger=false;added.Add(c);simple++;
                    }
                    else if(mf.name=="Object_4" && mf.sharedMesh.vertexCount<5000)
                    {
                        var c=Undo.AddComponent<MeshCollider>(mf.gameObject);
                        c.sharedMesh=mf.sharedMesh;c.convex=false;c.isTrigger=false;added.Add(c);meshes++;
                    }
                }
            }
            Physics.SyncTransforms();
            foreach(var c in added)
            {
                if(!c || !c.enabled || c.isTrigger || c.bounds.size.sqrMagnitude<.001f)throw new Exception("An added trunk collider was invalid.");
                bool hit=false;var b=c.bounds;
                float y=b.min.y+Mathf.Min(b.size.y*.18f,8f);
                for(int i=0;i<12 && !hit;i++)
                {
                    float a=i*Mathf.PI*2/12;
                    Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    var origin=new Vector3(b.center.x,y,b.center.z)+dir*(Mathf.Max(b.extents.x,b.extents.z)+3f);
                    hit=c.Raycast(new Ray(origin,-dir),out _,2*Mathf.Max(b.extents.x,b.extents.z)+6f);
                }
                if(!hit)throw new Exception("A trunk collider failed a side sweep: "+c.name);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("First-island environment could not be saved.");
            return "Added "+simple+" lightweight trunk capsules and "+meshes+" low-poly trunk mesh colliders; all passed side raycasts.";
        }
        finally{EditorSceneManager.CloseScene(scene,true);}
    }
}
