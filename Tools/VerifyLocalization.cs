using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Mavis;
public static class VerifyLocalization
{
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    static bool Has(string text) => UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(t=>t.text.Contains(text));
    public static async Task<object> Run()
    {
        bool had=PlayerPrefs.HasKey(GameLocalization.PreferenceKey); int previous=PlayerPrefs.GetInt(GameLocalization.PreferenceKey,0);
        float oldTime=Time.timeScale;
        GameObject actor=null, menuObject=null;
        try
        {
            PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey);
            typeof(GameLocalization).GetField("loaded",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);
            Check(GameLocalization.Language==GameLanguage.English,"Fresh install must default to English");
            Check(GameLocalization.Font,"Bundled Chinese font missing");
            Check(GameLocalization.TMPFont.HasCharacters("设置语言猪妖小仙人暗潮抽象之岛",out uint[] missing,true,true),"Chinese font missing glyphs");
            var menuPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab");
            menuObject=UnityEngine.Object.Instantiate(menuPrefab); var menu=menuObject.GetComponent<PauseSettingsMenu>();
            menu.Open(); typeof(PauseSettingsMenu).GetMethod("ShowSettings",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(menu,null);
            await Task.Delay(150);
            var language=menu.options.Single(o=>o.key=="language");
            Check(language.value.text=="English","Language option absent");
            var row=language.button.transform.parent.parent;
            Check(row.name=="language row" && row.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="LANGUAGE"),"Language row layout invalid");
            language.button.onClick.Invoke(); await Task.Delay(150);
            Check(GameLocalization.Language==GameLanguage.SimplifiedChinese && language.value.text=="简体中文","Language button did not switch");
            Check(menu.sectionTitle.text=="游戏","Settings labels did not update");
            Check(PlayerPrefs.GetInt(GameLocalization.PreferenceKey,-1)==1,"Language not saved");
            typeof(GameLocalization).GetField("loaded",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);
            Check(GameLocalization.Language==GameLanguage.SimplifiedChinese,"Stored language not reloaded");
            menu.Resume();
            actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(1000,1000,1000),Quaternion.identity);
            actor.GetComponent<ThirdPersonPlayerController>().enabled=false;
            await Task.Delay(100);
            var loadout=actor.GetComponent<SahurLoadoutUI>(); loadout.SetOpen(true);
            Check(Has("技能还在修炼"),"Chinese skill description missing");
            GameLocalization.SetLanguage(GameLanguage.English); await Task.Delay(100);
            Check(Has("No skills available yet") && Has("GEAR & SKILLS"),"Loadout did not switch to English");
            loadout.SelectEquipment(SahurLoadoutUI.EquipmentSlot.Feet);
            Check(Has("Defeat Karen Fairy"),"Equipment description untranslated");
            foreach(var item in Resources.LoadAll<EquipmentItem>("Equipment/Nailong")) actor.GetComponent<EquipmentInventory>().Collect(item);
            loadout.SelectEquipment(SahurLoadoutUI.EquipmentSlot.Feet);
            Check(Has("Karen Fairy Clogs"),"English collected equipment name missing");
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese); await Task.Delay(100);
            Check(Has("猪妖小仙人洞洞鞋") && Has("击败猪妖小仙人"),"Chinese collected gear missing");
            GameLocalization.SetLanguage(GameLanguage.English); await Task.Delay(100);
            loadout.SetOpen(false); await Task.Delay(50);
            var map=actor.GetComponent<IslandMapUI>(); map.SetOpen(true); await Task.Delay(150);
            Check(Has("ISLAND MAP") && Has("Map width"),"Map not English");
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese); await Task.Delay(100);
            Check(language.value.text=="简体中文","External language change left a stale setting value");
            Check(Has("岛屿地图") && Has("地图宽度"),"Map did not switch to Chinese");
            map.SetOpen(false); await Task.Delay(50);
            actor.GetComponent<PlayerHealth>().ApplyDamage(9999,actor.transform.position);
            Check(Has("这把先寄了"),"Chinese death text missing");
            GameLocalization.SetLanguage(GameLanguage.English); await Task.Delay(100);
            Check(Has("YOU DIED"),"Death screen did not switch to English");
            actor.GetComponent<PlayerDeathRespawn>().Respawn();
            menu.Open(); typeof(PauseSettingsMenu).GetMethod("ShowSettings",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(menu,null);
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese); await Task.Delay(100);
            var shot=ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.GetFullPath("Tools/ChineseSettings.png"),shot.EncodeToPNG()); UnityEngine.Object.Destroy(shot);
            return new {success=true,defaultEnglish=true,languageButton=true,persisted=true,settings=true,loadout=true,map=true,death=true,chineseGlyphs=true};
        }
        finally
        {
            if(menuObject) { menuObject.GetComponent<PauseSettingsMenu>().Resume(); UnityEngine.Object.Destroy(menuObject); }
            if(actor) UnityEngine.Object.Destroy(actor);
            GameLocalization.SetLanguage(previous==1 ? GameLanguage.SimplifiedChinese : GameLanguage.English);
            if(!had) PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey); else PlayerPrefs.SetInt(GameLocalization.PreferenceKey,previous);
            PlayerPrefs.Save(); Time.timeScale=oldTime;
        }
    }
}
