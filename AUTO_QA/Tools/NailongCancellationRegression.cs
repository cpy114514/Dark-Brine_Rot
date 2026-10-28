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
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class AutoQaNailongCancellationRegression
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Private).SetValue(o, value);
    static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, Private).Invoke(o, args);
    static void Begin(NailongAI ai, string pattern) => Call(ai, "BeginAttack", Enum.Parse(typeof(NailongAI).GetNestedType("AttackPattern", BindingFlags.NonPublic), pattern));
    static async Task Wait(float seconds)
    {
        float end = Time.time + seconds; var deadline = DateTime.UtcNow.AddSeconds(15);
        while (Time.time < end)
        {
            if (!Application.isPlaying || DateTime.UtcNow > deadline) throw new Exception("Native frame progress interrupted");
            await Task.Delay(5);
        }
    }
    static GameObject Victim()
    {
        var obj = new GameObject("AUTO_QA excluded target-loss victim"); obj.AddComponent<GameSaveExcluded>(); obj.tag = "Player";
        obj.transform.position = new Vector3(2000, 500, 2012);
        var body = obj.AddComponent<CharacterController>(); body.height = 2; body.center = Vector3.up; body.radius = .3f;
        var hp = obj.AddComponent<PlayerHealth>(); hp.maxHealth = hp.currentHealth = 1000;
        return obj;
    }
    public static async Task<object> RunInFlight(string take)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene");
        if (!System.Text.RegularExpressions.Regex.IsMatch(take, "^[a-z0-9-]+$") || File.Exists("AUTO_QA/Evidence/" + take + ".json") || SessionState.GetBool("AUTO_QA.NailongCancellation", false)) throw new Exception("Invalid take / fixture owned");
        SessionState.SetBool("AUTO_QA.NailongCancellation", true);
        var checks = new List<object>(); string error = null; GameObject boss = null, victim = null;
        Action<bool, string> check = (ok, name) => checks.Add(new { passed = ok, name });
        try
        {
            victim = Victim();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
            boss = UnityEngine.Object.Instantiate(prefab, new Vector3(2000, 500, 2000), Quaternion.identity); boss.AddComponent<GameSaveExcluded>();
            boss.GetComponent<NailongLoot>().drops = Array.Empty<EquipmentItem>();
            var ai = boss.GetComponent<NailongAI>(); var effects = boss.GetComponent<NailongCombatEffects>(); var health = boss.GetComponent<NailongHealth>();
            await Wait(.03f); Set(ai, "target", victim.transform); Set(ai, "targetHealth", victim.GetComponent<PlayerHealth>()); Begin(ai, "GroundSlam");
            var glob = effects.Spit(victim.transform, 16, 0); effects.QueueSpit(victim.transform, 16, 0);
            effects.GroundSlam(boss.GetComponent<NailongAttack>(), 20, 3, .4f, 2.4f);
            var waves = (List<NailongGroundSlam>)typeof(NailongCombatEffects).GetField("sonars", Private).GetValue(effects); var wave = waves.Single();
            wave.Tick(.02f); check(glob && glob.gameObject.activeSelf && wave.PulsesReleased == 1, "Released glob and first wave exist before target loss");
            victim.SetActive(false); Call(ai, "Update"); Call(effects, "LateUpdate");
            check(ai.CurrentState == NailongAI.State.Idle, "Target loss exits attack with in-flight effects");
            check(glob && glob.gameObject.activeSelf && effects.ActiveProjectileCount == 1, "Target loss preserves launched glob and cancels queued glob");
            wave.Tick(.5f); check(wave && wave.PulsesReleased == 1, "Target loss preserves launched wave but stops later emissions");
            victim.SetActive(true); Set(ai, "acquireAt", Time.time - 1); Set(ai, "nextActionTime", Time.time + 10); Call(ai, "Update");
            check((Transform)typeof(NailongAI).GetField("target", Private).GetValue(ai) == victim.transform, "Returning active target is reacquired with effects in flight");
            health.ApplyDamage(health.currentHealth + 1, boss.transform.position); await Wait(.04f);
            check(effects.ActiveProjectileCount == 0 && !wave, "Death still recycles launched glob and destroys remaining waves");
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            if (boss) UnityEngine.Object.DestroyImmediate(boss); if (victim) UnityEngine.Object.DestroyImmediate(victim);
            SessionState.SetBool("AUTO_QA.NailongCancellation", false);
        }
        bool passed = error == null && checks.All(c => (bool)c.GetType().GetProperty("passed").GetValue(c));
        var result = new { passed, error, checkCount = checks.Count, checks, fixtureCleared = !boss && !victim };
        File.WriteAllText("AUTO_QA/Evidence/" + take + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented)); return result;
    }
    public static async Task<object> RunTargetLoss(string take)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene");
        if (!System.Text.RegularExpressions.Regex.IsMatch(take, "^[a-z0-9-]+$") || File.Exists("AUTO_QA/Evidence/" + take + ".json") || SessionState.GetBool("AUTO_QA.NailongCancellation", false)) throw new Exception("Invalid take / preserve evidence / fixture owned");
        SessionState.SetBool("AUTO_QA.NailongCancellation", true);
        var checks = new List<object>(); var cases = new List<object>(); string error = null;
        Action<bool, string> check = (ok, name) => checks.Add(new { passed = ok, name });
        GameObject floor = null, victim = null, boss = null;
        try
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.position = new Vector3(2000, 499.5f, 2000); floor.transform.localScale = new Vector3(80, 1, 80);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
            foreach (string loss in new[] { "inactive", "destroyed" })
            foreach (bool acquireDue in new[] { true, false })
            {
                victim = Victim(); boss = UnityEngine.Object.Instantiate(prefab, new Vector3(2000, 500, 2000), Quaternion.identity);
                boss.AddComponent<GameSaveExcluded>(); boss.GetComponent<NailongLoot>().drops = Array.Empty<EquipmentItem>();
                var ai = boss.GetComponent<NailongAI>(); var phase = boss.GetComponent<NailongPhaseAnimation>(); var effects = boss.GetComponent<NailongCombatEffects>();
                await Wait(.03f); Set(ai, "target", victim.transform); Set(ai, "targetHealth", victim.GetComponent<PlayerHealth>()); Set(ai, "engaged", true);
                Begin(ai, "ScoldingSpit"); await Wait(.15f);
                check(phase.IsPlaying && ai.CurrentState == NailongAI.State.Attack, loss + "/" + acquireDue + " attack setup");
                effects.QueueSpit(victim.transform, 16, 0);
                Set(ai, "acquireAt", Time.time + (acquireDue ? -1 : 10));
                if (loss == "inactive") victim.SetActive(false); else UnityEngine.Object.DestroyImmediate(victim);
                Call(ai, "Update"); Call(effects, "LateUpdate");
                string state = ai.CurrentState.ToString(); bool playing = phase.IsPlaying; int globs = effects.ActiveProjectileCount;
                check(ai.CurrentState == NailongAI.State.Idle && !playing, loss + "/" + acquireDue + " lost target ends attack immediately");
                check(globs == 0, loss + "/" + acquireDue + " lost target cancels pending release");
                await Wait(.06f);
                check(ai.CurrentState == NailongAI.State.Idle && !phase.IsPlaying, loss + "/" + acquireDue + " lost target remains idle on native frames");
                cases.Add(new { loss, acquireDue, stateAtLoss = state, graphAtLoss = playing, activeGlobsAtLoss = globs, stateAfterFrames = ai.CurrentState.ToString() });
                if (!victim) victim = Victim(); else victim.SetActive(true);
                Set(ai, "acquireAt", Time.time - 1); Set(ai, "nextActionTime", Time.time + 10); Call(ai, "Update");
                check((Transform)typeof(NailongAI).GetField("target", Private).GetValue(ai) == victim.transform, loss + "/" + acquireDue + " active target can be reacquired");
                UnityEngine.Object.DestroyImmediate(boss); boss = null; UnityEngine.Object.DestroyImmediate(victim); victim = null;
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            if (boss) UnityEngine.Object.DestroyImmediate(boss); if (victim) UnityEngine.Object.DestroyImmediate(victim); if (floor) UnityEngine.Object.DestroyImmediate(floor);
            SessionState.SetBool("AUTO_QA.NailongCancellation", false);
        }
        bool passed = error == null && checks.All(c => (bool)c.GetType().GetProperty("passed").GetValue(c));
        var result = new { passed, error, checkCount = checks.Count, checks, cases, fixtureCleared = !boss && !victim && !floor };
        File.WriteAllText("AUTO_QA/Evidence/" + take + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return new { passed, error, checkCount = checks.Count, failedChecks = checks.Count(c => !(bool)c.GetType().GetProperty("passed").GetValue(c)), cases };
    }
    public static async Task<object> Run(string take)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene");
        if (!System.Text.RegularExpressions.Regex.IsMatch(take, "^[a-z0-9-]+$")) throw new Exception("Invalid take");
        string path = "AUTO_QA/Evidence/" + take + ".json";
        if (File.Exists(path) || SessionState.GetBool("AUTO_QA.NailongCancellation", false)) throw new Exception("Preserve evidence / another fixture active");
        SessionState.SetBool("AUTO_QA.NailongCancellation", true);
        var checks = new List<object>(); var cases = new List<object>(); string error = null;
        Action<bool, string> check = (ok, name) => checks.Add(new { passed = ok, name });
        GameObject floor = null, victim = null, boss = null;
        try
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "AUTO_QA Nailong cancellation floor";
            floor.transform.position = new Vector3(2000, 499.5f, 2000); floor.transform.localScale = new Vector3(80, 1, 80);
            victim = new GameObject("AUTO_QA excluded victim"); victim.AddComponent<GameSaveExcluded>(); victim.tag = "Player";
            victim.transform.position = new Vector3(2000, 500, 2012);
            var body = victim.AddComponent<CharacterController>(); body.height = 2; body.center = Vector3.up; body.radius = .3f;
            var hp = victim.AddComponent<PlayerHealth>(); hp.maxHealth = hp.currentHealth = 1000;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
            foreach (string pattern in new[] { "ScoldingSpit", "GroundSlam", "Roar", "Rampage", "ShamelessCharge" })
            foreach (string exit in new[] { "heavy hit", "death", "disable AI", "return" })
            {
                boss = UnityEngine.Object.Instantiate(prefab, new Vector3(2000, 500, 2000), Quaternion.identity);
                boss.AddComponent<GameSaveExcluded>(); boss.GetComponent<NailongLoot>().drops = Array.Empty<EquipmentItem>();
                var ai = boss.GetComponent<NailongAI>(); var phase = boss.GetComponent<NailongPhaseAnimation>();
                var health = boss.GetComponent<NailongHealth>(); var effects = boss.GetComponent<NailongCombatEffects>();
                await Wait(.03f); Set(ai, "target", victim.transform); Set(ai, "targetHealth", hp); Begin(ai, pattern);
                await Wait(.15f);
                bool started = phase.IsPlaying;
                var mixer = (AnimationMixerPlayable)typeof(NailongPhaseAnimation).GetField("mixer", Private).GetValue(phase);
                float actionWeight = started ? mixer.GetInputWeight(1) : 0;
                check(started && actionWeight > .5f, pattern + "/" + exit + " real imported animation graph owns action weight");
                // Pending release must be cancelled; an already-launched glob intentionally survives a stagger.
                effects.QueueSpit(victim.transform, 16, 0);
                NailongSpitProjectile launched = exit == "heavy hit" ? effects.Spit(victim.transform, 16, 0) : null;
                if (exit == "heavy hit") health.ApplyCombatDamage(1, boss.transform.position + Vector3.forward, CombatHitKind.ChargedHeavy);
                else if (exit == "death") health.ApplyDamage(health.currentHealth + 1, boss.transform.position);
                else if (exit == "disable AI") ai.enabled = false;
                else Call(ai, "BeginReturn");
                bool playingAtExit = phase.IsPlaying; string state = ai.CurrentState.ToString();
                Call(effects, "LateUpdate");
                check(!playingAtExit, pattern + "/" + exit + " releases imported graph immediately");
                if (exit == "heavy hit") check(ai.IsStaggered && launched && launched.gameObject.activeSelf && effects.ActiveProjectileCount == 1, pattern + " stagger cancels pending spit and preserves launched glob");
                else if (exit == "death" || exit == "disable AI") check(effects.ActiveProjectileCount == 0, pattern + "/" + exit + " cancels pending and active projectiles");
                await Wait(.05f);
                check(!phase.IsPlaying, pattern + "/" + exit + " graph remains released on native frames");
                cases.Add(new { pattern, exit, started, actionWeight, state, playingAtExit, playingAfterFrames = phase.IsPlaying });
                UnityEngine.Object.DestroyImmediate(boss); boss = null; victim.SetActive(true);
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            if (boss) UnityEngine.Object.DestroyImmediate(boss);
            if (victim) UnityEngine.Object.DestroyImmediate(victim);
            if (floor) UnityEngine.Object.DestroyImmediate(floor);
            SessionState.SetBool("AUTO_QA.NailongCancellation", false);
        }
        bool passed = error == null && checks.All(c => (bool)c.GetType().GetProperty("passed").GetValue(c));
        var result = new { passed, error, checkCount = checks.Count, checks, cases, fixtureCleared = !boss && !victim && !floor };
        Directory.CreateDirectory("AUTO_QA/Evidence"); File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
