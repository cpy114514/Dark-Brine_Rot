using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class VerifyJumpGraceV006
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task<object> Verify()
    {
        Require(Application.isPlaying,"Play mode required");
        var originals=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b.enabled && (b is ThirdPersonPlayerController || b is SahurAttack || b is PlayerInput || b is MainMenuController)).ToArray();
        foreach(var b in originals)b.enabled=false;
        var keys=InputSystem.AddDevice<Keyboard>("Jump grace fixture");var oldCursor=Cursor.lockState;var holder=new GameObject("Jump grace fixture");holder.SetActive(false);holder.transform.position=new Vector3(2000,500,2000);
        var floor=new GameObject("Jump grace floor");floor.transform.position=holder.transform.position-Vector3.up*.5f;var collider=floor.AddComponent<BoxCollider>();collider.size=new Vector3(80,1,80);
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);actor.AddComponent<GameSaveExcluded>();
        var player=actor.GetComponent<ThirdPersonPlayerController>();player.waterSplashes=false;actor.GetComponent<PlayerStamina>().enabled=false;
        foreach(var input in actor.GetComponentsInChildren<PlayerInput>(true))input.enabled=false;
        float sole=player.LowestFootWorldOffset;var vertical=typeof(ThirdPersonPlayerController).GetField("verticalSpeed",BindingFlags.Instance|BindingFlags.NonPublic);
        async Task Hold(int ms,params Key[] held){keys.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(keys,new KeyboardState(held));await Task.Delay(ms);}
        try
        {
            holder.SetActive(true);sole=player.LowestFootWorldOffset;player.RestoreSavedPose(holder.transform.position+Vector3.up*(.02f-sole),Quaternion.identity);Physics.SyncTransforms();await Hold(350);
            Require(player.CanUseGroundAttack,"Fixture not grounded");collider.enabled=false;Physics.SyncTransforms();await Hold(60);
            await Hold(80,GameInputSettings.Get(GameInputSettings.Action.Jump));float coyoteVelocity=(float)vertical.GetValue(player);
            Require(coyoteVelocity>1,"100ms ground grace did not accept a 60ms late jump: "+coyoteVelocity);
            await Hold(150);collider.enabled=true;player.RestoreSavedPose(holder.transform.position+Vector3.up*(.35f-sole),Quaternion.identity);vertical.SetValue(player,-4f);Physics.SyncTransforms();
            Require(!player.CanUseGroundAttack,"Buffered jump fixture already grounded");
            await Hold(120,GameInputSettings.Get(GameInputSettings.Action.Jump));float bufferedVelocity=(float)vertical.GetValue(player);
            Require(bufferedVelocity>1,"150ms pre-landing buffer did not jump after landing: "+bufferedVelocity);
            return new{coyoteSeconds=player.coyoteTime,bufferSeconds=player.jumpBufferTime,coyoteVelocity,bufferedVelocity};
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(holder);UnityEngine.Object.DestroyImmediate(floor);InputSystem.RemoveDevice(keys);Cursor.lockState=oldCursor;
            foreach(var b in originals)if(b!=null)b.enabled=true;
        }
    }
}
