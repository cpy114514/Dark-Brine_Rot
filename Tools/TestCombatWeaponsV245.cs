using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Mavis;
public static class TestCombatWeaponsV245
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> checks=new List<string>();
    static Keyboard keyboard;static Mouse mouse;static bool right;static Key[] keys=Array.Empty<Key>();
    static void Check(bool c,string text){if(!c)throw new Exception(text);checks.Add(text);}
    static void Inject(){if(keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(keys));InputState.Change(mouse,new MouseState().WithButton(MouseButton.Right,right));}
    static async Task Until(Func<bool> condition,float timeout=6){float end=Time.unscaledTime+timeout;while(!condition()){if(Time.unscaledTime>end)throw new Exception("Native input state timeout");await Task.Delay(8);}}
    public static async Task<object> Run()
    {
        var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;GameObject aircraft=null,cam=null;checks.Clear();
        var measures=new Dictionary<string,object>();
        try
        {
            CombatFeelPreview.Spawn();await Task.Delay(700);
            var player=CombatFeelPreview.Player;var motor=player.GetComponent<ThirdPersonPlayerController>();var hp=player.GetComponent<PlayerHealth>();
            if(!hp)hp=player.AddComponent<PlayerHealth>();hp.maxHealth=hp.currentHealth=100;hp.NarrativeHealthFloor=0;
            var attack=player.GetComponent<SahurAttack>();var loadout=player.GetComponent<SahurWeaponLoadout>();var feedback=player.GetComponent<CombatHitFeedback>();
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();var input=player.GetComponent<PlayerInput>();InputUser.PerformPairingWithDevice(keyboard,input.user);InputUser.PerformPairingWithDevice(mouse,input.user);input.ActivateInput();InputSystem.onBeforeUpdate+=Inject;
            loadout.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);await Task.Delay(500);
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Guard)};await Until(()=>attack.GuardReady);
            var point=player.GetComponent<CharacterController>().bounds.center;float before=hp.currentHealth;int count=feedback.HitCount;
            Check(!hp.ApplyAttackDamage(30,point,player.transform.position+player.transform.forward*4) && hp.currentHealth==before && feedback.HitCount==count+1,"Native E guard blocks frontal damage with exactly one resistance event");
            Check(!hp.ApplyAttackDamage(30,point,player.transform.position+Quaternion.AngleAxis(70,Vector3.up)*player.transform.forward*4) && hp.currentHealth==before,"Native twin guard covers a wide frontal attack");
            Check(hp.ApplyAttackDamage(10,point,player.transform.position-player.transform.forward*4) && Mathf.Abs(hp.currentHealth-(before-7))<.001f,"Rear attack remains dangerous and uses the new hostile damage balance");
            keys=Array.Empty<Key>();await Task.Delay(850);
            CombatFeelPreview.Target.transform.position=player.transform.position+Vector3.forward*100;Physics.SyncTransforms();
            right=true;await Until(()=>attack.IsCharging);await Until(()=>attack.Charge01>=.99f,9);right=false;
            var rush=player.GetComponent<SahurTwinBladeMotion>();await Until(()=>rush.IsDamageImmune,4);
            before=hp.currentHealth;count=feedback.HitCount;
            hp.ApplyDamage(20,point);bool incoming=hp.ApplyAttackDamage(20,point,player.transform.position+Vector3.forward*3);
            bool routed=CombatHitFeedback.Apply(CombatFeelPreview.Target,hp,20,point,CombatHitKind.ChargedHeavy);
            Check(!incoming&&!routed&&hp.currentHealth==before&&feedback.HitCount==count,"Twin rush still rejects raw, hostile and routed damage without false hurt feedback");
            await Until(()=>!attack.IsCombatMotionActive,8);await Task.Delay(350);
            loadout.Restore(true,SahurWeaponLoadout.Weapon.M4A1,true);await Task.Delay(600);
            Check(loadout.UsesRifle,"Native loadout changes to the existing rifle controller");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab");
            aircraft=UnityEngine.Object.Instantiate(prefab,player.transform.position+Vector3.forward*30+Vector3.up*15,Quaternion.identity);aircraft.AddComponent<GameSaveExcluded>();
            var boss=aircraft.GetComponent<BombardinoBoss>();boss.enabled=false;var health=aircraft.GetComponent<Health>();
            await Task.Delay(150);var engine=aircraft.GetComponentInChildren<BossWeakPoint>();Check(engine!=null,"Actual aircraft engine weak point exists");
            var hitbox=engine.GetComponent<Collider>();var rifle=player.GetComponent<SahurRifleCombat>();float bossBefore=health.currentHealth;int hits=rifle.Hits;count=feedback.HitCount;
            float expected=SahurRifleCombat.DamageAtDistance(10,true,boss.IsExposed,true);
            typeof(SahurRifleCombat).GetMethod("ImpactAt",Flags).Invoke(rifle,new object[]{hitbox,engine.transform.position,10f});
            measures["expectedEngineDamage"]=expected;measures["actualEngineDamage"]=bossBefore-health.currentHealth;
            Check(Mathf.Abs(bossBefore-health.currentHealth-expected)<.001f,"Rifle keeps exact engine damage without applying the weak-point multiplier twice");
            Check(rifle.Hits==hits+1&&feedback.HitCount==count+1&&feedback.LastHitKind==CombatHitKind.Rifle,"Actual rifle impact path emits one confirmed rifle event");
            Check(aircraft.GetComponent<CombatDamageFlash>().IsFlashing,"Aircraft has a real received-hit material response");
            Check(!CombatLocalHitStop.Active(player),"Bullet impact does not freeze the player's rifle locomotion");
            hp.ApplyAttackDamage(6,point,aircraft.transform.position);await Task.Delay(150);
            Check(player.GetComponent<CombatReceivedReaction>().Weight>.05f,"Authored hurt overlay rebinds after the rifle Animator controller switch");
            cam=new GameObject("Temporary frontal impulse contract");var shake=cam.AddComponent<CombatCameraShake>();
            Vector3 pos=cam.transform.position;Quaternion rot=cam.transform.rotation;
            shake.PulseDirectional(Vector3.forward,.065f,.13f);typeof(CombatCameraShake).GetMethod("LateUpdate",Flags).Invoke(shake,null);
            float angle=Quaternion.Angle(rot,cam.transform.rotation);measures["frontalImpulseDegrees"]=angle;
            Check(angle>.25f && angle<1.1f,"Frontal contact has a readable bounded angular impulse");
            typeof(CombatCameraShake).GetMethod("RemoveOffset",Flags).Invoke(shake,null);
            Check(Vector3.Distance(pos,cam.transform.position)<.00001f&&Quaternion.Angle(rot,cam.transform.rotation)<.001f,"Frontal camera impulse removes its exact offset without drift");
            var data=new{passed=true,checks,measures};File.WriteAllText(".codex/combat-recovery-20261023/weapons-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;
        }
        catch(Exception e){File.WriteAllText(".codex/combat-recovery-20261023/weapons-failure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks,measures,error=e.ToString()},Newtonsoft.Json.Formatting.Indented));throw;}
        finally
        {
            InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;
            oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();keys=Array.Empty<Key>();right=false;
            if(aircraft)UnityEngine.Object.DestroyImmediate(aircraft);if(cam)UnityEngine.Object.DestroyImmediate(cam);CombatFeelPreview.Clear();
        }
    }
}
