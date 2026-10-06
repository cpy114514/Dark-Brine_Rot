using UnityEngine;

namespace Mavis
{
    public sealed class BombardinoBossHUD : MonoBehaviour
    {
        BombardinoBoss boss; Health health; BossHudPanel hud; float deadAge;
        public float DisplayedFraction=>hud!=null?hud.DisplayedFraction:0;
        public bool IsVisible=>hud!=null&&hud.IsVisible;
        void Awake()=>Initialize();
        public void Initialize()
        {
            if(hud!=null)return;boss=GetComponent<BombardinoBoss>();health=GetComponent<Health>();
            hud=new BossHudPanel(transform,"Bombardino boss health");
        }
        void LateUpdate()=>Refresh(Time.deltaTime);
        public void Refresh(float dt)
        {
            if(hud==null)Initialize();if(boss==null||health==null)return;
            if(health.IsDead)deadAge+=Mathf.Max(0,dt);
            var viewer=Camera.main;
            bool visible=viewer&&(boss.FightActive||health.IsDead&&deadAge<2.5f);
            hud.Refresh(visible,viewer?(transform.position-viewer.transform.position).sqrMagnitude:float.PositiveInfinity,
                health.Ratio,health.currentHealth,health.maxHealth,health.IsDead?"BOSS DEFEATED":"BOMBARDINO CROCODILO",
                boss.Enraged?"AERIAL HUNTER  /  OVERDRIVE":"AERIAL HUNTER  /  PHASE I",boss.Hint,dt);
        }
        void OnDisable()=>hud?.Hide();
        void OnDestroy()=>hud?.Dispose();
    }
}
