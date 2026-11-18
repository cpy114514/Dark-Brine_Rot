using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class InspectSecondIslandUpgrade
{
    public static object Run()
    {
        const string path="Assets/ThirdParty/SecondIslandMotion/UAL2_Standard.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        if(importer.animationType!=ModelImporterAnimationType.Human)
        {importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();}
        var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Select(c=>new{c.name,c.length,c.isHumanMotion}).ToArray();
        var prefabs=new[]{"Assets/Resources/SecondIslandHuangMei/Huang Mei Boss.prefab","Assets/Resources/SecondIslandZhuBaJie/ZhuBaJie Boss.prefab"}.Select(p=>{
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(p);
            return new{path=p,animators=prefab.GetComponentsInChildren<Animator>(true).Select(a=>new{a.name,human=a.avatar&&a.avatar.isHuman,valid=a.avatar&&a.avatar.isValid,controller=a.runtimeAnimatorController?a.runtimeAnimatorController.name:"none"}),
                skins=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>new{r.name,vertices=r.sharedMesh.vertexCount,bones=r.bones.Select(b=>b?b.name:"null").ToArray()})};
        });
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Second Island/Second Island.unity");
        object roots;
        try{roots=scene.GetRootGameObjects().Select(r=>new{r.name,position=r.transform.position.ToString(),children=r.transform.childCount}).ToArray();}
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        var result=new{clips,prefabs=prefabs.ToArray(),roots};
        Directory.CreateDirectory(".codex/island2-upgrade");File.WriteAllText(".codex/island2-upgrade/inspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return new{clips=clips.Select(c=>c.name).ToArray(),output=".codex/island2-upgrade/inspection.json"};
    }
}
