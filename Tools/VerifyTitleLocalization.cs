using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Mavis;
public static class VerifyTitleLocalization
{
    public static async Task<object> Run()
    {
        bool had=PlayerPrefs.HasKey(GameLocalization.PreferenceKey); int old=PlayerPrefs.GetInt(GameLocalization.PreferenceKey,0);
        float time=Time.timeScale;
        const string path="Assets/Scenes/Main Menu/MainMenu.unity";
        var operation=SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
        while(!operation.isDone) await Task.Delay(40);
        var scene=SceneManager.GetSceneByPath(path);
        try
        {
            var controller=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MainMenuController>(true)).Single();
            var labels=controller.GetComponentsInChildren<TMP_Text>(true);
            GameLocalization.SetLanguage(GameLanguage.English); await Task.Delay(100);
            if(!labels.Any(l=>l.text=="NEW GAME") || !labels.Any(l=>l.text=="MAIN MENU")) throw new Exception("English title screen missing");
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese); await Task.Delay(100);
            if(!labels.Any(l=>l.text=="开始游戏") || !labels.Any(l=>l.text=="主菜单")) throw new Exception("Chinese title screen missing");
            controller.Settings(); await Task.Delay(100);
            if(!controller.settingsMenu.options.Any(o=>o.key=="language")) throw new Exception("Title settings did not inherit language option");
            controller.settingsMenu.Resume();
            return new {success=true,titleEnglish=true,titleChinese=true,titleSettingsLanguage=true};
        }
        finally
        {
            var unload=SceneManager.UnloadSceneAsync(scene); while(!unload.isDone) await Task.Delay(40);
            GameLocalization.SetLanguage(old==1 ? GameLanguage.SimplifiedChinese : GameLanguage.English);
            if(!had) PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey); PlayerPrefs.Save(); Time.timeScale=time;
        }
    }
}
