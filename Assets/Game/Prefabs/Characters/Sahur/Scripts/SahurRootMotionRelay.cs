using Mavis;
using UnityEngine;

/// <summary>
/// Forwards the imported humanoid attack root motion to Sahur's collision root.
/// The Animator lives on the visual child, so its own transform must not move.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class SahurRootMotionRelay : MonoBehaviour
{
    Animator animator;
    ThirdPersonPlayerController movement;
    SahurAttack attack;

    public void Initialize(ThirdPersonPlayerController owner, SahurAttack combat)
    {
        animator = GetComponent<Animator>();
        movement = owner;
        attack = combat;
    }

    void OnAnimatorMove()
    {
        if (movement == null || !movement.isActiveAndEnabled || attack == null || !attack.UsesAnimationRootMotion || Time.deltaTime<=0f)
            return;

        movement.ApplyAttackRootMotion(animator.deltaPosition, animator.deltaRotation);
    }
}
