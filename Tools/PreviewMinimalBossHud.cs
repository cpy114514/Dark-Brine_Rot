using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Mavis;

public static class PreviewMinimalBossHud
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Stopped Editor required");
        const string output=".codex/boss-ui-cleanup-20261013/";
        var scene=EditorSceneManager.NewPreviewScene();
        BossHudPanel hud=null;RenderTexture texture=null;Texture2D image=null;
        var oldTarget=RenderTexture.active;
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab");
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);actor.transform.position=Vector3.right*1000;
            var health=actor.GetComponent<Health>();var ai=actor.GetComponent<CappuccinoAI>();
            var ultimate=actor.GetComponent<CappuccinoUltimate>();
            if(ultimate==null)throw new Exception("Prefab ultimate missing");
            const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
            object Call(string name,params object[] args)=>typeof(CappuccinoUltimate).GetMethod(name,Private).Invoke(ultimate,args);
            Call("Awake");ultimate.showWarnings=true;
            float initialMax=health.maxHealth,initialDamage=ai.damage;
            health.currentHealth=initialMax*.4f;
            var states=new List<object>();
            void Observe(string expected)
            {
                states.Add(new{phase=ultimate.CurrentPhase.ToString(),health.currentHealth,health.maxHealth,ai.damage,ultimate.MovementLocked,ultimate.AttackLocked});
                if(ultimate.CurrentPhase.ToString()!=expected)throw new Exception("Unexpected ultimate phase");
                if(actor.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Capri Ultimate Warning"))throw new Exception("Legacy warnings reappeared");
            }
            if(!(bool)Call("TryActivate",100f))throw new Exception("Ultimate no longer activates");Observe("Charging");
            Call("Simulate",100+ultimate.chargeSeconds+.001f);Observe("Empowered");
            if(Mathf.Abs(health.maxHealth-initialMax*ultimate.healthMultiplier)>.001f || Mathf.Abs(ai.damage-initialDamage*ultimate.damageMultiplier)>.001f)throw new Exception("Empowerment changed");
            Call("Simulate",100+ultimate.chargeSeconds+ultimate.empoweredSeconds+.001f);Observe("Immobilized");
            if(Mathf.Abs(health.maxHealth-initialMax)>.001f || Mathf.Abs(health.currentHealth-initialMax)>.001f || Mathf.Abs(ai.damage-initialDamage)>.001f)throw new Exception("Health restoration changed");
            Call("Simulate",100+ultimate.chargeSeconds+ultimate.empoweredSeconds+ultimate.immobilizedSeconds+.001f);Observe("Spent");
            Call("ResetEncounter");Observe("Ready");
            Call("OnDestroy");

            hud=new BossHudPanel(actor.transform,"Minimal boss HUD preview");
            health.currentHealth=initialMax*.4f;
            hud.Refresh(true,0,health.Ratio,health.currentHealth,health.maxHealth,"CAPPUCCINO ASSASSINO",1);
            var canvas=actor.GetComponentsInChildren<Canvas>(true).Single(c=>c.name=="Minimal boss HUD preview");
            canvas.GetComponent<CanvasGroup>().alpha=1;
            var cameraObject=new GameObject("Isolated UI preview camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.16f,.17f);camera.orthographic=true;
            canvas.renderMode=RenderMode.WorldSpace;
            var canvasRect=(RectTransform)canvas.transform;
            canvasRect.sizeDelta=new Vector2(1920,1080);canvasRect.position=Vector3.zero;canvasRect.rotation=Quaternion.identity;
            var parentScale=canvasRect.parent.lossyScale;canvasRect.localScale=new Vector3(1/parentScale.x,1/parentScale.y,1/parentScale.z);
            camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=540;camera.farClipPlane=100;
            texture=new RenderTexture(1920,1080,24);texture.Create();camera.targetTexture=texture;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;
            image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes(output+"boss-hud-preview.png",image.EncodeToPNG());
            var texts=canvas.GetComponentsInChildren<Text>(true).Select(t=>new{name=t.name,text=t.text}).ToArray();
            var result=new{nativePrefab=true,editModePreview=true,gameplayPhasesPreserved=true,legacyWarningFlagIgnored=true,texts,states};
            File.WriteAllText(output+"review.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            hud?.Dispose();RenderTexture.active=oldTarget;
            if(image!=null)UnityEngine.Object.DestroyImmediate(image);
            if(texture!=null){texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
