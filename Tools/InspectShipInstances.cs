using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class InspectShipInstances
{
    public static object Run()
    {
        return new { scene=EditorSceneManager.GetActiveScene().path,
            viewPosition=SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera.transform.position.ToString():"none",
            ships=Resources.FindObjectsOfTypeAll<MeshFilter>().Where(f=>f.gameObject.scene.IsValid() &&
                AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("ship_g.fbx")).Select(f=>new {
                    scene=f.gameObject.scene.path, f.name, root=f.transform.root.name,
                    position=f.transform.position.ToString(), scale=f.transform.lossyScale.ToString(),
                    active=f.gameObject.activeInHierarchy,enabled=f.GetComponent<Renderer>().enabled,
                    forcedOff=f.GetComponent<Renderer>().forceRenderingOff,
                    materials=f.GetComponent<Renderer>().sharedMaterials.Select(m=>new {
                        name=m ? m.name:"missing", path=AssetDatabase.GetAssetPath(m),
                        cull=m && m.HasProperty("_Cull") ? m.GetFloat("_Cull"):-1}).ToArray()
                }).ToArray() };
    }
}
