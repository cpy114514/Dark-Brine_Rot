using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Samples the actual Sahur mesh once in the editor, not during gameplay.
public static class BakeSecondIslandGetUpContacts
{
    [Serializable] sealed class Pose { public Vector3[] points; public bool[] valid; }
    [Serializable] sealed class Data { public float sourceScale=3f; public Pose[] poses; }
    public static object Bake()
    {
        var source=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        if (!source) throw new Exception("Open the second island with Sahur before baking.");
        var scene=EditorSceneManager.NewPreviewScene();
        var root=UnityEngine.Object.Instantiate(source.gameObject);
        SceneManager.MoveGameObjectToScene(root,scene);
        var mesh=new Mesh();
        try
        {
            foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            var capsule=root.GetComponent<CharacterController>(); if(capsule)capsule.enabled=false;
            root.transform.SetPositionAndRotation(new Vector3(0,500,0),Quaternion.identity);
            var rootAnimator=root.GetComponent<Animator>();
            var animator=root.transform.Find("Pbr Sahur Visual").GetComponent<Animator>();
            animator.enabled=false;animator.avatar=rootAnimator.avatar;animator.runtimeAnimatorController=rootAnimator.runtimeAnimatorController;
            rootAnimator.enabled=false; animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.enabled=true;animator.Rebind();animator.Update(0);
            var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(x=>x.enabled).ToArray();
            const int side=12, cells=side*side; const float cell=1.5f;
            var data=new Data{sourceScale=Mathf.Abs(root.transform.lossyScale.y),poses=new Pose[61]};
            int maxCells=0;
            for(int k=0;k<=60;k++)
            {
                float phase=k/60f;animator.SetFloat("ArrivalGetUpPhase",phase);
                animator.Play("Base Layer.Shore Get Up",0,phase);animator.Update(0);
                var pose=new Pose{points=new Vector3[cells],valid=new bool[cells]};
                foreach(var skin in skins)
                {
                    skin.BakeMesh(mesh,false);
                    foreach(var v in mesh.vertices)
                    {
                        Vector3 p=skin.transform.TransformPoint(v)-root.transform.position;
                        int x=Mathf.FloorToInt(p.x/cell+side*.5f), z=Mathf.FloorToInt(p.z/cell+side*.5f);
                        if(x<0||x>=side||z<0||z>=side)throw new Exception("Contact grid too small: "+p);
                        int index=z*side+x;
                        if(!pose.valid[index]||p.y<pose.points[index].y){pose.valid[index]=true;pose.points[index]=p;}
                    }
                }
                maxCells=Mathf.Max(maxCells,pose.valid.Count(x=>x));data.poses[k]=pose;
            }
            string path="Assets/Resources/SecondIslandAwakening/GetUpContacts.json";
            File.WriteAllText(path,JsonUtility.ToJson(data));AssetDatabase.ImportAsset(path);
            return new{path,phases=data.poses.Length,maxCells,scale=data.sourceScale,skins=skins.Length};
        }
        finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
