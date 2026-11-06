using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Continuous grab, carry and fall into the existing island awakening.</summary>
    [DisallowMultipleComponent,DefaultExecutionOrder(19000)]
    public sealed class BombardinoAbduction : MonoBehaviour
    {
        const string Destination="Assets/Scenes/Second Island/Second Island.unity";
        public static bool Playing {get;private set;}
        public string Phase {get;private set;}="approach";
        public float GripError=>pose!=null?pose.GripError:0;
        BombardinoBoss boss;ThirdPersonPlayerController player;
        CharacterController capsule;SahurAttack attack;SahurAbductionPose pose;
        Camera view;Transform bellyFrame;Image cover;GameObject fadeRoot;
        Vector3 bellyLocal,cameraVelocity,home,landing,facing,carryFrom;
        Quaternion homeRotation,visualRest;
        float bodyHeight,carryAge,gripWeight,fallPhase;
        BombardinoCaptureClaws claws;
        bool carried,falling,sceneReady,restored,attackEnabled,playerEnabled,capsuleEnabled,oldControl,oldMovement;
        Scene destination;SahurWeaponLoadout.Weapon selectedWeapon;
        bool Armed=>Time.timeScale>0&&!PauseSettingsMenu.IsOpen;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>Playing=false;
        public static bool TryBegin(BombardinoBoss source)
        {
            if(!Application.isPlaying||Playing||!source||!source.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/"))return false;
            var body=source.target?source.target.GetComponent<ThirdPersonPlayerController>():null;
            if(!body||!body.isActiveAndEnabled||!Application.CanStreamedLevelBeLoaded(Destination))return false;
            Playing=true;var run=source.gameObject.AddComponent<BombardinoAbduction>();run.boss=source;run.player=body;return true;
        }
        IEnumerator Start()
        {
            home=player.transform.position;homeRotation=player.transform.rotation;
            facing=Vector3.ProjectOnPlane(boss.transform.forward,Vector3.up).normalized;if(facing.sqrMagnitude<.01f)facing=player.transform.forward;
            visualRest=boss.visual?boss.visual.localRotation:Quaternion.identity;
            bellyFrame=boss.visual?boss.visual:boss.transform;
            claws=new BombardinoCaptureClaws(bellyFrame);bellyLocal=claws.GripLocal;
            LockPlayer();view=Camera.main;MakeCover();CinematicLetterbox.Set(this,true);
            boss.Explosion(boss.transform.position,3.5f);
            var face=Quaternion.LookRotation(facing);
            pose.Sample(0,0);
            yield return FlyTo(RootForBelly(pose.Shoulders+Vector3.up*(pose.ArmReach*.8f),face),face,1.65f,-9);
            Phase="grasp";carryFrom=player.transform.position;carried=true;carryAge=0;
            yield return HoldTime(.55f);
            Phase="climb";
            yield return FlyTo(boss.transform.position+facing*90+Vector3.up*48,face,3.4f,12);
            Phase="transit";yield return Fade(1,.32f);
            DontDestroyOnLoad(boss.gameObject);player.transform.SetParent(boss.transform,true);
            SceneManager.sceneLoaded+=SceneArrived;
            if(!SceneLoadingScreen.Load(Destination)){Abort();yield break;}
            float deadline=Time.realtimeSinceStartup+90;
            while(!sceneReady&&Time.realtimeSinceStartup<deadline)yield return null;
            if(!sceneReady){Abort();yield break;}
            while(SceneLoadingScreen.IsLoading)yield return null;
            yield return null;if(!player){Abort();yield break;}
            FindLanding();face=Quaternion.LookRotation(facing);view=Camera.main;cameraVelocity=Vector3.zero;
            Phase="island-pass";carryAge=1;gripWeight=1;
            Vector3 release=landing+Vector3.up*(bodyHeight*5.5f)-facing*bodyHeight*2.4f;
            boss.transform.SetPositionAndRotation(RootForBelly(release,face),face);
            Bank(-5);ApplyCarry();FrameCamera(true);
            yield return Fade(0,.3f);
            yield return FlyTo(boss.transform.position+facing*bodyHeight*2,face,.8f,-5);
            Phase="release";carried=false;falling=true;player.transform.SetParent(null,true);
            Vector3 start=player.transform.position,course=boss.transform.position;Quaternion from=player.transform.rotation;
            const float seconds=1.65f;float elapsed=0;
            Vector3 velocity=facing*bodyHeight*1.35f+Vector3.up*bodyHeight*.25f;
            Vector3 acceleration=(landing-start-velocity*seconds)*(2/(seconds*seconds));
            while(elapsed<seconds)
            {
                if(Armed)
                {
                    fallPhase=Mathf.Clamp01(elapsed/seconds);
                    player.transform.SetPositionAndRotation(start+velocity*elapsed+acceleration*(.5f*elapsed*elapsed),
                        Quaternion.Slerp(from,face*Quaternion.Euler(-36,0,14),Mathf.SmoothStep(0,1,fallPhase)));
                    boss.transform.position=course+facing*(elapsed*bodyHeight*7)+Vector3.up*(elapsed*elapsed*bodyHeight*1.7f);Bank(14);
                    elapsed+=Time.deltaTime;
                }
                yield return null;
            }
            player.transform.position=landing;falling=false;Phase="impact";pose.Sample(.82f);SeatImpact();
            BrineVfxWorld.Get()?.Ground(landing,Mathf.Clamp(bodyHeight*.48f,1,3),.7f);
            yield return Fade(1,.12f);yield return HoldTime(.28f);
            Phase="awakening";Finish();
        }
        void LockPlayer()
        {
            player.PrepareRuntime();capsule=player.GetComponent<CharacterController>();attack=player.GetComponent<SahurAttack>();
            playerEnabled=player.enabled;capsuleEnabled=capsule&&capsule.enabled;attackEnabled=attack&&attack.enabled;
            oldControl=player.ExternalControlLock;oldMovement=player.ExternalMovementLock;
            player.GetComponent<CombatLocalHitStop>()?.Release();player.GetComponent<CombatImpactVisuals>()?.ClearTrail();
            if(attack)attack.enabled=false;
            player.Motion.Claim(this,SahurControlState.Cinematic);player.ExternalControlLock=true;player.ExternalMovementLock=true;player.enabled=false;
            if(capsule)capsule.enabled=false;
            bodyHeight=capsule?capsule.height*Mathf.Abs(player.transform.lossyScale.y):4;
            selectedWeapon=player.GetComponent<SahurWeaponLoadout>()?player.GetComponent<SahurWeaponLoadout>().CurrentWeapon:SahurWeaponLoadout.Weapon.Stick;
            var stow=player.GetComponent<SahurSwimmingWeapon>();if(stow){stow.CinematicTraversal=true;stow.CinematicStowWeight=1;}
            player.GetComponent<SahurWeaponLoadout>()?.SetSkyboardStowed(true);
            pose=new SahurAbductionPose(player);
        }
        void UnlockPlayer()
        {
            if(!player)return;pose?.Dispose();pose=null;
            player.Motion?.Release(this);player.ExternalControlLock=oldControl;player.ExternalMovementLock=oldMovement;
            player.enabled=playerEnabled;if(capsule)capsule.enabled=capsuleEnabled;if(attack)attack.enabled=attackEnabled;
            var stow=player.GetComponent<SahurSwimmingWeapon>();if(stow){stow.CinematicTraversal=false;stow.CinematicStowWeight=-1;}
            player.GetComponent<SahurWeaponLoadout>()?.SetSkyboardStowed(false);
        }
        void SceneArrived(Scene scene,LoadSceneMode mode)
        {
            if(scene.path!=Destination)return;SceneManager.sceneLoaded-=SceneArrived;destination=scene;
            ThirdPersonPlayerController resident=null;
            foreach(var root in scene.GetRootGameObjects())foreach(var body in root.GetComponentsInChildren<ThirdPersonPlayerController>(true))if(body!=player)resident=body;
            if(!resident)return;
            var previous=player;var oldInventory=previous.GetComponent<EquipmentInventory>();var inventory=resident.GetComponent<EquipmentInventory>();
            if(oldInventory&&inventory){inventory.ClearForRestore();foreach(var pair in oldInventory.Items)inventory.RestoreStack(pair.Key,pair.Value);}
            var worn=previous.GetComponent<SahurWearableEquipment>();var destinationWorn=resident.GetComponent<SahurWearableEquipment>();
            if(worn&&destinationWorn)foreach(var item in worn.EquippedItems)destinationWorn.Equip(item);
            var oldHealth=previous.GetComponent<PlayerHealth>();var health=resident.GetComponent<PlayerHealth>();if(health&&oldHealth)health.currentHealth=Mathf.Clamp(oldHealth.currentHealth,1,health.maxHealth);
            var oldStamina=previous.GetComponent<PlayerStamina>();var stamina=resident.GetComponent<PlayerStamina>();if(stamina&&oldStamina)stamina.currentStamina=Mathf.Clamp(oldStamina.currentStamina,0,stamina.maxStamina);
            var glasses=previous.GetComponent<WeaknessGlasses>();if(glasses)resident.GetComponent<WeaknessGlasses>()?.Restore(glasses.Owned,glasses.Equipped,glasses.ActiveRemaining,glasses.CooldownRemaining);
            var savedWeapon=selectedWeapon;
            var loadout=previous.GetComponent<SahurWeaponLoadout>();if(loadout)resident.GetComponent<SahurWeaponLoadout>()?.Restore(loadout.HasTwinBlades,savedWeapon,loadout.HasM4A1);
            var rifle=previous.GetComponent<SahurRifleCombat>();if(rifle)resident.GetComponent<SahurRifleCombat>()?.RestoreAmmo(rifle.Rounds,rifle.ReserveRounds);
            bool excluded=previous.GetComponent<GameSaveExcluded>();UnlockPlayer();previous.gameObject.SetActive(false);Destroy(previous.gameObject);
            player=resident;if(excluded&&!player.GetComponent<GameSaveExcluded>())player.gameObject.AddComponent<GameSaveExcluded>();
            home=resident.transform.position;homeRotation=resident.transform.rotation;
            LockPlayer();selectedWeapon=savedWeapon;player.transform.SetParent(boss.transform,true);SceneManager.MoveGameObjectToScene(boss.gameObject,scene);sceneReady=true;
        }
        void FindLanding()
        {
            landing=home;facing=Vector3.ProjectOnPlane(homeRotation*Vector3.forward,Vector3.up).normalized;if(facing.sqrMagnitude<.01f)facing=Vector3.forward;
            if(Surface(home,out var hit))
            {
                float sole=capsule?(capsule.center.y-capsule.height*.5f)*Mathf.Abs(player.transform.lossyScale.y):0;
                landing.y=hit.point.y-sole+.06f;
            }
        }
        bool Surface(Vector3 point,out RaycastHit best)
        {
            best=default;float nearest=float.MaxValue;
            foreach(var h in Physics.RaycastAll(point+Vector3.up*40,Vector3.down,120,~0,QueryTriggerInteraction.Ignore))
            {
                if(h.normal.y<.55f||h.distance>=nearest||h.collider.transform.IsChildOf(boss.transform)||h.collider.transform.IsChildOf(player.transform))continue;
                best=h;nearest=h.distance;
            }
            return nearest<float.MaxValue;
        }
        void LateUpdate()
        {
            if(restored||!player||pose==null)return;
            player.GetComponent<PlayerHealth>()?.GrantProtection(.5f);
            if(carried){if(Armed){carryAge+=Time.deltaTime;gripWeight=Mathf.SmoothStep(0,1,Mathf.Clamp01(carryAge/.4f));}ApplyCarry();}
            else if(falling)pose.Sample(Mathf.Lerp(.27f,.82f,fallPhase));
            else if(Phase=="impact")pose.Sample(.82f);
            else pose.Sample(0,0);
            if(!carried&&claws!=null&&falling)claws.Sample(1-Mathf.Clamp01(fallPhase*5));
            FrameCamera(false);
        }
        void ApplyCarry()
        {
            if(!player||pose==null)return;
            Vector3 grip=bellyFrame.TransformPoint(bellyLocal);Vector3 right=bellyFrame.right;
            // Imported aircraft visual axes can face backwards relative to the
            // flight root. Preserve anatomical left/right through that conversion.
            if(Vector3.Dot(right,player.transform.right)<0)right=-right;
            float sway=Mathf.Sin(carryAge*3.1f)*Mathf.Exp(-carryAge*.6f)*7;
            player.transform.rotation=Quaternion.LookRotation(facing)*Quaternion.Euler(8+(1-Mathf.Exp(-carryAge*2))*9,0,sway);
            pose.Sample(.2f+Mathf.Sin(carryAge*1.6f)*.025f,Phase=="grasp"?gripWeight:1);
            Vector3 desired=grip-Vector3.up*(pose.ArmReach*.74f)-(pose.Shoulders-player.transform.position)-facing*(bodyHeight*.02f);
            player.transform.position=Phase=="grasp"?Vector3.Lerp(carryFrom,desired,gripWeight):desired;
            pose.Hold(grip,right,bodyHeight*.12f,gripWeight);
            claws?.Hold(grip,right,bodyHeight*.12f,gripWeight);
        }
        IEnumerator FlyTo(Vector3 target,Quaternion face,float seconds,float bank)
        {
            Vector3 from=boss.transform.position;Quaternion rotation=boss.transform.rotation;float elapsed=0;
            while(elapsed<seconds)
            {
                if(Armed)
                {
                    float u=Mathf.Clamp01(elapsed/seconds),t=u*u*u*(u*(u*6-15)+10);
                    boss.transform.SetPositionAndRotation(Vector3.Lerp(from,target,t),Quaternion.Slerp(rotation,face,Mathf.SmoothStep(0,1,u)));
                    Bank(bank*Mathf.Sin(u*Mathf.PI));elapsed+=Time.deltaTime;
                }
                yield return null;
            }
            boss.transform.SetPositionAndRotation(target,face);Bank(0);
        }
        void Bank(float angle){if(boss.visual)boss.visual.localRotation=visualRest*Quaternion.Euler(-angle,0,Mathf.Sin(carryAge*1.7f)*2);}
        Vector3 RootForBelly(Vector3 target,Quaternion rotation)
        {
            Quaternion previous=boss.transform.rotation;boss.transform.rotation=rotation;
            Vector3 offset=bellyFrame.TransformPoint(bellyLocal)-boss.transform.position;boss.transform.rotation=previous;return target-offset;
        }
        void FrameCamera(bool snap)
        {
            if(!view)view=Camera.main;if(!view||!player)return;
            Vector3 focus=pose!=null?pose.Hips+Vector3.up*bodyHeight*.20f:player.transform.position;
            // Keep the person framed during the dive; the aircraft enters the
            // shot from above instead of pulling the lens up to empty sky.
            Vector3 desired=focus+(-facing*2.3f+Vector3.Cross(Vector3.up,facing)*1.9f+Vector3.up*.72f)*bodyHeight;
            if(snap)view.transform.position=desired;
            else if(Armed)view.transform.position=Vector3.SmoothDamp(view.transform.position,desired,ref cameraVelocity,.20f,Mathf.Infinity,Time.deltaTime);
            if((focus-view.transform.position).sqrMagnitude>.01f)view.transform.rotation=Quaternion.LookRotation(focus-view.transform.position);
        }
        void MakeCover()
        {
            fadeRoot=new GameObject("Abduction transition cover",typeof(RectTransform),typeof(Canvas));fadeRoot.transform.SetParent(transform,false);
            var canvas=fadeRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32758;
            var go=new GameObject("Black",typeof(RectTransform),typeof(Image));go.transform.SetParent(fadeRoot.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            cover=go.GetComponent<Image>();cover.color=Color.clear;cover.raycastTarget=false;
        }
        void SeatImpact()
        {
            if(!Surface(landing,out var ground))return;
            var mesh=new Mesh();float lowest=float.MaxValue;
            try
            {
                foreach(var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>())if(skin.enabled)
                {
                    skin.BakeMesh(mesh,false);
                    foreach(var vertex in mesh.vertices)lowest=Mathf.Min(lowest,skin.transform.TransformPoint(vertex).y);
                }
                float correction=ground.point.y+.035f-lowest;
                if(lowest<float.MaxValue&&Mathf.Abs(correction)<bodyHeight*1.5f)player.transform.position+=Vector3.up*correction;
            }
            finally{Destroy(mesh);}
        }
        void Update(){if(fadeRoot)fadeRoot.GetComponent<Canvas>().enabled=!PauseSettingsMenu.IsOpen;}
        IEnumerator Fade(float alpha,float duration)
        {
            float from=cover?cover.color.a:0,elapsed=0;
            while(elapsed<duration){if(Armed){if(cover)cover.color=new Color(0,0,0,Mathf.SmoothStep(from,alpha,elapsed/duration));elapsed+=Time.deltaTime;}yield return null;}
            if(cover)cover.color=new Color(0,0,0,alpha);
        }
        IEnumerator HoldTime(float seconds){float t=0;while(t<seconds){if(Armed)t+=Time.deltaTime;yield return null;}}
        void Finish()
        {
            player.transform.SetParent(null,true);player.RestoreSavedPose(landing,homeRotation);UnlockPlayer();Playing=false;
            bool started=SecondIslandAwakening.BeginAfterAbduction(player);
            if(!started&&view)player.ContinueCameraFromCurrentView(view,cameraVelocity);
            restored=true;CinematicLetterbox.Set(this,false);Destroy(boss.gameObject);
        }
        void Abort()
        {
            if(restored)return;restored=true;Playing=false;SceneManager.sceneLoaded-=SceneArrived;
            claws?.Dispose();claws=null;
            if(player){player.transform.SetParent(null,true);player.transform.SetPositionAndRotation(home,homeRotation);UnlockPlayer();if(view)player.ContinueCameraFromCurrentView(view,cameraVelocity);}
            if(boss){boss.ReleaseAbduction();if(boss.visual)boss.visual.localRotation=visualRest;}
            CinematicLetterbox.Set(this,false);if(fadeRoot)Destroy(fadeRoot);
        }
        void OnDisable(){if(!restored){StopAllCoroutines();Abort();}}
        void OnDestroy(){SceneManager.sceneLoaded-=SceneArrived;if(!restored)Abort();pose?.Dispose();claws?.Dispose();Playing=false;}
    }
}
