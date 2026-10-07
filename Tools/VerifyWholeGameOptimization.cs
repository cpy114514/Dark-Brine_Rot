using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerifyWholeGameOptimization
{
    static void Check(bool valid,string message){if(!valid)throw new Exception(message);}
    static async Task Load(string path){var op=SceneManager.LoadSceneAsync(path);while(!op.isDone)await Task.Delay(30);}
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static async Task<object> Run()
    {
        Check(Application.isPlaying,"Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] originalSave=File.Exists(save)?File.ReadAllBytes(save):null;
        var language=GameLocalization.Language;bool hadLanguage=PlayerPrefs.HasKey(GameLocalization.PreferenceKey);
        int storedLanguage=PlayerPrefs.GetInt(GameLocalization.PreferenceKey);
        float scale=Time.timeScale;var cursor=Cursor.lockState;bool cursorVisible=Cursor.visible;
        GameObject temporary=null,labelObject=null,graphicObject=null;Mesh testMesh=null;
        try
        {
            GameSaveManager.CancelPendingContinue();await Load("Assets/Scenes/First Island/Main.unity");
            for(int i=0;i<300 && AdditiveSceneBootstrap.IsLoading;i++)await Task.Delay(30);
            await Task.Delay(1000);
            Check(!AdditiveSceneBootstrap.IsLoading && SceneManager.GetSceneByName("Enemies").isLoaded,"Additive loading did not finish.");
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            player.gameObject.AddComponent<GameSaveExcluded>();player.ExternalControlLock=true;
            var arrival=player.GetComponent<FirstIslandArrival>();arrival.StopAllCoroutines();
            typeof(FirstIslandArrival).GetMethod("ReleaseCinematic",Private).Invoke(arrival,null);
            arrival.RestoreProgress(true,true);arrival.enabled=false;player.enabled=false;
            ((Image)typeof(FirstIslandArrival).GetField("blackout",Private).GetValue(arrival)).color=Color.clear;
            var camera=Camera.main;var urp=camera.GetComponent<UniversalAdditionalCameraData>();
            int mask=camera.cullingMask;bool shadows=urp.renderShadows,post=urp.renderPostProcessing;
            var depth=urp.requiresDepthOption;var color=urp.requiresColorOption;
            bool Restored()=>camera.cullingMask==mask && urp.renderShadows==shadows && urp.renderPostProcessing==post && urp.requiresDepthOption==depth && urp.requiresColorOption==color;
            var map=player.GetComponent<IslandMapUI>();
            map.enabled=true;
            Check(map.SetOpen(true) && camera.cullingMask==0 && !urp.renderShadows && !urp.renderPostProcessing,"Map still renders the world.");
            Canvas.ForceUpdateCanvases();Check(map.MapGraphic.canvasRenderer.GetMesh().vertexCount>0,"Map terrain disappeared.");
            await Task.Delay(100);CaptureScreen("map.png");
            map.SetOpen(false);Check(Restored() && !IslandMapUI.IsOpen && Time.timeScale==1,"Closing map changed camera settings or time.");
            await Task.Delay(100);Check(map.SetOpen(true),"Second map open was blocked.");map.enabled=false;
            Check(Restored() && !IslandMapUI.IsOpen && Time.timeScale==1,"Disabling map did not restore rendering: mask="+camera.cullingMask+"/"+mask+" shadows="+urp.renderShadows+"/"+shadows+" post="+urp.renderPostProcessing+"/"+post+" depth="+urp.requiresDepthOption+"/"+depth+" color="+urp.requiresColorOption+"/"+color+" time="+Time.timeScale+" open="+IslandMapUI.IsOpen);map.enabled=true;
            await Task.Delay(30);
            temporary=new GameObject("Temporary map lifecycle",typeof(IslandMapUI));
            var temporaryMap=temporary.GetComponent<IslandMapUI>();Check(temporaryMap.SetOpen(true),"Temporary map failed to open.");
            UnityEngine.Object.DestroyImmediate(temporary);temporary=null;
            Check(Restored() && !IslandMapUI.IsOpen && Time.timeScale==1,"Destroying map did not restore rendering.");

            labelObject=new GameObject("Temporary localization",typeof(RectTransform),typeof(Text));
            var label=labelObject.GetComponent<Text>();label.text="MAIN MENU";LocalizedGameText.Bind(label);
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese);Check(label.text==GameLocalization.Text("MAIN MENU"),"Existing label did not change language.");
            label.text="FIRST ISLAND";await Task.Delay(50);Check(label.text==GameLocalization.Text("FIRST ISLAND"),"Dynamic label no longer translates.");
            GameLocalization.SetLanguage(GameLanguage.English);Check(label.text=="FIRST ISLAND","Dynamic source was lost during language change.");

            temporary=new GameObject("Temporary map terrain");temporary.SetActive(false);
            var terrain=temporary.AddComponent<ProceduralIsland>();terrain.enabled=false;
            testMesh=new Mesh();testMesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.forward,Vector3.right*5,Vector3.right*6,Vector3.right*5+Vector3.forward};
            testMesh.subMeshCount=2;testMesh.SetTriangles(new[]{0,1,2},0,false);testMesh.SetTriangles(new[]{0,1,2},1,false,3);testMesh.RecalculateBounds();
            temporary.GetComponent<MeshFilter>().sharedMesh=testMesh;
            graphicObject=new GameObject("Temporary map geometry",typeof(RectTransform),typeof(CanvasRenderer),typeof(IslandMapGraphic));
            var graphic=graphicObject.GetComponent<IslandMapGraphic>();
            ((List<ProceduralIsland>)typeof(IslandMapGraphic).GetField("islands",Private).GetValue(graphic)).Add(terrain);
            using(var vertices=new VertexHelper())
            {
                typeof(IslandMapGraphic).GetMethod("OnPopulateMesh",Private|BindingFlags.DeclaredOnly).Invoke(graphic,new object[]{vertices});var drawn=new Mesh();
                try{vertices.FillMesh(drawn);Check(drawn.vertexCount==6 && drawn.triangles.SequenceEqual(new[]{0,1,2,3,4,5}),"Map lost a submesh or base vertex.");}
                finally{UnityEngine.Object.DestroyImmediate(drawn);}
            }
            UnityEngine.Object.DestroyImmediate(graphicObject);graphicObject=null;UnityEngine.Object.DestroyImmediate(temporary);temporary=null;
            UnityEngine.Object.DestroyImmediate(testMesh);testMesh=null;

            var library=Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library");
            Check(library.entries.Length==30 && library.entries.Count(e=>e.close)==5,"Foliage library is incomplete.");
            foreach(var entry in library.entries.Where(e=>e.close))
            {
                Check(entry.near.subMeshCount==entry.close.subMeshCount && entry.near.bounds==entry.close.bounds,"Foliage material slots or bounds changed.");
                Check(entry.near.vertexCount==entry.near.uv.Length && entry.near.vertexCount==entry.near.normals.Length,"Foliage lost UVs or normals.");
                long before=Enumerable.Range(0,entry.close.subMeshCount).Sum(i=>(long)entry.close.GetIndexCount(i));
                long after=Enumerable.Range(0,entry.near.subMeshCount).Sum(i=>(long)entry.near.GetIndexCount(i));Check(after<before,"Balanced mesh is not lighter.");
            }
            var manager=Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().First(m=>m.gameObject.scene.IsValid() && m.gameObject.scene.name=="DontDestroyOnLoad");
            var managed=(System.Collections.ICollection)typeof(WorldPerformanceManager).GetField("foliage",Private).GetValue(manager);
            Check(managed.Count>10000,"Foliage cache was not built after additive loading.");
            manager.enabled=false;Check(managed.Count==0,"Disabling manager leaves a stale cache.");
            manager.enabled=true;await Task.Delay(300);Check(managed.Count>10000,"Reenabling manager did not rebuild foliage.");

            camera.transform.SetPositionAndRotation(new Vector3(7.6f,51.34f,762.5f),Quaternion.LookRotation(new Vector3(8,-2,37)));
            await Task.Delay(300);CaptureCamera(camera,"forest.png");
            float next=(float)typeof(WorldPerformanceManager).GetField("nextUpdate",Private).GetValue(manager);
            typeof(WorldPerformanceManager).GetField("nextUpdate",Private).SetValue(manager,float.PositiveInfinity);
            var changed=new List<(MeshFilter filter,Mesh mesh)>();
            try
            {
                foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    var entry=library.entries.FirstOrDefault(e=>e.close && e.near==filter.sharedMesh);if(entry==null)continue;
                    changed.Add((filter,filter.sharedMesh));filter.sharedMesh=entry.close;
                }
                CaptureCamera(camera,"forest-old-detail.png");
            }
            finally{foreach(var item in changed)if(item.filter)item.filter.sharedMesh=item.mesh;typeof(WorldPerformanceManager).GetField("nextUpdate",Private).SetValue(manager,next);}
            var checkpoint=UnityEngine.Object.FindFirstObjectByType<IslandRespawnPoint>();
            player.GetComponent<CharacterController>().enabled=false;player.transform.position=checkpoint.transform.position;
            await Task.Delay(600);Check(player.GetComponent<PlayerDeathRespawn>().Checkpoint==checkpoint,"Checkpoint activation changed.");
            var effects=UnityEngine.Object.FindFirstObjectByType<NailongCombatEffects>();
            Check(effects!=null,"Nailong missing.");effects.ShowTaunt(.1f);effects.Roar(3);effects.Spit(player.transform,10,0);
            Check(effects.ActiveProjectileCount==1,"Muted audio prevented spit attack.");effects.Cancel();await Task.Delay(30);
            Check(effects.ActiveProjectileCount==0,"Spit cleanup failed.");
            Check(AudioListener.volume==0 && !GameAudioPolicy.SoundEnabled,"Audio was reenabled.");
            return new {success=true,additiveCache=true,mapCameraRoundTrip=true,mapDisableAndDestroy=true,languageAndDynamicLabels=true,
                mapMultipleSubmeshes=true,foliageMaterialSlotsAndBounds=true,foliageReenable=true,checkpoint=true,mutedCombat=true,previews=Path.GetFullPath(".codex/performance/visuals")};
        }
        finally
        {
            if(temporary)UnityEngine.Object.DestroyImmediate(temporary);if(labelObject)UnityEngine.Object.DestroyImmediate(labelObject);
            if(graphicObject)UnityEngine.Object.DestroyImmediate(graphicObject);if(testMesh)UnityEngine.Object.DestroyImmediate(testMesh);
            GameLocalization.SetLanguage(language);if(hadLanguage)PlayerPrefs.SetInt(GameLocalization.PreferenceKey,storedLanguage);else PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey);PlayerPrefs.Save();
            Time.timeScale=1;await Load("Assets/Scenes/Main Menu/MainMenu.unity");
            Time.timeScale=scale;Cursor.lockState=cursor;Cursor.visible=cursorVisible;GameSaveManager.CancelPendingContinue();
            if(originalSave!=null)File.WriteAllBytes(save,originalSave);else if(File.Exists(save))File.Delete(save);
        }
    }
    static string Destination(string name){string path=Path.GetFullPath(".codex/performance/visuals/"+name);Directory.CreateDirectory(Path.GetDirectoryName(path));return path;}
    static void CaptureScreen(string name){var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Destination(name),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
    static void CaptureCamera(Camera camera,string name)
    {
        var oldTarget=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Destination(name),image.EncodeToPNG());}
        finally{camera.targetTexture=oldTarget;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
}
