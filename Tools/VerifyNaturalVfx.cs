using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEngine.SceneManagement;
public static class VerifyNaturalVfx
{
    static void Require(bool valid,string message) {if(!valid)throw new Exception(message);}
    public static object VerifyBudgets()
    {
        var root=new GameObject("VFX budget fixture");
        var random=UnityEngine.Random.state;
        try
        {
            var camera=Camera.main;
            if(camera==null)
            {
                var cameraGo=new GameObject("VFX distance fixture camera");cameraGo.transform.SetParent(root.transform);
                camera=cameraGo.AddComponent<Camera>();camera.tag="MainCamera";
            }
            Vector3 at=camera!=null?camera.transform.position:Vector3.zero;
            root.transform.position=at;
            var smokeGo=new GameObject("Smoke reset fixture");smokeGo.transform.SetParent(root.transform,false);
            var smoke=smokeGo.AddComponent<Mavis.BombardinoBlastFX>();smoke.Initialize(3);
            var originalMaterial=smoke.material;
            smoke.Tick(.4f);smoke.Initialize(3);
            Require(smokeGo.GetComponentsInChildren<ParticleSystem>().Length==1,"Reinitializing smoke allocated another emitter.");
            Require(smoke.material==originalMaterial,"Reinitializing smoke leaked its previous material.");
            Require(smoke.material.IsKeywordEnabled("_PARTICLE_MASK"),"Downloaded smoke mask not active.");
            var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
            var wreck=root.AddComponent<Story1WreckEffects>();wreck.Initialize(asset.hulls[0].material,asset.hulls[0].mesh);
            for(int i=0;i<40;i++)wreck.ShipImpact(at,Vector3.forward);
            var systems=root.GetComponentsInChildren<ParticleSystem>();
            foreach(var p in systems)Require(p.particleCount<=p.main.maxParticles,"Repeated bursts exceeded the particle budget.");
            var wood=systems.Single(p=>p.name=="Small hull splinters");
            Require(wood.GetComponent<ParticleSystemRenderer>().mesh.triangles.Length==36,"Small chips are still using a hull mesh.");
            var water=systems.Single(p=>p.name=="Contact water droplets");
            Require(water.main.gravityModifier.constant>1f,"Droplets have lost their downward acceleration.");
            if(camera!=null)
            {
                Require(NaturalParticleEffects.BurstBudget(at,100)==100,"Nearby impact was culled.");
                Require(NaturalParticleEffects.BurstBudget(at+Vector3.right*110,100)==20,"Distant impact did not thin out.");
                Require(NaturalParticleEffects.BurstBudget(at+Vector3.right*150,100)==0,"Very distant particles still emit.");
            }
            return new{reusesSmokeEmitterAndMaterial=true,maskedSmoke=true,repeatedBurstsBounded=true,gravity=true,distanceChecks=camera!=null,
                systems=systems.Select(p=>new{p.name,p.particleCount,budget=p.main.maxParticles}).ToArray()};
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Random.state=random;}
    }
    public static object VerifyWater()
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        var random=UnityEngine.Random.state;var lockState=Cursor.lockState;bool cursorVisible=Cursor.visible;
        GameObject root=null,oceanGo=null;
        try
        {
            oceanGo=new GameObject("VFX water fixture");oceanGo.SetActive(false);
            var ocean=oceanGo.AddComponent<OceanWorld>();ocean.resolution=16;ocean.oceanSize=40;ocean.followCamera=false;ocean.oceanHeight=500;
            ocean.wave1.amplitude=ocean.wave2.amplitude=ocean.wave3.amplitude=ocean.wave4.amplitude=0;oceanGo.SetActive(true);
            root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(0,498,0),Quaternion.identity);
            root.AddComponent<Mavis.GameSaveExcluded>();
            var player=root.GetComponent<ThirdPersonPlayerController>();player.enabled=false;player.waterSplashes=true;player.underwaterBubbleParticles=true;
            root.GetComponent<Mavis.SahurAttack>().enabled=false;
            var cameraGo=new GameObject("VFX water fixture camera");cameraGo.transform.SetParent(root.transform,false);
            var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var type=typeof(ThirdPersonPlayerController);
            type.GetField("playerCamera",flags).SetValue(player,camera);
            type.GetField("swimmingOcean",flags).SetValue(player,ocean);
            type.GetMethod("CreateWaterSplashEffect",flags).Invoke(player,null);
            type.GetMethod("CreateUnderwaterPresentation",flags).Invoke(player,null);
            var emit=type.GetMethod("EmitWaterSplash",flags);
            for(int i=0;i<20;i++)
            {
                emit.Invoke(player,new object[]{1f,5});
                foreach(var p in root.GetComponentsInChildren<ParticleSystem>())p.Simulate(.1f,true,false,false);
            }
            type.GetField("planarVelocity",flags).SetValue(player,Vector3.forward*12);
            var bubbles=type.GetMethod("UpdateUnderwaterBubbles",flags);
            bubbles.Invoke(player,new object[]{true});
            var systems=root.GetComponentsInChildren<ParticleSystem>();
            var rings=systems.Single(p=>p.name=="Water Ripple Rings");
            var drops=systems.Single(p=>p.name=="Water Splash Columns");
            var air=systems.Single(p=>p.name=="Underwater Bubble Trail");
            Require(rings.particleCount>0 && drops.particleCount>0 && air.particleCount>0,"A swimming effect stopped emitting.");
            Require(rings.main.maxParticles==12 && drops.main.maxParticles==24 && air.main.maxParticles==16,"Swimming budgets did not apply.");
            int before=air.particleCount;bubbles.Invoke(player,new object[]{false});Require(air.particleCount==before,"Idle swimming emitted extra bubbles.");
            var particles=new ParticleSystem.Particle[12];int count=rings.GetParticles(particles);
            for(int i=0;i<count;i++)Require(Mathf.Abs(particles[i].position.y-500.025f)<.001f,"A ripple emitted below the water surface.");
            return new{surfaceContact=true,idleDoesNotEmitBubbles=true,systems=systems.Select(p=>new{p.name,p.particleCount,budget=p.main.maxParticles}).ToArray()};
        }
        finally
        {
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(oceanGo!=null)UnityEngine.Object.DestroyImmediate(oceanGo);
            UnityEngine.Random.state=random;Cursor.lockState=lockState;Cursor.visible=cursorVisible;
        }
    }
    public static object Capture(string label)
    {
        var random=UnityEngine.Random.state;
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
        var root=new GameObject("VFX inspection");
        SceneManager.MoveGameObjectToScene(root,scene);
        RenderTexture target=null;Texture2D frame=null;
        var active=RenderTexture.active;
        try
        {
            var cameraGo=new GameObject("VFX inspection camera");cameraGo.transform.SetParent(root.transform);
            var camera=cameraGo.AddComponent<Camera>();camera.cullingMask=1<<30;
            camera.transform.position=new Vector3(0,4,-15);camera.transform.LookAt(new Vector3(0,1.5f,0));
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.17f,.21f);
            camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            var lightGo=new GameObject("VFX inspection light");lightGo.transform.SetParent(root.transform);
            var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;
            light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(40,30,0);
            var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
            var wreckGo=new GameObject("Wreck effect fixture");wreckGo.transform.SetParent(root.transform);
            var wreck=wreckGo.AddComponent<Story1WreckEffects>();wreck.Initialize(asset.hulls[0].material,asset.hulls[0].mesh);
            UnityEngine.Random.InitState(54321);wreck.ShipImpact(new Vector3(-4,0,0),Vector3.forward);
            var smokeGo=new GameObject("Smoke fixture");smokeGo.transform.SetParent(root.transform);smokeGo.transform.position=new Vector3(4,0,0);
            var smoke=smokeGo.AddComponent<Mavis.BombardinoBlastFX>();UnityEngine.Random.InitState(235);smoke.Initialize(3);
            foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            foreach(var p in root.GetComponentsInChildren<ParticleSystem>())p.Simulate(.32f,true,false,false);
            var inventory=root.GetComponentsInChildren<ParticleSystem>().Select(p=>new {p.name,count=p.particleCount,budget=p.main.maxParticles,
                triangles=p.GetComponent<ParticleSystemRenderer>().mesh!=null?p.GetComponent<ParticleSystemRenderer>().mesh.triangles.Length/3:2,
                texture=p.GetComponent<ParticleSystemRenderer>().sharedMaterial.HasProperty("_MaskTex")?p.GetComponent<ParticleSystemRenderer>().sharedMaterial.GetTexture("_MaskTex")?.name:null}).ToArray();
            string folder=Path.GetFullPath(".codex/vfx/visuals");Directory.CreateDirectory(folder);
            target=new RenderTexture(1280,720,24);frame=new Texture2D(1280,720,TextureFormat.RGB24,false);
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            frame.ReadPixels(new Rect(0,0,1280,720),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(folder,label+".png"),frame.EncodeToPNG());
            var shaders=new[]{Shader.Find("DarkBrine/Procedural Water VFX")};
            if(shaders.Any(s=>s==null || ShaderUtil.ShaderHasError(s)))throw new Exception("VFX shader missing or failed compilation.");
            var report=new{label,inventory,impactCount=wreck.ImpactCount};
            File.WriteAllText(Path.Combine(folder,label+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally
        {
            RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(root);
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(frame!=null)UnityEngine.Object.DestroyImmediate(frame);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEngine.Random.state=random;
        }
    }
}
