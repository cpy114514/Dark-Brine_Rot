using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class VerifyFoliage
{
    public static async Task<object> Run()
    {
        var shader = Shader.Find("Mavis/FoliageWind");
        var messages = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity.ToString()=="Error").ToArray();
        if (!shader.isSupported || messages.Length > 0) throw new Exception("Foliage shader errors: " + string.Join(";",messages.Select(m=>m.message)));
        var driver = UnityEngine.Object.FindFirstObjectByType<FoliageWindDriver>();
        var system = driver.GetComponent<FoliageInteractionSystem>();
        if (!system) throw new Exception("Interaction system was not installed at runtime");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var actor = UnityEngine.Object.Instantiate(prefab, new Vector3(1000,1000,1000), Quaternion.identity);
        var controller = actor.GetComponent<ThirdPersonPlayerController>(); controller.enabled = false;
        var capsule = actor.GetComponent<CharacterController>();
        var r = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).First(g => g.enabled && g.sharedMaterials.Any(m=>m && m.name == "Grass_Stems") && g.bounds.size.y > 1f);
        var privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        var scan = typeof(FoliageInteractionSystem).GetMethod("ScanBodies",privateFlags);
        float oldTime = Time.timeScale; Time.timeScale = 1f;
        try
        {
            Vector3 feet = new Vector3(r.bounds.center.x, r.bounds.min.y + .2f, r.bounds.center.z);
            Physics.SyncTransforms();
            actor.transform.position += feet - new Vector3(capsule.bounds.center.x,capsule.bounds.min.y,capsule.bounds.center.z);
            Physics.SyncTransforms(); scan.Invoke(system,null);
            for (int i=0;i<20 && Shader.GetGlobalInt("_MavisFoliageBodyCount")==0;i++) await Task.Delay(50);
            if (Shader.GetGlobalInt("_MavisFoliageBodyCount") < 1) throw new Exception("No actor published to shader");
            for (int i=0;i<8;i++) { actor.transform.position += Vector3.right*.15f; Physics.SyncTransforms(); await Task.Delay(50); }
            var list = (System.Collections.IList)typeof(FoliageInteractionSystem).GetField("trails",privateFlags).GetValue(system);
            int movingTrails = list.Count;
            var particles = (ParticleSystem)typeof(FoliageInteractionSystem).GetField("leaves",privateFlags).GetValue(system);
            var audio = (AudioSource)typeof(FoliageInteractionSystem).GetField("rustle",privateFlags).GetValue(system);
            if (movingTrails == 0 || !particles || !particles.isPlaying || particles.particleCount == 0 || !audio || audio.volume <= 0f) throw new Exception("Missing trails, simulated grass particles or rustle");
            actor.SetActive(false); scan.Invoke(system,null);
            await Task.Delay(1500);
            if (list.Count != 0) throw new Exception("Footprints did not recover");
            int windRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(g=>g.sharedMaterials.Any(m=>m&&m.shader==shader));
            var windMaterial = r.sharedMaterials.First(m=>m && m.shader == shader);
            return new { success=true, windRenderers, movingTrails, recovery=true, particles=true, rustle=true, shadowPass=windMaterial.FindPass("ShadowCaster"), depthPass=windMaterial.FindPass("DepthOnly"), shaderErrors=messages.Length };
        }
        finally { UnityEngine.Object.Destroy(actor); Time.timeScale=oldTime; }
    }
}
