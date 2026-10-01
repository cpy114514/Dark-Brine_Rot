using UnityEngine;

namespace Mavis
{
    public sealed class NailongSpitProjectile : MonoBehaviour
    {
        readonly RaycastHit[] hits = new RaycastHit[32];
        Transform owner;
        Transform intendedTarget;
        Vector3 velocity;
        float damage;
        float expires;
        bool resolved;
        public bool HasImpacted => resolved;

        public void Launch(Transform source, Transform target, Vector3 initialVelocity, float amount)
        {
            owner = source;
            intendedTarget = target;
            velocity = initialVelocity;
            damage = amount;
            expires = Time.time + 3f;
        }

        void Update() => Simulate(Time.deltaTime);

        public void Simulate(float deltaTime)
        {
            if (resolved || PauseSettingsMenu.IsOpen || deltaTime <= 0f) return;
            if (owner == null || Time.time >= expires) { Destroy(gameObject); return; }
            Vector3 step = velocity * deltaTime;
            float distance = step.magnitude;
            if (distance > 0f)
            {
                int count = Physics.SphereCastNonAlloc(transform.position, 0.11f, step / distance,
                    hits, distance, ~0, QueryTriggerInteraction.Collide);
                int closest = -1;
                float nearest = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i];
                    if (hit.collider == null || hit.collider.transform.IsChildOf(owner) ||
                        hit.collider.transform.IsChildOf(transform)) continue;
                    var victim = hit.collider.GetComponentInParent<IDamageable>() as Component;
                    // Ignore non-solid interaction triggers, but include a
                    // target's hurtbox even if that collider is a trigger.
                    if (hit.collider.isTrigger && victim == null) continue;
                    if (hit.distance < nearest) { nearest = hit.distance; closest = i; }
                }
                if (closest >= 0)
                {
                    var hit = hits[closest];
                    resolved = true;
                    transform.position = hit.point;
                    var victim = hit.collider.GetComponentInParent<IDamageable>();
                    var player = hit.collider.GetComponentInParent<PlayerHealth>();
                    bool intended = intendedTarget != null &&
                        (hit.collider.transform.IsChildOf(intendedTarget) || intendedTarget.IsChildOf(hit.collider.transform));
                    if (victim != null && (player != null || intended)) victim.ApplyDamage(damage, hit.point);
                    // Leave a short visible splash instead of disappearing mid-air.
                    velocity = Vector3.zero;
                    transform.localScale = new Vector3(0.32f, 0.06f, 0.32f);
                    Destroy(gameObject, 0.16f);
                    return;
                }
            }
            transform.position += step;
        }
    }
}
