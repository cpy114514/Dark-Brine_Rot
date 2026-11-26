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
using System.Reflection;
public static class ReviewContactsV224
{
    public static object Start(string label="first-15",int fps=15,bool realInputs=false)
    {
        if(!Application.isPlaying)throw new Exception("Enter the existing Editor's play mode first");
        if(UnityEngine.Object.FindFirstObjectByType<ContactsObserverV224>()!=null)throw new Exception("Review already running");
        var go=new GameObject("Opening v214 actual camera recorder");UnityEngine.Object.DontDestroyOnLoad(go);
        var observer=go.AddComponent<ContactsObserverV224>();observer.label=label;observer.fps=fps;observer.realInputs=realInputs;
        return new{started=true,label,fps};
    }
}
[DefaultExecutionOrder(31000)]
public sealed class ContactsObserverV224:MonoBehaviour
{
    public string label;public int fps;public bool realInputs;
    bool mapTested;int realGateFrames;Story1WreckBattle.Lesson lastLesson=(Story1WreckBattle.Lesson)(-1);
    Story1WreckBattle.OpeningStage lastInputStage=(Story1WreckBattle.OpeningStage)(-1);
    bool lastInputWaiting;
    readonly List<string> inputChecks=new List<string>();
    readonly List<object> deckRows=new List<object>();
    readonly List<object> nativeRows=new List<object>();
    readonly Dictionary<Story1WreckBattle.Lesson,int> nativeSamples=new Dictionary<Story1WreckBattle.Lesson,int>();
    Mesh nativeProbe;object nativeTree;
    Unity.Profiling.ProfilerRecorder solidsRecorder;
    string folder;int frames,waitingFrames;bool done;Story1WreckBattle battle;Story1SharkCollisionSequence sequence;
    readonly List<object> rows=new List<object>();RenderTexture target;Texture2D pixels;double started;
    void Start()
    {
        folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/encounter-redesign-20261017/"+label;
        if(Directory.Exists(folder))throw new Exception("Keep previous takes; choose a new label");
        Directory.CreateDirectory(folder);Time.captureFramerate=fps;
        target=new RenderTexture(640,360,24);target.Create();pixels=new Texture2D(640,360,TextureFormat.RGB24,false);
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();started=Time.realtimeSinceStartupAsDouble;
        battle.RedesignNearPassDiagnostics=false;battle.DiagnosticScatterSeed=223;
        solidsRecorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Scripts,"Opening.NativeSolids",16);
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
        rows.Add(new{frame=frames,clock=battle.RedesignClock,stage=battle.RedesignStage.ToString(),beat=battle.RedesignBeat,lesson=battle.CurrentLesson.ToString(),battle.TutorialWaiting,battle.RedesignShot,battle.RedesignContacts,battle.RedesignContactGap,battle.RedesignSignedContactGap,battle.RedesignWaterPredictionCost,battle.RedesignWaterBodyPenetration,battle.RedesignLostStickFloating,battle.RedesignDeckClearance,battle.RedesignCameraWoodObstruction,battle.TutorialDamage,battle.TutorialDamageBudget,battle.TutorialHealthFloor,health.currentHealth,hero.enabled,hero.Swimming,hero.OnFloatingWreckDeck,support=hero.GetComponent<Story1WreckRider>()?.Support()?.name,hero=V(hero.transform.position),head=V(head.position),chest=V(chest.position),shark=V(sequence.swimmer.position),camera=V(sequence.storyCamera.transform.position),rate=Time.timeScale});
        if(battle.RedesignNearPassValid&&Mathf.Abs(battle.RedesignNearPassSampleClock-battle.RedesignClock)<.09f)
            nearPassRows.Add(new{frame=frames,clock=battle.RedesignNearPassSampleClock,gap=battle.RedesignNearPassSignedGap,regions=V(battle.RedesignNearPassRegionGaps),battle.RedesignNearPassClosestRegion,battle.RedesignNearPassEnvelopeMeasured,radii=V(battle.RedesignNearPassEnvelopeRadii),battle.RedesignNearPassMeshClosed,battle.RedesignNearPassInside,battle.RedesignNearPassInsideUncertain,heroPoint=V(battle.RedesignNearPassHeroPoint),sharkPoint=V(battle.RedesignNearPassSharkPoint)});
        clearanceRows.Add(new{frame=frames,clock=battle.RedesignClock,battle.RedesignCameraLensInsideWood,battle.RedesignRiggingGroupCount,battle.RedesignRiggingReleasedCount,battle.RedesignRiggingMaximumSourceGap,battle.RedesignRiggingMaximumClothWarpError,battle.RedesignRiggingPrepareMs,battle.RedesignRiggingMaximumTickMs,battle.RedesignRiggingMaximumSkinReadMs,battle.RedesignRiggingMaximumGeometryMs,battle.RedesignRiggingMaximumSkinTreeBuildMs,battle.RedesignRiggingMaximumSkinTreeRefitMs,battle.RedesignRiggingMaximumNativeTreeBuildMs,battle.RedesignRiggingMaximumQueryMs,battle.RedesignRiggingMaximumReplaceMs,battle.RedesignRiggingMaximumBodyInitializeMs,battle.RedesignRiggingMaximumSweepSamples,battle.RedesignRiggingSkinTriangleCount,battle.RedesignRiggingMaximumTrianglePairTests});
        float woodCost=0;
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial&&battle.CurrentLesson==Story1WreckBattle.Lesson.Dodge&&!battle.TutorialWaiting)
        {
            Call("RedWaterCollectShapes");Call("RedTutorialBody");
            var shapes=(System.Collections.IList)Field("redWaterShapes");var support=(Story1WreckPlank)Field("redCombatBoard");
            for(int i=shapes.Count-1;i>=0;i--)if(ReferenceEquals(shapes[i].GetType().GetField("mesh").GetValue(shapes[i]),support.Collision))shapes.RemoveAt(i);
            object[] args={Field("redWaterCapsules"),Vector3.zero,0f,0f,null};woodCost=(float)Call("RedWaterBodyCost",args);
        }
        tutorialRows.Add(new{frame=frames,clock=battle.RedesignClock,stage=battle.RedesignStage.ToString(),lesson=battle.CurrentLesson.ToString(),battle.TutorialWaiting,battle.RedesignTutorialHeroGap,battle.RedesignDodgeCandidates,battle.RedesignDodgeTravel,battle.RedesignSwimControlSeconds,battle.RedesignTutorialClearanceMs,battle.RedesignDodgePlanMs,battle.RedesignTutorialPreparationMs,woodCost});
        if(frames%15==0&&battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial&&battle.CurrentLesson!=Story1WreckBattle.Lesson.Move&&battle.CurrentLesson!=Story1WreckBattle.Lesson.Jump&&battle.CurrentLesson!=Story1WreckBattle.Lesson.Swim)
        {
            nativeSamples.TryGetValue(battle.CurrentLesson,out int count);
            if(count<4){nativeRows.Add(VerifyNativeGeometry());nativeSamples[battle.CurrentLesson]=count+1;}
        }
        if(battle.RedesignActive&&battle.RedesignStage!=Story1WreckBattle.OpeningStage.Crush&&(frames%3==0||battle.RedesignStage==Story1WreckBattle.OpeningStage.Pursuit||battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel))filmRows.Add(MeasureFilm());
        Capture();frames++;
        if(frames%fps==0){File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows.Last()));File.WriteAllText(folder+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));File.WriteAllText(folder+"/deck-gaps.json",Newtonsoft.Json.JsonConvert.SerializeObject(deckRows));}
        if(realInputs)DriveRealInputs();
        else if(battle.TutorialWaiting)
        {
            waitingFrames++;
            if(waitingFrames>=fps){battle.DiagnosticAcceptLesson();waitingFrames=0;}
        }
        else if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial)battle.DiagnosticCompleteMovement();
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Finished||battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&(float)Field("redesignTime")>24&&battle.RedesignContacts<3||battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial&&battle.CurrentLesson==Story1WreckBattle.Lesson.Move&&!battle.TutorialWaiting&&(float)Field("lessonTime")>5||frames>fps*165||Time.realtimeSinceStartupAsDouble-started>900)Finish();
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
            if(lesson==Story1WreckBattle.Lesson.Dodge&&battle.TutorialWaiting&&realGateFrames==fps-1)CheckAdaptiveRoutes();
            // Release charge as soon as the game itself declares it ready.
        }
        if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,key==Key.None?new KeyboardState():new KeyboardState(key));
        if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));
    }
    float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
    object Field(string name)=>typeof(Story1WreckBattle).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(battle);
    object Call(string name,params object[] args)=>typeof(Story1WreckBattle).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(battle,args);
    readonly List<object> filmRows=new List<object>();
    object MeasureFilm()
    {
        var skin=(SkinnedMeshRenderer)Call("RedContactSkin");
        if(nativeProbe==null)nativeProbe=new Mesh();skin.BakeMesh(nativeProbe,true);var fresh=nativeProbe.vertices;
        var points=new Vector3[fresh.Length];for(int i=0;i<fresh.Length;i++)points[i]=skin.transform.TransformPoint(fresh[i]);
        var type=typeof(Story1WreckBattle).GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
        if(nativeTree==null)nativeTree=Activator.CreateInstance(type,new object[]{points,skin.sharedMesh.triangles});else type.GetMethod("Refit").Invoke(nativeTree,new object[]{points});
        Call("RedTutorialBody");var capsules=(System.Collections.IList)Field("redWaterCapsules");
        float minimum=10000;int region=-1,index=0;
        foreach(var capsule in capsules)
        {
            var c=capsule.GetType();float gap=(float)type.GetMethod("CapsuleGapUpTo").Invoke(nativeTree,new object[]{(Vector3)c.GetField("a").GetValue(capsule),(Vector3)c.GetField("b").GetValue(capsule),(float)c.GetField("radius").GetValue(capsule),.5f});
            if(gap<minimum){minimum=gap;region=index;}index++;
        }
        var wood=new List<string>();
        foreach(var shape in UnityEngine.Object.FindObjectsByType<Story1WreckMeshCollision>(FindObjectsSortMode.None))
        {
            if(!shape.Active||!shape.WorldBounds.Intersects(skin.bounds))continue;
            var filter=shape.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
            var vertices=filter.sharedMesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=shape.transform.TransformPoint(vertices[i]);
            object[] args={vertices,filter.sharedMesh.triangles,Vector3.zero};
            if((bool)type.GetMethod("Touch",new[]{typeof(Vector3[]),typeof(int[]),typeof(Vector3).MakeByRefType()}).Invoke(nativeTree,args))wood.Add(shape.name);
        }
        float heroWoodCost=0,heroWoodDepth=0;string heroWoodPiece=null;
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Pursuit||battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel)
        {
            Call("RedWaterCollectShapes");var shapes=(System.Collections.IList)Field("redWaterShapes");var support=(Story1WreckPlank)Field("redCombatBoard");
            bool supported=battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel||(float)Field("redesignTime")>=9;
            if(supported)for(int i=shapes.Count-1;i>=0;i--)if(ReferenceEquals(shapes[i].GetType().GetField("mesh").GetValue(shapes[i]),support.Collision))shapes.RemoveAt(i);
            object[] args={Field("redWaterCapsules"),Vector3.zero,0f,0f,null};heroWoodCost=(float)Call("RedWaterBodyCost",args);
            if(heroWoodCost>0)
            {
                var query=(CapsuleCollider)Field("redWaterQuery");query.enabled=true;
                try
                {
                    foreach(var capsule in capsules)
                    {
                        var c=capsule.GetType();var a=(Vector3)c.GetField("a").GetValue(capsule);var b=(Vector3)c.GetField("b").GetValue(capsule);float radius=(float)c.GetField("radius").GetValue(capsule);
                        query.radius=radius;query.height=Vector3.Distance(a,b)+2*radius;Quaternion turn=Quaternion.FromToRotation(Vector3.up,b-a);
                        Bounds bounds=new Bounds((a+b)*.5f,Vector3.zero);bounds.Encapsulate(a);bounds.Encapsulate(b);bounds.Expand(radius*2);
                        foreach(var shape in shapes)
                        {
                            var mesh=(Story1WreckMeshCollision)shape.GetType().GetField("mesh").GetValue(shape);if(!mesh.WorldBounds.Intersects(bounds))continue;
                            for(int part=0;part<mesh.Parts.Length;part++)
                                if(mesh.PartBounds(part).Intersects(bounds)&&Physics.ComputePenetration(query,(a+b)*.5f,turn,mesh.Parts[part],mesh.PartPosition(part),mesh.PartRotation(part),out _,out float depth)&&depth>heroWoodDepth)
                                {heroWoodDepth=depth;heroWoodPiece=mesh.name;}
                        }
                    }
                }
                finally{query.enabled=false;}
            }
        }
        return new{frame=frames,stage=battle.RedesignStage.ToString(),clock=battle.RedesignClock,localTime=(float)Field("redesignTime"),beat=battle.RedesignBeat,minimum,region,wood,heroWoodCost,heroWoodDepth,heroWoodPiece,battle.RedesignSolidWoodContacts,battle.RedesignSolidFractures,battle.RedesignSolidUnresolved,battle.RedesignSolidSupportLift,battle.RedesignSolidMs,profilerNs=solidsRecorder.Valid?solidsRecorder.LastValue:-1,scale=V(sequence.swimmer.localScale)};
    }
    object VerifyNativeGeometry()
    {
        var skin=(SkinnedMeshRenderer)Call("RedContactSkin");
        var cached=(List<Vector3>)Field("redPosedVertices");
        if(nativeProbe==null)nativeProbe=new Mesh();skin.BakeMesh(nativeProbe,true);var fresh=nativeProbe.vertices;
        float bakeError=0;for(int i=0;i<fresh.Length;i++)bakeError=Mathf.Max(bakeError,Vector3.Distance(fresh[i],cached[i]));
        if(bakeError>.0001f)throw new Exception("Cached bone pose does not match a fresh native bake: "+bakeError);
        var matrix=skin.transform.localToWorldMatrix;var points=new Vector3[fresh.Length];float matrixError=0;
        for(int i=0;i<fresh.Length;i++){points[i]=matrix.MultiplyPoint3x4(fresh[i]);matrixError=Mathf.Max(matrixError,Vector3.Distance(points[i],skin.transform.TransformPoint(fresh[i])));}
        if(matrixError>.001f)throw new Exception("Cached transform matrix changed world geometry: "+matrixError);
        var type=typeof(Story1WreckBattle).GetNestedType("RedRiggingTree",BindingFlags.NonPublic);
        if(nativeTree==null)nativeTree=Activator.CreateInstance(type,new object[]{points,skin.sharedMesh.triangles});else type.GetMethod("Refit").Invoke(nativeTree,new object[]{points});
        Call("RedTutorialBody");var capsules=(System.Collections.IList)Field("redWaterCapsules");
        var exact=type.GetMethod("CapsuleGap");var capped=type.GetMethod("CapsuleGapUpTo");float minimum=float.PositiveInfinity,maximumError=0;double exactMs=0,cappedMs=0;
        foreach(var capsule in capsules)
        {
            var body=capsule.GetType();Vector3 a=(Vector3)body.GetField("a").GetValue(capsule),b=(Vector3)body.GetField("b").GetValue(capsule);float radius=(float)body.GetField("radius").GetValue(capsule);
            var watch=System.Diagnostics.Stopwatch.StartNew();float actual=(float)exact.Invoke(nativeTree,new object[]{a,b,radius});exactMs+=watch.Elapsed.TotalMilliseconds;
            watch.Restart();float limited=(float)capped.Invoke(nativeTree,new object[]{a,b,radius,.4f});cappedMs+=watch.Elapsed.TotalMilliseconds;
            minimum=Mathf.Min(minimum,actual);maximumError=Mathf.Max(maximumError,Mathf.Abs(limited-Mathf.Min(actual,.4f)));
        }
        if(maximumError>.001f)throw new Exception("Native posed shark query changed: "+maximumError);
        if(battle.CurrentLesson==Story1WreckBattle.Lesson.Dodge&&minimum<.39f)throw new Exception("Dodge lost actual native body clearance: "+minimum);
        return new{frame=frames,lesson=battle.CurrentLesson.ToString(),bakeError,matrixError,minimum,maximumError,exactMs,cappedMs};
    }
    void CheckAdaptiveRoutes()
    {
        Call("RedPlanTutorialDodge");Vector3 original=(Vector3)Field("redDodgeEndLocal");
        var board=(Story1WreckPlank)Field("redCombatBoard");Vector3 target=board.transform.TransformPoint(original);
        var piece=battle.WreckDetails.Where(p=>p!=null&&!p.IsBroken&&p.Collision!=null&&p.Collision.Active&&p.Collision.WorldBounds.size.magnitude>1&&p.Collision.WorldBounds.size.magnitude<9).OrderBy(p=>Mathf.Abs(p.Collision.WorldBounds.size.magnitude-4)).FirstOrDefault();
        if(piece==null)throw new Exception("Adaptive route fixture has no native timber");
        var transform=piece.transform;Vector3 position=transform.position;Quaternion rotation=transform.rotation;
        var body=piece.GetComponent<Rigidbody>();Vector3 velocity=body.linearVelocity,spin=body.angularVelocity;
        Vector3 alternate;int candidates;float travel;
        try
        {
            transform.position+=target+Vector3.up*1.5f-piece.Collision.WorldBounds.center;body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
            Call("RedPlanTutorialDodge");alternate=(Vector3)Field("redDodgeEndLocal");candidates=battle.RedesignDodgeCandidates;travel=battle.RedesignDodgeTravel;
        }
        finally{transform.SetPositionAndRotation(position,rotation);body.linearVelocity=velocity;body.angularVelocity=spin;Physics.SyncTransforms();Call("RedPlanTutorialDodge");}
        float difference=Vector3.Distance(board.transform.TransformPoint(original),board.transform.TransformPoint(alternate));
        File.WriteAllText(folder+"/adaptive-obstacle.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{original=V(original),alternate=V(alternate),difference,candidates,travel,nativeTimber=piece.name}));
        if(difference<.5f||travel<1.5f)throw new Exception("Native obstacle did not produce another usable roll destination");
        inputChecks.Add("Moving real timber into the chosen endpoint selected a different valid destination");
    }
    readonly List<object> nearPassRows=new List<object>(),clearanceRows=new List<object>(),tutorialRows=new List<object>();
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
        solidsRecorder.Dispose();
        SaveClearances();
        if(!done&&folder!=null&&Directory.Exists(folder))File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{fps,frames,stage=battle!=null?battle.RedesignStage.ToString():"Stopped",aborted=true,diagnosticInputs=!realInputs,inputChecks,rows},Newtonsoft.Json.Formatting.Indented));
        Time.captureFramerate=0;if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,new MouseState());if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);if(nativeProbe!=null)Destroy(nativeProbe);
    }
    void SaveClearances(){if(folder!=null&&Directory.Exists(folder)){File.WriteAllText(folder+"/film-solids.json",Newtonsoft.Json.JsonConvert.SerializeObject(filmRows));File.WriteAllText(folder+"/near-pass.json",Newtonsoft.Json.JsonConvert.SerializeObject(nearPassRows));File.WriteAllText(folder+"/clearance.json",Newtonsoft.Json.JsonConvert.SerializeObject(clearanceRows));File.WriteAllText(folder+"/tutorial.json",Newtonsoft.Json.JsonConvert.SerializeObject(tutorialRows));File.WriteAllText(folder+"/native-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(nativeRows));}}
}
