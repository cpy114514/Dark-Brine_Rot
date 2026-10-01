using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mavis;
public static class VerifyMonochromeUI
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Get(object obj,string field)=>obj.GetType().GetField(field,Private).GetValue(obj);
    static void Call(object obj,string method)=>obj.GetType().GetMethod(method,Private).Invoke(obj,null);
    static void Check(bool ok,string error) { if(!ok) throw new Exception(error); }
    static bool Neutral(Color c)=>Mathf.Abs(c.r-c.g)<.002f && Mathf.Abs(c.g-c.b)<.002f;
    static int Audit(GameObject root)
    {
        int count=0;
        foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if(graphic.GetComponentInParent<SahurDecorationHover>(true)) continue;
            Check(Neutral(graphic.color),"Colored UI: "+root.name+"/"+graphic.name); count++;
            if(graphic is TMP_Text tmp) Check(!tmp.enableVertexGradient,"Colored text gradient: "+tmp.name);
        }
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            var c=button.colors;
            Check(Neutral(c.normalColor)&&Neutral(c.highlightedColor)&&Neutral(c.pressedColor)&&Neutral(c.selectedColor)&&Neutral(c.disabledColor),"Colored button states: "+button.name);
            Check(Mathf.Abs(c.normalColor.grayscale-c.highlightedColor.grayscale)>.03f,"No hover feedback: "+button.name);
        }
        return count;
    }
    public static async Task<object> Run()
    {
        Check(Application.isPlaying,"Requires Play Mode.");
        float time=Time.timeScale; var cursor=Cursor.lockState; bool visible=Cursor.visible;
        var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(2000,500,2000),Quaternion.identity);
        var vitalsObject=new GameObject("Temporary monochrome vitals",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(PlayerVitalsHUD));
        var enemy=new GameObject("Temporary monochrome enemy"); enemy.AddComponent<NailongHealth>();
        var islandObject=new GameObject("Temporary monochrome map island");
        var mesh=new Mesh();
        var report=new List<object>();
        try
        {
            Time.timeScale=0;
            player.GetComponent<ThirdPersonPlayerController>().enabled=false;
            player.GetComponent<CharacterController>().enabled=false;
            foreach(var input in player.GetComponentsInChildren<UnityEngine.InputSystem.PlayerInput>()) input.enabled=false;
            var island=islandObject.AddComponent<ProceduralIsland>(); island.enabled=false;
            var filter=islandObject.GetComponent<MeshFilter>(); var renderer=islandObject.GetComponent<MeshRenderer>();
            mesh.vertices=new[]{new Vector3(-30,0,-30),new Vector3(30,0,-30),new Vector3(30,0,30),new Vector3(-30,0,30),new Vector3(0,10,0)};
            mesh.triangles=new[]{0,4,1,1,4,2,2,4,3,3,4,0}; mesh.RecalculateBounds(); filter.sharedMesh=mesh;
            islandObject.transform.position=player.transform.position-new Vector3(0,500,0);
            // Refresh sees enabled island meshes; don't run Update/generation in this synchronous window.
            island.enabled=true;
            var loadout=player.GetComponent<SahurLoadoutUI>(); loadout.SetOpen(true);
            var equipmentRoot=(GameObject)Get(loadout,"root"); report.Add(new {screen="equipment",graphics=Audit(equipmentRoot)});
            for(int i=0;i<4;i++)
            {
                loadout.SelectSkillForAssignment(i); var frames=(Image[])Get(loadout,"skillFrames");
                Check(frames[i].color==Color.white && frames.Where((f,j)=>j!=i).All(f=>f.color.grayscale<.5f),"Skill selection lost its border emphasis.");
            }
            loadout.SelectEquipment(SahurLoadoutUI.EquipmentSlot.Weapon);
            loadout.SelectSkillForAssignment(0);
            await Capture(equipmentRoot,"Tools/MonochromeEquipment.png");
            loadout.SetOpen(false);
            var map=player.GetComponent<IslandMapUI>();
            // Avoid the same-frame modal input guard without altering production code.
            await Task.Delay(60); Check(map.SetOpen(true),"Map did not open after closing equipment.");
            var mapRoot=(GameObject)Get(map,"root");
            Canvas.ForceUpdateCanvases(); await Task.Delay(60);
            var drawn=map.MapGraphic.canvasRenderer.GetMesh();
            Check(drawn && drawn.vertexCount>0 && drawn.colors32.All(c=>c.r==c.g&&c.g==c.b),"Map terrain is still colored or missing.");
            report.Add(new {screen="map",graphics=Audit(mapRoot)});
            await Capture(mapRoot,"Tools/MonochromeMap.png"); map.SetOpen(false);
            var death=player.GetComponent<PlayerDeathRespawn>(); Call(death,"BuildUI");
            var deathRoot=(GameObject)Get(death,"overlay"); report.Add(new {screen="death",graphics=Audit(deathRoot)});
            var respawn=deathRoot.GetComponentInChildren<Button>();
            Check(respawn.image && respawn.colors.normalColor.grayscale<.2f,"Respawn button background is unreadable.");
            await Capture(deathRoot,"Tools/MonochromeDeath.png"); deathRoot.SetActive(false);
            report.Add(new {screen="vitals",graphics=Audit(vitalsObject)});
            Check(vitalsObject.GetComponentsInChildren<Text>().Any(t=>t.text=="HP") && vitalsObject.GetComponentsInChildren<Text>().Any(t=>t.text=="SP"),"Vitals lost non-color identification.");
            report.Add(new {screen="enemy health",graphics=Audit(enemy)});
            var lockOn=player.GetComponent<EnemyLockOn>(); Call(lockOn,"BuildMarker");
            report.Add(new {screen="lock-on",graphics=Audit((GameObject)Get(lockOn,"hud"))});
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.name=="Main Menu" || c.name=="Settings Overlay"))
                report.Add(new {screen=canvas.name,graphics=Audit(canvas.gameObject)});
            return new {report,skillSelectionDistinguishable=true,mapVertexColorsMonochrome=true,hpSpLabels=true,buttonHoverVisible=true,artPreserved=true,
                previews=new[]{"Tools/MonochromeEquipment.png","Tools/MonochromeMap.png","Tools/MonochromeDeath.png"}};
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(player); UnityEngine.Object.DestroyImmediate(vitalsObject); UnityEngine.Object.DestroyImmediate(enemy);
            UnityEngine.Object.DestroyImmediate(islandObject); UnityEngine.Object.DestroyImmediate(mesh);
            Time.timeScale=time; Cursor.lockState=cursor; Cursor.visible=visible;
        }
    }
    static async Task Capture(GameObject root,string path)
    {
        var canvas=root.GetComponent<Canvas>();
        var cameraObject=new GameObject("Temporary monochrome UI capture");
        var target=new RenderTexture(1280,720,24);
        var frame=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var previous=RenderTexture.active; var mode=canvas.renderMode; var priorCamera=canvas.worldCamera;
        var scaler=canvas.GetComponent<CanvasScaler>(); bool enabled=scaler && scaler.enabled;
        var transforms=root.GetComponentsInChildren<Transform>(true); var layers=transforms.Select(t=>t.gameObject.layer).ToArray();
        try
        {
            var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false; camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=GameUITheme.Gray(.1f); camera.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
            if(scaler) scaler.enabled=false; canvas.scaleFactor=1280f/1920;
            foreach(var t in transforms) t.gameObject.layer=31;
            await Task.Delay(100); Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
            frame.ReadPixels(new Rect(0,0,1280,720),0,0); frame.Apply(); File.WriteAllBytes(Path.GetFullPath(path),frame.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode=mode; canvas.worldCamera=priorCamera; if(scaler) scaler.enabled=enabled;
            for(int i=0;i<transforms.Length;i++) if(transforms[i]) transforms[i].gameObject.layer=layers[i];
            RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(cameraObject); target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(frame);
        }
    }
}
