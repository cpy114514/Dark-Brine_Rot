using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class VerifySecondIslandAwakening
{
    const string Key = "NealProject.VerifySecondIslandAwakening";
    const string Folder = "Temp/SecondIslandAwakeningReview";
    static Keyboard keyboard;
    static Key[] keys = Array.Empty<Key>();
    public static string Status = "idle";
    public static object Summary;
    static VerifySecondIslandAwakening() => EditorApplication.playModeStateChanged += OnMode;
    public static void Arm() { SessionState.SetBool(Key,true); Status="armed"; }
    static void OnMode(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false)) return;
        SessionState.SetBool(Key,false); _ = Run();
    }
    static void Inject()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard,new KeyboardState(keys));
    }
    static object GroundClearance(ThirdPersonPlayerController player, float phase)
    {
        var distances=new List<float>(); var mesh=new Mesh();
        try
        {
            foreach(var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!skin.enabled)continue;
                skin.BakeMesh(mesh,false); var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i+=Mathf.Max(1,vertices.Length/2000))
                {
                    var point=skin.transform.TransformPoint(vertices[i]);
                    float nearest=float.PositiveInfinity, surface=0;
                    foreach(var hit in Physics.RaycastAll(point+Vector3.up*15,Vector3.down,35,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(hit.collider.transform.IsChildOf(player.transform) || hit.normal.y<.45f || hit.distance>=nearest)continue;
                        nearest=hit.distance;surface=hit.point.y;
                    }
                    if(!float.IsPositiveInfinity(nearest))distances.Add(point.y-surface);
                }
            }
            distances.Sort();
            return new{phase,samples=distances.Count,min=distances.Count>0?distances[0]:999,
                low1=distances.Count>0?distances[Mathf.Min(distances.Count-1,distances.Count/100)]:999,
                penetrations=distances.FindAll(d=>d<-.1f).Count};
        }
        finally{UnityEngine.Object.DestroyImmediate(mesh);}
    }
    static async Task Run()
    {
        Directory.CreateDirectory(Folder);
        try
        {
            Status="playing"; Summary=null;
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var effect=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
            if (!player || !effect) throw new Exception("Awakening not present on scene start");
            keyboard=Keyboard.current;
            if (keyboard==null) throw new Exception("No keyboard available");
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
            InputSystem.onBeforeUpdate+=Inject;
            var start=player.transform.position; float initial=Time.time;
            bool blockedEveryFrame=true, pauseVerified=false, dialoguePauseVerified=false, menusBlocked=false, sawGetUp=false; var shots=new HashSet<string>(); var samples=new List<object>();
            float minHead=float.PositiveInfinity,maxHead=float.NegativeInfinity;var groundReview=new List<object>();
            Vector3 lastHips=Vector3.zero,lastCamera=Vector3.zero;bool sampled=false;
            float maxHipsStep=0,maxCameraStep=0;
            int lastLine=-1;float lineStarted=0;var dialogueLines=new List<int>();
            float deadline=Time.realtimeSinceStartup+35f;
            while (effect && effect.Playing)
            {
                if(Time.realtimeSinceStartup>deadline)throw new Exception("Opening or dialogue stalled");
                if (!pauseVerified && Time.time-initial>.8f)
                {
                    var menu=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();
                    if (!menu) throw new Exception("Pause menu unavailable");
                    float beforePause=effect.Opening;
                    menu.Open();
                    try
                    {
                        await Task.Delay(300);
                        if (!PauseSettingsMenu.IsOpen || effect.GetComponent<Canvas>().enabled ||
                            Mathf.Abs(effect.Opening-beforePause)>.001f)
                            throw new Exception($"Pause must show its menu and freeze the eyelids: menu={PauseSettingsMenu.IsOpen}, overlay={effect.GetComponent<Canvas>().enabled}, before={beforePause}, after={effect.Opening}, timeScale={Time.timeScale}");
                        ScreenCapture.CaptureScreenshot(Folder+"/pause.png");
                        pauseVerified=true;
                    }
                    finally { menu.Resume(); }
                }
                blockedEveryFrame &= player.ExternalControlLock;
                float open=effect.Opening, opacity=effect.Opacity;
                if(effect.Dialogue && effect.Dialogue.Active)
                {
                    int current=effect.Dialogue.CurrentLine;
                    if(current!=lastLine)
                    {
                        lastLine=current;lineStarted=Time.time;dialogueLines.Add(current);
                        var panel=GameObject.Find("Fairy conversation");
                        if(!panel)throw new Exception("Dialogue panel is missing.");
                        foreach(var text in panel.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                            if(System.Text.RegularExpressions.Regex.IsMatch(text.text ?? "", @"[\u3400-\u9fff]"))
                                throw new Exception("Non-English dialogue text: "+text.text);
                        var body=panel.transform.Find("English dialogue")?.GetComponent<UnityEngine.UI.Text>();
                        if(!body || body.text!=SecondIslandDialogue.Lines[current].english)
                            throw new Exception("English dialogue is missing or incorrect on line "+current);
                        Canvas.ForceUpdateCanvases();
                        if(body.preferredHeight>body.rectTransform.rect.height+1f)
                            throw new Exception("English dialogue is clipped on line "+current);
                        if(current==0 || current==1 || current==5 || current==7)ScreenCapture.CaptureScreenshot(Folder+"/dialogue-"+current+".png");
                    }
                    float age=Time.time-lineStarted;
                    if(current==0 && age>.18f && !dialoguePauseVerified)
                    {
                        var map=player.GetComponent<Mavis.IslandMapUI>();var loadout=player.GetComponent<Mavis.SahurLoadoutUI>();
                        bool mapOpened=map && map.SetOpen(true);
                        if(loadout)loadout.SetOpen(true);
                        menusBlocked=!mapOpened && !Mavis.SahurLoadoutUI.IsOpen;
                        var menu=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();menu.Open();
                        try
                        {
                            await Task.Delay(300);
                            dialoguePauseVerified=PauseSettingsMenu.IsOpen && !effect.GetComponent<Canvas>().enabled &&
                                effect.Dialogue.Active && effect.Dialogue.CurrentLine==current;
                        }
                        finally {menu.Resume();}
                    }
                    keys=age>=.50f && age<.60f ? new[]{GameInputSettings.Get(GameInputSettings.Action.Forward),UnityEngine.InputSystem.Key.Enter} :
                        new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
                }
                var hips=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.Hips);
                if(hips && Camera.main)
                {
                    if(sampled){maxHipsStep=Mathf.Max(maxHipsStep,Vector3.Distance(lastHips,hips.position));maxCameraStep=Mathf.Max(maxCameraStep,Vector3.Distance(lastCamera,Camera.main.transform.position));}
                    lastHips=hips.position;lastCamera=Camera.main.transform.position;sampled=true;
                }
                if (effect.GettingUp)
                {
                    sawGetUp=true;
                    var head=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head);
                    if(head){minHead=Mathf.Min(minHead,head.position.y);maxHead=Mathf.Max(maxHead,head.position.y);}
                    string pose = effect.GetUpPhase < .15f ? "06-lying" : effect.GetUpPhase>.4f && effect.GetUpPhase<.55f ? "07-rising" :
                        effect.GetUpPhase>.82f && effect.GetUpPhase<.95f ? "08-standing" : null;
                    if(pose!=null && shots.Add(pose))
                    {
                        ScreenCapture.CaptureScreenshot(Folder+"/"+pose+".png");
                        groundReview.Add(GroundClearance(player,effect.GetUpPhase));
                    }
                }
                samples.Add(new{time=Time.time-initial,open,opacity,gettingUp=effect.GettingUp,phase=effect.GetUpPhase,blocked=player.ExternalControlLock,
                    hips=hips?new[]{hips.position.x,hips.position.y,hips.position.z}:null,
                    camera=Camera.main?new[]{Camera.main.transform.position.x,Camera.main.transform.position.y,Camera.main.transform.position.z}:null});
                string shot = open < .01f ? "01-closed" : open>.22f && open<.30f ? "02-glimpse" :
                    open>.55f && open<.68f ? "03-opening" : open>1f && opacity>.98f ? "04-wide" : null;
                if (shot!=null && shots.Add(shot)) ScreenCapture.CaptureScreenshot(Folder+"/"+shot+".png");
                await Task.Delay(15);
            }
            var end=player.transform.position;
            float heldMovement=Vector3.ProjectOnPlane(end-start,Vector3.up).magnitude;
            bool unlocked=!player.ExternalControlLock;
            await Task.Delay(900);
            float afterMovement=Vector3.ProjectOnPlane(player.transform.position-end,Vector3.up).magnitude;
            ScreenCapture.CaptureScreenshot(Folder+"/05-open.png");
            if (!blockedEveryFrame || !unlocked || !pauseVerified || !dialoguePauseVerified || !menusBlocked || !sawGetUp || maxHead-minHead<1f || heldMovement>.2f || afterMovement<.3f || shots.Count<7 ||
                dialogueLines.Count!=SecondIslandDialogue.Lines.Length)
                throw new Exception($"Input or visuals failed: blocked={blockedEveryFrame}, unlocked={unlocked}, getUp={sawGetUp}, headRise={maxHead-minHead}, held={heldMovement}, after={afterMovement}, shots={shots.Count}");
            Summary=new{blockedEveryFrame,unlocked,pauseVerified,dialoguePauseVerified,menusBlocked,sawGetUp,headRise=maxHead-minHead,heldMovement,afterMovement,maxHipsStep,maxCameraStep,groundReview,dialogueLines,shots=shots.Count,duration=Time.time-initial};
            File.WriteAllText(Folder+"/samples.json",Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));
            Status="completed";
        }
        catch(Exception e) { Status="failed: "+e; }
        finally
        {
            keys=Array.Empty<Key>(); InputSystem.onBeforeUpdate-=Inject;
            if (keyboard!=null) InputState.Change(keyboard,new KeyboardState());
            keyboard=null;
            File.WriteAllText(Folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{Status,Summary},Newtonsoft.Json.Formatting.Indented));
        }
    }
}
