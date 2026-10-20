using System;
using UnityEngine.Playables;
using System.Collections;
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
public static class ReviewContactsPerfV225
{
    public static object Start(string label="first-15",int fps=15,bool realInputs=false)
    {
        if(!Application.isPlaying)throw new Exception("Enter the existing Editor's play mode first");
        if(UnityEngine.Object.FindFirstObjectByType<ContactsObserverPerfV225>()!=null)throw new Exception("Review already running");
        var go=new GameObject("Opening v214 actual camera recorder");UnityEngine.Object.DontDestroyOnLoad(go);
        var observer=go.AddComponent<ContactsObserverPerfV225>();observer.label=label;observer.fps=fps;observer.realInputs=realInputs;
        return new{started=true,label,fps};
    }
}
[DefaultExecutionOrder(31000)]
public sealed class ContactsObserverPerfV225:MonoBehaviour
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
        folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-performance-20261019/"+label;
        if(Directory.Exists(folder))throw new Exception("Keep previous takes; choose a new label");
        Directory.CreateDirectory(folder);Time.captureFramerate=fps;
        target=new RenderTexture(640,360,24);target.Create();pixels=new Texture2D(640,360,TextureFormat.RGB24,false);
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();started=Time.realtimeSinceStartupAsDouble;
        battle.RedesignNearPassDiagnostics=false;battle.DiagnosticScatterSeed=223;
        solidsRecorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Scripts,"Opening.NativeSolids",16);
    }
    bool fixturesRun,fallVerified;
    void LateUpdate()
    {
        if(done)return;
        try{Observe();}
        catch(Exception e){File.WriteAllText(folder+"/capture-error.txt",e.ToString());Finish();}
    }
    void Observe()
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
        if(fps==30&&!fixturesRun&&battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial&&battle.CurrentLesson==Story1WreckBattle.Lesson.Attack&&battle.TutorialWaiting)
        {
            fixturesRun=true;TestOpeningSnapshotsV225.Run();TestNativeWoodSeparationPerfV225.Run();TestJointNativeWoodPerfV225.Run();
        }
        if(!fallVerified&&battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&(float)Field("redesignTime")>=20.9f)
        {fallVerified=true;VerifyPreparedFall();}
        if(realInputs)DriveRealInputs();
        else if(battle.TutorialWaiting)
        {
            waitingFrames++;
            if(waitingFrames>=fps){battle.DiagnosticAcceptLesson();waitingFrames=0;}
        }
        else if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial)battle.DiagnosticCompleteMovement();
        if(battle.RedesignStage==Story1WreckBattle.OpeningStage.Finished||battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&(float)Field("redesignTime")>24&&battle.RedesignContacts<3||battle.RedesignStage==Story1WreckBattle.OpeningStage.Tutorial&&battle.CurrentLesson==Story1WreckBattle.Lesson.Move&&!battle.TutorialWaiting&&(float)Field("lessonTime")>5||frames>fps*165||Time.realtimeSinceStartupAsDouble-started>900)Finish();
    }
    void VerifyPreparedFall()
    {
        var radii=(float[])Field("redWaterFallRadii");
        if(radii==null)throw new Exception("Exact fall envelope did not finish preparing before tail contact");
        var initial=(float[])Field("redWaterBodyRadii");var original=(float[])initial.Clone();
        var clip=(AnimationClip)Field("redClip");var playable=(UnityEngine.Animations.AnimationClipPlayable)Field("redPose");float time=(float)playable.GetTime();
        var player=sequence.sahur.transform;var position=player.position;var rotation=player.rotation;
        var graph=(UnityEngine.Playables.PlayableGraph)Field("redGraph");float error=0;
        try
        {
            Call("RedPose","PLAYER_Fall_v006",2.2f,false);player.rotation=Quaternion.LookRotation((Vector3)Field("redForward"));graph.Evaluate(0);
            Call("RedWaterReadBody");Call("RedWaterMeasureNativeEnvelope");var expected=(float[])Field("redWaterBodyRadii");
            for(int i=0;i<radii.Length;i++)error=Mathf.Max(error,Mathf.Abs(expected[i]-Mathf.Max(original[i],radii[i])));
            if(error>.002f)throw new Exception("Prepared fall envelope differs from a fresh original native measurement: "+error);
            File.WriteAllText(folder+"/prepared-fall-oracle.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,capsules=radii.Length,maximumRadiusError=error}));
        }
        finally
        {
            typeof(Story1WreckBattle).GetField("redWaterBodyRadii",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(battle,initial);
            Array.Copy(original,initial,original.Length);player.SetPositionAndRotation(position,rotation);Call("RedPose",clip.name,time,false);Call("RedWaterReadBody");
        }
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
        return new{frame=frames,stage=battle.RedesignStage.ToString(),clock=battle.RedesignClock,localTime=(float)Field("redesignTime"),beat=battle.RedesignBeat,minimum,region,wood,heroWoodCost,heroWoodDepth,heroWoodPiece,battle.RedesignSolidWoodContacts,battle.RedesignSolidFractures,battle.RedesignSolidUnresolved,battle.RedesignSolidSupportLift,battle.RedesignSolidMs,battle.RedesignWaterPreviewPrepared,profilerNs=solidsRecorder.Valid?solidsRecorder.LastValue:-1,scale=V(sequence.swimmer.localScale)};
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



public static class TestOpeningSnapshotsV225
{
 struct RedWaterCapsule {public Vector3 a,b;public float radius;}
 struct RedWaterShape {public Story1WreckMeshCollision mesh;public Rigidbody body;public Bounds bounds;public Vector3 velocity,spin,centre;}
 static readonly List<RedWaterShape> redWaterShapes=new List<RedWaterShape>();static CapsuleCollider redWaterQuery;
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 public static object Run()
 {
  var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();var type=typeof(Story1WreckBattle);
  object Field(string n)=>type.GetField(n,Private).GetValue(battle);object Call(string n,params object[] args)=>type.GetMethod(n,Private).Invoke(battle,args);
  Call("RedWaterCollectShapes");Call("RedTutorialBody");redWaterQuery=(CapsuleCollider)Field("redWaterQuery");
  var runtimeBody=(IList)Field("redWaterCapsules");var body=new List<RedWaterCapsule>();
  foreach(var c in runtimeBody){var t=c.GetType();body.Add(new RedWaterCapsule{a=(Vector3)t.GetField("a").GetValue(c),b=(Vector3)t.GetField("b").GetValue(c),radius=(float)t.GetField("radius").GetValue(c)});}
  void Snapshot()
  {
   redWaterShapes.Clear();foreach(var value in (IList)Field("redWaterShapes")){var t=value.GetType();object Get(string n)=>t.GetField(n).GetValue(value);redWaterShapes.Add(new RedWaterShape{mesh=(Story1WreckMeshCollision)Get("mesh"),body=(Rigidbody)Get("body"),bounds=(Bounds)Get("bounds"),velocity=(Vector3)Get("velocity"),spin=(Vector3)Get("spin"),centre=(Vector3)Get("centre")});}
  }
  Snapshot();var rows=new List<object>();float error=0,pushError=0;double originalMs=0,cachedMs=0;var method=type.GetMethod("RedWaterBodyCost",Private);
  float[] times={0,.7f,1.3f,1.65f,2.35f,3.45f};
  for(int i=0;i<48;i++)
  {
   if(i==24){Call("RedWaterCollectShapes");Snapshot();}
   Vector3 offset=new Vector3((i%5-2)*1.2f,0,(i%7-3)*1.1f);float seconds=times[i%6],margin=i%2==0?.1f:.65f;
   var watch=System.Diagnostics.Stopwatch.StartNew();float expected=RedWaterBodyCost(body,offset,seconds,margin,out var push);originalMs+=watch.Elapsed.TotalMilliseconds;
   object[] args={runtimeBody,offset,seconds,margin,Vector3.zero};watch.Restart();float actual=(float)method.Invoke(battle,args);cachedMs+=watch.Elapsed.TotalMilliseconds;
   error=Mathf.Max(error,Mathf.Abs(expected-actual));pushError=Mathf.Max(pushError,Vector3.Distance(push,(Vector3)args[4]));
   if(error>.0001f||pushError>.001f)throw new Exception("Water snapshot changes original loop "+i+": "+error+" / "+pushError);
   rows.Add(new{i,expected,actual});
  }
  var shape=redWaterShapes[0].mesh;var box=shape.WorldBounds;float rayError=0;int rayChecks=0;
  var snapshot=typeof(Story1WreckMeshCollision).GetMethod("ReadRaycastSnapshot",Private).Invoke(shape,null);var rayMethod=snapshot.GetType().GetMethod("Raycast");
  for(int i=0;i<180;i++)
  {
   var origin=box.center+new Vector3((i%15-7)/6f*box.extents.x,box.extents.y+10,(i/15-5.5f)/5f*box.extents.z);var ray=new Ray(origin,Vector3.down);float reach=box.size.y+25;
   bool expected=LegacyRay(shape,ray,out var old,reach);object[] args={ray,default(RaycastHit),reach};bool actual=(bool)rayMethod.Invoke(snapshot,args);var hit=(RaycastHit)args[1];
   if(expected!=actual)throw new Exception("Support batch changes native ray admission "+i);
   if(actual){rayError=Mathf.Max(rayError,Vector3.Distance(old.point,hit.point));if(rayError>.0001f)throw new Exception("Support batch changes native hit point");}rayChecks++;
  }
  var result=new{passed=true,forecastChecks=rows.Count,maximumCostError=error,maximumPushError=pushError,originalMs,cachedMs,rayChecks,maximumRayPointError=rayError,rows};
  File.WriteAllText(".codex/animation-performance-20261019/snapshot-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 static bool LegacyRay(Story1WreckMeshCollision shape,Ray ray,out RaycastHit nearest,float reach)
 {
  var body=shape.GetComponent<Rigidbody>();Matrix4x4 toPhysics=Matrix4x4.identity,toVisual=Matrix4x4.identity;
  if(body&&body.interpolation!=RigidbodyInterpolation.None){var physics=Matrix4x4.TRS(body.position,body.rotation,shape.transform.lossyScale);toPhysics=physics*shape.transform.worldToLocalMatrix;toVisual=shape.transform.localToWorldMatrix*physics.inverse;}
  var query=new Ray(toPhysics.MultiplyPoint3x4(ray.origin),toPhysics.MultiplyVector(ray.direction));nearest=default;bool found=false;
  for(int i=0;i<shape.Parts.Length;i++)
  {
   var part=shape.Parts[i];var bounds=shape.PartBounds(i);bounds.Expand(.04f);if(!bounds.IntersectRay(ray,out var entry)||entry>reach)continue;
   if(part.enabled&&part.Raycast(query,out var hit,reach)){hit.point=toVisual.MultiplyPoint3x4(hit.point);hit.normal=toVisual.MultiplyVector(hit.normal).normalized;nearest=hit;reach=hit.distance;found=true;}
  }
  return found;
 }
    static Vector3 RedWaterForecastTranslation(RedWaterShape shape,float seconds)
    {
        // Water damps vertical velocity quickly. Preserve measured horizontal
        // drift and retain the initial vertical momentum over its first .3 s.
        return Vector3.ProjectOnPlane(shape.velocity,Vector3.up)*seconds+
            Vector3.up*shape.velocity.y*(.3f*(1-Mathf.Exp(-seconds/.3f)));
    }
    static float RedWaterCapsuleCost(RedWaterCapsule capsule,Vector3 offset,float seconds,float margin,out Vector3 avoidance)
    {
        avoidance=Vector3.zero;float cost=0;
        Vector3 a=capsule.a+offset,b=capsule.b+offset;float radius=capsule.radius+margin;
        var capsuleBounds=new Bounds((a+b)*.5f,Vector3.zero);capsuleBounds.Encapsulate(a);capsuleBounds.Encapsulate(b);capsuleBounds.Expand(radius*2);
        redWaterQuery.radius=radius;redWaterQuery.height=(b-a).magnitude+radius*2;
        Quaternion capsuleRotation=(b-a).sqrMagnitude>.00001f?Quaternion.FromToRotation(Vector3.up,b-a):Quaternion.identity;
        redWaterQuery.enabled=true;
        try
        {
        foreach(var shape in redWaterShapes)
        {
            Vector3 translation=RedWaterForecastTranslation(shape,seconds);
            Quaternion rotation=shape.spin.sqrMagnitude>.0001f?Quaternion.AngleAxis(shape.spin.magnitude*Mathf.Rad2Deg*Mathf.Min(seconds,.7f),shape.spin.normalized):Quaternion.identity;
            // Rotation-aware broad phase; exact checks below use real cut parts.
            float reach=shape.bounds.extents.magnitude+Vector3.Distance(shape.bounds.center,shape.centre);
            Bounds future=new Bounds(shape.centre+translation,Vector3.one*(reach*2));
            if(!future.Intersects(capsuleBounds))continue;
            for(int i=0;i<shape.mesh.Parts.Length;i++)
            {
                var part=shape.mesh.Parts[i];
                if(part==null||!part.enabled||!part.gameObject.activeInHierarchy)continue;
                Vector3 point=shape.centre+translation+rotation*(shape.mesh.PartPosition(i)-shape.centre);
                Quaternion turn=rotation*shape.mesh.PartRotation(i);
                Bounds present=shape.mesh.PartBounds(i);Vector3 e=present.extents;
                Vector3 x=rotation*(Vector3.right*e.x),y=rotation*(Vector3.up*e.y),z=rotation*(Vector3.forward*e.z);
                Vector3 extents=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
                Bounds partBounds=new Bounds(shape.centre+translation+rotation*(present.center-shape.centre),extents*2);
                partBounds.Expand(radius*2);
                if(!partBounds.Intersects(capsuleBounds))continue;
                if(!Physics.ComputePenetration(redWaterQuery,(a+b)*.5f,capsuleRotation,part,point,turn,out var direction,out float depth))continue;
                cost+=depth*depth+.01f;
                Vector3 planar=Vector3.ProjectOnPlane(direction,Vector3.up);
                if(planar.sqrMagnitude<.04f)planar=Vector3.ProjectOnPlane((a+b)*.5f-(shape.centre+translation),Vector3.up);
                if(planar.sqrMagnitude>.0001f)avoidance+=planar.normalized*depth;
            }
        }
        return cost;
        }
        finally{redWaterQuery.enabled=false;}
    }
    static float RedWaterBodyCost(List<RedWaterCapsule> body,Vector3 offset,float seconds,float margin,out Vector3 avoidance)
    {
        avoidance=Vector3.zero;float cost=0;
        foreach(var capsule in body){cost+=RedWaterCapsuleCost(capsule,offset,seconds,margin,out var push);avoidance+=push;}
        return cost;
    }

}

public static class TestNativeWoodSeparationPerfV225
{
 public static object Run()
 {
  var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();if(b==null||!b.RedesignActive)throw new Exception("Run during the existing opening");
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var t=typeof(Story1WreckBattle);
  object Call(string name,params object[] args)=>t.GetMethod(name,flags).Invoke(b,args);
  var skin=(SkinnedMeshRenderer)Call("RedContactSkin");
  if(t.GetField("redSolidSharkTree",flags).GetValue(b)==null)throw new Exception("Wait until the tutorial begins");
  var source=b.Boards.First(p=>p!=null&&!p.IsBroken&&p.Collision.Active);
  var go=new GameObject("Native timber contact fixture v224",typeof(MeshFilter),typeof(Rigidbody),typeof(Story1WreckMeshCollision));
  var body=go.GetComponent<Rigidbody>();body.isKinematic=true;
  go.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
  go.transform.localScale=source.transform.lossyScale;
  var shape=go.GetComponent<Story1WreckMeshCollision>();shape.Initialize(go.GetComponent<MeshFilter>().sharedMesh,source.FractureSource?.splinters);
  foreach(var c in shape.Parts)c.isTrigger=true;
  var rows=new List<object>();int intersected=0;float biggest=0;var native=new Mesh();
  try
  {
   for(int i=0;i<24;i++)
   {
    go.transform.SetPositionAndRotation(skin.bounds.center+Quaternion.AngleAxis(i*60,Vector3.up)*Vector3.forward*(i%4)*skin.bounds.extents.x*.3f,Quaternion.Euler(i*13,i*47,i*7));
    Physics.SyncTransforms();object[] query={shape,skin,Vector3.zero,Vector3.zero};
    bool touch=(bool)Call("RedSolidTouch",query);float travel=0;bool remaining=false;
    // Independent full native triangle query, without the production broad
    // phase: contained timber must never be rejected as a distant shape.
    var matrix=skin.transform.worldToLocalMatrix*go.transform.localToWorldMatrix;
    var points=go.GetComponent<MeshFilter>().sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
    var tree=t.GetField("redSolidSharkTree",flags).GetValue(b);var treeType=tree.GetType();
    var woodTree=Activator.CreateInstance(treeType,new object[]{points,go.GetComponent<MeshFilter>().sharedMesh.triangles});
    var exact=treeType.GetMethod("Touch",new[]{treeType,typeof(Vector3).MakeByRefType()});
    bool reference=(bool)exact.Invoke(tree,new[]{woodTree,(object)Vector3.zero});
    if(touch!=reference)throw new Exception("Broad phase changed actual native contact for fixture "+i+": production="+touch+" reference="+reference);
    if(touch)
    {
     intersected++;var offset=(Vector3)Call("RedSolidSeparation",shape,skin,(shape.WorldBounds.center-skin.bounds.center).normalized);
     travel=offset.magnitude;biggest=Mathf.Max(biggest,travel);go.transform.position+=offset;Physics.SyncTransforms();
     remaining=(bool)Call("RedSolidTouch",new object[]{shape,skin,Vector3.zero,Vector3.zero});
     if(remaining)throw new Exception("Native timber remains in posed skin for fixture "+i);
    }
    rows.Add(new{i,touch,travel,remaining});
   }
   if(intersected<8)throw new Exception("Too few real native intersection cases: "+intersected);
   var result=new{passed=true,cases=rows.Count,intersected,biggest,rows};
   File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-performance-20261019/native-separation-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
   return result;
  }
  finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(native);}
 }
}

public static class TestJointNativeWoodPerfV225
{
 public static object Run()
 {
  var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();if(b==null||!b.RedesignActive)throw new Exception("Use current opening");
  var t=typeof(Story1WreckBattle);var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  object Call(string name,params object[] args)=>t.GetMethod(name,flags).Invoke(b,args);
  var skin=(SkinnedMeshRenderer)Call("RedContactSkin");Call("RedTutorialBody");
  var source=b.Boards.Where(p=>p!=null&&!p.IsBroken&&p.Collision.Active&&p.Dimensions.y<1.2f&&Mathf.Min(p.Dimensions.x,p.Dimensions.z)>7).OrderByDescending(p=>Mathf.Min(p.Dimensions.x,p.Dimensions.z)).First();
  var go=new GameObject("Joint native timber fixture v224",typeof(MeshFilter),typeof(Rigidbody),typeof(Story1WreckMeshCollision));
  go.GetComponent<Rigidbody>().isKinematic=true;go.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
  go.transform.localScale=source.transform.lossyScale;var shape=go.GetComponent<Story1WreckMeshCollision>();shape.Initialize(go.GetComponent<MeshFilter>().sharedMesh,source.FractureSource?.splinters);
  foreach(var c in shape.Parts)c.isTrigger=true;
  var hero=(ThirdPersonPlayerController)t.GetField("player",flags).GetValue(b);var chest=hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.Chest).position;
  var root=(Transform)t.GetField("shark",flags).GetValue(b);Vector3 originalRoot=root.position;
  var rows=new List<object>();int overlaps=0,joint=0;float maximum=0;
  try
  {
   // A reversible fixture brings the actual posed head beside the hero so
   // the test exercises simultaneous constraints, even at a distant gate.
   var tree=t.GetField("redSolidSharkTree",flags).GetValue(b);if(tree==null)throw new Exception("Wait for a solved opening frame");
   object[] closest={skin.transform.InverseTransformPoint(chest),0,Vector3.zero,Vector3.zero};tree.GetType().GetMethod("Closest").Invoke(tree,closest);
   Vector3 surface=skin.transform.TransformPoint((Vector3)closest[3]);root.position+=chest+Vector3.right*.35f-surface;
   for(int i=0;i<20;i++)
   {
    var centre=chest+Vector3.right*(i%5)*.1f;
    go.transform.SetPositionAndRotation(centre+new Vector3(i%2*.4f,0,(i%3-1)*.3f),Quaternion.Euler(i*17,i*47,i*13));Physics.SyncTransforms();
    bool shark=(bool)Call("RedSolidTouch",new object[]{shape,skin,Vector3.zero,Vector3.zero});
    bool body=(bool)Call("RedSolidWoodHeroOverlap",new object[]{shape,Vector3.zero,Vector3.zero});Vector3 offset=Vector3.zero;
    if(shark&&body)joint++;
    if(shark)offset=(Vector3)Call("RedSolidSeparation",shape,skin,(shape.WorldBounds.center-skin.bounds.center).normalized);
    else if(body)offset=(Vector3)t.GetMethod("RedSolidBoundsSeparation",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{shape.WorldBounds,Call("RedSolidBodyBounds")});
    if(shark||body)
    {
     overlaps++;offset=(Vector3)Call("RedSolidKeepWoodOffHero",shape,skin,offset);go.transform.position+=offset;Physics.SyncTransforms();
     bool sharkAfter=(bool)Call("RedSolidTouch",new object[]{shape,skin,Vector3.zero,Vector3.zero});
     bool heroAfter=(bool)Call("RedSolidWoodHeroOverlap",new object[]{shape,Vector3.zero,Vector3.zero});
     if(sharkAfter||heroAfter)throw new Exception("Residual joint overlap "+i+": shark="+sharkAfter+" hero="+heroAfter);
     maximum=Mathf.Max(maximum,offset.magnitude);if(maximum>150)throw new Exception("World-origin dependent displacement "+maximum);
    }
    rows.Add(new{i,shark,body,travel=offset.magnitude});
   }
   if(overlaps<12||joint<4)throw new Exception("Insufficient native joint overlap cases: "+overlaps+" simultaneous="+joint);
   var result=new{passed=true,cases=rows.Count,overlaps,joint,maximum,rows};File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/animation-performance-20261019/joint-native-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally{root.position=originalRoot;UnityEngine.Object.DestroyImmediate(go);Physics.SyncTransforms();}
 }
}
