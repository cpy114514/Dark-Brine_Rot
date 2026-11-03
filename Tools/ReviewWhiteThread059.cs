using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Mavis;
public static class ReviewWhiteThread059
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit preview only");
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        MainQuestGuidance guide=null;RenderTexture texture=null;Texture2D pixels=null;var active=RenderTexture.active;
        const BindingFlags f=BindingFlags.Instance|BindingFlags.NonPublic;
        try
        {
            var owner=new GameObject("White thread native review",typeof(CharacterController));SceneManager.MoveGameObjectToScene(owner,scene);guide=owner.AddComponent<MainQuestGuidance>();
            typeof(MainQuestGuidance).GetMethod("OnEnable",f).Invoke(guide,null);
            var route=(Vector3[])typeof(MainQuestGuidance).GetField("route",f).GetValue(guide);
            var lengths=(float[])typeof(MainQuestGuidance).GetField("lengths",f).GetValue(guide);
            for(int i=0;i<32;i++){route[i]=new Vector3(Mathf.Sin(i*.11f)*2,0,i*.8f);if(i>0)lengths[i]=lengths[i-1]+Vector3.Distance(route[i],route[i-1]);}
            typeof(MainQuestGuidance).GetField("routeCount",f).SetValue(guide,32);typeof(MainQuestGuidance).GetField("fade",f).SetValue(guide,1f);
            typeof(MainQuestGuidance).GetMethod("Draw",f).Invoke(guide,null);
            var root=(Transform)typeof(MainQuestGuidance).GetField("root",f).GetValue(guide);root.GetComponent<MeshRenderer>().enabled=true;
            var mesh=root.GetComponent<MeshFilter>().sharedMesh;var colors=mesh.colors;
            if(colors[0].r!=1||colors[0].g!=1||colors[0].b!=1)throw new Exception("Core is not white");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);SceneManager.MoveGameObjectToScene(floor,scene);floor.transform.position=new Vector3(0,-.1f,13);floor.transform.localScale=new Vector3(5,1,5);
            var lightGO=new GameObject("Preview light",typeof(Light));SceneManager.MoveGameObjectToScene(lightGO,scene);lightGO.GetComponent<Light>().type=LightType.Directional;lightGO.transform.rotation=Quaternion.Euler(45,20,0);
            var cameraGO=new GameObject("Preview view",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraGO,scene);var camera=cameraGO.GetComponent<Camera>();camera.scene=scene;camera.backgroundColor=new Color(.08f,.12f,.15f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=58;camera.transform.SetPositionAndRotation(new Vector3(7,6,-7),Quaternion.LookRotation(new Vector3(-5,-3,18)));
            texture=new RenderTexture(1280,720,24);texture.Create();camera.targetTexture=texture;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(".codex/thread059/white-native.png",pixels.EncodeToPNG());
            var result=new{passed=true,vertices=mesh.vertexCount,coreDiameter=.056f,sideDiameter=.022f,preview="Native isolated preview; live-game camera review pending",shaderMessages=Array.ConvertAll(ShaderUtil.GetShaderMessages(root.GetComponent<MeshRenderer>().sharedMaterial.shader),m=>m.message)};
            File.WriteAllText(".codex/thread059/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            RenderTexture.active=active;if(guide)typeof(MainQuestGuidance).GetMethod("Dispose",f).Invoke(guide,new object[]{true});UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);if(texture){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
