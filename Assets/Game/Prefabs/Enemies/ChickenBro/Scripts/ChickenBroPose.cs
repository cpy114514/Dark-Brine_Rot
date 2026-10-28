using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    /// <summary>
    /// Readable stand-in motion on Chicken Bro's own bones.
    /// Stops overriding as soon as an Animator clip is actually playing.
    /// </summary>
    [DefaultExecutionOrder(40)]
    [RequireComponent(typeof(ChickenBroAI))]
    public sealed class ChickenBroPose : MonoBehaviour
    {
        Animator animator;
        ChickenBroAI body;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly Dictionary<Transform, Quaternion> bindRotation = new Dictionary<Transform, Quaternion>();
        readonly Dictionary<Transform, Vector3> bindPosition = new Dictionary<Transform, Vector3>();

        void Awake()
        {
            body = GetComponent<ChickenBroAI>();
            animator = GetComponentInChildren<Animator>();
            if (animator && animator.runtimeAnimatorController && animator.runtimeAnimatorController.animationClips.Length > 0)
            {
                enabled = false;
                return;
            }
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                if (!bone || bones.ContainsKey(bone.name)) continue;
                bones.Add(bone.name, bone);
                bindRotation.Add(bone, bone.localRotation);
                bindPosition.Add(bone, bone.localPosition);
            }
        }

        void LateUpdate()
        {
            if (animator && animator.isActiveAndEnabled && animator.GetCurrentAnimatorClipInfo(0).Length > 0) return;
            float phase = body ? body.ActionPhase : 0f;
            float t = Time.time;
            var mode = body ? body.Mode : ChickenBroAI.Mood.Idle;
            bool moving = mode == ChickenBroAI.Mood.Chase || mode == ChickenBroAI.Mood.Patrol;
            float stride = moving ? Mathf.Sin(t * (mode == ChickenBroAI.Mood.Chase ? 11f : 7f)) : 0f;
            Swing("Hips", transform.up, 0f, moving ? Mathf.Abs(stride) * .04f : Mathf.Sin(t * 1.6f) * .015f);
            Swing("Spine", transform.right, moving ? stride * 6f : Mathf.Sin(t * 1.6f) * 2f);
            Swing("Spine1", transform.forward, mode == ChickenBroAI.Mood.Lunge ? -18f : mode == ChickenBroAI.Mood.Flinch ? 14f : 0f);
            Swing("Head", transform.right, mode == ChickenBroAI.Mood.Strike ? -12f : Mathf.Sin(t * 1.4f) * 4f);
            Swing("LeftUpLeg", transform.right, stride * 28f);
            Swing("RightUpLeg", transform.right, -stride * 28f);
            Swing("LeftLeg", transform.right, Mathf.Max(0f, -stride) * 22f);
            Swing("RightLeg", transform.right, Mathf.Max(0f, stride) * 22f);
            float arm = moving ? -stride * 18f : Mathf.Sin(t * 1.6f) * 4f;
            float strike = mode == ChickenBroAI.Mood.Strike || mode == ChickenBroAI.Mood.Lunge ? StrikeArm(phase, mode) : 0f;
            Swing("LeftArm", transform.right, arm);
            Swing("RightArm", transform.right, -arm + strike);
            Swing("RightForeArm", transform.right, strike * .35f);
            if (mode == ChickenBroAI.Mood.Launch || mode == ChickenBroAI.Mood.Knockdown || mode == ChickenBroAI.Mood.Dead)
            {
                float fall = mode == ChickenBroAI.Mood.Dead ? Mathf.Clamp01(phase) : Mathf.Clamp01(phase * 1.4f);
                Swing("Hips", transform.right, Mathf.Lerp(0f, -78f, fall));
                Swing("Spine", transform.right, Mathf.Lerp(0f, -18f, fall));
            }
        }

        static float StrikeArm(float phase, ChickenBroAI.Mood mode)
        {
            float wind = mode == ChickenBroAI.Mood.Lunge ? .38f : .32f;
            float hit = mode == ChickenBroAI.Mood.Lunge ? .68f : .52f;
            if (phase < wind) return Mathf.Lerp(8f, -55f, phase / Mathf.Max(.01f, wind));
            if (phase < hit) return Mathf.Lerp(-55f, 70f, (phase - wind) / Mathf.Max(.01f, hit - wind));
            return Mathf.Lerp(70f, 8f, (phase - hit) / Mathf.Max(.01f, 1f - hit));
        }

        void Swing(string name, Vector3 worldAxis, float degrees, float lift = 0f)
        {
            if (!bones.TryGetValue(name, out var bone) || !bone || !bone.parent) return;
            Vector3 axis = bone.parent.InverseTransformDirection(worldAxis);
            if (axis.sqrMagnitude < .0001f) return;
            bone.localRotation = bindRotation[bone] * Quaternion.AngleAxis(degrees, axis.normalized);
            if (Mathf.Abs(lift) > .0001f)
            {
                Vector3 up = bone.parent.InverseTransformDirection(Vector3.up);
                bone.localPosition = bindPosition[bone] + up.normalized * lift;
            }
        }
    }
}
