using UnityEngine;

namespace Mavis
{
    // The authored finisher owns its turn; only the controller owns rush travel.
    [DisallowMultipleComponent, DefaultExecutionOrder(-5)]
    public sealed class SahurTwinBladeMotion : MonoBehaviour
    {
        [Min(0)] public float rushDistance=16f;
        [Tooltip("Partial charges travel farther but deal less damage. Full charge retains rushDistance.")]
        [Min(1)] public float partialRushDistanceMultiplier = 1.25f;
        public float CurrentRushDistance { get; private set; }
        public Vector2 RushWindow => SahurAttack.TwinBladeRushWindow;
        SahurAttack attack;
        SahurWeaponLoadout weapons;
        ThirdPersonPlayerController movement;
        CharacterController body;
        PlayerHealth health;
        CombatImpactVisuals pressure;
        bool rushing;
        bool movingRush;
        bool RushAllowed => isActiveAndEnabled && weapons && weapons.UsesTwinBlades && attack && attack.isActiveAndEnabled &&
            attack.IsHeavyAttackActive && (!health || health.currentHealth > 0) && !attack.CinematicHandoffPending && movement && movement.isActiveAndEnabled &&
            !movement.Swimming && !movement.ExternalMovementLock && !movement.ExternalControlLock && body && body.enabled;
        // Read the live attack clock, not a timed buff: cancel, unequip and disable end immunity immediately.
        // The final low-rate movement segment remains protected until CharacterController.Move returns.
        public bool IsDamageImmune => RushAllowed && (movingRush || attack.HeavyAttackPhase >= RushWindow.x &&
            (attack.HeavyAttackPhase < RushWindow.y || rushing && lastRushProgress < 1f));
        float lastRushProgress;
        Vector3 rushDirection;
        void Awake()
        {
            attack = GetComponent<SahurAttack>(); weapons = GetComponent<SahurWeaponLoadout>();
            movement = GetComponent<ThirdPersonPlayerController>(); body = GetComponent<CharacterController>(); health = GetComponent<PlayerHealth>();
        }
        void LateUpdate()
        {
            if (PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput || Time.deltaTime <= 0) return;
            if (RushAllowed)
            {
                if (!rushing)
                {
                    rushing = true; lastRushProgress = 0; rushDirection=transform.forward;
                    CurrentRushDistance = rushDistance * Mathf.Lerp(partialRushDistanceMultiplier, 1f, attack.ReleasedCharge01);
                }
                float progress = Mathf.SmoothStep(0,1,Mathf.InverseLerp(RushWindow.x,RushWindow.y,attack.HeavyAttackPhase));
                float distance = Mathf.Max(0,progress-lastRushProgress) * CurrentRushDistance;
                // The physical controller limits the rush against walls and enemy bodies.
                if (distance > 0 && movement.CanUseGroundAttack)
                {
                    if(lastRushProgress<=0)
                    {
                        if(!pressure)pressure=GetComponent<CombatImpactVisuals>();
                        if(pressure)pressure.Rush(body.bounds.center-rushDirection*.5f,rushDirection);
                    }
                    movingRush = true;
                    try { body.Move(rushDirection * distance); }
                    finally { movingRush = false; }
                }
                lastRushProgress = progress;
            }
            else { rushing = false; lastRushProgress = 0; }
        }
        void OnDisable() { rushing=false; movingRush=false; lastRushProgress=0; }
    }
}
