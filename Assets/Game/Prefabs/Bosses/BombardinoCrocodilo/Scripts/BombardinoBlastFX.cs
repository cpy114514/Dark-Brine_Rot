using UnityEngine;
namespace Mavis
{
    public sealed class BombardinoBlastFX : MonoBehaviour
    {
        public Material material;
        float age;
        ParticleSystem particles;
        public void Initialize(float radius)
        {
            age = 0f;
            BossCombatVfx.Burst(transform.position+Vector3.up*.25f,Vector3.up,new Color(1,.55f,.15f,.8f),18,radius,true);
            BossCombatVfx.Burst(transform.position,Vector3.up,new Color(.26f,.23f,.19f,.65f),12,radius*.75f);
            if (particles == null)
            {
                material = NaturalParticleEffects.SmokeMaterial("Impact dust and smoke", Color.white);
                particles = NaturalParticleEffects.Emitter(transform, "Impact smoke", material, 32, -.025f);
            }
            else particles.Clear();
            var main=particles.main;main.startRotation=new ParticleSystem.MinMaxCurve(0f,Mathf.PI*2f);
            var rotation=particles.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(-.15f,.15f);
            var size = particles.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, .6f, 1, 1.7f));
            var drag = particles.limitVelocityOverLifetime; drag.enabled = true; drag.dampen = .35f; drag.limit = 3f;
            int count = NaturalParticleEffects.BurstBudget(transform.position, 24);
            for (int i = 0; i < count; i++)
            {
                Vector3 outward = Random.insideUnitSphere; outward.y = Mathf.Abs(outward.y);
                particles.Emit(new ParticleSystem.EmitParams {
                    position = transform.position + outward * radius * .14f,
                    velocity = outward * Random.Range(2f, 6f),
                    startSize = Random.Range(.32f, .65f) * Mathf.Clamp(radius, 1, 5),
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
