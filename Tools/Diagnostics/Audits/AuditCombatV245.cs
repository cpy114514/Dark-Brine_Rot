using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
public static class AuditCombatV245
{
    static object Actor(GameObject go)
    {
        var health=go.GetComponent<Health>();var nl=go.GetComponent<NailongHealth>();var player=go.GetComponent<PlayerHealth>();
        return new{go.name,health=health?new{health.maxHealth,health.currentHealth}:null,nailong=nl?new{nl.maxHealth,nl.currentHealth}:null,player=player?new{player.maxHealth,player.currentHealth}:null,
            components=go.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name).ToArray(),
            attacks=go.GetComponents<Component>().Where(c=>c&&(c is NailongAttack||c is CappuccinoAI||c is BombardinoBoss)).Select(c=>new{type=c.GetType().Name,data=EditorJsonUtility.ToJson(c)}).ToArray(),
            bones=go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s=>new{s.name,bones=s.bones.Where(b=>b).Select(b=>b.name).ToArray()}).ToArray()};
    }
    public static object Read()
    {
        var paths=new[]{"Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab"};
        var prefabs=paths.Select(p=>new{p,actor=Actor(AssetDatabase.LoadAssetAtPath<GameObject>(p))}).ToArray();
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/First Island/Enemies.unity");object[] actors;
        try{actors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c&&(c is NailongHealth||c is BombardinoBoss||c is CappuccinoEnemy)).Select(c=>Actor(c.gameObject)).ToArray();}
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Encounter/v006/PLAYER_Stagger_v006/PLAYER_Stagger_v006.fbx").OfType<AnimationClip>().Select(c=>new{c.name,c.length,c.isHumanMotion,c.isLooping}).ToArray();
        var data=new{prefabs,actors,clips};Directory.CreateDirectory(".codex/combat-recovery-20261023");File.WriteAllText(".codex/combat-recovery-20261023/audit-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
        return new{file=".codex/combat-recovery-20261023/audit-before.json",bosses=actors.Length,clips};
    }
}
