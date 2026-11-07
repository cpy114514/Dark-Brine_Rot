using System;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Uses the real walking and interaction controls to play the melody garden.</summary>
public static class VerifyCandyMelodyGarden
{
    public static string Status="idle",Result="";
    static Keyboard keyboard;
    static Keyboard previousKeyboard;
    static Key[] keys=Array.Empty<Key>();
    static ThirdPersonPlayerController player;
    static CharacterController body;
    static Transform destination;
    static Transform gardenCentre;
    static bool walking;
    static string interactTrace="";
    static int injectedWithF, fPressedAfter;
    static void TraceInteract()
    {
        if(keyboard!=null && keyboard.fKey.isPressed)fPressedAfter++;
        if(keyboard==null || !keyboard.fKey.wasPressedThisFrame)return;
        var g=UnityEngine.Object.FindFirstObjectByType<CandyMelodyGarden>();
        interactTrace=" F="+GameControls.Down(GameControls.Command.Interact)+" modal="+GameUITheme.ModalOpen+" cursor="+Cursor.lockState+" near="+(g&&g.ReservesInteraction)+" lock="+(player&&player.ExternalControlLock)+" movement="+(player&&player.ExternalMovementLock);
    }
    static readonly FieldInfo Yaw=typeof(ThirdPersonPlayerController).GetField("yaw",BindingFlags.Instance|BindingFlags.NonPublic);
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        if(Array.IndexOf(keys,Key.F)>=0)injectedWithF++;
        keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        if(walking && destination && player)
        {
            var delta=destination.position-player.transform.position;delta.y=0;
            if(delta.sqrMagnitude>3.15f*3.15f)
            {
                Yaw.SetValue(player,Quaternion.LookRotation(delta).eulerAngles.y);
                keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
            }
            else keys=Array.Empty<Key>();
        }
        InputState.Change(keyboard,new KeyboardState(keys));
    }
    static async Task Wait(float seconds)
    {
        float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(25);
        while(Time.time<end){if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)throw new Exception("Play mode stopped or stalled.");await Task.Delay(20);}
    }
    static async Task Until(Func<bool> condition,float seconds,string label)
    {
        float end=Time.time+seconds;
        while(!condition()) {if(!EditorApplication.isPlaying || Time.time>end)throw new Exception("Timed out: "+label);await Task.Delay(25);}
    }
    static async Task Press(Key key)
    {walking=false;keys=Array.Empty<Key>();await Wait(.15f);keys=new[]{key};await Wait(.25f);keys=Array.Empty<Key>();await Wait(.22f);}
    static async Task WalkTo(Transform target)
    {
        if(Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-target.position,Vector3.up),Vector3.zero)<4f)
        {
            destination=gardenCentre;walking=true;
            await Until(()=>Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-target.position,Vector3.up),Vector3.zero)>4.5f,5,"leave note");
            walking=false;keys=Array.Empty<Key>();await Wait(.15f);
        }
        destination=target;walking=true;
        await Until(()=>Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-target.position,Vector3.up),Vector3.zero)<3.25f,9,"walk to "+target.name);
        walking=false;keys=Array.Empty<Key>();await Wait(.32f);
        float distance=Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-target.position,Vector3.up),Vector3.zero);
        if(distance>3.7f)throw new Exception("Walk stopped outside interaction range: "+distance);
    }
    static void Place(Vector3 ground)
    {
        float sole=(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
        player.RestoreSavedPose(ground+Vector3.up*(.1f-sole),Quaternion.identity);
    }
    public static async void Run()
    {
        Status="running";Result="";interactTrace="";injectedWithF=fPressedAfter=0;
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,System.IO.Path.GetFullPath("Temp/candy-melody-test.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.4f);
            player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();body=player.GetComponent<CharacterController>();
            previousKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();
            var garden=UnityEngine.Object.FindFirstObjectByType<CandyMelodyGarden>();var board=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            gardenCentre=garden.transform;
            if(!player || !garden || !board || keyboard==null)throw new Exception("Garden, player, board or keyboard missing.");
            if(garden.RewardClaimed || garden.CurrentStage!=CandyMelodyGarden.Stage.Idle)throw new Exception("Garden did not start unclaimed and idle.");
            foreach(var renderer in garden.GetComponentsInChildren<Renderer>(true))
                if(!renderer.GetComponent<Collider>() && !renderer.GetComponentInParent<Collider>(true))throw new Exception("Visible garden object lacks collision: "+renderer.name);
            if(!board.CollectSpawnTestKit())throw new Exception("Could not set up board interaction test.");
            var island=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            var q=garden.StartSign.position+new Vector3(0,0,-3);
            if(!island.Raycast(new Ray(q+Vector3.up*50,Vector3.down),out var hit,100))throw new Exception("No ground by the garden sign.");
            Place(hit.point);InputSystem.onBeforeUpdate+=Inject;InputSystem.onAfterUpdate+=TraceInteract;await Wait(.5f);
            await Press(Key.F);
            if(board.Flying || garden.CurrentStage!=CandyMelodyGarden.Stage.Showing)throw new Exception("Sign did not start the melody or F launched the board. F input frames="+injectedWithF+" pressed="+fPressedAfter+interactTrace);
            await Until(()=>garden.CurrentStage==CandyMelodyGarden.Stage.Listening,8,"first sequence");
            int first=garden.SequenceAt(0),second=garden.SequenceAt(1),third=garden.SequenceAt(2);
            // The sign replays the same melody and clears partially entered notes.
            await Press(Key.F);if(garden.CurrentStage!=CandyMelodyGarden.Stage.Showing)throw new Exception("Sign did not replay the melody.");
            await Until(()=>garden.CurrentStage==CandyMelodyGarden.Stage.Listening,8,"replayed sequence");
            if(garden.SequenceAt(0)!=first || garden.SequenceAt(1)!=second || garden.SequenceAt(2)!=third)throw new Exception("Replay changed the notes.");
            int wrong=(first+1)%4;
            Status="checking mistake and retry";await WalkTo(garden.NoteTransform(wrong));
            if(garden.Mistakes!=1 || garden.Round!=0 || garden.CurrentStage!=CandyMelodyGarden.Stage.Showing)throw new Exception("Wrong note did not retry the same round.");
            await Until(()=>garden.CurrentStage==CandyMelodyGarden.Stage.Listening,8,"retry after wrong note");
            if(garden.SequenceAt(0)!=first)throw new Exception("A mistake changed the sequence.");
            for(int round=0;round<3;round++)
            {
                if(garden.Round!=round || garden.CurrentStage!=CandyMelodyGarden.Stage.Listening)throw new Exception("Unexpected round: "+garden.Round+" "+garden.CurrentStage);
                int length=garden.SequenceLength;
                for(int j=0;j<length;j++)
                {
                    Status="playing round "+(round+1)+" note "+(j+1)+"/"+length;
                    int before=garden.InputCount;int note=garden.SequenceAt(j);
                    await WalkTo(garden.NoteTransform(note));
                    if(j<length-1 && (garden.CurrentStage!=CandyMelodyGarden.Stage.Listening || garden.InputCount!=before+1))
                        throw new Exception("Correct note was not accepted in round "+round+", note "+j+" at "+player.transform.position);
                }
                if(round<2)await Until(()=>garden.CurrentStage==CandyMelodyGarden.Stage.Listening && garden.Round==round+1,10,"next melody round");
            }
            var inventory=player.GetComponent<EquipmentInventory>();var scroll=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
            if(garden.CurrentStage!=CandyMelodyGarden.Stage.Complete || !garden.RewardClaimed || inventory.Count(scroll)!=11)throw new Exception("Win or first-time reward failed.");
            if(!player.GetComponent<SecondIslandSaveState>().Capture().melodyGardenReward || !GameSaveManager.SaveCurrentGame())throw new Exception("Melody garden was not saved.");
            var map=player.GetComponent<IslandMapUI>();if(!map.SetOpen(true))throw new Exception("Could not open map.");
            Canvas.ForceUpdateCanvases();var labels=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None);
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var targets=(System.Collections.Generic.List<Transform>)typeof(IslandMapUI).GetField("targets",flags).GetValue(map);
            var markers=(System.Collections.Generic.List<RectTransform>)typeof(IslandMapUI).GetField("markers",flags).GetValue(map);
            int markerIndex=targets.IndexOf(garden.MapAnchor);
            bool marker=markerIndex>=0 && markers[markerIndex].GetComponent<UnityEngine.UI.Text>().text=="✓",legend=Array.Exists(labels,t=>t.text.Contains("Melody garden"));
            if(!marker || !legend || !map.MapGraphic.rectTransform.rect.Contains(map.MapGraphic.WorldToMap(garden.MapAnchor.position)))throw new Exception("Melody map marker or legend is missing.");
            map.SetOpen(false);
            if(!GameSaveManager.QueueContinue())throw new Exception("Could not queue isolated save.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Second Island/Second Island.unity");
            float deadline=Time.realtimeSinceStartup+15;
            while(GameSaveManager.IsContinuePending && Time.realtimeSinceStartup<deadline)await Task.Delay(30);
            await Wait(.5f);
            garden=UnityEngine.Object.FindFirstObjectByType<CandyMelodyGarden>();player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();body=player.GetComponent<CharacterController>();
            if(GameSaveManager.IsContinuePending || !garden.RewardClaimed || player.GetComponent<EquipmentInventory>().Count(scroll)!=11)throw new Exception("Reward did not survive Continue.");
            // Replay the three rounds after loading; the one-time reward must not duplicate.
            var start=typeof(CandyMelodyGarden).GetMethod("StartGame",BindingFlags.Instance|BindingFlags.NonPublic);
            var select=typeof(CandyMelodyGarden).GetMethod("SelectNote",BindingFlags.Instance|BindingFlags.NonPublic);
            start.Invoke(garden,null);
            for(int round=0;round<3;round++)
            {
                int r=round;await Until(()=>garden.CurrentStage==CandyMelodyGarden.Stage.Listening && garden.Round==r,10,"replay after load");
                int length=garden.SequenceLength;
                for(int j=0;j<length;j++)select.Invoke(garden,new object[]{garden.SequenceAt(j)});
            }
            if(garden.CurrentStage!=CandyMelodyGarden.Stage.Complete || player.GetComponent<EquipmentInventory>().Count(scroll)!=11)throw new Exception("Replay duplicated the reward.");
            Status="passed";Result="Walked and played all three melody rounds with real input; mistake retry, replay, board shortcut, collision, map, first reward and Continue passed.";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        finally
        {
            walking=false;keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=Inject;InputSystem.onAfterUpdate-=TraceInteract;
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(previousKeyboard!=null && previousKeyboard.added)previousKeyboard.MakeCurrent();
            keyboard=null;previousKeyboard=null;
        }
        Debug.Log("Candy melody garden test: "+Status+" "+Result);
    }
}
