using System;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class VerifySahurChargedSwing
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(object obj, string method) => obj.GetType().GetMethod(method, Private).Invoke(obj, null);
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj, value);

    public static object Verify()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var root = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 500, 0), Quaternion.identity);
        var baked = new Mesh();
        try
        {
            var attack = root.GetComponent<Mavis.SahurAttack>();
            var animator = attack.animator;
            var controller = (AnimatorController)animator.runtimeAnimatorController;
            var states = controller.layers[0].stateMachine.states.Select(child => child.state).ToArray();
            if (states.Single(state => state.name == "Heavy Attack").motion == states.Single(state => state.name == "Attack").motion)
                throw new Exception("Heavy attack still uses the old ordinary strike.");
            if (states.Single(state => state.name == "Heavy Attack").motion != attack.chargeClip)
                throw new Exception("Windup and released strike use different clips.");
            root.GetComponent<ThirdPersonPlayerController>().enabled = false;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var guard = animator.GetComponent<SahurCombatGuardIK>();
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>()
                .OrderByDescending(s => s.sharedMesh.vertexCount).First();
            var bones = skin.bones;
            Func<int, bool> headBone = index => index < bones.Length && bones[index] != null &&
                (bones[index] == head || bones[index].IsChildOf(head));
            var headVertices = skin.sharedMesh.boneWeights.Select((w, i) => new { w, i })
                .Where(v => (headBone(v.w.boneIndex0) ? v.w.weight0 : 0f) +
                    (headBone(v.w.boneIndex1) ? v.w.weight1 : 0f) +
                    (headBone(v.w.boneIndex2) ? v.w.weight2 : 0f) +
                    (headBone(v.w.boneIndex3) ? v.w.weight3 : 0f) > 0.5f)
                .Select(v => v.i).ToArray();
            if (headVertices.Length == 0) throw new Exception("No head geometry found for clearance verification.");
            int layer = animator.GetLayerIndex("Charge Upper Body");
            float minHeadDistance = float.MaxValue, maxFirstStep = 0f;
            bool rearward = false, forward = false;
            var closest = "";
            var cases = new System.Collections.Generic.List<object>();
            int headIntersections = 0;
            int originalSwingSamples = 0;
            foreach (float charge in new[] { 0.25f, 1f })
            {
                float caseMin = float.MaxValue;
                attack.SuspendForSwimming();
                animator.Play("Locomotion", 0, 0f);
                animator.Update(0f);
                root.GetComponent<Mavis.PlayerStamina>().currentStamina = 100f;
                Call(attack, "StartCharge");
                Set(attack, "chargeStartedAt", Time.time - attack.fullChargeTime * charge);
                animator.SetLayerWeight(layer, 1f);
                Call(attack, "UpdateChargePose");
                animator.Update(0.02f);
                Vector3 previousHand = hand.position;
                Call(attack, "ReleaseCharge");
                if (!attack.IsHeavyAttackActive) throw new Exception("Charge did not release into heavy attack.");
                float expectedDamage = attack.damage * Mathf.Lerp(attack.minChargeDamageMultiplier, attack.maxChargeDamageMultiplier, charge);
                if (Mathf.Abs((float)typeof(Mavis.SahurAttack).GetField("swingDamage", Private).GetValue(attack) - expectedDamage) > 0.01f)
                    throw new Exception("Charge damage changed while restoring the animation.");
                for (int i = 0; i < 200; i++)
                {
                    animator.SetLayerWeight(layer, 1f);
                    animator.Update(1f / 120f);
                    if (i == 0) maxFirstStep = Mathf.Max(maxFirstStep, Vector3.Distance(previousHand, hand.position));
                    if (attack.HeavyAttackPhase > 0.93f) break;
                    if (attack.HeavyAttackPhase >= attack.heavySwingWindowStart && attack.HeavyAttackPhase <= attack.heavySwingWindowEnd)
                    {
                        originalSwingSamples++;
                    }
                    var capsule = (CapsuleCollider)attack.stickHitbox;
                    Vector3 axis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
                    Vector3 center = capsule.transform.TransformPoint(capsule.center);
                    Vector3 extent = capsule.transform.TransformVector(axis * Mathf.Max(0, capsule.height * 0.5f - capsule.radius));
                    Vector3 a = center - extent, b = center + extent;
                    skin.BakeMesh(baked);
                    var vertices = baked.vertices;
                    var headBounds = new Bounds(skin.transform.TransformPoint(vertices[headVertices[0]]), Vector3.zero);
                    foreach (int vertex in headVertices) headBounds.Encapsulate(skin.transform.TransformPoint(vertices[vertex]));
                    float radius = capsule.radius * Mathf.Max(capsule.transform.lossyScale.x,
                        capsule.transform.lossyScale.y, capsule.transform.lossyScale.z);
                    headBounds.Expand(radius * 2f);
                    float entry;
                    if (headBounds.Contains(a) || headBounds.Contains(b) ||
                        (headBounds.IntersectRay(new Ray(a, (b - a).normalized), out entry) && entry <= Vector3.Distance(a, b)))
                        headIntersections++;
                    Vector3 segment = b - a;
                    float t = Mathf.Clamp01(Vector3.Dot(head.position - a, segment) / segment.sqrMagnitude);
                    float distance = Vector3.Distance(head.position, a + t * segment);
                    caseMin = Mathf.Min(caseMin, distance);
                    if (distance < minHeadDistance)
                    {
                        minHeadDistance = distance;
                        closest = "charge=" + charge + " phase=" + attack.HeavyAttackPhase + " head=" + root.transform.InverseTransformPoint(head.position) + " hand=" + root.transform.InverseTransformPoint(hand.position) + " bat=" + root.transform.InverseTransformPoint(center);
                    }
                    var localCenter = root.transform.InverseTransformPoint(center);
                    var localHead = root.transform.InverseTransformPoint(head.position);
                    rearward |= localCenter.z < localHead.z - 0.1f;
                    forward |= localCenter.z > localHead.z + 0.1f;
                }
                cases.Add(new { charge, minDistance = caseMin });
                attack.SuspendForSwimming();
                animator.Update(0.02f);
                if (animator.GetLayerWeight(layer) > 0.001f) throw new Exception("Cancellation left the charge layer active.");
            }
            if (maxFirstStep > 0.10f || !rearward || !forward || headIntersections != 0)
                throw new Exception("Release continuity or rear-to-front arc failed: firstStep=" + maxFirstStep + ", rearward=" + rearward + ", forward=" + forward + ", headIntersections=" + headIntersections + ", closest=" + closest);
            if (originalSwingSamples < 20) throw new Exception("Downloaded strike was not sufficiently sampled.");
            return new { maxFirstStepMetres = maxFirstStep, minHeadBoneToStickMetres = minHeadDistance,
                rearward, forward, closest, cases, headIntersections, headVertices = headVertices.Length,
                shortAndFullChargeTested = true, cancelClearsIK = true, strikeSamples = originalSwingSamples, downloadedAnimation = true, chargeDamagePreserved = true };
        }
        finally { UnityEngine.Object.DestroyImmediate(baked); UnityEngine.Object.DestroyImmediate(root); }
    }
}
