using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class VerifyCombatHitFeedback
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static async Task<object> Verify()
    {
        Require(Application.isPlaying, "Play mode required");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        var boss = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 500, 0), Quaternion.identity);
        var attacker = new GameObject("Feedback test attacker");
        var cameraObject = new GameObject("Isolated feedback camera");
        var shake = cameraObject.AddComponent<CombatCameraShake>();
        GameObject player = null, hurtbox = null;
        try
        {
            var ai = boss.GetComponent<NailongAI>();
            ai.enabled = false;
            var hp = boss.GetComponent<NailongHealth>();
            hp.maxHealth = hp.currentHealth = 1000f;
            var reaction = boss.GetComponent<NailongHitReaction>();
            var pattern = typeof(NailongAI).GetNestedType("AttackPattern", BindingFlags.NonPublic);
            var begin = typeof(NailongAI).GetMethod("BeginAttack", Private);
            var tick = typeof(NailongAI).GetMethod("TickAttack", Private);
            var state = typeof(NailongAI).GetField("state", Private);
            var active = typeof(NailongAI).GetField("activeAttack", Private);
            foreach (string move in new[] { "ClawFlurry", "Roar", "ScoldingSpit" })
            {
                state.SetValue(ai, NailongAI.State.Idle);
                begin.Invoke(ai, new[] { Enum.Parse(pattern, move) });
                CombatHitFeedback.Apply(attacker, hp, 25f, boss.transform.position + boss.transform.right, CombatHitKind.ComboOne);
                Require(ai.CurrentState == NailongAI.State.Attack && reaction.IsReacting, "Light hit cancelled attack or missed reaction");
                CombatHitFeedback.Apply(attacker, hp, 55f, boss.transform.position, CombatHitKind.ChargedHeavy);
                Require(ai.IsStaggered && active.GetValue(ai).ToString() == "None", "Heavy did not cancel " + move);
                tick.Invoke(ai, null);
                begin.Invoke(ai, new[] { Enum.Parse(pattern, move) });
                Require(ai.IsStaggered, "Attack resumed during stagger");
            }
            state.SetValue(ai, NailongAI.State.Idle);
            begin.Invoke(ai, new[] { Enum.Parse(pattern, "Roar") });
            CombatHitFeedback.Apply(attacker, hp, 42.5f, boss.transform.position, CombatHitKind.JumpSlash);
            Require(ai.IsStaggered, "Jump slash did not interrupt");
            var feedback = attacker.GetComponent<CombatHitFeedback>();
            Require(feedback.HitCount == 7 && feedback.LastHitKind == CombatHitKind.JumpSlash, "Feedback count/kind mismatch");
            var clips = (AudioClip[])typeof(CombatHitFeedback).GetField("clips", Private).GetValue(feedback);
            Require(clips.Length == 6 && clips[3].length > clips[0].length, "Impact audio variants missing");
            foreach (var clip in clips)
            {
                var pcm = new float[clip.samples]; clip.GetData(pcm, 0);
                float peak = 0f; foreach (var value in pcm) peak = Mathf.Max(peak, Mathf.Abs(value));
                Require(peak > 0.05f && peak <= 1f, "Silent/clipped audio");
            }
            var skin = boss.GetComponentInChildren<SkinnedMeshRenderer>();
            Transform spine = null;
            foreach (var bone in skin.bones) if (bone != null && bone.name == "Spine2") spine = bone;
            Require(spine != null, "Reaction bone missing");
            await Task.Delay(80);
            var baseline = spine.localRotation;
            typeof(NailongHitReaction).GetMethod("LateUpdate", Private).Invoke(reaction, null);
            Require(Quaternion.Angle(baseline, spine.localRotation) > 0.1f, "Hit reaction did not animate bones");
            var basePosition = cameraObject.transform.position;
            var baseRotation = cameraObject.transform.rotation;
            shake.Pulse(0.065f, 0.18f);
            typeof(CombatCameraShake).GetMethod("LateUpdate", Private).Invoke(shake, null);
            Require(Vector3.Distance(basePosition, cameraObject.transform.position) < 0.1f, "Shake too strong");
            typeof(CombatCameraShake).GetMethod("RemoveOffset", Private).Invoke(shake, null);
            Require(Vector3.Distance(basePosition, cameraObject.transform.position) < 0.00001f
                && Quaternion.Angle(baseRotation, cameraObject.transform.rotation) < 0.001f, "Shake left camera drift");
            ai.enabled = true;
            await Task.Delay(800);
            Require(!ai.IsStaggered && !reaction.IsReacting, "Stagger/recoil never recovered");
            ai.enabled = false;
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
            player = UnityEngine.Object.Instantiate(playerPrefab, new Vector3(0, 500, 0), Quaternion.identity);
            player.GetComponent<ThirdPersonPlayerController>().enabled = false;
            var melee = player.GetComponent<SahurAttack>();
            melee.enabled = false;
            melee.stickHitbox.enabled = true;
            melee.hitMask = ~0;
            hurtbox = new GameObject("Feedback melee hurtbox");
            hurtbox.transform.SetParent(boss.transform);
            hurtbox.transform.position = melee.stickHitbox.bounds.center;
            var box = hurtbox.AddComponent<BoxCollider>();
            box.size = Vector3.one;
            var hits = (System.Collections.Generic.HashSet<IDamageable>)typeof(SahurAttack).GetField("hitThisSwing", Private).GetValue(melee);
            foreach (var kind in new[] { CombatHitKind.ComboOne, CombatHitKind.ComboTwo, CombatHitKind.ComboFinisher, CombatHitKind.ChargedHeavy, CombatHitKind.JumpSlash })
            {
                hits.Clear();
                typeof(SahurAttack).GetField("comboStep", Private).SetValue(melee, (int)kind <= 2 ? (int)kind : -1);
                string hashField = kind == CombatHitKind.ChargedHeavy ? "heavyStateHash" : kind == CombatHitKind.JumpSlash ? "jumpSlashStateHash" : "lightStateHash";
                typeof(SahurAttack).GetField("activeAttackHash", Private).SetValue(melee, typeof(SahurAttack).GetField(hashField, Private).GetValue(melee));
                typeof(SahurAttack).GetField("swingDamage", Private).SetValue(melee, 10f);
                Physics.SyncTransforms();
                float previousHealth = hp.currentHealth;
                var hitMethod = typeof(SahurAttack).GetMethod("TryHit", Private);
                hitMethod.Invoke(melee, new object[] { box });
                hitMethod.Invoke(melee, new object[] { box });
                var meleeFeedback = player.GetComponent<CombatHitFeedback>();
                Require(Mathf.Abs(previousHealth - hp.currentHealth - 10f) < 0.01f && meleeFeedback != null
                    && meleeFeedback.LastHitKind == kind, "Melee routing/dedup failed: " + kind);
            }
            int count = feedback.HitCount;
            hp.currentHealth = 0f;
            Require(!CombatHitFeedback.Apply(attacker, hp, 25f, Vector3.zero, CombatHitKind.ChargedHeavy)
                && feedback.HitCount == count && !reaction.IsReacting, "Dead target still emits hits");
            return new { success = true, interruptedMoves = 3, lightPreservesAttack = true,
                jumpInterrupt = true, audioVariants = clips.Length, reactionBones = true, cameraNoDrift = true, recovered = true, meleeKindsAndDedup = 5 };
        }
        finally
        {
            UnityEngine.Object.Destroy(boss);
            UnityEngine.Object.Destroy(attacker);
            UnityEngine.Object.Destroy(cameraObject);
            if (player != null) UnityEngine.Object.Destroy(player);
            if (hurtbox != null) UnityEngine.Object.Destroy(hurtbox);
        }
    }
}
