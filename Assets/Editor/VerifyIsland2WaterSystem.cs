using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mavis;

// Exercise the saved scene using real player movement and actual URP renders.
[InitializeOnLoad]
public static class VerifyIsland2WaterSystem
{
    const string Folder=".codex/island2-water-system/playtest/", Flag="Island2Water.Verify";
    static Keyboard keyboard,oldKeyboard;static bool walk,jump;static Camera review;
    static readonly List<string> errors=new List<string>();
    static VerifyIsland2WaterSystem(){EditorApplication.playModeStateChanged+=Changed;}
    static void Write(string name,object value){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+name,Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));}
    public static object Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||SceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit scene required");
        Write("saves-before.json",Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save*.json*").ToDictionary(p=>p,p=>Convert.ToBase64String(File.ReadAllBytes(p))));
        SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();return "Isolated lake, river and cloudfall playtest queued";
    }
    static void Changed(PlayModeStateChange s)
    {
        if(!SessionState.GetBool(Flag,false))return;
        if(s==PlayModeStateChange.EnteredPlayMode){typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,Path.GetFullPath(Folder+"isolated-save.json"));GameSaveManager.BeginEditorTestSession();EditorApplication.delayCall+=Run;}
        if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);var before=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Folder+"saves-before.json"));Write("preservation.json",new{formalSavesUnchanged=before.All(p=>File.Exists(p.Key)&&Convert.ToBase64String(File.ReadAllBytes(p.Key))==p.Value),scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,stopped=true});}
    }
    static async Task Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end){if(!EditorApplication.isPlaying)throw new Exception("Play interrupted");await Task.Delay(20);}}
    static void Log(string m,string t,LogType k){if(k==LogType.Error||k==LogType.Exception||k==LogType.Assert)errors.Add(m);}
    static void Inject(){if(keyboard!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(jump?new[]{Key.Space}:walk?new[]{Key.W}:Array.Empty<Key>()));}}
    static Vector3 Floor(Collider g,Vector2 p){if(!g.Raycast(new Ray(new Vector3(p.x,2800,p.y),Vector3.down),out var h,1900))throw new Exception("No ground: "+p);return h.point;}
    static Color32[] Shot(string name)
    {
        var rt=new RenderTexture(1600,1000,24);var prev=RenderTexture.active;var t=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        try{RenderPipeline.SubmitRenderRequest(review,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1600,1000),0,0);t.Apply();File.WriteAllBytes(Folder+name+".png",t.EncodeToPNG());return t.GetPixels32();}
        finally{RenderTexture.active=prev;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(t);}
    }
    static void CameraAt(Vector3 p,Vector3 target){review.transform.position=p;review.transform.LookAt(target);}
    static void Require(bool condition,string failure)
    {
        if(!condition)throw new Exception(failure);
    }
    static async Task VerifyChocolateContact(ThirdPersonPlayerController player, Collider ground, List<object> checks)
    {
        var contact=player.GetComponent<SecondIslandChocolateContact>();
        var skin=player.GetComponentInChildren<SkinnedMeshRenderer>();
        Require(contact&&contact.isActiveAndEnabled,"Chocolate contact did not load");
        void Place(Vector2 p,float extraHeight=0)
        {
            Vector3 world=Floor(ground,p);
            player.RestoreSavedPose(world+Vector3.up*(-player.LowestFootWorldOffset+.06f+extraHeight),Quaternion.LookRotation(Vector3.left));
        }
        void CloseUp()
        {
            Vector3 feet=player.transform.position+Vector3.up*player.LowestFootWorldOffset;
            CameraAt(feet+new Vector3(-8,4,-7),feet+Vector3.up*2.5f);
        }

        player.ExternalMovementLock=false;
        player.ExternalControlLock=false;
        oldKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=Inject;
        var lakePosition=new Vector2(-38,2070);
        contact.enabled=false;
        Place(lakePosition);
        await Wait(.38f);
        var originalMaterial=skin.sharedMaterial;
        CloseUp();
        Shot("sahur-dry-reference");
        var start=player.transform.position;
        walk=true;await Wait(1.2f);walk=false;
        float baseline=start.x-player.transform.position.x;

        contact.enabled=true;
        Place(lakePosition);
        await Wait(.53f);
        float depth=contact.Depth;
        Require(depth>.4f && contact.SpeedFactor<.9f && contact.Coating>.4f,"Lake contact did not build depth, drag and coating: "+depth);
        Require(skin.sharedMaterial!=originalMaterial,"Sahur still has his dry material while submerged");
        start=player.transform.position;
        int splashes=contact.Splashes;
        walk=true;await Wait(1.2f);walk=false;
        float slowed=start.x-player.transform.position.x;
        Require(slowed>2 && slowed<baseline*.9f,"Wading did not affect real input travel: "+slowed+" vs "+baseline);
        Require(contact.Splashes>splashes && contact.ActiveParticles>0,"Footsteps did not disturb the chocolate");
        CloseUp();
        Shot("sahur-wading-chocolate");
        checks.Add(new{name="depth and viscous drag",depth,baseline,slowed,splashes=contact.Splashes-splashes,passed=true});

        jump=true;
        bool stretched=false, emerged=false;
        float until=Time.realtimeSinceStartup+.85f;
        while(Time.realtimeSinceStartup<until)
        {
            await Wait(.017f);
            stretched|=contact.Stretching;
            emerged|=contact.Depth<=0;
            if(stretched&&emerged)break;
        }
        jump=false;
        Require(stretched&&emerged,"Manual jump did not clear the chocolate and stretch a thread");
        checks.Add(new{name="manual jump stretches chocolate",passed=true});

        Place(lakePosition,9);
        await Wait(2.6f);
        Require(contact.Depth>.4f&&contact.Spatter>.3f,"Hard landing did not coat Sahur with upward spatter");
        CloseUp();
        Shot("sahur-chocolate-spatter");
        checks.Add(new{name="hard landing spatters Sahur",coverage=contact.Spatter,passed=true});

        Place(new Vector2(-74,2075));
        await Wait(.6f);
        walk=true;await Wait(4);walk=false;
        Require(contact.Depth==0 && contact.Coating>.18f,
            $"Coating did not persist after walking out of the lake: depth={contact.Depth}, coating={contact.Coating}");
        CloseUp();
        Shot("sahur-coated-on-shore");
        checks.Add(new{name="coating stays on dry ground",coverage=contact.Coating,passed=true});

        player.ExternalMovementLock=true;
        contact.enabled=false;
        Require(skin.sharedMaterial==originalMaterial,"Disabling did not restore original skin");
        contact.enabled=true;
        InputSystem.onBeforeUpdate-=Inject;InputSystem.RemoveDevice(keyboard);keyboard=null;
        if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();
    }
    static async void Run()
    {
        bool passed=false;string failure=null;var traversal=new List<object>();var render=new List<object>();var performance=new List<object>();var contacts=new List<object>();errors.Clear();Application.logMessageReceived+=Log;
        try{
            var op=SceneManager.LoadSceneAsync(SecondIslandAudio.ScenePath);while(!op.isDone)await Wait(.05f);await Wait(1.3f);
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.Destroy(opening.gameObject);await Wait(.15f);
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();var g=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            var root=GameObject.Find("Lake to Cloudfall - connected chocolate water system");if(!root)throw new Exception("Saved water system did not load");
            oldKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=Inject;
            foreach(var cross in new[]{(name:"upper ford",a:new Vector2(14,1926),b:new Vector2(28,1913)),(name:"middle ford",a:new Vector2(-36,1861),b:new Vector2(-16,1852)),(name:"lower ford",a:new Vector2(-66,1812),b:new Vector2(-55,1796)),(name:"lake shore entry",a:new Vector2(-103,2075),b:new Vector2(-75,2075)),(name:"northwest river crossing",a:new Vector2(-318,2130),b:new Vector2(-278,2130)),(name:"lake outlet crossing",a:new Vector2(58,2010),b:new Vector2(20,2010))}){
                var heading=Floor(g,cross.b)-Floor(g,cross.a);heading.y=0;player.ExternalControlLock=false;player.ExternalMovementLock=false;
                player.RestoreSavedPose(Floor(g,cross.a)+Vector3.up*(-player.LowestFootWorldOffset+.06f),Quaternion.LookRotation(heading));await Wait(.25f);
                var start=player.transform.position;walk=true;await Wait(3.1f);walk=false;await Wait(.12f);var end=player.transform.position;float forward=Vector3.Dot(end-start,heading.normalized);
                bool okay=forward>9&&float.IsFinite(end.y)&&Mathf.Abs(end.y-Floor(g,new Vector2(end.x,end.z)).y)<8;traversal.Add(new{cross.name,forward,start=start.ToString(),end=end.ToString(),passed=okay});
                if(!okay)throw new Exception("Player traversal blocked at "+cross.name+": "+forward);
            }
            InputSystem.onBeforeUpdate-=Inject;InputSystem.RemoveDevice(keyboard);keyboard=null;if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();player.ExternalMovementLock=true;
            var sound=SecondIslandAudio.Current;
            if(!sound.InChocolate(sound.chocolateLakeCenter)||!SecondIslandAudio.Movement(0,sound.chocolateLakeCenter,null).name.StartsWith("RiverStep"))throw new Exception("Lake needs chocolate foley");
            if(sound.GetComponentsInChildren<AudioSource>().Count(s=>s.loop)!=4)throw new Exception("Four bounded ambient beds required");
            var original=Camera.main;original.enabled=false;review=new GameObject("Water validation review camera").AddComponent<Camera>();review.CopyFrom(original);review.enabled=true;review.fieldOfView=58;review.farClipPlane=4500;review.nearClipPlane=.3f;
            var styles=Mavis.VisualStyles.VisualStyleManager.Instance;styles.Select("realistic",false);
            await VerifyChocolateContact(player,g,contacts);
            foreach(var view in new[]{(name:"lake",p:new Vector3(-110,2555,1985),t:new Vector3(0,2523,2070)),(name:"river",p:Floor(g,new Vector2(-39,1881))+Vector3.up*6,t:Floor(g,new Vector2(-24,1857))+Vector3.up),(name:"cloudfall",p:new Vector3(-415,2440,2590),t:new Vector3(-250,2290,2460)),(name:"cloudfall-profile",p:new Vector3(-470,2410,2420),t:new Vector3(-250,2260,2490)),(name:"connected-system",p:new Vector3(-100,3120,2130),t:new Vector3(-110,2460,2050))}){
                CameraAt(view.p,view.t);await Wait(.2f);Shot(view.name);
                // Render only the three actual chocolate surfaces. No cloud, NPC or camera
                // movement can make this animation check pass.
                var surfaces=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.shader.name=="Dark Brine/Second Island/Viscous Chocolate").ToArray();var layers=surfaces.Select(r=>r.gameObject.layer).ToArray();int mask=review.cullingMask;var flags=review.clearFlags;var background=review.backgroundColor;
                for(int i=0;i<surfaces.Length;i++)surfaces[i].gameObject.layer=31;review.cullingMask=1<<31;review.clearFlags=CameraClearFlags.SolidColor;review.backgroundColor=Color.black;
                var a=Shot(view.name+"-surface-a");await Wait(1);var b=Shot(view.name+"-surface-b");int visible=0,changed=0;long difference=0;
                for(int i=0;i<a.Length;i++){int lum=a[i].r+a[i].g+a[i].b;if(lum<12)continue;visible++;int d=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);difference+=d;if(d>7)changed++;}
                for(int i=0;i<surfaces.Length;i++)surfaces[i].gameObject.layer=layers[i];review.cullingMask=mask;review.clearFlags=flags;review.backgroundColor=background;
                bool okay=visible>1000&&changed>visible*.025;render.Add(new{view.name,visibleWaterPixels=visible,animatedPixels=changed,meanDifference=difference/(float)Math.Max(1,visible),passed=okay});if(!okay)throw new Exception("Water not visibly animated: "+view.name);
                if(view.name=="lake"||view.name=="cloudfall"){foreach(bool on in new[]{false,true}){root.SetActive(on);await Wait(.3f);var times=new List<float>();var batches=new List<int>();int frame=Time.frameCount;while(times.Count<30){await Task.Delay(8);if(Time.frameCount==frame)continue;frame=Time.frameCount;times.Add(Time.unscaledDeltaTime*1000);batches.Add(UnityEditor.UnityStats.batches);}times.Sort();performance.Add(new{view=view.name,water=on,medianEditorFrameMs=times[15],averageBatches=batches.Average()});}root.SetActive(true);}
            }
            CameraAt(new Vector3(-110,2555,1985),new Vector3(0,2523,2070));styles.Select("van-gogh",false);await Wait(.2f);Shot("lake-alternate-style");styles.RestorePreference();passed=errors.Count==0;if(!passed)failure="Runtime errors captured";
        }catch(Exception e){failure=e.ToString();}
        finally{walk=jump=false;InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);keyboard=null;if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(review)UnityEngine.Object.Destroy(review.gameObject);Application.logMessageReceived-=Log;Write("result.json",new{passed,failure,traversal,render,performance,contacts,errors});EditorApplication.ExitPlaymode();}
    }
}
