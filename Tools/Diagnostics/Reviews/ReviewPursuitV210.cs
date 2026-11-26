using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class ReviewPursuitV210
{
    static Task pending;
    public static object Start(){if(Application.isPlaying)throw new Exception("Edit preview required");pending=Run();return new{started=true};}
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    static async Task Run()
    {
        const string folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/chase-entry-20261014/native-clips-v3";Directory.CreateDirectory(folder);
        var rows=new List<object>();
        try
        {
            for(int kind=0;kind<2;kind++)
            {
                string name=kind==0?"PLAYER_PursuitSwim_v210":"CH1_SharkPursuit_v210";
                var scene=EditorSceneManager.NewPreviewScene();
                var prefab=kind==0?AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"):Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations").rigPrefab;
                var actor=UnityEngine.Object.Instantiate(prefab,Vector3.up*500,Quaternion.identity);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
                foreach(var behaviour in actor.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
                foreach(var collider in actor.GetComponentsInChildren<Collider>())collider.enabled=false;
                var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.speed=0;animator.Rebind();animator.Update(0);
                var graph=PlayableGraph.Create("Native pursuit staging review");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/AnimationStaging/"+name+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
                var pose=AnimationClipPlayable.Create(graph,clip);pose.SetSpeed(0);AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(pose);graph.Play();
                var cameraGo=new GameObject("Preview camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.19f);
                var lightGo=new GameObject("Preview light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,scene);var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35,-35,0);
                try
                {
                    for(int frame=0;frame<=6;frame++)
                    {
                        await Task.Yield();float time=clip.length*frame/6;pose.SetTime(time);graph.Evaluate(0);
                        var markers=kind==0?new[]{animator.GetBoneTransform(HumanBodyBones.Head),animator.GetBoneTransform(HumanBodyBones.LeftHand),animator.GetBoneTransform(HumanBodyBones.RightHand)}:
                            new[]{actor.GetComponentsInChildren<Transform>().First(t=>t.name=="Nose_Marker"),actor.GetComponentsInChildren<Transform>().First(t=>t.name=="Tail_Tip")};
                        rows.Add(new{name,time,markers=markers.Select(t=>new{t.name,position=V(t.position-actor.transform.position)}).ToArray(),avatar=animator.avatar.name,avatarValid=animator.avatar.isValid});
                        var meshRoot=new GameObject("Sampled native skin");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(meshRoot,scene);var baked=new List<Mesh>();Bounds bounds=default;bool first=true;
                        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var mesh=new Mesh();skin.BakeMesh(mesh,true);baked.Add(mesh);
                            var part=new GameObject("native skin",typeof(MeshFilter),typeof(MeshRenderer));part.layer=31;part.transform.SetParent(meshRoot.transform);part.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);part.transform.localScale=skin.transform.lossyScale;
                            part.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=part.GetComponent<MeshRenderer>();renderer.sharedMaterials=skin.sharedMaterials;if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                        }
                        camera.orthographic=true;camera.orthographicSize=Mathf.Max(kind==0?3:.05f,bounds.extents.magnitude*1.1f);
                        foreach(var view in new[]{("front",Vector3.forward),("side",Vector3.right),("three-quarter",new Vector3(1,.15f,1).normalized)})
                        {
                            camera.transform.position=bounds.center+view.Item2*50;camera.transform.LookAt(bounds.center);
                            var target=new RenderTexture(480,480,24);var pixels=new Texture2D(480,480,TextureFormat.RGB24,false);var active=RenderTexture.active;
                            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,480,480),0,0);File.WriteAllBytes(folder+"/"+name+"-"+view.Item1+"-"+frame+".png",pixels.EncodeToPNG());}
                            finally{camera.targetTexture=null;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
                        }
                        UnityEngine.Object.DestroyImmediate(meshRoot);foreach(var mesh in baked)UnityEngine.Object.DestroyImmediate(mesh);
                    }
                }
                finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);EditorSceneManager.ClosePreviewScene(scene);}
            }
            File.WriteAllText(folder+"/review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{stagedOnly=true,rows},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception ex){File.WriteAllText(folder+"/error.txt",ex.ToString());}
    }
}
