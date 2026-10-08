using UnityEngine;

namespace Mavis
{
    // Capri's cyclone and forward rush are layered over his retargeted slash.
    [DisallowMultipleComponent, DefaultExecutionOrder(-5)]
    public sealed class SahurTwinBladeMotion : MonoBehaviour
    {
        SahurAttack attack;
        SahurWeaponLoadout weapons;
        ThirdPersonPlayerController movement;
        CharacterController body;
        Quaternion spinStart, lastSpin;
        bool spinning, rushing;
        float lastRushProgress;
        void Awake()
        {
            attack = GetComponent<SahurAttack>(); weapons = GetComponent<SahurWeaponLoadout>();
            movement = GetComponent<ThirdPersonPlayerController>(); body = GetComponent<CharacterController>();
        }
        void LateUpdate()
        {
            if (PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput || Time.deltaTime <= 0) return;
            bool active = weapons && weapons.UsesTwinBlades && attack && attack.enabled && movement && movement.enabled && !movement.Swimming;
            if (active && attack.CurrentComboStage == 2)
            {
                if (!spinning) { spinStart = transform.rotation; spinning = true; }
                float turn = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.82f,attack.CurrentAttackPhase));
                lastSpin = Quaternion.AngleAxis(turn * 360f,Vector3.up) * spinStart;
                transform.rotation = lastSpin;
            }
            else RestoreSpin();
            if (active && attack.IsHeavyAttackActive && body && body.enabled && !movement.ExternalMovementLock && !movement.ExternalControlLock)
            {
                if (!rushing) { rushing = true; lastRushProgress = 0; }
                float progress = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.58f,attack.CurrentAttackPhase));
                float distance = Mathf.Max(0,progress-lastRushProgress) * 3f;
                lastRushProgress = progress;
                // The physical controller limits the rush against walls and enemy bodies.
                if (distance > 0 && movement.CanUseGroundAttack) body.Move(transform.forward * distance);
            }
            else { rushing = false; lastRushProgress = 0; }
        }
        void RestoreSpin()
        {
            if (!spinning) return;
            if (Quaternion.Angle(transform.rotation,lastSpin)<.1f) transform.rotation=spinStart;
            spinning=false;
        }
        void OnDisable() { RestoreSpin(); rushing=false; }
    }
}
