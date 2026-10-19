using UnityEngine;

namespace Mavis
{
    // Each boss reuses one tracker. Sampling, casts and projection allocate nothing per frame.
    public sealed class BossTargetPrediction
    {
        public const float MaxSpeed = 40f, MaxLeadSeconds = .85f, MaxLeadDistance = 6f;
        public Transform Target { get; private set; }
        public Vector3 Velocity { get; private set; }
        Collider body;
        Vector3 previous;
        float sampledAt;
        int sampledFrame = -1;
        readonly RaycastHit[] obstacles = new RaycastHit[24];

        public void Observe(Transform target)
        {
            if (target != Target)
            {
                Target = target; body = target ? target.GetComponent<CharacterController>() : null;
                if (!body && target) body = target.GetComponent<Collider>();
                previous = target ? target.position : Vector3.zero;
                var controller = body as CharacterController;
                Velocity = controller && controller.enabled ? Planar(controller.velocity) : Vector3.zero;
                Velocity = Vector3.ClampMagnitude(Velocity, MaxSpeed);
                sampledAt = Time.time; sampledFrame = Time.frameCount;
                return;
            }
            if (!target || sampledFrame == Time.frameCount) return;
            sampledFrame = Time.frameCount;
            float elapsed = Time.time - sampledAt;
            Vector3 delta = Planar(target.position - previous);
            previous = target.position; sampledAt = Time.time;
            if (elapsed <= .0001f) return;
            // Teleports, respawns and cinematic handoffs must not create an aim velocity.
            if (elapsed > .3f || delta.magnitude > MaxSpeed * elapsed + .75f)
            { Velocity = Vector3.zero; return; }
            Vector3 measured = Vector3.ClampMagnitude(delta / elapsed, MaxSpeed);
            if (measured.sqrMagnitude < .04f) Velocity = Vector3.zero;
            else Velocity = Vector3.Lerp(Velocity, measured, 1 - Mathf.Exp(-16 * elapsed));
        }

        public static Vector3 Offset(Vector3 velocity, float seconds, float maximum = MaxLeadDistance)
            => Vector3.ClampMagnitude(Planar(velocity) * Mathf.Clamp(seconds, 0, MaxLeadSeconds), Mathf.Max(0, maximum));

        public Vector3 PointAfter(float seconds, Transform owner = null, float maximum = MaxLeadDistance)
            => Lead(Target ? Target.position : previous, seconds, owner, maximum);

        public Vector3 BodyAfter(float seconds, Transform owner = null, float maximum = MaxLeadDistance)
            => Lead(body ? body.bounds.center : Target ? Target.position + Vector3.up : previous, seconds, owner, maximum);

        public Vector3 BodyIntercept(Vector3 origin, float speed, Transform owner = null)
        {
            Vector3 current = body ? body.bounds.center : Target ? Target.position + Vector3.up : previous;
            float seconds = Vector3.Distance(origin, current) / Mathf.Max(1, speed);
            Vector3 point = current + Offset(Velocity, seconds);
            seconds = Vector3.Distance(origin, point) / Mathf.Max(1, speed);
            return BodyAfter(seconds, owner);
        }

        Vector3 Lead(Vector3 current, float seconds, Transform owner, float maximum)
        {
            Vector3 offset = Offset(Velocity, seconds, maximum);
            float length = offset.magnitude;
            if (!Target || length < .001f) return current;
            Vector3 origin = body ? body.bounds.center : Target.position + Vector3.up;
            int count = Physics.SphereCastNonAlloc(origin, .2f, offset / length, obstacles, length, ~0, QueryTriggerInteraction.Ignore);
            float clearance = length;
            for (int i = 0; i < count; i++)
            {
                var hit = obstacles[i];
                if (!hit.collider || hit.transform.IsChildOf(Target) || Target.IsChildOf(hit.transform) ||
                    (owner && hit.transform.IsChildOf(owner))) continue;
                clearance = Mathf.Min(clearance, Mathf.Max(0, hit.distance - .15f));
            }
            // An overfull buffer may omit the nearest wall: keep the current aim instead.
            return count == obstacles.Length ? current : current + offset * (clearance / length);
        }
        static Vector3 Planar(Vector3 v) { v.y = 0; return v; }
    }
}
