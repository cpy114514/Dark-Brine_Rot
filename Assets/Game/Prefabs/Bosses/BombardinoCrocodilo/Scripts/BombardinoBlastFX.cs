using UnityEngine;
namespace Mavis
{
    public sealed class BombardinoBlastFX : MonoBehaviour
    {
        public Material material;
        float age;
        public void Initialize(float radius)
        {
            material = NaturalParticleEffects.Material("Impact dust and smoke", Color.white);
            var particles = NaturalParticleEffects.Emitter(transform, "Impact smoke", material, 48, -.025f);
            var size = particles.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, .6f, 1, 1.7f));
            var drag = particles.limitVelocityOverLifetime; drag.enabled = true; drag.dampen = .35f; drag.limit = 3f;
            for (int i = 0; i < 32; i++)
            {
                Vector3 outward = Random.insideUnitSphere; outward.y = Mathf.Abs(outward.y);
                particles.Emit(new ParticleSystem.EmitParams {
                    position = transform.position + outward * radius * .14f,
                    velocity = outward * Random.Range(2f, 6f),
                    startSize = Random.Range(.22f, .55f) * Mathf.Clamp(radius, 1, 5),
                    startLifetime = Random.Range(.7f, 1.5f),
                    startColor = Color.Lerp(new Color(.30f, .28f, .25f, .38f), new Color(.42f, .39f, .34f, .5f), Random.value)
                }, 1);
            }
        }
        void Update() => Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            if (PauseSettingsMenu.IsOpen || dt <= 0) return;
            age += dt;
            if (age > 1.7f) {if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);}
        }
        void OnDestroy() {if (material != null) {if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);}}
    }
}
