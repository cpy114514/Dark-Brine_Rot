using UnityEngine;

namespace Mavis
{
    [DefaultExecutionOrder(100)]
    public sealed class NailongHitReaction : MonoBehaviour
    {
        Transform spine, head;
        NailongHealth health;
        float started, duration, strength, side;
        public bool IsReacting => health != null && !health.IsDead && Time.time < started + duration;
        void Awake()
        {
            health = GetComponent<NailongHealth>();
            var skin = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null) return;
            foreach (var bone in skin.bones)
            {
                if (bone == null) continue;
                if (bone.name == "Spine2") spine = bone;
                if (bone.name == "Head") head = bone;
            }
        }
        public void Hit(Vector3 point, CombatHitKind kind)
        {
            bool heavy = kind == CombatHitKind.ChargedHeavy || kind == CombatHitKind.JumpSlash;
            float nextStrength = heavy ? 19f : kind == CombatHitKind.ComboFinisher ? 11f : kind == CombatHitKind.ComboTwo ? 7f : 4.5f;
            if (IsReacting && strength > nextStrength) return;
            strength = nextStrength;
            duration = heavy ? 0.6f : kind == CombatHitKind.ComboFinisher ? 0.28f : 0.19f;
            started = Time.time;
            side = Mathf.Clamp(transform.InverseTransformPoint(point).x, -1f, 1f);
        }
        void LateUpdate()
        {
            if (!IsReacting || Time.timeScale <= 0f) return;
            float t = (Time.time - started) / duration;
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.25f) * 0.5f) * Mathf.Pow(1f - t, 2f);
            if (spine != null) spine.localRotation *= Quaternion.Euler(-strength * envelope, side * strength * 0.35f * envelope, 0f);
            if (head != null) head.localRotation *= Quaternion.Euler(-strength * 0.55f * envelope, 0f, -side * strength * 0.25f * envelope);
        }
    }
}
