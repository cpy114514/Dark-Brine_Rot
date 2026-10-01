using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class VerifyNailongLiveAI
{
    public static async Task<object> Verify()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var player = new GameObject("Nailong live AI test victim");
        player.transform.position = new Vector3(0, 500, 2.5f);
        player.tag = "Player";
        var controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = .3f;
        controller.center = Vector3.up;
        var hp = player.AddComponent<PlayerHealth>();
        hp.maxHealth = hp.currentHealth = 1000f;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        var boss = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 500, 0), Quaternion.identity);
        try
        {
            var ai = boss.GetComponent<NailongAI>();
            bool enteredAttack = false;
            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(100);
                if (!Application.isPlaying) throw new Exception("Play Mode ended during the live test.");
                enteredAttack |= ai.CurrentState == NailongAI.State.Attack;
            }
            float damage = 1000f - hp.currentHealth;
            var agent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (!enteredAttack || damage <= 0f || (agent != null && agent.enabled && !agent.isOnNavMesh))
                throw new Exception("Live AI did not attack or failed no-NavMesh fallback.");
            return new { autonomousTargetAcquisition = true, entersCombat = enteredAttack,
                damageDealt = damage, noNavMeshFallbackWorks = true, finalState = ai.CurrentState.ToString() };
        }
        finally
        {
            if (boss != null) UnityEngine.Object.DestroyImmediate(boss);
            if (player != null) UnityEngine.Object.DestroyImmediate(player);
        }
    }
}
