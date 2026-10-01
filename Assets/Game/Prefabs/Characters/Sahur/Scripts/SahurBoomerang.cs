using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    [DisallowMultipleComponent, DefaultExecutionOrder(50)]
    public sealed class SahurBoomerang : MonoBehaviour
    {
        [Min(1f)] public float throwDistance = 12f;
        [Min(1f)] public float outwardSpeed = 16f;
        [Min(1f)] public float returnSpeed = 23f;
        [Min(0f)] public float damage = 18f;
        [Min(0f)] public float staminaCost = 6f;
        [Min(0f)] public float cooldown = .65f;
        [Min(.05f)] public float hitRadius = .4f;
        public string throwState = "Boomerang Throw";
        [Range(.1f, .8f)] public float releasePhase = .30f;
        public bool IsThrowing { get; private set; }
        public bool IsBusy => IsThrowing || IsFlying;
        public bool IsFlying => visual != null;
        public bool IsReturning => IsFlying && returning;
        public Vector3 FlightPosition => center;

        SahurAttack attack;
        ThirdPersonPlayerController movement;
        PlayerStamina stamina;
        Transform stick;
        MeshFilter mesh;
        GameObject visual;
        Renderer[] heldRenderers;
        bool[] heldVisible;
        Collider[] heldColliders;
        bool[] heldCollision;
        Vector3 center;
        Vector3 launchCenter;
        Vector3 direction;
        Quaternion rotation;
        float elapsed;
        float nextThrow;
        bool returning;
        int throwHash;
        bool released;
        bool enteredThrow;
        float throwElapsed;
        readonly HashSet<IDamageable> hitOutward = new HashSet<IDamageable>();
        readonly HashSet<IDamageable> hitReturning = new HashSet<IDamageable>();
        readonly RaycastHit[] hits = new RaycastHit[64];
        readonly Collider[] overlaps = new Collider[64];

        void Awake()
        {
            attack = GetComponent<SahurAttack>();
            movement = GetComponent<ThirdPersonPlayerController>();
            stamina = GetComponent<PlayerStamina>();
            foreach (var child in GetComponentsInChildren<Transform>(true))
                if (child.name == "Sahur Stick") { stick = child; break; }
            if (stick != null) mesh = stick.GetComponent<MeshFilter>();
            throwHash = Animator.StringToHash("Base Layer." + throwState);
        }

        void Update()
        {
            if (!PauseSettingsMenu.IsOpen && !SahurLoadoutUI.BlocksInput && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame &&
                Cursor.lockState == CursorLockMode.Locked)
            {
                if (IsFlying) returning = true;
                else TryThrow();
            }
            Simulate(Time.deltaTime);
        }

        public bool TryThrow()
        {
            if (!enabled || IsBusy || stick == null || mesh == null || mesh.sharedMesh == null ||
                PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput || Time.time < nextThrow || attack == null || !attack.enabled ||
                attack.IsCombatMotionActive || movement == null || !movement.enabled || movement.Swimming)
                return false;
            var health = GetComponent<PlayerHealth>();
            if (health != null && health.currentHealth <= 0f) return false;
            var swimmingWeapon = GetComponent<SahurSwimmingWeapon>();
            if (swimmingWeapon != null && swimmingWeapon.IsStowed) return false;
            if (attack.animator == null || !attack.animator.HasState(0, throwHash)) return false;
            if (stamina != null && !stamina.TrySpend(staminaCost)) return false;

            var camera = Camera.main;
            direction = Vector3.ProjectOnPlane(camera != null ? camera.transform.forward : transform.forward, Vector3.up).normalized;
            if (direction.sqrMagnitude < .01f) direction = transform.forward;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            movement.BeginAttackFacing();
            direction = transform.forward;
            IsThrowing = true;
            released = enteredThrow = false;
            throwElapsed = 0f;
            int chargeLayer = attack.animator.GetLayerIndex("Charge Upper Body");
            if (chargeLayer >= 0) attack.animator.SetLayerWeight(chargeLayer, 0f);
            if (attack.stickHitbox != null) attack.stickHitbox.enabled = false;
            attack.animator.CrossFadeInFixedTime(throwHash, .06f, 0, 0f);
            return true;
        }

        void Launch()
        {
            released = true;
            heldRenderers = stick.GetComponentsInChildren<Renderer>(true);
            heldVisible = new bool[heldRenderers.Length];
            heldColliders = stick.GetComponentsInChildren<Collider>(true);
            heldCollision = new bool[heldColliders.Length];
            visual = Instantiate(stick.gameObject, stick.position, stick.rotation);
            visual.name = "Sahur Boomerang (Flying)";
            visual.transform.localScale = stick.lossyScale;
            foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>()) { script.enabled = false; Destroy(script); }
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) { collider.enabled = false; Destroy(collider); }
            foreach (var body in visual.GetComponentsInChildren<Rigidbody>()) { body.isKinematic = true; body.useGravity = false; }
            for (int i = 0; i < heldRenderers.Length; i++)
            { heldVisible[i] = heldRenderers[i].enabled; heldRenderers[i].enabled = false; }
            for (int i = 0; i < heldColliders.Length; i++)
            { heldCollision[i] = heldColliders[i].enabled; heldColliders[i].enabled = false; }
            center = launchCenter = stick.TransformPoint(mesh.sharedMesh.bounds.center);
            rotation = stick.rotation;
            elapsed = 0f;
            returning = false;
            hitOutward.Clear();
            hitReturning.Clear();
            PlaceVisual(0f);
        }

        public void Simulate(float deltaTime)
        {
            if (!IsBusy) return;
            var health = GetComponent<PlayerHealth>();
            if (stick == null || movement == null || !movement.enabled || movement.Swimming ||
                (health != null && health.currentHealth <= 0f)) { Catch(); return; }
            if (PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput || deltaTime <= 0f) return;
            if (IsThrowing)
            {
                throwElapsed += deltaTime;
                var animator = attack.animator;
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).fullPathHash == throwHash)
                    state = animator.GetNextAnimatorStateInfo(0);
                bool playing = state.fullPathHash == throwHash;
                enteredThrow |= playing;
                bool launchedThisStep = false;
                if (playing && !released && state.normalizedTime >= releasePhase)
                { Launch(); launchedThisStep = true; }
                if ((playing && state.normalizedTime >= .92f) || (enteredThrow && !playing) || throwElapsed > 2f)
                {
                    if (!released) { Catch(); return; }
                    FinishThrow();
                }
                // Spawn exactly at the animated grip, without a frame of flight
                // being added on the same update as the release.
                if (launchedThisStep) return;
            }
            if (!IsFlying) return;
            elapsed += deltaTime;
            Vector3 previous = center;
            bool returningThisStep = returning;
            if (!returning)
            {
                float progress = Mathf.Clamp01(elapsed * outwardSpeed / Mathf.Max(1f, throwDistance));
                Vector3 side = Vector3.Cross(Vector3.up, direction);
                center = launchCenter + direction * (throwDistance * progress) +
                    side * (Mathf.Sin(progress * Mathf.PI) * 1.2f) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * .35f);
                if (progress >= 1f) returning = true;
            }
            else center = Vector3.MoveTowards(center, stick.TransformPoint(mesh.sharedMesh.bounds.center), returnSpeed * deltaTime);
            Sweep(previous, center, returningThisStep);
            PlaceVisual(deltaTime);
            if ((returning && Vector3.Distance(center, stick.TransformPoint(mesh.sharedMesh.bounds.center)) < .3f) || elapsed > 4f)
                Catch();
        }

        void Sweep(Vector3 from, Vector3 to, bool returnLeg)
        {
            Vector3 step = to - from;
            if (step.sqrMagnitude < .00001f) return;
            int count = Physics.SphereCastNonAlloc(from, hitRadius, step.normalized, hits, step.magnitude, ~0, QueryTriggerInteraction.Collide);
            float wallDistance = float.MaxValue;
            int wall = -1;
            for (int i = 0; i < count; i++)
            {
                var other = hits[i].collider;
                if (other == null || other.transform.IsChildOf(transform) || other.transform.IsChildOf(visual.transform)) continue;
                if (!other.isTrigger && other.GetComponentInParent<IDamageable>() == null && hits[i].distance < wallDistance)
                { wallDistance = hits[i].distance; wall = i; }
            }
            var hitSet = returnLeg ? hitReturning : hitOutward;
            int overlapCount = Physics.OverlapSphereNonAlloc(from, hitRadius, overlaps, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < overlapCount; i++)
            {
                var other = overlaps[i];
                if (other == null || other.transform.IsChildOf(transform)) continue;
                var victim = other.GetComponentInParent<IDamageable>();
                if (victim != null && other.GetComponentInParent<PlayerHealth>() == null && hitSet.Add(victim))
                    CombatHitFeedback.Apply(gameObject, victim, damage, from, CombatHitKind.Boomerang);
            }
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                var other = hit.collider;
                if (other == null || hit.distance > wallDistance || other.transform.IsChildOf(transform)) continue;
                var victim = other.GetComponentInParent<IDamageable>();
                if (victim == null || other.GetComponentInParent<PlayerHealth>() != null) continue;
                if (hitSet.Add(victim)) CombatHitFeedback.Apply(gameObject, victim, damage, hit.point, CombatHitKind.Boomerang);
            }
            if (wall >= 0 && !returnLeg)
            {
                center = from + step.normalized * Mathf.Max(0f, wallDistance - .05f);
                returning = true;
            }
        }

        void PlaceVisual(float deltaTime)
        {
            if (visual == null) return;
            rotation = Quaternion.AngleAxis(1100f * deltaTime, Vector3.up) * rotation;
            visual.transform.rotation = rotation;
            visual.transform.position = center - visual.transform.TransformVector(mesh.sharedMesh.bounds.center);
        }

        public void Catch()
        {
            FinishThrow();
            if (visual != null) Destroy(visual);
            visual = null;
            if (heldRenderers != null)
                for (int i = 0; i < heldRenderers.Length; i++) if (heldRenderers[i] != null) heldRenderers[i].enabled = heldVisible[i];
            if (heldColliders != null)
                for (int i = 0; i < heldColliders.Length; i++) if (heldColliders[i] != null)
                    heldColliders[i].enabled = heldCollision[i] && heldColliders[i] != attack.stickHitbox;
            heldRenderers = null;
            heldColliders = null;
            returning = false;
            nextThrow = Time.time + cooldown;
        }
        void FinishThrow()
        {
            if (!IsThrowing) return;
            IsThrowing = false;
            movement?.EndAttackFacing();
            if (attack != null && attack.animator != null)
                attack.animator.CrossFadeInFixedTime("Base Layer.Locomotion", .12f, 0, 0f);
        }
        void OnDisable() => Catch();
        void OnDestroy() => Catch();
    }
}
