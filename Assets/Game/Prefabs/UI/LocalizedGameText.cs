using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Tracks literal labels without overwriting dynamic numbers or user-authored content.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(2000)]
    public sealed class LocalizedGameText : MonoBehaviour
    {
        Text legacy;
        TMP_Text tmp;
        string source, output;
        public static void BindTree(Transform root)
        {
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) Bind(label);
            foreach (var label in root.GetComponentsInChildren<Text>(true)) Bind(label);
        }
        public static void Bind(Component label)
        {
            var binding = label.GetComponent<LocalizedGameText>();
            if (!binding) binding = label.gameObject.AddComponent<LocalizedGameText>();
            binding.ReadAndRefresh();
        }
        void Awake()
        {
            legacy = GetComponent<Text>(); tmp = GetComponent<TMP_Text>();
            source = tmp ? tmp.text : legacy ? legacy.text : "";
            if (legacy && GameLocalization.Font) legacy.font = GameLocalization.Font;
            if (tmp && GameLocalization.TMPFont)
            {
                // A bundled asset owns its atlas/material across scene unloads. Never add
                // scene-owned runtime font materials to a shared project's fallback table.
                tmp.font = GameLocalization.TMPFont;
            }
            GameLocalization.Changed += LanguageChanged;
            Refresh();
        }
        void LateUpdate() { ReadAndRefresh(); }
        void ReadAndRefresh()
            => ReadAndRefresh(false);
        void LanguageChanged() => ReadAndRefresh(true);
        void ReadAndRefresh(bool force)
        {
            if(tmp && GameLocalization.TMPFont && tmp.font!=GameLocalization.TMPFont)tmp.font=GameLocalization.TMPFont;
            string current = tmp ? tmp.text : legacy ? legacy.text : "";
            if(!force && current==output)return;
            if (current != output) source = current;
            Refresh();
        }
        void Refresh()
        {
            output = GameLocalization.Text(source);
            if (tmp) { if(tmp.text != output) tmp.text = output; }
            else if (legacy && legacy.text != output) legacy.text = output;
        }
        void OnDestroy() { GameLocalization.Changed -= LanguageChanged; }
    }
}
