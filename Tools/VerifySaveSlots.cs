using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class VerifySaveSlots
{
    const string Root=".codex/save-slots/";
    static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
    static void RequireIsolatedPlay()=>Check(Application.isPlaying&&GameSaveManager.EditorTestSessionActive,
        "Use VerifyTwinBladeMovingSession.Prepare before running isolated save checks");
    static async Task Until(Func<bool> done)
    {
        var limit=DateTime.UtcNow.AddSeconds(65);
        while(!done()){if(!Application.isPlaying||DateTime.UtcNow>limit)throw new Exception("Scene transition did not complete");await Task.Delay(30);}
    }
    static Button Slot(int slot)=>UnityEngine.Object.FindFirstObjectByType<SaveSlotMenu>().transform.Find("Slot panel/Slot "+slot).GetComponent<Button>();
    static async Task Shot(string name){await Task.Delay(200);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Root+name+".png"));await Task.Delay(150);}
    public static async Task<object> Run()
    {
        RequireIsolatedPlay();
        Directory.CreateDirectory(Root);
        string directory=Root+Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        string file=Path.GetFullPath(directory+"/save.json");
        typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,file);
        var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
        var protect=player.GetComponent<SecondIslandStartProtection>();if(protect)typeof(SecondIslandStartProtection).GetProperty("WaitingForMovement").SetValue(protect,false);
        player.ExternalControlLock=player.ExternalMovementLock=false;
        var hp=player.GetComponent<PlayerHealth>();hp.currentHealth=hp.maxHealth;
        Check(GameSaveManager.SaveCurrentGame(1),"Slot 1 write failed");
        string original=File.ReadAllText(file);
        var legacy=JObject.Parse(original);legacy["version"]=1;File.WriteAllText(file,legacy.ToString());
        Check(GameSaveManager.GetSlot(1).readable,"Legacy slot not readable");
        hp.currentHealth=hp.maxHealth*.6f;
        Check(GameSaveManager.SaveCurrentGame(2),"Slot 2 write failed");
        string second=Path.Combine(Path.GetDirectoryName(file),"save-slot-2.json");
        string third=Path.Combine(Path.GetDirectoryName(file),"save-slot-3.json");
        string secondContents=File.ReadAllText(second);
        Check(File.ReadAllText(file)==legacy.ToString(),"Writing slot 2 overwrote slot 1");
        File.WriteAllText(third,"broken");
        Check(GameSaveManager.GetSlot(3).exists&&!GameSaveManager.GetSlot(3).readable,"Corrupt slot not identified");
        File.WriteAllText(third+".bak",original);
        Check(GameSaveManager.GetSlot(3).readable,"Backup recovery failed");
        var pause=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();
        pause.Open();
        var manual=pause.homePanel.transform.Find("Save Game").GetComponentInChildren<Button>();
        Check(manual.interactable,"Manual save disabled");
        manual.onClick.Invoke();
        Check(SaveSlotMenu.IsOpen&&Time.timeScale==0,"Save picker did not pause");
        await Shot("save-picker");
        Slot(3).onClick.Invoke();
        Check(SaveSlotMenu.IsOpen&&GameSaveManager.ActiveSlot==2,"Overwrite ran without confirmation");
        Slot(3).onClick.Invoke();
        Check(!SaveSlotMenu.IsOpen&&GameSaveManager.ActiveSlot==3,"Manual save did not select slot");
        Check(File.ReadAllText(second)==secondContents,"Saving slot 3 changed slot 2");
        await Shot("pause-menu");
        var main=pause.homePanel.transform.Find("Quit to Main Menu").GetComponentInChildren<Button>();
        main.onClick.Invoke();
        Check(PauseSettingsMenu.IsOpen&&!SceneLoadingScreen.IsLoading,"First quit click skipped confirmation");
        main.onClick.Invoke();
        await Until(()=>UnityEngine.Object.FindFirstObjectByType<MainMenuController>()&&!SceneLoadingScreen.IsLoading);
        Check(Time.timeScale==1&&!PauseSettingsMenu.IsOpen,"Main menu retained pause state");
        var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        menu.ContinueGame();
        Check(SaveSlotMenu.IsOpen,"Load picker did not open");
        await Shot("load-picker");
        Slot(2).onClick.Invoke();
        await Until(()=>UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>()&&!SceneLoadingScreen.IsLoading&&!GameSaveManager.IsContinuePending);
        player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        hp=player.GetComponent<PlayerHealth>();
        Check(GameSaveManager.ActiveSlot==2&&Mathf.Abs(hp.currentHealth-hp.maxHealth*.6f)<.1f,"Selected slot did not restore its own health");
        pause=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();pause.Open();
        pause.quitButton.onClick.Invoke();
        Check(PauseSettingsMenu.IsOpen&&Application.isPlaying,"Desktop exit skipped confirmation");
        pause.Resume();
        var result=new{passed=true,legacySlot=true,independentSlots=true,backupRecovery=true,manualOverwriteConfirmation=true,returnToMainMenu=true,selectedSlotLoad=true,desktopConfirmation=true,file};
        File.WriteAllText(Root+"result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    public static object Desktop()
    {
        RequireIsolatedPlay();
        var pause=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();
        pause.Open();pause.quitButton.onClick.Invoke();pause.quitButton.onClick.Invoke();return "Desktop quit requested through the actual button";
    }
    public static async Task<object> SaveFailure()
    {
        RequireIsolatedPlay();
        string file=(string)typeof(GameSaveManager).GetProperty("SavePath",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        string before=File.ReadAllText(file);
        int active=GameSaveManager.ActiveSlot;
        var pause=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();pause.Open();
        Check(GameSaveManager.HasSavableGame,"Failure check requires a savable player");
        using(var locked=new FileStream(file+".tmp",FileMode.Create,FileAccess.ReadWrite,FileShare.None))
        {
            var main=pause.homePanel.transform.Find("Quit to Main Menu").GetComponentInChildren<Button>();
            main.onClick.Invoke();main.onClick.Invoke();
            Check(PauseSettingsMenu.IsOpen&&!SceneLoadingScreen.IsLoading&&pause.footerHint.text.Contains("SAVE FAILED"),"Exit continued after write failure");
            pause.homePanel.transform.Find("Save Game").GetComponentInChildren<Button>().onClick.Invoke();
            Slot(active).onClick.Invoke();Slot(active).onClick.Invoke();
            Check(SaveSlotMenu.IsOpen&&GameSaveManager.ActiveSlot==active,"Failed manual save closed picker or changed active slot");
            await Task.Delay(80);
            UnityEngine.Object.FindFirstObjectByType<SaveSlotMenu>().Close();
            Check(Time.timeScale==0&&PauseSettingsMenu.IsOpen,"Closing failed save picker resumed gameplay");
        }
        Check(File.ReadAllText(file)==before,"Failed write damaged existing save");
        pause.Resume();
        var result=new {passed=true,failedExitCancelled=true,failedSaveRetainsPicker=true,existingSavePreserved=true};
        File.WriteAllText(Root+"failure-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    public static async Task<object> NewGame()
    {
        RequireIsolatedPlay();
        var pause=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();pause.Open();
        var main=pause.homePanel.transform.Find("Quit to Main Menu").GetComponentInChildren<Button>();
        main.onClick.Invoke();main.onClick.Invoke();
        await Until(()=>UnityEngine.Object.FindFirstObjectByType<MainMenuController>()&&!SceneLoadingScreen.IsLoading);
        string file=(string)typeof(GameSaveManager).GetProperty("BaseSavePath",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        string original=File.ReadAllText(file);
        string second=Path.Combine(Path.GetDirectoryName(file),"save-slot-2.json");
        string secondContents=File.ReadAllText(second);
        var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        menu.NewGame();Slot(1).onClick.Invoke();
        Check(SaveSlotMenu.IsOpen&&File.ReadAllText(file)==original,"New game overwrote before confirmation");
        UnityEngine.Object.FindFirstObjectByType<SaveSlotMenu>().Close();
        Check(File.ReadAllText(file)==original&&Time.timeScale==1,"Cancelling new game changed progress");
        menu.NewGame();Slot(3).onClick.Invoke();Slot(3).onClick.Invoke();
        await Until(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Story1"&&!SceneLoadingScreen.IsLoading);
        Check(GameSaveManager.ActiveSlot==3&&!GameSaveManager.GetSlot(3).exists&&!GameSaveManager.IsContinuePending,"New game retained old slot data");
        Check(Directory.GetFiles(Path.GetDirectoryName(file),"save-slot-3.json.previous-*").Length==1,"Replaced save was not archived");
        Check(File.ReadAllText(file)==original&&File.ReadAllText(second)==secondContents,"New game changed another slot");
        Check(Time.timeScale==1,"Prologue retained paused state");
        Check(GameSaveManager.QueueContinue(2),"Could not reload slot 2 after new game");
        SceneLoadingScreen.Load(GameSaveManager.ContinueScenePath);
        await Until(()=>UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>()&&!SceneLoadingScreen.IsLoading&&!GameSaveManager.IsContinuePending);
        var result=new {passed=true,cancelPreservesSave=true,newGamePrologue=true,oldSlotArchived=true,otherSlotsUnchanged=true};
        File.WriteAllText(Root+"new-game-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
