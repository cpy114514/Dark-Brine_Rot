using UnityEngine;

namespace Mavis
{
    public partial class SahurAttack
    {
        // The controller can finish the last piece of rush travel on the frame
        // the animation exits its damage window. Query that actual travel before
        // restoring the usual pose-window hitboxes. Never use requested travel:
        // CharacterController.Move may have stopped against scenery.
        internal void ScanTwinBladeRushTravel(Vector3 travel)
        {
            if (!UsesTwinBlades || !IsHeavyAttackActive || !stickHitbox || !animator ||
                travel.sqrMagnitude < .000001f || localStop && localStop.IsStopped) return;
            var first = stickHitbox;
            bool firstEnabled = first.enabled;
            bool secondEnabled = secondBlade && secondBlade.enabled;
            first.enabled = true;
            if (secondBlade) secondBlade.enabled = true;
            Physics.SyncTransforms();
            try
            {
                swingDirection = travel;
                GetWeaponVolume(out var a, out var b, out float radius);
                ScanWeaponSweep(a - travel, b - travel, a, b, radius);
                if (secondBlade)
                {
                    stickHitbox = secondBlade;
                    GetWeaponVolume(out a, out b, out radius);
                    ScanWeaponSweep(a - travel, b - travel, a, b, radius);
                }
            }
            finally
            {
                stickHitbox = first;
                first.enabled = firstEnabled;
                if (secondBlade) secondBlade.enabled = secondEnabled;
            }
        }
    }
}
