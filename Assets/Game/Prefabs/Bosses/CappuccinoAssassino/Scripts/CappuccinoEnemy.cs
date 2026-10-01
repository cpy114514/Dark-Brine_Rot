using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    /// <summary>Presentation for the attackable Cappuccino enemy. Health owns damage and extension events.</summary>
    [RequireComponent(typeof(Health))]
    public sealed class CappuccinoEnemy : MonoBehaviour
    {
        public Transform visual;
        public Renderer[] bodyRenderers;
        public Collider bodyCollider;
        [Min(0f)] public float hitReactionDuration = 0.22f;
        [Min(0.1f)] public float deathDuration = 0.9f;
        [Min(0f)] public float removeAfterDeath = 4f;
        public Color hitColor = new Color(1f, 0.32f, 0.18f, 1f);
        public UnityEvent onDefeated = new UnityEvent();

        Health health;
        MaterialPropertyBlock properties;
        Vector3 restPosition;
        Quaternion restRotation;
        float hitTime = -100f;
        float deathTime = -1f;
        float modelHeight;

        void Awake()
        {
            health = GetComponent<Health>();
            health.OnDamaged ??= new UnityEvent<float>();
            health.OnDeath ??= new UnityEvent();
            health.OnHealthChanged ??= new UnityEvent<float, float>();
            properties = new MaterialPropertyBlock();
            if (visual != null)
            {
                restPosition = visual.localPosition;
                restRotation = visual.localRotation;
            }
            if (bodyCollider != null) modelHeight = bodyCollider.bounds.size.y / Mathf.Max(transform.lossyScale.y, 0.001f);
            health.OnDamaged.AddListener(OnHit);
            health.OnDeath.AddListener(OnDeath);
            if (health.IsDead) OnDeath();
        }

        void OnHit(float amount) { hitTime = Time.time; }

        void OnDeath()
        {
            if (deathTime >= 0f) return;
            deathTime = Time.time;
            if (bodyCollider != null) bodyCollider.enabled = false;
            onDefeated.Invoke();
            if (removeAfterDeath > 0f) Destroy(gameObject, Mathf.Max(removeAfterDeath, deathDuration));
        }

        void Update()
        {
            float hit = Mathf.Clamp01(1f - (Time.time - hitTime) / Mathf.Max(0.01f, hitReactionDuration));
            if (bodyRenderers != null)
                foreach (var renderer in bodyRenderers)
                {
                    if (renderer == null) continue;
                    renderer.GetPropertyBlock(properties);
                    properties.SetColor("_BaseColor", Color.Lerp(Color.white, hitColor, hit));
                    renderer.SetPropertyBlock(properties);
                }
            if (visual == null) return;
            if (deathTime >= 0f)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - deathTime) / deathDuration));
                // Lift the pivot while tipping so the cup settles onto its side above the ground.
                visual.localPosition = restPosition + Vector3.up * (modelHeight * 0.43f * t);
                visual.localRotation = restRotation * Quaternion.Euler(0f, 0f, 88f * t);
            }
            else
            {
                float kick = Mathf.Sin(hit * Mathf.PI) * hit;
                visual.localPosition = restPosition + Vector3.back * (0.10f * kick);
                visual.localRotation = restRotation * Quaternion.Euler(-6f * kick, 0f, 0f);
            }
        }

        void OnDestroy()
        {
            if (health == null) return;
            health.OnDamaged?.RemoveListener(OnHit);
            health.OnDeath?.RemoveListener(OnDeath);
        }
    }
}
