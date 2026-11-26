using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Actual input, normal Animator evaluation and final SRP camera frames.
public static class ReviewJumpTransitionsV276
{
    static Keyboard keyboard;
    static Mouse mouse;
    static bool left, right, recording; static readonly List<InputDevice> physical=new List<InputDevice>();
    static Key[] keys = Array.Empty<Key>();
    static GameObject player;
    static SahurAttack attack;
    static ThirdPersonPlayerController motor;
    static PlayerStamina stamina;
    static Animator animator;
    static Transform[] bones;
    static readonly HumanBodyBones[] BoneIds = { HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.LeftUpperArm,
        HumanBodyBones.RightUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftHand,
        HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot };
    static Camera camera;
    static RenderTexture rt;
    static Texture2D texture;
    static string folder, section;
    static Task pending;
    static int lastFrame;
    static int hz; static bool candidate,overrideCandidate; static AnimatorOverrideController previewOverride;
    static readonly List<CombatHitEvent> hits = new List<CombatHitEvent>();
    static void Hit(CombatHitEvent hit) { if(hit.attacker==player) hits.Add(hit); }
    static readonly List<object> frames = new List<object>(), checks = new List<object>();
    static Vector3 origin;
    static void Input()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); mouse.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left, left).WithButton(MouseButton.Right, right));
    }
    static async Task Wait(float seconds)
    {
        float end = Time.time + seconds; var timeout = DateTime.UtcNow.AddSeconds(45);
        while (Time.time < end) { if(!inputCleanupArmed||!Application.isPlaying)throw new OperationCanceledException("Input fixture stopped"); if (DateTime.UtcNow > timeout) throw new Exception("Stalled: " + section); await Task.Delay(5); }
    }
    static async Task Until(Func<bool> condition, float seconds = 5)
    {
        float end = Time.time + seconds;
        while (!condition()) { if (Time.time > end) throw new Exception("State timeout: " + section); await Wait(.001f); }
    }
    static float[] V(Vector3 value) => new[] { value.x, value.y, value.z };
    static float[] Q(Quaternion value) => new[] { value.x, value.y, value.z, value.w };
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks.Add(text); }
    public static object Start(string take, int simulationHz=30)
    {
        if (!Application.isPlaying || SessionState.GetBool("CombatCombinationTests", false)) throw new Exception("Ready Play Mode required");
        folder = ".codex/jump-transitions-v276/" + take;
        hz=simulationHz; candidate=take.Contains("after")||take.Contains("final"); overrideCandidate=take.Contains("after");
        if (Directory.Exists(folder)) throw new Exception("Preserve prior takes");
        Directory.CreateDirectory(folder + "/frames");
        SessionState.SetBool("CombatCombinationTests", true); EditorApplication.isPaused = false;
        pending = Run(); return new { started = true, folder };
    }
    static async Task Reset(string next)
    {
        recording = false; left = right = false; keys = Array.Empty<Key>(); await Wait(.12f);
        attack.SuspendForSwimming(); player.GetComponent<CombatLocalHitStop>()?.Release(); animator.speed = 1;
        motor.EndAttackFacing(true); motor.RestoreSavedPose(origin, Quaternion.identity); stamina.currentStamina = 100;
        await Wait(.5f); section = next; recording = true;
    }
    static object WeaponPoints(){
 var c=attack.stickHitbox as CapsuleCollider;if(!c)return null;
 Vector3 axis=c.direction==0?Vector3.right:c.direction==1?Vector3.up:Vector3.forward;
 float half=Mathf.Max(0,c.height*.5f-c.radius);
 return new{a=V(player.transform.InverseTransformPoint(c.transform.TransformPoint(c.center+axis*half))),b=V(player.transform.InverseTransformPoint(c.transform.TransformPoint(c.center-axis*half)))};
 }
 static void Frame(ScriptableRenderContext context, Camera rendered)
    {
        if (!recording || rendered != camera || Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        var state = animator.GetCurrentAnimatorStateInfo(0); var next = animator.GetNextAnimatorStateInfo(0);
        int layer = animator.GetLayerIndex("Twin Blade Charge");
        var geometry=section.EndsWith("early chop") && attack.CurrentComboStage<0 && attack.CurrentAttackPhase>0 ? JumpWeaponGeometryV276.Sample(player,animator,attack.stickHitbox) : null; frames.Add(new { geometry, frame = Time.frameCount, time = Time.time, section, phase = attack.CurrentAttackPhase, recovering=attack.IsJumpSlashRecovering, moving=motor.RequestedMoveDirection.sqrMagnitude>.01f,
            stage = attack.CurrentComboStage, charging = attack.IsCharging, heavy = attack.IsHeavyAttackActive,
            spin = attack.IsSpinning, guard = attack.IsGuarding, stop = CombatLocalHitStop.Active(player),
            state = state.fullPathHash, statePhase = state.normalizedTime, next = next.fullPathHash,
            transition = animator.IsInTransition(0) ? animator.GetAnimatorTransitionInfo(0).normalizedTime : -1,
            chargeWeight = layer >= 0 ? animator.GetLayerWeight(layer) : 0, chargePose = attack.ChargePose01,
            worldRotations=bones.Select(b=>Q(b.rotation)).ToArray(), root = V(player.transform.position), rotations = bones.Select(b => Q(b.localRotation)).ToArray(),
            positions = bones.Select(b => V(player.transform.InverseTransformPoint(b.position))).ToArray(), weapon=WeaponPoints(),
            airborne=motor.CanUseAirAttack, grounded=motor.CanUseGroundAttack, usesBlades=attack.UsesTwinBlades, hitCount=hits.Count, bladeActive = attack.stickHitbox && attack.stickHitbox.enabled });
        var active = RenderTexture.active;
        try { RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            File.WriteAllBytes(folder + "/frames/" + frames.Count.ToString("D5") + ".png", texture.EncodeToPNG()); }
        finally { RenderTexture.active = active; }
    }
    public static void PositionCamera()
    {
        if (!recording || !camera || !player) return;
        Vector3 center = player.GetComponent<CharacterController>().bounds.center + Vector3.up * .2f;
        camera.transform.position = center + new Vector3(7, 2, 7); camera.transform.LookAt(center); camera.fieldOfView = 46;
    }
    // Idempotent input cleanup must run even when Play stops, a domain reload starts,
    // or report serialization fails. The tests never own the user's real devices.
    static bool inputCleanupArmed;
    static Keyboard priorKeyboard;
    static Mouse priorMouse;
    static void ArmInputCleanup()
    {
        if (inputCleanupArmed) throw new InvalidOperationException("Input fixture already active");
        priorKeyboard=Keyboard.current;priorMouse=Mouse.current;inputCleanupArmed=true;
        AssemblyReloadEvents.beforeAssemblyReload+=RestoreTestInput;
        EditorApplication.playModeStateChanged+=InputFixturePlayModeChanged;
        EditorApplication.quitting+=RestoreTestInput;
    }
    static void InputFixturePlayModeChanged(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.ExitingPlayMode||state==PlayModeStateChange.EnteredEditMode)RestoreTestInput();
    }
    static void RestoreTestInput()
    {
        recording=false;InputSystem.onBeforeUpdate-=Input;
        RenderPipelineManager.endCameraRendering-=Frame;CombatHitFeedback.HitConfirmed-=Hit;
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
        if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        keyboard=null;mouse=null;
        foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);
        physical.Clear();
        if(priorKeyboard!=null&&priorKeyboard.added)priorKeyboard.MakeCurrent();
        if(priorMouse!=null&&priorMouse.added)priorMouse.MakeCurrent();
        priorKeyboard=null;priorMouse=null;inputCleanupArmed=false;
        SessionState.SetBool("CombatCombinationTests",false);
        AssemblyReloadEvents.beforeAssemblyReload-=RestoreTestInput;
        EditorApplication.playModeStateChanged-=InputFixturePlayModeChanged;
        EditorApplication.quitting-=RestoreTestInput;
    }
    static async Task Run()
    {
        float capture = Time.captureDeltaTime; var oldKeyboard = Keyboard.current; var oldMouse = Mouse.current;
        bool passed = false; hits.Clear(); frames.Clear(); checks.Clear(); lastFrame = -1;
        try
        {
            ArmInputCleanup();
            CombatFeelPreview.Spawn(); Time.captureDeltaTime = 1f / hz;
            player = CombatFeelPreview.Player; attack = player.GetComponent<SahurAttack>();
            motor = player.GetComponent<ThirdPersonPlayerController>(); stamina = player.GetComponent<PlayerStamina>();
            physical.Clear(); foreach(var d in InputSystem.devices.ToArray())if((d is Keyboard||d is Mouse)&&d.enabled){physical.Add(d);InputSystem.DisableDevice(d);} keyboard = InputSystem.AddDevice<Keyboard>("Sahur jump review keyboard"); mouse = InputSystem.AddDevice<Mouse>("Sahur jump review mouse");
            left = right = false; keys = Array.Empty<Key>(); InputSystem.onBeforeUpdate += Input;
            var input = player.GetComponent<PlayerInput>(); InputUser.PerformPairingWithDevice(keyboard, input.user);
            InputUser.PerformPairingWithDevice(mouse, input.user); input.ActivateInput();
            await Wait(.7f); var loadout = player.GetComponent<SahurWeaponLoadout>();
            loadout.Restore(true, SahurWeaponLoadout.Weapon.M4A1, true); await Wait(.3f);
            Check(loadout.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades), "Actual rifle to blade equip");
            await Wait(.4f); animator = attack.animator; origin = player.transform.position;
            if(overrideCandidate){previewOverride=new AnimatorOverrideController(animator.runtimeAnimatorController);
                previewOverride["ClubJumpChopV269"]=Resources.Load<AnimationClip>("Combat/Combinations/ClubJumpChopV276");
                previewOverride["BladeJumpChopV270"]=Resources.Load<AnimationClip>("Combat/Combinations/BladeJumpChopV276");
                animator.runtimeAnimatorController=previewOverride;}
            bones = BoneIds.Select(id => animator.GetBoneTransform(id)).ToArray();
            CombatFeelPreview.Target.transform.position = new Vector3(2000, 500, 2040); Physics.SyncTransforms();
            camera = CombatFeelPreview.Camera; foreach(var renderer in CombatFeelPreview.Target.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            rt = new RenderTexture(960, 540, 24) { name = "Temporary blade review capture" }; rt.Create();
            texture = new Texture2D(960, 540, TextureFormat.RGB24, false); camera.targetTexture = rt;
            player.AddComponent<JumpReviewCameraV276>(); RenderPipelineManager.endCameraRendering += Frame;
            CombatHitFeedback.HitConfirmed += Hit;
            foreach(var weapon in new[]{SahurWeaponLoadout.Weapon.Stick,SahurWeaponLoadout.Weapon.CapriTwinBlades})
            {
                Check(loadout.TryEquip(weapon), "Equip " + weapon);
                string label=weapon.ToString();
                await Reset(label+" early chop");
                CombatFeelPreview.Target.transform.position=origin+new Vector3(.5f,0,2.2f);Physics.SyncTransforms();
                int hitBefore=hits.Count;
                keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};await Wait(.12f);keys=Array.Empty<Key>();
                Check(motor.CanUseAirAttack && !motor.CanUseGroundAttack,label+" airborne classification");
                left=true;await Wait(.07f);left=false;
                await Until(()=>attack.IsCombatMotionActive);
                Check(attack.CurrentComboStage<0 && !attack.UsesAnimationRootMotion,label+" chop owns pose, movement owns jump");
                await Until(()=>attack.CurrentAttackPhase>=.36f);
                string clip=animator.GetCurrentAnimatorClipInfo(0).OrderByDescending(c=>c.weight).First().clip.name;
                Check(clip.Contains(weapon==SahurWeaponLoadout.Weapon.Stick?"ClubJumpChop":"BladeJumpChop"),label+" correct full authored action");
                var liveClip=animator.GetCurrentAnimatorClipInfo(0).OrderByDescending(c=>c.weight).First().clip;
                Check(AssetDatabase.GetAssetPath(liveClip).EndsWith((candidate?(weapon==SahurWeaponLoadout.Weapon.Stick?"ClubJumpChopV276.anim":"BladeJumpChopV276.anim"):(weapon==SahurWeaponLoadout.Weapon.Stick?"ClubJumpChopV269.anim":"BladeJumpChopV270.anim"))),label+" diagonal revision is the one actually rendered");
                await Until(()=>!attack.IsCombatMotionActive);await Wait(.5f);
                var actual=hits.Skip(hitBefore).Where(h=>h.kind==CombatHitKind.JumpSlash).ToArray();
                Check(actual.Length==1,label+" actual swept weapon confirms one hit per victim");
                Check(!attack.stickHitbox.enabled && animator.speed==1,label+" recovery clears collision and local stop");
                Check(motor.CanUseGroundAttack && Time.timeScale==1,label+" lands with global clock unchanged");
                await Reset(label+" simultaneous jump and attack");
                CombatFeelPreview.Target.transform.position=origin+Vector3.forward*35;
                keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};left=true;
                await Wait(.1f);keys=Array.Empty<Key>();left=false;
                await Until(()=>attack.IsCombatMotionActive);
                Check(attack.CurrentComboStage<0,label+" Space plus LMB is a chop rather than ground combo");
                await Until(()=>!attack.IsCombatMotionActive);await Wait(.5f);
                await Reset(label+" falling chop miss");hitBefore=hits.Count;
                keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Jump)};await Wait(.1f);keys=Array.Empty<Key>();await Wait(.3f);
                left=true;await Wait(.07f);left=false;await Until(()=>attack.IsCombatMotionActive);
                Check(attack.CurrentComboStage<0,label+" apex/falling press selects chop");
                await Until(()=>!attack.IsCombatMotionActive);await Wait(.5f);
                Check(hits.Count==hitBefore,label+" miss does not invent landing radius damage");
                await Reset(label+" moving recovery");CombatFeelPreview.Target.transform.position=origin+Vector3.forward*100;
                var forward=GameInputSettings.Get(GameInputSettings.Action.Forward);keys=new[]{forward};await Wait(.35f);
                keys=new[]{forward,GameInputSettings.Get(GameInputSettings.Action.Jump)};await Wait(.12f);keys=new[]{forward};left=true;await Wait(.07f);left=false;
                await Until(()=>attack.IsJumpSlashRecovering);await Wait(.04f);
                int locomotion=Animator.StringToHash("Base Layer.Locomotion");
                Check(motor.CanUseGroundAttack && (animator.GetNextAnimatorStateInfo(0).fullPathHash==locomotion||animator.GetCurrentAnimatorStateInfo(0).fullPathHash==locomotion),label+" moving grounded recovery blends into locomotion");
                Vector3 startMove=player.transform.position;await Wait(.12f);
                Check(player.transform.position.z>startMove.z+.05f,label+" movement returns smoothly during recovery");
                await Until(()=>!attack.IsCombatMotionActive);keys=Array.Empty<Key>();await Wait(.35f);
                await Reset(label+" high fall recovery");motor.RestoreSavedPose(origin+Vector3.up*40,Quaternion.identity);await Wait(.1f);
                left=true;await Wait(.07f);left=false;await Until(()=>attack.IsJumpSlashRecovering);await Wait(.04f);
                int air=Animator.StringToHash("Base Layer.Jump Loop");
                Check(!motor.CanUseGroundAttack && (animator.GetNextAnimatorStateInfo(0).fullPathHash==air||animator.GetCurrentAnimatorStateInfo(0).fullPathHash==air),label+" airborne recovery blends into falling rather than ground idle");
                await Until(()=>!attack.IsCombatMotionActive);await Until(()=>motor.CanUseGroundAttack,4);await Wait(.4f);

            }
            Check(loadout.TryEquip(SahurWeaponLoadout.Weapon.Stick),"Restore club for single-hand spin");
            await Reset("Single-hand club spin");left=right=true;
            await Until(()=>attack.IsSpinning);await Wait(.95f);
            var spinClip=animator.GetCurrentAnimatorClipInfo(0).OrderByDescending(c=>c.weight).First().clip.name;
            Check(spinClip.Contains("ClubSpinOneHand"),"Native club spin uses new single-hand full clip");
            Vector3 hand=animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            var collider=attack.stickHitbox;
            Vector3 closest=collider.ClosestPoint(hand);
            Check(Vector3.Distance(hand,closest)>.2f,"Free left hand stays clear of club shaft");
            left=right=false;await Wait(.7f);Check(!attack.IsSpinning && !attack.stickHitbox.enabled,"Spin release fully clears damage");
            passed = true;
        }
        catch (Exception error) { File.WriteAllText(folder + "/error.txt", error.ToString()); }
        finally
        {
            RestoreTestInput(); // Restore hardware before any fallible report/file operation.
            recording = false; RenderPipelineManager.endCameraRendering -= Frame; InputSystem.onBeforeUpdate -= Input; CombatHitFeedback.HitConfirmed -= Hit;
            File.WriteAllText(folder + "/review.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                passed, simulationHz = hz, capture = "Native endCameraRendering", bones = BoneIds.Select(x => x.ToString()).ToArray(), checks, frames }, Newtonsoft.Json.Formatting.Indented));
            if (camera) camera.targetTexture = null;
            if (rt) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            if (texture) UnityEngine.Object.DestroyImmediate(texture);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (mouse != null) InputSystem.RemoveDevice(mouse);
            keyboard = null; mouse = null;foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);physical.Clear();
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            JumpWeaponGeometryV276.Clear(); CombatFeelPreview.Clear(); if(previewOverride)UnityEngine.Object.DestroyImmediate(previewOverride); Time.captureDeltaTime = capture; SessionState.SetBool("CombatCombinationTests", false); EditorApplication.isPlaying=false;
        }
    }
}
[DefaultExecutionOrder(32000)] public sealed class JumpReviewCameraV276 : MonoBehaviour
{
    void LateUpdate() => ReviewJumpTransitionsV276.PositionCamera();
}


