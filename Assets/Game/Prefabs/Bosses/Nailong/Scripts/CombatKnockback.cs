using UnityEngine;

namespace Mavis
{
    // Collision-resolved displacement, applied before the character's camera
    // LateUpdate. Never teleport the victim through props or the shoreline.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class CombatKnockback : MonoBehaviour
    {
        CharacterController controller;
        Vector3 remaining;
        public bool IsBeingPushed => remaining.sqrMagnitude > 0.0001f;

        void Awake() => controller = GetComponent<CharacterController>();
        public void Push(Vector3 displacement)
        {
            displacement.y = 0f;
            remaining = Vector3.ClampMagnitude(remaining + displacement, 5f);
        }
        void Update() => Simulate(Time.deltaTime);
        public void Simulate(float deltaTime)
        {
            if (PauseSettingsMenu.IsOpen || deltaTime <= 0f || !IsBeingPushed || controller == null || !controller.enabled) return;
            Vector3 step = remaining * (1f - Mathf.Exp(-12f * deltaTime));
            remaining -= step;
            controller.Move(step);
        }
        void OnDisable() => remaining = Vector3.zero;
    }
}
