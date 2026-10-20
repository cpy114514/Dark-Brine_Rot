using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    /// <summary>One non-homing ballistic glob. Cosmetic beads never deal damage.</summary>
    public sealed class NailongSpitProjectile : MonoBehaviour
    {
        const float Lifetime = 4f, SplashSeconds = .32f;
        readonly RaycastHit[] hits = new RaycastHit[64];
        Transform owner, intendedTarget;
        Vector3 velocity, launchPoint, launchVelocity, splashNormal;
        float damage, age, gravity, splashAge, collisionRadius = .11f;
        bool resolved;
        MeshRenderer core;
        readonly MeshRenderer[] beads = new MeshRenderer[6];
        readonly Vector3[] splashVelocity = new Vector3[6];
        MaterialPropertyBlock properties;
        static readonly int VisibilityId = Shader.PropertyToID("_Visibility");
        public bool HasImpacted => resolved;
        public Vector3 Velocity => velocity;
        public float Gravity => gravity;
        public float FlightAge => age;
        public float CollisionRadius => collisionRadius;

        public void PrepareVisuals(Material material)
        {
            if (core != null) return;
            properties = new MaterialPropertyBlock();
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Refractive water glob"; sphere.transform.SetParent(transform, false);
            var collider = sphere.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            core = sphere.GetComponent<MeshRenderer>(); core.sharedMaterial = material;
            core.shadowCastingMode = ShadowCastingMode.Off; core.receiveShadows = false;
            var mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            for (int i = 0; i < beads.Length; i++)
            {
                var bead = new GameObject("Cosmetic water bead " + i); bead.transform.SetParent(transform, false);
                bead.AddComponent<MeshFilter>().sharedMesh = mesh;
                beads[i] = bead.AddComponent<MeshRenderer>(); beads[i].sharedMaterial = material;
                beads[i].shadowCastingMode = ShadowCastingMode.Off; beads[i].receiveShadows = false;
                beads[i].enabled = false;
            }
        }

        public void Launch(Transform source, Transform target, Vector3 initialVelocity, float amount, float radius = .11f, float acceleration = 18f)
        {
            owner = source; intendedTarget = target;
            velocity = launchVelocity = initialVelocity; launchPoint = transform.position;
            damage = amount; collisionRadius = Mathf.Max(.01f, radius); gravity = Mathf.Max(1f, acceleration);
            age = splashAge = 0; resolved = false;
            if (core != null)
            {
                core.enabled = true; properties.SetFloat(VisibilityId, 1); core.SetPropertyBlock(properties);
                foreach (var bead in beads) { bead.enabled = false; bead.SetPropertyBlock(properties); }
                DrawFlight();
            }
        }

        void Update() => Simulate(Time.deltaTime);

        public void Simulate(float deltaTime)
        {
            if (PauseSettingsMenu.IsOpen || deltaTime <= 0f || !gameObject.activeSelf) return;
            if (owner == null || !owner.gameObject.activeInHierarchy) { Recycle(); return; }
            if (resolved)
            {
                splashAge += deltaTime;
                if (splashAge >= SplashSeconds) { Recycle(); return; }
                DrawSplash(); return;
            }
            // Consume the entire game step, including low-FPS frames. Short arc
            // segments bound curvature error; each segment is swept continuously.
            float remaining = Mathf.Min(deltaTime, Lifetime-age);
            while (remaining > .000001f)
            {
                float dt = Mathf.Min(remaining, 1f/30f);
                Vector3 step = velocity*dt + Vector3.down*(.5f*gravity*dt*dt);
                float distance = step.magnitude;
                if (distance > .000001f)
                {
                    int count = Physics.SphereCastNonAlloc(transform.position, collisionRadius, step/distance,
                        hits, distance, ~0, QueryTriggerInteraction.Collide);
                    int closest = -1; float nearest = float.PositiveInfinity;
                    for (int i = 0; i < count; i++)
                    {
                        var hit = hits[i];
                        if (!hit.collider || hit.transform.IsChildOf(owner) || hit.transform.IsChildOf(transform)) continue;
                        if (hit.collider.isTrigger && hit.collider.GetComponentInParent<IDamageable>() == null) continue;
                        if (hit.distance < nearest) { nearest = hit.distance; closest = i; }
                    }
                    if (closest >= 0) { Impact(hits[closest], step.normalized); return; }
                }
                transform.position += step; velocity += Vector3.down*(gravity*dt);
                remaining -= dt; age += dt;
            }
            if (age >= Lifetime-.00001f) { Recycle(); return; }
            if (velocity.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(velocity);
            DrawFlight();
        }

        void Impact(RaycastHit hit, Vector3 incoming)
        {
            resolved = true; splashAge = 0;
            transform.position = hit.point + hit.normal*.02f; splashNormal = hit.normal;
            var victim = hit.collider.GetComponentInParent<IDamageable>();
            var player = hit.collider.GetComponentInParent<PlayerHealth>();
            bool intended = intendedTarget != null &&
                (hit.transform.IsChildOf(intendedTarget) || intendedTarget.IsChildOf(hit.transform));
            if (player != null) player.ApplyAttackDamage(damage, hit.point, hit.point-incoming*2);
            else if (victim != null && intended) victim.ApplyDamage(damage, hit.point);
            velocity = Vector3.zero;
            if (core != null)
            {
                core.enabled = false;
                Vector3 tangent = Vector3.Cross(splashNormal, Vector3.up);
                if (tangent.sqrMagnitude < .01f) tangent = Vector3.right;
                tangent.Normalize(); Vector3 bitangent = Vector3.Cross(splashNormal, tangent);
                for (int i = 0; i < beads.Length; i++)
                {
                    float angle = i * 2.399963f;
                    splashVelocity[i] = (tangent*Mathf.Cos(angle)+bitangent*Mathf.Sin(angle))*(.8f+i*.17f)
                        + splashNormal*(1.2f+(i%3)*.25f);
                    beads[i].enabled = true;
                }
                DrawSplash();
            }
        }

        void DrawFlight()
        {
            if (core == null) return;
            float diameter = collisionRadius*2;
            float wobble = Mathf.Sin(age*23)*.035f;
            core.transform.localScale = new Vector3(diameter*(1+wobble), diameter*(1-wobble), diameter*1.42f);
            for (int i = 0; i < beads.Length; i++)
            {
                float t = age-.035f*(i+1);
                bool show = i < 2 && t > 0;
                beads[i].enabled = show;
                if (!show) continue;
                beads[i].transform.position = launchPoint+launchVelocity*t+Vector3.down*(.5f*gravity*t*t);
                beads[i].transform.rotation = transform.rotation;
                float scale = diameter*(i == 0 ? .32f : .2f);
                beads[i].transform.localScale = new Vector3(scale, scale, scale*1.25f);
            }
        }

        void DrawSplash()
        {
            if (core == null) return;
            float fade = 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,SplashSeconds,splashAge));
            properties.SetFloat(VisibilityId, fade);
            for (int i = 0; i < beads.Length; i++)
            {
                var bead = beads[i];
                bead.transform.position = transform.position+splashVelocity[i]*splashAge+Vector3.down*(4*splashAge*splashAge);
                float size = collisionRadius*(.32f+(i%3)*.14f);
                bead.transform.localScale = new Vector3(size,size*1.2f,size);
                bead.SetPropertyBlock(properties);
            }
        }

        public void Recycle()
        {
            resolved = true; owner = intendedTarget = null;
            gameObject.SetActive(false);
        }
    }
}
