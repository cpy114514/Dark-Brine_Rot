using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>Bounded local evidence when a standalone stops pumping frames.</summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class RuntimeStallDiagnostics : MonoBehaviour
    {
        static string activity="frame loop";
        static CappuccinoAI capri;
        static NailongAI nailong;
        static BombardinoBoss aircraft;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetBosses(){SceneManager.sceneUnloaded-=ClearDestroyedBosses;capri=null;nailong=null;aircraft=null;activity="frame loop";}
        static void ClearDestroyedBosses(Scene scene){if(!capri)capri=null;if(!nailong)nailong=null;if(!aircraft)aircraft=null;}
        public static void TrackBoss(Component owner)
        {
            if(owner.TryGetComponent<CappuccinoAI>(out var c))capri=c;
            if(owner.TryGetComponent<NailongAI>(out var n))nailong=n;
            if(owner.TryGetComponent<BombardinoBoss>(out var b))aircraft=b;
        }
        long pulse;
        int monitor,reported,writing;
        string snapshot="startup",path;
        double nextSnapshot,lastWarning;
        Timer watchdog;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneUnloaded+=ClearDestroyedBosses;
            if(Application.isEditor)return;
            var go=new GameObject("Local stall diagnostics");DontDestroyOnLoad(go);go.AddComponent<RuntimeStallDiagnostics>();
        }
        public readonly struct Activity : IDisposable
        {
            readonly string previous;
            public Activity(string name){previous=Volatile.Read(ref activity);Volatile.Write(ref activity,name);}
            public void Dispose()=>Volatile.Write(ref activity,previous);
        }
        public static Activity Measure(string name)=>new Activity(name);
        void Awake()
        {
            path=Path.Combine(Application.persistentDataPath,"runtime-stalls.log");
            Write("session "+DateTime.UtcNow.ToString("o")+" / "+Application.unityVersion+" / "+SystemInfo.graphicsDeviceName);
            Interlocked.Exchange(ref pulse,Stopwatch.GetTimestamp());
            Volatile.Write(ref monitor,Application.isFocused?1:0);
            watchdog=new Timer(CheckPulse,null,1000,1000);
        }
        void Update()
        {
            long now=Stopwatch.GetTimestamp();
            double gap=(now-Interlocked.Exchange(ref pulse,now))/(double)Stopwatch.Frequency;
            if(gap>=1 && Volatile.Read(ref monitor)==1 && Time.realtimeSinceStartupAsDouble-lastWarning>10)
            {
                lastWarning=Time.realtimeSinceStartupAsDouble;
                Write("resumed after "+gap.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"s / "+Volatile.Read(ref snapshot));
            }
            Interlocked.Exchange(ref reported,0);
            if(Time.realtimeSinceStartupAsDouble<nextSnapshot)return;
            nextSnapshot=Time.realtimeSinceStartupAsDouble+2;
            var player=FindFirstObjectByType<ThirdPersonPlayerController>();
            string state="frame="+Time.frameCount+" scene="+SceneManager.GetActiveScene().name+" scale="+Time.timeScale+
                " loading="+SceneLoadingScreen.IsLoading+" additive="+AdditiveSceneBootstrap.IsLoading+
                " nativeMB="+UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576+
                " managedMB="+GC.GetTotalMemory(false)/1048576;
            if(player)state+=" player="+player.transform.position.ToString()+" control="+player.ExternalControlLock+" movement="+player.ExternalMovementLock;
            state+=" vsync="+QualitySettings.vSyncCount+" frameLimit="+Application.targetFrameRate+" delta="+Time.unscaledDeltaTime;
            if(capri)state+=" cappuccino="+capri.CurrentState+"/"+capri.CurrentAttack+"/"+capri.FightActive;
            if(nailong)state+=" nailong="+nailong.CurrentState+"/"+nailong.AttackName+"/"+nailong.FightActive;
            if(aircraft)state+=" aircraft="+aircraft.CurrentState+"/"+aircraft.CurrentAttack+"/"+aircraft.FightActive;
            Volatile.Write(ref snapshot,state);
        }
        // This callback uses only cached managed values: no Unity API off-thread.
        void CheckPulse(object unused)
        {
            if(Volatile.Read(ref monitor)==0)return;
            double gap=(Stopwatch.GetTimestamp()-Interlocked.Read(ref pulse))/(double)Stopwatch.Frequency;
            if(gap<5 || Interlocked.Exchange(ref reported,1)!=0)return;
            Write("no heartbeat for "+gap.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"s / operation="+Volatile.Read(ref activity)+" / "+Volatile.Read(ref snapshot));
        }
        void Write(string line)
        {
            if(Interlocked.Exchange(ref writing,1)!=0)return;
            try
            {
                if(File.Exists(path)&&new FileInfo(path).Length>65536)File.WriteAllText(path,"older stall entries truncated\n");
                File.AppendAllText(path,DateTime.UtcNow.ToString("o")+" "+line+Environment.NewLine);
            }
            catch(IOException){}catch(UnauthorizedAccessException){}
            finally{Volatile.Write(ref writing,0);}
        }
        void OnApplicationFocus(bool focus){Interlocked.Exchange(ref pulse,Stopwatch.GetTimestamp());Volatile.Write(ref monitor,focus?1:0);}
        void OnApplicationPause(bool paused){Interlocked.Exchange(ref pulse,Stopwatch.GetTimestamp());Volatile.Write(ref monitor,!paused&&Application.isFocused?1:0);}
        void OnEnable(){Interlocked.Exchange(ref pulse,Stopwatch.GetTimestamp());Volatile.Write(ref monitor,Application.isFocused?1:0);}
        void OnDisable()=>Volatile.Write(ref monitor,0);
        void OnDestroy(){Volatile.Write(ref monitor,0);watchdog?.Dispose();watchdog=null;SceneManager.sceneUnloaded-=ClearDestroyedBosses;capri=null;nailong=null;aircraft=null;}
    }
}
