using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AutoQaExtendedSystemsRegression
{
    public static object PreserveChangedWorkspace()
    {
        if (Application.isPlaying || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != "Assets/Scenes/Combat Test/CombatTest.unity" || SceneManager.GetActiveScene().isDirty) throw new Exception("Changed workspace must be stopped and clean");
        const string key = "AUTO_QA.Batch002.SceneSetup";
        string original = SessionState.GetString(key, "");
        string current = Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup());
        string path = "AUTO_QA/Evidence/batch010-workspace-change.json";
        using (var stream = new FileStream(path, FileMode.CreateNew))
        using (var writer = new StreamWriter(stream)) writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(new { original, current, reason = "Observed workspace changed outside QA fixture; restore latest clean user workspace" }, Newtonsoft.Json.Formatting.Indented));
        SessionState.SetString("AUTO_QA.Batch010.PreviousSceneSetup", original);
        SessionState.SetString(key, current);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        return new { isolated = true, restoreLatestWorkspace = true };
    }
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object o, string name) => o.GetType().GetField(name, Private).GetValue(o);
    static void Set(object o, string name, object v) => o.GetType().GetField(name, Private).SetValue(o, v);
    static void Prop(object o, string name, object v) => o.GetType().GetProperty(name).SetValue(o, v);
    static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, Private).Invoke(o, args);
    static void Guard(string take)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene");
        if (!System.Text.RegularExpressions.Regex.IsMatch(take, "^[a-z0-9-]+$") || File.Exists("AUTO_QA/Evidence/" + take + ".json") || SessionState.GetBool("AUTO_QA.ExtendedSystems", false)) throw new Exception("Invalid take / existing evidence or fixture");
        SessionState.SetBool("AUTO_QA.ExtendedSystems", true);
    }
    static object Save(string take, List<object> checks, List<object> cases, string error)
    {
        int failed = checks.Count(c => !(bool)c.GetType().GetProperty("passed").GetValue(c));
        var result = new { passed = error == null && failed == 0, error, checkCount = checks.Count, failed, checks, cases };
        File.WriteAllText("AUTO_QA/Evidence/" + take + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return new { result.passed, error, result.checkCount, failed, cases };
    }
    static async Task Wait(float seconds)
    {
        float end = Time.time + seconds; var deadline = DateTime.UtcNow.AddSeconds(15);
        while (Time.time < end) { if (!Application.isPlaying || DateTime.UtcNow > deadline) throw new Exception("Native frame progress interrupted"); await Task.Delay(5); }
    }
    public static async Task<object> Bombardino(string take)
    {
        Guard(take); var checks = new List<object>(); var cases = new List<object>(); string error = null;
        Action<bool, string> check = (ok, name) => checks.Add(new { passed = ok, name });
        GameObject floor = null, victim = null, plane = null;
        try
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.position = new Vector3(2000, 499.5f, 2000); floor.transform.localScale = new Vector3(300, 1, 300);
            victim = new GameObject("AUTO_QA excluded aircraft victim"); victim.AddComponent<GameSaveExcluded>(); victim.transform.position = new Vector3(2000, 500, 2000);
            var body = victim.AddComponent<CharacterController>(); body.center = Vector3.up; body.height = 2; body.radius = .3f;
            var hp = victim.AddComponent<PlayerHealth>(); hp.maxHealth = hp.currentHealth = 1000;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab");
            foreach (string mode in new[] { "boss disabled", "boss dead", "inactive target", "disabled health" })
            foreach (bool acquireDue in new[] { true, false })
            {
                victim.SetActive(true); hp.enabled = true; hp.currentHealth = 1000;
                plane = UnityEngine.Object.Instantiate(prefab, new Vector3(2000, 500, 2000), Quaternion.identity); plane.AddComponent<GameSaveExcluded>();
                var boss = plane.GetComponent<BombardinoBoss>(); await Wait(.04f);
                boss.target = victim.transform; Set(boss, "targetHealth", hp); Prop(boss, "FightActive", true);
                Prop(boss, "CurrentAttack", BombardinoBoss.AttackKind.FlameRun); Call(boss, "SetState", BombardinoBoss.State.Attack);
                float clock = (float)Get(boss, "clock"); Set(boss, "acquireAt", clock + (acquireDue ? -1 : 10));
                var impacts = (IList)Get(boss, "fireImpacts"); impacts.Add((victim.transform.position, clock + .05f, 8f));
                var go = new GameObject("AUTO_QA owned fire"); go.transform.position = victim.transform.position;
                var fire = go.AddComponent<BombardinoOrdnance>(); fire.Launch(boss, BombardinoOrdnance.Kind.Fire, Vector3.down, victim.transform, victim.transform.position, 8, 3);
                var jet = plane.GetComponent<BombardinoFlameJet>(); Call(jet, "LateUpdate");
                var particles = (ParticleSystem)Get(jet, "jet"); check(particles && particles.emission.rateOverTime.constant > 0, mode + "/" + acquireDue + " flame setup active");
                int initialZones = boss.FlameZonesCreated;
                if (mode == "boss disabled") boss.enabled = false;
                else if (mode == "boss dead") plane.GetComponent<Mavis.Health>().Kill();
                else if (mode == "inactive target") victim.SetActive(false);
                else hp.enabled = false;
                if (boss.enabled) boss.Tick(.1f);
                Call(jet, "LateUpdate");
                float emission = particles.emission.rateOverTime.constant;
                int queued = ((IList)Get(boss, "fireImpacts")).Count;
                int owned = ((IList)Get(boss, "ordnance")).Count;
                check(emission == 0, mode + "/" + acquireDue + " no continuing flame emission");
                check(queued == 0 && owned == 0, mode + "/" + acquireDue + " queued and live hazards cleared");
                check(boss.FlameZonesCreated == initialZones, mode + "/" + acquireDue + " no delayed fire created after ownership loss");
                if (mode.Contains("target") || mode == "disabled health") check(!boss.FightActive && boss.CurrentState == BombardinoBoss.State.Returning, mode + "/" + acquireDue + " leaves encounter promptly");
                cases.Add(new { mode, acquireDue, emission, queued, owned, zonesCreatedAfterLoss = boss.FlameZonesCreated - initialZones, state = boss.CurrentState.ToString(), boss.FightActive });
                await Wait(.04f); check(!fire, mode + "/" + acquireDue + " native fire object retired");
                UnityEngine.Object.DestroyImmediate(plane); plane = null;
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            if (plane) UnityEngine.Object.DestroyImmediate(plane); if (victim) UnityEngine.Object.DestroyImmediate(victim); if (floor) UnityEngine.Object.DestroyImmediate(floor);
            foreach (var p in UnityEngine.Object.FindObjectsByType<BombardinoOrdnance>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(p.gameObject);
            SessionState.SetBool("AUTO_QA.ExtendedSystems", false);
        }
        return Save(take, checks, cases, error);
    }
    public static object Hotpaths(string take, bool requireAllocationCounter = true)
    {
        Guard(take); var checks = new List<object>(); var cases = new List<object>(); string error = null;
        Action<bool, string> check = (ok, name) => checks.Add(new { passed = ok, name });
        GameObject footprints = null, camera = null; float scale = Time.timeScale;
        try
        {
            footprints = new GameObject("AUTO_QA measured footprints"); var component = footprints.AddComponent<SandFootprints>(); component.enabled = false;
            var renderers = component.GetComponentsInChildren<MeshRenderer>(true); var method = typeof(SandFootprints).GetMethod("SetAlpha", Private | BindingFlags.Static);
            var alpha = (Action<MeshRenderer, float>)(method.IsStatic ? Delegate.CreateDelegate(typeof(Action<MeshRenderer, float>), method) : Delegate.CreateDelegate(typeof(Action<MeshRenderer, float>), component, method));
            long controlStart = GC.GetAllocatedBytesForCurrentThread(); var control = new byte[1024 * 1024]; GC.KeepAlive(control);
            long controlBytes = GC.GetAllocatedBytesForCurrentThread() - controlStart;
            for (int i = 0; i < 100; i++) alpha(renderers[0], .5f);
            long before = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 10000; i++) alpha(renderers[i % renderers.Length], .5f);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (requireAllocationCounter) check(controlBytes > 0, "Allocation counter positive control detects one megabyte allocation");
            if (controlBytes > 0) check(allocated == 0, "10000 warmed bound footprint alpha calls allocate zero managed bytes");
            cases.Add(new { system = "SandFootprints.SetAlpha", calls = 10000, allocated, controlBytes, validAllocationCounter = controlBytes > 0, measurement = "Bound delegate, native renderer, warmed; no per-call reflection" });
            var block = new MaterialPropertyBlock(); block.SetFloat("_AutoQaSentinel", 42); renderers[0].SetPropertyBlock(block);
            block.Clear(); block.SetFloat("_AutoQaSentinel", 7); renderers[1].SetPropertyBlock(block);
            alpha(renderers[0], .5f); alpha(renderers[1], .25f); renderers[0].GetPropertyBlock(block);
            check(Mathf.Abs(block.GetColor("_Color").a - .31f) < .0001f && block.GetFloat("_AutoQaSentinel") == 42, "First renderer retains original alpha and unrelated properties");
            renderers[1].GetPropertyBlock(block); check(Mathf.Abs(block.GetColor("_Color").a - .155f) < .0001f && block.GetFloat("_AutoQaSentinel") == 7, "Second renderer has independent properties");
            var cache = typeof(SandFootprints).GetField("stampProperties", Private);
            if (cache != null) { cache.SetValue(component, null); alpha(renderers[0], 1); check(cache.GetValue(component) != null, "Lost nonserialized property-block cache recovers lazily"); }
            var entries = (Array)Get(component, "stamps"); var entry = entries.GetValue(0); entry.GetType().GetField("born").SetValue(entry, Time.time - component.lifetime * .85f); entries.SetValue(entry, 0);
            renderers[0].gameObject.SetActive(true); Call(component, "Fade"); renderers[0].GetPropertyBlock(block);
            check(Mathf.Abs(block.GetColor("_Color").a - .62f * (1 - (.85f - .72f) / .28f)) < .0001f, "Original lifetime fade curve preserved");
            entry.GetType().GetField("born").SetValue(entry, Time.time - component.lifetime - 1); entries.SetValue(entry, 0); Call(component, "Fade");
            check(!renderers[0].gameObject.activeSelf && renderers.Length == 28, "Expiry and bounded 28-stamp pool preserved");
            camera = new GameObject("AUTO_QA camera offset fixture"); var shake = camera.AddComponent<CombatCameraShake>();
            Vector3 position = new Vector3(10, 20, 30); Quaternion rotation = Quaternion.Euler(15, 38, 9); camera.transform.SetPositionAndRotation(position, rotation);
            for (int i = 0; i < 100; i++) { shake.PulseDirectional(new Vector3(1, 0, 1), .08f, .2f); Call(shake, "LateUpdate"); Call(shake, "Update"); }
            check(Vector3.Distance(camera.transform.position, position) < .0002f && Quaternion.Angle(camera.transform.rotation, rotation) < .02f, "100 camera impact/remove cycles preserve base pose without drift");
            shake.PulseDirectional(Vector3.forward, .1f, .2f); Call(shake, "LateUpdate"); Time.timeScale = 0; float remaining = (float)Get(shake, "remaining"); Call(shake, "Update"); Call(shake, "LateUpdate");
            check(camera.transform.position == position && Mathf.Approximately((float)Get(shake, "remaining"), remaining), "Pause removes camera offset and preserves shake lifetime");
            Time.timeScale = scale; shake.enabled = false;
            check(!shake.IsShaking && Vector3.Distance(camera.transform.position, position) < .0002f, "Disabling camera feedback releases offset and timer");
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally { Time.timeScale = scale; if (footprints) UnityEngine.Object.DestroyImmediate(footprints); if (camera) UnityEngine.Object.DestroyImmediate(camera); SessionState.SetBool("AUTO_QA.ExtendedSystems", false); }
        return Save(take, checks, cases, error);
    }
    public static object Ballistics(string take)
    {
        Guard(take); var checks = new List<object>(); var cases = new List<object>(); string error = null;
        Action<bool, string> check = (ok, name) => checks.Add(new { passed = ok, name });
        GameObject plane = null, projectile = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab");
            plane = UnityEngine.Object.Instantiate(prefab, new Vector3(2000, 500, 2000), Quaternion.identity); plane.AddComponent<GameSaveExcluded>(); var boss = plane.GetComponent<BombardinoBoss>();
            Vector3 origin = new Vector3(100, 500, 100), aim = origin + new Vector3(10, -100, 0);
            Vector3 expected = origin + Vector3.right * (10 / Mathf.Sqrt(200f / 25)) + Vector3.down * 12.5f;
            foreach (int partitions in new[] { 1, 10, 30, 60 })
            {
                projectile = new GameObject("AUTO_QA ballistic partition"); projectile.transform.position = origin;
                var bomb = projectile.AddComponent<BombardinoOrdnance>(); bomb.Launch(boss, BombardinoOrdnance.Kind.Bomb, Vector3.down, null, aim, 0, 3);
                for (int i = 0; i < partitions; i++) bomb.Tick(1f / partitions);
                float simulatedAge = (float)Get(bomb, "age"), distance = Vector3.Distance(projectile.transform.position, expected);
                check(Mathf.Abs(simulatedAge - 1) < .0001f, partitions + " partitions consume full second");
                check(distance < .003f, partitions + " partitions match authored constant-gravity trajectory");
                cases.Add(new { partitions, simulatedAge, trajectoryError = distance, position = projectile.transform.position.ToString("F5"), expected = expected.ToString("F5") });
                UnityEngine.Object.DestroyImmediate(projectile); projectile = null;
            }
            projectile = new GameObject("AUTO_QA fire lifetime"); projectile.transform.position = origin;
            var fire = projectile.AddComponent<BombardinoOrdnance>(); fire.Launch(boss, BombardinoOrdnance.Kind.Fire, Vector3.down, null, origin, 0, 3); fire.Tick(5.1f);
            bool expired = (bool)Get(fire, "resolved"); check(expired, "Ground fire consumes full elapsed lifetime on a long frame"); cases.Add(new { fireLongFrameExpired = expired, simulatedAge = (float)Get(fire, "age") });
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            if (projectile) UnityEngine.Object.DestroyImmediate(projectile); if (plane) UnityEngine.Object.DestroyImmediate(plane);
            SessionState.SetBool("AUTO_QA.ExtendedSystems", false);
        }
        return Save(take, checks, cases, error);
    }
    public static object Contacts(string take)
    {
        Guard(take); var checks = new List<object>(); var cases = new List<object>(); string error = null;
        Action<bool, string> check = (ok, name) => checks.Add(new { passed = ok, name });
        GameObject plane = null, projectile = null, wall = null, victim = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab");
            plane = UnityEngine.Object.Instantiate(prefab, new Vector3(2000, 500, 2000), Quaternion.identity); plane.AddComponent<GameSaveExcluded>(); var boss = plane.GetComponent<BombardinoBoss>();
            Vector3 origin = new Vector3(100, 500, 100);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = origin + Vector3.right; wall.transform.localScale = new Vector3(.02f, 40, 8); Physics.SyncTransforms();
            foreach (var kind in new[] { BombardinoOrdnance.Kind.Bomb, BombardinoOrdnance.Kind.Missile })
            {
                projectile = new GameObject("AUTO_QA thin wall sweep"); projectile.transform.position = origin;
                var payload = projectile.AddComponent<BombardinoOrdnance>(); payload.Launch(boss, kind, Vector3.right, null, origin + new Vector3(10, -100, 0), 0, 3);
                payload.Tick(1); check((bool)Get(payload, "resolved") && projectile.transform.position.x < wall.transform.position.x + .3f, kind + " long frame sweeps into thin wall");
                UnityEngine.Object.DestroyImmediate(projectile); projectile = null;
            }
            UnityEngine.Object.DestroyImmediate(wall); wall = null;
            victim = new GameObject("AUTO_QA excluded blast victim"); victim.AddComponent<GameSaveExcluded>(); victim.transform.position = origin + Vector3.right * 2;
            var body = victim.AddComponent<CharacterController>(); body.center = Vector3.up; body.height = 2; body.radius = .3f;
            var hp = victim.AddComponent<PlayerHealth>(); hp.maxHealth = hp.currentHealth = 1000;
            var extra = new GameObject("Second physical hurtbox"); extra.transform.SetParent(victim.transform, false); extra.AddComponent<BoxCollider>().center = Vector3.up;
            Physics.SyncTransforms();
            projectile = new GameObject("AUTO_QA deduplicated blast"); projectile.transform.position = origin;
            var bomb = projectile.AddComponent<BombardinoOrdnance>(); bomb.Launch(boss, BombardinoOrdnance.Kind.Bomb, Vector3.right, victim.transform, origin + Vector3.right * 10, 20, 5);
            float initialAge = (float)Get(bomb, "age"); bomb.Tick(0); check((float)Get(bomb, "age") == initialAge && projectile.transform.position == origin, "Zero-delta payload tick preserves state");
            bomb.Detonate(); float after = hp.currentHealth; bomb.Detonate();
            check(after < 1000 && after >= 980, "Blast damages real victim once despite multiple hurtboxes"); check(hp.currentHealth == after, "Repeated detonation cannot duplicate damage");
            cases.Add(new { blastDamage = 1000 - after }); UnityEngine.Object.DestroyImmediate(projectile); projectile = null;
            hp.currentHealth = 1000; Set(hp, "recoveryUntil", -1f);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = origin + Vector3.right; wall.transform.localScale = new Vector3(.1f, 8, 8); Physics.SyncTransforms();
            projectile = new GameObject("AUTO_QA cover blast"); projectile.transform.position = origin; bomb = projectile.AddComponent<BombardinoOrdnance>(); bomb.Launch(boss, BombardinoOrdnance.Kind.Bomb, Vector3.right, victim.transform, origin, 20, 5); bomb.Detonate();
            check(hp.currentHealth == 1000, "Solid cover still blocks blast damage"); UnityEngine.Object.DestroyImmediate(projectile); projectile = null; UnityEngine.Object.DestroyImmediate(wall); wall = null;
            victim.transform.position = origin + new Vector3(2, 3, 0); Physics.SyncTransforms();
            projectile = new GameObject("AUTO_QA ground fire height"); projectile.transform.position = origin; var fire = projectile.AddComponent<BombardinoOrdnance>(); fire.Launch(boss, BombardinoOrdnance.Kind.Fire, Vector3.down, victim.transform, origin, 8, 5); fire.Tick(.02f);
            check(hp.currentHealth == 1000, "Floor fire does not burn a victim above its height band"); UnityEngine.Object.DestroyImmediate(projectile); projectile = null;
            victim.transform.position = origin + Vector3.right * 2; Physics.SyncTransforms();
            projectile = new GameObject("AUTO_QA ground fire cadence"); projectile.transform.position = origin; fire = projectile.AddComponent<BombardinoOrdnance>(); fire.Launch(boss, BombardinoOrdnance.Kind.Fire, Vector3.down, victim.transform, origin, 8, 5); fire.Tick(.02f);
            float pulse = Mathf.Max(1, Mathf.Round(8 * CombatFeelProfile.Default.incomingDamageMultiplier));
            check(hp.currentHealth == 1000 - pulse, "Floor fire preserves authored pulse through existing player mitigation");
            // Tick is stepped directly here; release the independent player recovery clock to isolate fire cadence.
            Set(hp, "recoveryUntil", -1f); fire.Tick(.05f); check(hp.currentHealth == 1000 - pulse, "Floor fire cannot repeat within its cooldown");
            Set(hp, "recoveryUntil", -1f); fire.Tick(.65f); check(hp.currentHealth == 1000 - 2 * pulse, "Floor fire repeats after authored cooldown");
            Set(hp, "recoveryUntil", -1f); fire.Tick(5.1f);
            check((bool)Get(fire, "resolved") && hp.currentHealth == 1000 - 2 * pulse, "Expired long-frame fire cannot deliver a final late pulse");
            cases.Add(new { authoredPulse = 8, mitigatedPulse = pulse, playerRecoveryClearedBetweenDirectSteps = true });
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally
        {
            if (projectile) UnityEngine.Object.DestroyImmediate(projectile); if (wall) UnityEngine.Object.DestroyImmediate(wall); if (victim) UnityEngine.Object.DestroyImmediate(victim); if (plane) UnityEngine.Object.DestroyImmediate(plane);
            SessionState.SetBool("AUTO_QA.ExtendedSystems", false);
        }
        return Save(take, checks, cases, error);
    }
}
