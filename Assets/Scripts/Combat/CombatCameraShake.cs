using UnityEngine;

namespace Mavis
{
    // Remove our additive offset BEFORE the follow camera reads its previous position.
    [DefaultExecutionOrder(1000)]
    public sealed class CombatCameraShake : MonoBehaviour
    {
        float remaining, duration, amplitude;
        Vector3 offset;
        Quaternion rotation = Quaternion.identity;
        bool applied;
        public bool IsShaking => remaining > 0f;
        public void Pulse(float strength, float seconds)
        {
            amplitude = Mathf.Max(amplitude, Mathf.Clamp(strength, 0f, 0.075f));
            remaining = duration = Mathf.Max(remaining, Mathf.Clamp(seconds, 0.01f, 0.22f));
        }
        void RemoveOffset()
        {
            if (!applied) return;
            transform.position -= offset;
            transform.rotation *= Quaternion.Inverse(rotation);
            applied = false;
        }
        void Update()
        {
            RemoveOffset();
            if (Time.timeScale <= 0f) return;
            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            if (remaining == 0f) amplitude = 0f;
        }
        void LateUpdate()
        {
            if (remaining <= 0f || Time.timeScale <= 0f) return;
            float a = amplitude * Mathf.Pow(remaining / duration, 2f);
            float phase = Time.time * 150f;
            offset = transform.right * (Mathf.Sin(phase) * a) + transform.up * (Mathf.Sin(phase * 1.37f) * a * 0.65f);
            rotation = Quaternion.Euler(Mathf.Sin(phase * 0.8f) * a * 7f, 0f, Mathf.Sin(phase) * a * 5f);
            transform.position += offset;
            transform.rotation *= rotation;
            applied = true;
        }
        void OnDisable() { RemoveOffset(); remaining = amplitude = 0f; }
    }
}
