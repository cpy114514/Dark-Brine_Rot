using UnityEngine;

namespace Mavis
{
    [DefaultExecutionOrder(900)]
    public sealed class CombatLocalHitStop : MonoBehaviour
    {
        Animator animator;
        float savedSpeed, until, burstStartedAt, resumeUntil;
        bool ownsSpeed;
        Transform[] pose;
        Vector3[] positions;
        Quaternion[] rotations;
        bool capturedPose;
        int layerSpeed;
        float savedLayerSpeed;
        bool hasLightParameter,hasHeavyParameter;
        static readonly int LightSpeed=Animator.StringToHash("LightPlaybackSpeed"),HeavySpeed=Animator.StringToHash("HeavyPlaybackSpeed");
        public bool UsesLayerFreeze => ownsSpeed && layerSpeed!=0;
        public bool IsStopped => ownsSpeed && Time.unscaledTime < until;
        public static bool Active(GameObject actor) => actor && actor.TryGetComponent<CombatLocalHitStop>(out var stop) && stop.IsStopped;
        public static void Freeze(GameObject actor, Animator target, float seconds)
        {
            if (!actor || !target || !target.enabled || seconds<=0) return;
            var stop=actor.GetComponent<CombatLocalHitStop>();
            if (!stop) stop=actor.AddComponent<CombatLocalHitStop>();
            stop.Hold(target,seconds);
        }
        public void Hold(Animator target, float seconds)
        {
            float now = Time.unscaledTime;
            if (ownsSpeed && now >= until) EndBurst();
            if (now < resumeUntil) return;
            if (ownsSpeed && animator != target) Release();

            if (animator != target || pose == null)
            {
                animator = target;
                pose = target.GetComponentsInChildren<Transform>(true);
                positions = new Vector3[pose.Length];
                rotations = new Quaternion[pose.Length];
                hasLightParameter = hasHeavyParameter = false;
                foreach (var parameter in animator.parameters)
                {
                    if (parameter.type != AnimatorControllerParameterType.Float) continue;
                    if (parameter.nameHash == LightSpeed) hasLightParameter = true;
                    if (parameter.nameHash == HeavySpeed) hasHeavyParameter = true;
                }
            }
            if (!ownsSpeed)
            {
                savedSpeed = animator.speed;
                ownsSpeed = true;
                capturedPose = false;
                layerSpeed = 0;
                burstStartedAt = now;
                // Moving attacks pause on their own speed parameter so the legs keep stepping.
                var melee=GetComponent<SahurAttack>();
                if(melee && (melee.CanMoveDuringCombat || GetComponent<ThirdPersonPlayerController>() is ThirdPersonPlayerController motor && motor.BladeFootworkActive))
                {
                    int hash = melee.IsHeavyAttackActive ? HeavySpeed : LightSpeed;
                    if (melee.IsHeavyAttackActive ? hasHeavyParameter : hasLightParameter)
                    {
                        layerSpeed = hash;
                        savedLayerSpeed = animator.GetFloat(hash);
                    }
                }
            }
            // Cap repeated hits from the start of the burst.
            until = Mathf.Min(Mathf.Max(until, now + Mathf.Clamp(seconds, 0, .13f)),
                burstStartedAt + CombatFeelProfile.Default.maximumContinuousStop);
            if (layerSpeed != 0)
                animator.SetFloat(layerSpeed, 0);
            else
                animator.speed = 0;
        }
        void LateUpdate()
        {
            if (!IsStopped || pose == null || layerSpeed != 0) return;
            // Capture the pose after additive animation has run.
            for (int i = 1; i < pose.Length; i++)
            {
                if (!pose[i]) continue;
                if (capturedPose)
                {
                    pose[i].localPosition = positions[i];
                    pose[i].localRotation = rotations[i];
                }
                else
                {
                    positions[i] = pose[i].localPosition;
                    rotations[i] = pose[i].localRotation;
                }
            }
            capturedPose=true;
        }
        void Update()
        {
            if (!ownsSpeed) return;
            if (!animator || !animator.enabled || !animator.gameObject.activeInHierarchy)Release();
            else if(Time.unscaledTime>=until)EndBurst();
            else if(layerSpeed!=0)animator.SetFloat(layerSpeed,0);
        }
        public void Release()
        {
            // A death/cinematic may have acquired playback since the hit.
            if (ownsSpeed && animator)
            {
                if (layerSpeed != 0)
                {
                    if (Mathf.Approximately(animator.GetFloat(layerSpeed), 0))
                        animator.SetFloat(layerSpeed, savedLayerSpeed);
                }
                else if (Mathf.Approximately(animator.speed, 0))
                    animator.speed = savedSpeed;
            }
            ownsSpeed = false;
            capturedPose = false;
            until = 0;
            layerSpeed = 0;
            resumeUntil = 0;
        }
        void EndBurst()
        {
            Release();
            resumeUntil = Time.unscaledTime + CombatFeelProfile.Default.hitStopResumeSeconds;
        }
        void OnDisable() => Release();
        void OnDestroy() => Release();
    }
}
