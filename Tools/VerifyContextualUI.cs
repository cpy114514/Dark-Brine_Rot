using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Mavis;
public static class VerifyContextualUI
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Field<T>(object obj,string name) => (T)obj.GetType().GetField(name,Private).GetValue(obj);
    static void Call(object obj,string method) => obj.GetType().GetMethod(method,Private).Invoke(obj,null);
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play mode.");
        var ui = (SahurLoadoutUI)typeof(SahurLoadoutUI).GetField("active",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        if (!ui) throw new Exception("No live loadout owner.");
        var movement = ui.GetComponent<ThirdPersonPlayerController>();
        var vitals = UnityEngine.Object.FindFirstObjectByType<PlayerVitalsHUD>();
        var map = ui.GetComponent<IslandMapUI>();
        var hint = Field<Text>(ui,"loadoutHint");
        var pickup = Field<Text>(ui,"pickupNotice");
        var hotbar = Field<CanvasGroup>(ui,"hud");
        float oldTime = Time.timeScale;
        float oldHint = Field<float>(ui,"hintRemaining");
        bool oldMovement = movement.enabled;
        var characterController = movement.GetComponent<CharacterController>();
        bool oldController = characterController != null && characterController.enabled;
        GameObject menuObject = null, boss = null;
        try
        {
            if (characterController != null) characterController.enabled = true;
            movement.enabled = true;
            Time.timeScale = 0f;
            boss = new GameObject("Contextual UI verification enemy");
            boss.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 3;
            boss.AddComponent<NailongHealth>();
            var bar = boss.GetComponent<NailongHealthBar>();
            Call(ui,"RefreshVisibility");
            Check(!hotbar.gameObject.activeSelf,"Empty skills remain visible");
            typeof(SahurLoadoutUI).GetField("hintRemaining",Private).SetValue(ui,0f);
            Call(ui,"RefreshVisibility");
            Check(!hint.gameObject.activeSelf && !pickup.gameObject.activeSelf,"Expired hint or empty pickup remains visible");
            if(vitals) { Call(vitals,"Update"); Check(Field<CanvasGroup>(vitals,"canvasGroup").alpha==1f,"Gameplay vitals hidden"); }
            ui.SetOpen(true); Call(ui,"RefreshVisibility"); bar.Refresh(0);
            Check(ui.PanelVisible && !hotbar.gameObject.activeSelf && !hint.gameObject.activeSelf && !bar.IsVisible,"Loadout has underlying HUD");
            if(vitals) { Call(vitals,"Update"); Check(Field<CanvasGroup>(vitals,"canvasGroup").alpha==0f,"Loadout has vitals behind it"); }
            ui.SetOpen(false); await Task.Delay(80);
            Check(map.SetOpen(true),"Map failed to open"); Call(ui,"RefreshVisibility"); bar.Refresh(0);
            if(vitals) { Call(vitals,"Update"); Check(Field<CanvasGroup>(vitals,"canvasGroup").alpha==0f,"Map has vitals behind it"); }
            Check(!bar.IsVisible && !pickup.gameObject.activeSelf,"Map has enemy bar or pickup behind it");
            map.SetOpen(false); await Task.Delay(80);
            menuObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab"));
            var menu = menuObject.GetComponent<PauseSettingsMenu>(); menu.Open();
            Call(ui,"RefreshVisibility"); bar.Refresh(0);
            if(vitals) { Call(vitals,"Update"); Check(Field<CanvasGroup>(vitals,"canvasGroup").alpha==0f,"Pause has vitals behind it"); }
            Check(!bar.IsVisible && !hint.gameObject.activeSelf,"Pause has enemy bar or hint behind it");
            menu.Resume(); await Task.Delay(80);
            boss.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 3;
            Call(ui,"RefreshVisibility"); bar.Refresh(0);
            if(vitals) { Call(vitals,"Update"); Check(Field<CanvasGroup>(vitals,"canvasGroup").alpha==1f,"Gameplay vitals not restored"); }
            Check(bar.IsVisible,"Enemy bar not restored");
            movement.enabled = false; Call(ui,"RefreshVisibility");
            Check(!hotbar.gameObject.activeSelf && !hint.gameObject.activeSelf && !pickup.gameObject.activeSelf,"Inactive player still shows HUD");
            return new { success=true, emptySkillsHidden=true, expiredHintsHidden=true, pause=true, map=true, loadout=true, gameplayRestored=true, inactivePlayerHidden=true };
        }
        finally
        {
            if(menuObject) { menuObject.GetComponent<PauseSettingsMenu>().Resume(); UnityEngine.Object.Destroy(menuObject); }
            ui.SetOpen(false); map.SetOpen(false);
            movement.enabled = oldMovement;
            if (characterController != null) characterController.enabled = oldController;
            typeof(SahurLoadoutUI).GetField("hintRemaining",Private).SetValue(ui,oldHint);
            Time.timeScale=oldTime; Call(ui,"RefreshVisibility");
            if(boss) UnityEngine.Object.Destroy(boss);
        }
    }
}
