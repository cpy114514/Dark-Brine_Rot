using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AutoQaCappuccinoCancellationRegression
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Private).Invoke(obj, args);
    static async Task Wait(float seconds)
    {
        float until = Time.time + seconds; var deadline = DateTime.UtcNow.AddSeconds(15);
        while (Time.time < until)
        {
            if (!Application.isPlaying || DateTime.UtcNow > deadline) throw new Exception("Native frame progress interrupted");
            await Task.Delay(5);
        }
    }
    static async Task Until(Func<bool> condition, float seconds = 3)
    {
        float until = Time.time + seconds;
        while (!condition()) { if (Time.time > until) throw new Exception("Expected native state not reached"); await Wait(.001f); }
    }
    public static async Task<object> Run(string take, bool regression = false)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene");
        if (!System.Text.RegularExpressions.Regex.IsMatch(take, "^[a-z0-9-]+$") || File.Exists("AUTO_QA/Evidence/" + take + ".json") || SessionState.GetBool("AUTO_QA.CappuccinoCancellation", false)) throw new Exception("Invalid take / preserve evidence / fixture owned");
        SessionState.SetBool("AUTO_QA.CappuccinoCancellation", true);
        var checks = new List<object>(); var cases = new List<object>(); string error = null;
        Action<bool, string> check = (ok, name) => checks.Add(new { passed = ok, name });
        int inactiveAnimatorWarnings = 0;
        Application.LogCallback log = (message, stack, type) => { if (message == "Game object with animator is inactive") inactiveAnimatorWarnings++; };
        Application.logMessageReceived += log;
        var pickups = new HashSet<EquipmentPickup>(UnityEngine.Object.FindObjectsByType<EquipmentPickup>(FindObjectsSortMode.None));
        GameObject floor = null, victim = null, boss = null;
        try
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "AUTO_QA Capri floor";
            floor.transform.position = new Vector3(2000, 499.5f, 2000); floor.transform.localScale = new Vector3(80, 1, 80);
            victim = new GameObject("AUTO_QA excluded Capri victim"); victim.AddComponent<GameSaveExcluded>(); victim.tag = "Player";
            var body = victim.AddComponent<CharacterController>(); body.height = 2; body.center = Vector3.up; body.radius = .3f;
            var hp = victim.AddComponent<PlayerHealth>(); hp.maxHealth = hp.currentHealth = 1000;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab");
            foreach (var kind in new[] { CappuccinoAI.AttackKind.TwinSlash, CappuccinoAI.AttackKind.DashThrust, CappuccinoAI.AttackKind.Whirlwind })
            foreach (string mode in regression ? new[] { "uninterrupted" } : new[] { "component", "object" })
            {
                victim.transform.position = new Vector3(2000, 500, 2004.6f); hp.currentHealth = 1000;
                boss = UnityEngine.Object.Instantiate(prefab, new Vector3(2000, 500, 2000), Quaternion.identity); boss.AddComponent<GameSaveExcluded>();
                var ai = boss.GetComponent<CappuccinoAI>(); var ultimate = boss.GetComponent<CappuccinoUltimate>(); ultimate.ultimateEnabled = false;
                ai.target = victim.transform; await Wait(.03f); Physics.SyncTransforms();
                Call(ai, "BeginEncounter", Time.time); Call(ai, "BeginWindup", kind, Time.time);
                check(ai.CurrentState == CappuccinoAI.State.Windup, kind + "/" + mode + " enters authored telegraph");
                await Until(() => ai.CurrentState == CappuccinoAI.State.Attack); await Wait(.035f);
                check(hp.currentHealth == 1000 && ai.ActionProgress < .16f, kind + "/" + mode + " native attack reached before damage window");
                if (regression)
                {
                    await Until(() => hp.currentHealth < 1000, 1.8f);
                    check(hp.currentHealth < 1000, kind + " fresh uninterrupted attack still deals damage");
                    cases.Add(new { kind = kind.ToString(), mode, damage = 1000 - hp.currentHealth });
                }
                else
                {
                    float disabledAt = Time.time;
                    if (mode == "component") ai.enabled = false; else boss.SetActive(false);
                    var stateAtDisable = ai.CurrentState; bool windowAtDisable = ai.DamageWindowActive;
                    check(stateAtDisable == CappuccinoAI.State.Idle && !windowAtDisable, kind + "/" + mode + " disabling cancels unfinished action");
                    await Wait(.18f); check(hp.currentHealth == 1000, kind + "/" + mode + " disabled owner cannot damage");
                    float enabledAt = Time.time;
                    if (mode == "component") ai.enabled = true; else boss.SetActive(true);
                    await Wait(.05f);
                    float damage = 1000 - hp.currentHealth; var stateAfterEnable = ai.CurrentState;
                    check(damage == 0, kind + "/" + mode + " no old hit within 50ms of re-enable");
                    check(stateAfterEnable != CappuccinoAI.State.Attack, kind + "/" + mode + " fresh telegraph required before next attack");
                    cases.Add(new { kind = kind.ToString(), mode, disabledAt, enabledAt, stateAtDisable = stateAtDisable.ToString(), stateAfterEnable = stateAfterEnable.ToString(), windowAtDisable, damageWithin50ms = damage });
                }
                UnityEngine.Object.DestroyImmediate(boss); boss = null;
            }
            if (regression)
            {
                boss = UnityEngine.Object.Instantiate(prefab, new Vector3(2000, 500, 2000), Quaternion.identity); boss.AddComponent<GameSaveExcluded>();
                var ai = boss.GetComponent<CappuccinoAI>(); var ultimate = boss.GetComponent<CappuccinoUltimate>(); var health = boss.GetComponent<Mavis.Health>();
                health.currentHealth = 200; float now = Time.time, cap = health.maxHealth;
                check((bool)Call(ultimate, "TryActivate", now) && ultimate.CurrentPhase == CappuccinoUltimate.Phase.Charging && ultimate.AttackLocked, "Existing ultimate enters locked charge");
                Call(ultimate, "Simulate", now + ultimate.chargeSeconds + .001f);
                check(ultimate.IsEmpowered && !ultimate.AttackLocked && health.currentHealth == 200 && health.maxHealth == cap, "Empowerment preserves current fixed-health design");
                Call(ultimate, "Simulate", now + ultimate.chargeSeconds + ultimate.empoweredSeconds + .001f);
                check(ultimate.HasHealed && health.currentHealth == 200 + ultimate.healingAmount && ultimate.AttackLocked && ultimate.MovementLocked, "Existing heal and immobilized window preserved");
                Call(ultimate, "Simulate", now + ultimate.chargeSeconds + ultimate.empoweredSeconds + ultimate.immobilizedSeconds + .001f);
                check(ultimate.CurrentPhase == CappuccinoUltimate.Phase.Spent && !ultimate.AttackLocked, "Existing ultimate completes once");
                health.Kill(); ai.enabled = false;
                check(ai.CurrentState == CappuccinoAI.State.Dead && !ai.DamageWindowActive && ultimate.CurrentPhase == CappuccinoUltimate.Phase.Spent, "Disable preserves dead state and spent ultimate");
                UnityEngine.Object.DestroyImmediate(boss); boss = null;
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            if (boss) UnityEngine.Object.DestroyImmediate(boss); if (victim) UnityEngine.Object.DestroyImmediate(victim); if (floor) UnityEngine.Object.DestroyImmediate(floor);
            foreach (var pickup in UnityEngine.Object.FindObjectsByType<EquipmentPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (!pickups.Contains(pickup)) UnityEngine.Object.DestroyImmediate(pickup.gameObject);
            SessionState.SetBool("AUTO_QA.CappuccinoCancellation", false);
            Application.logMessageReceived -= log;
        }
        check(inactiveAnimatorWarnings == 0, "Fixture and teardown produce no inactive Animator warning");
        int failedChecks = checks.Count(c => !(bool)c.GetType().GetProperty("passed").GetValue(c));
        var result = new { passed = error == null && failedChecks == 0, error, checkCount = checks.Count, failedChecks, inactiveAnimatorWarnings, checks, cases, fixtureCleared = !boss && !victim && !floor };
        File.WriteAllText("AUTO_QA/Evidence/" + take + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return new { result.passed, error, result.checkCount, failedChecks, cases, result.fixtureCleared };
    }
}
