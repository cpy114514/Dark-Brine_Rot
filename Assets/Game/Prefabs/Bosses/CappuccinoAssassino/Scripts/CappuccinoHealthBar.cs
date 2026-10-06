using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    public sealed class CappuccinoHealthBar : MonoBehaviour
    {
        public Health health;
        public Image fill;
        public Image damageTrail;
        public Text valueText;
        public Canvas canvas;
        [Min(1f)] public float visibleDistance = 65f;
        [Min(0.1f)] public float trailSpeed = 1.2f;
        Camera viewer;
        float trail = 1f;
        int previousHealth = -1;
        int previousMax = -1;
        CappuccinoAI ai;

        void Awake()
        {
            ai=health ? health.GetComponent<CappuccinoAI>() : GetComponentInParent<CappuccinoAI>();
            if(fill)fill.color=GameUITheme.Foreground;
            if(damageTrail)damageTrail.color=GameUITheme.Muted;
            if(valueText)valueText.color=GameUITheme.Foreground;
        }

        void LateUpdate()
        {
            if (health == null) return;
            if (viewer == null || !viewer.isActiveAndEnabled) viewer = Camera.main;
            bool visible = !health.IsDead && viewer != null && !GameUITheme.ModalOpen && (ai==null || !ai.FightActive) &&
                (viewer.transform.position - transform.position).sqrMagnitude < visibleDistance * visibleDistance;
            if (canvas != null) canvas.enabled = visible;
            if (visible) transform.rotation = Quaternion.LookRotation(transform.position - viewer.transform.position, viewer.transform.up);
            float ratio = health.Ratio;
            if (fill != null) fill.fillAmount = ratio;
            trail = Mathf.MoveTowards(trail, ratio, Time.deltaTime * trailSpeed);
            if (damageTrail != null) damageTrail.fillAmount = trail;
            int hp = Mathf.CeilToInt(health.currentHealth), maximum = Mathf.CeilToInt(health.maxHealth);
            if (valueText != null && (hp != previousHealth || maximum != previousMax))
            {
                valueText.text = hp + " / " + maximum;
                previousHealth = hp; previousMax = maximum;
            }
        }
    }
}
