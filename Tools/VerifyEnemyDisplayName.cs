using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Mavis;
public static class VerifyEnemyDisplayName
{
    public static object Run()
    {
        bool had = PlayerPrefs.HasKey(GameLocalization.PreferenceKey);
        int previous = PlayerPrefs.GetInt(GameLocalization.PreferenceKey, 0);
        GameObject actor = null;
        try
        {
            actor = new GameObject("Enemy name verification");
            actor.AddComponent<NailongHealth>();
            var bar = actor.GetComponent<NailongHealthBar>();
            foreach (var language in new[] { GameLanguage.English, GameLanguage.SimplifiedChinese })
            {
                GameLocalization.SetLanguage(language);
                string expected = language == GameLanguage.English ? "Karen Fairy" : "猪妖小仙人";
                foreach (string key in new[] { "enemy.name", "Nailong", "奶龙" })
                    if (GameLocalization.Text(key) != expected) throw new Exception("Enemy name mismatch: " + key + " = " + GameLocalization.Text(key));
                if (!GameLocalization.Text("gear.details").Contains(expected)) throw new Exception("Gear description: " + GameLocalization.Text("gear.details"));
                if (!GameLocalization.Text("●  Nailong").Contains(expected)) throw new Exception("Map legend mismatch");
                foreach (var item in Resources.LoadAll<EquipmentItem>("Equipment/Nailong"))
                    if (!item.LocalizedName.Contains(expected)) throw new Exception("Equipment name mismatch: " + item.LocalizedName);
                bar.Refresh(0);
                var label = actor.GetComponentsInChildren<Text>(true).Single();
                if (!label.text.Contains(expected)) throw new Exception("Health label mismatch: " + label.text);
                var table = (System.Collections.Generic.Dictionary<string,string[]>)typeof(GameLocalization).GetField("table", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                if (table.Values.SelectMany(pair => pair).Any(text => text.Contains("Nailong") || text.Contains("奶龙"))) throw new Exception("Old enemy name in translation output");
            }
            return new { success = true, bilingualNames = true, healthBar = true, mapLegend = true, equipment = true, legacyAliases = true };
        }
        finally
        {
            if (actor) UnityEngine.Object.Destroy(actor);
            GameLocalization.SetLanguage(previous == 1 ? GameLanguage.SimplifiedChinese : GameLanguage.English);
            if (!had) PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey); else PlayerPrefs.SetInt(GameLocalization.PreferenceKey, previous);
            PlayerPrefs.Save();
        }
    }
}
