using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifySwimJumpV178
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    public static object Start(){_ = Observe(); return new {started=true};}
    static async Task Observe(){try{await Verify();}catch(Exception ex){Directory.CreateDirectory(".codex/opening-optimization-20261012/controls-v178");File.WriteAllText(".codex/opening-optimization-20261012/controls-v178/error.txt",ex.ToString());}}
    public static async Task<object> Verify()
    {
        Require(Application.isPlaying,"Play Mode required");
        var originalInputs=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(b=>b.enabled && (b is ThirdPersonPlayerController || b is SahurAttack || b is SahurBoomerang || b is PlayerInput)).ToArray();
        foreach(var input in originalInputs)input.enabled=false;
        var previousKeyboard=Keyboard.current;var previousMouse=Mouse.current;var cursor=Cursor.lockState;
        float capture=Time.captureDeltaTime;
        var keyboard=InputSystem.AddDevice<Keyboard>("Swim jump regression");
        var mouse=InputSystem.AddDevice<Mouse>("Swim jump regression mouse");
        var holder=new GameObject("Temporary isolated swim jump regression");holder.SetActive(false);
        holder.transform.position=new Vector3(2600,500,2600);
        var floor=new GameObject("Temporary jump regression floor");floor.transform.position=holder.transform.position-Vector3.up*.5f;
        var floorCollider=floor.AddComponent<BoxCollider>();floorCollider.size=new Vector3(80,1,80);
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
        actor.AddComponent<GameSaveExcluded>();var player=actor.GetComponent<ThirdPersonPlayerController>();player.waterSplashes=false;
        foreach(var input in actor.GetComponentsInChildren<PlayerInput>(true))input.enabled=false;
        var update=typeof(ThirdPersonPlayerController).GetMethod("Update",Flags);
        var swim=typeof(ThirdPersonPlayerController).GetMethod("UpdateSwimming",Flags);
        var vertical=typeof(ThirdPersonPlayerController).GetField("verticalSpeed",Flags);
        var animatorField=typeof(ThirdPersonPlayerController).GetField("motionState",Flags);
        var samples=new System.Collections.Generic.List<object>();
        void Tick(params Key[] held)
        {
            keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(held));InputSystem.Update();
            update.Invoke(player,null);player.CharacterAnimator.Update(Time.deltaTime);
        }
        void Reset(float gap=.02f)
        {
            floorCollider.enabled=true;
            player.RestoreSavedPose(holder.transform.position+Vector3.up*(gap-player.LowestFootWorldOffset),Quaternion.identity);
            actor.GetComponent<PlayerStamina>().currentStamina=100;
            Physics.SyncTransforms();Tick();Tick();
        }
        try
        {
            holder.SetActive(true);await Task.Delay(250);
            foreach(var behaviour in actor.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
            player.CharacterAnimator.enabled=false;
            Key jump=GameInputSettings.Get(GameInputSettings.Action.Jump);
            foreach(float dt in new[]{1f/60,1f/20,1f/10})
            {
                Time.captureDeltaTime=dt;await Task.Delay(100);
                Require(Mathf.Abs(Time.deltaTime-dt)<.002f,"Capture timestep not applied");
                Reset();Require(player.CanUseGroundAttack,"Native capsule did not ground");
                float start=actor.transform.position.y,highest=start;int takeoffs=0;float before=player.VerticalSpeed;
                for(int i=0;i<Mathf.CeilToInt(1.5f/dt);i++)
                {
                    Tick(i==0 ? new[]{jump}:Array.Empty<Key>());
                    highest=Mathf.Max(highest,actor.transform.position.y);
                    if(player.VerticalSpeed>1 && before<=0)takeoffs++;before=player.VerticalSpeed;
                }
                float height=highest-start;
                Require(Mathf.Abs(height-player.jumpHeight)<.08f,"Jump height changed with frame rate: "+height+" at "+dt);
                Require(takeoffs==1 && player.CanUseGroundAttack,"Jump did not land once: "+takeoffs);
                samples.Add(new{dt,height,targetHeight=player.jumpHeight,takeoffs,landed=true});
            }
            Time.captureDeltaTime=1f/60;await Task.Delay(100);Reset();
            // Coyote jump after stepping away from an actual collider.
            floorCollider.enabled=false;Physics.SyncTransforms();Tick();Tick();Tick();Tick(jump);
            float coyoteVelocity=player.VerticalSpeed;Require(coyoteVelocity>1,"Coyote jump failed");
            Reset();player.RestoreSavedPose(holder.transform.position+Vector3.up*(.34f-player.LowestFootWorldOffset),Quaternion.identity);
            vertical.SetValue(player,-4f);Physics.SyncTransforms();Tick(jump);
            bool buffered=false;for(int i=0;i<10;i++){Tick();buffered|=player.VerticalSpeed>1;}
            Require(buffered,"Buffered landing jump failed");
            Reset();player.LeaveMovingPlatform(new Vector3(4,0,2),true);Tick(jump);
            Require(player.VerticalSpeed>1,"Moving board handoff did not jump");
            Require(Vector3.ProjectOnPlane(player.WorldVelocity,Vector3.up).magnitude>4,"Moving board momentum was lost");
            float inheritedSpeed=Vector3.ProjectOnPlane(player.WorldVelocity,Vector3.up).magnitude;
            Reset();
            var ceiling=new GameObject("Temporary regression ceiling");ceiling.transform.SetParent(holder.transform);
            var capsule=actor.GetComponent<CharacterController>();
            ceiling.transform.position=new Vector3(actor.transform.position.x,capsule.bounds.max.y+.35f,actor.transform.position.z);
            ceiling.AddComponent<BoxCollider>().size=new Vector3(10,.2f,10);Physics.SyncTransforms();
            Tick(jump);bool stoppedByCeiling=(capsule.collisionFlags&CollisionFlags.Above)!=0 && player.VerticalSpeed<=0;
            var ceilingTrace=new System.Collections.Generic.List<object>();
            for(int i=0;i<30;i++)
            {
                float previous=player.VerticalSpeed;Tick();
                if((capsule.collisionFlags&CollisionFlags.Above)!=0 && player.VerticalSpeed<=0)stoppedByCeiling=true;
                ceilingTrace.Add(new{i,previous,current=player.VerticalSpeed,flags=capsule.collisionFlags.ToString(),top=capsule.bounds.max.y,ceiling=ceiling.transform.position.y});
            }
            Require(stoppedByCeiling,"Ceiling collision retained upward momentum: "+Newtonsoft.Json.JsonConvert.SerializeObject(ceilingTrace));UnityEngine.Object.DestroyImmediate(ceiling);
            var animator=player.CharacterAnimator;
            Reset();animator.Play("Locomotion",0,0);animator.Update(0);
            Vector3 leftSole=default,rightSole=default;SahurSupportPose.MeasureSoles(animator,ref leftSole,ref rightSole);
            var leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
            float restingSole=Mathf.Min(leftFoot.TransformPoint(leftSole).y,rightFoot.TransformPoint(rightSole).y);
            var landingSupport=new System.Collections.Generic.List<object>();
            for(int frame=0;frame<=25;frame++)
            {
                animator.Play("Land",0,frame/25f);animator.Update(0);
                float gap=Mathf.Min(leftFoot.TransformPoint(leftSole).y,rightFoot.TransformPoint(rightSole).y)-restingSole;
                landingSupport.Add(new{frame,gap});
            }
            var hips=animator.GetBoneTransform(HumanBodyBones.Hips);var head=animator.GetBoneTransform(HumanBodyBones.Head);
            var left=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);var right=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var facing=new System.Collections.Generic.List<object>();
            foreach(float heading in new[]{0f,90f,180f,270f})foreach(float fast in new[]{0f,1f})foreach(float phase in new[]{0f,.25f,.5f,.75f})
            {
                actor.transform.rotation=Quaternion.Euler(0,heading,0);animator.SetFloat("SwimAmount",1);animator.SetFloat("SwimFastBlend",fast);
                animator.Play("Swimming",0,phase);animator.Update(0);
                float dot=Vector3.Dot(Vector3.ProjectOnPlane(head.position-hips.position,Vector3.up).normalized,actor.transform.forward);
                Require(dot>.95f,"Swim pose faces away from actual heading: "+dot);
                facing.Add(new{heading,fast,phase,dot});
            }
            var swimming=new System.Collections.Generic.List<object>();
            foreach(bool fast in new[]{false,true})foreach(var direction in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left})
            {
                player.RestoreSavedPose(new Vector3(2600,-50,2600),Quaternion.identity);
                animator.SetFloat("SwimAmount",1);animator.SetFloat("SwimFastBlend",fast?1:0);animator.Play("Swimming",0,0);animator.Update(0);
                animatorField.SetValue(player,5);Vector3 start=actor.transform.position;
                for(int i=0;i<60;i++)
                {
                    swim.Invoke(player,new object[]{Vector3.forward,direction,fast,false});animator.Update(Time.deltaTime);
                }
                Vector3 travelled=Vector3.ProjectOnPlane(actor.transform.position-start,Vector3.up);
                float movementDot=Vector3.Dot(travelled.normalized,direction);
                float faceDot=Vector3.Dot(Vector3.ProjectOnPlane(head.position-hips.position,Vector3.up).normalized,direction);
                Require(movementDot>.99f && faceDot>.95f && travelled.magnitude>4,"Swim movement and pose disagree");
                Require(player.FastSwimming==fast,"Fast swimming did not select requested stroke");
                swimming.Add(new{fast,direction=direction.ToString(),distance=travelled.magnitude,movementDot,faceDot});
            }
            var result=new{jumpSamples=samples,coyoteVelocity,bufferedJump=buffered,inheritedSpeed,ceilingStopsAscent=stoppedByCeiling,landingSupport,swimFacing=facing,swimming};
            Directory.CreateDirectory(".codex/opening-optimization-20261012/controls-v178");File.WriteAllText(".codex/opening-optimization-20261012/controls-v178/runtime-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            Time.captureDeltaTime=capture;UnityEngine.Object.DestroyImmediate(holder);UnityEngine.Object.DestroyImmediate(floor);
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            previousKeyboard?.MakeCurrent();previousMouse?.MakeCurrent();Cursor.lockState=cursor;
            foreach(var input in originalInputs)if(input!=null)input.enabled=true;
        }
    }
}
