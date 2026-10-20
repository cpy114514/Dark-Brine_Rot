using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Profiling;
using Mavis;
public static class ProfileCombatCombinations
{
    static Mouse mouse;static Keyboard keyboard;static bool held;
    static void Inject(){if(mouse==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;mouse.MakeCurrent();keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState());InputState.Change(mouse,new MouseState().WithButton(MouseButton.Left,held).WithButton(MouseButton.Right,held));}
    static async Task Frames(int count)
    {int until=Time.frameCount+count;var deadline=DateTime.UtcNow.AddSeconds(25);while(Time.frameCount<until){if(DateTime.UtcNow>deadline)throw new Exception("Frame loop stalled");await Task.Delay(5);}}
    public static async Task<object> Run()
    {
        float capture=Time.captureDeltaTime;var previousMouse=Mouse.current;var previousKeyboard=Keyboard.current;
        ProfilerRecorder sweep=default;
        try
        {
            CombatFeelPreview.Spawn();Time.captureDeltaTime=1f/60;
            mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();held=false;InputSystem.onBeforeUpdate+=Inject;
            await Frames(20);var player=CombatFeelPreview.Player;var attack=player.GetComponent<SahurAttack>();
            CombatFeelPreview.Target.transform.position=new Vector3(2000,500,2003);Physics.SyncTransforms();held=true;await Frames(30);
            if(!attack.IsSpinning)throw new Exception("Spin did not start");
            int objectsBefore=player.transform.parent.GetComponentsInChildren<Transform>(true).Length;
            int particlesBefore=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
            sweep=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Sahur.WeaponSweep",512);
            var values=new List<double>();int previousFrame=-1;int end=Time.frameCount+180;
            while(Time.frameCount<end)
            {
                if(previousFrame!=Time.frameCount){previousFrame=Time.frameCount;if(sweep.LastValue>0)values.Add(sweep.LastValue/1000000.0);}
                await Task.Delay(5);
            }
            values.Sort();
            int objectsAfter=player.transform.parent.GetComponentsInChildren<Transform>(true).Length;
            int particlesAfter=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
            var result=new{passed=attack.IsSpinning && objectsBefore==objectsAfter && particlesBefore==particlesAfter,simulationHz=60,observedFrames=180,objectsBefore,objectsAfter,particlesBefore,particlesAfter,recorderValid=sweep.Valid,samples=values.Count,sweepMedianMs=values.Count>0?values[values.Count/2]:-1,sweepP95Ms=values.Count>0?values[(int)(values.Count*.95)]:-1,sweepMaxMs=values.Count>0?values.Last():-1,scope="Existing Editor, warmed native club-spin weapon sweeps only; excludes initial cache setup, total frame time, GPU, real-time FPS and allocations from this async observation harness."};
            File.WriteAllText(".codex/combat-combinations-20261019/performance.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            if(sweep.Valid)sweep.Dispose();held=false;InputSystem.onBeforeUpdate-=Inject;
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);mouse=null;keyboard=null;
            if(previousMouse!=null && previousMouse.added)previousMouse.MakeCurrent();if(previousKeyboard!=null && previousKeyboard.added)previousKeyboard.MakeCurrent();
            CombatFeelPreview.Clear();Time.captureDeltaTime=capture;
        }
    }
    public static object AuditAssets()
    {
        var rows=new List<object>();
        foreach(string name in new[]{"ClubSpin","BladeSpin","BladeCharge","BladeRush","BladeGuard","JumpChop"})
        {
            var clip=Resources.Load<AnimationClip>("Combat/Combinations/"+name);if(!clip)throw new Exception(name+" clip missing");
            string path="Assets/AnimationStaging/PLAYER_"+name+"_v001/PLAYER_"+name+"_v001.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);var take=importer.clipAnimations.Single();
            rows.Add(new{name,path,clip.frameRate,clip.length,clip.isLooping,events=clip.events.Length,avatarValid=importer.sourceAvatar.isValid,avatarHuman=importer.sourceAvatar.isHuman,take.rotationOffset,take.lockRootRotation,take.lockRootHeightY,take.lockRootPositionXZ,runtimeBytes=new FileInfo(AssetDatabase.GetAssetPath(clip)).Length,runtimeMemoryBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(clip),importer.isReadable});
        }
        File.WriteAllText(".codex/combat-combinations-20261019/assets-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
    }
}
