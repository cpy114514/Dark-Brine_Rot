using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Mavis;

public static class VerifyInitializationWarnings
{
    public static async Task<object> Verify()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var messages = new List<string>();
        Application.LogCallback handler = (message, trace, level) => {
            if (message.Contains("AnimatorController") || message.Contains("Failed to create agent") ||
                message.Contains("NoSubscription")) messages.Add(message);
        };
        Application.logMessageReceived += handler;
        var empty = new GameObject("Unconfigured Animator test", typeof(Animator));
        GameObject hero = null, boss = null;
        try
        {
            empty.AddComponent<SahurCombatGuardIK>();
            hero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"), new Vector3(0, 500, 0), Quaternion.identity);
            boss = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab"), new Vector3(10, 500, 0), Quaternion.identity);
            var agent = boss.GetComponent<NavMeshAgent>();
            if (agent.enabled) throw new Exception("NavMesh agent enabled before checking its surface.");
            await Task.Delay(250);
            if (messages.Count != 0) throw new Exception(string.Join("\n", messages));
            var guard = hero.GetComponentInChildren<SahurCombatGuardIK>();
            if (guard == null || guard.GetComponent<Animator>().runtimeAnimatorController == null)
                throw new Exception("Visual Animator never received its controller.");
            return new { controllerlessAnimatorSafe = true, playerControllerAssigned = true,
                navMeshFallbackWithoutWarning = !agent.enabled, matchingWarnings = messages.Count };
        }
        finally
        {
            Application.logMessageReceived -= handler;
            UnityEngine.Object.DestroyImmediate(empty);
            if (hero != null) UnityEngine.Object.DestroyImmediate(hero);
            if (boss != null) UnityEngine.Object.DestroyImmediate(boss);
        }
    }
}
