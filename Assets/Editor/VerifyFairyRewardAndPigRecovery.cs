using System;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Play-mode regression for the fairy reward and sustained boss combat.</summary>
public static class VerifyFairyRewardAndPigRecovery
{
    public static string Status="idle",Result="";
    static Keyboard keyboard;
    static Key[] keys=Array.Empty<Key>();
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(keys));
    }
    static async Task Wait(float seconds)
    {
        float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(20);
        while(Time.time<end){if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)throw new Exception("Play mode stopped or stalled.");await Task.Delay(20);}
    }
    static async Task Press(Key key)
    { keys=Array.Empty<Key>();await Wait(.15f);keys=new[]{key};await Wait(.12f);keys=Array.Empty<Key>();await Wait(.25f); }
    static void Place(ThirdPersonPlayerController player,Vector3 point)
    {
        var ground=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
        if(!ground.Raycast(new Ray(point+Vector3.up*100,Vector3.down),out var hit,200))throw new Exception("No island floor near "+point);
        var body=player.GetComponent<CharacterController>();
        float sole=(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
        player.RestoreSavedPose(hit.point+Vector3.up*(.1f-sole),Quaternion.identity);
    }
    static Color EffectiveSkinColor(SkinnedMeshRenderer skin)
    {
        var block=new MaterialPropertyBlock();skin.GetPropertyBlock(block,0);
        var material=skin.sharedMaterial;string key=material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        return block.HasColor(key) ? block.GetColor(key) : material.GetColor(key);
    }
    static Texture EffectiveSkinTexture(SkinnedMeshRenderer skin)
    {
        var block=new MaterialPropertyBlock();skin.GetPropertyBlock(block,0);
        return block.HasTexture("_BaseMap") ? block.GetTexture("_BaseMap") : skin.sharedMaterial.GetTexture("_BaseMap");
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,System.IO.Path.GetFullPath("Temp/fairy-pig-regression.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.4f);
            var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var quest=UnityEngine.Object.FindFirstObjectByType<CandyHuntQuest>();keyboard=Keyboard.current;
            if(!p || !quest || keyboard==null)throw new Exception("Player, fairy quest or keyboard missing.");
            var skin=p.GetComponentInChildren<SkinnedMeshRenderer>();
            if(!skin || !skin.sharedMaterial)throw new Exception("Sahur's original material is missing.");
            var originalColor=EffectiveSkinColor(skin);var originalTexture=EffectiveSkinTexture(skin);
            InputSystem.onBeforeUpdate+=Inject;
            quest.RestoreProgress(true,1022);
            if(quest.Completed || quest.RewardClaimed)throw new Exception("Reward unlocked before collecting the final candy.");
            Place(p,quest.candies[0].position);await Wait(.5f);
            if(!quest.Completed || quest.RewardClaimed || quest.NextTarget!=quest.fairy)throw new Exception("The final candy did not direct the player back to the fairy without awarding early.");
            Place(p,quest.fairy.position+new Vector3(0,0,-3));await Wait(.6f);await Press(Key.F);
            if(!CandyHuntQuest.StyleSelectorOpen || quest.RewardClaimed)throw new Exception("Fairy did not open the reward selection.");
            await Press(Key.Digit3);
            if(!quest.RewardClaimed || quest.SelectedStyle!=2 || CandyHuntQuest.StyleSelectorOpen)throw new Exception("Fairy did not grant the selected style.");
            var saved=p.GetComponent<SecondIslandSaveState>().Capture();
            if(!saved.candyHuntRewardClaimed || saved.candyHuntSelectedStyle!=2 || saved.candyHuntMask!=1023)throw new Exception("Fairy reward did not persist in progress.");
            if(EffectiveSkinColor(skin)!=originalColor || EffectiveSkinTexture(skin)!=originalTexture)throw new Exception("Selecting a Skyboard style changed Sahur's original appearance.");
            if(!GameSaveManager.SaveCurrentGame() || !GameSaveManager.QueueContinue())throw new Exception("Could not save the isolated reward.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Second Island/Second Island.unity");
            float deadline=Time.realtimeSinceStartup+15;
            while(GameSaveManager.IsContinuePending && Time.realtimeSinceStartup<deadline)await Task.Delay(30);
            await Wait(.6f);
            p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();quest=UnityEngine.Object.FindFirstObjectByType<CandyHuntQuest>();
            if(GameSaveManager.IsContinuePending || !quest.RewardClaimed || quest.SelectedStyle!=2)throw new Exception("Fairy reward did not survive Continue.");
            skin=p.GetComponentInChildren<SkinnedMeshRenderer>();
            if(EffectiveSkinColor(skin)!=originalColor || EffectiveSkinTexture(skin)!=originalTexture)throw new Exception("Loading a chosen style changed Sahur's original appearance.");
            var board=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            if(!board.CollectSpawnTestKit())throw new Exception("Could not equip the test Skyboard.");
            for(int i=0;i<5;i++)
            {
                quest.RestoreProgress(true,1023,true,i);board.RestoreProgress(true,0);
                if(!board.TryStartFlight())throw new Exception("Could not start Skyboard style "+i);
                var ridden=GameObject.Find("Sahur skyboard");
                var boardMaterial=Array.Find(ridden.GetComponentsInChildren<Renderer>(),r=>r.sharedMaterial && r.sharedMaterial.name=="Candy-colour Skyboard");
                var texture=boardMaterial ? boardMaterial.sharedMaterial.GetTexture("_BaseMap") : null;
                if(!texture || texture.name!=CandySkyboardStyles.Names[i]+" pattern")throw new Exception("Pattern did not match Skyboard style "+i);
                if(EffectiveSkinColor(skin)!=originalColor || EffectiveSkinTexture(skin)!=originalTexture)throw new Exception("Skyboard style "+i+" changed Sahur's original appearance.");
                typeof(SkyboardReward).GetMethod("EndFlight",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(board,null);
                await Wait(.15f);
            }
            Result="fairy return, choice, save, all five board styles and unchanged Sahur passed; ";

            var boss=UnityEngine.Object.FindFirstObjectByType<PigRoninBoss>();var hp=boss.GetComponent<Health>();
            hp.maxHealth=hp.currentHealth=5000; p.GetComponent<PlayerHealth>().GrantProtection(20);
            Place(p,boss.transform.position+boss.transform.forward*6);await Wait(.8f);
            if(!boss.FightActive)throw new Exception("Boss did not engage.");
            var response=boss.GetComponent<CombatImpactResponse>();
            if(response.stopMultiplier!=0)throw new Exception("The armored boss's procedural rig can still be frozen by every hit.");
            int attacks=boss.AttacksStarted;bool sawStagger=false,sawResume=false;
            for(int i=0;i<38;i++)
            {
                CombatHitFeedback.Apply(p.gameObject,hp,25,boss.transform.position+Vector3.up,CombatHitKind.ComboOne);
                if(boss.CurrentState==PigRoninBoss.State.Staggered)sawStagger=true;
                if(sawStagger && boss.CurrentState!=PigRoninBoss.State.Staggered)sawResume=true;
                await Wait(.15f);
            }
            if(!sawStagger || !sawResume || boss.AttacksStarted<=attacks)throw new Exception("Boss failed to recover and attack during repeated hits: "+boss.CurrentState+", attacks="+boss.AttacksStarted);
            var volume=boss.GetComponentInChildren<UnityEngine.Rendering.Volume>(true);
            if(!volume || !volume.enabled || !volume.profile.TryGet<UnityEngine.Rendering.Universal.MotionBlur>(out var blur) || blur.intensity.value!=0)throw new Exception("Boss clarity override missing.");
            Result+="sustained hits, stagger recovery, attacks and clarity passed";Status="passed";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        finally{keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputState.Change(keyboard,new KeyboardState());}
        Debug.Log("Fairy and pig regression: "+Status+" "+Result);
    }
}
