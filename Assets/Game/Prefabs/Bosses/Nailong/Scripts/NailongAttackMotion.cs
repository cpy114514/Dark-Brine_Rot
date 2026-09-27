using UnityEngine;

namespace Mavis
{
    // Procedural upper-body animation layered over the imported idle pose.
    // It runs in LateUpdate, after Animator has written the humanoid bones.
    public sealed class NailongAttackMotion : MonoBehaviour
    {
        public enum Style { LeftClaw, RightClaw, DoubleClaw, ShoulderBump }

        Transform leftShoulder;
        Transform leftForeArm;
        Transform leftHand;
        Transform rightShoulder;
        Transform rightForeArm;
        Transform rightHand;
        Transform spine;
        Transform head;
        Style style;
        float startedAt;
        float duration;
        bool active;

        void Awake()
        {
            var skin = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null) return;
            foreach (Transform bone in skin.bones)
            {
                if (bone == null) continue;
                switch (bone.name)
                {
                    case "LeftShoulder": leftShoulder = bone; break;
                    case "LeftForeArm": leftForeArm = bone; break;
                    case "LeftHand": leftHand = bone; break;
                    case "RightShoulder": rightShoulder = bone; break;
                    case "RightForeArm": rightForeArm = bone; break;
                    case "RightHand": rightHand = bone; break;
                    case "Spine2": spine = bone; break;
                    case "Head": head = bone; break;
                }
            }
        }

        public void Begin(Style nextStyle, float seconds)
        {
            style = nextStyle;
            duration = Mathf.Max(0.1f, seconds);
            startedAt = Time.time;
            active = true;
        }

        public void Stop() => active = false;

        void LateUpdate()
        {
            if (!active) return;
            float phase = Mathf.Clamp01((Time.time - startedAt) / duration);
            if (phase >= 1f) { active = false; return; }

            if (style == Style.ShoulderBump)
            {
                float wind = Wind(phase);
                float strike = Strike(phase);
                Lean(31f * strike - 9f * wind, 8f * strike);
                PoseArm(true, wind, strike, true);
                PoseArm(false, wind, strike, true);
                if (head != null)
                    head.rotation = Quaternion.AngleAxis(14f * strike, transform.right) * head.rotation;
                return;
            }

            float left = 0f;
            float right = 0f;
            float leftWind = 0f;
            float rightWind = 0f;
            if (style == Style.DoubleClaw)
            {
                float first = Mathf.Clamp01(phase / 0.62f);
                float second = Mathf.Clamp01((phase - 0.35f) / 0.62f);
                left = Strike(first);
                leftWind = Wind(first);
                right = Strike(second);
                rightWind = Wind(second);
            }
            else if (style == Style.LeftClaw)
            {
                left = Strike(phase);
                leftWind = Wind(phase);
            }
            else
            {
                right = Strike(phase);
                rightWind = Wind(phase);
            }

            Lean(13f * Mathf.Max(left, right), 17f * (right - left));
            PoseArm(true, leftWind, left, false);
            PoseArm(false, rightWind, right, false);
        }

        void Lean(float forwardDegrees, float twistDegrees)
        {
            if (spine == null) return;
            spine.rotation = Quaternion.AngleAxis(twistDegrees, transform.up) *
                Quaternion.AngleAxis(forwardDegrees, transform.right) * spine.rotation;
        }

        void PoseArm(bool left, float wind, float strike, bool bump)
        {
            Transform shoulder = left ? leftShoulder : rightShoulder;
            Transform foreArm = left ? leftForeArm : rightForeArm;
            Transform hand = left ? leftHand : rightHand;
            if (shoulder == null || hand == null) return;

            // Aim the hand from the actual animated pose. Imported humanoid
            // clips fold the arms differently from the FBX rest pose, so a
            // fixed Euler rotation can bury the claw inside the torso.
            Vector3 arm = hand.position - shoulder.position;
            if (arm.sqrMagnitude < 0.001f) return;
            float side = left ? -1f : 1f;
            Vector3 windDirection = transform.TransformDirection(
                new Vector3(side * 0.72f, 0.58f, -0.25f).normalized);
            Vector3 strikeDirection = transform.TransformDirection(
                new Vector3(side * (bump ? 0.12f : 0.24f), bump ? -0.18f : 0.02f, 1f).normalized);
            Vector3 aim = Vector3.Slerp(arm.normalized, windDirection, wind);
            aim = Vector3.Slerp(aim, strikeDirection, strike);
            shoulder.rotation = Quaternion.FromToRotation(arm, aim) * shoulder.rotation;

            if (foreArm != null)
            {
                Vector3 wrist = hand.position - foreArm.position;
                if (wrist.sqrMagnitude > 0.001f)
                {
                    Vector3 reach = transform.TransformDirection(new Vector3(side * 0.12f, -0.12f, 1f));
                    foreArm.rotation = Quaternion.Slerp(Quaternion.identity,
                        Quaternion.FromToRotation(wrist, reach), 0.55f * strike) * foreArm.rotation;
                }
            }
            if (hand != null)
                hand.rotation = Quaternion.AngleAxis(side * 25f * strike, transform.up) * hand.rotation;
        }

        static float Wind(float phase)
        {
            if (phase < 0.22f) return Smooth(phase / 0.22f);
            if (phase < 0.43f) return 1f - Smooth((phase - 0.22f) / 0.21f);
            return 0f;
        }

        static float Strike(float phase)
        {
            if (phase < 0.22f) return 0f;
            if (phase < 0.44f) return Smooth((phase - 0.22f) / 0.22f);
            if (phase < 0.57f) return 1f;
            if (phase < 0.90f) return 1f - Smooth((phase - 0.57f) / 0.33f);
            return 0f;
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
