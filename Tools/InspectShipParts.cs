using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class InspectShipParts
{
    public static string Run()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/Props/Ship/ship_g.fbx");
        var ship=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);
        var filters=ship.GetComponentsInChildren<MeshFilter>();
        string[] names={"Object_16","Object_17","Object_19","Object_21","Object_28","Object_30"};
        var cameraObject=new GameObject("Parts inspection camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.AddComponent<Camera>(); camera.scene=scene;camera.enabled=false;camera.orthographic=true;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.23f,.27f);camera.farClipPlane=500;camera.nearClipPlane=.01f;
        var lamp=new GameObject("Inspection light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lamp,scene);
        lamp.AddComponent<Light>().type=LightType.Directional; lamp.transform.rotation=Quaternion.Euler(50,35,0);
        var rt=new RenderTexture(500,400,24);camera.targetTexture=rt;var sheet=new Texture2D(1500,800,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            for(int i=0;i<names.Length;i++)
            {
                foreach(var filter in filters)filter.GetComponent<Renderer>().enabled=filter.name==names[i];
                var renderer=filters.First(f=>f.name==names[i]).GetComponent<Renderer>();var bounds=renderer.bounds;
                camera.orthographicSize=Mathf.Max(bounds.extents.z,bounds.extents.x/1.25f)*1.15f;
                camera.transform.position=bounds.center+Vector3.up*100;
                camera.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);
                camera.Render();RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,500,400),i%3*500,(1-i/3)*400);
            }
            sheet.Apply(); string path=Path.GetFullPath(".codex/ship-wreck-blender/parts.png");File.WriteAllBytes(path,sheet.EncodeToPNG());return path;
        }
        finally {RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(sheet);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
