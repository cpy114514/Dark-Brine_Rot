using UnityEngine;
namespace Mavis
{
    public sealed class BombardinoBlastFX : MonoBehaviour
    {
        // Retained for prefab compatibility; shared VFX materials now own rendering.
        public Material material;
        float age;
        public void Initialize(float radius)
        {
            age=0;var world=BrineVfxWorld.Get();
            if(world)world.Emit(GetInstanceID(),transform.position+Vector3.up*.08f,Vector3.up,Mathf.Clamp(radius,1,12),1.2f,true,false,true);
        }
        void Update()=>Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            if(PauseSettingsMenu.IsOpen||dt<=0)return;age+=dt;
            if(age>1.7f){if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);}
        }
    }
}
