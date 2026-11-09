using System;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Walks Sahur through the actual cottage entrance and back out.</summary>
public static class VerifyCandyCottage
{
    public static string Status="idle", Result="";
    static Keyboard keyboard;
    static Key[] keys=Array.Empty<Key>();
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        InputState.Change(keyboard,new KeyboardState(keys));
    }
    static async Task Wait(float seconds)
    {
        float until=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(20);
        while(Time.time<until)
        {
            if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)throw new Exception("Play mode stopped or stalled.");
            await Task.Delay(20);
        }
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
            // The review moves the player through the real scene. Do not let
            // an autosave overwrite the player's regular campaign slot.
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static)
                .SetValue(null,System.IO.Path.GetFullPath("Temp/candy-cottage-test.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
            if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.4f);
            var house=UnityEngine.Object.FindFirstObjectByType<CandyHouseMarker>();
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(!house || !player)throw new Exception("House or player missing.");
            var body=player.GetComponent<CharacterController>();
            var ground=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            keyboard=Keyboard.current;
            if(!body || !ground || keyboard==null)throw new Exception("Player, ground or keyboard missing.");
            var enemyRoot=GameObject.Find("Marshmallow Friends - Second Island");if(enemyRoot)enemyRoot.SetActive(false);
            player.GetComponent<PlayerHealth>().GrantProtection(60);
            player.GetComponent<MarshmallowTraversal>()?.SetNormalGravity(false);
            var start=house.transform.TransformPoint(new Vector3(0,0,-22));
            if(!ground.Raycast(new Ray(start+Vector3.up*50,Vector3.down),out var hit,100))throw new Exception("No ground before the ramp.");
            float footOffset=-(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
            start.y=hit.point.y+footOffset+.06f;
            player.RestoreSavedPose(start,house.transform.rotation);
            typeof(ThirdPersonPlayerController).GetField("yaw",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,house.transform.eulerAngles.y);
            InputSystem.onBeforeUpdate+=Inject;
            await Wait(.35f);
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
            float deadline=Time.time+12f;
            while(house.transform.InverseTransformPoint(player.transform.position).z<1f && Time.time<deadline)await Task.Delay(20);
            keys=Array.Empty<Key>();await Wait(.25f);
            var inside=house.transform.InverseTransformPoint(player.transform.position);
            if(inside.z<1f || Mathf.Abs(inside.x)>2.5f)throw new Exception("Could not walk through the open doorway: "+inside);
            deadline=Time.time+4f;
            while((!body.isGrounded || Mathf.Abs(body.bounds.min.y-house.transform.position.y)>.35f) && Time.time<deadline)await Task.Delay(20);
            if(!body.isGrounded || Mathf.Abs(body.bounds.min.y-house.transform.position.y)>.35f)throw new Exception("The interior floor does not support the player.");
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Back)};
            deadline=Time.time+12f;
            while(house.transform.InverseTransformPoint(player.transform.position).z>-18f && Time.time<deadline)await Task.Delay(20);
            keys=Array.Empty<Key>();await Wait(.2f);
            var outside=house.transform.InverseTransformPoint(player.transform.position);
            if(outside.z>-18f)throw new Exception("Could not walk back down the ramp: "+outside);
            var renderers=house.GetComponentsInChildren<Renderer>();
            int structure=0,pickupVisuals=0;
            foreach(var renderer in renderers)
            {
                if(!renderer.enabled)continue;
                if(renderer.GetComponentInParent<CandyHouseHealingPickup>())pickupVisuals++;else structure++;
            }
            // Two readable 3D blackboards replaced two old text renderers;
            // their frame, slate and lettering add four visible renderers.
            if(structure>36 || pickupVisuals>9)throw new Exception($"Too many cottage renderers: structure={structure}, pickups={pickupVisuals}.");
            Result="Walked from island up the ramp, through the doorway, across the supported floor and back outside.";Status="passed";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        finally
        {
            keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=Inject;
            if(keyboard!=null)InputState.Change(keyboard,new KeyboardState());
        }
        Debug.Log("Candy cottage test: "+Status+" "+Result);
    }
}
