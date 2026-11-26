using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class ReviewCrushV219
{
    public static object Start(string label="first-15",int fps=15,bool realInputs=false)
    {
        if(!Application.isPlaying)throw new Exception("Enter the existing Editor's play mode first");
        if(UnityEngine.Object.FindFirstObjectByType<CrushObserverV219>()!=null)throw new Exception("Review already running");
        var go=new GameObject("Opening v214 actual camera recorder");UnityEngine.Object.DontDestroyOnLoad(go);
        var observer=go.AddComponent<CrushObserverV219>();observer.label=label;observer.fps=fps;observer.realInputs=realInputs;
        return new{started=true,label,fps};
    }
}
[DefaultExecutionOrder(31000)]
public sealed class CrushObserverV219:MonoBehaviour
{
    public string label;public int fps;public bool realInputs;
    bool mapTested;int realGateFrames;Story1WreckBattle.Lesson lastLesson=(Story1WreckBattle.Lesson)(-1);
    Story1WreckBattle.OpeningStage lastInputStage=(Story1WreckBattle.OpeningStage)(-1);
    bool lastInputWaiting;
    readonly List<string> inputChecks=new List<string>();
    readonly List<object> deckRows=new List<object>();
    OceanWorld water;Transform tailTip;string folder;int frames,waitingFrames;bool done;Story1WreckBattle battle;Story1SharkCollisionSequence sequence;
    readonly List<object> rows=new List<object>();RenderTexture target;Texture2D pixels;double started;
    void Start()
    {
        folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/encounter-redesign-20261017/"+label;
        if(Directory.Exists(folder))throw new Exception("Keep previous takes; choose a new label");
        Directory.CreateDirectory(folder);Time.captureFramerate=fps;
        target=new RenderTexture(640,360,24);target.Create();pixels=new Texture2D(640,360,TextureFormat.RGB24,false);
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();started=Time.realtimeSinceStartupAsDouble;
        water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();battle.RedesignNearPassDiagnostics=false;tailTip=sequence.swimmer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Tail_Tip");
    }
    void LateUpdate()
    {
        if(done||battle==null)return;
        var hero=sequence.sahur;var animator=hero.CharacterAnimator;var health=hero.GetComponent<Mavis.PlayerHealth>();
        var head=animator.GetBoneTransform(HumanBodyBones.Head);var chest=animator.GetBoneTransform(HumanBodyBones.Chest);
        if(!battle.RedesignActive||battle.RedesignStage==Story1WreckBattle.OpeningStage.Crush&&battle.RedesignClock<6)
        {
            Vector3 ls=Vector3.zero,rs=Vector3.zero;SahurSupportPose.MeasureSoles(animator,ref ls,ref rs);
            Vector3 l=animator.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint(ls),r=animator.GetBoneTransform(HumanBodyBones.RightFoot).TransformPoint(rs);
            var surfaces=sequence.ship.GetComponentsInChildren<Collider>();
            bool a=Story1SahurPassenger.TryDeckSurface(surfaces,l,out var lp),b=Story1SahurPassenger.TryDeckSurface(surfaces,r,out var rp);
            deckRows.Add(new{frame=frames,active=battle.RedesignActive,clock=battle.RedesignClock,leftGap=a?l.y-lp.y:999,rightGap=b?r.y-rp.y:999,sharkScale=V(sequence.swimmer.localScale)});
        }
        rows.Add(new{tailTipLocalEuler=tailTip!=null?V(tailTip.localEulerAngles):null,headWaterGap=head.position.y-(water!=null?water.SampleSurfaceHeight(head.position,Time.time):sequence.sahur.seaLevel),frame=frames,clock=battle.RedesignClock,stage=battle.RedesignStage.ToString(),beat=battle.RedesignBeat,lesson=battle.CurrentLesson.ToString(),battle.TutorialWaiting,battle.RedesignShot,battle.RedesignContacts,battle.RedesignContactGap,battle.RedesignSignedContactGap,battle.RedesignWaterPredictionCost,battle.RedesignWaterBodyPenetration,battle.RedesignLostStickFloating,battle.RedesignDeckClearance,battle.RedesignCameraWoodObstruction,battle.TutorialDamage,battle.TutorialDamageBudget,battle.TutorialHealthFloor,health.currentHealth,hero.enabled,hero.Swimming,hero.OnFloatingWreckDeck,support=hero.GetComponent<Story1WreckRider>()?.Support()?.name,hero=V(hero.transform.position),head=V(head.position),chest=V(chest.position),shark=V(sequence.swimmer.position),camera=V(sequence.storyCamera.transform.position),rate=Time.timeScale});
        if(battle.RedesignNearPassValid&&Mathf.Abs(battle.RedesignNearPassSampleClock-battle.RedesignClock)<.09f)
            nearPassRows.Add(new{frame=frames,clock=battle.RedesignNearPassSampleClock,gap=battle.RedesignNearPassSignedGap,regions=V(battle.RedesignNearPassRegionGaps),battle.RedesignNearPassClosestRegion,battle.RedesignNearPassEnvelopeMeasured,radii=V(battle.RedesignNearPassEnvelopeRadii),battle.RedesignNearPassMeshClosed,battle.RedesignNearPassInside,battle.RedesignNearPassInsideUncertain,heroPoint=V(battle.RedesignNearPassHeroPoint),sharkPoint=V(battle.RedesignNearPassSharkPoint)});
        clearanceRows.Add(new{frame=frames,clock=battle.RedesignClock,battle.RedesignCameraLensInsideWood,battle.RedesignRiggingGroupCount,battle.RedesignRiggingReleasedCount,battle.RedesignRiggingMaximumSourceGap,battle.RedesignRiggingMaximumClothWarpError,battle.RedesignRiggingPrepareMs,battle.RedesignRiggingMaximumTickMs,battle.RedesignRiggingMaximumSkinReadMs,battle.RedesignRiggingMaximumGeometryMs,battle.RedesignRiggingMaximumSkinTreeBuildMs,battle.RedesignRiggingMaximumSkinTreeRefitMs,battle.RedesignRiggingMaximumNativeTreeBuildMs,battle.RedesignRiggingMaximumQueryMs,battle.RedesignRiggingMaximumReplaceMs,battle.RedesignRiggingMaximumBodyInitializeMs,battle.RedesignRiggingMaximumSweepSamples,battle.RedesignRiggingSkinTriangleCount,battle.RedesignRiggingMaximumTrianglePairTests});
        Capture();frames++;
        if(frames%fps==0){File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows.Last()));File.WriteAllText(folder+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));File.WriteAllText(folder+"/deck-gaps.json",Newtonsoft.Json.JsonConvert.SerializeObject(deckRows));}
        if(realInputs)DriveRealInputs();
        else if(battle.TutorialWaiting)
        {
            waitingFrames++;
            if(waitingFrames>=fps){battle.DiagnosticAcceptLesson();waitingFrames=0;}
        }
        else if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial)battle.DiagnosticCompleteMovement();
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial||frames>fps*45||Time.realtimeSinceStartupAsDouble-started>450)Finish();
    }
    void DriveRealInputs()
    {
        var lesson=battle.CurrentLesson;
        if(lesson!=lastLesson||battle.RedesignStage!=lastInputStage||battle.TutorialWaiting&&!lastInputWaiting){lastLesson=lesson;lastInputStage=battle.RedesignStage;realGateFrames=0;}
        lastInputWaiting=battle.TutorialWaiting;
        realGateFrames++;
        Key key=Key.None;bool left=false,right=false;
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial)
        {
            if(lesson==Story1WreckBattle.Lesson.Move&&!mapTested)
            {
                if(realGateFrames<fps){key=Key.PageDown;if(!battle.TutorialWaiting||Time.timeScale!=0)throw new Exception("Wrong input advanced tutorial");}
                else if(realGateFrames==fps){sequence.sahur.GetComponent<IslandMapUI>()?.SetOpen(true);inputChecks.Add("Wrong input left gate paused");}
                else if(realGateFrames<fps*2){key=GameInputSettings.Get(GameInputSettings.Action.Forward);if(!battle.TutorialWaiting)throw new Exception("Map input advanced tutorial");}
                else {sequence.sahur.GetComponent<IslandMapUI>()?.SetOpen(false);mapTested=true;inputChecks.Add("Map input did not consume gate");}
            }
            else if(realGateFrames>=fps)
            {
                if(lesson==Story1WreckBattle.Lesson.Move||lesson==Story1WreckBattle.Lesson.Swim)key=GameInputSettings.Get(GameInputSettings.Action.Forward);
                else if(lesson==Story1WreckBattle.Lesson.Attack)left=true;
                else if(lesson==Story1WreckBattle.Lesson.Dodge)key=GameInputSettings.Get(GameInputSettings.Action.Dodge);
                else if(lesson==Story1WreckBattle.Lesson.Jump)key=GameInputSettings.Get(GameInputSettings.Action.Jump);
                else if(lesson==Story1WreckBattle.Lesson.ChargeHold)right=true;
            }
            // Release charge as soon as the game itself declares it ready.
        }
        if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,key==Key.None?new KeyboardState():new KeyboardState(key));
        if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));
    }
    float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
    readonly List<object> nearPassRows=new List<object>(),clearanceRows=new List<object>();
    void Capture()
    {
        var camera=sequence.storyCamera;var previous=camera.targetTexture;var active=RenderTexture.active;
        var overlays=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras=overlays.Select(c=>c.worldCamera).ToArray();var distances=overlays.Select(c=>c.planeDistance).ToArray();
        try
        {
            foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=2;}
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0,false);
            File.WriteAllBytes(folder+"/game-"+frames.ToString("D5")+".png",pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previous;RenderTexture.active=active;
            for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];}
        }
    }
    void Finish()
    {
        SaveClearances();
        done=true;File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{fps,frames,stage=battle.RedesignStage.ToString(),diagnosticInputs=!realInputs,inputChecks,battle.RedesignEvents,rows},Newtonsoft.Json.Formatting.Indented));
        Time.captureFramerate=0;Time.timeScale=1;EditorApplication.isPlaying=false;
    }
    void OnDestroy()
    {
        SaveClearances();
        if(!done&&folder!=null&&Directory.Exists(folder))File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{fps,frames,stage=battle!=null?battle.RedesignStage.ToString():"Stopped",aborted=true,diagnosticInputs=!realInputs,inputChecks,rows},Newtonsoft.Json.Formatting.Indented));
        Time.captureFramerate=0;if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,new MouseState());if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);
    }
    void SaveClearances(){if(folder!=null&&Directory.Exists(folder)){File.WriteAllText(folder+"/near-pass.json",Newtonsoft.Json.JsonConvert.SerializeObject(nearPassRows));File.WriteAllText(folder+"/clearance.json",Newtonsoft.Json.JsonConvert.SerializeObject(clearanceRows));}}
}
