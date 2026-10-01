using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class InspectShipHullRendering
{
    public static object Run(bool verifyFixed = false)
    {
        var ship = UnityEngine.Object.FindFirstObjectByType<ShipSailingMotion>();
        UnityEngine.SceneManagement.Scene preview = default;
        Transform hull;
        if(ship == null)
        {
            preview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/First story/Story1.unity");
            ship=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShipSailingMotion>(true)).First();
            hull=ship.transform;
        }
        else hull=ship.transform;
        var renderers = hull.GetComponentsInChildren<MeshRenderer>(true);
        var materials = renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
        var culls = materials.Select(m=>m.HasProperty("_Cull") ? m.GetFloat("_Cull") : -1f).ToArray();
        var obj = new GameObject("Temporary hull inspection camera");
        var camera = obj.AddComponent<Camera>();
        if(preview.IsValid()) camera.scene=preview;
        camera.enabled=false; camera.fieldOfView=48; camera.nearClipPlane=.1f; camera.farClipPlane=3500;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.2f,.35f,.5f);
        var target=new RenderTexture(700,450,24);
        var sheet=new Texture2D(1400,900,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;
            Vector3 heading=Vector3.ProjectOnPlane(hull.right,Vector3.up).normalized;
            Vector3 side=Vector3.Cross(Vector3.up,heading);
            for(int mode=0;mode<2;mode++)
            {
                if(mode==1) foreach(var mat in materials) if(mat.HasProperty("_Cull")) mat.SetFloat("_Cull",0);
                for(int direction=0;direction<2;direction++)
                {
                    float sideDistance=verifyFixed ? 65f : 105f;
                    camera.transform.position=hull.position+side*(direction==0 ? sideDistance:-sideDistance)+
                        heading*(verifyFixed ? (mode==0 ? -55f:55f):25f)+Vector3.up*(verifyFixed ? 7f:12f);
                    camera.transform.LookAt(hull.position+Vector3.up*15);
                    camera.Render(); RenderTexture.active=target;
                    sheet.ReadPixels(new Rect(0,0,700,450),mode*700,(1-direction)*450);
                }
            }
            sheet.Apply(); File.WriteAllBytes(Path.GetFullPath(verifyFixed ? "Tools/ShipHullFixed.png":"Tools/ShipHullCullComparison.png"),sheet.EncodeToPNG());
        }
        finally
        {
            for(int i=0;i<materials.Length;i++) if(culls[i]>=0) materials[i].SetFloat("_Cull",culls[i]);
            RenderTexture.active=previous;
            UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(sheet);
        }
        var report = new { playing=Application.isPlaying, ship=hull.name, scale=hull.lossyScale.ToString(),
            renderers=renderers.Select(r=>new {r.name,r.enabled,active=r.gameObject.activeInHierarchy,
                mesh=r.GetComponent<MeshFilter>().sharedMesh.name, bounds=r.bounds.ToString(),
                materials=r.sharedMaterials.Select(m=>m ? m.name : "missing").ToArray()}).ToArray(),
            materials=materials.Select(m=>new {m.name,path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,
                cull=m.HasProperty("_Cull")?m.GetFloat("_Cull"):-1, surface=m.HasProperty("_Surface")?m.GetFloat("_Surface"):-1,
                alphaClip=m.HasProperty("_AlphaClip")?m.GetFloat("_AlphaClip"):-1}).ToArray() };
        bool allTwoSided=materials.All(m=>m.HasProperty("_Cull") && m.GetFloat("_Cull")==0);
        if(preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
        if(verifyFixed)
        {
            if(!allTwoSided) throw new Exception("Story1 has a ship material which still culls its back faces.");
            return new { shipMaterialsVerified=materials.Length, allTwoSided, image="Tools/ShipHullFixed.png" };
        }
        return report;
    }
}
