using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class InspectFoliage
{
    public static Task<object> Run()
    {
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return Task.FromResult<object>(new {
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
            drivers = Object.FindObjectsByType<Mavis.FoliageWindDriver>(FindObjectsSortMode.None).Select(d => new {d.name, d.baseStrength,d.globalResponse, zone=d.windZone ? d.windZone.windMain : -1}).ToArray(),
            triggers = Object.FindObjectsByType<Mavis.FoliageSquishTrigger>(FindObjectsSortMode.None).Length,
            groups = renderers.Where(r => r.sharedMaterials.Any(m => m && (m.name.ToLower().Contains("grass") || m.name.ToLower().Contains("forest") || m.name.ToLower().Contains("leaves") || m.name.ToLower().Contains("tree") || m.shader.name.Contains("Foliage")))).GroupBy(r => string.Join(";", r.sharedMaterials.Where(m=>m).Select(m=>m.name+":"+m.shader.name))).Select(g => new {material=g.Key,count=g.Count(),sample=g.Take(3).Select(r=>new {r.name,position=r.transform.position.ToString(),bounds=r.bounds.ToString(),parent=r.transform.parent ? r.transform.parent.name : "", materials=r.sharedMaterials.Where(m=>m).Select(m=>new {path=AssetDatabase.GetAssetPath(m), texture=m.mainTexture ? AssetDatabase.GetAssetPath(m.mainTexture) : "",alpha=m.HasProperty("_AlphaMap")&&m.GetTexture("_AlphaMap") ? AssetDatabase.GetAssetPath(m.GetTexture("_AlphaMap")) : ""}).ToArray()}).ToArray()}).ToArray()
        });
    }
}
