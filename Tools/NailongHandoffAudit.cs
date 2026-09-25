using System;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NailongHandoffAudit
{
    public static string Report()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nailong/Nailong.prefab");
        var boss = Boss();
        var sahur = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        return "playing=" + Application.isPlaying +
            " prefabAttack=" + prefab.GetComponent<NailongAttack>().enabled +
            " prefabMotion=" + prefab.GetComponent<NailongAttackMotion>().enabled +
            " sceneAttack=" + boss.GetComponent<NailongAttack>().enabled +
            " sceneMotion=" + boss.GetComponent<NailongAttackMotion>().enabled +
            " state=" + boss.GetComponent<NailongAI>().CurrentState +
            " boss=" + boss.transform.position +
            " sahur=" + (sahur == null ? "null" : sahur.transform.position.ToString()) +
            " sahurHealth=" + (sahur == null ? -1f : sahur.currentHealth);
    }

    public static string Near()
    {
        if (!Application.isPlaying) return "Play mode required";
        Application.runInBackground = true;
        var boss = Boss();
        var sahur = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        var controller = sahur.GetComponent<ThirdPersonPlayerController>();
        if (controller != null) controller.enabled = false;
        var island = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();
        var cc = sahur.GetComponent<CharacterController>();
        Vector3 position = boss.transform.position + boss.transform.forward * 1.7f;
        position.y = island.GetWorldSurfaceHeight(position) +
            (cc.height * 0.5f - cc.center.y) * Mathf.Abs(sahur.transform.lossyScale.y);
        sahur.transform.position = position;
        sahur.currentHealth = sahur.maxHealth;
        return Report();
    }

    public static string Far()
    {
        if (!Application.isPlaying) return "Play mode required";
        Application.runInBackground = true;
        var boss = Boss();
        var sahur = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        var controller = sahur.GetComponent<ThirdPersonPlayerController>();
        if (controller != null) controller.enabled = false;
        var island = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();
        var cc = sahur.GetComponent<CharacterController>();
        Vector3 position = boss.transform.position + boss.transform.forward * 8f;
        position.y = island.GetWorldSurfaceHeight(position) +
            (cc.height * 0.5f - cc.center.y) * Mathf.Abs(sahur.transform.lossyScale.y);
        sahur.transform.position = position;
        sahur.currentHealth = sahur.maxHealth;
        return Report();
    }

    static GameObject Boss()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Enemies.unity");
        return Array.Find(scene.GetRootGameObjects(), x => x.name == "Nailong");
    }
}
