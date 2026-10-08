using UnityEngine;

namespace Mavis
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-20)]
    public sealed class SahurTwinBladeLocomotion : MonoBehaviour
    {
        SahurAttack attack;
        public static bool IsMoving(Animator animator) => animator && animator.GetFloat("Speed") > .15f;

        void Awake()
        {
            attack = GetComponent<SahurAttack>();
        }

        public void Refresh()
        {
            if (!attack || !attack.animator) return;
            // Restore older saved holding overrides once. Locomotion always uses the same graph.
            var animator = attack.animator;
            var legacy = animator.runtimeAnimatorController as AnimatorOverrideController;
            if (legacy && legacy.name == "SahurCapriHolding")
                animator.runtimeAnimatorController = legacy.runtimeAnimatorController;
        }

        void Update() => Refresh();
    }
}
