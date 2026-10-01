using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class VerifyNailongCombat
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj, value);
    static void Call(object obj, string method) => obj.GetType().GetMethod(method, Private).Invoke(obj, null);
    static GameObject MakePlayer(Vector3 position)
    {
        var obj = new GameObject("Nailong test victim");
        obj.transform.position = position;
        obj.tag = "Player";
        var capsule = obj.AddComponent<CharacterController>();
        capsule.height = 2f;
        capsule.radius = 0.3f;
        capsule.center = Vector3.up;
        obj.AddComponent<PlayerHealth>();
        var child = new GameObject("Extra hurtbox");
        child.transform.SetParent(obj.transform, false);
        var hurtbox = child.AddComponent<SphereCollider>();
        hurtbox.radius = 0.35f;
        hurtbox.center = Vector3.up;
        hurtbox.isTrigger = true;
        return obj;
    }
    public static object Verify()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        var boss = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 500, 0), Quaternion.identity);
        var player = MakePlayer(new Vector3(0, 500, 2.5f));
        var second = MakePlayer(new Vector3(2, 500, 3));
        GameObject wall = null;
        var shots = new List<NailongSpitProjectile>();
        try
        {
            var ai = boss.GetComponent<NailongAI>();
            ai.enabled = false;
            Set(ai, "target", player.transform);
            var hp = player.GetComponent<PlayerHealth>();
            var hp2 = second.GetComponent<PlayerHealth>();
            var effects = boss.GetComponent<NailongCombatEffects>();
            var attack = boss.GetComponent<NailongAttack>();
            var motion = boss.GetComponent<NailongAttackMotion>();
            var pattern = typeof(NailongAI).GetNestedType("AttackPattern", BindingFlags.NonPublic);
            var available = new HashSet<string>();
            for (int i = 0; i < 200; i++)
                available.Add(typeof(NailongAI).GetMethod("ChooseAttack", Private).Invoke(ai, new object[] { 2.5f }).ToString());
            if (!available.Contains("ClawFlurry") || !available.Contains("Roar") || !available.Contains("ScoldingSpit") || available.Count != 3)
                throw new Exception("AI does not mix the three new moves.");
            string farAttack = typeof(NailongAI).GetMethod("ChooseAttack", Private).Invoke(ai, new object[] { 12f }).ToString();
            if (farAttack != "ScoldingSpit") throw new Exception("Mid-range AI did not select saliva.");
            Set(ai, "nextActionTime", Time.time + 100f);
            if (typeof(NailongAI).GetMethod("ChooseAttack", Private).Invoke(ai, new object[] { 2f }).ToString() != "None")
                throw new Exception("AI ignored recovery cooldown.");
            Set(ai, "nextActionTime", 0f);
            void Begin(string name) => typeof(NailongAI).GetMethod("BeginAttack", Private).Invoke(ai, new[] { Enum.Parse(pattern, name) });
            void Tick(float elapsed)
            {
                Set(ai, "attackStartedAt", Time.time - elapsed);
                Call(ai, "TickAttack");
            }
            Begin("ClawFlurry");
            Tick(ai.flurryWindup - 0.01f);
            if (hp.currentHealth != 100f) throw new Exception("Flurry hit during the windup.");
            for (int i = 0; i < ai.flurryCount; i++)
            {
                Tick(ai.flurryWindup + ai.flurryInterval * i + 0.01f);
                Tick(ai.flurryWindup + ai.flurryInterval * i + 0.02f);
            }
            float clawDamage = 100f - hp.currentHealth;
            if (Mathf.Abs(clawDamage - attack.damage * ai.flurryDamageMultiplier * ai.flurryCount) > 0.01f)
                throw new Exception("Wrong flurry hit count: " + clawDamage);
            Tick(5f);
            if (ai.CurrentState == NailongAI.State.Attack) throw new Exception("Flurry did not recover.");

            hp.currentHealth = hp2.currentHealth = 100f;
            Physics.SyncTransforms();
            Begin("Roar");
            Tick(ai.roarWindup - 0.01f);
            if (hp.currentHealth != 100f) throw new Exception("Roar hit during warning.");
            Tick(ai.roarWindup + 0.01f);
            Tick(ai.roarWindup + 0.02f);
            float roarDamage = 100f - hp.currentHealth;
            if (Mathf.Abs(roarDamage - attack.damage * ai.roarDamageMultiplier) > 0.01f || Mathf.Abs(hp.currentHealth - hp2.currentHealth) > 0.01f)
                throw new Exception("Roar repeated damage or missed an adjacent victim.");
            var knockback = player.GetComponent<CombatKnockback>();
            if (knockback == null || !knockback.IsBeingPushed) throw new Exception("Roar did not apply knockback.");
            Vector3 before = player.transform.position;
            for (int i = 0; i < 120; i++) knockback.Simulate(1f / 60f);
            float pushed = Vector3.Distance(before, player.transform.position);
            if (pushed < 2f || pushed > 4.1f) throw new Exception("Unexpected knockback displacement: " + pushed);
            Tick(5f);

            player.transform.position = new Vector3(0, 500, 7f);
            second.SetActive(false);
            hp.currentHealth = 100f;
            Physics.SyncTransforms();
            var projectile = effects.Spit(player.transform, ai.spitSpeed, attack.damage * ai.spitDamageMultiplier);
            shots.Add(projectile);
            for (int i = 0; i < 100 && !projectile.HasImpacted; i++) projectile.Simulate(0.02f);
            float salivaDamage = 100f - hp.currentHealth;
            if (Mathf.Abs(salivaDamage - attack.damage * ai.spitDamageMultiplier) > 0.01f) throw new Exception("Projectile missed or hit multiple hurtboxes: " + salivaDamage);
            projectile.Simulate(1f);
            if (Mathf.Abs(100f - hp.currentHealth - salivaDamage) > 0.001f) throw new Exception("Projectile damaged twice.");

            hp.currentHealth = 100f;
            player.transform.position = new Vector3(0, 500, -2f);
            if (attack.TryHit(player.transform, 3f)) throw new Exception("Claws hit behind the boss.");
            player.transform.position = new Vector3(0, 505, 2f);
            if (attack.TryHit(player.transform, 3f)) throw new Exception("Claws hit far above the boss.");
            player.transform.position = new Vector3(0, 500, 20f);
            if (attack.TryHit(player.transform, 3f)) throw new Exception("Claws hit outside reach.");
            player.transform.position = new Vector3(0, 500, 7f);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Nailong projectile test wall";
            wall.transform.position = new Vector3(0, 502, 3.5f);
            wall.transform.localScale = new Vector3(10, 8, 0.3f);
            Physics.SyncTransforms();
            var blocked = effects.Spit(player.transform, ai.spitSpeed, attack.damage);
            shots.Add(blocked);
            blocked.Simulate(1f); // Deliberately cross the wall in one low-FPS step.
            if (!blocked.HasImpacted || hp.currentHealth != 100f) throw new Exception("Saliva tunneled through a wall.");
            UnityEngine.Object.DestroyImmediate(wall);
            wall = null;
            effects.Cancel();
            Begin("ScoldingSpit");
            Tick(ai.spitWindup - 0.01f);
            if (effects.ActiveProjectileCount != 0) throw new Exception("Spit fired before its cue.");
            Tick(ai.spitWindup + ai.spitInterval * (ai.spitCount - 1) + 0.01f);
            if (effects.ActiveProjectileCount != ai.spitCount) throw new Exception("Wrong saliva burst size.");
            boss.GetComponent<NailongHealth>().ApplyDamage(10000, boss.transform.position);
            if (ai.CurrentState != NailongAI.State.Dead || effects.ActiveProjectileCount != 0)
                throw new Exception("Death did not cancel combat/projectiles.");
            return new { clawHits = ai.flurryCount, clawDamage, roarDamage, roarHitsBothPlayersOnce = true,
                pushedMetres = pushed, salivaDamage, projectileHitsOnce = true, wallBlocksAtLowFPS = true,
                salivaBurstCount = ai.spitCount, windupsSafe = true, deathCancelsAttacks = true,
                aiMixesThreeAttacks = true, rangedChoiceCorrect = true, cooldownRespected = true, clawRangeAndFacingCorrect = true };
        }
        finally
        {
            foreach (var shot in shots) if (shot != null) UnityEngine.Object.DestroyImmediate(shot.gameObject);
            if (wall != null) UnityEngine.Object.DestroyImmediate(wall);
            UnityEngine.Object.DestroyImmediate(boss);
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(second);
        }
    }
}
