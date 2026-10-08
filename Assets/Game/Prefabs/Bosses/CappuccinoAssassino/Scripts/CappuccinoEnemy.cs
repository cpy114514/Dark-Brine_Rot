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
        [Header("Authored animation")]
        public Animator animator;
        public CappuccinoMotionRig motionRig;
        public AnimationClip attackClip;
        public AnimationClip deathClip;
        public bool attackOnHit = true;
        [Min(0f)] public float hitReactionDuration = 0.22f;
        [Min(0.1f)] public float deathDuration = 0.9f;
        [Min(0f)] public float removeAfterDeath = 4f;
        public Color hitColor = new Color(1f, 0.32f, 0.18f, 1f);
        public UnityEvent onDefeated = new UnityEvent();

        Health health;
        CappuccinoUltimate ultimate;
        Vector3 restPosition;
        Quaternion restRotation;
        float hitTime = -100f;
        float deathTime = -1f;
        float modelHeight;
        float nextAttackTime;
        static readonly int AttackTrigger = Animator.StringToHash("Attack");
        static readonly int DeathTrigger = Animator.StringToHash("Die");

        void Awake()
        {
            if (GetComponent<CappuccinoWeaponLoot>() == null) gameObject.AddComponent<CappuccinoWeaponLoot>();
            FitBodyCollision();
            health = GetComponent<Health>();
            ultimate = GetComponent<CappuccinoUltimate>();
            health.OnDamaged ??= new UnityEvent<float>();
            health.OnDeath ??= new UnityEvent();
            health.OnHealthChanged ??= new UnityEvent<float, float>();
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

        public void FitBodyCollision()
        {
            var motor=GetComponent<CharacterController>();
            if(motor==null||bodyRenderers==null)return;
            // Fit the cup, excluding its swords. The old 1.65 m radius left the
            // sides of the 4.48 m wide cup outside the physical character body.
            foreach(var renderer in bodyRenderers)
            {
                if(renderer==null||renderer.name!="Capuchino")continue;
                Bounds source=renderer.localBounds;
                var frame=renderer is SkinnedMeshRenderer skin&&skin.rootBone!=null?skin.rootBone:renderer.transform;
                var matrix=transform.worldToLocalMatrix*frame.localToWorldMatrix;
                Bounds local=new Bounds(matrix.MultiplyPoint3x4(source.center),Vector3.zero);
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    local.Encapsulate(matrix.MultiplyPoint3x4(source.center+Vector3.Scale(source.extents,new Vector3(x,y,z))));
                motor.radius=Mathf.Max(local.extents.x,local.extents.z)+.06f;
                motor.height=Mathf.Max(local.size.y,motor.radius*2);
                motor.center=local.center;
                motor.detectCollisions=true;
                bodyCollider=motor;
                break;
            }
        }

        void OnHit(float amount)
        {
            hitTime = Time.time;
            if (attackOnHit && health != null && !health.IsDead) PlayAttack();
        }

        [ContextMenu("Play Attack (Play Mode)")]
        void PreviewAttack() { if (Application.isPlaying) PlayAttack(); }

        public void PlayAttack() => TryPlayAttack();

        public bool TryPlayAttack() => TryPlayAttack(Time.time);

        // The boss action clock owns the authored pose and the matching damage window.
        internal void SetBossPose(float normalized, bool mirrored = false)
        {
            if (animator == null || deathTime >= 0f) return;
            animator.ResetTrigger(AttackTrigger);
            animator.speed = 0f;
            animator.Play(mirrored ? "AttackMirrored" : "Attack", 0, Mathf.Clamp01(normalized));
        }

        internal void ResumeLocomotion()
        {
            if (animator == null || deathTime >= 0f) return;
            animator.speed = 1f;
            animator.ResetTrigger(AttackTrigger);
            animator.CrossFadeInFixedTime("Idle", .14f);
        }

        internal bool TryPlayAttack(float now)
        {
            if (animator == null || deathTime >= 0f ||
                (health != null && health.IsDead) || (ultimate != null && ultimate.AttackLocked) || now < nextAttackTime) return false;
            animator.SetTrigger(AttackTrigger);
            nextAttackTime = now + (attackClip != null ? attackClip.length : 1.5f) + 0.25f;
            return true;
        }

        void OnDeath()
        {
            if (deathTime >= 0f) return;
            deathTime = Time.time;
            GetComponent<CappuccinoWeaponLoot>()?.Drop();
            if (bodyRenderers != null) foreach (var renderer in bodyRenderers)
                if (renderer && renderer.name == "Katana") renderer.enabled = false;
            if (bodyCollider != null) bodyCollider.enabled = false;
            if (animator != null && deathClip != null)
            {
                animator.speed = 1f;
                animator.ResetTrigger(AttackTrigger);
                animator.SetTrigger(DeathTrigger);
                motionRig?.SetDeathGrounding();
            }
            onDefeated.Invoke();
            float fallDuration = deathClip != null ? deathClip.length + 0.65f : deathDuration;
            if (removeAfterDeath > 0f) Destroy(gameObject, Mathf.Max(removeAfterDeath, fallDuration));
        }

        void Update()
        {
            float hit = Mathf.Clamp01(1f - (Time.time - hitTime) / Mathf.Max(0.01f, hitReactionDuration));
            if (visual == null) return;
            if (deathTime >= 0f)
            {
                if (animator != null && deathClip != null)
                {
                    visual.localPosition = restPosition;
                    visual.localRotation = restRotation;
                    return;
                }
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
