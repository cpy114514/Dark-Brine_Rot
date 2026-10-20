using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class TestNailongVolumeV230
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Private).SetValue(o,v);
    static object Call(object o,string n)=>o.GetType().GetMethod(n,Private).Invoke(o,null);
    public static async Task<object> Run()
    {
        if(!Application.isPlaying)throw new Exception("Existing Editor Play Mode required.");
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())root.SetActive(false);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary Nailong validation floor";
        floor.transform.position=new Vector3(0,499.5f,0);floor.transform.localScale=new Vector3(100,1,100);
        var victim=new GameObject("Temporary excluded Nailong victim");victim.AddComponent<GameSaveExcluded>();
        victim.transform.position=new Vector3(0,500,12);victim.tag="Player";
        var body=victim.AddComponent<CharacterController>();body.height=2;body.radius=.3f;body.center=Vector3.up;
        var hp=victim.AddComponent<PlayerHealth>();hp.maxHealth=hp.currentHealth=100;
        var extra=new GameObject("Duplicate trigger hurtbox");extra.transform.SetParent(victim.transform,false);
        var trigger=extra.AddComponent<SphereCollider>();trigger.center=Vector3.up;trigger.radius=.35f;trigger.isTrigger=true;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        var boss=UnityEngine.Object.Instantiate(prefab,new Vector3(0,500,0),Quaternion.identity);
        var ai=boss.GetComponent<NailongAI>();ai.enabled=false;
        var effects=boss.GetComponent<NailongCombatEffects>();var attack=boss.GetComponent<NailongAttack>();
        var notes=new List<object>();int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);notes.Add(new{name,passed=true});passed++;};
        GameObject wall=null;var arcs=new List<NailongSpitProjectile>();
        var pattern=typeof(NailongAI).GetNestedType("AttackPattern",BindingFlags.NonPublic);
        Action<string> begin=n=>typeof(NailongAI).GetMethod("BeginAttack",Private).Invoke(ai,new[]{Enum.Parse(pattern,n)});
        Action<float> tick=t=>{Set(ai,"attackStartedAt",Time.time-t);Call(ai,"TickAttack");};
        try
        {
            EditorApplication.isPaused=false;Time.timeScale=1;int firstFrame=Time.frameCount;
            while(Time.frameCount<firstFrame+3||Time.deltaTime<=0)await Task.Delay(20);
            Set(ai,"target",victim.transform);Physics.SyncTransforms();
            check(ai.spitCount==1,"Legacy prefab volley count migrates to one at Awake");
            ai.spitCount=6; // Stale data must not re-enable a volley.
            begin("ScoldingSpit");tick(ai.spitWindup-.01f);
            check(effects.ActiveProjectileCount==0,"Windup cannot spawn saliva");
            tick(ai.spitWindup+.01f);tick(ai.spitWindup+.02f);Call(effects,"LateUpdate");
            check(effects.ActiveProjectileCount==1,"Repeated release tick produces exactly one glob");
            var visibleGlob=UnityEngine.Object.FindObjectsByType<NailongSpitProjectile>(FindObjectsSortMode.None).Single(s=>s.gameObject.activeSelf&&!s.HasImpacted);
            check(Mathf.Abs(visibleGlob.CollisionRadius-.24f*ai.SizeFactor)<.00001f,"Water diameter and collision radius are twice v228 size");
            tick(ai.spitWindup+.5f);Call(effects,"LateUpdate");
            check(effects.ActiveProjectileCount==1,"Old six-shot serialization cannot add shots");
            effects.Cancel();
            check(effects.ActiveProjectileCount==0,"Cancel immediately recycles active glob");
            foreach(int hz in new[]{10,30,60})
            {
                hp.currentHealth=100;victim.transform.position=new Vector3(0,500,12);Physics.SyncTransforms();
                var shot=effects.Spit(victim.transform,ai.EffectiveSpitSpeed,16);arcs.Add(shot);
                check(shot!=null&&shot.Velocity.y>0,"Ballistic launch rises before descent at "+hz+" Hz");
                float maxY=shot.transform.position.y,startY=maxY;
                for(int i=0;i<hz*3&&!shot.HasImpacted;i++){shot.Simulate(1f/hz);maxY=Mathf.Max(maxY,shot.transform.position.y);}
                check(shot.HasImpacted&&Mathf.Abs(hp.currentHealth-84)<.001f,"Arc hits real capsule exactly once at "+hz+" Hz");
                check(maxY>startY+.45f,"Visible apex above mouth at "+hz+" Hz");
                shot.Simulate(.05f);check(hp.currentHealth==84,"Splash cannot damage duplicate hurtbox at "+hz+" Hz");
                effects.Cancel();
            }
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,504,5);
            wall.transform.localScale=new Vector3(8,12,.1f);Physics.SyncTransforms();hp.currentHealth=100;
            var blocked=effects.Spit(victim.transform,ai.EffectiveSpitSpeed,16);arcs.Add(blocked);blocked.Simulate(1);
            check(blocked.HasImpacted&&hp.currentHealth==100,"Full one-second step sweeps arc into thin wall");
            UnityEngine.Object.DestroyImmediate(wall);wall=null;effects.Cancel();
            begin("ScoldingSpit");tick(ai.spitWindup+.01f);ai.Interrupt(.4f);Call(effects,"LateUpdate");
            check(effects.ActiveProjectileCount==0,"Stagger cancels queued but unreleased glob");
            Set(ai,"state",NailongAI.State.Idle);begin("Rampage");tick(1.66f);Call(effects,"LateUpdate");
            check(effects.ActiveProjectileCount==1,"Rampage release also produces one glob rather than fan volley");effects.Cancel();
            Set(ai,"state",NailongAI.State.Idle);
            // Ballistic position must be independent of frame partition, not semi-implicit Euler.
            var point=new Vector3(30,540,0);var initial=new Vector3(1,4,8);
            Vector3 expected=point+initial*.5f+Vector3.down*(.5f*18*.5f*.5f);
            foreach(int hz in new[]{10,30,60})
            {
                var shot=effects.Spit(victim.transform,ai.EffectiveSpitSpeed,0);arcs.Add(shot);
                shot.transform.position=point;shot.Launch(boss.transform,victim.transform,initial,0,.1f,18);
                for(int i=0;i<hz/2;i++)shot.Simulate(1f/hz);
                check(Vector3.Distance(shot.transform.position,expected)<.0002f,"Exact ballistic position at "+hz+" Hz");effects.Cancel();
            }
            var geometryGo=new GameObject("Closed 3D pressure geometry contract");geometryGo.transform.position=new Vector3(0,500,0);
            var geometry=geometryGo.AddComponent<NailongGroundSlam>();geometry.Initialize(attack,10,1,1,10,.4f,true);
            geometry.Tick(.001f);
            var mesh=geometryGo.GetComponentInChildren<MeshFilter>().sharedMesh;
            check(mesh.vertexCount==65*13&&mesh.triangles.Length==64*12*6,"Full 360-degree torus with twelve closed cross-section faces");
            check(mesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&new Vector2(v.x,v.z).magnitude>0),"Birth geometry stays finite without inverted inner radius");
            geometry.Tick(3);
            var verts=mesh.vertices;var normals=mesh.normals;
            check(Enumerable.Range(0,65).All(i=>Vector3.Distance(verts[i*13],verts[i*13+12])<.00001f)&&Enumerable.Range(0,13).All(i=>Vector3.Distance(verts[i],verts[64*13+i])<.00001f),"Both geometric seams close exactly");
            check(Enumerable.Range(0,65).All(i=>Vector3.Distance(normals[i*13],normals[i*13+12])<.00001f),"Tube seam normals are continuous");
            check(mesh.bounds.size.y>.70f&&normals.Any(n=>n.y<-.9f)&&normals.Any(n=>n.y>.9f),"Volume includes rounded top and underside, not a raised flat strip");
            var triangles=mesh.triangles;
            check(Enumerable.Range(0,triangles.Length/3).All(i=>Vector3.Dot(Vector3.Cross(verts[triangles[i*3+1]]-verts[triangles[i*3]],verts[triangles[i*3+2]]-verts[triangles[i*3]]),normals[triangles[i*3]])>0),"Every torus triangle faces outward for single-pass backface culling");
            geometry.Tick(.001f);long waveBytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)geometry.Tick(.001f);
            waveBytes=GC.GetAllocatedBytesForCurrentThread()-waveBytes;
            check(waveBytes==0,"1000 warmed closed-torus updates allocate zero managed bytes");
            UnityEngine.Object.DestroyImmediate(geometryGo);
            var water=Resources.Load<Material>("BossCombat/NailongSaliva");
            check(water.GetColor("_WaterTint").r<.2f&&water.GetFloat("_Absorption")>=.4f,"Persistent saliva material uses deep teal transmission and body absorption");
            hp.currentHealth=100;victim.transform.position=new Vector3(4,500,0);Physics.SyncTransforms();
            var waveGo=new GameObject("Native damage wave contract");waveGo.transform.position=new Vector3(0,500,0);
            var wave=waveGo.AddComponent<NailongGroundSlam>();wave.Initialize(attack,10,1,1,1,.4f);
            for(int i=0;i<60;i++)wave.Tick(1f/60);
            check(hp.currentHealth==85&&wave.DamageHits==1,"Transparent sonar retains one fixed 15-damage crossing");
            UnityEngine.Object.DestroyImmediate(waveGo);
            hp.currentHealth=100;body.enabled=false;victim.transform.position=new Vector3(4,501,0);body.enabled=true;Physics.SyncTransforms();
            waveGo=new GameObject("Jump wave contract");waveGo.transform.position=new Vector3(0,500,0);
            wave=waveGo.AddComponent<NailongGroundSlam>();wave.Initialize(attack,10,1,1,1,.4f);
            for(int i=0;i<60;i++)wave.Tick(1f/60);
            check(hp.currentHealth==100&&wave.JumpDodges==1,"Transparent sonar remains jumpable");UnityEngine.Object.DestroyImmediate(waveGo);
            victim.transform.position=new Vector3(4,500,0);Physics.SyncTransforms();
            waveGo=new GameObject("Cosmetic roar pressure contract");waveGo.transform.position=new Vector3(0,500,0);
            wave=waveGo.AddComponent<NailongGroundSlam>();wave.Initialize(attack,10,1,1,1,.65f,true);
            for(int i=0;i<60;i++)wave.Tick(1f/60);
            check(hp.currentHealth==100&&wave.DamageHits==0,"Roar pressure visual adds no second damage event");UnityEngine.Object.DestroyImmediate(waveGo);
            foreach(string name in new[]{"NailongSpit","NailongRoar"})
            {
                var clip=Resources.Load<AnimationClip>("BossCombat/Animations/v228/"+name);
                check(clip&&clip.humanMotion&&!clip.isLooping,"Complete non-looping humanoid action: "+name);
            }
            check(Mathf.Abs(NailongPhaseAnimation.MapCueTime(1.4f,2.1f,1.4f,1.6f,.9f)-.9f)<1e-5f,"Authored release remaps exactly to configured cue");
            var pooled=effects.Spit(victim.transform,16,0);effects.Cancel();var reused=effects.Spit(victim.transform,16,0);
            check(pooled==reused,"Projectile and all water beads reuse prewarmed pool");effects.Cancel();
            // A warm isolated projectile update should allocate no managed memory.
            var perf=effects.Spit(victim.transform,16,0);perf.transform.position=point;perf.Launch(boss.transform,victim.transform,initial,0,.1f,18);
            perf.Simulate(.001f);long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)perf.Simulate(.001f);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;
            check(allocated==0,"1000 warmed ballistic and water-visual updates allocate zero managed bytes");effects.Cancel();
            var result=new{passed,notes,allocated};Directory.CreateDirectory(".codex/nailong-volume-20261020");
            File.WriteAllText(".codex/nailong-volume-20261020/contracts.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            effects.Cancel();UnityEngine.Object.DestroyImmediate(boss);UnityEngine.Object.DestroyImmediate(victim);
            UnityEngine.Object.DestroyImmediate(floor);if(wall)UnityEngine.Object.DestroyImmediate(wall);
        }
    }
}
