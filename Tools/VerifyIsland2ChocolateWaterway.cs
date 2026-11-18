using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;
using Object=UnityEngine.Object;

public static class VerifyIsland2ChocolateWaterway
{
    const string Folder=".codex/chocolate-rebuild/";
    static Keyboard keyboard;
    static bool walk,jump;
    static void Inject()
    {
        if(keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();
        Cursor.lockState=CursorLockMode.Locked;
        InputState.Change(keyboard,jump?new KeyboardState(Key.Space):walk?new KeyboardState(Key.W):new KeyboardState());
    }
    static void Check(bool accepted,string label)
    {
        if(!accepted)throw new InvalidOperationException(label);
    }
    static async Task Tick(float seconds)
    {
        double end=Time.realtimeSinceStartupAsDouble+seconds;
        while(Time.realtimeSinceStartupAsDouble<end)await Task.Delay(14);
    }
    static bool Floor(MeshCollider ground,Vector3 p,out RaycastHit hit)=>
        ground.Raycast(new Ray(new Vector3(p.x,ground.bounds.max.y+3,p.z),Vector3.down),out hit,ground.bounds.size.y+6);
    static Vector2 XZ(Vector3 p)=>new Vector2(p.x,p.z);
    static void Shot(string n)=>ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+n+".png"));

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && GameSaveManager.BeginEditorTestSession(),"Isolated Play Mode required.");
        var player=Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var contact=player.GetComponent<SecondIslandChocolateContact>();
        var ground=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<MeshCollider>();
        var audio=Object.FindFirstObjectByType<SecondIslandAudio>();
        var data=audio.waterway;
        Check(data && contact && contact.enabled,"The staged waterway is not bound.");
        var opening=Object.FindFirstObjectByType<SecondIslandAwakening>();
        if(opening)Object.Destroy(opening.gameObject);
        var health=player.GetComponent<PlayerHealth>();if(health)health.GrantProtection(150);
        var original=Keyboard.current;
        keyboard=InputSystem.AddDevice<Keyboard>();
        InputSystem.onBeforeUpdate+=Inject;
        var cases=new List<object>();
        var errors=new List<string>();
        string failure=null;
        void Log(string message,string trace,LogType type)
        {if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(message);}
        Application.logMessageReceived+=Log;
        void Place(Vector3 at,Vector3 axis,float elevation=0)
        {
            Check(Floor(ground,at,out var hit),"No terrain at "+at);
            player.ExternalMovementLock=player.ExternalControlLock=false;
            axis.y=0;
            player.RestoreSavedPose(hit.point+Vector3.up*(.085f-player.LowestFootWorldOffset+elevation),Quaternion.LookRotation(axis));
        }

        try
        {
            await Tick(.35f);
            foreach(var c in data.crossings)
            {
                Vector3 aim=c.to-c.from;aim.y=0;
                Vector3 axis=aim.normalized;
                Place(c.from,axis);
                await Tick(.45f);
                float peak=0,progress=0;bool exited=false;
                double end=Time.realtimeSinceStartupAsDouble+8;
                walk=true;
                while(Time.realtimeSinceStartupAsDouble<end)
                {
                    await Tick(.06f);peak=Mathf.Max(peak,contact.Depth);
                    progress=Vector3.Dot(player.transform.position-c.from,axis);
                    if(progress>aim.magnitude-1.25f&&contact.Depth<.05f){exited=true;break;}
                }
                walk=false;
                var at=player.transform.position;
                float strafe=Mathf.Abs(Vector3.Dot(at-c.from,Vector3.Cross(axis,Vector3.up)));
                cases.Add(new{c.label,progress,strafe,peak,exited});
                Check(exited&&peak>.10f&&strafe<7,"Crossing failed: "+c.label+" "+progress+"m depth "+peak);
            }
            var cross=data.crossings.Last();
            Vector3 near=cross.to+(cross.to-cross.from).normalized*4;
            var next=data.sections.First(s=>s.distance>322);
            Vector3 shore=next.right+(next.right-next.left).normalized*9;
            Place(near,shore-near);
            var start=player.transform.position;
            walk=true;await Tick(2.1f);walk=false;
            float lookout=Vector2.Distance(XZ(start),XZ(player.transform.position));
            Check(lookout>8,"Cloudfall bank approach blocked.");
            cases.Add(new{label="Cloudfall lookout approach",travel=lookout});

            Place(new Vector3(-147,0,2174),Vector3.forward);
            await Tick(.30f);
            start=player.transform.position;
            walk=true;await Tick(2.2f);walk=false;
            float hopApproach=player.transform.position.z-start.z;
            Check(hopApproach>13 && contact.Depth<.01f,"Existing marshmallow hop approach was obstructed or flooded.");
            cases.Add(new{label="Existing hop course approach",travel=hopApproach});

            Vector3 lake=data.lakeCenter+new Vector3(-43,0,3);
            contact.enabled=false;
            Place(lake,Vector3.left);
            await Tick(.45f);
            Vector3 dryStart=player.transform.position;
            walk=true;await Tick(1.05f);walk=false;
            float unresisted=Vector2.Distance(XZ(dryStart),XZ(player.transform.position));
            contact.enabled=true;
            Place(lake,Vector3.left);
            await Tick(.60f);
            float depth=contact.Depth;
            float drag=contact.SpeedFactor;
            Vector3 initial=player.transform.position;
            await Tick(2.1f);
            float moved=Vector2.Distance(XZ(initial),XZ(player.transform.position));
            Check(depth>.65f&&contact.SpeedFactor<.91f&&contact.Coating>.36f&&moved>.012f,"Sahur needs real current, resistance and adhesion.");
            int splashes=contact.Splashes;walk=true;
            await Tick(1.05f);walk=false;await Tick(.06f);
            float wading=Vector2.Distance(XZ(initial),XZ(player.transform.position));
            Check(wading<unresisted*.90f&&wading>2,"Real input did not slow during wading.");
            Check(contact.Splashes>splashes&&contact.ActiveParticles>0,"Wading failed to create physical droplets.");
            Shot("sahur-wading");
            jump=true;
            bool stringSeen=false,air=false;
            double jumpEnd=Time.realtimeSinceStartupAsDouble+1.35;
            while(Time.realtimeSinceStartupAsDouble<jumpEnd&&!(stringSeen&&air))
            {
                await Tick(.025f);
                stringSeen|=contact.Stretching;air|=contact.Depth<=.01f;
            }
            jump=false;
            Check(stringSeen&&air,"Jump didn't stretch the chocolate and exit the surface.");
            cases.Add(new{label="Standing current and wet jump",depth,speedFactor=drag,drift=moved,
                unresisted,wading,stringSeen,splashes=contact.Splashes-splashes});

            var rim=data.lakeOutline.OrderBy(v=>Mathf.Abs(v.x+47)+Mathf.Abs(v.z-2021)).First();
            Vector3 outward=(rim-data.lakeCenter).normalized;
            Place(rim+outward*7,-outward);
            await Tick(.35f);
            Check(contact.Depth<.01f,"Lake approach should start on dry ground.");
            walk=true;
            double entryEnd=Time.realtimeSinceStartupAsDouble+3;
            while(Time.realtimeSinceStartupAsDouble<entryEnd&&contact.Depth<.2f)await Tick(.05f);
            walk=false;
            Check(contact.Depth>=.2f,"Walking from the shore failed to enter the lake.");
            cases.Add(new {label="Lake shore entry",depth=contact.Depth});
            Place(rim-outward*8,outward,8.5f);
            await Tick(2.5f);
            Check(contact.Spatter>.22f,"Expected wet landing spatter.");
            walk=true;
            double exitEnd=Time.realtimeSinceStartupAsDouble+5;
            while(Time.realtimeSinceStartupAsDouble<exitEnd && contact.Depth>.01f)await Tick(.05f);
            walk=false;
            Check(contact.Depth<.01f&&contact.Coating>.15f&&contact.SpeedFactor>.99f,"Sahur didn't retain coating after returning to dry ground.");
            Shot("sahur-coated");
            float wetCoating=contact.Coating, spatter=contact.Spatter;
            await Tick(3f);
            Check(contact.Coating<wetCoating-.02f,"Runoff did not start drying the coating.");
            cases.Add(new{label="Coating above shoreline",coating=wetCoating,spatter,afterThreeSeconds=contact.Coating});
            var old=data.previousChannel.Select(p=>
            {
                if(!Floor(ground,p,out var h))return (pose:p,delta:0f);
                return (pose:p-Vector3.up*(player.LowestFootWorldOffset+.38f),delta:h.point.y-p.y);
            }).OrderByDescending(s=>s.delta).First();
            var corrected=contact.CorrectSavedPosition(old.pose);
            Check(corrected.x==old.pose.x&&corrected.z==old.pose.z&&corrected.y>old.pose.y+1,"Old low riverbed restore was not corrected vertically.");
            var valid=player.transform.position;
            Check((contact.CorrectSavedPosition(valid)-valid).sqrMagnitude<.01f,"Current dry shoreline save jumped on load.");
            cases.Add(new{label="Legacy riverbed and current shoreline save positions",verticalCorrection=corrected.y-old.pose.y});

            if(SecondIslandAudio.IsIsland)
            {
                Floor(ground,data.lakeCenter,out var wetFloor);
                Check(audio.InChocolate(wetFloor.point)&&!audio.InChocolate(data.lakeCenter+Vector3.up*.04f),"Foley and rendered immersion disagree.");
                Check(SecondIslandAudio.Movement(0,wetFloor.point,null).name.StartsWith("RiverStep"),"Wet step doesn't resolve river audio.");
                Check(audio.GetComponentsInChildren<AudioSource>().Any(s=>s.name.Contains("cloudfall")&&
                    Vector3.Distance(s.transform.position,data.FallLip)<.05f&&s.loop),"Fall audio did not follow the new lip.");
                cases.Add(new{label="Four ambient loops and relocated fall",loops=audio.GetComponentsInChildren<AudioSource>().Count(s=>s.loop)});
            }
            Check(errors.Count==0,"Runtime errors were recorded.");
        }
        catch(Exception e){failure=e.Message;}
        finally
        {
            walk=jump=false;Application.logMessageReceived-=Log;InputSystem.onBeforeUpdate-=Inject;
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
            if(original!=null&&original.added)original.MakeCurrent();
        }
        var result=new{passed=failure==null,failure,scene=player.gameObject.scene.path,cases,errors};
        File.WriteAllText(Folder+(SecondIslandAudio.IsIsland?"gameplay.json":"preview-gameplay.json"),
            Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static async Task<object> Saves()
    {
        Check(Application.isPlaying&&SecondIslandAudio.IsIsland&&GameSaveManager.BeginEditorTestSession(),
            "Start isolated Play Mode on formal Island 2.");
        string path=Path.GetFullPath(Folder+"isolated-continue.json");
        typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,path);
        var player=Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var ground=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<MeshCollider>();
        var contact=player.GetComponent<SecondIslandChocolateContact>();
        var river=SecondIslandAudio.Current.waterway;
        var opening=Object.FindFirstObjectByType<SecondIslandAwakening>();
        if(opening)Object.Destroy(opening.gameObject);
        await Tick(.2f);
        player.ExternalMovementLock=player.ExternalControlLock=false;
        Check(GameSaveManager.SaveCurrentGame(1),"Could not write isolated seed save.");
        string seed=File.ReadAllText(path);

        var old=river.previousChannel.Select(p=>
        {
            Floor(ground,p,out var hit);
            return (floor:hit.point.y,pose:p-Vector3.up*(player.LowestFootWorldOffset+.38f),gap:hit.point.y-p.y);
        }).OrderByDescending(p=>p.gap).First();
        var bank=river.lakeOutline.OrderBy(v=>Mathf.Abs(v.x+47)+Mathf.Abs(v.z-2021)).First();
        bank+=(bank-river.lakeCenter).normalized*9;
        Check(Floor(ground,bank,out var dry),"Current dry save has no floor.");
        Vector3 dryPose=dry.point+Vector3.up*(.085f-player.LowestFootWorldOffset);
        var cases=new List<object>();
        foreach(var entry in new[]{(name:"Old riverbed",pose:old.pose),(name:"New shoreline",pose:dryPose)})
        {
            var file=Newtonsoft.Json.Linq.JObject.Parse(seed);
            file["playerPosition"]=new Newtonsoft.Json.Linq.JObject{{"x",entry.pose.x},{"y",entry.pose.y},{"z",entry.pose.z}};
            File.WriteAllText(path,file.ToString());
            Check(GameSaveManager.QueueContinue(1),"Could not queue "+entry.name);
            var load=SceneManager.LoadSceneAsync(SecondIslandAudio.ScenePath);
            while(!load.isDone)await Tick(.04f);
            double end=Time.realtimeSinceStartupAsDouble+12;
            while(GameSaveManager.IsContinuePending&&Time.realtimeSinceStartupAsDouble<end)await Tick(.04f);
            Check(!GameSaveManager.IsContinuePending,"Restore timed out for "+entry.name);
            await Tick(.25f);
            player=Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            Vector3 actual=player.transform.position;
            float vertical=actual.y-entry.pose.y;
            Check(Vector2.Distance(XZ(actual),XZ(entry.pose))<.4f,entry.name+" changed its saved horizontal position.");
            Check(entry.name=="Old riverbed"?vertical>10:Mathf.Abs(vertical)<.5f,entry.name+" restored at an invalid height.");
            cases.Add(new{entry.name,vertical,restored=actual.ToString()});
        }
        File.WriteAllText(Folder+"save-restore.json",Newtonsoft.Json.JsonConvert.SerializeObject(cases,Newtonsoft.Json.Formatting.Indented));
        return cases;
    }
}
