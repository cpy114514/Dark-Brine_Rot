using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class VerifyHealthBarBoomerang
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Release(SahurBoomerang boom, SahurAttack attack)
    {
        for (int i = 0; i < 120 && !boom.IsFlying; i++)
        {
            attack.animator.Update(1f / 120f);
            boom.Simulate(1f / 120f);
        }
        if (!boom.IsFlying) throw new Exception("Throw animation did not release the stick.");
    }
    public static object Verify()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var sahurPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        var player = UnityEngine.Object.Instantiate(sahurPrefab, new Vector3(0, 500, 0), Quaternion.identity);
        var boss = UnityEngine.Object.Instantiate(bossPrefab, new Vector3(20, 500, 0), Quaternion.identity);
        var enemy = new GameObject("Boomerang test target");
        GameObject wall = null;
        try
        {
            boss.GetComponent<NailongAI>().enabled = false;
            var bossHp = boss.GetComponent<NailongHealth>();
            var bar = boss.GetComponent<NailongHealthBar>();
            if (bar == null) throw new Exception("Nailong health bar did not auto-attach.");
            bossHp.ApplyDamage(bossHp.maxHealth * .5f, boss.transform.position);
            bar.Refresh(.02f);
            if (Mathf.Abs(bar.DisplayedFraction - .5f) > .001f) throw new Exception("Health bar fill is incorrect.");
            bossHp.ApplyDamage(10000f, boss.transform.position);
            bar.Refresh(.02f);
            if (bar.IsVisible || bar.DisplayedFraction != 0f) throw new Exception("Dead boss health bar remains visible.");

            var boom = player.GetComponent<SahurBoomerang>();
            var attack = player.GetComponent<SahurAttack>();
            var stamina = player.GetComponent<PlayerStamina>();
            var stick = player.GetComponent<SahurSwimmingWeapon>().stick;
            Vector3 gripPosition = stick.localPosition;
            Quaternion gripRotation = stick.localRotation;
            bool heldVisible = stick.GetComponent<Renderer>().enabled;
            float staminaBefore = stamina.currentStamina;
            var hp = enemy.AddComponent<Health>();
            var box = enemy.AddComponent<BoxCollider>();
            box.size = new Vector3(3f, 3f, 3f);
            var extra = enemy.AddComponent<SphereCollider>();
            extra.radius = 1f;
            extra.isTrigger = true;
            if (!boom.TryThrow()) throw new Exception("Throw refused from idle.");
            if (!boom.IsThrowing || boom.IsFlying || !stick.GetComponent<Renderer>().enabled)
                throw new Exception("Stick detached before the throw animation.");
            if (boom.TryThrow()) throw new Exception("Repeated input restarted the windup.");
            Release(boom, attack);
            Vector3 direction = (Vector3)typeof(SahurBoomerang).GetField("direction", Private).GetValue(boom);
            enemy.transform.position = boom.FlightPosition + direction * 6f;
            Physics.SyncTransforms();
            if (boom.TryThrow() || stick.GetComponent<Renderer>().enabled) throw new Exception("Duplicate throw or held weapon remained visible.");
            attack.TriggerAttack();
            if ((bool)typeof(SahurAttack).GetField("attacking", Private).GetValue(attack)) throw new Exception("Melee started while the stick was away.");
            if (Mathf.Abs(staminaBefore - stamina.currentStamina - boom.staminaCost) > .001f)
                throw new Exception("Throw spent stamina more than once.");
            for (int i = 0; i < 120 && boom.IsThrowing; i++)
            {
                attack.animator.Update(1f / 120f);
                boom.Simulate(1f / 120f);
            }
            if (boom.IsThrowing || !boom.IsFlying || attack.IsCombatMotionActive)
                throw new Exception("Throw recovery did not release the movement lock while the stick was flying.");
            for (int i = 0; i < 400 && boom.IsFlying; i++) boom.Simulate(1f / 120f);
            float damage = 100f - hp.currentHealth;
            if (boom.IsFlying || Mathf.Abs(damage - boom.damage * 2f) > .01f)
                throw new Exception("Return or hit deduplication failed: damage=" + damage);
            if (stick.GetComponent<Renderer>().enabled != heldVisible || Vector3.Distance(gripPosition, stick.localPosition) > .0001f ||
                Quaternion.Angle(gripRotation, stick.localRotation) > .001f || attack.stickHitbox.enabled)
                throw new Exception("Catch did not restore the original grip and collider state.");
            if (boom.TryThrow()) throw new Exception("Catch cooldown was ignored.");
            enemy.SetActive(false);

            typeof(SahurBoomerang).GetField("nextThrow", Private).SetValue(boom, -1f);
            stamina.currentStamina = 100f;
            if (!boom.TryThrow()) throw new Exception("Second throw refused.");
            Release(boom, attack);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = boom.FlightPosition + direction * 3f;
            wall.transform.localScale = Vector3.one * 2f;
            Physics.SyncTransforms();
            boom.Simulate(.5f);
            if (!boom.IsReturning) throw new Exception("Wall did not initiate a return.");
            player.transform.position += Vector3.right * 3f;
            for (int i = 0; i < 200 && boom.IsFlying; i++) boom.Simulate(.02f);
            if (boom.IsFlying) throw new Exception("Boomerang did not catch a moving owner.");
            UnityEngine.Object.DestroyImmediate(wall);
            wall = null;

            typeof(SahurBoomerang).GetField("nextThrow", Private).SetValue(boom, -1f);
            if (!boom.TryThrow()) throw new Exception("Cancellation test throw refused.");
            boom.enabled = false;
            if (boom.IsFlying || !stick.GetComponent<Renderer>().enabled) throw new Exception("Disable lost the weapon.");
            boom.enabled = true;
            typeof(SahurBoomerang).GetField("nextThrow", Private).SetValue(boom, -1f);
            stamina.currentStamina = 100f;
            if (!boom.TryThrow()) throw new Exception("Death cancellation throw refused.");
            var playerHealth = player.GetComponent<PlayerHealth>();
            float oldHealth = playerHealth.currentHealth;
            playerHealth.currentHealth = 0f;
            boom.Simulate(0f);
            if (boom.IsBusy || !stick.GetComponent<Renderer>().enabled)
                throw new Exception("Death during windup did not restore the stick.");
            playerHealth.currentHealth = oldHealth;
            return new { bossHalfHealthFill = .5f, deathHidesBar = true, throwDamageAcrossBothLegs = damage,
                oneHitPerLeg = true, duplicateHurtboxesSafe = true, meleeBlockedWhileAway = true,
                staminaSpentOnce = true, catchRestoresGrip = true, catchCooldown = true,
                wallInitiatesReturn = true, followsMovingOwner = true, disableRestoresStick = true,
                animatedWindup = true, releaseSynchronized = true, throwRecoveryUnlocksMovement = true, deathCancelsWindup = true };
        }
        finally
        {
            if (wall != null) UnityEngine.Object.DestroyImmediate(wall);
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(boss);
            UnityEngine.Object.DestroyImmediate(enemy);
        }
    }
}