// exact deformed mesh rays never participate in gameplay.
public static class JumpWeaponGeometryV276 {
 static int[] triangles; static BoneWeight[] weights; static Scene scene; static SkinnedMeshRenderer skin; static Mesh mesh; static MeshCollider probe;
 public static object Sample(GameObject player,Animator animator,Collider primary){
  if(!skin){skin=player.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(x=>x.sharedMesh.vertexCount).First();
   triangles=skin.sharedMesh.triangles;weights=skin.sharedMesh.boneWeights; mesh=new Mesh();scene=SceneManager.CreateScene("Temporary jump mesh probe",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
   var go=new GameObject("Temporary jump mesh probe");SceneManager.MoveGameObjectToScene(go,scene);probe=go.AddComponent<MeshCollider>();}
  skin.BakeMesh(mesh,true);probe.sharedMesh=null;probe.sharedMesh=mesh;
  probe.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);probe.transform.localScale=skin.transform.parent?skin.transform.parent.lossyScale:Vector3.one;
  scene.GetPhysicsScene().Simulate(.0001f);
  var results=new List<object>();
  var weapons=player.GetComponentsInChildren<CapsuleCollider>(true).Where(c=>c==primary||(primary&&primary.name=="Right blade"&&c.name=="Left blade")).Distinct().ToArray();
  foreach(var c in weapons){
   Vector3 axis=c.direction==0?Vector3.right:c.direction==1?Vector3.up:Vector3.forward;
   float half=Mathf.Max(0,c.height*.5f-c.radius);Vector3 a=c.transform.TransformPoint(c.center+axis*half),b=c.transform.TransformPoint(c.center-axis*half);
   var rh=animator.GetBoneTransform(HumanBodyBones.RightHand);var lh=animator.GetBoneTransform(HumanBodyBones.LeftHand);
   float da=Mathf.Min(Vector3.Distance(a,rh.position),Vector3.Distance(a,lh.position));float db=Mathf.Min(Vector3.Distance(b,rh.position),Vector3.Distance(b,lh.position));
   if(db<da){var temp=a;a=b;b=temp;}Vector3 direction=(b-a).normalized;
   a+=direction*.20f;float length=Vector3.Distance(a,b);Vector3 u=Vector3.Cross(direction,Vector3.up).normalized;if(u.sqrMagnitude<.1f)u=Vector3.right;Vector3 v=Vector3.Cross(direction,u);
   float radius=c.radius*Mathf.Max(c.transform.lossyScale.x,c.transform.lossyScale.y,c.transform.lossyScale.z);int crossings=0;var hits=new List<float[]>();var parts=new List<string>();
   for(int i=-1;i<8;i++){
    Vector3 offset=i<0?Vector3.zero:(u*Mathf.Cos(i*Mathf.PI/4)+v*Mathf.Sin(i*Mathf.PI/4))*radius;
    if(probe.Raycast(new Ray(a+offset,direction),out var hit,length)||probe.Raycast(new Ray(b+offset,-direction),out hit,length)){crossings++;var local=player.transform.InverseTransformPoint(hit.point);hits.Add(new[]{local.x,local.y,local.z}); for(int j=0;j<3;j++){int vi=triangles[hit.triangleIndex*3+j];var bw=weights[vi];float[] ww={bw.weight0,bw.weight1,bw.weight2,bw.weight3};int[] bi={bw.boneIndex0,bw.boneIndex1,bw.boneIndex2,bw.boneIndex3};int k=System.Array.IndexOf(ww,ww.Max());parts.Add(skin.bones[bi[k]].name);}}
   }
   var exactParts=new List<string>();int exactTests=0;
   if(parts.Any(part=>!part.Contains("Hand"))){
    var filter=c.GetComponent<MeshFilter>();
    if(filter&&filter.sharedMesh){var vertices=filter.sharedMesh.vertices;var edges=filter.sharedMesh.triangles;var visited=new HashSet<ulong>();
     for(int ti=0;ti<edges.Length;ti+=3)for(int ei=0;ei<3;ei++){
      int va=edges[ti+ei],vb=edges[ti+(ei+1)%3];ulong key=((ulong)Mathf.Min(va,vb)<<32)|(uint)Mathf.Max(va,vb);if(!visited.Add(key))continue;
      var aa=filter.transform.TransformPoint(vertices[va]);var bb=filter.transform.TransformPoint(vertices[vb]);var delta=bb-aa;exactTests++;
      if(probe.Raycast(new Ray(aa,delta.normalized),out var contact,delta.magnitude)||probe.Raycast(new Ray(bb,-delta.normalized),out contact,delta.magnitude)){
       for(int j=0;j<3;j++){var bw=weights[triangles[contact.triangleIndex*3+j]];float[] ww={bw.weight0,bw.weight1,bw.weight2,bw.weight3};int[] bi={bw.boneIndex0,bw.boneIndex1,bw.boneIndex2,bw.boneIndex3};string part=skin.bones[bi[System.Array.IndexOf(ww,ww.Max())]].name;if(!part.Contains("Hand"))exactParts.Add(part);}
      }
     }
    }
   }
   results.Add(new{weapon=c.name,rays=9,crossings,points=hits,parts,exactTests,exactParts});
  }
  return results;
 }
 public static void Clear(){if(probe)UnityEngine.Object.DestroyImmediate(probe.gameObject);if(mesh)UnityEngine.Object.DestroyImmediate(mesh);if(scene.IsValid())SceneManager.UnloadSceneAsync(scene);probe=null;mesh=null;skin=null;}
}
