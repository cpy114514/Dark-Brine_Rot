using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class ReviewCommonV008
{
    public static async Task<object> Review(string selected="")
    {
        if(!Application.isPlaying)throw new Exception("Play mode required.");
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),Vector3.up*500,Quaternion.identity);
        actor.AddComponent<GameSaveExcluded>();var animator=actor.GetComponent<ThirdPersonPlayerController>().CharacterAnimator;
        foreach(var behaviour in actor.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
        actor.GetComponent<CharacterController>().enabled=false;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.speed=0;
        var cameraGo=new GameObject("Common action review camera");var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.19f);
        var lightGo=new GameObject("Action review light");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35,-35,0);
        var graph=PlayableGraph.Create("Common v008 review");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var mixer=AnimationMixerPlayable.Create(graph,1);AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(mixer);graph.Play();
        string folder=".codex/encounter-v008-common-review";Directory.CreateDirectory(folder);int count=0;
        try
        {
            foreach(string name in new[]{"PLAYER_Walk_v006","PLAYER_Run_v006","PLAYER_JumpStart_v006","PLAYER_JumpAir_v006","PLAYER_JumpLand_v006","PLAYER_Climb_v006","PLAYER_Climb_v009","PLAYER_Climb_v010","PLAYER_BoardDrive_v006","PLAYER_BoardLeft_v006","PLAYER_SwimFast_v007","CH1_SahurParry_v008","CH1_SahurSlash_v006","PLAYER_Fall_v006"})
            {
                if(selected!="" && selected!=name)continue;
                string version=name.Substring(name.LastIndexOf('_')+1);var clip=Resources.LoadAll<AnimationClip>("Encounter/"+version+"/"+name+"/"+name).Single(c=>!c.name.StartsWith("__preview__"));
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetSpeed(0);playable.SetApplyFootIK(false);mixer.ConnectInput(0,playable,0);mixer.SetInputWeight(0,1);
                for(int i=0;i<=12;i++)
                {
                    float time=Mathf.Min(clip.length-.00001f,clip.length*i/12);playable.SetTime(time);graph.Evaluate(0);await Task.Delay(20);graph.Evaluate(0);
                    var root=new GameObject("Actual sampled skin");var meshes=new System.Collections.Generic.List<Mesh>();Bounds bounds=new Bounds();bool first=true;
                    foreach(var source in actor.GetComponentsInChildren<Renderer>())
                    {
                        if(!source.enabled || !source.gameObject.activeInHierarchy)continue;Mesh mesh;
                        if(source is SkinnedMeshRenderer skin){mesh=new Mesh();skin.BakeMesh(mesh,true);meshes.Add(mesh);}
                        else{var filter=source.GetComponent<MeshFilter>();if(filter==null)continue;mesh=filter.sharedMesh;}
                        var part=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));part.layer=31;part.transform.SetParent(root.transform);part.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);part.transform.localScale=source.transform.lossyScale;
                        part.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=part.GetComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials;if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                    }
                    camera.orthographic=true;camera.orthographicSize=Mathf.Max(4,bounds.extents.magnitude*1.05f);
                    foreach(var view in new[]{("front",Vector3.forward),("side",Vector3.right),("three-quarter",new Vector3(1,0,1).normalized)})
                    {
                        camera.transform.position=bounds.center+(view.Item2+Vector3.up*.1f)*25;camera.transform.LookAt(bounds.center);
                        var target=new RenderTexture(480,480,24);var image=new Texture2D(480,480,TextureFormat.RGB24,false);var active=RenderTexture.active;
                        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,480,480),0,0);image.Apply();File.WriteAllBytes(folder+"/"+name+"-"+view.Item1+"-"+i.ToString("D2")+".png",image.EncodeToPNG());count++;}
                        finally{camera.targetTexture=null;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
                    }
                    UnityEngine.Object.DestroyImmediate(root);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
                }
                mixer.DisconnectInput(0);graph.DestroyPlayable(playable);
            }
            return new{count,folder};
        }
        finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);}
    }
}

