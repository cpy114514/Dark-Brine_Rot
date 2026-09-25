using System;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Safe, repeatable handoff while the new Blender clips are being authored.
// It never rebuilds the Animator Controller or restores the old attack.
public static class NailongBossSetup
{
    const string PrefabPath = "Assets/Models/Nailong/Nailong.prefab";

    [MenuItem("Tools/Boss/Prepare Nailong for Blender Animations")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before editing the Nailong prefab.");

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null) throw new InvalidOperationException("Nailong prefab was not found.");
        try
        {
            var ai = root.GetComponent<NailongAI>();
            var health = root.GetComponent<NailongHealth>();
            var animator = root.GetComponent<Animator>();
            if (ai == null || health == null || animator == null)
                throw new InvalidOperationException("Nailong is missing a required boss component.");

            // Preserve the legacy components and their settings for rollback,
            // but make it impossible for the temporary boss to deal damage.
            var oldAttack = root.GetComponent<NailongAttack>();
            if (oldAttack != null) oldAttack.enabled = false;
            var oldMotion = root.GetComponent<NailongAttackMotion>();
            if (oldMotion != null) oldMotion.enabled = false;
            var oldHitbox = root.transform.Find("NailongHitbox");
            if (oldHitbox != null && oldHitbox.TryGetComponent(out Collider hitbox))
                hitbox.enabled = false;

            animator.applyRootMotion = false;
            ai.animator = animator;
            ai.health = health;
            ai.followStopDistance = 2.4f;
            ai.keepVisualGroundOffset = true;

            var agent = root.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = ai.chaseSpeed;
                agent.stoppingDistance = ai.followStopDistance;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            if (!success) throw new InvalidOperationException("Nailong prefab save failed.");
            AssetDatabase.SaveAssets();
            Debug.Log("[Nailong] Old attack disabled. Temporary behaviour: idle and follow Sahur only.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
