using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(2000)]
public sealed class PigRoninMotionProbe : MonoBehaviour
{
    public Transform actor, hand, sword;
    public float worstSwordStep, worstHandStep;
    Quaternion lastSword;
    Vector3 lastHand;
    bool sampled;
    public void ResetSamples() { sampled = false; worstSwordStep = worstHandStep = 0; }
    void LateUpdate()
    {
        if (!actor || !hand || !sword || Time.deltaTime <= 0 || Time.deltaTime > .12f) return;
        Quaternion rotation = Quaternion.Inverse(actor.rotation) * sword.rotation;
        Vector3 position = actor.InverseTransformPoint(hand.position);
        if (sampled)
        {
            worstSwordStep = Mathf.Max(worstSwordStep, Quaternion.Angle(lastSword, rotation));
            worstHandStep = Mathf.Max(worstHandStep, Vector3.Distance(lastHand, position) * Mathf.Abs(actor.lossyScale.x));
        }
        lastSword = rotation; lastHand = position; sampled = true;
    }
}

/// <summary>In-play encounter smoke test; affects runtime objects only.</summary>
public static class VerifyPigRoninEncounter
{
    public static string Status = "idle";
    public static string Result = "";
    static Mouse mouse;
    static Keyboard keyboard;
    static bool click;
    static void Inject()
    {
        if (mouse == null || keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        mouse.MakeCurrent(); keyboard.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        var state = new MouseState(); if (click) state = state.WithButton(MouseButton.Left);
        InputState.Change(mouse, state); InputState.Change(keyboard, new KeyboardState());
    }
    static async Task<bool> Until(Func<bool> test, int timeoutMs)
    {
        int elapsed = 0;
        while (!test() && elapsed < timeoutMs) { await Task.Delay(50); elapsed += 50; }
        return test();
    }
    public static async void Run()
    {
        Status = "running"; Result = "";
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Enter play mode before running the review.");
            typeof(Mavis.GameSaveManager).GetField("editorSavePath",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,Path.GetFullPath("Temp/pig-ronin-test.json"));
            var awakening = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
            if (awakening) UnityEngine.Object.DestroyImmediate(awakening.gameObject);
            var marshmallows = GameObject.Find("Marshmallow Friends - Second Island");
            if (marshmallows) marshmallows.SetActive(false);
            var boss = UnityEngine.Object.FindFirstObjectByType<PigRoninBoss>();
            var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var hp = player.GetComponent<Mavis.PlayerHealth>();
            var body = player.GetComponent<CharacterController>();
            var bossHp = boss.GetComponent<Mavis.Health>();
            if (!boss || !player || !hp || !body || !bossHp) throw new Exception("Encounter objects missing.");
            var visual = boss.GetComponent<PigRoninBossVisual>();
            if (!visual || !visual.HasBlade || !boss.CompareTag("Enemy")) throw new Exception("Boss rig, sword, or enemy tag missing.");
            var skin = boss.GetComponentInChildren<SkinnedMeshRenderer>();
            var arm = Array.Find(skin.bones, x => x && x.name == "RightUpperArm");
            var forearm = Array.Find(skin.bones, x => x && x.name == "RightLowerArm");
            var hand = Array.Find(skin.bones, x => x && x.name == "RightHand");
            var hips = Array.Find(skin.bones, x => x && x.name == "Hips");
            var chest = Array.Find(skin.bones, x => x && x.name == "Chest");
            var sword = boss.transform.Find("Drawn katana");
            var trail=sword ? sword.GetComponentInChildren<TrailRenderer>(true) : null;
            if(!trail)throw new Exception("Sword motion trail missing.");
            var bladePart = sword ? sword.Find("Blade") : null;
            var grip = sword ? sword.Find("Grip") : null;
            var gripHand = sword ? sword.Find("Gripping hand") : null;
            if (!arm || !forearm || !hand || !hips || !chest || !sword || !bladePart || !grip || !gripHand ||
                !gripHand.GetComponent<MeshFilter>() || !gripHand.GetComponent<MeshFilter>().sharedMesh)
                throw new Exception("Animated arm, fingered grip or sword is missing.");
            var probeObject = new GameObject("Sword clearance test");
            probeObject.layer = 2;
            var probe = probeObject.AddComponent<CapsuleCollider>();
            probe.direction = 2; probe.radius = .1f; probe.isTrigger = true;
            probe.height = bladePart.lossyScale.z;
            var motion = probeObject.AddComponent<PigRoninMotionProbe>();
            motion.actor = boss.transform; motion.hand = hand; motion.sword = sword;
            int handIndex = Array.IndexOf(skin.bones, hand), weightedVertex = -1;
            var weights = skin.sharedMesh.boneWeights;
            for (int i = 0; i < weights.Length; i++)
            {
                var w = weights[i];
                if (w.boneIndex0 == handIndex && w.weight0 > .8f) { weightedVertex = i; break; }
            }
            if (weightedVertex < 0) throw new Exception("No hand-weighted mesh vertex found.");
            var baked = new Mesh();
            Directory.CreateDirectory("Temp/PigRoninReview");

            void Place(float distance)
            {
                Vector3 pos = boss.transform.position + boss.transform.forward * distance;
                float best = float.PositiveInfinity;
                foreach (var hit in Physics.RaycastAll(pos + Vector3.up * 10, Vector3.down, 30, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.collider || hit.collider.transform.IsChildOf(boss.transform) ||
                        hit.collider.transform.IsChildOf(player.transform) ||
                        Mavis.CombatBodyCollision.IsActor(hit.collider) || hit.normal.y < .6f || hit.distance >= best) continue;
                    best = hit.distance;
                    pos.y = hit.point.y - (body.center.y - body.height * .5f) * Mathf.Abs(player.transform.lossyScale.y) + .05f;
                }
                player.RestoreSavedPose(pos, Quaternion.LookRotation(-boss.transform.forward));
                hp.currentHealth = hp.maxHealth;
            }
            Place(4.5f);
            if (!await Until(() => boss.FightActive && boss.HudVisible, 6000)) throw new Exception("Boss did not engage or show its health bar.");
            Result += "engagement and HUD OK; ";
            boss.enabled = false;
            hp.GrantProtection(8);
            Place(3f);
            var attack = player.GetComponent<Mavis.SahurAttack>();
            keyboard = Keyboard.current; mouse = Mouse.current;
            if (keyboard == null || mouse == null || !attack) throw new Exception("Player input or attack is unavailable.");
            InputSystem.onBeforeUpdate += Inject;
            if (!await Until(() => player.CanUseGroundAttack && !attack.IsCombatMotionActive, 8000))
                throw new Exception("Player could not perform a grounded attack.");
            await Task.Delay(150);
            float beforeBoss = bossHp.currentHealth;
            click = true; await Task.Delay(110); click = false;
            if (!await Until(() => bossHp.currentHealth < beforeBoss, 4000))
                throw new Exception("A real player sword attack did not hit the boss.");
            Result += "player sword damage OK; ";
            InputSystem.onBeforeUpdate -= Inject;
            boss.enabled = true;
            await Task.Delay(1000);
            foreach (PigRoninBoss.AttackKind kind in Enum.GetValues(typeof(PigRoninBoss.AttackKind)))
            {
                if (!await Until(() => boss.CurrentState != PigRoninBoss.State.Attack &&
                    boss.CurrentState != PigRoninBoss.State.Windup && hp.CanReceiveCombatDamage, 12000))
                    throw new Exception(kind + " did not become ready.");
                Place(kind == PigRoninBoss.AttackKind.Dash ? 6f : 4.2f);
                Quaternion startArm = Quaternion.Inverse(boss.transform.rotation) * arm.rotation;
                Quaternion startSword = Quaternion.Inverse(boss.transform.rotation) * sword.rotation;
                Quaternion startTorso = Quaternion.Inverse(hips.rotation) * chest.rotation;
                Vector3 startHand = boss.transform.InverseTransformPoint(hand.position);
                skin.BakeMesh(baked); Vector3 startVertex = baked.vertices[weightedVertex];
                motion.ResetSamples();
                if (!boss.TryBeginAttack(kind)) throw new Exception("Could not start " + kind);
                float before = hp.currentHealth;
                int landed = boss.HitsLanded;
                await Task.Delay(180);
                if (hp.currentHealth < before || boss.GetComponentsInChildren<LineRenderer>(true).Length != 0)
                    throw new Exception(kind + " damaged during windup or displayed an attack warning.");
                ScreenCapture.CaptureScreenshot("Temp/PigRoninReview/" + kind + "-windup.png");
                float maxArm = 0, maxSword = 0, maxHand = 0, maxVertex = 0, separation = 0, maxWristGap = 0, maxTorsoTwist = 0, maxPenetration = 0;
                bool sawAttack = false, sawTrail=false;
                int samples = 0;
                while (boss.CurrentState == PigRoninBoss.State.Windup || boss.CurrentState == PigRoninBoss.State.Attack)
                {
                    if (boss.CurrentState == PigRoninBoss.State.Attack && !sawAttack)
                    { sawAttack = true; ScreenCapture.CaptureScreenshot("Temp/PigRoninReview/" + kind + "-attack.png"); }
                    if(boss.DamageWindowActive && trail.emitting)sawTrail=true;
                    maxArm = Mathf.Max(maxArm, Quaternion.Angle(startArm, Quaternion.Inverse(boss.transform.rotation) * arm.rotation));
                    maxSword = Mathf.Max(maxSword, Quaternion.Angle(startSword, Quaternion.Inverse(boss.transform.rotation) * sword.rotation));
                    maxHand = Mathf.Max(maxHand, Vector3.Distance(startHand, boss.transform.InverseTransformPoint(hand.position)));
                    Vector3 expectedGrip = hand.position + (hand.position - forearm.position).normalized * (.0833f * Mathf.Abs(boss.transform.lossyScale.x));
                    separation = Mathf.Max(separation, Vector3.Distance(expectedGrip, grip.position));
                    Vector3 palm = gripHand.TransformPoint(new Vector3(-.065f, -.01f, .08f));
                    maxWristGap = Mathf.Max(maxWristGap, Vector3.Distance(hand.position, palm));
                    if (gripHand.parent != sword || gripHand.localPosition.sqrMagnitude > .000001f)
                        throw new Exception("The fingers separated from the sword grip.");
                    maxTorsoTwist = Mathf.Max(maxTorsoTwist, Quaternion.Angle(startTorso, Quaternion.Inverse(hips.rotation) * chest.rotation));
                    if (Physics.ComputePenetration(probe, bladePart.position, bladePart.rotation,
                        body, body.transform.position, body.transform.rotation, out _, out float depth))
                        maxPenetration = Mathf.Max(maxPenetration, depth);
                    if ((samples++ % 3) == 0)
                    { skin.BakeMesh(baked); maxVertex = Mathf.Max(maxVertex, Vector3.Distance(startVertex, baked.vertices[weightedVertex])); }
                    if (samples > 120) throw new Exception(kind + " animation stalled.");
                    await Task.Delay(50);
                }
                if (!sawAttack || maxArm < 15 || maxSword < 15 || maxHand < .04f || maxVertex < .01f || separation > .03f)
                    throw new Exception($"{kind} visual movement failed: arm={maxArm:F1}, sword={maxSword:F1}, hand={maxHand:F3}, skin={maxVertex:F3}, separation={separation:F3}");
                if(!sawTrail)throw new Exception(kind+" did not show a sword trail during the strike.");
                if (maxWristGap > .42f) throw new Exception($"{kind} hand disconnected from the wrist: {maxWristGap:F2}.");
                if (maxPenetration > .05f) throw new Exception($"{kind} blade penetrated the player capsule by {maxPenetration:F2}.");
                if (motion.worstSwordStep > 50 || motion.worstHandStep > .55f)
                    throw new Exception($"{kind} pose jumped: sword {motion.worstSwordStep:F1} degrees, hand {motion.worstHandStep:F2} units in one frame.");
                if (kind == PigRoninBoss.AttackKind.Spin && maxTorsoTwist > 35)
                    throw new Exception($"Spin twisted the upper body relative to the hips by {maxTorsoTwist:F1} degrees.");
                if (boss.HitsLanded <= landed || hp.currentHealth >= before)
                    throw new Exception(kind + " missed its target: before=" + before + ", after=" + hp.currentHealth + ", landed=" + boss.HitsLanded + ", boss=" + boss.transform.position + ", player=" + player.transform.position);
                Result += $"{kind} OK (wrist {maxWristGap:F2}, sword step {motion.worstSwordStep:F0}°, hand step {motion.worstHandStep:F2}); ";
            }
            // Test close range separately: both character capsules must remain
            // separated and the drawn blade must not pass through Sahur.
            hp.GrantProtection(5);
            Place(2.5f);
            if (!boss.TryBeginAttack(PigRoninBoss.AttackKind.Spin)) throw new Exception("Close-range spin could not start.");
            float closeBlade = 0, closeBodies = 0, torso = 0;
            Quaternion closeTorso = Quaternion.Inverse(hips.rotation) * chest.rotation;
            int closeSamples = 0;
            while (boss.CurrentState == PigRoninBoss.State.Windup || boss.CurrentState == PigRoninBoss.State.Attack)
            {
                if (Physics.ComputePenetration(probe, bladePart.position, bladePart.rotation,
                    body, body.transform.position, body.transform.rotation, out _, out float bladeDepth))
                    closeBlade = Mathf.Max(closeBlade, bladeDepth);
                var bossBody = boss.GetComponent<CharacterController>();
                if (Physics.ComputePenetration(bossBody, bossBody.transform.position, bossBody.transform.rotation,
                    body, body.transform.position, body.transform.rotation, out _, out float bodyDepth))
                    closeBodies = Mathf.Max(closeBodies, bodyDepth);
                torso = Mathf.Max(torso, Quaternion.Angle(closeTorso, Quaternion.Inverse(hips.rotation) * chest.rotation));
                if (++closeSamples > 120) throw new Exception("Close-range spin stalled.");
                await Task.Delay(50);
            }
            if (closeBlade > .05f || closeBodies > .12f || torso > 35)
                throw new Exception($"Close-range spin: blade overlap {closeBlade:F2}, body overlap {closeBodies:F2}, torso twist {torso:F1}.");
            Result += $"close range OK (blade {closeBlade:F2}, bodies {closeBodies:F2}, torso {torso:F0}°); ";
            UnityEngine.Object.Destroy(baked);
            UnityEngine.Object.Destroy(probeObject);
            bossHp.Kill();
            await Task.Delay(200);
            if (boss.CurrentState != PigRoninBoss.State.Dead || boss.FightActive || boss.GetComponent<CharacterController>().enabled)
                throw new Exception("Boss death did not end the encounter.");
            var guide=UnityEngine.Object.FindFirstObjectByType<IslandActivityGuide>();
            if(!guide || !guide.VisibleTip.Contains("guarding the drifting path"))throw new Exception("Fairy's victory story did not appear.");
            if (!await Until(() => !boss.HudVisible, 5000)) throw new Exception("Boss HUD did not disappear.");
            Result += "death and HUD cleanup OK";
            Status = "passed";
        }
        catch (Exception e) { Result += "FAILED: " + e; Status = "failed"; }
        finally { click = false; InputSystem.onBeforeUpdate -= Inject; }
        File.WriteAllText("Temp/PigRoninReview-result.txt", Status + "\n" + Result);
        Debug.Log("Pig Ronin review: " + Status + " " + Result);
    }
}
