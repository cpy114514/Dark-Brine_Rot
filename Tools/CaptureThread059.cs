using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class CaptureThread059
{
    public static object Run()
    {
        var g=UnityEngine.Object.FindFirstObjectByType<MainQuestGuidance>();
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var original=typeof(MainQuestGuidance).GetField("mesh",flags).GetValue(g) as Mesh;
        var basis=typeof(MainQuestGuidance).GetField("root",flags).GetValue(g) as Transform;
        if(!original||original.vertexCount!=1152)throw new Exception("Need populated live route");
        var mesh=UnityEngine.Object.Instantiate(original);GameObject go=null,cameraObject=null;
        RenderTexture texture=null;Texture2D pixels=null;var active=RenderTexture.active;
        try
        {
            go=new GameObject("Disposable white thread review",typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetPositionAndRotation(basis.position,basis.rotation);go.transform.localScale=basis.lossyScale;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Guidance/QuestThread");
            var properties=new MaterialPropertyBlock();properties.SetFloat("_Fade",1);properties.SetFloat("_Clock",Time.time);go.GetComponent<MeshRenderer>().SetPropertyBlock(properties);
            // Original Draw supplied a route. Rebuild its style without
            // advancing objectives, moving the player or changing the live camera.
            var points=mesh.vertices;var route=(Vector3[])typeof(MainQuestGuidance).GetField("route",flags).GetValue(g);
            Vector3 focus=Vector3.Lerp(route[5],route[Math.Min(g.RoutePointCount-1,18)],.5f)+Vector3.up*2.8f;
            cameraObject=new GameObject("Native thread review camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            if(Camera.main)camera.CopyFrom(Camera.main);camera.enabled=false;camera.fieldOfView=58;
            Vector3 offset=new Vector3(9,5,-12);camera.transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset));
            texture=new RenderTexture(1280,720,24);texture.Create();camera.targetTexture=texture;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var centres=new Vector3[3];
            for(int p=0;p<64;p++)
            {
                Vector3 middle=Vector3.zero;
                for(int s=0;s<3;s++){Vector3 c=Vector3.zero;for(int r=0;r<6;r++)c+=points[(s*64+p)*6+r];centres[s]=c/6;middle+=centres[s]/3;}
                for(int s=0;s<3;s++)for(int r=0;r<6;r++){int i=(s*64+p)*6+r;points[i]=middle+(centres[s]-middle)*.4f+(points[i]-centres[s])*(s==0?.3111111f:.2f);}
            }
            mesh.vertices=points;var colors=mesh.colors;
            for(int i=0;i<colors.Length;i++){int strand=i/(64*6);colors[i]=new Color(strand==0?1:.86f,strand==0?1:.90f,1,colors[i].a*(strand==0?1:.48f));}mesh.colors=colors;mesh.RecalculateBounds();
            camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(".codex/thread059/white-native.png",pixels.EncodeToPNG());
            var shader=go.GetComponent<MeshRenderer>().sharedMaterial.shader;var messages=ShaderUtil.GetShaderMessages(shader);
            return new{captured=true,vertices=mesh.vertexCount,shaderMessages=Array.ConvertAll(messages,m=>m.message),previewOnly=true};
        }
        finally{RenderTexture.active=active;if(cameraObject)UnityEngine.Object.DestroyImmediate(cameraObject);if(go)UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mesh);if(texture){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
