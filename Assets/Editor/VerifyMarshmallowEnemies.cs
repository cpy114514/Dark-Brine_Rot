using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;

// Runs against the actual second-island scene in play mode; all test changes
// are temporary and disappear when play mode stops.
[InitializeOnLoad]
public static class VerifyMarshmallowEnemies
{
    const string Key = "MarshmallowEnemies.Review";
    const string Folder = "Temp/MarshmallowEnemyReview";
    static VerifyMarshmallowEnemies() => EditorApplication.playModeStateChanged += OnMode;
    [MenuItem("Tools/Second Island Review/Test Marshmallow Enemies")]
    public static void Start()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop play before starting the test.");
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/status.txt", "starting");
        SessionState.SetBool(Key, true); VerifySecondIslandAwakening.Arm(); EditorApplication.isPlaying = true;
    }
    static void OnMode(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, false); _ = Run();
    }
    static async Task WaitUntil(Func<bool> predicate, float timeout, string description)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!predicate())
        {
            if (!EditorApplication.isPlaying) throw new Exception("Play mode ended.");
            if (Time.realtimeSinceStartup > deadline) throw new Exception("Timed out: " + description);
            await Task.Delay(50);
        }
    }
    static async Task Run()
    {
        GameObject instance = null;
        try
        {
            File.WriteAllText(Folder + "/status.txt", "opening");
            await WaitUntil(() => VerifySecondIslandAwakening.Status == "completed" || VerifySecondIslandAwakening.Status.StartsWith("failed"), 40, "opening");
            if (VerifySecondIslandAwakening.Status != "completed") throw new Exception(VerifySecondIslandAwakening.Status);
            var group = GameObject.Find("Marshmallow Friends - Second Island");
            if (!group) throw new Exception("The scene enemy group is missing.");
            var sceneEnemies = group.GetComponentsInChildren<MarshmallowEnemyHealth>();
            if (sceneEnemies.Length == 0) throw new Exception("No marshmallow enemies in the scene.");
            foreach (var child in sceneEnemies)
                if (!child.GetComponent<ChickenBroAI>() || !child.GetComponent<CharacterController>() || !child.GetComponent<MarshmallowEnemyVisual>())
                    throw new Exception("Enemy is missing a required component: " + child.name);
            group.SetActive(false);
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var victim = player.GetComponent<PlayerHealth>();
            var surface = player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            var results = new List<string>();
            foreach (string name in new[] { "Strawberry", "Mint", "Sky", "Lemon", "Lavender" })
            {
                await WaitUntil(() => victim.CanReceiveCombatDamage, 8, name + " player recovery");
                Vector3 basePosition = player.transform.position;
                Vector3 ground = Vector3.zero; bool found = false;
                for (int i = 0; i < 16; i++)
                {
                    float a = i * Mathf.PI * 2 / 16; var at = basePosition + new Vector3(Mathf.Cos(a)*5f, 20, Mathf.Sin(a)*5f);
                    if (surface.Raycast(new Ray(at, Vector3.down), out var hit, 50) && hit.normal.y > .85f && Mathf.Abs(hit.point.y-basePosition.y) < 1.5f)
                    { ground = hit.point; found = true; break; }
                }
                if (!found) throw new Exception("No test ground near the player for " + name);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Marshmallow/" + name + " Marshmallow.prefab");
                if (!prefab) throw new Exception("Missing prefab: " + name);
                instance = UnityEngine.Object.Instantiate(prefab, ground + Vector3.up*.08f, Quaternion.identity);
                var ai = instance.GetComponent<ChickenBroAI>(); var hp = instance.GetComponent<MarshmallowEnemyHealth>();
                var visual = instance.GetComponent<MarshmallowEnemyVisual>();
                if (!ai || !hp || !visual || hp.maxHealth <= 0 || hp.currentHealth != hp.maxHealth || ai.strikeDamage <= 0)
                    throw new Exception(name + " has invalid enemy settings.");
                Vector3 initialPosition = instance.transform.position; Quaternion initialArm = visual.leftArm.localRotation;
                File.WriteAllText(Folder + "/status.txt", name + ": chase");
                await WaitUntil(() => ai.Mode == ChickenBroAI.Mood.Chase || ai.HoldsAttack, 8, name + " spotting player");
                await WaitUntil(() => Vector3.Distance(instance.transform.position, initialPosition) > .3f, 8, name + " approaching player");
                if (!visual.IsAlert || !Mathf.Approximately(ai.MovementMultiplier, 1.5f))
                    throw new Exception(name + " did not apply the alert movement boost while chasing.");
                await WaitUntil(() => Quaternion.Angle(initialArm, visual.leftArm.localRotation) > 1f, 2, name + " limb animation");
                float moved = Vector3.Distance(instance.transform.position, initialPosition);
                if (name == "Strawberry")
                {
                    // Keep the existing alert, then move the enemy beyond its
                    // activation distance to prove the real motor keeps chasing.
                    Vector3 pursuitGround = Vector3.zero; bool pursuitFound = false;
                    Vector3 playerAt = player.transform.position;
                    for (int i = 0; i < 16 && !pursuitFound; i++)
                    {
                        float a = i * Mathf.PI * 2 / 16;
                        Vector3 horizontal = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        bool clear = true; Vector3 final = Vector3.zero;
                        for (int d = 9; d <= 27; d += 9)
                        {
                            var sample = playerAt + horizontal * d + Vector3.up * 25;
                            if (!surface.Raycast(new Ray(sample, Vector3.down), out var hit, 55) || hit.normal.y < .8f || Mathf.Abs(hit.point.y-playerAt.y) > 3f)
                            { clear = false; break; }
                            final = hit.point;
                        }
                        if (clear) { pursuitGround = final; pursuitFound = true; }
                    }
                    if (!pursuitFound) throw new Exception("No suitable ground for the 27-unit pursuit test.");
                    var motor = instance.GetComponent<CharacterController>();
                    motor.enabled = false; instance.transform.position = pursuitGround + Vector3.up * .08f; motor.enabled = true;
                    float initialRange = Vector3.Distance(instance.transform.position, player.transform.position);
                    await WaitUntil(() => visual.IsAlert && ai.Mode == ChickenBroAI.Mood.Chase, 5, "pursuit beyond 25 units");
                    await WaitUntil(() => Vector3.Distance(instance.transform.position, player.transform.position) < initialRange - .3f, 8, "actual movement towards player outside 25 units");
                    if (!Mathf.Approximately(ai.MovementMultiplier, 1.5f)) throw new Exception("The pursuit lost its speed boost.");
                    motor.enabled = false; instance.transform.position = ground + Vector3.up * .08f; motor.enabled = true;
                }
                File.WriteAllText(Folder + "/status.txt", name + ": enemy attack");
                float playerHealth = victim.currentHealth;
                await WaitUntil(() => victim.currentHealth < playerHealth, 15, name + " enemy attack damages player");
                float received = playerHealth - victim.currentHealth;
                if (!Mathf.Approximately(ai.attackCooldown, 1.4f)) throw new Exception(name + " has incorrect attack cooldown: " + ai.attackCooldown);
                await WaitUntil(() => !ai.HoldsAttack, 4, name + " finishes first attack");
                float finishedAt = Time.time;
                await WaitUntil(() => ai.HoldsAttack, 8, name + " starts a follow-up attack");
                float followupDelay = Time.time - finishedAt;
                File.WriteAllText(Folder + "/status.txt", name + ": player attack");
                float before = hp.currentHealth;
                if (!CombatHitFeedback.Apply(player.gameObject, hp, 5f, instance.transform.position + Vector3.up, CombatHitKind.ComboOne))
                    throw new Exception("Sahur's combat pipeline did not hit " + name);
                if (hp.currentHealth >= before || ai.Mode != ChickenBroAI.Mood.Flinch) throw new Exception(name + " did not take damage and stagger.");
                float healthAfterHit = hp.currentHealth;
                if (!CombatHitFeedback.Apply(player.gameObject, hp, 100f, instance.transform.position + Vector3.up, CombatHitKind.ComboFinisher) || !hp.IsDead || ai.Mode != ChickenBroAI.Mood.Dead)
                    throw new Exception("Lethal attack did not defeat " + name);
                results.Add($"{name}: max health={hp.maxHealth:F1}; moved={moved:F2}; player damage={received:F1}; follow-up delay={followupDelay:F2}; health after hit={healthAfterHit:F1}; defeated={hp.IsDead}");
                UnityEngine.Object.DestroyImmediate(instance); instance = null;
            }
            File.WriteAllLines(Folder + "/result.txt", results);
            File.WriteAllText(Folder + "/status.txt", "completed");
        }
        catch (Exception e) { File.WriteAllText(Folder + "/status.txt", "failed: " + e); }
        finally { if (instance) UnityEngine.Object.Destroy(instance); }
    }
}
