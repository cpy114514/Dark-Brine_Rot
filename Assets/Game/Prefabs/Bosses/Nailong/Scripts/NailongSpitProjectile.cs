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
        float collisionRadius = .11f;
        bool resolved;
        public bool HasImpacted => resolved;

        public void Launch(Transform source, Transform target, Vector3 initialVelocity, float amount, float radius = .11f)
        {
            owner = source;
            intendedTarget = target;
            velocity = initialVelocity;
            damage = amount;
            collisionRadius = Mathf.Max(.01f, radius);
            expires = Time.time + 3f;
        }

        void Update() => Simulate(Time.deltaTime);

        public void Simulate(float deltaTime)
        {
            if (resolved || PauseSettingsMenu.IsOpen || deltaTime <= 0f) return;
            if (owner == null || Time.time >= expires) { Destroy(gameObject); return; }
            float dt=Mathf.Min(deltaTime,.1f);velocity+=Vector3.down*3.5f*dt;
            Vector3 step = velocity * dt;
            float distance = step.magnitude;
            if (distance > 0f)
            {
                int count = Physics.SphereCastNonAlloc(transform.position, collisionRadius, step / distance,
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
                    BossCombatVfx.Burst(hit.point,hit.normal,new Color(.55f,.65f,.36f,.75f),10,1.6f);
                    velocity = Vector3.zero;
                    transform.localScale = new Vector3(0.32f, 0.06f, 0.32f) * (collisionRadius / .11f);
                    Destroy(gameObject, 0.16f);
                    return;
                }
            }
            transform.position += step;
            if(velocity.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(velocity);
        }
    }
}
