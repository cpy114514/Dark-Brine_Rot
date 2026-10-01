using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class NailongHealthBar : MonoBehaviour
    {
        NailongHealth health;
        SkinnedMeshRenderer skin;
        RectTransform bar;
        Image fill;
        Image chip;
        Text label;
        float delayedFraction = 1f;
        Sprite white;
        public float DisplayedFraction => fill != null ? fill.fillAmount : 0f;
        public bool IsVisible => bar != null && bar.gameObject.activeSelf;

        void Awake()
        {
            health = GetComponent<NailongHealth>();
            var texture = Texture2D.whiteTexture;
            white = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            skin = GetComponentInChildren<SkinnedMeshRenderer>();
            var obj = new GameObject("Nailong Health Bar", typeof(RectTransform), typeof(Canvas));
            bar = (RectTransform)obj.transform;
            bar.SetParent(transform, false);
            bar.sizeDelta = new Vector2(180f, 42f);
            obj.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var background = MakeImage("Background", GameUITheme.Gray(0,.95f), 0f);
            background.rectTransform.sizeDelta = new Vector2(184, 16);
            chip = MakeImage("Damage trail", GameUITheme.Muted, 1f);
            fill = MakeImage("Health", GameUITheme.Foreground, 1f);
            var textObject = new GameObject("Name and HP", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(bar, false);
            label = textObject.GetComponent<Text>();
            label.font = GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            GameUITheme.OutlineSymbol(label);
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(180, 22);
            label.rectTransform.anchoredPosition = new Vector2(0, 18);
            Refresh(0f);
        }

        Image MakeImage(string name, Color color, float filled)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(bar, false);
            var image = obj.GetComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(180, 12);
            image.color = color;
            image.sprite = white;
            image.raycastTarget = false;
            if (filled > 0f)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = 0;
            }
            return image;
        }

        void LateUpdate() => Refresh(Time.deltaTime);
        public void Refresh(float deltaTime)
        {
            if (health == null || bar == null) return;
            var camera = Camera.main;
            bool visible = enabled && !health.IsDead && camera != null && !PauseSettingsMenu.IsOpen && !SahurLoadoutUI.IsOpen &&
                !IslandMapUI.BlocksInput && !PlayerDeathRespawn.IsOpen &&
                Vector3.Distance(camera.transform.position, transform.position) < 70f;
            bar.gameObject.SetActive(visible);
            float fraction = health.HealthFraction;
            fill.fillAmount = fraction;
            delayedFraction = fraction >= delayedFraction ? fraction :
                Mathf.MoveTowards(delayedFraction, fraction, Mathf.Max(0f, deltaTime) * .6f);
            chip.fillAmount = delayedFraction;
            label.text = GameLocalization.Text("enemy.name") + "  " + Mathf.CeilToInt(health.currentHealth) + " / " + Mathf.CeilToInt(health.maxHealth);
            if (!visible) return;
            Vector3 anchor = skin != null ? new Vector3(skin.bounds.center.x, skin.bounds.max.y + .5f, skin.bounds.center.z)
                : transform.position + Vector3.up * 5f;
            bar.position = anchor;
            bar.rotation = camera.transform.rotation;
            Vector3 scale = transform.lossyScale;
            bar.localScale = new Vector3(.012f / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                .012f / Mathf.Max(.001f, Mathf.Abs(scale.y)), .012f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
        }
        void OnDisable() { if (bar != null) bar.gameObject.SetActive(false); }
        void OnDestroy() { if (white != null) Destroy(white); }
    }
}
